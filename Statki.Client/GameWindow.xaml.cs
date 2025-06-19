using Statki.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.Json;

namespace Statki.Client
{
    public partial class GameWindow : Window
    {
        private GameBoard _playerBoard;
        private TcpClient? client;
        private NetworkStream? stream;

        private int currentShipLength = 4;
        private int shipsPlaced = 0;
        private Direction currentDirection = Direction.Horizontal;
        private bool placingShipsMode = true;
        private List<Button> previewedCells = new();
        private List<Position> invalidPlacementPositions = new(); // Dodana lista
        private bool isMyTurn = false;

        private readonly List<int> shipsToPlace = new() { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

        public GameWindow(TcpClient client, NetworkStream stream)
        {
            InitializeComponent();

            this.client = client;
            this.stream = stream;

            _playerBoard = new GameBoard();

            GenerateGrid(PlayerGrid, isPlayer: true);
            GenerateGrid(EnemyGrid, isPlayer: false);

            this.KeyDown += GameWindow_KeyDown;
        }

        private void GenerateGrid(UniformGrid grid, bool isPlayer)
        {
            for (int y = 0; y < GameBoard.Size; y++)
            {
                for (int x = 0; x < GameBoard.Size; x++)
                {
                    var cell = new Button
                    {
                        Tag = (x, y),
                        Margin = new Thickness(1),
                        Background = isPlayer ? Brushes.LightBlue : Brushes.LightGray
                    };

                    if (isPlayer)
                    {
                        cell.MouseEnter += PlayerGrid_HighlightPreview;
                        cell.Click += PlayerGrid_ConfirmShip;
                    }
                    else
                    {
                        cell.Click += EnemyGrid_Click;
                    }

                    grid.Children.Add(cell);
                }
            }
        }

        private async Task ListenToServerAsync()
        {
            byte[] buffer = new byte[1024];
            while (client?.Connected == true)
            {
                int length = await stream!.ReadAsync(buffer, 0, buffer.Length);
                if (length == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, length);
                Console.WriteLine($"Otrzymano od serwera: {message}");

                if (message.StartsWith("HIT") || message.StartsWith("MISS") || message.StartsWith("SUNK"))
                {
                    var parts = message.Split(' ');
                    string resultType = parts[0];
                    var coords = parts[1].Split(',');
                    int x = int.Parse(coords[0]);
                    int y = int.Parse(coords[1]);
                    var btn = GetCellAt(EnemyGrid, x, y);

                    if (btn != null)
                    {
                        switch (resultType)
                        {
                            case "HIT":
                            case "SUNK":
                                btn.Background = Brushes.OrangeRed;
                                btn.BorderBrush = Brushes.OrangeRed;
                                btn.Foreground = Brushes.White;
                                btn.Content = null;
                                break;
                            case "MISS":
                                btn.Background = Brushes.LightGray;
                                btn.Content = new TextBlock
                                {
                                    Text = "X",
                                    Foreground = Brushes.Black,
                                    FontSize = 20,
                                    FontWeight = FontWeights.Bold,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    TextAlignment = TextAlignment.Center
                                };
                                break;
                        }
                        btn.IsHitTestVisible = false;
                    }
                }
                else if (message == "YOUR_TURN")
                {
                    isMyTurn = true;
                    Dispatcher.Invoke(() => MessageBox.Show("Twoja tura!"));
                }
                else if (message == "WAIT_TURN")
                {
                    isMyTurn = false;
                    Dispatcher.Invoke(() => MessageBox.Show("Czekaj na przeciwnika..."));
                }
                else if (message.StartsWith("WIN"))
                {
                    isMyTurn = false;
                    Dispatcher.Invoke(() => MessageBox.Show("Wygrałeś!"));
                }
                else if (message.StartsWith("LOSE"))
                {
                    isMyTurn = false;
                    Dispatcher.Invoke(() => MessageBox.Show("Przegrałeś!"));
                }
            }
        }


        private void PlayerGrid_HighlightPreview(object sender, MouseEventArgs e)
        {
            if (!placingShipsMode || !(sender is Button btn) || !(btn.Tag is ValueTuple<int, int> coords))
                return;

            int startX = coords.Item1;
            int startY = coords.Item2;

            ClearPreview();

            var positions = GetPotentialShipPositions(startX, startY, currentShipLength, currentDirection);
            if (positions == null)
                return;

            foreach (var pos in positions)
            {
                var cell = GetCellAt(PlayerGrid, pos.X, pos.Y);
                if (cell != null)
                {
                    cell.Background = Brushes.Yellow;
                    previewedCells.Add(cell);
                }
            }
        }
        private async void PlayerGrid_ConfirmShip(object sender, RoutedEventArgs e)
        {
            if (!placingShipsMode || previewedCells.Count == 0)
                return;

            var first = previewedCells[0];
            if (!(first.Tag is ValueTuple<int, int> coords))
                return;

            var ship = new Ship(coords.Item1, coords.Item2, currentShipLength, currentDirection);
            if (!_playerBoard.PlaceShip(ship))
            {
                return;
            }

            foreach (var pos in ship.Positions)
            {
                var cell = GetCellAt(PlayerGrid, pos.X, pos.Y);
                if (cell != null)
                    cell.Background = Brushes.DarkBlue;
            }

            // Dodanie pól wokół statku jako niedostępne
            MarkSurroundingCellsAsInvalid(ship);

            previewedCells.Clear();
            shipsPlaced++;

            if (shipsPlaced >= shipsToPlace.Count)
            {
                placingShipsMode = false;
                MessageBox.Show("Wszystkie statki rozmieszczone!");
                ResetAllInvalidPlacementColors();

                // Wyślij gotowość do serwera
                if (stream != null)
                {
                    string shipsJson = JsonSerializer.Serialize(_playerBoard.Ships);
                    byte[] boardMsg = Encoding.UTF8.GetBytes("BOARD " + shipsJson);
                    await stream.WriteAsync(boardMsg, 0, boardMsg.Length);
                }

                // Rozpocznij nasłuchiwanie odpowiedzi od serwera
                _ = ListenToServerAsync();
            }
            else
            {
                currentShipLength = shipsToPlace[shipsPlaced];
            }
        }

        private void ResetAllInvalidPlacementColors()
        {
            for (int y = 0; y < GameBoard.Size; y++)
            {
                for (int x = 0; x < GameBoard.Size; x++)
                {
                    var position = new Position { X = x, Y = y };
                    var cell = GetCellAt(PlayerGrid, x, y);
                    if (cell != null && cell.Background == Brushes.Red) // Sprawdź, czy komórka jest czerwona
                    {
                        cell.Background = Brushes.LightBlue; // Domyślny kolor
                    }
                }
            }
        }

        private void MarkSurroundingCellsAsInvalid(Ship ship)
        {
            foreach (var pos in ship.Positions)
            {
                for (int x = pos.X - 1; x <= pos.X + 1; x++)
                {
                    for (int y = pos.Y - 1; y <= pos.Y + 1; y++)
                    {
                        if (GameBoard.IsInBounds(x, y) && !ship.Positions.Any(p => p.X == x && p.Y == y))
                        {
                            if (!invalidPlacementPositions.Any(p => p.X == x && p.Y == y))
                            {
                                invalidPlacementPositions.Add(new Position { X = x, Y = y });
                                var cell = GetCellAt(PlayerGrid, x, y);
                                if (cell != null)
                                {
                                    cell.Background = Brushes.Red; // Opcjonalnie: wizualne oznaczenie
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ClearPreview()
        {
            foreach (var cell in previewedCells)
            {
                if (cell != null)
                    cell.Background = Brushes.LightBlue;
            }
            previewedCells.Clear();
        }

        private List<Position>? GetPotentialShipPositions(int startX, int startY, int length, Direction direction)
        {
            var positions = new List<Position>();

            for (int i = 0; i < length; i++)
            {
                int x = direction == Direction.Horizontal ? startX + i : startX;
                int y = direction == Direction.Vertical ? startY + i : startY;

                if (!GameBoard.IsInBounds(x, y) || _playerBoard.IsOccupied(x, y) || invalidPlacementPositions.Any(p => p.X == x && p.Y == y))
                    return null;

                positions.Add(new Position { X = x, Y = y });
            }

            return positions;
        }

        private Button? GetCellAt(UniformGrid grid, int x, int y)
        {
            int index = y * GameBoard.Size + x;
            if (index >= 0 && index < grid.Children.Count)
                return grid.Children[index] as Button;
            return null;
        }

        private void GameWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (placingShipsMode && e.Key == Key.R)
            {
                currentDirection = currentDirection == Direction.Horizontal ? Direction.Vertical : Direction.Horizontal;
                // Odśwież podświetlenie, aby uwzględnić zmianę kierunku
                if (Mouse.DirectlyOver is Button btn && btn.Tag is ValueTuple<int, int> coords)
                {
                    PlayerGrid_HighlightPreview(btn, new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount));
                }
            }
        }

        private async void EnemyGrid_Click(object sender, RoutedEventArgs e)
        {
            if (!isMyTurn)
            {
                Console.WriteLine("[Client] Nie jest twoja tura - kliknięcie ignorowane.");
                return;
            }

            if (sender is Button btn && btn.Tag is ValueTuple<int, int> coords)
            {
                int x = coords.Item1;
                int y = coords.Item2;

                if (!btn.IsEnabled)
                {
                    Console.WriteLine("[Client] To pole zostało już trafione.");
                    return;
                }

                string fireMsg = $"FIRE {x},{y}";
                byte[] data = Encoding.UTF8.GetBytes(fireMsg);
                await stream!.WriteAsync(data, 0, data.Length);

                Console.WriteLine($"[Client] Wysłano strzał na: {x},{y}");
                isMyTurn = false;
            }
        }
    }
}

using Statki.Shared.Models;
using System;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

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

        private void PlayerGrid_ConfirmShip(object sender, RoutedEventArgs e)
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

            previewedCells.Clear();
            shipsPlaced++;

            if (shipsPlaced >= shipsToPlace.Count)
            {
                placingShipsMode = false;
                MessageBox.Show("Wszystkie statki rozmieszczone!");
            }
            else
            {
                currentShipLength = shipsToPlace[shipsPlaced];
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

                if (!GameBoard.IsInBounds(x, y) || _playerBoard.IsOccupied(x, y))
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
            }
        }

        private void EnemyGrid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ValueTuple<int, int> coords)
            {
                int x = coords.Item1;
                int y = coords.Item2;
                MessageBox.Show($"Strzał na planszy przeciwnika – pole ({x}, {y})");
                btn.Background = Brushes.DarkGray;

                //TODO: wysłanie strzału do serwera
            }
        }
    }
}
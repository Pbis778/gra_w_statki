using Statki.Shared.Models;
using System;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Statki.Client
{
    public partial class GameWindow : Window
    {
        private GameBoard _playerBoard;
        private TcpClient? client;
        private NetworkStream? stream;

        public GameWindow(TcpClient client, NetworkStream stream)
        {
            InitializeComponent();
            this.client = client;
            this.stream = stream;

            _playerBoard = new GameBoard();

            GenerateGrid(PlayerGrid, isPlayer: true);
            GenerateGrid(EnemyGrid, isPlayer: false);
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

                    cell.Click += isPlayer ? PlayerGrid_Click : EnemyGrid_Click;

                    grid.Children.Add(cell);
                }
            }
        }

        private void PlayerGrid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ValueTuple<int, int> coords)
            {
                int x = coords.Item1;
                int y = coords.Item2;
                MessageBox.Show($"Twoja plansza – kliknięto pole ({x}, {y})");
                //w przyszłości: rozmieszczanie statków
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

using Statki.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Statki.Client
{
    public partial class GameWindow : Window
    {
        private GameBoard _board;
        private TcpClient client;
        private NetworkStream stream;

        public GameWindow(TcpClient client, NetworkStream stream)
        {
            InitializeComponent();
            this.client = client;
            this.stream = stream;
            _board = new GameBoard();
            GenerateGrid();
        }

        private void GenerateGrid()
        {
            for (int y = 0; y < GameBoard.Size; y++)
            {
                for (int x = 0; x < GameBoard.Size; x++)
                {
                    var cell = new Button
                    {
                        Tag = (x, y),
                        Margin = new Thickness(1),
                        Background = Brushes.LightBlue
                    };

                    cell.Click += Cell_Click;
                    GameGrid.Children.Add(cell);
                }
            }
        }

        private void Cell_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ValueTuple<int, int> coords)
            {
                int x = coords.Item1;
                int y = coords.Item2;
                MessageBox.Show($"Kliknięto pole ({x}, {y})");
                // tutaj możesz wysłać strzał do serwera lub zaznaczyć miejsce
            }
        }
    }
}

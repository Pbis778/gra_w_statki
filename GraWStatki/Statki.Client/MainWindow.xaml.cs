using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Statki.Client
{
    public partial class MainWindow : Window
    {
        private TcpClient client;
        private NetworkStream stream;
        private Thread listenThread;
        private volatile bool isListening = false; // flaga do kontrolowania pętli
        private Window waitingWindow;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void ConnectToServer(string ip)
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(ip, 8080);
                stream = client.GetStream();

                StartListening();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd połączenia: " + ex.Message);
            }
        }

        private void StartListening()
        {
            isListening = true;
            listenThread = new Thread(() =>
            {
                while (isListening && client.Connected)
                {
                    try
                    {
                        byte[] buffer = new byte[1024];
                        int length = stream.Read(buffer, 0, buffer.Length);
                        if (length > 0)
                        {
                            string message = Encoding.UTF8.GetString(buffer, 0, length);

                            Dispatcher.Invoke(() =>
                            {
                                HandleServerMessage(message);
                            });
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show("Serwer rozłączył połączenie.");
                                CloseAllWindows();
                            });
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        if (isListening)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show("Rozłączono z serwerem.");
                                CloseAllWindows();
                            });
                        }
                        break;
                    }
                }
            });

            listenThread.IsBackground = true;
            listenThread.Start();
        }

        private void HandleServerMessage(string message)
        {
            switch (message)
            {
                case "WAIT":
                    ShowWaitingWindow();
                    break;

                case "START":
                    CloseWaitingWindow();
                    this.Hide();
                    var gameWindow = new GameWindow(client, stream);
                    gameWindow.Show();
                    break;

                default:
                    MessageBox.Show("Wiadomość z serwera: " + message);
                    break;
            }
        }

        private void ShowWaitingWindow()
        {
            if (waitingWindow == null)
            {
                waitingWindow = new Window
                {
                    Title = "Oczekiwanie",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Content = new Grid
                    {
                        Children =
        {
            new TextBlock
            {
                Text = "Oczekiwanie na przeciwnika...",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            }
        }
                    }
                };

                waitingWindow.Show();
            }
        }

        private void CloseWaitingWindow()
        {
            if (waitingWindow != null)
            {
                waitingWindow.Close();
                waitingWindow = null;
            }
        }

        private void SendMessage(string message)
        {
            if (client == null || !client.Connected) return;

            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);
        }

        private void CloseAllWindows()
        {
            isListening = false;

            try { stream?.Close(); } catch { }
            try { client?.Close(); } catch { }

            Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            CloseAllWindows();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            ConnectToServer(txtIP.Text);
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            SendMessage(txtMessage.Text);
        }
    }
}
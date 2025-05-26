using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace Statki.Client
{
    public partial class MainWindow : Window
    {
        private TcpClient client;
        private NetworkStream stream;
        private Thread listenThread;
        private volatile bool isListening = false; // flaga do kontrolowania pętli

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ConnectToServer(string ip)
        {
            try
            {
                client = new TcpClient();
                client.Connect(ip, 8080);
                stream = client.GetStream();

                MessageBox.Show("Połączono z serwerem!");

                StartListening(); // Uruchom nasłuchiwanie od serwera

                this.Hide();

                var gameWindow = new GameWindow(client, stream); // Przekaż połączenie do GameWindow
                gameWindow.Show();
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
                            // Połączenie zamknięte od serwera (length == 0)
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
                        if (isListening) // jeśli nie zamknęliśmy sami
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
            // Przykładowa obsługa komunikatu z serwera
            MessageBox.Show("Wiadomość z serwera: " + message);
            // W przyszłości tutaj można przesyłać dane do GameWindow
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

            try
            {
                stream?.Close();
            }
            catch { }
            try
            {
                client?.Close();
            }
            catch { }

            // Zamknij wszystkie okna aplikacji (łącznie z MainWindow i GameWindow)
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

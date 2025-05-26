using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Statki.Serwer
{
    public partial class MainWindow : Window
    {
        private TcpListener listener;
        private TcpClient client;
        private NetworkStream stream;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            Log("Uruchamianie serwera...");
            await StartServerAsync();
        }

        private async Task StartServerAsync()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, 8080);
                listener.Start();
                Log("Serwer nasłuchuje na porcie 8080...");

                client = await listener.AcceptTcpClientAsync();
                Log("Połączono z klientem!");

                stream = client.GetStream();
                await ReceiveMessagesAsync();
            }
            catch (Exception ex)
            {
                Log("Błąd: " + ex.Message);
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int length = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (length == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, length);
                    Log("Odebrano: " + message);

                    // Odpowiedź (opcjonalna)
                    string response = "Odebrano: " + message;
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(responseData, 0, responseData.Length);
                }
            }
            catch (Exception ex)
            {
                Log("Połączenie zakończone. " + ex.Message);
            }
            finally
            {
                stream?.Close();
                client?.Close();
                listener?.Stop();
                Log("Połączenie zamknięte.");
                btnStart.IsEnabled = true;
            }
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtLog.AppendText(message + Environment.NewLine);
                txtLog.ScrollToEnd();
            });
        }
    }
}
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Statki.Serwer
{
    public partial class MainWindow : Window
    {
        private TcpListener listener;
        private readonly List<TcpClient> clients = new List<TcpClient>();
        private readonly List<NetworkStream> streams = new List<NetworkStream>();
        private const int MaxClients = 2;

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

                while (true)
                {
                    var newClient = await listener.AcceptTcpClientAsync();

                    if (clients.Count >= MaxClients)
                    {
                        Log("Odrzucono połączenie: limit 2 graczy.");
                        using var rejectStream = newClient.GetStream();
                        byte[] rejectMsg = Encoding.UTF8.GetBytes("Serwer pełny - tylko 2 graczy może się połączyć.");
                        await rejectStream.WriteAsync(rejectMsg, 0, rejectMsg.Length);
                        newClient.Close();
                        continue;
                    }

                    clients.Add(newClient);
                    var stream = newClient.GetStream();
                    streams.Add(stream);

                    Log($"Gracz {clients.Count} dołączył.");
                    _ = ReceiveMessagesAsync(newClient, stream);
                }
            }
            catch (Exception ex)
            {
                Log("Błąd serwera: " + ex.Message);
            }
        }

        private async Task ReceiveMessagesAsync(TcpClient client, NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (client.Connected)
                {
                    int length = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (length == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, length);
                    Log($"Odebrano: {message}");

                    // Broadcast do drugiego gracza
                    foreach (var s in streams)
                    {
                        if (s != stream) // Nie wysyłaj do nadawcy
                        {
                            byte[] responseData = Encoding.UTF8.GetBytes(message);
                            await s.WriteAsync(responseData, 0, responseData.Length);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Połączenie zakończone. " + ex.Message);
            }
            finally
            {
                int index = clients.IndexOf(client);
                clients.Remove(client);
                streams.Remove(stream);
                stream?.Close();
                client?.Close();
                Log($"Gracz {index + 1} odłączony.");
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

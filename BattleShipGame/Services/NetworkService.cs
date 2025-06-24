using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BattleShip.Services
{
    public class NetworkService
    {
        private TcpListener tcpListener;
        private TcpClient tcpClient;
        private NetworkStream stream;

        public event Action<string> MessageReceived;
        public event Action ConnectionLost;
        public event Action<string> StatusChanged;

        public bool IsConnected => tcpClient?.Connected ?? false;
        public bool IsServer { get; private set; }

        public async Task<bool> StartServer(int port)
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                IsServer = true;

                StatusChanged?.Invoke("Oczekiwanie na gracza...");

                tcpClient = await tcpListener.AcceptTcpClientAsync();
                stream = tcpClient.GetStream();

                StatusChanged?.Invoke("Gracz dołączył!");
                _ = Task.Run(ListenForMessages);
                return true;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Błąd serwera: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ConnectToServer(string ipAddress, int port)
        {
            try
            {
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(ipAddress, port);
                stream = tcpClient.GetStream();
                IsServer = false;

                StatusChanged?.Invoke("Połączono z serwerem!");
                _ = Task.Run(ListenForMessages);
                return true;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Błąd połączenia: {ex.Message}");
                return false;
            }
        }

        public async Task SendMessage(string message)
        {
            try
            {
                if (stream != null && IsConnected)
                {
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Błąd wysyłania: {ex.Message}");
            }
        }

        private async Task ListenForMessages()
        {
            byte[] buffer = new byte[1024];

            try
            {
                while (IsConnected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        MessageReceived?.Invoke(message);
                    }
                }
            }
            catch
            {
                ConnectionLost?.Invoke();
            }
        }

        public void Disconnect()
        {
            try
            {
                stream?.Close();
                tcpClient?.Close();
                tcpListener?.Stop();
            }
            catch { }

            StatusChanged?.Invoke("Rozłączono");
        }
    }
}

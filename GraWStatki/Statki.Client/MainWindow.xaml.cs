using System.Net.Sockets;
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

namespace Statki.Client
{
    public partial class MainWindow : Window
    {
        private TcpClient client;
        private NetworkStream stream;

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
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd połączenia: " + ex.Message);
            }
        }

        private void SendMessage(string message)
        {
            if (client == null || !client.Connected) return;

            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);

            // Odbiór odpowiedzi
            byte[] buffer = new byte[1024];
            int length = stream.Read(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, length);
            MessageBox.Show("Odpowiedź serwera: " + response);
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
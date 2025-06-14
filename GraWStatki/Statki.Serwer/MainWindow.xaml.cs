﻿using Statki.Shared.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
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
        GameBoard[] boards = new GameBoard[2];
        private bool[] isReady = new bool[2];
        private int currentPlayerIndex = 0;

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

                    if (clients.Count == 1)
                    {
                        byte[] waitMsg = Encoding.UTF8.GetBytes("WAIT");
                        await stream.WriteAsync(waitMsg, 0, waitMsg.Length);
                        Log("Pierwszy gracz dołączył, czeka na przeciwnika...");
                    }
                    else if (clients.Count == 2)
                    {
                        foreach (var client in clients)
                        {
                            var clientStream = client.GetStream();
                            byte[] startMsg = Encoding.UTF8.GetBytes("START");
                            await clientStream.WriteAsync(startMsg, 0, startMsg.Length);
                        }
                        Log("Drugi gracz dołączył - start gry");
                    }

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

                    int playerIndex = clients.IndexOf(client);

                    if (message.StartsWith("BOARD "))
                    {
                        string json = message.Substring("BOARD ".Length);
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            IncludeFields = true
                        };
                        List<Ship> ships = JsonSerializer.Deserialize<List<Ship>>(json, options);

                        if (boards[playerIndex] == null)
                            boards[playerIndex] = new GameBoard();

                        boards[playerIndex].Ships = ships;
                        Log($"Gracz {playerIndex + 1} przesłał statki.");
                        isReady[playerIndex] = true;

                        if (isReady[0] && isReady[1])
                        {
                            currentPlayerIndex = 0;

                            await streams[0].WriteAsync(Encoding.UTF8.GetBytes("YOUR_TURN"));
                            await streams[1].WriteAsync(Encoding.UTF8.GetBytes("WAIT_TURN"));

                            Log("Gra się rozpoczęła!");
                        }
                    }
                    else if (message.StartsWith("FIRE "))
                    {
                        if (playerIndex != currentPlayerIndex)
                        {
                            await streams[playerIndex].WriteAsync(Encoding.UTF8.GetBytes("WAIT_TURN"));
                            continue;
                        }

                        int opponentIndex = 1 - playerIndex;
                        string[] parts = message.Split(' ');
                        string[] xy = parts[1].Split(',');

                        int x = int.Parse(xy[0]);
                        int y = int.Parse(xy[1]);

                        var (isHit, isSunk, hitShip) = boards[opponentIndex].ReceiveShot(x, y);

                        string resultType = isHit ? (isSunk ? "SUNK" : "HIT") : "MISS";
                        string resultMsg = $"{resultType} {x},{y}";

                        if (boards[opponentIndex].AllShipsSunk())
                        {
                            await streams[playerIndex].WriteAsync(Encoding.UTF8.GetBytes($"WIN {x},{y}"));
                            await streams[opponentIndex].WriteAsync(Encoding.UTF8.GetBytes($"LOSE {x},{y}"));
                            Log($"Gracz {playerIndex + 1} wygrał!");
                            continue;
                        }

                        await streams[playerIndex].WriteAsync(Encoding.UTF8.GetBytes(resultMsg));
                        await streams[opponentIndex].WriteAsync(Encoding.UTF8.GetBytes(resultMsg));

                        if (!isHit)
                            currentPlayerIndex = opponentIndex;

                        await streams[currentPlayerIndex].WriteAsync(Encoding.UTF8.GetBytes("YOUR_TURN"));
                        await streams[1 - currentPlayerIndex].WriteAsync(Encoding.UTF8.GetBytes("WAIT_TURN"));
                    }
                    else
                    {
                        Log($"Nieznana wiadomość: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Połączenie zakończone: " + ex.Message);
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

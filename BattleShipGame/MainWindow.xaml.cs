using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BattleShip.Models;
using BattleShip.Services;
using BattleShip.Utilities;

namespace BattleShip
{
    public partial class MainWindow : Window
    {
        private NetworkService networkService;
        private GameLogicService gameLogic;
        private ChatService chatService;

        private Button[,] playerButtons = new Button[GameConstants.GRID_SIZE, GameConstants.GRID_SIZE];
        private Button[,] enemyButtons = new Button[GameConstants.GRID_SIZE, GameConstants.GRID_SIZE];

        private bool isPlacingHorizontally = true;

        public MainWindow()
        {
            InitializeComponent();
            InitializeServices();
            InitializeGame();
        }

        private void InitializeServices()
        {
            networkService = new NetworkService();
            gameLogic = new GameLogicService();
            chatService = new ChatService();

            networkService.MessageReceived += OnMessageReceived;
            networkService.ConnectionLost += OnConnectionLost;
            networkService.StatusChanged += OnNetworkStatusChanged;

            gameLogic.StatusChanged += OnGameStatusChanged;
            gameLogic.TurnChanged += OnTurnChanged;
            gameLogic.GameEnded += OnGameEnded;

            chatService.MessageAdded += OnChatMessageAdded;
        }

        private void InitializeGame()
        {
            CreateGameGrids();
            UpdateUI();
            UpdateGameStatus("Gotowy do gry - wybierz opcję połączenia");
        }

        private void CreateGameGrids()
        {
            CreateGrid(PlayerGrid, playerButtons, true);
            CreateGrid(EnemyGrid, enemyButtons, false);
        }

        private void CreateGrid(Grid targetGrid, Button[,] buttonArray, bool isPlayerGrid)
        {
            targetGrid.Children.Clear();
            targetGrid.RowDefinitions.Clear();
            targetGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < GameConstants.GRID_SIZE; i++)
            {
                targetGrid.RowDefinitions.Add(new RowDefinition());
                targetGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int row = 0; row < GameConstants.GRID_SIZE; row++)
            {
                for (int col = 0; col < GameConstants.GRID_SIZE; col++)
                {
                    Button cellButton = new Button
                    {
                        Style = (Style)FindResource("WaterCellStyle"),
                        Tag = $"{row},{col}"
                    };

                    if (isPlayerGrid)
                    {
                        cellButton.Click += PlayerCell_Click;
                        cellButton.MouseRightButtonDown += PlayerCell_RightClick;
                    }
                    else
                    {
                        cellButton.Click += EnemyCell_Click;
                    }

                    Grid.SetRow(cellButton, row);
                    Grid.SetColumn(cellButton, col);
                    targetGrid.Children.Add(cellButton);
                    buttonArray[row, col] = cellButton;
                }
            }
        }

        private async void CreateGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int port = int.Parse(PortTextBox.Text);
                bool success = await networkService.StartServer(port);

                if (success)
                {
                    gameLogic.CurrentState = GameState.PlacingShips;
                    EnableConnectionButtons(false);
                    EnableShipPlacement(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd tworzenia gry: {ex.Message}");
            }
        }

        private async void JoinGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string ipAddress = IpTextBox.Text;
                int port = int.Parse(PortTextBox.Text);
                bool success = await networkService.ConnectToServer(ipAddress, port);

                if (success)
                {
                    gameLogic.CurrentState = GameState.PlacingShips;
                    EnableConnectionButtons(false);
                    EnableShipPlacement(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd połączenia: {ex.Message}");
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            networkService.Disconnect();
            gameLogic.ResetGame();
            ResetUI();
        }

        private void PlayerCell_Click(object sender, RoutedEventArgs e)
        {
            if (gameLogic.CurrentState != GameState.PlacingShips) return;

            Button clickedButton = sender as Button;
            string[] position = clickedButton.Tag.ToString().Split(',');
            int row = int.Parse(position[0]);
            int col = int.Parse(position[1]);

            if (gameLogic.TryPlaceCurrentShip(row, col, isPlacingHorizontally))
            {
                UpdatePlayerBoardVisuals();
                UpdateShipsToPlace();

                if (gameLogic.AllShipsPlaced)
                {
                    ReadyButton.IsEnabled = true;
                }
            }
        }

        private void PlayerCell_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (gameLogic.CurrentState == GameState.PlacingShips)
            {
                isPlacingHorizontally = !isPlacingHorizontally;
                UpdateGameStatus($"Orientacja: {(isPlacingHorizontally ? "Pozioma" : "Pionowa")} - Rozmieszczaj statek ({gameLogic.CurrentShipSize} pól)");
            }
        }

        private void EnemyCell_Click(object sender, RoutedEventArgs e)
        {
            if (gameLogic.CurrentState != GameState.Playing || !gameLogic.IsPlayerTurn) return;

            Button clickedButton = sender as Button;
            string[] position = clickedButton.Tag.ToString().Split(',');
            int row = int.Parse(position[0]);
            int col = int.Parse(position[1]);

            if (gameLogic.ProcessPlayerShot(row, col))
            {
                networkService.SendMessage($"{GameConstants.MSG_SHOT}:{row},{col}");
            }
        }

        private void RandomPlacementButton_Click(object sender, RoutedEventArgs e)
        {
            gameLogic.PlaceShipsRandomly();
            UpdatePlayerBoardVisuals();
            UpdateShipsToPlace();
            ReadyButton.IsEnabled = true;
        }

        private void ClearBoardButton_Click(object sender, RoutedEventArgs e)
        {
            gameLogic.ClearPlayerBoard();
            UpdatePlayerBoardVisuals();
            UpdateShipsToPlace();
            ReadyButton.IsEnabled = false;
        }

        private void ReadyButton_Click(object sender, RoutedEventArgs e)
        {
            gameLogic.SetPlayerReady();
            networkService.SendMessage(GameConstants.MSG_READY);
            EnableShipPlacement(false);
        }


        private void SendChatButton_Click(object sender, RoutedEventArgs e)
        {
            SendChatMessage();
        }

        private void ChatInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendChatMessage();
            }
        }

        private void SendChatMessage()
        {
            string message = ChatInputTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                chatService.AddMessage("Ty", message);
                networkService.SendMessage($"{GameConstants.MSG_CHAT}:{message}");
                ChatInputTextBox.Clear();
            }
        }

        private void OnMessageReceived(string message)
        {
            Dispatcher.Invoke(() => ProcessNetworkMessage(message));
        }

        private void OnConnectionLost()
        {
            Dispatcher.Invoke(() => {
                chatService.AddSystemMessage("Połączenie przerwane");
                ResetUI();
            });
        }

        private void OnNetworkStatusChanged(string status)
        {
            Dispatcher.Invoke(() => UpdateGameStatus(status));
        }

        private void ProcessNetworkMessage(string message)
        {
            string[] parts = message.Split(':');
            string command = parts[0];

            switch (command)
            {
                case GameConstants.MSG_READY:
                    gameLogic.SetEnemyReady();
                    break;

                case GameConstants.MSG_SHOT:
                    ProcessEnemyShot(parts[1]);
                    break;

                case GameConstants.MSG_HIT:
                    ProcessShotResult(parts[1], true);
                    break;

                case GameConstants.MSG_MISS:
                    ProcessShotResult(parts[1], false);
                    break;

                case GameConstants.MSG_CHAT:
                    if (parts.Length > 1)
                    {
                        chatService.AddMessage("Przeciwnik", parts[1]);
                    }
                    break;

                case GameConstants.MSG_GAME_OVER:
                    ProcessGameOver(parts[1]);
                    break;
            }
        }

        private void ProcessEnemyShot(string position)
        {
            string[] coords = position.Split(',');
            int row = int.Parse(coords[0]);
            int col = int.Parse(coords[1]);

            bool isHit = gameLogic.ProcessEnemyShot(row, col);

            string response = isHit ? GameConstants.MSG_HIT : GameConstants.MSG_MISS;
            networkService.SendMessage($"{response}:{position}");

            UpdatePlayerBoardVisuals();

            if (gameLogic.PlayerBoard.AllShipsSunk())
            {
                networkService.SendMessage($"{GameConstants.MSG_GAME_OVER}:WIN");
            }
        }

        private void ProcessShotResult(string position, bool isHit)
        {
            string[] coords = position.Split(',');
            int row = int.Parse(coords[0]);
            int col = int.Parse(coords[1]);

            gameLogic.ProcessShotResult(row, col, isHit);
            UpdateEnemyBoardVisuals();
            UpdateStatistics();
        }

        private void ProcessGameOver(string result)
        {
            bool playerWon = result == "LOSE";
            EndGame(playerWon);
        }


        private void OnGameStatusChanged(string status)
        {
            Dispatcher.Invoke(() => UpdateGameStatus(status));
        }

        private void OnTurnChanged()
        {
            Dispatcher.Invoke(() => UpdateTurnStatus());
        }

        private void OnGameEnded()
        {
            Dispatcher.Invoke(() => {
                bool playerWon = gameLogic.EnemyBoard.AllShipsSunk();
                EndGame(playerWon);
            });
        }

        private void OnChatMessageAdded(string message)
        {
            Dispatcher.Invoke(() => {
                ChatTextBlock.Text += message + "\n";
                ChatScrollViewer.ScrollToEnd();
            });
        }


        private void UpdateUI()
        {
            UpdatePlayerBoardVisuals();
            UpdateEnemyBoardVisuals();
            UpdateShipsToPlace();
            UpdateStatistics();
            UpdateTurnStatus();
        }

        private void UpdatePlayerBoardVisuals()
        {
            for (int row = 0; row < GameConstants.GRID_SIZE; row++)
            {
                for (int col = 0; col < GameConstants.GRID_SIZE; col++)
                {
                    CellState state = gameLogic.PlayerBoard.GetCellState(row, col);
                    UpdateCellVisual(playerButtons[row, col], state);
                }
            }
        }

        private void UpdateEnemyBoardVisuals()
        {
            for (int row = 0; row < GameConstants.GRID_SIZE; row++)
            {
                for (int col = 0; col < GameConstants.GRID_SIZE; col++)
                {
                    CellState state = gameLogic.EnemyBoard.GetCellState(row, col);
                    
                    if (state == CellState.Ship)
                        state = CellState.Water;
                    UpdateCellVisual(enemyButtons[row, col], state);
                }
            }
        }

        private void UpdateCellVisual(Button button, CellState state)
        {
            switch (state)
            {
                case CellState.Water:
                    button.Style = (Style)FindResource("WaterCellStyle");
                    break;
                case CellState.Ship:
                    button.Style = (Style)FindResource("ShipCellStyle");
                    break;
                case CellState.Hit:
                    button.Style = (Style)FindResource("HitCellStyle");
                    break;
                case CellState.Miss:
                    button.Style = (Style)FindResource("MissCellStyle");
                    break;
            }
        }

        private void UpdateGameStatus(string status)
        {
            GameStatusTextBlock.Text = status;
        }

        private void UpdateTurnStatus()
        {
            if (gameLogic.CurrentState == GameState.Playing)
            {
                TurnStatusTextBlock.Text = gameLogic.IsPlayerTurn ? "Twoja tura" : "Tura przeciwnika";
            }
            else
            {
                TurnStatusTextBlock.Text = "";
            }
        }

        private void UpdateShipsToPlace()
        {
            ShipsToPlaceTextBlock.Text = gameLogic.ShipsToPlace.ToString();
        }

        private void UpdateStatistics()
        {
            ShotsCountTextBlock.Text = $"Strzały: {gameLogic.ShotsCount}";
            HitsCountTextBlock.Text = $"Trafienia: {gameLogic.HitsCount}";
            AccuracyTextBlock.Text = $"Celność: {gameLogic.Accuracy:F1}%";

            int enemyShipsHit = 0;
            for (int row = 0; row < GameConstants.GRID_SIZE; row++)
            {
                for (int col = 0; col < GameConstants.GRID_SIZE; col++)
                {
                    if (gameLogic.EnemyBoard.GetCellState(row, col) == CellState.Hit)
                        enemyShipsHit++;
                }
            }
            EnemyShipsLeftTextBlock.Text = (17 - enemyShipsHit).ToString();
        }

        private void EnableConnectionButtons(bool enabled)
        {
            CreateGameButton.IsEnabled = enabled;
            JoinGameButton.IsEnabled = enabled;
            DisconnectButton.IsEnabled = !enabled;
        }

        private void EnableShipPlacement(bool enabled)
        {
            RandomPlacementButton.IsEnabled = enabled;
            ClearBoardButton.IsEnabled = enabled;
            ReadyButton.IsEnabled = enabled && gameLogic.AllShipsPlaced;
        }

        private void ResetUI()
        {
            EnableConnectionButtons(true);
            EnableShipPlacement(false);
            ReadyButton.IsEnabled = false;

            UpdateGameStatus("Rozłączono - gotowy do nowej gry");
            UpdateUI();
        }

        private void EndGame(bool playerWon)
        {
            string message = playerWon ? "Gratulacje! Wygrałeś!" : "Przegrałeś! Spróbuj ponownie.";
            chatService.AddSystemMessage($"Gra zakończona: {message}");

            MessageBox.Show(message, "Koniec gry", MessageBoxButton.OK,
                playerWon ? MessageBoxImage.Information : MessageBoxImage.Exclamation);
        }

        protected override void OnClosed(EventArgs e)
        {
            networkService?.Disconnect();
            base.OnClosed(e);
        }
    }
}

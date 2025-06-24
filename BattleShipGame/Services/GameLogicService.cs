using System.Windows;
using BattleShip.Models;
using BattleShip.Utilities;

namespace BattleShip.Services
{
    public class GameLogicService
    {
        private readonly int[] SHIP_SIZES = GameConstants.SHIP_SIZES;
        private const int TOTAL_SHIP_CELLS = 17;

        public GameBoard PlayerBoard { get; private set; } = new GameBoard();
        public GameBoard EnemyBoard { get; private set; } = new GameBoard();

        public GameState CurrentState { get; set; } = GameState.WaitingForConnection;
        public bool IsPlayerTurn { get; set; }
        public bool IsPlayerReady { get; set; }
        public bool IsEnemyReady { get; set; }

        public int CurrentShipIndex { get; set; } = 0;

        public int ShotsCount { get; set; } = 0;
        public int HitsCount { get; set; } = 0;

        private List<Ship> reconstructedEnemyShips = new List<Ship>();
        private HashSet<Ship> sunkEnemyShips = new HashSet<Ship>();

        public event Action<string> StatusChanged;
        public event Action TurnChanged;
        public event Action GameEnded;

        public double Accuracy => ShotsCount > 0 ? (double)HitsCount / ShotsCount * 100 : 0;
        public int ShipsToPlace => SHIP_SIZES.Length - CurrentShipIndex;
        public int CurrentShipSize => CurrentShipIndex < SHIP_SIZES.Length ? SHIP_SIZES[CurrentShipIndex] : 0;
        public bool AllShipsPlaced => CurrentShipIndex >= SHIP_SIZES.Length;


        public bool TryPlaceCurrentShip(int row, int col, bool horizontal)
        {
            if (CurrentShipIndex >= SHIP_SIZES.Length) return false;

            if (PlayerBoard.TryPlaceShip(row, col, CurrentShipSize, horizontal))
            {
                CurrentShipIndex++;

                if (AllShipsPlaced)
                {
                    StatusChanged?.Invoke("Wszystkie statki rozmieszczone! Kliknij 'Gotowy!'");
                }
                else
                {
                    StatusChanged?.Invoke($"Rozmieść statek ({CurrentShipSize} pól) - PPM aby obrócić");
                }

                return true;
            }
            return false;
        }

        public void PlaceShipsRandomly()
        {
            ClearPlayerBoard();
            Random random = new Random();

            foreach (int shipSize in SHIP_SIZES)
            {
                bool placed = false;
                int attempts = 0;

                while (!placed && attempts < 100)
                {
                    int row = random.Next(GameConstants.GRID_SIZE);
                    int col = random.Next(GameConstants.GRID_SIZE);
                    bool horizontal = random.Next(2) == 0;

                    if (PlayerBoard.TryPlaceShip(row, col, shipSize, horizontal))
                    {
                        placed = true;
                        CurrentShipIndex++;
                    }
                    attempts++;
                }
            }

            StatusChanged?.Invoke("Statki rozmieszczone losowo! Kliknij 'Gotowy!'");
        }

        public void ClearPlayerBoard()
        {
            PlayerBoard.Clear();
            CurrentShipIndex = 0;
            StatusChanged?.Invoke("Plansza wyczyszczona - rozmieść statki");
        }

        public void SetPlayerReady()
        {
            IsPlayerReady = true;

            if (IsEnemyReady)
            {
                StartGame();
            }
            else
            {
                CurrentState = GameState.WaitingForReady;
                StatusChanged?.Invoke("Oczekiwanie na gotowość przeciwnika...");
            }
        }

        public void SetEnemyReady()
        {
            IsEnemyReady = true;

            if (IsPlayerReady)
            {
                StartGame();
            }
        }

        private void StartGame()
        {
            CurrentState = GameState.Playing;
            IsPlayerTurn = true; // Można zmienić logikę kto zaczyna
            StatusChanged?.Invoke("Gra rozpoczęta!");
            TurnChanged?.Invoke();
        }


        public bool ProcessPlayerShot(int row, int col)
        {
            if (CurrentState != GameState.Playing || !IsPlayerTurn) return false;
            if (!EnemyBoard.CanShoot(row, col)) return false;

            ShotsCount++;
            return true;
        }

        public void ProcessShotResult(int row, int col, bool isHit)
        {
            if (isHit)
            {
                EnemyBoard.SetCellState(row, col, CellState.Hit);
                HitsCount++;

                Ship hitShip = GetShipAtPosition(row, col);
                if (hitShip != null && IsShipSunk(hitShip))
                {
                    if (!sunkEnemyShips.Contains(hitShip))
                    {
                        sunkEnemyShips.Add(hitShip);
                        StatusChanged?.Invoke("Trafienie! Strzelaj ponownie.");
                    }
                    else
                    {
                        StatusChanged?.Invoke("Trafienie! Strzelaj ponownie.");
                    }
                }
                else
                {
                    StatusChanged?.Invoke("Statek zatopiony! Strzelaj ponownie.");
                }

                if (AreAllEnemyShipsSunk())
                {
                    EndGame(true);
                    return;
                }
            }
            else
            {
                EnemyBoard.SetCellState(row, col, CellState.Miss);
                IsPlayerTurn = false;
                StatusChanged?.Invoke("Pudło! Tura przeciwnika.");
            }

            TurnChanged?.Invoke();
        }

        public bool ProcessEnemyShot(int row, int col)
        {
            bool isHit = PlayerBoard.ProcessShot(row, col);

            if (isHit)
            {
                Ship hitShip = PlayerBoard.GetShipAt(row, col);
                if (hitShip != null && hitShip.IsSunk(PlayerBoard))
                {
                    StatusChanged?.Invoke("Przeciwnik zatopił Twój statek! Jego kolejna tura.");
                }
                else
                {
                    StatusChanged?.Invoke("Przeciwnik trafił! Jego kolejna tura.");
                }

                if (PlayerBoard.AllShipsSunk())
                {
                    EndGame(false);
                    return isHit;
                }
            }
            else
            {
                IsPlayerTurn = true;
                StatusChanged?.Invoke("Przeciwnik spudłował! Twoja tura.");
                TurnChanged?.Invoke();
            }

            return isHit;
        }

        private Ship GetShipAtPosition(int row, int col)
        {
            if (EnemyBoard.GetCellState(row, col) != CellState.Hit)
                return null;

            foreach (var ship in reconstructedEnemyShips)
            {
                foreach (var cell in ship.Cells)
                {
                    if ((int)cell.X == row && (int)cell.Y == col)
                        return ship;
                }
            }

            Ship newShip = ReconstructShipFromHit(row, col);
            if (newShip != null)
            {
                reconstructedEnemyShips.Add(newShip);
                return newShip;
            }

            return null;
        }

        private Ship ReconstructShipFromHit(int hitRow, int hitCol)
        {
            List<Point> shipCells = new List<Point>();
            shipCells.Add(new Point(hitRow, hitCol));

            List<Point> horizontalCells = GetConnectedHits(hitRow, hitCol, true);
            List<Point> verticalCells = GetConnectedHits(hitRow, hitCol, false);

            List<Point> finalCells = horizontalCells.Count > verticalCells.Count ?
                horizontalCells : verticalCells;

            if (finalCells.Count == 1)
            {
                bool hasAdjacentHits = HasAdjacentHits(hitRow, hitCol);
                if (!hasAdjacentHits)
                {
                    return new Ship(1) { Cells = finalCells };
                }
                return null;
            }

            if (IsShipComplete(finalCells))
            {
                bool isHorizontal = finalCells.Count > 1 &&
                    finalCells[0].X == finalCells[1].X;

                return new Ship(finalCells.Count)
                {
                    Cells = finalCells,
                    IsHorizontal = isHorizontal
                };
            }

            return null;
        }

        private List<Point> GetConnectedHits(int startRow, int startCol, bool horizontal)
        {
            List<Point> cells = new List<Point>();
            cells.Add(new Point(startRow, startCol));

            if (horizontal)
            {
                for (int col = startCol - 1; col >= 0; col--)
                {
                    if (EnemyBoard.GetCellState(startRow, col) == CellState.Hit)
                        cells.Insert(0, new Point(startRow, col));
                    else
                        break;
                }
                for (int col = startCol + 1; col < GameConstants.GRID_SIZE; col++)
                {
                    if (EnemyBoard.GetCellState(startRow, col) == CellState.Hit)
                        cells.Add(new Point(startRow, col));
                    else
                        break;
                }
            }
            else
            {
                for (int row = startRow - 1; row >= 0; row--)
                {
                    if (EnemyBoard.GetCellState(row, startCol) == CellState.Hit)
                        cells.Insert(0, new Point(row, startCol));
                    else
                        break;
                }
                for (int row = startRow + 1; row < GameConstants.GRID_SIZE; row++)
                {
                    if (EnemyBoard.GetCellState(row, startCol) == CellState.Hit)
                        cells.Add(new Point(row, startCol));
                    else
                        break;
                }
            }

            return cells;
        }

        private bool HasAdjacentHits(int row, int col)
        {
            int[] directions = { -1, 0, 1 };

            foreach (int dr in directions)
            {
                foreach (int dc in directions)
                {
                    if (dr == 0 && dc == 0) continue;

                    int newRow = row + dr;
                    int newCol = col + dc;

                    if (newRow >= 0 && newRow < GameConstants.GRID_SIZE && newCol >= 0 && newCol < GameConstants.GRID_SIZE)
                    {
                        if (EnemyBoard.GetCellState(newRow, newCol) == CellState.Hit)
                            return true;
                    }
                }
            }

            return false;
        }

        private bool IsShipComplete(List<Point> shipCells)
        {
            foreach (var cell in shipCells)
            {
                int row = (int)cell.X;
                int col = (int)cell.Y;

                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;

                        int newRow = row + dr;
                        int newCol = col + dc;

                        if (newRow < 0 || newRow >= GameConstants.GRID_SIZE || newCol < 0 || newCol >= GameConstants.GRID_SIZE)
                            continue;

                        if (shipCells.Any(c => (int)c.X == newRow && (int)c.Y == newCol))
                            continue;

                        CellState state = EnemyBoard.GetCellState(newRow, newCol);

                        if (state != CellState.Miss && state != CellState.Water)
                        {
                            if (state == CellState.Hit)
                                return false;
                        }
                    }
                }
            }

            return true;
        }

        private bool IsShipSunk(Ship ship)
        {
            return ship.Cells.All(cell =>
                EnemyBoard.GetCellState((int)cell.X, (int)cell.Y) == CellState.Hit);
        }

        private bool AreAllEnemyShipsSunk()
        {
            int hitCells = 0;
            for (int row = 0; row < GameConstants.GRID_SIZE; row++)
            {
                for (int col = 0; col < GameConstants.GRID_SIZE; col++)
                {
                    if (EnemyBoard.GetCellState(row, col) == CellState.Hit)
                        hitCells++;
                }
            }
            return hitCells >= TOTAL_SHIP_CELLS;
        }

        public void ClearReconstructedShips()
        {
            reconstructedEnemyShips.Clear();
            sunkEnemyShips.Clear();
        }

        private void EndGame(bool playerWon)
        {
            CurrentState = GameState.GameOver;
            string message = playerWon ? "Gratulacje! Wygrałeś!" : "Przegrałeś! Spróbuj ponownie.";
            StatusChanged?.Invoke(message);
            GameEnded?.Invoke();
        }

        public void ResetGame()
        {
            PlayerBoard.Clear();
            EnemyBoard.Clear();
            CurrentShipIndex = 0;
            ShotsCount = 0;
            HitsCount = 0;
            IsPlayerReady = false;
            IsEnemyReady = false;
            IsPlayerTurn = false;
            CurrentState = GameState.WaitingForConnection;
            ClearReconstructedShips();
            StatusChanged?.Invoke("Gra zresetowana");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace BattleShip.Models
{
    public class GameBoard
    {
        private const int GRID_SIZE = 10;
        private CellState[,] board = new CellState[GRID_SIZE, GRID_SIZE];
        private List<Ship> ships = new List<Ship>();

        public CellState GetCellState(int row, int col)
        {
            if (row < 0 || row >= GRID_SIZE || col < 0 || col >= GRID_SIZE)
                throw new ArgumentOutOfRangeException("Współrzędne poza planszą");

            return board[row, col];
        }

        public void SetCellState(int row, int col, CellState state)
        {
            if (row < 0 || row >= GRID_SIZE || col < 0 || col >= GRID_SIZE)
                throw new ArgumentOutOfRangeException("Współrzędne poza planszą");

            board[row, col] = state;
        }

        public List<Ship> GetShips() => new List<Ship>(ships);

        public int GridSize => GRID_SIZE;

        public bool TryPlaceShip(int startRow, int startCol, int shipSize, bool horizontal)
        {
            if (!CanPlaceShip(startRow, startCol, shipSize, horizontal))
                return false;

            var ship = new Ship(shipSize, horizontal);

            for (int i = 0; i < shipSize; i++)
            {
                int row = horizontal ? startRow : startRow + i;
                int col = horizontal ? startCol + i : startCol;

                board[row, col] = CellState.Ship;
                ship.Cells.Add(new Point(row, col));
            }

            ships.Add(ship);
            return true;
        }

        private bool CanPlaceShip(int startRow, int startCol, int shipSize, bool horizontal)
        {
            if (horizontal && startCol + shipSize > GRID_SIZE) return false;
            if (!horizontal && startRow + shipSize > GRID_SIZE) return false;

            for (int i = 0; i < shipSize; i++)
            {
                int checkRow = horizontal ? startRow : startRow + i;
                int checkCol = horizontal ? startCol + i : startCol;

                if (board[checkRow, checkCol] != CellState.Water) return false;
                if (HasAdjacentShip(checkRow, checkCol)) return false;
            }

            return true;
        }

        private bool HasAdjacentShip(int row, int col)
        {
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    int newRow = row + dr;
                    int newCol = col + dc;

                    if (IsValidPosition(newRow, newCol) && board[newRow, newCol] == CellState.Ship)
                        return true;
                }
            }
            return false;
        }

        private bool IsValidPosition(int row, int col)
        {
            return row >= 0 && row < GRID_SIZE && col >= 0 && col < GRID_SIZE;
        }

        public bool ProcessShot(int row, int col)
        {
            if (!IsValidPosition(row, col))
                return false;

            if (board[row, col] == CellState.Ship)
            {
                board[row, col] = CellState.Hit;
                return true;
            }
            else if (board[row, col] == CellState.Water)
            {
                board[row, col] = CellState.Miss;
                return false;
            }

            return false;
        }

        public bool AllShipsSunk()
        {
            return ships.All(ship => ship.IsSunk(this));
        }

        public int GetSunkShipsCount()
        {
            return ships.Count(ship => ship.IsSunk(this));
        }

        public bool IsShipSunk(Ship ship)
        {
            return ship.IsSunk(this);
        }

        public Ship GetShipAt(int row, int col)
        {
            return ships.FirstOrDefault(ship =>
                ship.Cells.Any(cell => cell.X == row && cell.Y == col));
        }

        public int GetRemainingShipCells()
        {
            int remaining = 0;
            for (int row = 0; row < GRID_SIZE; row++)
            {
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    if (board[row, col] == CellState.Ship)
                        remaining++;
                }
            }
            return remaining;
        }

        public void Clear()
        {
            for (int row = 0; row < GRID_SIZE; row++)
            {
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    board[row, col] = CellState.Water;
                }
            }
            ships.Clear();
        }

        public bool HasShipAt(int row, int col)
        {
            return IsValidPosition(row, col) && board[row, col] == CellState.Ship;
        }

        public bool CanShoot(int row, int col)
        {
            return IsValidPosition(row, col) &&
                   (board[row, col] == CellState.Water || board[row, col] == CellState.Ship);
        }
    }
}
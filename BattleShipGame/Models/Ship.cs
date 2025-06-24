using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace BattleShip.Models
{
    public class Ship
    {
        public List<Point> Cells { get; set; } = new List<Point>();
        public int Size { get; set; }
        public bool IsHorizontal { get; set; }

        public bool IsSunk(GameBoard board)
        {
            return Cells.Count > 0 && Cells.All(cell =>
                board.GetCellState((int)cell.X, (int)cell.Y) == CellState.Hit);
        }

            public Ship(int size)
        {
            Size = size;
        }

        public Ship(int size, bool isHorizontal) : this(size)
        {
            IsHorizontal = isHorizontal;
        }
    }
}
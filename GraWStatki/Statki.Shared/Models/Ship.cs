using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Statki.Shared.Models
{
    public enum Direction
    {
        Horizontal,
        Vertical
    }

    public class Ship
    {
        public int Length { get; set; }
        public List<Position> Positions { get; set; } = new();

        //sprawdzanie czy wszystkie elementy statku zostały trafione
        public bool IsSunk => Positions.All(p => p.IsHit);

        public Ship(int startX, int startY, int length, Direction direction)
        {
            Length = length;

            for (int i = 0; i < length; i++)
            {
                //tworzenie kolejnych segmentów statku
                Positions.Add(direction switch
                {
                    Direction.Horizontal => new Position { X = startX + i, Y = startY },
                    Direction.Vertical => new Position { X = startX, Y = startY + i },
                    _ => throw new ArgumentOutOfRangeException(nameof(direction))
                });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Statki.Shared.Models
{
    public class GameBoard
    {
        public const int Size = Game.GameConfig.BoardSize;
        public List<Ship> Ships { get; set; } = new();
        //macierz sprawdzajaca gdzie otrzymano juz strzały
        public bool[,] ShotsReceived { get; set; } = new bool[Size, Size];

        public bool PlaceShip(Ship ship)
        {
            foreach (var pos in ship.Positions)
            {
                //sprawdzanie czy każda cześć statku jest w granicach planszy
                if (!IsInBounds(pos.X, pos.Y) || IsOccupied(pos.X, pos.Y))
                {
                    return false;
                }
            }
            Ships.Add(ship);
            return true;
        }

        public (bool hit, bool sunk, Ship? ship) ReceiveShot(int x, int y)
        {
            ShotsReceived[x, y] = true;

            //sprawdzanie czy jakiś segment statku został trafiony
            foreach (var ship in Ships)
            {
                var target = ship.Positions.FirstOrDefault(p => p.X == x && p.Y == y);
                if (target != null)
                {
                    target.IsHit = true;
                    return (true, ship.IsSunk, ship);
                }
            }

            return (false, false, null);
        }

        //sprawdzanie czy pole jest zajęte przez inny statek
        public bool IsOccupied(int x, int y)
        {
            return Ships.Any(ship => ship.Positions.Any(pos => pos.X == x && pos.Y == y));
        }

        //czy współrzedne na planszy
        public static bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size;
        }

        public bool AllShipsSunk()
        {
            return Ships.All(s => s.IsSunk);
        }
    }
}

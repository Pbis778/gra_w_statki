using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Statki.Shared.Models
{
    public class Position
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsHit { get; set; } = false;
    }
}

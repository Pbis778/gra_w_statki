using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Statki.Shared.Networking
{
    public enum MessageType
    {
        Join,
        Move,
        Hit,
        Miss,
        Sunk,
        GameOver,
        Restart
    }

    public class NetworkMessage
    {
        public MessageType Type { get; set; }
        public string Payload { get; set; } = string.Empty;
    }
}

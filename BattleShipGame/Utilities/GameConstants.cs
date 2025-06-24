namespace BattleShip.Utilities
{
    public static class GameConstants
    {
        public const int GRID_SIZE = 10;
        public static readonly int[] SHIP_SIZES = { 5, 4, 3, 3, 2 };
        public const int DEFAULT_PORT = 8888;
        public const string DEFAULT_IP = "127.0.0.1";

        public const string MSG_READY = "READY";
        public const string MSG_SHOT = "SHOT";
        public const string MSG_HIT = "HIT";
        public const string MSG_MISS = "MISS";
        public const string MSG_CHAT = "CHAT";
        public const string MSG_GAME_OVER = "GAME_OVER";

        public static readonly string[] SHIP_NAMES =
        {
            "Lotniskowiec",
            "Krążownik",
            "Niszczyciel",
            "Łódź podwodna",
            "Patrol"
        };
    }
}

namespace BattleShip.Models
{
    public enum GameState
    {
        WaitingForConnection,
        PlacingShips,
        WaitingForReady,
        Playing,
        GameOver
    }

    public enum CellState
    {
        Water,
        Ship,
        Hit,
        Miss
    }
}
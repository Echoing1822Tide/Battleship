namespace MauiBattleship.GameCore.Enums
{
    public sealed class AttackResult
    {
        public int Row { get; init; }
        public int Col { get; init; }

        public bool IsHit { get; init; }
        public bool IsMiss => !IsHit;

        public bool IsSunk { get; init; }
        public string? SunkShipName { get; init; }

        public bool GameOver { get; init; }
        public string Message { get; init; } = "";
    }
}

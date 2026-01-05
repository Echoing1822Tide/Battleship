namespace MauiBattleship.Models;

public sealed class AttackResult
{
    public int Row { get; set; }
    public int Col { get; set; }

    public bool IsHit { get; set; }
    public bool IsSunk { get; set; }

    public string? ShipName { get; set; }
    public string Message { get; set; } = "";
}

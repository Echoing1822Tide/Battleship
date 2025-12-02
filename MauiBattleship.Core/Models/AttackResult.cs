namespace MauiBattleship.Models;

/// <summary>
/// Represents the result of an attack on the game board.
/// </summary>
public class AttackResult
{
    /// <summary>
    /// The row that was attacked.
    /// </summary>
    public int Row { get; }

    /// <summary>
    /// The column that was attacked.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Indicates whether the attack was a hit.
    /// </summary>
    public bool IsHit { get; }

    /// <summary>
    /// Indicates whether a ship was sunk by this attack.
    /// </summary>
    public bool IsSunk { get; }

    /// <summary>
    /// The name of the ship that was sunk, if any.
    /// </summary>
    public string? SunkShipName { get; }

    /// <summary>
    /// Indicates whether the cell was already attacked.
    /// </summary>
    public bool AlreadyAttacked { get; }

    /// <summary>
    /// Creates a new attack result.
    /// </summary>
    public AttackResult(int row, int column, bool isHit, bool isSunk = false, string? sunkShipName = null, bool alreadyAttacked = false)
    {
        Row = row;
        Column = column;
        IsHit = isHit;
        IsSunk = isSunk;
        SunkShipName = sunkShipName;
        AlreadyAttacked = alreadyAttacked;
    }
}

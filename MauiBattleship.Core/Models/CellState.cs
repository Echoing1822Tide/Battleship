namespace MauiBattleship.Models;

/// <summary>
/// Represents the state of a cell on the game board.
/// </summary>
public enum CellState
{
    /// <summary>Empty cell that hasn't been attacked.</summary>
    Empty,
    /// <summary>Cell containing a ship that hasn't been hit.</summary>
    Ship,
    /// <summary>Cell that was attacked but contained no ship.</summary>
    Miss,
    /// <summary>Cell containing a ship that has been hit.</summary>
    Hit
}

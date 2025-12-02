namespace MauiBattleship.Models;

/// <summary>
/// Represents the current phase of the game.
/// </summary>
public enum GamePhase
{
    /// <summary>Game hasn't started yet.</summary>
    NotStarted,
    /// <summary>Player is placing ships.</summary>
    PlacingShips,
    /// <summary>Player's turn to attack.</summary>
    PlayerTurn,
    /// <summary>Computer's turn to attack.</summary>
    ComputerTurn,
    /// <summary>Game is over.</summary>
    GameOver
}

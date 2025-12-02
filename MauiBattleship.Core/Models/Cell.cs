namespace MauiBattleship.Models;

/// <summary>
/// Represents a single cell on the game board.
/// </summary>
public class Cell
{
    /// <summary>
    /// The row position of this cell (0-9).
    /// </summary>
    public int Row { get; }

    /// <summary>
    /// The column position of this cell (0-9).
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// The current state of this cell.
    /// </summary>
    public CellState State { get; set; }

    /// <summary>
    /// Reference to the ship occupying this cell, if any.
    /// </summary>
    public Ship? Ship { get; set; }

    /// <summary>
    /// Creates a new cell at the specified position.
    /// </summary>
    /// <param name="row">The row position (0-9).</param>
    /// <param name="column">The column position (0-9).</param>
    public Cell(int row, int column)
    {
        Row = row;
        Column = column;
        State = CellState.Empty;
    }

    /// <summary>
    /// Indicates whether this cell has been attacked.
    /// </summary>
    public bool IsAttacked => State == CellState.Hit || State == CellState.Miss;

    /// <summary>
    /// Indicates whether this cell contains a ship.
    /// </summary>
    public bool HasShip => Ship != null;
}

namespace MauiBattleship.Models;

public sealed class Cell
{
    public int Row { get; }
    public int Col { get; }

    public CellState State { get; set; } = CellState.Water;

    // If this cell contains a ship, this is the ship's name (used by UI for "You hit their ___")
    public string? ShipName { get; set; }

    public Cell(int row, int col)
    {
        Row = row;
        Col = col;
    }
}

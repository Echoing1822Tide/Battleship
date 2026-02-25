namespace Battleship.GameCore;

public class Cell
{
    public int Row { get; }
    public int Col { get; }

    public CellState State { get; set; } = CellState.Empty;

    public Ship? Ship { get; set; }

    public Cell(int row, int col)
    {
        Row = row;
        Col = col;
    }

    public bool HasBeenAttacked => State == CellState.Miss || State == CellState.Hit;
}

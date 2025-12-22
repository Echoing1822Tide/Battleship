namespace MauiBattleship.Models;

public class GameBoard
{
    public int Size { get; }

    private readonly Cell[,] _cells;
    private List<Ship> _fleet = new();

    public int ShipsSunk => _fleet.Count(s => s.IsSunk);
    public int TotalShips => _fleet.Count;
    public bool AllShipsSunk => _fleet.Count > 0 && _fleet.All(s => s.IsSunk);

    public GameBoard(int size)
    {
        Size = size;
        _cells = new Cell[size, size];

        for (var r = 0; r < size; r++)
        for (var c = 0; c < size; c++)
            _cells[r, c] = new Cell(r, c);
    }

    public void SetFleet(List<Ship> fleet)
    {
        _fleet = fleet;
    }

    public Cell GetCell(int row, int col)
    {
        if (!InBounds(row, col))
            throw new ArgumentOutOfRangeException($"Cell out of bounds ({row},{col}).");

        return _cells[row, col];
    }

    public bool TryPlaceShip(Ship ship, int startRow, int startCol, bool vertical)
    {
        if (ship.Positions.Count > 0)
            return false; // already placed

        // compute cells needed
        var coords = new List<(int Row, int Col)>();
        for (var i = 0; i < ship.Size; i++)
        {
            var r = vertical ? startRow + i : startRow;
            var c = vertical ? startCol : startCol + i;

            if (!InBounds(r, c))
                return false;

            if (_cells[r, c].Ship is not null)
                return false;

            coords.Add((r, c));
        }

        // place
        foreach (var (r, c) in coords)
        {
            _cells[r, c].Ship = ship;
            ship.Positions.Add(new ShipPosition(r, c));
        }

        return true;
    }

    public string Attack(int row, int col)
    {
        if (!InBounds(row, col))
            return "Out of bounds.";

        var cell = _cells[row, col];

        if (cell.State != CellState.Empty)
            return "Already attacked.";

        if (cell.HasShip && cell.Ship is not null)
        {
            cell.State = CellState.Hit;
            cell.Ship.RegisterHit(row, col);

            if (cell.Ship.IsSunk)
                return $"Hit and sunk {cell.Ship.Name}!";

            return "Hit!";
        }

        cell.State = CellState.Miss;
        return "Miss.";
    }

    public bool IsAlreadyAttacked(int row, int col)
    {
        if (!InBounds(row, col))
            return true;

        var cell = GetCell(row, col);
        return cell.State == CellState.Hit || cell.State == CellState.Miss;
    }

    private bool InBounds(int row, int col)
        => row >= 0 && row < Size && col >= 0 && col < Size;
}

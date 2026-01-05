namespace MauiBattleship.Models;

public sealed class GameBoard
{
    public int Size { get; }
    private readonly Cell[,] _grid;

    private List<Ship> _fleet = new();

    public bool AllShipsSunk => _fleet.Count > 0 && _fleet.All(s => s.IsSunk);

    public GameBoard(int size = 10)
    {
        Size = size;
        _grid = new Cell[size, size];

        for (int r = 0; r < size; r++)
        for (int c = 0; c < size; c++)
            _grid[r, c] = new Cell(r, c);
    }

    public void SetFleet(List<Ship> ships)
    {
        _fleet = ships;
    }

    public Cell GetCell(int row, int col) => _grid[row, col];

    public bool IsInside(int row, int col) => row >= 0 && row < Size && col >= 0 && col < Size;

    public bool IsAlreadyAttacked(int row, int col)
    {
        var s = _grid[row, col].State;
        return s == CellState.Hit || s == CellState.Miss;
    }

    public bool TryPlaceShip(Ship ship, int row, int col, bool isVertical)
    {
        if (ship.IsPlaced) return false;

        int endRow = isVertical ? row + ship.Size - 1 : row;
        int endCol = isVertical ? col : col + ship.Size - 1;

        if (!IsInside(row, col) || !IsInside(endRow, endCol))
            return false;

        // Check overlap
        for (int i = 0; i < ship.Size; i++)
        {
            int r = isVertical ? row + i : row;
            int c = isVertical ? col : col + i;

            if (_grid[r, c].State == CellState.Ship)
                return false;
        }

        // Place
        ship.ClearPlacement();

        for (int i = 0; i < ship.Size; i++)
        {
            int r = isVertical ? row + i : row;
            int c = isVertical ? col : col + i;

            ship.Positions.Add((r, c));

            _grid[r, c].State = CellState.Ship;
            _grid[r, c].ShipName = ship.Name;
        }

        return true;
    }

    public AttackResult Attack(int row, int col)
    {
        var result = new AttackResult { Row = row, Col = col };

        if (!IsInside(row, col))
        {
            result.IsHit = false;
            result.Message = "Out of bounds.";
            return result;
        }

        if (IsAlreadyAttacked(row, col))
        {
            result.IsHit = false;
            result.Message = "Already attacked.";
            return result;
        }

        var cell = _grid[row, col];

        if (cell.State == CellState.Ship)
        {
            cell.State = CellState.Hit;

            var ship = _fleet.FirstOrDefault(s => s.Occupies(row, col));
            if (ship != null)
            {
                ship.RegisterHit(row, col);
                result.ShipName = ship.Name;
                result.IsSunk = ship.IsSunk;
            }

            result.IsHit = true;
            result.Message = result.IsSunk && !string.IsNullOrWhiteSpace(result.ShipName)
                ? $"Hit! You sunk the {result.ShipName}!"
                : "Hit!";
        }
        else
        {
            cell.State = CellState.Miss;
            result.IsHit = false;
            result.Message = "Miss.";
        }

        return result;
    }
}

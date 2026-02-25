using System;
using System.Collections.Generic;
using System.Linq;

namespace Battleship.GameCore;

public sealed class GameBoard
{
    public int Size { get; }
    public Cell[,] Cells { get; }

    private List<Ship> _fleet = new();

    public IReadOnlyList<Ship> Fleet => _fleet;

    public int ShipsSunk => _fleet.Count(s => s.IsSunk);
    public int TotalShips => _fleet.Count;
    public bool AllShipsSunk => _fleet.Count > 0 && _fleet.All(s => s.IsSunk);

    public GameBoard(int size)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Board size must be greater than zero.");

        Size = size;
        Cells = new Cell[size, size];

        for (var r = 0; r < size; r++)
        for (var c = 0; c < size; c++)
            Cells[r, c] = new Cell(r, c);
    }

    public void SetFleet(List<Ship> fleet)
    {
        ArgumentNullException.ThrowIfNull(fleet);
        _fleet = fleet;
    }

    public bool InBounds(int row, int col)
        => row >= 0 && row < Size && col >= 0 && col < Size;

    public Cell GetCell(int row, int col)
    {
        if (!InBounds(row, col))
            throw new ArgumentOutOfRangeException($"Cell out of bounds ({row},{col}).");

        return Cells[row, col];
    }

    public bool IsAlreadyAttacked(int row, int col)
    {
        if (!InBounds(row, col)) return true;
        return Cells[row, col].HasBeenAttacked;
    }

    public bool TryPlaceShip(Ship ship, int startRow, int startCol, ShipOrientation orientation)
    {
        ArgumentNullException.ThrowIfNull(ship);

        if (ship.Positions.Count > 0) return false;

        bool vertical = orientation == ShipOrientation.Vertical;

        var coords = new List<(int Row, int Col)>();
        for (int i = 0; i < ship.Size; i++)
        {
            int r = vertical ? startRow + i : startRow;
            int c = vertical ? startCol : startCol + i;

            if (!InBounds(r, c)) return false;
            if (Cells[r, c].Ship is not null) return false;

            coords.Add((r, c));
        }

        foreach (var (r, c) in coords)
        {
            Cells[r, c].Ship = ship;
            Cells[r, c].State = CellState.Ship;

            if (!ship.TryAddPosition(r, c))
                return false;
        }

        ship.IsPlaced = true;
        return true;
    }

    public ShotInfo Attack(int row, int col)
    {
        if (!InBounds(row, col))
            return new ShotInfo(row, col, AttackResult.Invalid, false, null, "Out of bounds.");

        var cell = Cells[row, col];

        if (cell.HasBeenAttacked)
            return new ShotInfo(row, col, AttackResult.AlreadyTried, false, null, "Already attacked.");

        if (cell.Ship is not null)
        {
            cell.State = CellState.Hit;
            cell.Ship.RegisterHit(row, col);

            if (cell.Ship.IsSunk)
                return new ShotInfo(row, col, AttackResult.Sunk, true, cell.Ship.Name, $"Hit and sunk {cell.Ship.Name}!");

            return new ShotInfo(row, col, AttackResult.Hit, true, cell.Ship.Name, "Hit!");
        }

        cell.State = CellState.Miss;
        return new ShotInfo(row, col, AttackResult.Miss, false, null, "Miss!");
    }
}

using MauiBattleship.GameCore.Enums;
using MauiBattleship.GameCore.Ships;

namespace MauiBattleship.GameCore.Models
{
    public sealed class Cell
    {
        public int Row { get; }
        public int Col { get; }

        public CellState State { get; set; } = CellState.Empty;

        // If a ship occupies this cell, reference it (helps UI + sunk logic)
        public ShipBase? Ship { get; set; }

        public Cell(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public bool HasBeenAttacked => State == CellState.Miss || State == CellState.Hit;
    }
}

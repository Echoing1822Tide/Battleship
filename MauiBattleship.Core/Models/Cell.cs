using System;

namespace MauiBattleship.Models
{
    public sealed class Cell
    {
        public int Row { get; }
        public int Col { get; }

        public CellState State { get; set; } = CellState.Empty;

        /// <summary>
        /// Ship occupying this cell (null if none).
        /// </summary>
        public Ship? Ship { get; set; }

        /// <summary>
        /// Convenience flag used by the UI and placement logic.
        /// </summary>
        public bool HasShip => Ship is not null;

        public Cell(int row, int col)
        {
            Row = row;
            Col = col;
        }
    }
}

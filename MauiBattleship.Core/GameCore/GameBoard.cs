using System;
using System.Collections.Generic;
using System.Linq;

namespace MauiBattleship.Models
{
    public sealed class GameBoard
    {
        public const int BoardSize = 10;

        public Cell[,] Cells { get; }
        public List<Ship> Ships { get; } = new();

        public int SunkShipCount { get; private set; }
        public int TotalShipCount => Ships.Count;
        public bool AllShipsSunk => TotalShipCount > 0 && SunkShipCount >= TotalShipCount;

        // ----------------------------------------------------
        // Backwards-compatible aliases for older UI code
        // ----------------------------------------------------

        /// <summary>
        /// Alias for UI code that expects a ShipsSunk property.
        /// </summary>
        public int ShipsSunk => SunkShipCount;

        /// <summary>
        /// Simple accessor so UI code can ask the board for a cell.
        /// </summary>
        public Cell GetCell(int row, int col) => Cells[row, col];

        public GameBoard()
        {
            Cells = new Cell[BoardSize, BoardSize];

            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    Cells[r, c] = new Cell(r, c);
                }
            }
        }



        public bool InBounds(int row, int col) =>
            row >= 0 && row < BoardSize && col >= 0 && col < BoardSize;

        public bool CanPlaceShip(Ship ship, int startRow, int startCol, ShipOrientation orientation)
        {
            if (ship is null) throw new ArgumentNullException(nameof(ship));

            for (int i = 0; i < ship.Size; i++)
            {
                int r = startRow + (orientation == ShipOrientation.Vertical ? i : 0);
                int c = startCol + (orientation == ShipOrientation.Horizontal ? i : 0);

                if (!InBounds(r, c))
                    return false;

                if (Cells[r, c].HasShip)
                    return false;
            }

            return true;
        }

        public bool PlaceShip(Ship ship, int startRow, int startCol, ShipOrientation orientation)
        {
            if (!CanPlaceShip(ship, startRow, startCol, orientation))
                return false;

            var positions = new List<(int Row, int Col)>();

            for (int i = 0; i < ship.Size; i++)
            {
                int r = startRow + (orientation == ShipOrientation.Vertical ? i : 0);
                int c = startCol + (orientation == ShipOrientation.Horizontal ? i : 0);

                var cell = Cells[r, c];
                cell.Ship = ship;
                positions.Add((r, c));
            }

            if (!Ships.Contains(ship))
            {
                Ships.Add(ship);
            }

            ship.SetPositions(positions);

            return true;
        }

        public AttackResult FireAt(int row, int col)
        {
            if (!InBounds(row, col))
                return AttackResult.Invalid;

            var cell = Cells[row, col];

            if (cell.State == CellState.Hit || cell.State == CellState.Miss)
                return AttackResult.AlreadyTried;

            if (!cell.HasShip)
            {
                cell.State = CellState.Miss;
                return AttackResult.Miss;
            }

            cell.State = CellState.Hit;

            var ship = cell.Ship!;
            ship.RegisterHit();

            if (ship.IsSunk)
            {
                SunkShipCount++;
                return AttackResult.Sunk;
            }

            return AttackResult.Hit;
        }

        public void Reset()
        {
            Ships.Clear();
            SunkShipCount = 0;

            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    var cell = Cells[r, c];
                    cell.Ship = null;
                    cell.State = CellState.Empty;
                }
            }
        }
    }
}

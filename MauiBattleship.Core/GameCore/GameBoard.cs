using MauiBattleship.GameCore.Enums;
using MauiBattleship.GameCore.Models;
using MauiBattleship.GameCore.Ships;

namespace MauiBattleship.GameCore
{
    public sealed class GameBoard
    {
        public const int BoardSize = 10;

        private readonly Cell[,] _cells;

        public GameBoard()
        {
            _cells = new Cell[BoardSize, BoardSize];
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    _cells[r, c] = new Cell(r, c);
                }
            }
        }

        public Cell GetCell(int row, int col) => _cells[row, col];

        public bool InBounds(int row, int col)
            => row >= 0 && row < BoardSize && col >= 0 && col < BoardSize;

        public bool CanPlaceShip(ShipBase ship, int startRow, int startCol, ShipOrientation orientation)
        {
            for (int i = 0; i < ship.Size; i++)
            {
                int r = startRow + (orientation == ShipOrientation.Vertical ? i : 0);
                int c = startCol + (orientation == ShipOrientation.Horizontal ? i : 0);

                if (!InBounds(r, c)) return false;

                var cell = GetCell(r, c);
                if (cell.State != CellState.Empty) return false;
            }

            return true;
        }

        public bool PlaceShip(ShipBase ship, int startRow, int startCol, ShipOrientation orientation)
        {
            if (!CanPlaceShip(ship, startRow, startCol, orientation))
                return false;

            ship.Positions.Clear();

            for (int i = 0; i < ship.Size; i++)
            {
                int r = startRow + (orientation == ShipOrientation.Vertical ? i : 0);
                int c = startCol + (orientation == ShipOrientation.Horizontal ? i : 0);

                var cell = GetCell(r, c);
                cell.State = CellState.Ship;
                cell.Ship = ship;

                ship.Positions.Add(cell);
            }

            ship.IsPlaced = true;
            ship.IsSunk = false;

            return true;
        }

        public AttackResult Attack(int row, int col)
        {
            var cell = GetCell(row, col);

            if (cell.HasBeenAttacked)
            {
                return new AttackResult
                {
                    Row = row,
                    Col = col,
                    IsHit = false,
                    Message = "Already attacked."
                };
            }

            if (cell.State == CellState.Ship && cell.Ship != null)
            {
                cell.State = CellState.Hit;

                var ship = cell.Ship;
                ship.RecalculateSunk();

                return new AttackResult
                {
                    Row = row,
                    Col = col,
                    IsHit = true,
                    IsSunk = ship.IsSunk,
                    SunkShipName = ship.IsSunk ? ship.Name : null,
                    Message = ship.IsSunk ? $"Hit! You sunk the {ship.Name}!" : "Hit!"
                };
            }

            cell.State = CellState.Miss;

            return new AttackResult
            {
                Row = row,
                Col = col,
                IsHit = false,
                Message = "Miss."
            };
        }
    }
}

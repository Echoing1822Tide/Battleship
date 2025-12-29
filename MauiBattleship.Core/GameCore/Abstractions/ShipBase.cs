using MauiBattleship.GameCore.Models;

namespace MauiBattleship.GameCore.Ships
{
    public abstract class ShipBase
    {
        public string Name { get; protected set; } = "";
        public int Size { get; protected set; }

        public bool IsPlaced { get; set; }
        public bool IsSunk { get; set; }

        // Cells this ship occupies (set at placement time)
        public List<Cell> Positions { get; } = new();

        public int HitsTaken => Positions.Count(c => c.State == GameCore.Enums.CellState.Hit);

        public void RecalculateSunk()
        {
            if (Positions.Count == 0)
            {
                IsSunk = false;
                return;
            }

            IsSunk = Positions.All(c => c.State == GameCore.Enums.CellState.Hit);
        }
    }
}

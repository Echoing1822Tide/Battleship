using System.Collections.Generic;
using System.Linq;

namespace MauiBattleship.Models
{
    public sealed class Ship
    {
        public string Name { get; }
        public int Size { get; }

        /// <summary>
        /// List of board coordinates occupied by this ship.
        /// </summary>
        public List<(int Row, int Col)> Positions { get; } = new();

        public int Hits { get; private set; }

        public bool IsPlaced => Positions.Count == Size;
        public bool IsSunk   => Hits >= Size;

        public Ship(string name, int size)
        {
            Name = name;
            Size = size;
        }

        internal void SetPositions(IEnumerable<(int Row, int Col)> positions)
        {
            Positions.Clear();
            Positions.AddRange(positions);
        }

        internal void ClearPositions()
        {
            Positions.Clear();
            Hits = 0;
        }

        public void RegisterHit()
        {
            if (!IsSunk)
            {
                Hits++;
            }
        }
    }
}

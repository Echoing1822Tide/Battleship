namespace MauiBattleship.Models
{
    public class Ship
    {
        public string Name { get; }
        public int Size { get; }

        // Board coordinates occupied by the ship
        public List<(int Row, int Col)> Positions { get; } = new();

        public bool IsPlaced { get; set; }
        public int Hits { get; private set; }
        public bool IsSunk => Hits >= Size;

        public Ship(string name, int size)
        {
            Name = name;
            Size = size;
        }

        public void RegisterHit()
        {
            // Guard: don't exceed size (prevents weirdness if hit logic is called incorrectly)
            if (Hits < Size)
                Hits++;
        }
    }
}

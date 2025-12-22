namespace MauiBattleship.Models;

public class Ship
{
    public string Name { get; set; } = "Ship";
    public int Size { get; set; }

    // Placed cells
    public List<ShipPosition> Positions { get; } = new();

    // Hits tracked by coordinate
    private readonly HashSet<ShipPosition> _hits = new();

    public bool IsSunk => Positions.Count > 0 && _hits.Count >= Positions.Count;

    public Ship() { }

    public Ship(string name, int size)
    {
        Name = name;
        Size = size;
    }

    public void RegisterHit(int row, int col)
    {
        _hits.Add(new ShipPosition(row, col));
    }
}

public readonly record struct ShipPosition(int Row, int Col);

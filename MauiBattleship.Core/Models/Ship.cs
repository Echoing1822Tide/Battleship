namespace MauiBattleship.Models;

public sealed class Ship
{
    public string Name { get; }
    public int Size { get; }

    // All occupied coordinates after placement
    public List<(int Row, int Col)> Positions { get; } = new();

    // Which of those coords have been hit
    private readonly HashSet<(int Row, int Col)> _hits = new();

    public bool IsPlaced => Positions.Count == Size;
    public bool IsSunk => IsPlaced && _hits.Count >= Size;

    public Ship(string name, int size)
    {
        Name = name;
        Size = size;
    }

    public void ClearPlacement()
    {
        Positions.Clear();
        _hits.Clear();
    }

    public void RegisterHit(int row, int col)
    {
        _hits.Add((row, col));
    }

    public bool Occupies(int row, int col) => Positions.Contains((row, col));
}

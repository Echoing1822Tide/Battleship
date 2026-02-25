namespace Battleship.GameCore;

public class Ship
{
    private readonly HashSet<ShipPosition> _positions = new();
    private readonly HashSet<ShipPosition> _hitPositions = new();

    public string Name { get; }
    public int Size { get; }

    public List<ShipPosition> Positions { get; } = new();

    public bool IsPlaced { get; set; }
    public int Hits { get; private set; }
    public bool IsSunk => Hits >= Size;

    public Ship(string name, int size)
    {
        Name = name;
        Size = size;
    }

    internal bool TryAddPosition(int row, int col)
    {
        var position = new ShipPosition(row, col);
        if (!_positions.Add(position))
            return false;

        Positions.Add(position);
        return true;
    }

    public void RegisterHit(int row, int col)
    {
        var position = new ShipPosition(row, col);

        if (!_positions.Contains(position))
            return;

        if (_hitPositions.Add(position) && Hits < Size)
            Hits++;
    }
}

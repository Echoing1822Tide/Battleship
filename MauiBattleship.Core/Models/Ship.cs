namespace MauiBattleship.Models;

/// <summary>
/// Represents a ship in the Battleship game.
/// </summary>
public class Ship
{
    /// <summary>
    /// The name of the ship (e.g., "Carrier", "Battleship").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The size/length of the ship in cells.
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// The starting row position of the ship.
    /// </summary>
    public int StartRow { get; set; }

    /// <summary>
    /// The starting column position of the ship.
    /// </summary>
    public int StartColumn { get; set; }

    /// <summary>
    /// The orientation of the ship on the board.
    /// </summary>
    public ShipOrientation Orientation { get; set; }

    /// <summary>
    /// The number of times this ship has been hit.
    /// </summary>
    public int HitCount { get; private set; }

    /// <summary>
    /// Indicates whether the ship has been placed on the board.
    /// </summary>
    public bool IsPlaced { get; set; }

    /// <summary>
    /// Creates a new ship with the specified name and size.
    /// </summary>
    /// <param name="name">The name of the ship.</param>
    /// <param name="size">The size/length of the ship.</param>
    public Ship(string name, int size)
    {
        Name = name;
        Size = size;
        Orientation = ShipOrientation.Horizontal;
        HitCount = 0;
        IsPlaced = false;
    }

    /// <summary>
    /// Indicates whether the ship has been sunk (all cells hit).
    /// </summary>
    public bool IsSunk => HitCount >= Size;

    /// <summary>
    /// Records a hit on this ship.
    /// </summary>
    public void Hit()
    {
        if (HitCount < Size)
        {
            HitCount++;
        }
    }

    /// <summary>
    /// Resets the ship to its initial state.
    /// </summary>
    public void Reset()
    {
        HitCount = 0;
        IsPlaced = false;
    }

    /// <summary>
    /// Gets all the coordinates occupied by this ship.
    /// </summary>
    /// <returns>List of (row, column) tuples.</returns>
    public List<(int Row, int Column)> GetCoordinates()
    {
        var coordinates = new List<(int Row, int Column)>();
        
        if (!IsPlaced) return coordinates;

        for (int i = 0; i < Size; i++)
        {
            int row = Orientation == ShipOrientation.Vertical ? StartRow + i : StartRow;
            int col = Orientation == ShipOrientation.Horizontal ? StartColumn + i : StartColumn;
            coordinates.Add((row, col));
        }

        return coordinates;
    }
}

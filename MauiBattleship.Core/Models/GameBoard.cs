namespace MauiBattleship.Models;

/// <summary>
/// Represents a 10x10 game board for Battleship.
/// </summary>
public class GameBoard
{
    /// <summary>
    /// The size of the game board (10x10).
    /// </summary>
    public const int BoardSize = 10;

    /// <summary>
    /// The 2D array of cells making up the board.
    /// </summary>
    public Cell[,] Cells { get; }

    /// <summary>
    /// The list of ships on this board.
    /// </summary>
    public List<Ship> Ships { get; }

    /// <summary>
    /// Creates a new empty game board.
    /// </summary>
    public GameBoard()
    {
        Cells = new Cell[BoardSize, BoardSize];
        Ships = new List<Ship>();
        InitializeBoard();
    }

    /// <summary>
    /// Initializes all cells on the board.
    /// </summary>
    private void InitializeBoard()
    {
        for (int row = 0; row < BoardSize; row++)
        {
            for (int col = 0; col < BoardSize; col++)
            {
                Cells[row, col] = new Cell(row, col);
            }
        }
    }

    /// <summary>
    /// Checks if a ship can be placed at the specified position.
    /// </summary>
    /// <param name="ship">The ship to place.</param>
    /// <param name="startRow">Starting row position.</param>
    /// <param name="startColumn">Starting column position.</param>
    /// <param name="orientation">Ship orientation.</param>
    /// <returns>True if placement is valid, false otherwise.</returns>
    public bool CanPlaceShip(Ship ship, int startRow, int startColumn, ShipOrientation orientation)
    {
        // Check bounds
        if (startRow < 0 || startColumn < 0)
            return false;

        int endRow = orientation == ShipOrientation.Vertical ? startRow + ship.Size - 1 : startRow;
        int endColumn = orientation == ShipOrientation.Horizontal ? startColumn + ship.Size - 1 : startColumn;

        if (endRow >= BoardSize || endColumn >= BoardSize)
            return false;

        // Check for overlapping ships
        for (int i = 0; i < ship.Size; i++)
        {
            int row = orientation == ShipOrientation.Vertical ? startRow + i : startRow;
            int col = orientation == ShipOrientation.Horizontal ? startColumn + i : startColumn;

            if (Cells[row, col].HasShip)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Places a ship on the board at the specified position.
    /// </summary>
    /// <param name="ship">The ship to place.</param>
    /// <param name="startRow">Starting row position.</param>
    /// <param name="startColumn">Starting column position.</param>
    /// <param name="orientation">Ship orientation.</param>
    /// <returns>True if placement was successful, false otherwise.</returns>
    public bool PlaceShip(Ship ship, int startRow, int startColumn, ShipOrientation orientation)
    {
        if (!CanPlaceShip(ship, startRow, startColumn, orientation))
            return false;

        ship.StartRow = startRow;
        ship.StartColumn = startColumn;
        ship.Orientation = orientation;
        ship.IsPlaced = true;

        // Mark cells as occupied
        for (int i = 0; i < ship.Size; i++)
        {
            int row = orientation == ShipOrientation.Vertical ? startRow + i : startRow;
            int col = orientation == ShipOrientation.Horizontal ? startColumn + i : startColumn;

            Cells[row, col].Ship = ship;
            Cells[row, col].State = CellState.Ship;
        }

        if (!Ships.Contains(ship))
        {
            Ships.Add(ship);
        }

        return true;
    }

    /// <summary>
    /// Processes an attack at the specified position.
    /// </summary>
    /// <param name="row">The row to attack.</param>
    /// <param name="column">The column to attack.</param>
    /// <returns>The result of the attack.</returns>
    public AttackResult Attack(int row, int column)
    {
        if (row < 0 || row >= BoardSize || column < 0 || column >= BoardSize)
            throw new ArgumentOutOfRangeException("Attack position is out of bounds.");

        var cell = Cells[row, column];

        // Already attacked this cell
        if (cell.IsAttacked)
        {
            return new AttackResult(row, column, false, alreadyAttacked: true);
        }

        if (cell.HasShip)
        {
            cell.State = CellState.Hit;
            cell.Ship!.Hit();

            bool isSunk = cell.Ship.IsSunk;
            return new AttackResult(row, column, true, isSunk, isSunk ? cell.Ship.Name : null);
        }
        else
        {
            cell.State = CellState.Miss;
            return new AttackResult(row, column, false);
        }
    }

    /// <summary>
    /// Gets a cell at the specified position.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <returns>The cell at the position.</returns>
    public Cell GetCell(int row, int column)
    {
        if (row < 0 || row >= BoardSize || column < 0 || column >= BoardSize)
            throw new ArgumentOutOfRangeException("Position is out of bounds.");

        return Cells[row, column];
    }

    /// <summary>
    /// Checks if all ships on the board have been sunk.
    /// </summary>
    /// <returns>True if all ships are sunk, false otherwise.</returns>
    public bool AllShipsSunk()
    {
        return Ships.Count > 0 && Ships.All(s => s.IsSunk);
    }

    /// <summary>
    /// Gets the count of ships that have been sunk.
    /// </summary>
    public int SunkShipCount => Ships.Count(s => s.IsSunk);

    /// <summary>
    /// Gets the total number of ships.
    /// </summary>
    public int TotalShipCount => Ships.Count;

    /// <summary>
    /// Resets the board to its initial state.
    /// </summary>
    public void Reset()
    {
        for (int row = 0; row < BoardSize; row++)
        {
            for (int col = 0; col < BoardSize; col++)
            {
                Cells[row, col] = new Cell(row, col);
            }
        }

        foreach (var ship in Ships)
        {
            ship.Reset();
        }

        Ships.Clear();
    }

    /// <summary>
    /// Gets all cells that haven't been attacked yet.
    /// </summary>
    /// <returns>List of unattacked cells.</returns>
    public List<Cell> GetUnattackedCells()
    {
        var cells = new List<Cell>();
        for (int row = 0; row < BoardSize; row++)
        {
            for (int col = 0; col < BoardSize; col++)
            {
                if (!Cells[row, col].IsAttacked)
                {
                    cells.Add(Cells[row, col]);
                }
            }
        }
        return cells;
    }
}

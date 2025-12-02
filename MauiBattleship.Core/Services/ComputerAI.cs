namespace MauiBattleship.Services;

using MauiBattleship.Models;

/// <summary>
/// Provides AI logic for the computer opponent.
/// </summary>
public class ComputerAI
{
    private readonly Random _random;
    private readonly List<(int Row, int Column)> _lastHits;
    private readonly HashSet<(int Row, int Column)> _attackedCells;

    /// <summary>
    /// Creates a new computer AI instance.
    /// </summary>
    public ComputerAI()
    {
        _random = new Random();
        _lastHits = new List<(int Row, int Column)>();
        _attackedCells = new HashSet<(int Row, int Column)>();
    }

    /// <summary>
    /// Gets the next attack position for the computer.
    /// </summary>
    /// <param name="targetBoard">The board to attack.</param>
    /// <returns>The (row, column) to attack.</returns>
    public (int Row, int Column) GetNextAttack(GameBoard targetBoard)
    {
        // If we have hits to follow up on, try adjacent cells
        if (_lastHits.Count > 0)
        {
            var followUp = GetSmartAttack(targetBoard);
            if (followUp.HasValue)
            {
                return followUp.Value;
            }
        }

        // Otherwise, attack randomly
        return GetRandomAttack(targetBoard);
    }

    /// <summary>
    /// Gets a smart attack position based on previous hits.
    /// </summary>
    private (int Row, int Column)? GetSmartAttack(GameBoard targetBoard)
    {
        foreach (var hit in _lastHits.ToList())
        {
            // Check adjacent cells (up, down, left, right)
            var adjacentCells = new[]
            {
                (hit.Row - 1, hit.Column),
                (hit.Row + 1, hit.Column),
                (hit.Row, hit.Column - 1),
                (hit.Row, hit.Column + 1)
            };

            foreach (var (row, col) in adjacentCells)
            {
                if (IsValidTarget(targetBoard, row, col))
                {
                    return (row, col);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets a random attack position.
    /// </summary>
    private (int Row, int Column) GetRandomAttack(GameBoard targetBoard)
    {
        var unattackedCells = targetBoard.GetUnattackedCells();
        
        if (unattackedCells.Count == 0)
        {
            throw new InvalidOperationException("No valid attack positions remaining.");
        }

        // Filter out cells we've already queued for attack
        var validCells = unattackedCells
            .Where(c => !_attackedCells.Contains((c.Row, c.Column)))
            .ToList();

        if (validCells.Count == 0)
        {
            validCells = unattackedCells;
        }

        var cell = validCells[_random.Next(validCells.Count)];
        return (cell.Row, cell.Column);
    }

    /// <summary>
    /// Checks if a position is a valid attack target.
    /// </summary>
    private bool IsValidTarget(GameBoard board, int row, int column)
    {
        if (row < 0 || row >= GameBoard.BoardSize || column < 0 || column >= GameBoard.BoardSize)
            return false;

        var cell = board.GetCell(row, column);
        return !cell.IsAttacked && !_attackedCells.Contains((row, column));
    }

    /// <summary>
    /// Processes the result of an attack to update AI strategy.
    /// </summary>
    /// <param name="result">The attack result.</param>
    public void ProcessAttackResult(AttackResult result)
    {
        _attackedCells.Add((result.Row, result.Column));

        if (result.IsHit)
        {
            _lastHits.Add((result.Row, result.Column));
        }

        if (result.IsSunk)
        {
            // Clear hits for the sunk ship - we'll need to determine which hits
            // belonged to this ship, but for simplicity, clear all if a ship sinks
            _lastHits.Clear();
        }
    }

    /// <summary>
    /// Places ships randomly on the given board.
    /// </summary>
    /// <param name="board">The board to place ships on.</param>
    /// <param name="ships">The ships to place.</param>
    public void PlaceShipsRandomly(GameBoard board, List<Ship> ships)
    {
        foreach (var ship in ships)
        {
            bool placed = false;
            int attempts = 0;
            const int maxAttempts = 1000;

            while (!placed && attempts < maxAttempts)
            {
                int row = _random.Next(GameBoard.BoardSize);
                int col = _random.Next(GameBoard.BoardSize);
                var orientation = _random.Next(2) == 0 ? ShipOrientation.Horizontal : ShipOrientation.Vertical;

                if (board.CanPlaceShip(ship, row, col, orientation))
                {
                    board.PlaceShip(ship, row, col, orientation);
                    placed = true;
                }

                attempts++;
            }

            if (!placed)
            {
                throw new InvalidOperationException($"Could not place ship: {ship.Name}");
            }
        }
    }

    /// <summary>
    /// Resets the AI state for a new game.
    /// </summary>
    public void Reset()
    {
        _lastHits.Clear();
        _attackedCells.Clear();
    }
}

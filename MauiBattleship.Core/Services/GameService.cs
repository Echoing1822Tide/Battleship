namespace MauiBattleship.Services;

using MauiBattleship.Models;

/// <summary>
/// Manages the overall game state and logic.
/// </summary>
public class GameService
{
    /// <summary>
    /// The player's game board.
    /// </summary>
    public GameBoard PlayerBoard { get; private set; }

    /// <summary>
    /// The computer's game board.
    /// </summary>
    public GameBoard ComputerBoard { get; private set; }

    /// <summary>
    /// The current phase of the game.
    /// </summary>
    public GamePhase CurrentPhase { get; private set; }

    /// <summary>
    /// The computer AI opponent.
    /// </summary>
    public ComputerAI ComputerAI { get; }

    /// <summary>
    /// The ships available for the player to place.
    /// </summary>
    public List<Ship> PlayerShips { get; private set; }

    /// <summary>
    /// Event raised when the game state changes.
    /// </summary>
    public event EventHandler? GameStateChanged;

    /// <summary>
    /// Event raised when a message should be displayed.
    /// </summary>
    public event EventHandler<string>? MessageReceived;

    /// <summary>
    /// Creates a new game service.
    /// </summary>
    public GameService()
    {
        PlayerBoard = new GameBoard();
        ComputerBoard = new GameBoard();
        ComputerAI = new ComputerAI();
        PlayerShips = CreateStandardFleet();
        CurrentPhase = GamePhase.NotStarted;
    }

    /// <summary>
    /// Creates the standard fleet of ships.
    /// </summary>
    /// <returns>List of ships for a standard game.</returns>
    public static List<Ship> CreateStandardFleet()
    {
        return new List<Ship>
        {
            new Ship("Carrier", 5),
            new Ship("Battleship", 4),
            new Ship("Cruiser", 3),
            new Ship("Submarine", 3),
            new Ship("Destroyer", 2)
        };
    }

    /// <summary>
    /// Starts a new game.
    /// </summary>
    public void StartNewGame()
    {
        PlayerBoard = new GameBoard();
        ComputerBoard = new GameBoard();
        ComputerAI.Reset();
        
        PlayerShips = CreateStandardFleet();
        var computerShips = CreateStandardFleet();
        
        // Place computer ships randomly
        ComputerAI.PlaceShipsRandomly(ComputerBoard, computerShips);
        
        CurrentPhase = GamePhase.PlacingShips;
        GameStateChanged?.Invoke(this, EventArgs.Empty);
        MessageReceived?.Invoke(this, "Place your ships on the board.");
    }

    /// <summary>
    /// Attempts to place a player's ship on the board.
    /// </summary>
    /// <param name="ship">The ship to place.</param>
    /// <param name="row">Starting row.</param>
    /// <param name="column">Starting column.</param>
    /// <param name="orientation">Ship orientation.</param>
    /// <returns>True if placement was successful.</returns>
    public bool PlacePlayerShip(Ship ship, int row, int column, ShipOrientation orientation)
    {
        if (CurrentPhase != GamePhase.PlacingShips)
            return false;

        bool success = PlayerBoard.PlaceShip(ship, row, column, orientation);
        
        if (success)
        {
            GameStateChanged?.Invoke(this, EventArgs.Empty);
            
            // Check if all ships are placed
            if (PlayerShips.All(s => s.IsPlaced))
            {
                CurrentPhase = GamePhase.PlayerTurn;
                MessageReceived?.Invoke(this, "All ships placed! Your turn to attack.");
            }
            else
            {
                var nextShip = PlayerShips.FirstOrDefault(s => !s.IsPlaced);
                MessageReceived?.Invoke(this, $"Place your {nextShip?.Name} ({nextShip?.Size} cells).");
            }
        }
        
        return success;
    }

    /// <summary>
    /// Processes a player's attack on the computer's board.
    /// </summary>
    /// <param name="row">Row to attack.</param>
    /// <param name="column">Column to attack.</param>
    /// <returns>The attack result, or null if attack not allowed.</returns>
    public AttackResult? PlayerAttack(int row, int column)
    {
        if (CurrentPhase != GamePhase.PlayerTurn)
            return null;

        var result = ComputerBoard.Attack(row, column);
        
        if (result.AlreadyAttacked)
        {
            MessageReceived?.Invoke(this, "You already attacked this cell!");
            return result;
        }

        if (result.IsHit)
        {
            if (result.IsSunk)
            {
                MessageReceived?.Invoke(this, $"Hit! You sunk the enemy's {result.SunkShipName}!");
            }
            else
            {
                MessageReceived?.Invoke(this, "Hit!");
            }
        }
        else
        {
            MessageReceived?.Invoke(this, "Miss!");
        }

        GameStateChanged?.Invoke(this, EventArgs.Empty);

        // Check for win
        if (ComputerBoard.AllShipsSunk())
        {
            CurrentPhase = GamePhase.GameOver;
            MessageReceived?.Invoke(this, "Congratulations! You won!");
            return result;
        }

        // Computer's turn
        CurrentPhase = GamePhase.ComputerTurn;
        return result;
    }

    /// <summary>
    /// Executes the computer's turn.
    /// </summary>
    /// <returns>The attack result.</returns>
    public AttackResult ComputerAttack()
    {
        if (CurrentPhase != GamePhase.ComputerTurn)
            throw new InvalidOperationException("Not the computer's turn.");

        var (row, column) = ComputerAI.GetNextAttack(PlayerBoard);
        var result = PlayerBoard.Attack(row, column);
        
        ComputerAI.ProcessAttackResult(result);
        
        if (result.IsHit)
        {
            if (result.IsSunk)
            {
                MessageReceived?.Invoke(this, $"Computer hit and sunk your {result.SunkShipName}!");
            }
            else
            {
                MessageReceived?.Invoke(this, $"Computer hit your ship at ({row + 1}, {column + 1})!");
            }
        }
        else
        {
            MessageReceived?.Invoke(this, $"Computer missed at ({row + 1}, {column + 1}).");
        }

        GameStateChanged?.Invoke(this, EventArgs.Empty);

        // Check for loss
        if (PlayerBoard.AllShipsSunk())
        {
            CurrentPhase = GamePhase.GameOver;
            MessageReceived?.Invoke(this, "Game Over! The computer won!");
            return result;
        }

        // Back to player's turn
        CurrentPhase = GamePhase.PlayerTurn;
        return result;
    }

    /// <summary>
    /// Gets the next ship that needs to be placed.
    /// </summary>
    /// <returns>The next unplaced ship, or null if all are placed.</returns>
    public Ship? GetNextShipToPlace()
    {
        return PlayerShips.FirstOrDefault(s => !s.IsPlaced);
    }

    /// <summary>
    /// Indicates whether the game is over.
    /// </summary>
    public bool IsGameOver => CurrentPhase == GamePhase.GameOver;

    /// <summary>
    /// Indicates whether the player won.
    /// </summary>
    public bool PlayerWon => IsGameOver && ComputerBoard.AllShipsSunk();
}

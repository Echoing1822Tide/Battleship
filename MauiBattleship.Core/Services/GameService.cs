using MauiBattleship.Models;

namespace MauiBattleship.Services;

public sealed class GameService
{
    private readonly FleetService _fleetService = new();
    private readonly Random _rnd = new();

    public GameBoard PlayerBoard { get; private set; } = new(10);
    public GameBoard EnemyBoard  { get; private set; } = new(10);

    public List<Ship> PlayerShips { get; private set; } = new();
    public List<Ship> EnemyShips  { get; private set; } = new();

    public GamePhase Phase { get; private set; } = GamePhase.PlacingShips;

    // UI uses this to place ships one-by-one
    public int CurrentPlacementIndex { get; private set; } = 0;

    // Orientation toggle (horizontal default)
    public bool CurrentPlacementIsVertical { get; private set; } = false;

    public Ship? CurrentShipToPlace =>
        (Phase == GamePhase.PlacingShips && CurrentPlacementIndex < PlayerShips.Count)
            ? PlayerShips[CurrentPlacementIndex]
            : null;

    public void StartNewGame()
    {
        PlayerBoard = new GameBoard(10);
        EnemyBoard  = new GameBoard(10);

        PlayerShips = _fleetService.CreateStandardFleet();
        EnemyShips  = _fleetService.CreateStandardFleet();

        PlayerBoard.SetFleet(PlayerShips);
        EnemyBoard.SetFleet(EnemyShips);

        CurrentPlacementIndex = 0;
        CurrentPlacementIsVertical = false;
        Phase = GamePhase.PlacingShips;

        AutoPlaceFleetRandomly(EnemyBoard, EnemyShips);
    }

    public void ToggleOrientation()
    {
        if (Phase != GamePhase.PlacingShips) return;
        CurrentPlacementIsVertical = !CurrentPlacementIsVertical;
    }

    public bool TryPlaceNextPlayerShip(int row, int col)
    {
        if (Phase != GamePhase.PlacingShips) return false;

        var ship = CurrentShipToPlace;
        if (ship is null) return false;

        bool placed = PlayerBoard.TryPlaceShip(ship, row, col, CurrentPlacementIsVertical);
        if (!placed) return false;

        CurrentPlacementIndex++;

        if (CurrentPlacementIndex >= PlayerShips.Count)
            Phase = GamePhase.PlayerTurn;

        return true;
    }

    public AttackResult PlayerAttack(int row, int col)
    {
        if (Phase != GamePhase.PlayerTurn)
        {
            return new AttackResult
            {
                Row = row, Col = col,
                IsHit = false,
                Message = "Not your turn."
            };
        }

        var result = EnemyBoard.Attack(row, col);

        if (result.Message == "Already attacked.")
            return result;

        if (EnemyBoard.AllShipsSunk)
        {
            Phase = GamePhase.GameOver;
            result.Message = "Enemy fleet destroyed. Victory!";
            return result;
        }

        Phase = GamePhase.EnemyTurn;
        return result;
    }

    public AttackResult EnemyAttackRandom()
    {
        if (Phase != GamePhase.EnemyTurn)
        {
            return new AttackResult
            {
                Row = -1, Col = -1,
                IsHit = false,
                Message = "Not enemy turn."
            };
        }

        int row, col;
        do
        {
            row = _rnd.Next(0, PlayerBoard.Size);
            col = _rnd.Next(0, PlayerBoard.Size);
        } while (PlayerBoard.IsAlreadyAttacked(row, col));

        var result = PlayerBoard.Attack(row, col);

        if (PlayerBoard.AllShipsSunk)
        {
            Phase = GamePhase.GameOver;
            result.Message = "Your fleet was destroyed. Defeat.";
            return result;
        }

        Phase = GamePhase.PlayerTurn;
        return result;
    }

    private void AutoPlaceFleetRandomly(GameBoard board, List<Ship> fleet)
    {
        foreach (var ship in fleet)
        {
            bool placed = false;
            int safety = 0;

            while (!placed && safety < 2000)
            {
                safety++;

                bool vertical = _rnd.Next(0, 2) == 0;

                int maxRow = vertical ? board.Size - ship.Size : board.Size - 1;
                int maxCol = vertical ? board.Size - 1 : board.Size - ship.Size;

                int r = _rnd.Next(0, maxRow + 1);
                int c = _rnd.Next(0, maxCol + 1);

                placed = board.TryPlaceShip(ship, r, c, vertical);
            }
        }
    }
}

using MauiBattleship.Models;
using MauiBattleship.Services;

namespace MauiBattleship.Services;

public class GameService
{
    private readonly FleetService _fleetService = new();
    private readonly Random _rnd = new();

    private List<Ship> _playerFleet = new();
    private List<Ship> _enemyFleet = new();

    public GameBoard PlayerBoard { get; private set; } = new(10);
    public GameBoard EnemyBoard { get; private set; } = new(10);

    public GamePhase CurrentPhase { get; private set; } = GamePhase.PlacingShips;

    public void StartNewGame()
    {
        PlayerBoard = new GameBoard(10);
        EnemyBoard = new GameBoard(10);

        _playerFleet = _fleetService.CreateStandardFleet();
        _enemyFleet = _fleetService.CreateStandardFleet();

        PlayerBoard.SetFleet(_playerFleet);
        EnemyBoard.SetFleet(_enemyFleet);

        // Simple enemy auto-placement so the game can proceed
        AutoPlaceFleetRandomly(EnemyBoard, _enemyFleet);

        CurrentPhase = GamePhase.PlacingShips;
    }

    public List<Ship> GetPlayerFleet() => _playerFleet;

    public Ship? GetNextPlayerShipToPlace()
        => _playerFleet.FirstOrDefault(s => s.Positions.Count == 0);

    public bool PlacePlayerShip(Ship ship, int row, int col, bool isVertical)
    {
        if (CurrentPhase != GamePhase.PlacingShips)
            return false;

        // IMPORTANT: use the instance from _playerFleet (not a stale reference)
        var fleetShip = _playerFleet.FirstOrDefault(s => s.Name == ship.Name && s.Size == ship.Size && s.Positions.Count == 0);
        if (fleetShip is null)
            return false;

        var ok = PlayerBoard.TryPlaceShip(fleetShip, row, col, isVertical);
        if (!ok) return false;

        // If all placed, move to player turn
        if (_playerFleet.All(s => s.Positions.Count > 0))
            CurrentPhase = GamePhase.PlayerTurn;

        return true;
    }

    public string PlayerAttacksEnemy(int row, int col)
    {
        if (CurrentPhase != GamePhase.PlayerTurn)
            return "Not your turn.";

        var result = EnemyBoard.Attack(row, col);

        if (EnemyBoard.AllShipsSunk)
        {
            CurrentPhase = GamePhase.GameOver;
            return "You win! Enemy fleet destroyed.";
        }

        // Enemy attacks back
        var enemyResult = EnemyTurn();
        return $"{result} | Enemy: {enemyResult}";
    }

    private string EnemyTurn()
    {
        int row, col;
        do
        {
            row = _rnd.Next(0, PlayerBoard.Size);
            col = _rnd.Next(0, PlayerBoard.Size);
        } while (PlayerBoard.IsAlreadyAttacked(row, col));

        var result = PlayerBoard.Attack(row, col);

        if (PlayerBoard.AllShipsSunk)
        {
            CurrentPhase = GamePhase.GameOver;
            return "Enemy wins! Your fleet is destroyed.";
        }

        return result;
    }

    private void AutoPlaceFleetRandomly(GameBoard board, List<Ship> fleet)
    {
        foreach (var ship in fleet)
        {
            var placed = false;

            for (var attempts = 0; attempts < 500 && !placed; attempts++)
            {
                var vertical = _rnd.Next(0, 2) == 0;
                var row = _rnd.Next(0, board.Size);
                var col = _rnd.Next(0, board.Size);

                placed = board.TryPlaceShip(ship, row, col, vertical);
            }

            if (!placed)
                throw new InvalidOperationException($"Failed to auto-place {ship.Name}.");
        }
    }
}

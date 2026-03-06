using Battleship.GameCore;
using BattleshipMaui.ViewModels;

namespace BattleshipMaui.Tests;

public class EnemyTargetingStrategyTests
{
    private static readonly (string Name, int Size)[] FleetTemplateData =
    [
        ("Aircraft Carrier", 5),
        ("Battleship", 4),
        ("Cruiser", 3),
        ("Submarine", 3),
        ("Destroyer", 2)
    ];

    [Fact]
    [Trait("Category", "Core9")]
    public void GetNextShot_ReturnsUniqueInBoundsCoordinates()
    {
        var strategy = new EnemyTargetingStrategy(10, new Random(5));
        var seen = new HashSet<BoardCoordinate>();

        for (int i = 0; i < 100; i++)
        {
            var shot = strategy.GetNextShot();
            Assert.InRange(shot.Row, 0, 9);
            Assert.InRange(shot.Col, 0, 9);
            Assert.True(seen.Add(shot));
            strategy.RegisterShotOutcome(shot, AttackResult.Miss);
        }

        Assert.Throws<InvalidOperationException>(() => strategy.GetNextShot());
    }

    [Fact]
    [Trait("Category", "Core9")]
    public void RegisterHit_PrioritizesAdjacentTargetShots()
    {
        var strategy = new EnemyTargetingStrategy(10, new Random(9));
        var hit = new BoardCoordinate(4, 4);

        strategy.RegisterShotOutcome(hit, AttackResult.Hit);
        var next = strategy.GetNextShot();

        int manhattanDistance = Math.Abs(next.Row - hit.Row) + Math.Abs(next.Col - hit.Col);
        Assert.Equal(1, manhattanDistance);
    }

    [Fact]
    public void EasyMode_AfterHit_StillFocusesImmediateNextShotNearImpact()
    {
        var strategy = new EnemyTargetingStrategy(10, new Random(15), CpuDifficulty.Easy);
        var hit = new BoardCoordinate(6, 6);

        strategy.RegisterShotOutcome(hit, AttackResult.Hit);
        var next = strategy.GetNextShot();

        int manhattanDistance = Math.Abs(next.Row - hit.Row) + Math.Abs(next.Col - hit.Col);
        Assert.Equal(1, manhattanDistance);
    }

    [Fact]
    [Trait("Category", "Core9")]
    public void TwoAlignedHits_TargetsShipLineFirst()
    {
        var strategy = new EnemyTargetingStrategy(10, new Random(11));
        strategy.RegisterShotOutcome(new BoardCoordinate(3, 3), AttackResult.Hit);
        strategy.RegisterShotOutcome(new BoardCoordinate(3, 4), AttackResult.Hit);

        var next = strategy.GetNextShot();

        Assert.Equal(3, next.Row);
        Assert.True(next.Col == 2 || next.Col == 5);
    }

    [Fact]
    public void RegisterSunk_ClearsPendingTargetQueue()
    {
        var strategy = new EnemyTargetingStrategy(10, new Random(17));
        strategy.RegisterShotOutcome(new BoardCoordinate(5, 5), AttackResult.Hit);
        Assert.True(strategy.PendingTargetCount > 0);

        strategy.RegisterShotOutcome(new BoardCoordinate(5, 6), AttackResult.Sunk, sunkShipSize: 2);

        Assert.Equal(0, strategy.PendingTargetCount);
    }

    [Fact]
    public void PrimeFromBoard_PreservesUnresolvedHitsAndSkipsResolvedShips()
    {
        var board = new GameBoard(10);
        var destroyer = new Ship("Destroyer", 2);
        var battleship = new Ship("Battleship", 4);
        Assert.True(board.TryPlaceShip(destroyer, 1, 1, ShipOrientation.Horizontal));
        Assert.True(board.TryPlaceShip(battleship, 5, 4, ShipOrientation.Horizontal));
        board.SetFleet([destroyer, battleship]);

        board.Attack(1, 1);
        board.Attack(1, 2);
        board.Attack(5, 5);

        var strategy = new EnemyTargetingStrategy(
            10,
            new Random(21),
            CpuDifficulty.Hard,
            board.Fleet.Where(ship => !ship.IsSunk).Select(ship => ship.Size));

        strategy.PrimeFromBoard(board);
        var next = strategy.GetNextShot();

        int manhattanDistance = Math.Abs(next.Row - 5) + Math.Abs(next.Col - 5);
        Assert.Equal(1, manhattanDistance);
        Assert.NotEqual(new BoardCoordinate(1, 1), next);
        Assert.NotEqual(new BoardCoordinate(1, 2), next);
    }

    [Fact]
    public void HardMode_SinksRandomFleetsInFewerShotsThanEasyMode()
    {
        int easyShots = 0;
        int hardShots = 0;

        for (int seed = 0; seed < 24; seed++)
        {
            easyShots += SimulateFleetBattle(CpuDifficulty.Easy, boardSeed: 1000 + seed, strategySeed: 2000 + seed);
            hardShots += SimulateFleetBattle(CpuDifficulty.Hard, boardSeed: 1000 + seed, strategySeed: 3000 + seed);
        }

        Assert.True(hardShots < easyShots, $"Expected hard AI to outperform easy AI, but hard={hardShots} easy={easyShots}.");
        Assert.True(hardShots <= easyShots - 18, $"Expected a meaningful hard-mode advantage, but hard={hardShots} easy={easyShots}.");
    }

    private static int SimulateFleetBattle(CpuDifficulty difficulty, int boardSeed, int strategySeed)
    {
        var board = CreateRandomBoard(boardSeed);
        var strategy = new EnemyTargetingStrategy(
            10,
            new Random(strategySeed),
            difficulty,
            board.Fleet.Select(ship => ship.Size));

        int shots = 0;
        while (!board.AllShipsSunk)
        {
            var target = strategy.GetNextShot();
            var result = board.Attack(target.Row, target.Col);
            strategy.RegisterShotOutcome(target, result.Result, ResolveShipSize(board, result.SunkShipName));
            shots++;

            if (shots > 100)
                throw new InvalidOperationException("Enemy AI exceeded the available board shots.");
        }

        return shots;
    }

    private static GameBoard CreateRandomBoard(int seed)
    {
        var random = new Random(seed);
        var board = new GameBoard(10);
        var fleet = new List<Ship>(FleetTemplateData.Length);

        foreach (var (name, size) in FleetTemplateData)
        {
            var ship = new Ship(name, size);
            bool placed = false;

            for (int attempt = 0; attempt < 512 && !placed; attempt++)
            {
                int row = random.Next(10);
                int col = random.Next(10);
                var orientation = random.Next(2) == 0 ? ShipOrientation.Horizontal : ShipOrientation.Vertical;
                placed = board.TryPlaceShip(ship, row, col, orientation);
            }

            if (!placed)
                throw new InvalidOperationException($"Unable to place ship {name} for simulation seed {seed}.");

            fleet.Add(ship);
        }

        board.SetFleet(fleet);
        return board;
    }

    private static int? ResolveShipSize(GameBoard board, string? sunkShipName)
    {
        if (string.IsNullOrWhiteSpace(sunkShipName))
            return null;

        return board.Fleet.First(ship => string.Equals(ship.Name, sunkShipName, StringComparison.OrdinalIgnoreCase)).Size;
    }
}

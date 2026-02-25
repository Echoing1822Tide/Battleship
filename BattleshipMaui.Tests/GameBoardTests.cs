using Battleship.GameCore;

namespace BattleshipMaui.Tests;

public class GameBoardTests
{
    [Fact]
    [Trait("Category", "Core9")]
    public void TryPlaceShip_AllowsHorizontalAndVerticalPlacement()
    {
        var board = new GameBoard(10);
        var cruiser = new Ship("Cruiser", 3);
        var battleship = new Ship("Battleship", 4);

        bool horizontalPlaced = board.TryPlaceShip(cruiser, 0, 0, ShipOrientation.Horizontal);
        bool verticalPlaced = board.TryPlaceShip(battleship, 2, 5, ShipOrientation.Vertical);

        Assert.True(horizontalPlaced);
        Assert.True(verticalPlaced);
        Assert.Equal(3, cruiser.Positions.Count);
        Assert.Equal(4, battleship.Positions.Count);
    }

    [Fact]
    [Trait("Category", "Core9")]
    public void TryPlaceShip_RejectsOutOfBoundsAndOverlap()
    {
        var board = new GameBoard(10);
        var carrier = new Ship("Carrier", 5);
        var destroyer = new Ship("Destroyer", 2);

        bool outOfBounds = board.TryPlaceShip(carrier, 0, 7, ShipOrientation.Horizontal);
        bool firstPlacement = board.TryPlaceShip(carrier, 1, 1, ShipOrientation.Horizontal);
        bool overlapPlacement = board.TryPlaceShip(destroyer, 1, 2, ShipOrientation.Vertical);

        Assert.False(outOfBounds);
        Assert.True(firstPlacement);
        Assert.False(overlapPlacement);
    }

    [Fact]
    [Trait("Category", "Core9")]
    public void Attack_ReturnsHitSunkAlreadyTriedAndMiss()
    {
        var board = new GameBoard(10);
        var destroyer = new Ship("Destroyer", 2);
        Assert.True(board.TryPlaceShip(destroyer, 0, 0, ShipOrientation.Horizontal));
        board.SetFleet(new List<Ship> { destroyer });

        ShotInfo hit = board.Attack(0, 0);
        ShotInfo alreadyTried = board.Attack(0, 0);
        ShotInfo sunk = board.Attack(0, 1);
        ShotInfo miss = board.Attack(4, 4);

        Assert.Equal(AttackResult.Hit, hit.Result);
        Assert.Equal(AttackResult.AlreadyTried, alreadyTried.Result);
        Assert.Equal(AttackResult.Sunk, sunk.Result);
        Assert.Equal("Destroyer", sunk.SunkShipName);
        Assert.Equal(AttackResult.Miss, miss.Result);
        Assert.True(board.AllShipsSunk);
    }

    [Fact]
    public void IsAlreadyAttacked_TracksHitAndMiss_NotUnattackedShipCells()
    {
        var board = new GameBoard(10);
        var submarine = new Ship("Submarine", 3);
        Assert.True(board.TryPlaceShip(submarine, 2, 2, ShipOrientation.Vertical));

        Assert.False(board.IsAlreadyAttacked(2, 2));
        Assert.False(board.IsAlreadyAttacked(0, 0));

        _ = board.Attack(2, 2);
        _ = board.Attack(0, 0);

        Assert.True(board.IsAlreadyAttacked(2, 2));
        Assert.True(board.IsAlreadyAttacked(0, 0));
    }
}

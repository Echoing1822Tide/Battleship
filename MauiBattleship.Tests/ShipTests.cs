using MauiBattleship.Models;
using Xunit;

namespace MauiBattleship.Tests;

public class ShipTests
{
    [Fact]
    public void Ship_InitializesCorrectly()
    {
        var ship = new Ship("Battleship", 4);
        
        Assert.Equal("Battleship", ship.Name);
        Assert.Equal(4, ship.Size);
        Assert.Equal(0, ship.HitCount);
        Assert.False(ship.IsSunk);
        Assert.False(ship.IsPlaced);
        Assert.Equal(ShipOrientation.Horizontal, ship.Orientation);
    }

    [Fact]
    public void Ship_Hit_IncreasesHitCount()
    {
        var ship = new Ship("Destroyer", 2);
        
        ship.Hit();
        
        Assert.Equal(1, ship.HitCount);
        Assert.False(ship.IsSunk);
    }

    [Fact]
    public void Ship_IsSunk_WhenAllCellsHit()
    {
        var ship = new Ship("Destroyer", 2);
        
        ship.Hit();
        ship.Hit();
        
        Assert.Equal(2, ship.HitCount);
        Assert.True(ship.IsSunk);
    }

    [Fact]
    public void Ship_Hit_DoesNotExceedSize()
    {
        var ship = new Ship("Destroyer", 2);
        
        ship.Hit();
        ship.Hit();
        ship.Hit(); // Extra hit
        
        Assert.Equal(2, ship.HitCount);
    }

    [Fact]
    public void Ship_Reset_ClearsState()
    {
        var ship = new Ship("Battleship", 4);
        ship.IsPlaced = true;
        ship.Hit();
        ship.Hit();
        
        ship.Reset();
        
        Assert.Equal(0, ship.HitCount);
        Assert.False(ship.IsPlaced);
        Assert.False(ship.IsSunk);
    }

    [Fact]
    public void Ship_GetCoordinates_ReturnsEmpty_WhenNotPlaced()
    {
        var ship = new Ship("Cruiser", 3);
        
        var coords = ship.GetCoordinates();
        
        Assert.Empty(coords);
    }

    [Fact]
    public void Ship_GetCoordinates_ReturnsHorizontalPositions()
    {
        var ship = new Ship("Cruiser", 3);
        ship.StartRow = 2;
        ship.StartColumn = 3;
        ship.Orientation = ShipOrientation.Horizontal;
        ship.IsPlaced = true;
        
        var coords = ship.GetCoordinates();
        
        Assert.Equal(3, coords.Count);
        Assert.Contains((2, 3), coords);
        Assert.Contains((2, 4), coords);
        Assert.Contains((2, 5), coords);
    }

    [Fact]
    public void Ship_GetCoordinates_ReturnsVerticalPositions()
    {
        var ship = new Ship("Cruiser", 3);
        ship.StartRow = 2;
        ship.StartColumn = 3;
        ship.Orientation = ShipOrientation.Vertical;
        ship.IsPlaced = true;
        
        var coords = ship.GetCoordinates();
        
        Assert.Equal(3, coords.Count);
        Assert.Contains((2, 3), coords);
        Assert.Contains((3, 3), coords);
        Assert.Contains((4, 3), coords);
    }
}

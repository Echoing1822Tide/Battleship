using MauiBattleship.Models;
using Xunit;

namespace MauiBattleship.Tests;

public class CellTests
{
    [Fact]
    public void Cell_InitializesWithEmptyState()
    {
        var cell = new Cell(0, 0);
        
        Assert.Equal(CellState.Empty, cell.State);
        Assert.False(cell.HasShip);
        Assert.False(cell.IsAttacked);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 5)]
    [InlineData(9, 9)]
    public void Cell_StoresCorrectPosition(int row, int column)
    {
        var cell = new Cell(row, column);
        
        Assert.Equal(row, cell.Row);
        Assert.Equal(column, cell.Column);
    }

    [Fact]
    public void Cell_HasShip_WhenShipAssigned()
    {
        var cell = new Cell(0, 0);
        var ship = new Ship("Test", 3);
        
        cell.Ship = ship;
        
        Assert.True(cell.HasShip);
        Assert.Equal(ship, cell.Ship);
    }

    [Theory]
    [InlineData(CellState.Hit)]
    [InlineData(CellState.Miss)]
    public void Cell_IsAttacked_WhenHitOrMiss(CellState state)
    {
        var cell = new Cell(0, 0);
        cell.State = state;
        
        Assert.True(cell.IsAttacked);
    }

    [Theory]
    [InlineData(CellState.Empty)]
    [InlineData(CellState.Ship)]
    public void Cell_IsNotAttacked_WhenEmptyOrShip(CellState state)
    {
        var cell = new Cell(0, 0);
        cell.State = state;
        
        Assert.False(cell.IsAttacked);
    }
}

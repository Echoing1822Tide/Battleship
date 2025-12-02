using MauiBattleship.Models;
using MauiBattleship.Services;
using Xunit;

namespace MauiBattleship.Tests;

public class ComputerAITests
{
    [Fact]
    public void ComputerAI_PlaceShipsRandomly_PlacesAllShips()
    {
        var board = new GameBoard();
        var ships = new List<Ship>
        {
            new Ship("Carrier", 5),
            new Ship("Battleship", 4),
            new Ship("Cruiser", 3),
            new Ship("Submarine", 3),
            new Ship("Destroyer", 2)
        };
        var ai = new ComputerAI();
        
        ai.PlaceShipsRandomly(board, ships);
        
        Assert.Equal(5, board.Ships.Count);
        Assert.All(ships, s => Assert.True(s.IsPlaced));
    }

    [Fact]
    public void ComputerAI_GetNextAttack_ReturnsValidPosition()
    {
        var board = new GameBoard();
        var ai = new ComputerAI();
        
        var (row, column) = ai.GetNextAttack(board);
        
        Assert.InRange(row, 0, GameBoard.BoardSize - 1);
        Assert.InRange(column, 0, GameBoard.BoardSize - 1);
    }

    [Fact]
    public void ComputerAI_GetNextAttack_DoesNotRepeatPositions()
    {
        var board = new GameBoard();
        var ai = new ComputerAI();
        var attacks = new HashSet<(int, int)>();
        
        for (int i = 0; i < 50; i++)
        {
            var (row, column) = ai.GetNextAttack(board);
            var result = board.Attack(row, column);
            ai.ProcessAttackResult(result);
            
            Assert.DoesNotContain((row, column), attacks);
            attacks.Add((row, column));
        }
    }

    [Fact]
    public void ComputerAI_Reset_ClearsState()
    {
        var board = new GameBoard();
        var ai = new ComputerAI();
        
        // Make some attacks
        var (row, column) = ai.GetNextAttack(board);
        var result = board.Attack(row, column);
        ai.ProcessAttackResult(result);
        
        ai.Reset();
        
        // After reset, AI should work on a fresh board
        var board2 = new GameBoard();
        var (row2, column2) = ai.GetNextAttack(board2);
        Assert.InRange(row2, 0, GameBoard.BoardSize - 1);
        Assert.InRange(column2, 0, GameBoard.BoardSize - 1);
    }

    [Fact]
    public void ComputerAI_FollowsUp_AfterHit()
    {
        var board = new GameBoard();
        var ship = new Ship("Carrier", 5);
        board.PlaceShip(ship, 5, 5, ShipOrientation.Horizontal);
        
        var ai = new ComputerAI();
        
        // Simulate a hit
        var hitResult = new AttackResult(5, 5, true, false);
        ai.ProcessAttackResult(hitResult);
        
        // Get next attack - should be adjacent to the hit
        var (row, column) = ai.GetNextAttack(board);
        
        // Should be one of the adjacent cells
        bool isAdjacent = (row == 4 && column == 5) ||
                          (row == 6 && column == 5) ||
                          (row == 5 && column == 4) ||
                          (row == 5 && column == 6);
        
        Assert.True(isAdjacent, $"Expected adjacent cell, got ({row}, {column})");
    }
}

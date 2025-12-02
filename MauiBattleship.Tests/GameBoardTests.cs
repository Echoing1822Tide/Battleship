using MauiBattleship.Models;
using Xunit;

namespace MauiBattleship.Tests;

public class GameBoardTests
{
    [Fact]
    public void GameBoard_InitializesWithEmptyCells()
    {
        var board = new GameBoard();
        
        Assert.Equal(10, GameBoard.BoardSize);
        Assert.Empty(board.Ships);
        
        for (int row = 0; row < GameBoard.BoardSize; row++)
        {
            for (int col = 0; col < GameBoard.BoardSize; col++)
            {
                Assert.Equal(CellState.Empty, board.Cells[row, col].State);
                Assert.False(board.Cells[row, col].HasShip);
            }
        }
    }

    [Fact]
    public void GameBoard_CanPlaceShip_ValidPosition_Horizontal()
    {
        var board = new GameBoard();
        var ship = new Ship("Destroyer", 2);
        
        bool canPlace = board.CanPlaceShip(ship, 0, 0, ShipOrientation.Horizontal);
        
        Assert.True(canPlace);
    }

    [Fact]
    public void GameBoard_CanPlaceShip_ValidPosition_Vertical()
    {
        var board = new GameBoard();
        var ship = new Ship("Destroyer", 2);
        
        bool canPlace = board.CanPlaceShip(ship, 0, 0, ShipOrientation.Vertical);
        
        Assert.True(canPlace);
    }

    [Fact]
    public void GameBoard_CanPlaceShip_ReturnsFalse_WhenOutOfBounds_Horizontal()
    {
        var board = new GameBoard();
        var ship = new Ship("Carrier", 5);
        
        bool canPlace = board.CanPlaceShip(ship, 0, 7, ShipOrientation.Horizontal);
        
        Assert.False(canPlace);
    }

    [Fact]
    public void GameBoard_CanPlaceShip_ReturnsFalse_WhenOutOfBounds_Vertical()
    {
        var board = new GameBoard();
        var ship = new Ship("Carrier", 5);
        
        bool canPlace = board.CanPlaceShip(ship, 7, 0, ShipOrientation.Vertical);
        
        Assert.False(canPlace);
    }

    [Fact]
    public void GameBoard_CanPlaceShip_ReturnsFalse_WhenOverlapping()
    {
        var board = new GameBoard();
        var ship1 = new Ship("Destroyer", 2);
        var ship2 = new Ship("Cruiser", 3);
        
        board.PlaceShip(ship1, 0, 0, ShipOrientation.Horizontal);
        bool canPlace = board.CanPlaceShip(ship2, 0, 0, ShipOrientation.Vertical);
        
        Assert.False(canPlace);
    }

    [Fact]
    public void GameBoard_PlaceShip_UpdatesCells()
    {
        var board = new GameBoard();
        var ship = new Ship("Destroyer", 2);
        
        bool placed = board.PlaceShip(ship, 0, 0, ShipOrientation.Horizontal);
        
        Assert.True(placed);
        Assert.True(ship.IsPlaced);
        Assert.Contains(ship, board.Ships);
        Assert.Equal(CellState.Ship, board.Cells[0, 0].State);
        Assert.Equal(CellState.Ship, board.Cells[0, 1].State);
        Assert.Equal(ship, board.Cells[0, 0].Ship);
        Assert.Equal(ship, board.Cells[0, 1].Ship);
    }

    [Fact]
    public void GameBoard_Attack_ReturnsHit_WhenShipPresent()
    {
        var board = new GameBoard();
        var ship = new Ship("Destroyer", 2);
        board.PlaceShip(ship, 0, 0, ShipOrientation.Horizontal);
        
        var result = board.Attack(0, 0);
        
        Assert.True(result.IsHit);
        Assert.False(result.IsSunk);
        Assert.Equal(CellState.Hit, board.Cells[0, 0].State);
    }

    [Fact]
    public void GameBoard_Attack_ReturnsMiss_WhenNoShip()
    {
        var board = new GameBoard();
        
        var result = board.Attack(5, 5);
        
        Assert.False(result.IsHit);
        Assert.Equal(CellState.Miss, board.Cells[5, 5].State);
    }

    [Fact]
    public void GameBoard_Attack_ReturnsSunk_WhenShipDestroyed()
    {
        var board = new GameBoard();
        var ship = new Ship("Destroyer", 2);
        board.PlaceShip(ship, 0, 0, ShipOrientation.Horizontal);
        
        board.Attack(0, 0);
        var result = board.Attack(0, 1);
        
        Assert.True(result.IsHit);
        Assert.True(result.IsSunk);
        Assert.Equal("Destroyer", result.SunkShipName);
        Assert.True(ship.IsSunk);
    }

    [Fact]
    public void GameBoard_Attack_ReturnsAlreadyAttacked_WhenCellPreviouslyAttacked()
    {
        var board = new GameBoard();
        board.Attack(0, 0);
        
        var result = board.Attack(0, 0);
        
        Assert.True(result.AlreadyAttacked);
    }

    [Fact]
    public void GameBoard_Attack_ThrowsException_WhenOutOfBounds()
    {
        var board = new GameBoard();
        
        Assert.Throws<ArgumentOutOfRangeException>(() => board.Attack(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => board.Attack(0, 10));
    }

    [Fact]
    public void GameBoard_AllShipsSunk_ReturnsFalse_WhenNoShips()
    {
        var board = new GameBoard();
        
        Assert.False(board.AllShipsSunk());
    }

    [Fact]
    public void GameBoard_AllShipsSunk_ReturnsFalse_WhenShipsRemaining()
    {
        var board = new GameBoard();
        var ship = new Ship("Destroyer", 2);
        board.PlaceShip(ship, 0, 0, ShipOrientation.Horizontal);
        board.Attack(0, 0);
        
        Assert.False(board.AllShipsSunk());
    }

    [Fact]
    public void GameBoard_AllShipsSunk_ReturnsTrue_WhenAllSunk()
    {
        var board = new GameBoard();
        var ship = new Ship("Destroyer", 2);
        board.PlaceShip(ship, 0, 0, ShipOrientation.Horizontal);
        board.Attack(0, 0);
        board.Attack(0, 1);
        
        Assert.True(board.AllShipsSunk());
    }

    [Fact]
    public void GameBoard_Reset_ClearsBoard()
    {
        var board = new GameBoard();
        var ship = new Ship("Destroyer", 2);
        board.PlaceShip(ship, 0, 0, ShipOrientation.Horizontal);
        board.Attack(0, 0);
        
        board.Reset();
        
        Assert.Empty(board.Ships);
        Assert.Equal(CellState.Empty, board.Cells[0, 0].State);
        Assert.False(board.Cells[0, 0].HasShip);
    }

    [Fact]
    public void GameBoard_GetUnattackedCells_ReturnsAllCells_Initially()
    {
        var board = new GameBoard();
        
        var cells = board.GetUnattackedCells();
        
        Assert.Equal(100, cells.Count);
    }

    [Fact]
    public void GameBoard_GetUnattackedCells_ExcludesAttackedCells()
    {
        var board = new GameBoard();
        board.Attack(0, 0);
        board.Attack(5, 5);
        
        var cells = board.GetUnattackedCells();
        
        Assert.Equal(98, cells.Count);
        Assert.DoesNotContain(cells, c => c.Row == 0 && c.Column == 0);
        Assert.DoesNotContain(cells, c => c.Row == 5 && c.Column == 5);
    }
}

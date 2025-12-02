using MauiBattleship.Models;
using MauiBattleship.Services;
using Xunit;

namespace MauiBattleship.Tests;

public class GameServiceTests
{
    [Fact]
    public void GameService_InitializesCorrectly()
    {
        var service = new GameService();
        
        Assert.NotNull(service.PlayerBoard);
        Assert.NotNull(service.ComputerBoard);
        Assert.NotNull(service.ComputerAI);
        Assert.Equal(GamePhase.NotStarted, service.CurrentPhase);
    }

    [Fact]
    public void GameService_CreateStandardFleet_Returns5Ships()
    {
        var ships = GameService.CreateStandardFleet();
        
        Assert.Equal(5, ships.Count);
        Assert.Contains(ships, s => s.Name == "Carrier" && s.Size == 5);
        Assert.Contains(ships, s => s.Name == "Battleship" && s.Size == 4);
        Assert.Contains(ships, s => s.Name == "Cruiser" && s.Size == 3);
        Assert.Contains(ships, s => s.Name == "Submarine" && s.Size == 3);
        Assert.Contains(ships, s => s.Name == "Destroyer" && s.Size == 2);
    }

    [Fact]
    public void GameService_StartNewGame_SetsUpGame()
    {
        var service = new GameService();
        
        service.StartNewGame();
        
        Assert.Equal(GamePhase.PlacingShips, service.CurrentPhase);
        Assert.Equal(5, service.ComputerBoard.Ships.Count);
        Assert.NotNull(service.GetNextShipToPlace());
    }

    [Fact]
    public void GameService_PlacePlayerShip_PlacesShip()
    {
        var service = new GameService();
        service.StartNewGame();
        var ship = service.GetNextShipToPlace();
        
        bool success = service.PlacePlayerShip(ship!, 0, 0, ShipOrientation.Horizontal);
        
        Assert.True(success);
        Assert.True(ship!.IsPlaced);
    }

    [Fact]
    public void GameService_PlaceAllShips_AdvancesToPlayerTurn()
    {
        var service = new GameService();
        service.StartNewGame();
        
        int row = 0;
        while (service.CurrentPhase == GamePhase.PlacingShips)
        {
            var ship = service.GetNextShipToPlace();
            if (ship == null) break;
            service.PlacePlayerShip(ship, row, 0, ShipOrientation.Horizontal);
            row++;
        }
        
        Assert.Equal(GamePhase.PlayerTurn, service.CurrentPhase);
    }

    [Fact]
    public void GameService_PlayerAttack_ReturnsResult()
    {
        var service = new GameService();
        service.StartNewGame();
        
        // Place all player ships
        int row = 0;
        while (service.CurrentPhase == GamePhase.PlacingShips)
        {
            var ship = service.GetNextShipToPlace();
            if (ship == null) break;
            service.PlacePlayerShip(ship, row, 0, ShipOrientation.Horizontal);
            row++;
        }
        
        var result = service.PlayerAttack(0, 0);
        
        Assert.NotNull(result);
        // After player attack, it should be computer's turn
        Assert.Equal(GamePhase.ComputerTurn, service.CurrentPhase);
    }

    [Fact]
    public void GameService_ComputerAttack_ReturnsResult()
    {
        var service = new GameService();
        service.StartNewGame();
        
        // Place all player ships
        int row = 0;
        while (service.CurrentPhase == GamePhase.PlacingShips)
        {
            var ship = service.GetNextShipToPlace();
            if (ship == null) break;
            service.PlacePlayerShip(ship, row, 0, ShipOrientation.Horizontal);
            row++;
        }
        
        // Player attacks first
        service.PlayerAttack(0, 0);
        
        // Now computer attacks
        var result = service.ComputerAttack();
        
        Assert.NotNull(result);
        Assert.Equal(GamePhase.PlayerTurn, service.CurrentPhase);
    }

    [Fact]
    public void GameService_GameEnds_WhenAllComputerShipsSunk()
    {
        var service = new GameService();
        service.StartNewGame();
        
        // Place all player ships
        int row = 0;
        while (service.CurrentPhase == GamePhase.PlacingShips)
        {
            var ship = service.GetNextShipToPlace();
            if (ship == null) break;
            service.PlacePlayerShip(ship, row, 0, ShipOrientation.Horizontal);
            row++;
        }
        
        // Attack all computer ships (we know their positions through the board)
        foreach (var ship in service.ComputerBoard.Ships)
        {
            foreach (var (r, c) in ship.GetCoordinates())
            {
                if (service.CurrentPhase == GamePhase.PlayerTurn)
                {
                    service.PlayerAttack(r, c);
                    
                    if (service.CurrentPhase == GamePhase.ComputerTurn)
                    {
                        service.ComputerAttack();
                    }
                }
            }
        }
        
        Assert.True(service.IsGameOver);
        Assert.True(service.PlayerWon);
    }
}

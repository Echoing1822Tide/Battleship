using MauiBattleship.Models;

namespace MauiBattleship.Core.Services;

public class FleetService
{
    public List<Ship> CreateStandardFleet()
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
}
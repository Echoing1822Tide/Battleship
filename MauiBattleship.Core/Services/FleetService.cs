using MauiBattleship.Models;

namespace MauiBattleship.Services;

public sealed class FleetService
{
    public List<Ship> CreateStandardFleet()
    {
        // Names match your image naming intent (used by UI messages + sprite mapping).
        return new List<Ship>
        {
            new Ship("Aircraft Carrier", 5),
            new Ship("Battleship",       4),
            new Ship("Cruiser",          3),
            new Ship("Submarine",        3),
            new Ship("Destroyer",        2),
        };
    }
}

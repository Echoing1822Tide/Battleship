using System.Collections.Generic;
using MauiBattleship.Core.GameCore.Abstractions;
using MauiBattleship.Core.GameCore.Interfaces;
using MauiBattleship.Core.GameCore.Ships;

namespace MauiBattleship.Core.Services;

public interface IFleetService
{
    IEnumerable<string> DemoInterfaces();
}

public sealed class FleetService : IFleetService
{
    public IEnumerable<string> DemoInterfaces()
    {
        var log = new List<string>();

        // Polymorphic collection of the abstract base type
        List<ShipBase> fleet = new()
        {
            new Destroyer("USS Farragut", 3),
            new Carrier("USS Nimitz", 5),
            new Submarine("USS Seawolf", 3)
        };

        foreach (var ship in fleet)
        {
            // Call shared interface method (IFireWeapon) polymorphically
            if (ship is IFireWeapon shooter)
                log.Add($"{ship}: {shooter.Fire()}");

            // Demonstrate the �special ability� on the one class that has it
            if (ship is ISilentRunner ghost)
                log.Add($"{ship}: {ghost.RunSilent()}");
        }

        return log;
    }
}

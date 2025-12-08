using MauiBattleship.Core.GameCore.Abstractions;
using MauiBattleship.Core.GameCore.Interfaces;

namespace MauiBattleship.Core.GameCore.Ships;

public sealed class Destroyer : ShipBase, IFireWeapon
{
    public Destroyer() : base() { }
    public Destroyer(string name, int size) : base(name, size) { }

    public string Fire() => $"{Name}: Broadside cannons � BOOM!";
}

using MauiBattleship.Core.GameCore.Abstractions;
using MauiBattleship.Core.GameCore.Interfaces;

namespace MauiBattleship.Core.GameCore.Ships;

public sealed class Carrier : ShipBase, IFireWeapon
{
    public Carrier() : base() { }
    public Carrier(string name, int size) : base(name, size) { }

    public string Fire() => $"{Name}: Launching strike group. Wheels up!";
}

using MauiBattleship.Core.GameCore.Abstractions;
using MauiBattleship.Core.GameCore.Interfaces;

namespace MauiBattleship.Core.GameCore.Ships;

public sealed class Submarine : ShipBase, IFireWeapon, ISilentRunner
{
    public Submarine() : base() { }
    public Submarine(string name, int size) : base(name, size) { }

    public string Fire() => $"{Name}: Torpedo away — quiet run!";
    public string RunSilent() => $"{Name}: Running silent… sonar contacts minimal.";
}

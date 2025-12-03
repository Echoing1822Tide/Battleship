using System;

namespace MauiBattleship.Core.GameCore.Abstractions;

public abstract class ShipBase
{
    public string Name { get; protected set; }
    public int Size { get; protected set; }
    public int Health { get; protected set; }

    protected ShipBase()
    {
        Name = "Unnamed";
        Size = 0;
        Health = 0;
    }

    protected ShipBase(string name, int size)
    {
        Name = name;
        Size = size;
        Health = size; // start with HP equal to size, nice/simple default
    }

    public override string ToString() =>
        $"{GetType().Name} \"{Name}\" (Size {Size}, HP {Health})";
}

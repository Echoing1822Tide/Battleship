namespace MauiBattleship.Models;

public readonly record struct ShotInfo(
    int Row,
    int Col,
    AttackResult Result,
    bool IsHit,
    string? ShipName,
    string Message
);

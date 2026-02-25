namespace Battleship.GameCore;

public sealed class ShotInfo
{
    public int Row { get; }
    public int Col { get; }
    public AttackResult Result { get; }
    public bool IsHit { get; }
    public string? SunkShipName { get; }
    public string Message { get; }

    public ShotInfo(
        int row,
        int col,
        AttackResult result,
        bool isHit,
        string? sunkShipName,
        string message)
    {
        Row = row;
        Col = col;
        Result = result;
        IsHit = isHit;
        SunkShipName = sunkShipName;
        Message = message;
    }
}

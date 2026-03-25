using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct PlayerMartialArtModel
{
    public int MartialArtId;
    public string? Code;
    public string? Icon;
    public string? Name;
    public int Quality;
    public string? Category;
    public int CurrentStage;
    public long CurrentExp;
    public int MaxStage;
    public double QiAbsorptionRate;
    public bool IsActive;
}

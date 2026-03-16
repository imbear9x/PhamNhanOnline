using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct PotentialUpgradePreviewModel
{
    public int TargetStat;
    public int NextUpgradeCount;
    public int TierIndex;
    public int PotentialCost;
    public double StatGain;
    public bool IsAvailable;
    public bool CanUpgrade;
}

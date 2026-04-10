using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct PillRecipeMasteryStageModel
{
    public int StageId;
    public int RequiredTotalCraftCount;
    public double SuccessRateBonus;
    public bool IsReached;
}

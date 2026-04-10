using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct PillRecipeInputModel
{
    public int InputId;
    public ItemTemplateSummaryModel RequiredItem;
    public int RequiredQuantity;
    public int ConsumeMode;
    public bool IsOptional;
    public double SuccessRateBonus;
    public double MutationBonusRate;
    public int RequiredHerbMaturity;
}

using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct AlchemyCraftPreviewModel
{
    public int PillRecipeTemplateId;
    public int RequestedCraftCount;
    public int MaxCraftableCount;
    public bool CanCraft;
    public string? FailureReason;
    public double EffectiveSuccessRate;
    public double BoostedSuccessRate;
    public double EffectiveMutationRate;
    public double BoostedMutationRate;
    public int BoostedCraftCount;
    public List<AlchemyOptionalInputSelectionModel>? AppliedOptionalInputs;
    public List<AlchemyConsumedItemModel>? ConsumedItems;
}

using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct AlchemyCraftPreviewModel
{
    public int PillRecipeTemplateId;
    public bool CanCraft;
    public string? FailureReason;
    public double EffectiveSuccessRate;
    public double EffectiveMutationRate;
    public List<int>? AppliedOptionalInputIds;
    public List<AlchemyConsumedItemModel>? ConsumedItems;
}

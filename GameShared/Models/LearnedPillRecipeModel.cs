using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct LearnedPillRecipeModel
{
    public int PillRecipeTemplateId;
    public string? Code;
    public string? Name;
    public string? Description;
    public ItemTemplateSummaryModel ResultPill;
    public long CraftDurationSeconds;
    public double BaseSuccessRate;
    public double? SuccessRateCap;
    public double MutationRate;
    public double MutationRateCap;
    public int TotalCraftCount;
    public double CurrentSuccessRateBonus;
    public long? LearnedUnixMs;
}

using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct PracticeCompletionResultModel
{
    public long PracticeSessionId;
    public int PracticeType;
    public bool Success;
    public string? Title;
    public string? Message;
    public ItemTemplateSummaryModel? DisplayItem;
    public PracticeRewardItemModel? PrimaryReward;
    public List<PracticeRewardItemModel>? Rewards;
    public long? CompletedUnixMs;
}

using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct AlchemyPracticeStatusModel
{
    public PracticeSessionModel? Session;
    public PillRecipeDetailModel? Recipe;
    public List<AlchemyConsumedItemModel>? ConsumedItems;
    public PracticeCompletionResultModel? PendingResult;
}

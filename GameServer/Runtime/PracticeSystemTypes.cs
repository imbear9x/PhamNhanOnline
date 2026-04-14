namespace GameServer.Runtime;

public enum PracticeType
{
    Cultivation = 1,
    Alchemy = 2,
    Talisman = 3,
    Artifact = 4
}

public enum PracticeSessionState
{
    Active = 1,
    Paused = 2,
    ResultPendingAcknowledgement = 3,
    Completed = 4,
    Cancelled = 5
}

public sealed record PracticeConsumedEntry(
    long PlayerItemId,
    int ItemTemplateId,
    int Quantity);

public sealed record PracticeOptionalInputEntry(
    int InputId,
    int AppliedCount);

public sealed record PracticeRewardEntry(
    int ItemTemplateId,
    int Quantity);

public sealed record PracticeSessionPayload(
    int DefinitionId,
    int RequestedCraftCount,
    IReadOnlyList<PracticeOptionalInputEntry> SelectedOptionalInputs,
    IReadOnlyList<PracticeConsumedEntry> ConsumedEntries);

public sealed record PracticeCompletionPayload(
    bool Success,
    int RequestedCraftCount,
    int SuccessCount,
    int FailedCount,
    string Title,
    string Message,
    int? DisplayItemTemplateId,
    IReadOnlyList<PracticeRewardEntry> Rewards);

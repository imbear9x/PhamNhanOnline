namespace GameServer.Randomness;

public readonly record struct GameRandomContext(
    Guid? CharacterId = null,
    IReadOnlyCollection<string>? ContextTags = null);

public readonly record struct GameRandomOptions(
    double? Fortune = null);

public sealed record GameRandomEvaluatedEntry(
    string EntryId,
    int BaseChancePartsPerMillion,
    int EffectiveChancePartsPerMillion,
    int BonusChancePartsPerMillion,
    bool IsNone,
    IReadOnlyList<string> Tags);

public sealed record GameRandomTablePreview(
    string TableId,
    GameRandomTableMode Mode,
    double? Fortune,
    int AppliedFortuneBonusPartsPerMillion,
    IReadOnlyList<GameRandomEvaluatedEntry> EffectiveEntries);

public sealed record GameRandomRollResult(
    GameRandomTablePreview Table,
    int RollValue,
    GameRandomEvaluatedEntry SelectedEntry);

public readonly record struct GameRandomChanceCheckResult(
    int BaseChancePartsPerMillion,
    int EffectiveChancePartsPerMillion,
    int AppliedFortuneBonusPartsPerMillion,
    int RollValue,
    bool Success);

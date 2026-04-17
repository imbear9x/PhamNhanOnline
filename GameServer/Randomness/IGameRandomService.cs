namespace GameServer.Randomness;

public interface IGameRandomService
{
    GameRandomTablePreview PreviewTable(string tableId, GameRandomContext context = default, GameRandomOptions options = default);
    GameRandomRollResult Roll(string tableId, GameRandomContext context = default, GameRandomOptions options = default);
    GameRandomChanceCheckResult CheckChance(
        int baseChancePartsPerMillion,
        GameRandomContext context = default,
        GameRandomOptions options = default,
        GameRandomLuckModifierConfig? modifier = null,
        IReadOnlyCollection<string>? entryTags = null);
}

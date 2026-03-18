using GameServer.Randomness;

namespace GameServer.Runtime;

public sealed class MartialArtProgressionService
{
    private readonly CombatDefinitionCatalog _combatDefinitions;
    private readonly IGameRandomService _randomService;

    public MartialArtProgressionService(CombatDefinitionCatalog combatDefinitions, IGameRandomService randomService)
    {
        _combatDefinitions = combatDefinitions;
        _randomService = randomService;
    }

    public MartialArtExpGainResult AddExp(PlayerMartialArtProgressState progress, long gainedExp)
    {
        if (gainedExp <= 0)
            return MartialArtExpGainResult.Unchanged(progress, Array.Empty<MartialArtSkillUnlockDefinition>());

        if (!_combatDefinitions.TryGetMartialArt(progress.MartialArtId, out var martialArt))
            throw new InvalidOperationException($"Martial art {progress.MartialArtId} was not found.");

        var currentStage = Math.Max(1, progress.CurrentStage);
        var currentExp = Math.Max(0L, progress.CurrentExp) + gainedExp;
        while (currentStage < martialArt.MaxStage)
        {
            var stageDefinition = martialArt.Stages.FirstOrDefault(x => x.StageLevel == currentStage);
            if (stageDefinition is null || stageDefinition.ExpRequired <= 0)
                break;

            if (currentExp < stageDefinition.ExpRequired)
                break;

            if (stageDefinition.IsBottleneck)
                break;

            currentExp -= stageDefinition.ExpRequired;
            currentStage++;
        }

        var updated = progress with
        {
            CurrentStage = currentStage,
            CurrentExp = currentExp
        };
        return new MartialArtExpGainResult(
            progress,
            updated,
            GetUnlockedSkills(martialArt, progress.CurrentStage + 1, currentStage));
    }

    public MartialArtBreakthroughResult TryBreakthrough(PlayerMartialArtProgressState progress)
    {
        if (!_combatDefinitions.TryGetMartialArt(progress.MartialArtId, out var martialArt))
            throw new InvalidOperationException($"Martial art {progress.MartialArtId} was not found.");

        var currentStage = Math.Max(1, progress.CurrentStage);
        var currentStageDefinition = martialArt.Stages.FirstOrDefault(x => x.StageLevel == currentStage);
        if (currentStageDefinition is null || !currentStageDefinition.IsBottleneck)
            return MartialArtBreakthroughResult.NotReady(progress);

        if (progress.CurrentExp < currentStageDefinition.ExpRequired)
            return MartialArtBreakthroughResult.NotReady(progress);

        var chancePartsPerMillion = ResolveChancePartsPerMillion(currentStageDefinition.BreakthroughBaseRate);
        var chanceCheck = _randomService.CheckChance(chancePartsPerMillion);
        if (!chanceCheck.Success)
        {
            var remainingExp = Math.Max(0L, progress.CurrentExp - currentStageDefinition.BreakthroughExpPenalty);
            return MartialArtBreakthroughResult.Failed(progress with { CurrentExp = remainingExp }, chancePartsPerMillion);
        }

        var updated = progress with
        {
            CurrentStage = Math.Min(currentStage + 1, martialArt.MaxStage),
            CurrentExp = Math.Max(0L, progress.CurrentExp - currentStageDefinition.ExpRequired)
        };
        return MartialArtBreakthroughResult.Succeeded(
            updated,
            chancePartsPerMillion,
            GetUnlockedSkills(martialArt, currentStage + 1, updated.CurrentStage));
    }

    public FlatStatBonusBundle BuildFlatStatBonusBundle(IEnumerable<PlayerMartialArtProgressState> progressStates)
    {
        var total = FlatStatBonusBundle.Empty;
        foreach (var progress in progressStates)
        {
            if (!_combatDefinitions.TryGetMartialArt(progress.MartialArtId, out var martialArt))
                continue;

            foreach (var stage in martialArt.Stages)
            {
                if (stage.StageLevel > progress.CurrentStage)
                    break;

                foreach (var bonus in stage.StatBonuses)
                {
                    if (bonus.ValueType != CombatValueType.Flat)
                        continue;

                    total = total.Add(bonus.StatType, bonus.Value);
                }
            }
        }

        return total;
    }

    private static IReadOnlyList<MartialArtSkillUnlockDefinition> GetUnlockedSkills(
        MartialArtDefinition martialArt,
        int fromStageInclusive,
        int toStageInclusive)
    {
        if (toStageInclusive < fromStageInclusive)
            return Array.Empty<MartialArtSkillUnlockDefinition>();

        return martialArt.SkillUnlocks
            .Where(x => x.UnlockStage >= fromStageInclusive && x.UnlockStage <= toStageInclusive)
            .OrderBy(x => x.UnlockStage)
            .ThenBy(x => x.Id)
            .ToArray();
    }

    private static int ResolveChancePartsPerMillion(double? rawChance)
    {
        if (!rawChance.HasValue || rawChance.Value <= 0)
            return 0;

        var normalizedChance = rawChance.Value > 1d
            ? rawChance.Value / 100d
            : rawChance.Value;
        normalizedChance = Math.Clamp(normalizedChance, 0d, 1d);
        return (int)Math.Round(normalizedChance * 1_000_000d, MidpointRounding.AwayFromZero);
    }
}

public readonly record struct MartialArtExpGainResult(
    PlayerMartialArtProgressState PreviousState,
    PlayerMartialArtProgressState UpdatedState,
    IReadOnlyList<MartialArtSkillUnlockDefinition> NewlyUnlockedSkills)
{
    public static MartialArtExpGainResult Unchanged(
        PlayerMartialArtProgressState state,
        IReadOnlyList<MartialArtSkillUnlockDefinition> unlockedSkills)
    {
        return new MartialArtExpGainResult(state, state, unlockedSkills);
    }
}

public readonly record struct MartialArtBreakthroughResult(
    bool Success,
    bool IsReady,
    PlayerMartialArtProgressState UpdatedState,
    int ChancePartsPerMillion,
    IReadOnlyList<MartialArtSkillUnlockDefinition> NewlyUnlockedSkills)
{
    public static MartialArtBreakthroughResult NotReady(PlayerMartialArtProgressState state) =>
        new(false, false, state, 0, Array.Empty<MartialArtSkillUnlockDefinition>());

    public static MartialArtBreakthroughResult Failed(PlayerMartialArtProgressState state, int chancePartsPerMillion) =>
        new(false, true, state, chancePartsPerMillion, Array.Empty<MartialArtSkillUnlockDefinition>());

    public static MartialArtBreakthroughResult Succeeded(
        PlayerMartialArtProgressState state,
        int chancePartsPerMillion,
        IReadOnlyList<MartialArtSkillUnlockDefinition> unlockedSkills) =>
        new(true, true, state, chancePartsPerMillion, unlockedSkills);
}

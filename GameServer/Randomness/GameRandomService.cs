namespace GameServer.Randomness;

public sealed class GameRandomService : IGameRandomService
{
    private readonly IRandomNumberProvider _random;
    private readonly IReadOnlyDictionary<string, CompiledTable> _tables;

    public GameRandomService(GameRandomConfig config, IRandomNumberProvider random)
    {
        _random = random;
        _tables = BuildTables(config);
    }

    public GameRandomTablePreview PreviewTable(string tableId, GameRandomContext context = default, GameRandomOptions options = default)
    {
        var table = GetTable(tableId);
        return table.Mode switch
        {
            GameRandomTableMode.Exclusive => BuildExclusivePreview(table, context, options),
            _ => throw new InvalidOperationException($"Unsupported random table mode: {table.Mode}")
        };
    }

    public GameRandomRollResult Roll(string tableId, GameRandomContext context = default, GameRandomOptions options = default)
    {
        var preview = PreviewTable(tableId, context, options);
        var rollValue = _random.NextInt(GameRandomConfig.ChanceScale);
        var selectedEntry = preview.EffectiveEntries[^1];
        var cumulative = 0;
        for (var i = 0; i < preview.EffectiveEntries.Count; i++)
        {
            cumulative += preview.EffectiveEntries[i].EffectiveChancePartsPerMillion;
            if (rollValue < cumulative)
            {
                selectedEntry = preview.EffectiveEntries[i];
                break;
            }
        }

        return new GameRandomRollResult(preview, rollValue, selectedEntry);
    }

    public GameRandomChanceCheckResult CheckChance(
        int baseChancePartsPerMillion,
        GameRandomContext context = default,
        GameRandomOptions options = default,
        GameRandomFortuneModifierConfig? modifier = null,
        IReadOnlyCollection<string>? entryTags = null)
    {
        ValidateChance(baseChancePartsPerMillion, nameof(baseChancePartsPerMillion));

        var effectiveChance = baseChancePartsPerMillion;
        var appliedBonus = 0;
        var activeModifier = modifier ?? GameRandomFortuneModifierConfig.Disabled;
        if (ShouldApplyFortune(activeModifier, entryTags))
        {
            var requestedBonus = ResolveFortuneBonusPartsPerMillion(options.Fortune, activeModifier);
            appliedBonus = Math.Min(requestedBonus, GameRandomConfig.ChanceScale - baseChancePartsPerMillion);
            effectiveChance += appliedBonus;
        }

        var rollValue = _random.NextInt(GameRandomConfig.ChanceScale);
        return new GameRandomChanceCheckResult(
            baseChancePartsPerMillion,
            effectiveChance,
            appliedBonus,
            rollValue,
            rollValue < effectiveChance);
    }

    private static IReadOnlyDictionary<string, CompiledTable> BuildTables(GameRandomConfig config)
    {
        var result = new Dictionary<string, CompiledTable>(StringComparer.OrdinalIgnoreCase);
        foreach (var tableConfig in config.Tables)
        {
            if (string.IsNullOrWhiteSpace(tableConfig.TableId))
                throw new InvalidOperationException("Random table id cannot be empty.");
            if (result.ContainsKey(tableConfig.TableId))
                throw new InvalidOperationException($"Duplicate random table id: {tableConfig.TableId}");

            result[tableConfig.TableId] = CompileTable(tableConfig);
        }

        return result;
    }

    private static CompiledTable CompileTable(GameRandomTableConfig tableConfig)
    {
        if (tableConfig.Entries.Count == 0)
            throw new InvalidOperationException($"Random table '{tableConfig.TableId}' must have at least one entry.");

        var entries = new List<CompiledEntry>(tableConfig.Entries.Count + 1);
        var entryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var totalChance = 0;
        var explicitNoneIndex = -1;

        for (var i = 0; i < tableConfig.Entries.Count; i++)
        {
            var entryConfig = tableConfig.Entries[i];
            if (string.IsNullOrWhiteSpace(entryConfig.EntryId))
                throw new InvalidOperationException($"Random table '{tableConfig.TableId}' has an entry with an empty id.");
            if (!entryIds.Add(entryConfig.EntryId))
                throw new InvalidOperationException($"Random table '{tableConfig.TableId}' has duplicate entry id '{entryConfig.EntryId}'.");

            ValidateChance(entryConfig.ChancePartsPerMillion, $"Entries[{i}].ChancePartsPerMillion");
            totalChance = checked(totalChance + entryConfig.ChancePartsPerMillion);
            if (totalChance > GameRandomConfig.ChanceScale)
                throw new InvalidOperationException($"Random table '{tableConfig.TableId}' exceeds 100% total chance.");

            var isNone = entryConfig.IsNone ||
                         string.Equals(entryConfig.EntryId, tableConfig.FortuneModifier.NoneEntryId, StringComparison.OrdinalIgnoreCase);
            if (isNone)
            {
                if (explicitNoneIndex >= 0)
                    throw new InvalidOperationException($"Random table '{tableConfig.TableId}' has more than one none entry.");

                explicitNoneIndex = entries.Count;
            }

            entries.Add(new CompiledEntry(
                entryConfig.EntryId,
                entryConfig.ChancePartsPerMillion,
                isNone,
                entryConfig.Tags
                    .Where(static tag => !string.IsNullOrWhiteSpace(tag))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()));
        }

        if (tableConfig.Mode == GameRandomTableMode.Exclusive)
        {
            var remainingChance = GameRandomConfig.ChanceScale - totalChance;
            if (remainingChance > 0)
            {
                if (explicitNoneIndex >= 0)
                {
                    var explicitNone = entries[explicitNoneIndex];
                    entries[explicitNoneIndex] = explicitNone with
                    {
                        BaseChancePartsPerMillion = checked(explicitNone.BaseChancePartsPerMillion + remainingChance)
                    };
                }
                else
                {
                    entries.Add(new CompiledEntry(
                        tableConfig.FortuneModifier.NoneEntryId,
                        remainingChance,
                        true,
                        ["none"]));
                }
            }
        }

        return new CompiledTable(
            tableConfig.TableId,
            tableConfig.Mode,
            tableConfig.FortuneModifier,
            entries);
    }

    private GameRandomTablePreview BuildExclusivePreview(CompiledTable table, GameRandomContext context, GameRandomOptions options)
    {
        var entries = table.Entries
            .Select(static entry => new MutableEvaluatedEntry(entry))
            .ToArray();

        var noneEntry = entries.FirstOrDefault(static entry => entry.IsNone);
        var eligibleEntries = entries
            .Where(entry => !entry.IsNone && ShouldApplyFortune(table.FortuneModifier, entry.Tags))
            .ToArray();

        var appliedBonus = 0;
        if (noneEntry is not null && eligibleEntries.Length > 0)
        {
            var requestedBonus = ResolveFortuneBonusPartsPerMillion(options.Fortune, table.FortuneModifier);
            appliedBonus = Math.Min(requestedBonus, noneEntry.EffectiveChancePartsPerMillion);
            if (appliedBonus > 0)
            {
                DistributeBonusAcrossEntries(eligibleEntries, appliedBonus);
                noneEntry.EffectiveChancePartsPerMillion -= appliedBonus;
                noneEntry.BonusChancePartsPerMillion -= appliedBonus;
            }
        }

        var totalEffectiveChance = entries.Sum(static entry => entry.EffectiveChancePartsPerMillion);
        if (totalEffectiveChance != GameRandomConfig.ChanceScale)
            throw new InvalidOperationException($"Random table '{table.TableId}' does not normalize to 100% after modifiers.");

        return new GameRandomTablePreview(
            table.TableId,
            table.Mode,
            options.Fortune,
            appliedBonus,
            entries.Select(static entry => entry.ToResult()).ToArray());
    }

    private static void DistributeBonusAcrossEntries(MutableEvaluatedEntry[] eligibleEntries, int appliedBonus)
    {
        var eligibleBaseTotal = eligibleEntries.Sum(static entry => entry.BaseChancePartsPerMillion);
        if (eligibleBaseTotal <= 0 || appliedBonus <= 0)
            return;

        var distributions = new List<EntryBonusDistribution>(eligibleEntries.Length);
        var assignedBonus = 0;
        for (var i = 0; i < eligibleEntries.Length; i++)
        {
            var numerator = (long)appliedBonus * eligibleEntries[i].BaseChancePartsPerMillion;
            var baseBonus = (int)(numerator / eligibleBaseTotal);
            var remainder = numerator % eligibleBaseTotal;
            distributions.Add(new EntryBonusDistribution(i, baseBonus, remainder));
            assignedBonus += baseBonus;
        }

        var remainingBonus = appliedBonus - assignedBonus;
        if (remainingBonus > 0)
        {
            foreach (var distribution in distributions
                         .OrderByDescending(static x => x.Remainder)
                         .ThenBy(static x => x.Index))
            {
                if (remainingBonus <= 0)
                    break;

                distributions[distribution.Index] = distribution with
                {
                    Bonus = distribution.Bonus + 1
                };
                remainingBonus--;
            }
        }

        for (var i = 0; i < distributions.Count; i++)
        {
            var bonus = distributions[i].Bonus;
            if (bonus <= 0)
                continue;

            eligibleEntries[i].EffectiveChancePartsPerMillion += bonus;
            eligibleEntries[i].BonusChancePartsPerMillion += bonus;
        }
    }

    private static bool ShouldApplyFortune(GameRandomFortuneModifierConfig modifier, IReadOnlyCollection<string>? entryTags)
    {
        if (!modifier.Enabled)
            return false;

        if (modifier.ApplyToEntryTags.Count == 0)
            return true;

        if (entryTags is null || entryTags.Count == 0)
            return false;

        foreach (var tag in entryTags)
        {
            if (modifier.ApplyToEntryTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static int ResolveFortuneBonusPartsPerMillion(double? fortune, GameRandomFortuneModifierConfig modifier)
    {
        if (!fortune.HasValue || !modifier.Enabled || modifier.BonusPartsPerMillionPerFortunePoint <= 0 || fortune.Value <= 0)
            return 0;

        var maxBonus = modifier.MaxBonusPartsPerMillion <= 0
            ? GameRandomConfig.ChanceScale
            : Math.Min(GameRandomConfig.ChanceScale, modifier.MaxBonusPartsPerMillion);
        var requestedBonus = decimal.Truncate((decimal)fortune.Value * modifier.BonusPartsPerMillionPerFortunePoint);
        if (requestedBonus <= 0)
            return 0;

        if (requestedBonus >= maxBonus)
            return maxBonus;

        return decimal.ToInt32(requestedBonus);
    }

    private CompiledTable GetTable(string tableId)
    {
        if (string.IsNullOrWhiteSpace(tableId))
            throw new ArgumentException("Random table id cannot be empty.", nameof(tableId));
        if (!_tables.TryGetValue(tableId, out var table))
            throw new KeyNotFoundException($"Random table '{tableId}' was not found.");

        return table;
    }

    private static void ValidateChance(int value, string paramName)
    {
        if (value < 0 || value > GameRandomConfig.ChanceScale)
            throw new InvalidOperationException($"{paramName} must be between 0 and {GameRandomConfig.ChanceScale}.");
    }

    private sealed record CompiledTable(
        string TableId,
        GameRandomTableMode Mode,
        GameRandomFortuneModifierConfig FortuneModifier,
        IReadOnlyList<CompiledEntry> Entries);

    private sealed record CompiledEntry(
        string EntryId,
        int BaseChancePartsPerMillion,
        bool IsNone,
        IReadOnlyList<string> Tags);

    private sealed class MutableEvaluatedEntry
    {
        public MutableEvaluatedEntry(CompiledEntry entry)
        {
            EntryId = entry.EntryId;
            BaseChancePartsPerMillion = entry.BaseChancePartsPerMillion;
            EffectiveChancePartsPerMillion = entry.BaseChancePartsPerMillion;
            BonusChancePartsPerMillion = 0;
            IsNone = entry.IsNone;
            Tags = entry.Tags;
        }

        public string EntryId { get; }
        public int BaseChancePartsPerMillion { get; }
        public int EffectiveChancePartsPerMillion { get; set; }
        public int BonusChancePartsPerMillion { get; set; }
        public bool IsNone { get; }
        public IReadOnlyList<string> Tags { get; }

        public GameRandomEvaluatedEntry ToResult()
        {
            return new GameRandomEvaluatedEntry(
                EntryId,
                BaseChancePartsPerMillion,
                EffectiveChancePartsPerMillion,
                BonusChancePartsPerMillion,
                IsNone,
                Tags);
        }
    }

    private readonly record struct EntryBonusDistribution(
        int Index,
        int Bonus,
        long Remainder);
}

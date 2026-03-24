using GameServer.DTO;
using GameServer.Repositories;
using GameServer.Runtime;
using GameServer.World;

namespace GameServer.Services;

public sealed class CharacterFinalStatService
{
    private readonly EquipmentStatService _equipmentStatService;
    private readonly PlayerMartialArtRepository _playerMartialArts;
    private readonly CombatDefinitionCatalog _combatDefinitions;
    private readonly CharacterRuntimeService _characterRuntimeService;
    private readonly CharacterRuntimeCalculator _characterRuntimeCalculator;
    private readonly CharacterService _characterService;
    private readonly PotentialStatCatalog _potentialStatCatalog;

    public CharacterFinalStatService(
        EquipmentStatService equipmentStatService,
        PlayerMartialArtRepository playerMartialArts,
        CombatDefinitionCatalog combatDefinitions,
        CharacterRuntimeService characterRuntimeService,
        CharacterRuntimeCalculator characterRuntimeCalculator,
        CharacterService characterService,
        PotentialStatCatalog potentialStatCatalog)
    {
        _equipmentStatService = equipmentStatService;
        _playerMartialArts = playerMartialArts;
        _combatDefinitions = combatDefinitions;
        _characterRuntimeService = characterRuntimeService;
        _characterRuntimeCalculator = characterRuntimeCalculator;
        _characterService = characterService;
        _potentialStatCatalog = potentialStatCatalog;
    }

    public async Task<CharacterRuntimeSnapshot> ApplyAuthoritativeFinalStatsAsync(
        PlayerSession player,
        CancellationToken cancellationToken = default)
    {
        var currentSnapshot = player.RuntimeState.CaptureSnapshot();
        var updatedBaseStats = await BuildFinalStatsAsync(
            player.CharacterData.CharacterId,
            currentSnapshot.BaseStats,
            cancellationToken);

        if (HasMatchingFinalStats(currentSnapshot.BaseStats, updatedBaseStats))
            return currentSnapshot;

        var updatedSnapshot = _characterRuntimeService.ApplyBaseStatsMutation(player, _ => updatedBaseStats);
        await _characterService.UpdateCharacterRuntimeSnapshotAsync(
            updatedSnapshot.BaseStats,
            updatedSnapshot.CurrentState,
            cancellationToken);

        return updatedSnapshot;
    }

    public async Task<CharacterSnapshotDto> ApplyFinalStatsToSnapshotAsync(
        CharacterSnapshotDto snapshot,
        CancellationToken cancellationToken = default)
    {
        if (snapshot.BaseStats is null || snapshot.CurrentState is null)
            return snapshot;

        var updatedBaseStats = await BuildFinalStatsAsync(
            snapshot.Character.CharacterId,
            snapshot.BaseStats,
            cancellationToken);
        var updatedCurrentState = _characterRuntimeCalculator.ClampCurrentStateToBaseStats(updatedBaseStats, snapshot.CurrentState);

        return snapshot with
        {
            BaseStats = updatedBaseStats,
            CurrentState = updatedCurrentState
        };
    }

    public async Task<CharacterBaseStatsDto> BuildFinalStatsAsync(
        Guid characterId,
        CharacterBaseStatsDto baseStats,
        CancellationToken cancellationToken = default)
    {
        var potentialBonuses = _potentialStatCatalog.ResolveAllocatedBonuses(baseStats);
        var equipmentModifiers = await _equipmentStatService.BuildEquipmentStatModifiersAsync(characterId, cancellationToken);
        var martialArtModifiers = await BuildActiveMartialArtModifiersAsync(characterId, baseStats.ActiveMartialArtId, cancellationToken);

        var combinedModifiers = MergeModifierBundles(equipmentModifiers, martialArtModifiers);

        var hp = checked(baseStats.GetRawHp() + potentialBonuses.Hp + ResolvePercentIntBonus(baseStats.GetRawHp(), combinedModifiers, CharacterStatType.Hp) + ResolveFlatIntBonus(combinedModifiers, CharacterStatType.Hp));
        var mp = checked(baseStats.GetRawMp() + potentialBonuses.Mp + ResolvePercentIntBonus(baseStats.GetRawMp(), combinedModifiers, CharacterStatType.Mp) + ResolveFlatIntBonus(combinedModifiers, CharacterStatType.Mp));
        var attack = checked(baseStats.GetRawAttack() + potentialBonuses.Attack + ResolvePercentIntBonus(baseStats.GetRawAttack(), combinedModifiers, CharacterStatType.Attack) + ResolveFlatIntBonus(combinedModifiers, CharacterStatType.Attack));
        var speed = checked(baseStats.GetRawSpeed() + potentialBonuses.Speed + ResolvePercentIntBonus(baseStats.GetRawSpeed(), combinedModifiers, CharacterStatType.Speed) + ResolveFlatIntBonus(combinedModifiers, CharacterStatType.Speed));
        var spiritualSense = checked(baseStats.GetRawSpiritualSense() + potentialBonuses.SpiritualSense + ResolvePercentIntBonus(baseStats.GetRawSpiritualSense(), combinedModifiers, CharacterStatType.SpiritualSense) + ResolveFlatIntBonus(combinedModifiers, CharacterStatType.SpiritualSense));
        var stamina = baseStats.GetRawStamina();
        var fortune = baseStats.GetRawFortune() + potentialBonuses.Fortune + ResolvePercentFortuneBonus(baseStats.GetRawFortune(), combinedModifiers) + ResolveFlatFortuneBonus(combinedModifiers);

        return await _characterService.EnrichBaseStatsAsync(baseStats with
        {
            PotentialHpBonus = potentialBonuses.Hp,
            PotentialMpBonus = potentialBonuses.Mp,
            PotentialAttackBonus = potentialBonuses.Attack,
            PotentialSpeedBonus = potentialBonuses.Speed,
            PotentialSpiritualSenseBonus = potentialBonuses.SpiritualSense,
            PotentialFortuneBonus = potentialBonuses.Fortune,
            FinalHp = hp,
            FinalMp = mp,
            FinalAttack = attack,
            FinalSpeed = speed,
            FinalSpiritualSense = spiritualSense,
            FinalStamina = stamina,
            FinalFortune = fortune
        }, cancellationToken);
    }

    private async Task<ItemStatModifierBundle> BuildActiveMartialArtModifiersAsync(
        Guid characterId,
        int? activeMartialArtId,
        CancellationToken cancellationToken)
    {
        if (!activeMartialArtId.HasValue || activeMartialArtId.Value <= 0)
            return ItemStatModifierBundle.Empty;

        var progress = await _playerMartialArts.GetByPlayerAndMartialArtAsync(characterId, activeMartialArtId.Value, cancellationToken);
        if (progress is null)
            return ItemStatModifierBundle.Empty;

        if (!_combatDefinitions.TryGetMartialArt(activeMartialArtId.Value, out var martialArt))
            return ItemStatModifierBundle.Empty;

        var flat = new Dictionary<CharacterStatType, decimal>();
        var percent = new Dictionary<CharacterStatType, decimal>();

        foreach (var stage in martialArt.Stages)
        {
            if (stage.StageLevel > progress.CurrentStage)
                break;

            ApplyModifiers(flat, percent, stage.StatBonuses);
        }

        return new ItemStatModifierBundle(flat, percent);
    }

    private static ItemStatModifierBundle MergeModifierBundles(ItemStatModifierBundle left, ItemStatModifierBundle right)
    {
        if (ReferenceEquals(left, ItemStatModifierBundle.Empty))
            return right;
        if (ReferenceEquals(right, ItemStatModifierBundle.Empty))
            return left;

        var flat = new Dictionary<CharacterStatType, decimal>();
        var percent = new Dictionary<CharacterStatType, decimal>();

        MergeDictionary(flat, left.FlatValues);
        MergeDictionary(flat, right.FlatValues);
        MergeDictionary(percent, left.PercentValues);
        MergeDictionary(percent, right.PercentValues);

        return new ItemStatModifierBundle(flat, percent);
    }

    private static void MergeDictionary(
        IDictionary<CharacterStatType, decimal> target,
        IReadOnlyDictionary<CharacterStatType, decimal> source)
    {
        foreach (var pair in source)
        {
            target[pair.Key] = target.TryGetValue(pair.Key, out var current)
                ? current + pair.Value
                : pair.Value;
        }
    }

    private static void ApplyModifiers(
        IDictionary<CharacterStatType, decimal> flat,
        IDictionary<CharacterStatType, decimal> percent,
        IEnumerable<CharacterStatBonusValue> modifiers)
    {
        foreach (var modifier in modifiers)
        {
            if (modifier.StatType == CharacterStatType.None)
                continue;

            var target = modifier.ValueType == CombatValueType.Percent ? percent : flat;
            target[modifier.StatType] = target.TryGetValue(modifier.StatType, out var current)
                ? current + modifier.Value
                : modifier.Value;
        }
    }

    private static int ResolveFlatIntBonus(ItemStatModifierBundle modifiers, CharacterStatType statType)
    {
        if (!modifiers.FlatValues.TryGetValue(statType, out var value))
            return 0;

        return decimal.ToInt32(decimal.Truncate(value));
    }

    private static double ResolveFlatFortuneBonus(ItemStatModifierBundle modifiers)
    {
        return modifiers.FlatValues.TryGetValue(CharacterStatType.Fortune, out var value)
            ? (double)value
            : 0d;
    }

    private static int ResolvePercentIntBonus(
        int rawBaseValue,
        ItemStatModifierBundle modifiers,
        CharacterStatType statType)
    {
        if (!modifiers.PercentValues.TryGetValue(statType, out var percentValue))
            return 0;

        var ratio = NormalizePercentLikeRatio(percentValue);
        if (ratio <= 0m)
            return 0;

        return decimal.ToInt32(decimal.Truncate(rawBaseValue * ratio));
    }

    private static double ResolvePercentFortuneBonus(
        double rawBaseValue,
        ItemStatModifierBundle modifiers)
    {
        if (!modifiers.PercentValues.TryGetValue(CharacterStatType.Fortune, out var percentValue))
            return 0d;

        var ratio = NormalizePercentLikeRatio(percentValue);
        if (ratio <= 0m)
            return 0d;

        return rawBaseValue * (double)ratio;
    }

    private static decimal NormalizePercentLikeRatio(decimal rawValue)
    {
        if (rawValue <= 0m)
            return 0m;

        var normalized = rawValue > 1m ? rawValue / 100m : rawValue;
        return Math.Max(0m, normalized);
    }

    private static bool HasMatchingFinalStats(CharacterBaseStatsDto current, CharacterBaseStatsDto updated)
    {
        return current.FinalHp == updated.FinalHp &&
               current.FinalMp == updated.FinalMp &&
               current.FinalAttack == updated.FinalAttack &&
               current.FinalSpeed == updated.FinalSpeed &&
               current.FinalSpiritualSense == updated.FinalSpiritualSense &&
               current.FinalStamina == updated.FinalStamina &&
               (current.PotentialHpBonus ?? 0) == (updated.PotentialHpBonus ?? 0) &&
               (current.PotentialMpBonus ?? 0) == (updated.PotentialMpBonus ?? 0) &&
               (current.PotentialAttackBonus ?? 0) == (updated.PotentialAttackBonus ?? 0) &&
               (current.PotentialSpeedBonus ?? 0) == (updated.PotentialSpeedBonus ?? 0) &&
               (current.PotentialSpiritualSenseBonus ?? 0) == (updated.PotentialSpiritualSenseBonus ?? 0) &&
               Math.Abs((current.PotentialFortuneBonus ?? 0d) - (updated.PotentialFortuneBonus ?? 0d)) < 0.000001d &&
               Math.Abs((current.FinalFortune ?? 0d) - (updated.FinalFortune ?? 0d)) < 0.000001d &&
               string.Equals(current.RealmDisplayName ?? string.Empty, updated.RealmDisplayName ?? string.Empty, StringComparison.Ordinal) &&
               (current.RealmMaxCultivation ?? 0L) == (updated.RealmMaxCultivation ?? 0L) &&
               Math.Abs((current.BreakthroughChancePercent ?? 0d) - (updated.BreakthroughChancePercent ?? 0d)) < 0.000001d &&
               (current.HasNextRealm ?? false) == (updated.HasNextRealm ?? false);
    }
}

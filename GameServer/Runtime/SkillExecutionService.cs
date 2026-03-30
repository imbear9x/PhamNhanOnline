using GameServer.Randomness;
using GameServer.World;
using GameShared.Messages;

namespace GameServer.Runtime;

public sealed class SkillExecutionService
{
    private readonly CombatDefinitionCatalog _combatDefinitions;
    private readonly CharacterRuntimeService _characterRuntimeService;
    private readonly WorldManager _worldManager;
    private readonly IRandomNumberProvider _random;

    public SkillExecutionService(
        CombatDefinitionCatalog combatDefinitions,
        CharacterRuntimeService characterRuntimeService,
        WorldManager worldManager,
        IRandomNumberProvider random)
    {
        _combatDefinitions = combatDefinitions;
        _characterRuntimeService = characterRuntimeService;
        _worldManager = worldManager;
        _random = random;
    }

    public CombatStatSnapshot CaptureCasterStats(PlayerSession caster, DateTime utcNow) =>
        caster.CaptureCombatStatsSnapshot(utcNow);

    public void ResolveCastRelease(MapInstance instance, PendingSkillExecution execution, DateTime utcNow)
    {
        if (!_combatDefinitions.TryGetSkill(execution.SkillId, out var skillDefinition))
            return;

        if (!TryGetCaster(instance, execution.CasterPlayerId, out var caster))
            return;

        ApplyEffects(instance, caster, skillDefinition, execution, SkillTriggerTiming.OnCastRelease, utcNow);
    }

    public SkillImpactResolvedRuntimeEvent ResolveImpact(
        MapInstance instance,
        PendingSkillExecution execution,
        DateTime utcNow)
    {
        if (!_combatDefinitions.TryGetSkill(execution.SkillId, out var skillDefinition))
        {
            return BuildImpactEvent(
                execution,
                false,
                MessageCode.SkillNotLearned,
                0,
                null,
                false,
                utcNow);
        }

        if (!TryGetCaster(instance, execution.CasterPlayerId, out var caster))
        {
            return BuildImpactEvent(
                execution,
                false,
                MessageCode.CharacterNotInWorldInstance,
                0,
                null,
                false,
                utcNow);
        }

        var matchingEffects = skillDefinition.Effects
            .Where(effect => ResolveTriggerTiming(effect) == SkillTriggerTiming.OnHit)
            .OrderBy(effect => effect.OrderIndex)
            .ToArray();

        if (matchingEffects.Length == 0)
            return BuildImpactEvent(execution, true, MessageCode.None, 0, null, false, utcNow);

        var summary = ApplyEffects(instance, caster, skillDefinition, execution, SkillTriggerTiming.OnHit, utcNow);
        return BuildImpactEvent(
            execution,
            summary.AnyApplied,
            summary.Code,
            summary.DamageApplied,
            summary.RemainingEnemyHp,
            summary.EnemyKilled,
            utcNow);
    }

    private SkillEffectApplicationSummary ApplyEffects(
        MapInstance instance,
        PlayerSession caster,
        SkillDefinition skillDefinition,
        PendingSkillExecution execution,
        SkillTriggerTiming triggerTiming,
        DateTime utcNow)
    {
        var summary = SkillEffectApplicationSummary.Empty;
        var effects = skillDefinition.Effects
            .Where(effect => ResolveTriggerTiming(effect) == triggerTiming)
            .OrderBy(effect => effect.OrderIndex);

        foreach (var effect in effects)
        {
            if (!PassChance(effect.ChanceValue))
                continue;

            var targetScope = ResolveTargetScope(effect);
            switch (targetScope)
            {
                case SkillTargetScope.Self:
                    summary = summary.Merge(ApplyEffectToPlayer(caster, effect, execution.CasterStats, utcNow));
                    break;

                case SkillTargetScope.Primary:
                    summary = summary.Merge(ApplyPrimaryEffect(instance, caster, skillDefinition, execution, effect, utcNow));
                    break;
            }
        }

        return summary;
    }

    private SkillEffectApplicationSummary ApplyPrimaryEffect(
        MapInstance instance,
        PlayerSession caster,
        SkillDefinition skillDefinition,
        PendingSkillExecution execution,
        SkillEffectDefinition effect,
        DateTime utcNow)
    {
        if (skillDefinition.TargetType == SkillTargetType.Self || !execution.Target.HasValue)
            return ApplyEffectToPlayer(caster, effect, execution.CasterStats, utcNow);

        var target = execution.Target.Value;
        if (target.Kind == GameShared.Enums.CombatTargetKind.Character &&
            target.CharacterId.HasValue &&
            TryGetTargetPlayer(instance, target.CharacterId.Value, out var targetPlayer))
        {
            return ApplyEffectToPlayer(targetPlayer, effect, execution.CasterStats, utcNow);
        }

        if (target.RuntimeId.HasValue)
            return ApplyEffectToEnemy(instance, caster, target.RuntimeId.Value, effect, execution.CasterStats, utcNow);

        return SkillEffectApplicationSummary.Empty;
    }

    private SkillEffectApplicationSummary ApplyEffectToPlayer(
        PlayerSession player,
        SkillEffectDefinition effect,
        CombatStatSnapshot casterStats,
        DateTime utcNow)
    {
        var amount = EvaluateMagnitude(effect, casterStats);
        var durationMs = NormalizeDuration(effect.DurationMs);
        return effect.EffectType switch
        {
            SkillEffectType.Damage => ApplyPlayerDamage(player, amount),
            SkillEffectType.Heal => ApplyPlayerResources(player, amount, 0, 0),
            SkillEffectType.ResourceReduce => ApplyPlayerResourceByType(player, effect.ResourceType, -amount),
            SkillEffectType.ResourceRestore => ApplyPlayerResourceByType(player, effect.ResourceType, amount),
            SkillEffectType.Shield => ApplyPlayerShield(player, amount, durationMs, utcNow),
            SkillEffectType.Stun => ApplyPlayerStun(player, durationMs, utcNow),
            SkillEffectType.BuffStat => ApplyPlayerStatModifier(player, effect, casterStats, durationMs, utcNow, isDebuff: false),
            SkillEffectType.DebuffStat => ApplyPlayerStatModifier(player, effect, casterStats, durationMs, utcNow, isDebuff: true),
            _ => SkillEffectApplicationSummary.Empty
        };
    }

    private SkillEffectApplicationSummary ApplyEffectToEnemy(
        MapInstance instance,
        PlayerSession caster,
        int enemyRuntimeId,
        SkillEffectDefinition effect,
        CombatStatSnapshot casterStats,
        DateTime utcNow)
    {
        var amount = EvaluateMagnitude(effect, casterStats);
        var durationMs = NormalizeDuration(effect.DurationMs);
        return effect.EffectType switch
        {
            SkillEffectType.Damage => ApplyEnemyDamage(instance, caster, enemyRuntimeId, amount, utcNow),
            SkillEffectType.Heal => ApplyEnemyHealing(instance, enemyRuntimeId, amount, utcNow),
            SkillEffectType.ResourceReduce => ApplyEnemyResourceByType(instance, caster, enemyRuntimeId, effect.ResourceType, -amount, utcNow),
            SkillEffectType.ResourceRestore => ApplyEnemyResourceByType(instance, caster, enemyRuntimeId, effect.ResourceType, amount, utcNow),
            SkillEffectType.Shield => ApplyEnemyShield(instance, enemyRuntimeId, amount, durationMs, utcNow),
            SkillEffectType.Stun => ApplyEnemyStun(instance, enemyRuntimeId, durationMs, utcNow),
            SkillEffectType.BuffStat => ApplyEnemyStatModifier(instance, enemyRuntimeId, effect, casterStats, durationMs, utcNow, isDebuff: false),
            SkillEffectType.DebuffStat => ApplyEnemyStatModifier(instance, enemyRuntimeId, effect, casterStats, durationMs, utcNow, isDebuff: true),
            _ => SkillEffectApplicationSummary.Empty
        };
    }

    private SkillEffectApplicationSummary ApplyPlayerDamage(PlayerSession player, int amount)
    {
        if (amount <= 0)
            return SkillEffectApplicationSummary.Empty;

        _characterRuntimeService.ApplyDamage(player, amount);
        return SkillEffectApplicationSummary.Applied(MessageCode.None);
    }

    private SkillEffectApplicationSummary ApplyPlayerResources(PlayerSession player, int hpDelta, int mpDelta, int staminaDelta)
    {
        if (hpDelta == 0 && mpDelta == 0 && staminaDelta == 0)
            return SkillEffectApplicationSummary.Empty;

        _characterRuntimeService.ApplyResourceDelta(player, hpDelta, mpDelta, staminaDelta);
        return SkillEffectApplicationSummary.Applied(MessageCode.None);
    }

    private SkillEffectApplicationSummary ApplyPlayerResourceByType(
        PlayerSession player,
        CombatResourceType? resourceType,
        int delta)
    {
        if (delta == 0)
            return SkillEffectApplicationSummary.Empty;

        return (resourceType ?? CombatResourceType.None) switch
        {
            CombatResourceType.Hp => ApplyPlayerResources(player, delta, 0, 0),
            CombatResourceType.Mp => ApplyPlayerResources(player, 0, delta, 0),
            CombatResourceType.Stamina => ApplyPlayerResources(player, 0, 0, delta),
            _ => SkillEffectApplicationSummary.Empty
        };
    }

    private SkillEffectApplicationSummary ApplyPlayerShield(PlayerSession player, int amount, int? durationMs, DateTime utcNow)
    {
        if (amount <= 0)
            return SkillEffectApplicationSummary.Empty;

        player.CombatStatuses.AddShield(amount, ResolveExpiresAtUtc(durationMs, utcNow));
        return SkillEffectApplicationSummary.Applied(MessageCode.None);
    }

    private SkillEffectApplicationSummary ApplyPlayerStun(PlayerSession player, int? durationMs, DateTime utcNow)
    {
        if (durationMs is not > 0)
            return SkillEffectApplicationSummary.Empty;

        player.CombatStatuses.AddStun(utcNow.AddMilliseconds(durationMs.Value));
        return SkillEffectApplicationSummary.Applied(MessageCode.None);
    }

    private SkillEffectApplicationSummary ApplyPlayerStatModifier(
        PlayerSession player,
        SkillEffectDefinition effect,
        CombatStatSnapshot casterStats,
        int? durationMs,
        DateTime utcNow,
        bool isDebuff)
    {
        var amount = EvaluateMagnitudeDecimal(effect, casterStats);
        if (amount <= 0 || durationMs is not > 0 || !TryResolveStatModifierTarget(effect, out var statType, out var valueType))
            return SkillEffectApplicationSummary.Empty;

        player.CombatStatuses.AddStatModifier(
            statType,
            isDebuff ? -amount : amount,
            valueType,
            ResolveExpiresAtUtc(durationMs, utcNow));
        return SkillEffectApplicationSummary.Applied(MessageCode.None);
    }

    private SkillEffectApplicationSummary ApplyEnemyDamage(
        MapInstance instance,
        PlayerSession caster,
        int enemyRuntimeId,
        int amount,
        DateTime utcNow)
    {
        if (amount <= 0)
            return SkillEffectApplicationSummary.Empty;

        var result = instance.ApplyEnemyDamage(caster, enemyRuntimeId, amount, utcNow);
        if (!result.Applied)
            return SkillEffectApplicationSummary.Failed(result.Code);

        return new SkillEffectApplicationSummary(
            true,
            MessageCode.None,
            result.AppliedDamage,
            result.RemainingHp,
            result.IsKilled);
    }

    private SkillEffectApplicationSummary ApplyEnemyHealing(
        MapInstance instance,
        int enemyRuntimeId,
        int amount,
        DateTime utcNow)
    {
        if (amount <= 0)
            return SkillEffectApplicationSummary.Empty;

        var result = instance.ApplyEnemyHealing(enemyRuntimeId, amount, utcNow);
        if (!result.Applied)
            return result.Code == MessageCode.None
                ? SkillEffectApplicationSummary.Empty
                : SkillEffectApplicationSummary.Failed(result.Code);

        return SkillEffectApplicationSummary.Applied(MessageCode.None, result.CurrentHp);
    }

    private SkillEffectApplicationSummary ApplyEnemyResourceByType(
        MapInstance instance,
        PlayerSession caster,
        int enemyRuntimeId,
        CombatResourceType? resourceType,
        int delta,
        DateTime utcNow)
    {
        if (delta == 0)
            return SkillEffectApplicationSummary.Empty;

        return (resourceType ?? CombatResourceType.None) switch
        {
            CombatResourceType.Hp when delta < 0 => ApplyEnemyDamage(instance, caster, enemyRuntimeId, Math.Abs(delta), utcNow),
            CombatResourceType.Hp when delta > 0 => ApplyEnemyHealing(instance, enemyRuntimeId, delta, utcNow),
            _ => SkillEffectApplicationSummary.Empty
        };
    }

    private SkillEffectApplicationSummary ApplyEnemyShield(
        MapInstance instance,
        int enemyRuntimeId,
        int amount,
        int? durationMs,
        DateTime utcNow)
    {
        if (amount <= 0 || !instance.TryApplyEnemyShield(enemyRuntimeId, amount, durationMs, utcNow))
            return SkillEffectApplicationSummary.Empty;

        return SkillEffectApplicationSummary.Applied(MessageCode.None);
    }

    private SkillEffectApplicationSummary ApplyEnemyStun(
        MapInstance instance,
        int enemyRuntimeId,
        int? durationMs,
        DateTime utcNow)
    {
        if (durationMs is not > 0 || !instance.TryApplyEnemyStun(enemyRuntimeId, durationMs.Value, utcNow))
            return SkillEffectApplicationSummary.Empty;

        return SkillEffectApplicationSummary.Applied(MessageCode.None);
    }

    private SkillEffectApplicationSummary ApplyEnemyStatModifier(
        MapInstance instance,
        int enemyRuntimeId,
        SkillEffectDefinition effect,
        CombatStatSnapshot casterStats,
        int? durationMs,
        DateTime utcNow,
        bool isDebuff)
    {
        var amount = EvaluateMagnitudeDecimal(effect, casterStats);
        if (amount <= 0 ||
            durationMs is not > 0 ||
            !TryResolveStatModifierTarget(effect, out var statType, out var valueType))
        {
            return SkillEffectApplicationSummary.Empty;
        }

        var applied = instance.TryApplyEnemyStatModifier(
            enemyRuntimeId,
            statType,
            isDebuff ? -amount : amount,
            valueType,
            durationMs,
            utcNow);
        return applied
            ? SkillEffectApplicationSummary.Applied(MessageCode.None)
            : SkillEffectApplicationSummary.Empty;
    }

    private static bool TryResolveStatModifierTarget(
        SkillEffectDefinition effect,
        out CharacterStatType statType,
        out CombatValueType valueType)
    {
        statType = effect.StatType ?? CharacterStatType.None;
        valueType = effect.ValueType ?? CombatValueType.Flat;
        if (statType == CharacterStatType.None)
            return false;

        return true;
    }

    private static int EvaluateMagnitude(SkillEffectDefinition effect, CombatStatSnapshot casterStats)
    {
        return Math.Max(0, decimal.ToInt32(decimal.Truncate(EvaluateMagnitudeDecimal(effect, casterStats))));
    }

    private static decimal EvaluateMagnitudeDecimal(SkillEffectDefinition effect, CombatStatSnapshot casterStats)
    {
        var baseValue = effect.BaseValue ?? 0m;
        var ratioValue = effect.RatioValue ?? 0m;
        var extraValue = effect.ExtraValue ?? 0m;
        decimal rawValue = baseValue + extraValue;

        switch (effect.FormulaType ?? SkillFormulaType.Flat)
        {
            case SkillFormulaType.AttackRatio:
                rawValue += casterStats.Attack * ratioValue;
                break;

            case SkillFormulaType.CasterMaxHpRatio:
                rawValue += casterStats.MaxHp * ratioValue;
                break;

            case SkillFormulaType.CasterMaxMpRatio:
                rawValue += casterStats.MaxMp * ratioValue;
                break;
        }

        return Math.Max(0m, rawValue);
    }

    private bool PassChance(decimal? chanceValue)
    {
        if (!chanceValue.HasValue || chanceValue.Value <= 0)
            return true;

        var normalizedChance = chanceValue.Value <= 1m
            ? chanceValue.Value
            : Math.Min(1m, chanceValue.Value / 100m);

        var roll = _random.NextInt(1_000_000) / 1_000_000m;
        return roll <= normalizedChance;
    }

    private bool TryGetCaster(MapInstance instance, Guid casterPlayerId, out PlayerSession caster)
    {
        caster = null!;
        if (!_worldManager.TryGetPlayer(casterPlayerId, out var resolvedCaster) ||
            !resolvedCaster.IsConnected ||
            resolvedCaster.MapId != instance.MapId ||
            resolvedCaster.InstanceId != instance.InstanceId)
        {
            return false;
        }

        caster = resolvedCaster;
        return true;
    }

    private bool TryGetTargetPlayer(MapInstance instance, Guid characterId, out PlayerSession targetPlayer)
    {
        targetPlayer = null!;
        if (!_worldManager.TryGetPlayerByCharacterId(characterId, out var resolvedPlayer) ||
            !resolvedPlayer.IsConnected ||
            resolvedPlayer.MapId != instance.MapId ||
            resolvedPlayer.InstanceId != instance.InstanceId)
        {
            return false;
        }

        targetPlayer = resolvedPlayer;
        return true;
    }

    private static SkillTargetScope ResolveTargetScope(SkillEffectDefinition effect) =>
        effect.TargetScope ?? SkillTargetScope.Primary;

    private static SkillTriggerTiming ResolveTriggerTiming(SkillEffectDefinition effect) =>
        effect.TriggerTiming ?? SkillTriggerTiming.OnHit;

    private static int? NormalizeDuration(int? durationMs) =>
        durationMs is > 0 ? durationMs : null;

    private static DateTime? ResolveExpiresAtUtc(int? durationMs, DateTime utcNow) =>
        durationMs is > 0 ? utcNow.AddMilliseconds(durationMs.Value) : null;

    private static SkillImpactResolvedRuntimeEvent BuildImpactEvent(
        PendingSkillExecution execution,
        bool applied,
        MessageCode code,
        int damageApplied,
        int? remainingHp,
        bool isKilled,
        DateTime resolvedAtUtc)
    {
        return new SkillImpactResolvedRuntimeEvent(
            execution.ExecutionId,
            execution.CasterPlayerId,
            execution.CasterCharacterId,
            execution.Target,
            execution.SkillSlotIndex,
            execution.PlayerSkillId,
            execution.SkillId,
            applied,
            code,
            damageApplied,
            remainingHp,
            isKilled,
            resolvedAtUtc);
    }

    private readonly record struct SkillEffectApplicationSummary(
        bool AnyApplied,
        MessageCode Code,
        int DamageApplied,
        int? RemainingEnemyHp,
        bool EnemyKilled)
    {
        public static SkillEffectApplicationSummary Empty => new(false, MessageCode.None, 0, null, false);

        public static SkillEffectApplicationSummary Applied(MessageCode code, int? remainingEnemyHp = null) =>
            new(true, code, 0, remainingEnemyHp, false);

        public static SkillEffectApplicationSummary Failed(MessageCode code) =>
            new(false, code, 0, null, false);

        public SkillEffectApplicationSummary Merge(SkillEffectApplicationSummary other)
        {
            if (!other.AnyApplied && other.Code == MessageCode.None)
                return this;

            return new SkillEffectApplicationSummary(
                AnyApplied || other.AnyApplied,
                other.Code != MessageCode.None ? other.Code : Code,
                DamageApplied + other.DamageApplied,
                other.RemainingEnemyHp ?? RemainingEnemyHp,
                EnemyKilled || other.EnemyKilled);
        }
    }
}

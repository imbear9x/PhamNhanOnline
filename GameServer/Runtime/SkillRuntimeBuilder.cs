namespace GameServer.Runtime;

public sealed class SkillRuntimeBuilder
{
    private readonly CombatDefinitionCatalog _combatDefinitions;

    public SkillRuntimeBuilder(CombatDefinitionCatalog combatDefinitions)
    {
        _combatDefinitions = combatDefinitions;
    }

    public SkillRuntimeDefinition BuildRuntime(int martialArtSkillId, int currentMartialArtStage)
    {
        if (!_combatDefinitions.TryGetMartialArtSkill(martialArtSkillId, out var unlock))
            throw new InvalidOperationException($"Martial art skill {martialArtSkillId} was not found.");

        var effectiveStage = Math.Max(1, currentMartialArtStage - unlock.UnlockStage + 1);
        var cooldownMs = unlock.Skill.CooldownMs;
        var effects = unlock.Skill.Effects
            .Select(effect => CreateRuntimeEffect(effect, unlock.ScalingRules, effectiveStage))
            .ToArray();

        foreach (var rule in unlock.ScalingRules)
        {
            if (rule.ScalingTarget != SkillScalingTarget.SkillCooldownMs)
                continue;

            cooldownMs = ApplyIntDelta(cooldownMs, ResolveScalingDelta(rule, effectiveStage));
        }

        return new SkillRuntimeDefinition(
            unlock.SkillId,
            unlock.MartialArtId,
            unlock.Id,
            unlock.UnlockStage,
            currentMartialArtStage,
            effectiveStage,
            unlock.Skill.Code,
            unlock.Skill.Name,
            unlock.Skill.SkillType,
            unlock.Skill.TargetType,
            Math.Max(0, cooldownMs),
            effects);
    }

    private static SkillRuntimeEffect CreateRuntimeEffect(
        SkillEffectDefinition effect,
        IReadOnlyList<SkillScalingRuleDefinition> scalingRules,
        int effectiveStage)
    {
        var runtime = new SkillRuntimeEffect(
            effect.Id,
            effect.EffectType,
            effect.OrderIndex,
            effect.FormulaType,
            effect.ValueType,
            effect.BaseValue,
            effect.RatioValue,
            effect.ExtraValue,
            effect.ChanceValue,
            effect.DurationMs,
            effect.StatType,
            effect.ResourceType,
            effect.TargetScope,
            effect.TriggerTiming);

        foreach (var rule in scalingRules)
        {
            if (rule.SkillEffectId.HasValue && rule.SkillEffectId.Value != effect.Id)
                continue;
            if (rule.ScalingTarget == SkillScalingTarget.SkillCooldownMs)
                continue;

            var delta = ResolveScalingDelta(rule, effectiveStage);
            runtime = rule.ScalingTarget switch
            {
                SkillScalingTarget.EffectBaseValue => runtime with { BaseValue = (runtime.BaseValue ?? 0m) + delta },
                SkillScalingTarget.EffectRatioValue => runtime with { RatioValue = (runtime.RatioValue ?? 0m) + delta },
                SkillScalingTarget.EffectExtraValue => runtime with { ExtraValue = (runtime.ExtraValue ?? 0m) + delta },
                SkillScalingTarget.EffectChanceValue => runtime with { ChanceValue = (runtime.ChanceValue ?? 0m) + delta },
                SkillScalingTarget.EffectDurationMs => runtime with { DurationMs = ApplyIntDelta(runtime.DurationMs ?? 0, delta) },
                _ => runtime
            };
        }

        return runtime;
    }

    private static decimal ResolveScalingDelta(SkillScalingRuleDefinition rule, int effectiveStage)
    {
        return rule.BaseValue + (effectiveStage * rule.PerStageValue);
    }

    private static int ApplyIntDelta(int currentValue, decimal delta)
    {
        return checked(currentValue + decimal.ToInt32(decimal.Truncate(delta)));
    }
}

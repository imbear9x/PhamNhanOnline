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

        return new SkillRuntimeDefinition(
            unlock.SkillId,
            unlock.MartialArtId,
            unlock.Id,
            unlock.UnlockStage,
            currentMartialArtStage,
            unlock.Skill.GroupCode,
            unlock.Skill.SkillLevel,
            unlock.Skill.Code,
            unlock.Skill.Name,
            unlock.Skill.SkillType,
            unlock.Skill.SkillCategory,
            unlock.Skill.TargetType,
            unlock.Skill.CastRange,
            Math.Max(0, unlock.Skill.CooldownMs),
            unlock.Skill.Effects
                .Select(effect => new SkillRuntimeEffect(
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
                    effect.TriggerTiming))
                .ToArray());
    }
}

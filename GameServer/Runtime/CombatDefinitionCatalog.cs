using GameServer.Entities;
using GameServer.Repositories;
using GameShared.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Runtime;

public sealed class CombatDefinitionCatalog
{
    private readonly IReadOnlyDictionary<int, MartialArtDefinition> _martialArtsById;
    private readonly IReadOnlyDictionary<int, SkillDefinition> _skillsById;
    private readonly IReadOnlyDictionary<int, MartialArtSkillUnlockDefinition> _martialArtSkillsById;

    public CombatDefinitionCatalog(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var martialArts = scope.ServiceProvider.GetRequiredService<MartialArtRepository>().GetAllAsync().GetAwaiter().GetResult();
        var stages = scope.ServiceProvider.GetRequiredService<MartialArtStageRepository>().GetAllAsync().GetAwaiter().GetResult();
        var bonuses = scope.ServiceProvider.GetRequiredService<MartialArtStageStatBonusRepository>().GetAllAsync().GetAwaiter().GetResult();
        var skills = scope.ServiceProvider.GetRequiredService<SkillRepository>().GetAllAsync().GetAwaiter().GetResult();
        var effects = scope.ServiceProvider.GetRequiredService<SkillEffectRepository>().GetAllAsync().GetAwaiter().GetResult();
        var martialArtSkills = scope.ServiceProvider.GetRequiredService<MartialArtSkillRepository>().GetAllAsync().GetAwaiter().GetResult();

        _skillsById = BuildSkills(skills, effects);
        _martialArtSkillsById = BuildMartialArtSkills(martialArtSkills, _skillsById);
        _martialArtsById = BuildMartialArts(martialArts, stages, bonuses, _martialArtSkillsById);
    }

    public IReadOnlyCollection<MartialArtDefinition> GetAllMartialArts() => _martialArtsById.Values.ToArray();

    public IReadOnlyCollection<SkillDefinition> GetAllSkills() => _skillsById.Values.ToArray();

    public bool TryGetMartialArt(int martialArtId, out MartialArtDefinition definition) =>
        _martialArtsById.TryGetValue(martialArtId, out definition!);

    public bool TryGetSkill(int skillId, out SkillDefinition definition) =>
        _skillsById.TryGetValue(skillId, out definition!);

    public bool TryGetMartialArtSkill(int martialArtSkillId, out MartialArtSkillUnlockDefinition definition) =>
        _martialArtSkillsById.TryGetValue(martialArtSkillId, out definition!);

    private static IReadOnlyDictionary<int, SkillDefinition> BuildSkills(
        IReadOnlyCollection<SkillEntity> skills,
        IReadOnlyCollection<SkillEffectEntity> effects)
    {
        var effectsBySkillId = effects
            .GroupBy(x => x.SkillId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<SkillEffectDefinition>)x
                    .OrderBy(effect => effect.OrderIndex)
                    .Select(ToSkillEffectDefinition)
                    .ToArray());

        var result = new Dictionary<int, SkillDefinition>(skills.Count);
        foreach (var skill in skills)
        {
            result[skill.Id] = new SkillDefinition(
                skill.Id,
                skill.Code,
                skill.Name,
                ResolveSkillGroupCode(skill),
                Math.Max(1, skill.SkillLevel),
                (CombatSkillType)skill.SkillType,
                Enum.IsDefined(typeof(SkillCategory), skill.SkillCategory)
                    ? (SkillCategory)skill.SkillCategory
                    : SkillCategory.Normal,
                (SkillTargetType)skill.TargetType,
                (float)skill.CastRange,
                Math.Max(0, skill.CastTimeMs),
                Math.Max(0, skill.TravelTimeMs),
                skill.CooldownMs,
                skill.Description,
                effectsBySkillId.GetValueOrDefault(skill.Id, Array.Empty<SkillEffectDefinition>()));
        }

        return result;
    }

    private static IReadOnlyDictionary<int, MartialArtSkillUnlockDefinition> BuildMartialArtSkills(
        IReadOnlyCollection<MartialArtSkillEntity> martialArtSkills,
        IReadOnlyDictionary<int, SkillDefinition> skillsById)
    {
        var result = new Dictionary<int, MartialArtSkillUnlockDefinition>(martialArtSkills.Count);
        foreach (var mapping in martialArtSkills)
        {
            if (!skillsById.TryGetValue(mapping.SkillId, out var skillDefinition))
                throw new InvalidOperationException($"Skill {mapping.SkillId} referenced by martial art skill {mapping.Id} was not found.");

            result[mapping.Id] = new MartialArtSkillUnlockDefinition(
                mapping.Id,
                mapping.MartialArtId,
                mapping.SkillId,
                mapping.UnlockStage,
                skillDefinition);
        }

        return result;
    }

    private static IReadOnlyDictionary<int, MartialArtDefinition> BuildMartialArts(
        IReadOnlyCollection<MartialArtEntity> martialArts,
        IReadOnlyCollection<MartialArtStageEntity> stages,
        IReadOnlyCollection<MartialArtStageStatBonusEntity> bonuses,
        IReadOnlyDictionary<int, MartialArtSkillUnlockDefinition> martialArtSkillsById)
    {
        var bonusesByStageId = bonuses
            .GroupBy(x => x.MartialArtStageId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<CharacterStatBonusValue>)x
                    .OrderBy(bonus => bonus.Id)
                    .Select(ToStatBonusDefinition)
                    .ToArray());

        var stagesByMartialArtId = stages
            .GroupBy(x => x.MartialArtId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<MartialArtStageDefinition>)x
                    .OrderBy(stage => stage.StageLevel)
                    .Select(stage => new MartialArtStageDefinition(
                        stage.Id,
                        stage.MartialArtId,
                        stage.StageLevel,
                        stage.ExpRequired,
                        stage.IsBottleneck,
                        stage.BreakthroughBaseRate,
                        stage.BreakthroughExpPenalty,
                        bonusesByStageId.GetValueOrDefault(stage.Id, Array.Empty<CharacterStatBonusValue>())))
                    .ToArray());

        var skillUnlocksByMartialArtId = martialArtSkillsById.Values
            .GroupBy(x => x.MartialArtId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<MartialArtSkillUnlockDefinition>)x
                    .OrderBy(unlock => unlock.UnlockStage)
                    .ThenBy(unlock => unlock.Id)
                    .ToArray());

        var result = new Dictionary<int, MartialArtDefinition>(martialArts.Count);
        foreach (var martialArt in martialArts)
        {
            result[martialArt.Id] = new MartialArtDefinition(
                martialArt.Id,
                martialArt.Code,
                martialArt.Name,
                ResolveMartialArtIconKey(martialArt),
                martialArt.Quality,
                martialArt.Category,
                martialArt.Description,
                martialArt.QiAbsorptionRate,
                martialArt.MaxStage,
                stagesByMartialArtId.GetValueOrDefault(martialArt.Id, Array.Empty<MartialArtStageDefinition>()),
                skillUnlocksByMartialArtId.GetValueOrDefault(martialArt.Id, Array.Empty<MartialArtSkillUnlockDefinition>()));
        }

        return result;
    }

    private static CharacterStatBonusValue ToStatBonusDefinition(MartialArtStageStatBonusEntity entity)
    {
        return new CharacterStatBonusValue(
            entity.Id,
            (CharacterStatType)entity.StatType,
            entity.Value,
            (CombatValueType)entity.ValueType);
    }

    private static SkillEffectDefinition ToSkillEffectDefinition(SkillEffectEntity entity)
    {
        return new SkillEffectDefinition(
            entity.Id,
            entity.SkillId,
            (SkillEffectType)entity.EffectType,
            entity.OrderIndex,
            entity.FormulaType.HasValue ? (SkillFormulaType)entity.FormulaType.Value : null,
            entity.ValueType.HasValue ? (CombatValueType)entity.ValueType.Value : null,
            entity.BaseValue,
            entity.RatioValue,
            entity.ExtraValue,
            entity.ChanceValue,
            entity.DurationMs,
            entity.StatType.HasValue ? (CharacterStatType)entity.StatType.Value : null,
            entity.ResourceType.HasValue ? (CombatResourceType)entity.ResourceType.Value : null,
            entity.TargetScope.HasValue ? (SkillTargetScope)entity.TargetScope.Value : null,
            entity.TriggerTiming.HasValue ? (SkillTriggerTiming)entity.TriggerTiming.Value : null);
    }

    private static string ResolveMartialArtIconKey(MartialArtEntity entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.Icon))
            return entity.Icon.Trim();

        return entity.Code;
    }

    private static string ResolveSkillGroupCode(SkillEntity entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.SkillGroupCode))
            return entity.SkillGroupCode.Trim();

        return entity.Code;
    }
}

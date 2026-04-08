using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GameServer.Runtime;
using GameShared.Enums;

namespace GameServer.Descriptions;

public sealed class GameplayDescriptionService
{
    private const string DefaultSkillTemplate = "{effects_summary}";
    private const string DefaultMartialArtTemplate = "{qi_summary}\n{stage_summary}\n{unlocked_skills_summary}";

    private readonly DescriptionTemplateCompiler _compiler;
    private readonly CombatDefinitionCatalog _combatDefinitions;
    private readonly AlchemyDefinitionCatalog _alchemyDefinitions;

    public GameplayDescriptionService(
        DescriptionTemplateCompiler compiler,
        CombatDefinitionCatalog combatDefinitions,
        AlchemyDefinitionCatalog alchemyDefinitions)
    {
        _compiler = compiler;
        _combatDefinitions = combatDefinitions;
        _alchemyDefinitions = alchemyDefinitions;
    }

    public string? BuildSkillDescription(SkillDefinition definition)
    {
        var context = BuildSkillContext(definition);
        return CompileWithFallback(
            definition.DescriptionTemplate,
            definition.Description,
            context,
            DefaultSkillTemplate);
    }

    public string? BuildMartialArtDescription(MartialArtDefinition definition)
    {
        var context = BuildMartialArtContext(definition);
        return CompileWithFallback(
            definition.DescriptionTemplate,
            definition.Description,
            context,
            DefaultMartialArtTemplate);
    }

    public string? BuildItemDescription(ItemDefinition definition)
    {
        var context = BuildItemContext(definition);
        return CompileWithFallback(
            definition.DescriptionTemplate,
            definition.Description,
            context,
            BuildDefaultItemTemplate(definition));
    }

    private string? CompileWithFallback(
        string? template,
        string? legacyDescription,
        DescriptionTemplateContext context,
        string? fallbackTemplate)
    {
        if (!string.IsNullOrWhiteSpace(template))
        {
            var compiled = _compiler.Compile(template, context);
            if (compiled.Success && !string.IsNullOrWhiteSpace(compiled.Text))
                return compiled.Text;
        }

        if (!string.IsNullOrWhiteSpace(legacyDescription))
            return legacyDescription.Trim();

        if (string.IsNullOrWhiteSpace(fallbackTemplate))
            return null;

        var fallbackCompiled = _compiler.Compile(fallbackTemplate, context);
        return string.IsNullOrWhiteSpace(fallbackCompiled.Text) ? null : fallbackCompiled.Text;
    }

    private DescriptionTemplateContext BuildSkillContext(SkillDefinition definition)
    {
        var context = new DescriptionTemplateContext();
        context.Set("name", definition.Name);
        context.Set("code", definition.Code);
        context.Set("group_code", definition.GroupCode);
        context.Set("skill_level", definition.SkillLevel);
        context.Set("skill_type", definition.SkillType.ToString());
        context.Set("skill_type_label", GetSkillTypeLabel(definition.SkillType));
        context.Set("skill_category", definition.SkillCategory.ToString());
        context.Set("skill_category_label", GetSkillCategoryLabel(definition.SkillCategory));
        context.Set("target_type", definition.TargetType.ToString());
        context.Set("target_type_label", GetSkillTargetTypeLabel(definition.TargetType));
        context.Set("cast_range", definition.CastRange);
        context.Set("cast_time_ms", definition.CastTimeMs);
        context.Set("travel_time_ms", definition.TravelTimeMs);
        context.Set("cooldown_ms", definition.CooldownMs);
        context.Set("effects_count", definition.Effects.Count);
        context.Set("effects_summary", BuildSkillEffectsSummary(definition));
        context.Set("range_summary", BuildSkillRangeSummary(definition));
        context.Set("cast_summary", BuildSkillCastSummary(definition));
        context.Set("cooldown_summary", BuildSkillCooldownSummary(definition));

        for (var i = 0; i < definition.Effects.Count; i++)
        {
            var effect = definition.Effects[i];
            var prefix = $"effect{i + 1}.";
            context.Set(prefix + "effect_type", effect.EffectType.ToString());
            context.Set(prefix + "effect_type_label", GetSkillEffectTypeLabel(effect.EffectType));
            context.Set(prefix + "order_index", effect.OrderIndex);
            context.Set(prefix + "formula_type", effect.FormulaType?.ToString());
            context.Set(prefix + "formula_type_label", GetFormulaTypeLabel(effect.FormulaType));
            context.Set(prefix + "formula_subject_label", GetFormulaSubjectLabel(effect.FormulaType));
            context.Set(prefix + "formula_subject_icon_name", GetFormulaSubjectIconName(effect.FormulaType));
            context.Set(prefix + "formula_subject_rich", GetFormulaSubjectRich(effect.FormulaType));
            context.Set(prefix + "value_type", effect.ValueType?.ToString());
            context.Set(prefix + "value_type_label", GetCombatValueTypeLabel(effect.ValueType));
            context.Set(prefix + "base_value", effect.BaseValue);
            context.Set(prefix + "ratio_value", effect.RatioValue);
            context.Set(prefix + "extra_value", effect.ExtraValue);
            context.Set(prefix + "chance_value", effect.ChanceValue);
            context.Set(prefix + "duration_ms", effect.DurationMs);
            context.Set(prefix + "stat_type", effect.StatType?.ToString());
            context.Set(prefix + "stat_type_label", GetCharacterStatLabel(effect.StatType));
            context.Set(prefix + "resource_type", effect.ResourceType?.ToString());
            context.Set(prefix + "resource_type_label", GetCombatResourceLabel(effect.ResourceType));
            context.Set(prefix + "target_scope", effect.TargetScope?.ToString());
            context.Set(prefix + "target_scope_label", GetTargetScopeLabel(definition.TargetType, effect.TargetScope));
            context.Set(prefix + "target_label", GetTemplateTargetLabel(definition.TargetType, effect.TargetScope));
            context.Set(prefix + "trigger_timing", effect.TriggerTiming?.ToString());
            context.Set(prefix + "trigger_timing_label", GetTriggerTimingLabel(effect.TriggerTiming));
            context.Set(prefix + "summary", BuildSkillEffectSummary(definition, effect));
        }

        return context;
    }

    private DescriptionTemplateContext BuildMartialArtContext(MartialArtDefinition definition)
    {
        var context = new DescriptionTemplateContext();
        context.Set("name", definition.Name);
        context.Set("code", definition.Code);
        context.Set("quality", definition.Quality);
        context.Set("quality_label", GetItemRarityLabel(definition.Quality));
        context.Set("category", definition.Category);
        context.Set("qi_absorption_rate", definition.QiAbsorptionRate);
        context.Set("max_stage", definition.MaxStage);
        context.Set("stages_count", definition.Stages.Count);
        context.Set("skills_count", definition.SkillUnlocks.Count);
        context.Set("qi_summary", string.Format(CultureInfo.InvariantCulture, "Ti le hap thu linh khi: x{0:0.##}.", Math.Max(0m, definition.QiAbsorptionRate)));
        context.Set("stage_summary", string.Format(CultureInfo.InvariantCulture, "Canh gioi toi da: {0} tang.", Math.Max(0, definition.MaxStage)));
        context.Set("unlocked_skills_summary", BuildMartialArtUnlockSummary(definition));
        context.Set("stage_bonuses_summary", BuildMartialArtStageBonusSummary(definition));
        return context;
    }

    private DescriptionTemplateContext BuildItemContext(ItemDefinition definition)
    {
        var context = new DescriptionTemplateContext();
        context.Set("name", definition.Name);
        context.Set("code", definition.Code);
        context.Set("item_type", definition.ItemType.ToString());
        context.Set("item_type_label", GetItemTypeLabel(definition.ItemType));
        context.Set("rarity", definition.Rarity.ToString());
        context.Set("rarity_label", GetItemRarityLabel((int)definition.Rarity));
        context.Set("max_stack", definition.MaxStack);
        context.Set("equipment_stats_summary", BuildEquipmentStatsSummary(definition));
        context.Set("requirements_summary", BuildItemRequirementsSummary(definition));
        context.Set("use_effects_summary", BuildItemUseEffectsSummary(definition));
        context.Set("martial_art_book_summary", BuildMartialArtBookSummary(definition));
        context.Set("pill_recipe_book_summary", BuildPillRecipeBookSummary(definition));
        context.Set("soil_summary", BuildSoilSummary(definition));
        context.Set("herb_seed_summary", BuildHerbSeedSummary(definition));
        context.Set("herb_plant_summary", BuildHerbPlantSummary(definition));

        if (definition.Equipment is not null)
        {
            context.Set("equipment.slot_type", definition.Equipment.SlotType.ToString());
            context.Set("equipment.slot_type_label", GetEquipmentSlotLabel(definition.Equipment.SlotType));
            context.Set("equipment.equipment_type", definition.Equipment.EquipmentType.ToString());
            context.Set("equipment.equipment_type_label", GetEquipmentTypeLabel(definition.Equipment.EquipmentType));
            context.Set("equipment.level_requirement", definition.Equipment.LevelRequirement);
        }

        if (definition.MartialArtBook is not null &&
            _combatDefinitions.TryGetMartialArt(definition.MartialArtBook.MartialArtId, out var martialArt))
        {
            context.Set("martial_art_book_id", martialArt.Id);
            context.Set("martial_art_book_name", martialArt.Name);
        }

        if (_alchemyDefinitions.TryGetPillRecipeByBookItemTemplate(definition.Id, out var recipe))
        {
            context.Set("pill_recipe_id", recipe.Id);
            context.Set("pill_recipe_name", recipe.Name);
        }

        return context;
    }

    private string BuildDefaultItemTemplate(ItemDefinition definition)
    {
        return definition.ItemType switch
        {
            ItemType.Equipment => "{equipment_stats_summary}\n{requirements_summary}",
            ItemType.Consumable => "{use_effects_summary}",
            ItemType.MartialArtBook => "{martial_art_book_summary}",
            ItemType.PillRecipeBook => "{pill_recipe_book_summary}",
            ItemType.Soil => "{soil_summary}",
            ItemType.HerbSeed => "{herb_seed_summary}",
            ItemType.HerbPlant => "{herb_plant_summary}",
            _ => string.Empty
        };
    }

    private static string BuildItemRequirementsSummary(ItemDefinition definition)
    {
        if (definition.Equipment is null || definition.Equipment.LevelRequirement <= 0)
            return string.Empty;

        return string.Format(
            CultureInfo.InvariantCulture,
            "Yeu cau cap do: {0}.",
            definition.Equipment.LevelRequirement);
    }

    private static string BuildEquipmentStatsSummary(ItemDefinition definition)
    {
        if (definition.Equipment is null)
            return string.Empty;

        var parts = new List<string>
        {
            string.Format(
                CultureInfo.InvariantCulture,
                "Trang bi {0} ({1}).",
                GetEquipmentTypeLabel(definition.Equipment.EquipmentType),
                GetEquipmentSlotLabel(definition.Equipment.SlotType))
        };

        if (definition.Equipment.BaseStats.Count > 0)
        {
            var statParts = definition.Equipment.BaseStats
                .Select(stat => string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1} {2}",
                    stat.Value >= 0 ? "+" : string.Empty,
                    FormatStatModifierValue(stat.Value, stat.ValueType),
                    GetCharacterStatLabel(stat.StatType)))
                .ToArray();

            parts.Add("Chi so: " + string.Join(" | ", statParts) + ".");
        }

        return string.Join(Environment.NewLine, parts);
    }

    private string BuildItemUseEffectsSummary(ItemDefinition definition)
    {
        if (!_alchemyDefinitions.TryGetPillTemplate(definition.Id, out var pillDefinition) ||
            pillDefinition.UsageType != PillUsageType.ConsumeDirectly ||
            pillDefinition.Effects.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            Environment.NewLine,
            pillDefinition.Effects
                .OrderBy(x => x.OrderIndex)
                .Select(BuildPillEffectSummary)
                .Where(static text => !string.IsNullOrWhiteSpace(text)));
    }

    private string BuildMartialArtBookSummary(ItemDefinition definition)
    {
        if (definition.MartialArtBook is null)
            return string.Empty;

        if (_combatDefinitions.TryGetMartialArt(definition.MartialArtBook.MartialArtId, out var martialArt))
            return $"Hoc cong phap {martialArt.Name}.";

        return "Hoc mot cong phap lien ket voi sach nay.";
    }

    private string BuildPillRecipeBookSummary(ItemDefinition definition)
    {
        if (!_alchemyDefinitions.TryGetPillRecipeByBookItemTemplate(definition.Id, out var recipe))
            return string.Empty;

        return $"Hoc cong thuc luyen dan {recipe.Name}.";
    }

    private string BuildSoilSummary(ItemDefinition definition)
    {
        if (!_alchemyDefinitions.TryGetSoil(definition.Id, out var soil))
            return string.Empty;

        return string.Format(
            CultureInfo.InvariantCulture,
            "Dat linh tho x{0:0.##}, ton tai toi da {1}.",
            Math.Max(0m, soil.GrowthSpeedRate),
            FormatDuration(soil.MaxActiveSeconds * 1000L));
    }

    private string BuildHerbSeedSummary(ItemDefinition definition)
    {
        if (!_alchemyDefinitions.TryGetHerbBySeedItemTemplate(definition.Id, out var herb))
            return string.Empty;

        return $"Hat giong cua duoc thao {herb.Name}.";
    }

    private string BuildHerbPlantSummary(ItemDefinition definition)
    {
        if (!_alchemyDefinitions.TryGetHerbByReplantItemTemplate(definition.Id, out var herb))
            return string.Empty;

        return $"Co the trong lai duoc thao {herb.Name}.";
    }

    private static string BuildMartialArtUnlockSummary(MartialArtDefinition definition)
    {
        if (definition.SkillUnlocks.Count == 0)
            return string.Empty;

        var summary = string.Join(
            ", ",
            definition.SkillUnlocks
                .OrderBy(x => x.UnlockStage)
                .ThenBy(x => x.Id)
                .Select(unlock => string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} (tang {1})",
                    unlock.Skill.Name,
                    Math.Max(0, unlock.UnlockStage))));

        return $"Ky nang mo khoa: {summary}.";
    }

    private static string BuildMartialArtStageBonusSummary(MartialArtDefinition definition)
    {
        if (definition.Stages.Count == 0)
            return string.Empty;

        var lines = new List<string>();
        foreach (var stage in definition.Stages.OrderBy(x => x.StageLevel))
        {
            if (stage.StatBonuses.Count == 0)
                continue;

            var stats = string.Join(
                ", ",
                stage.StatBonuses.Select(bonus => string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1} {2}",
                    bonus.Value >= 0 ? "+" : string.Empty,
                    FormatStatModifierValue(bonus.Value, bonus.ValueType),
                    GetCharacterStatLabel(bonus.StatType))));
            lines.Add($"Tang {stage.StageLevel}: {stats}.");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private string BuildSkillEffectsSummary(SkillDefinition definition)
    {
        if (definition.Effects.Count == 0)
            return string.Empty;

        return string.Join(
            Environment.NewLine,
            definition.Effects
                .OrderBy(x => x.OrderIndex)
                .Select(effect => BuildSkillEffectSummary(definition, effect))
                .Where(static text => !string.IsNullOrWhiteSpace(text)));
    }

    private static string BuildSkillRangeSummary(SkillDefinition definition)
    {
        if (definition.TargetType == SkillTargetType.Self || definition.CastRange <= 0f)
            return string.Empty;

        return string.Format(
            CultureInfo.InvariantCulture,
            "Tam thi trien: {0:0.##}.",
            Math.Max(0f, definition.CastRange));
    }

    private static string BuildSkillCastSummary(SkillDefinition definition)
    {
        var parts = new List<string>();
        if (definition.CastTimeMs > 0)
            parts.Add($"Niem: {FormatDuration(definition.CastTimeMs)}");
        if (definition.TravelTimeMs > 0)
            parts.Add($"Cham dich: {FormatDuration(definition.TravelTimeMs)}");

        return parts.Count == 0 ? string.Empty : string.Join(" | ", parts) + ".";
    }

    private static string BuildSkillCooldownSummary(SkillDefinition definition)
    {
        return definition.CooldownMs <= 0
            ? string.Empty
            : $"Hoi chieu: {FormatDuration(definition.CooldownMs)}.";
    }

    private static string BuildSkillEffectSummary(SkillDefinition skill, SkillEffectDefinition effect)
    {
        var targetLabel = GetTargetScopeLabel(skill.TargetType, effect.TargetScope);
        var timingSuffix = BuildTimingSuffix(effect.TriggerTiming);
        return effect.EffectType switch
        {
            SkillEffectType.Damage => $"Gay {BuildSkillMagnitudeSummary(effect)} sat thuong len {targetLabel}{timingSuffix}.",
            SkillEffectType.Heal => $"Hoi {BuildSkillMagnitudeSummary(effect)} HP cho {targetLabel}{timingSuffix}.",
            SkillEffectType.ResourceReduce => $"Giam {BuildSkillMagnitudeSummary(effect)} {GetCombatResourceLabel(effect.ResourceType)} cua {targetLabel}{timingSuffix}.",
            SkillEffectType.ResourceRestore => $"Hoi {BuildSkillMagnitudeSummary(effect)} {GetCombatResourceLabel(effect.ResourceType)} cho {targetLabel}{timingSuffix}.",
            SkillEffectType.Shield => $"Tao la chan {BuildSkillMagnitudeSummary(effect)} cho {targetLabel}{BuildDurationSuffix(effect.DurationMs)}{timingSuffix}.",
            SkillEffectType.Stun => $"Lam choang {targetLabel}{BuildDurationSuffix(effect.DurationMs)}{BuildChanceSuffix(effect.ChanceValue)}{timingSuffix}.",
            SkillEffectType.BuffStat => $"Tang {GetCharacterStatLabel(effect.StatType)} {BuildSkillStatModifierSummary(effect)} cho {targetLabel}{BuildDurationSuffix(effect.DurationMs)}{timingSuffix}.",
            SkillEffectType.DebuffStat => $"Giam {GetCharacterStatLabel(effect.StatType)} {BuildSkillStatModifierSummary(effect)} cua {targetLabel}{BuildDurationSuffix(effect.DurationMs)}{timingSuffix}.",
            _ => string.Empty
        };
    }

    private static string BuildPillEffectSummary(PillEffectDefinition effect)
    {
        var amount = BuildPillMagnitudeSummary(effect);
        return effect.EffectType switch
        {
            PillEffectType.RecoverHp => $"Hoi {amount} HP.",
            PillEffectType.RecoverMp => $"Hoi {amount} MP.",
            _ => string.Empty
        };
    }

    private static string BuildPillMagnitudeSummary(PillEffectDefinition effect)
    {
        var parts = new List<string>();
        if (effect.BaseValue.HasValue)
            parts.Add(FormatPillAmount(effect.BaseValue.Value, effect.ValueType));
        if (effect.RatioValue.HasValue)
            parts.Add(FormatPillAmount(effect.RatioValue.Value, CombatValueType.Ratio));

        return parts.Count == 0 ? "gia tri khong ro" : string.Join(" + ", parts);
    }

    private static string FormatPillAmount(decimal value, CombatValueType? valueType)
    {
        return valueType switch
        {
            CombatValueType.Percent => FormatPercent(value, normalizeRatio: false),
            CombatValueType.Ratio => FormatPercent(value, normalizeRatio: true),
            _ => value.ToString("0.####", CultureInfo.InvariantCulture)
        };
    }

    private static string BuildSkillMagnitudeSummary(SkillEffectDefinition effect)
    {
        var parts = new List<string>();
        if (effect.BaseValue.HasValue && effect.BaseValue.Value != 0m)
            parts.Add(effect.BaseValue.Value.ToString("0.####", CultureInfo.InvariantCulture));
        if (effect.ExtraValue.HasValue && effect.ExtraValue.Value != 0m)
            parts.Add(effect.ExtraValue.Value.ToString("0.####", CultureInfo.InvariantCulture));

        if (effect.RatioValue.HasValue && effect.RatioValue.Value != 0m)
        {
            var ratioText = FormatPercent(effect.RatioValue.Value, normalizeRatio: true);
            parts.Add(effect.FormulaType switch
            {
                SkillFormulaType.AttackRatio => $"{ratioText} Cong",
                SkillFormulaType.CasterMaxHpRatio => $"{ratioText} HP toi da",
                SkillFormulaType.CasterMaxMpRatio => $"{ratioText} MP toi da",
                _ => ratioText
            });
        }

        return parts.Count == 0 ? "gia tri khong ro" : string.Join(" + ", parts);
    }

    private static string BuildSkillStatModifierSummary(SkillEffectDefinition effect)
    {
        var value = effect.BaseValue ?? effect.RatioValue ?? effect.ExtraValue ?? 0m;
        return effect.ValueType switch
        {
            CombatValueType.Percent => FormatPercent(value, normalizeRatio: false),
            CombatValueType.Ratio => FormatPercent(value, normalizeRatio: true),
            _ => value.ToString("0.####", CultureInfo.InvariantCulture)
        };
    }

    private static string BuildTimingSuffix(SkillTriggerTiming? triggerTiming)
    {
        return triggerTiming switch
        {
            SkillTriggerTiming.OnCastRelease => " khi tha chieu",
            SkillTriggerTiming.OnHit => " khi trung dich",
            _ => string.Empty
        };
    }

    private static string BuildDurationSuffix(int? durationMs)
    {
        return durationMs.HasValue && durationMs.Value > 0
            ? $" trong {FormatDuration(durationMs.Value)}"
            : string.Empty;
    }

    private static string BuildChanceSuffix(decimal? chanceValue)
    {
        return chanceValue.HasValue && chanceValue.Value > 0m && chanceValue.Value < 100m
            ? $" ({FormatPercent(chanceValue.Value, normalizeRatio: false)} xac suat)"
            : string.Empty;
    }

    private static string FormatStatModifierValue(decimal value, CombatValueType valueType)
    {
        return valueType switch
        {
            CombatValueType.Percent => FormatPercent(value, normalizeRatio: false),
            CombatValueType.Ratio => FormatPercent(value, normalizeRatio: true),
            _ => value.ToString("0.####", CultureInfo.InvariantCulture)
        };
    }

    private static string FormatPercent(decimal value, bool normalizeRatio)
    {
        if (normalizeRatio)
            value *= 100m;

        return value.ToString("0.####", CultureInfo.InvariantCulture) + "%";
    }

    private static string FormatDuration(long durationMs)
    {
        if (durationMs <= 0)
            return "0s";

        if (durationMs % 1000 == 0)
            return $"{durationMs / 1000}s";

        return (durationMs / 1000d).ToString("0.##s", CultureInfo.InvariantCulture);
    }

    private static string GetSkillTypeLabel(CombatSkillType skillType)
    {
        return skillType switch
        {
            CombatSkillType.Active => "Chu dong",
            CombatSkillType.Passive => "Bi dong",
            CombatSkillType.Toggle => "Bat/tat",
            _ => "Khong ro"
        };
    }

    private static string GetSkillCategoryLabel(SkillCategory category)
    {
        return category switch
        {
            SkillCategory.Basic => "Co ban",
            SkillCategory.Normal => "Thuong",
            SkillCategory.Special => "Dac biet",
            _ => "Khong ro"
        };
    }

    private static string GetSkillTargetTypeLabel(SkillTargetType targetType)
    {
        return targetType switch
        {
            SkillTargetType.Self => "Ban than",
            SkillTargetType.SingleEnemy => "Don muc tieu dich",
            SkillTargetType.EnemyArea => "Pham vi dich",
            SkillTargetType.SingleAlly => "Don muc tieu dong minh",
            SkillTargetType.AllyArea => "Pham vi dong minh",
            SkillTargetType.GroundArea => "Pham vi mat dat",
            SkillTargetType.AllEnemiesMap => "Toan bo dich trong map",
            SkillTargetType.AllAlliesMap => "Toan bo dong minh trong map",
            SkillTargetType.AllUnitsMap => "Toan bo don vi trong map",
            _ => "Khong ro"
        };
    }

    private static string GetSkillEffectTypeLabel(SkillEffectType effectType)
    {
        return effectType switch
        {
            SkillEffectType.Damage => "Gay sat thuong",
            SkillEffectType.Heal => "Hoi mau",
            SkillEffectType.ResourceReduce => "Giam tai nguyen",
            SkillEffectType.ResourceRestore => "Hoi tai nguyen",
            SkillEffectType.Shield => "La chan",
            SkillEffectType.Stun => "Lam choang",
            SkillEffectType.BuffStat => "Tang chi so",
            SkillEffectType.DebuffStat => "Giam chi so",
            _ => "Khong ro"
        };
    }

    private static string GetFormulaTypeLabel(SkillFormulaType? formulaType)
    {
        return formulaType switch
        {
            SkillFormulaType.Flat => "Co dinh",
            SkillFormulaType.AttackRatio => "Ti le cong",
            SkillFormulaType.CasterMaxHpRatio => "Ti le HP toi da",
            SkillFormulaType.CasterMaxMpRatio => "Ti le MP toi da",
            _ => string.Empty
        };
    }

    private static string GetFormulaSubjectLabel(SkillFormulaType? formulaType)
    {
        return formulaType switch
        {
            SkillFormulaType.AttackRatio => "Cong",
            SkillFormulaType.CasterMaxHpRatio => "HP toi da",
            SkillFormulaType.CasterMaxMpRatio => "MP toi da",
            SkillFormulaType.Flat => "gia tri co dinh",
            _ => "gia tri"
        };
    }

    private static string GetFormulaSubjectIconName(SkillFormulaType? formulaType)
    {
        return formulaType switch
        {
            SkillFormulaType.AttackRatio => "attack_icon_des",
            SkillFormulaType.CasterMaxHpRatio => "hp_icon_des",
            SkillFormulaType.CasterMaxMpRatio => "mp_icon_des",
            _ => string.Empty
        };
    }

    private static string GetFormulaSubjectRich(SkillFormulaType? formulaType)
    {
        var iconName = GetFormulaSubjectIconName(formulaType);
        return string.IsNullOrWhiteSpace(iconName)
            ? GetFormulaSubjectLabel(formulaType)
            : $"<sprite name=\"{iconName}\">";
    }

    private static string GetCombatValueTypeLabel(CombatValueType? valueType)
    {
        return valueType switch
        {
            CombatValueType.Flat => "Co dinh",
            CombatValueType.Ratio => "Ti le",
            CombatValueType.Percent => "Phan tram",
            _ => string.Empty
        };
    }

    private static string GetTargetScopeLabel(SkillTargetType skillTargetType, SkillTargetScope? targetScope)
    {
        return targetScope switch
        {
            SkillTargetScope.Self => "ban than",
            SkillTargetScope.Primary when skillTargetType == SkillTargetType.Self => "ban than",
            SkillTargetScope.Primary => "muc tieu chinh",
            SkillTargetScope.AreaAroundPrimary => "vung quanh muc tieu",
            SkillTargetScope.AllResolvedTargets => "tat ca muc tieu da xac dinh",
            SkillTargetScope.AllEnemiesMap => "toan bo dich trong map",
            SkillTargetScope.AllAlliesMap => "toan bo dong minh trong map",
            SkillTargetScope.AllUnitsMap => "tat ca don vi trong map",
            _ when skillTargetType == SkillTargetType.Self => "ban than",
            _ => "muc tieu"
        };
    }

    private static string GetTemplateTargetLabel(SkillTargetType skillTargetType, SkillTargetScope? targetScope)
    {
        return targetScope switch
        {
            SkillTargetScope.Self => "ban than",
            SkillTargetScope.Primary => skillTargetType == SkillTargetType.Self ? "ban than" : "muc tieu",
            SkillTargetScope.AreaAroundPrimary => "vung quanh muc tieu",
            SkillTargetScope.AllResolvedTargets => "tat ca muc tieu",
            SkillTargetScope.AllEnemiesMap => "toan bo dich trong map",
            SkillTargetScope.AllAlliesMap => "toan bo dong minh trong map",
            SkillTargetScope.AllUnitsMap => "tat ca don vi trong map",
            _ when skillTargetType == SkillTargetType.Self => "ban than",
            _ => "muc tieu"
        };
    }

    private static string GetTriggerTimingLabel(SkillTriggerTiming? triggerTiming)
    {
        return triggerTiming switch
        {
            SkillTriggerTiming.OnCastRelease => "Khi tha chieu",
            SkillTriggerTiming.OnHit => "Khi trung dich",
            SkillTriggerTiming.OnExpire => "Khi het han",
            SkillTriggerTiming.OnCastStart => "Khi bat dau niem",
            SkillTriggerTiming.OnInterval => "Theo nhip",
            _ => string.Empty
        };
    }

    private static string GetCharacterStatLabel(CharacterStatType? statType)
    {
        return statType switch
        {
            CharacterStatType.MaxHp => "HP toi da",
            CharacterStatType.MaxMp => "MP toi da",
            CharacterStatType.MaxStamina => "The luc toi da",
            CharacterStatType.Attack => "Cong",
            CharacterStatType.Speed => "Toc do",
            CharacterStatType.SpiritualSense => "Than thuc",
            CharacterStatType.Fortune => "Khi van",
            _ => "chi so"
        };
    }

    private static string GetCombatResourceLabel(CombatResourceType? resourceType)
    {
        return resourceType switch
        {
            CombatResourceType.Hp => "HP",
            CombatResourceType.Mp => "MP",
            CombatResourceType.Stamina => "The luc",
            _ => "tai nguyen"
        };
    }

    private static string GetItemTypeLabel(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.Equipment => "Trang bi",
            ItemType.Consumable => "Tieu hao",
            ItemType.Material => "Nguyen lieu",
            ItemType.Talisman => "Phu bao",
            ItemType.MartialArtBook => "Sach cong phap",
            ItemType.Currency => "Tien te",
            ItemType.QuestItem => "Nhiem vu",
            ItemType.PillRecipeBook => "Sach cong thuc dan",
            ItemType.HerbSeed => "Hat giong",
            ItemType.HerbMaterial => "Duoc lieu",
            ItemType.Soil => "Dat linh",
            ItemType.HerbPlant => "Cay trong",
            _ => "Vat pham"
        };
    }

    private static string GetItemRarityLabel(int rarity)
    {
        return rarity switch
        {
            1 => "Thuong",
            2 => "Tot",
            3 => "Hiem",
            4 => "Su thi",
            5 => "Truyen thuyet",
            _ => "Khong ro"
        };
    }

    private static string GetEquipmentSlotLabel(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => "Vu khi",
            EquipmentSlot.Armor => "Ao",
            EquipmentSlot.Pants => "Quan",
            EquipmentSlot.Shoes => "Giay",
            _ => "Khong ro"
        };
    }

    private static string GetEquipmentTypeLabel(EquipmentType equipmentType)
    {
        return equipmentType switch
        {
            EquipmentType.Sword => "Kiem",
            EquipmentType.Bow => "Cung",
            EquipmentType.Armor => "Phap y",
            EquipmentType.Pants => "Huyen khau",
            EquipmentType.Shoes => "Giay",
            _ => "Trang bi"
        };
    }
}

using Npgsql;

namespace AdminDesignerTool;

internal sealed record LookupOption(object? Value, string Display);

internal sealed record AdminColumnBinding(
    string ColumnName,
    string? HeaderText = null,
    Type? EnumType = null,
    string? LookupSql = null);

internal static class AdminColumnBindingCatalog
{
    private static readonly Dictionary<string, Dictionary<string, AdminColumnBinding>> Bindings =
        BuildBindings();

    public static AdminColumnBinding? Find(string tableName, string columnName)
    {
        if (!Bindings.TryGetValue(tableName, out var columns))
            return null;

        return columns.TryGetValue(columnName, out var binding) ? binding : null;
    }

    public static async Task<IReadOnlyList<LookupOption>> LoadOptionsAsync(
        string connectionString,
        AdminColumnBinding binding,
        bool allowNull)
    {
        var options = new List<LookupOption>();
        if (allowNull)
            options.Add(new LookupOption(null, string.Empty));

        if (binding.EnumType is not null)
        {
            foreach (var value in Enum.GetValues(binding.EnumType))
            {
                options.Add(new LookupOption(
                    Convert.ToInt32(value),
                    $"{Convert.ToInt32(value)} - {value}"));
            }

            return options;
        }

        if (string.IsNullOrWhiteSpace(binding.LookupSql))
            return options;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(binding.LookupSql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            options.Add(new LookupOption(reader.GetValue(0), reader.GetString(1)));
        }

        return options;
    }

    public static string ToHeaderText(string columnName)
    {
        return string.Join(
            " ",
            columnName
                .Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static Dictionary<string, Dictionary<string, AdminColumnBinding>> BuildBindings()
    {
        var result = new Dictionary<string, Dictionary<string, AdminColumnBinding>>(StringComparer.OrdinalIgnoreCase);

        void Add(string tableName, string columnName, string? headerText = null, Type? enumType = null, string? lookupSql = null)
        {
            if (!result.TryGetValue(tableName, out var tableBindings))
            {
                tableBindings = new Dictionary<string, AdminColumnBinding>(StringComparer.OrdinalIgnoreCase);
                result[tableName] = tableBindings;
            }

            tableBindings[columnName] = new AdminColumnBinding(columnName, headerText, enumType, lookupSql);
        }

        const string martialArtLookupSql = """
            select id as value, code || ' - ' || name as display
            from public.martial_arts
            order by id;
            """;

        const string martialArtStageLookupSql = """
            select mas.id as value, ma.code || ' - Stage ' || mas.stage_level as display
            from public.martial_art_stages mas
            inner join public.martial_arts ma on ma.id = mas.martial_art_id
            order by ma.id, mas.stage_level;
            """;

        const string skillLookupSql = """
            select id as value, code || ' - ' || name as display
            from public.skills
            order by id;
            """;

        const string skillEffectLookupSql = """
            select se.id as value, s.code || ' - Effect ' || se.order_index as display
            from public.skill_effects se
            inner join public.skills s on s.id = se.skill_id
            order by s.id, se.order_index;
            """;

        const string martialArtSkillLookupSql = """
            select mas.id as value, ma.code || ' -> ' || s.code || ' @Stage ' || mas.unlock_stage as display
            from public.martial_art_skills mas
            inner join public.martial_arts ma on ma.id = mas.martial_art_id
            inner join public.skills s on s.id = mas.skill_id
            order by ma.id, mas.unlock_stage, s.id;
            """;

        const string itemTemplateLookupSql = """
            select id as value, code || ' - ' || name as display
            from public.item_templates
            order by id;
            """;

        const string accountLookupSql = """
            select a.id as value,
                   coalesce(cred.provider_user_id, '(khong co credential)') as display
            from public.accounts a
            left join lateral (
                select ac.provider_user_id
                from public.account_credentials ac
                where ac.account_id = a.id
                order by ac.created_at nulls first, ac.id
                limit 1
            ) cred on true
            order by display, a.id;
            """;

        const string characterLookupSql = """
            select c.id as value,
                   c.name || ' - ' || coalesce(cred.provider_user_id, '(khong co credential)') as display
            from public.characters c
            left join lateral (
                select ac.provider_user_id
                from public.account_credentials ac
                where ac.account_id = c.account_id
                order by ac.created_at nulls first, ac.id
                limit 1
            ) cred on true
            order by c.name, c.id;
            """;

        const string equipmentItemTemplateLookupSql = """
            select id as value, code || ' - ' || name as display
            from public.item_templates
            where item_type = 1
            order by id;
            """;

        const string martialArtBookItemTemplateLookupSql = """
            select id as value, code || ' - ' || name as display
            from public.item_templates
            where item_type = 5
            order by id;
            """;

        const string pillRecipeBookItemTemplateLookupSql = """
            select id as value, code || ' - ' || name as display
            from public.item_templates
            where item_type = 8
            order by id;
            """;

        const string herbSeedItemTemplateLookupSql = """
            select id as value, code || ' - ' || name as display
            from public.item_templates
            where item_type = 9
            order by id;
            """;

        const string herbPlantItemTemplateLookupSql = """
            select id as value, code || ' - ' || name as display
            from public.item_templates
            where item_type = 12
            order by id;
            """;

        const string soilItemTemplateLookupSql = """
            select id as value, code || ' - ' || name as display
            from public.item_templates
            where item_type = 11
            order by id;
            """;

        const string equipmentTemplateLookupSql = """
            select et.item_template_id as value, it.code || ' - ' || it.name as display
            from public.equipment_templates et
            inner join public.item_templates it on it.id = et.item_template_id
            order by et.item_template_id;
            """;

        const string playerEquipmentItemLookupSql = """
            select pi.id as value,
                   '[' || pi.id || '] ' || coalesce(c.name, '(ground)') || ' - ' || it.name as display
            from public.player_items pi
            inner join public.item_templates it on it.id = pi.item_template_id
            inner join public.equipment_templates et on et.item_template_id = pi.item_template_id
            left join public.characters c on c.id = pi.player_id
            order by pi.id desc;
            """;

        const string craftRecipeLookupSql = """
            select id as value, code || ' - ' || name as display
            from public.craft_recipes
            order by id;
            """;

        const string spiritualEnergyTemplateLookupSql = """
            select id as value, code || ' - ' || name as display
            from public.spiritual_energy_templates
            order by id;
            """;

        const string mapTemplateLookupSql = """
            select id as value, name || ' (ID ' || id || ')' as display
            from public.map_templates
            order by id;
            """;

        const string pillTemplateLookupSql = """
            select pt.item_template_id as value, it.code || ' - ' || it.name as display
            from public.pill_templates pt
            inner join public.item_templates it on it.id = pt.item_template_id
            order by pt.item_template_id;
            """;

        const string pillRecipeLookupSql = """
            select id as value, code || ' - ' || name as display
            from public.pill_recipe_templates
            order by id;
            """;

        const string gameRandomTableLookupSql = """
            select id as value, table_id as display
            from public.game_random_tables
            order by id;
            """;

        const string gameRandomEntryLookupSql = """
            select gre.id as value, grt.table_id || ' -> ' || gre.entry_id as display
            from public.game_random_entries gre
            inner join public.game_random_tables grt on grt.id = gre.game_random_table_id
            order by grt.id, gre.order_index, gre.id;
            """;

        const string herbTemplateLookupSql = """
            select id as value, code || ' - ' || name as display
            from public.herb_templates
            order by id;
            """;

        const string enemyTemplateLookupSql = """
            select id as value, code || ' - ' || name as display
            from public.enemy_templates
            order by id;
            """;

        const string enemySpawnGroupLookupSql = """
            select id as value, code || ' - ' || name as display
            from public.map_enemy_spawn_groups
            order by id;
            """;

        Add("martial_arts", "icon", headerText: "Icon Key Công Pháp");
        Add("martial_arts", "qi_absorption_rate", headerText: "Hệ Số Hấp Thụ Linh Khí");
        Add("martial_art_stages", "martial_art_id", lookupSql: martialArtLookupSql);
        Add("martial_art_stage_stat_bonuses", "martial_art_stage_id", lookupSql: martialArtStageLookupSql);
        Add("martial_art_stage_stat_bonuses", "stat_type", enumType: typeof(CharacterStatType));
        Add("martial_art_stage_stat_bonuses", "value_type", enumType: typeof(CombatValueType));

        Add("skills", "skill_type", enumType: typeof(CombatSkillType));
        Add("skills", "target_type", enumType: typeof(SkillTargetType));
        Add("skills", "cast_range", headerText: "Tam Xa");

        Add("skill_effects", "skill_id", lookupSql: skillLookupSql);
        Add("skill_effects", "effect_type", enumType: typeof(SkillEffectType));
        Add("skill_effects", "formula_type", enumType: typeof(SkillFormulaType));
        Add("skill_effects", "value_type", enumType: typeof(CombatValueType));
        Add("skill_effects", "stat_type", enumType: typeof(CharacterStatType));
        Add("skill_effects", "resource_type", enumType: typeof(CombatResourceType));
        Add("skill_effects", "target_scope", enumType: typeof(SkillTargetScope));
        Add("skill_effects", "trigger_timing", enumType: typeof(SkillTriggerTiming));

        Add("martial_art_skills", "martial_art_id", lookupSql: martialArtLookupSql);
        Add("martial_art_skills", "skill_id", lookupSql: skillLookupSql);

        Add("martial_art_skill_scalings", "martial_art_skill_id", lookupSql: martialArtSkillLookupSql);
        Add("martial_art_skill_scalings", "skill_effect_id", lookupSql: skillEffectLookupSql);
        Add("martial_art_skill_scalings", "scaling_target", enumType: typeof(SkillScalingTarget));
        Add("martial_art_skill_scalings", "value_type", enumType: typeof(CombatValueType));

        Add("item_templates", "item_type", enumType: typeof(ItemType));
        Add("item_templates", "rarity", enumType: typeof(ItemRarity));

        Add("characters", "account_id", headerText: "Tai Khoan", lookupSql: accountLookupSql);

        Add("equipment_templates", "item_template_id", lookupSql: equipmentItemTemplateLookupSql);
        Add("equipment_templates", "slot_type", enumType: typeof(EquipmentSlot));
        Add("equipment_templates", "equipment_type", enumType: typeof(EquipmentType));

        Add("equipment_template_stats", "equipment_template_id", lookupSql: equipmentTemplateLookupSql);
        Add("equipment_template_stats", "stat_type", enumType: typeof(CharacterStatType));
        Add("equipment_template_stats", "value_type", enumType: typeof(CombatValueType));

        Add("martial_art_book_templates", "item_template_id", lookupSql: martialArtBookItemTemplateLookupSql);
        Add("martial_art_book_templates", "martial_art_id", lookupSql: martialArtLookupSql);

        Add("craft_recipes", "result_item_template_id", lookupSql: itemTemplateLookupSql);
        Add("craft_recipe_requirements", "craft_recipe_id", lookupSql: craftRecipeLookupSql);
        Add("craft_recipe_requirements", "required_item_template_id", lookupSql: itemTemplateLookupSql);
        Add("craft_recipe_requirements", "consume_mode", enumType: typeof(CraftConsumeMode));
        Add("craft_recipe_mutation_bonuses", "craft_recipe_id", lookupSql: craftRecipeLookupSql);
        Add("craft_recipe_mutation_bonuses", "stat_type", enumType: typeof(CharacterStatType));
        Add("craft_recipe_mutation_bonuses", "value_type", enumType: typeof(CombatValueType));

        Add("map_templates", "map_type", enumType: typeof(MapType));
        Add("map_zone_slots", "map_template_id", lookupSql: mapTemplateLookupSql);
        Add("map_zone_slots", "spiritual_energy_template_id", lookupSql: spiritualEnergyTemplateLookupSql);

        Add("game_random_tables", "mode", enumType: typeof(GameRandomTableMode));
        Add("game_random_entries", "game_random_table_id", lookupSql: gameRandomTableLookupSql);
        Add("game_random_entry_tags", "game_random_entry_id", lookupSql: gameRandomEntryLookupSql);
        Add("game_random_fortune_tags", "game_random_table_id", lookupSql: gameRandomTableLookupSql);

        Add("enemy_templates", "kind", enumType: typeof(EnemyKind));
        Add("enemy_template_skills", "enemy_template_id", lookupSql: enemyTemplateLookupSql);
        Add("enemy_template_skills", "skill_id", lookupSql: skillLookupSql);
        Add("enemy_reward_rules", "enemy_template_id", lookupSql: enemyTemplateLookupSql);
        Add("enemy_reward_rules", "delivery_type", enumType: typeof(RewardDeliveryType));
        Add("enemy_reward_rules", "target_rule", enumType: typeof(RewardTargetRule));
        Add("enemy_reward_rules", "game_random_table_id", lookupSql: gameRandomTableLookupSql);

        Add("map_enemy_spawn_groups", "map_template_id", lookupSql: mapTemplateLookupSql);
        Add("map_enemy_spawn_groups", "runtime_scope", enumType: typeof(MapSpawnRuntimeScope));
        Add("map_enemy_spawn_groups", "spawn_mode", enumType: typeof(EnemySpawnMode));
        Add("map_enemy_spawn_entries", "spawn_group_id", lookupSql: enemySpawnGroupLookupSql);
        Add("map_enemy_spawn_entries", "enemy_template_id", lookupSql: enemyTemplateLookupSql);
        Add("map_instance_configs", "map_template_id", lookupSql: mapTemplateLookupSql);
        Add("map_instance_configs", "instance_mode", enumType: typeof(InstanceMode));
        Add("map_instance_configs", "completion_rule", enumType: typeof(InstanceCompletionRule));

        Add("pill_templates", "item_template_id", lookupSql: itemTemplateLookupSql);
        Add("pill_templates", "pill_category", enumType: typeof(PillCategory));
        Add("pill_templates", "usage_type", enumType: typeof(PillUsageType));

        Add("pill_effects", "pill_template_id", lookupSql: pillTemplateLookupSql);
        Add("pill_effects", "effect_type", enumType: typeof(PillEffectType));
        Add("pill_effects", "value_type", enumType: typeof(CombatValueType));
        Add("pill_effects", "stat_type", enumType: typeof(CharacterStatType));

        Add("pill_recipe_templates", "recipe_book_item_template_id", lookupSql: pillRecipeBookItemTemplateLookupSql);
        Add("pill_recipe_templates", "result_pill_item_template_id", lookupSql: itemTemplateLookupSql);
        Add("pill_recipe_inputs", "pill_recipe_template_id", lookupSql: pillRecipeLookupSql);
        Add("pill_recipe_inputs", "required_item_template_id", lookupSql: itemTemplateLookupSql);
        Add("pill_recipe_inputs", "consume_mode", enumType: typeof(CraftConsumeMode));
        Add("pill_recipe_inputs", "required_herb_maturity", enumType: typeof(HerbMaturityRequirement));
        Add("pill_recipe_mastery_stages", "pill_recipe_template_id", lookupSql: pillRecipeLookupSql);

        Add("soil_templates", "item_template_id", lookupSql: soilItemTemplateLookupSql);

        Add("herb_templates", "seed_item_template_id", lookupSql: herbSeedItemTemplateLookupSql);
        Add("herb_templates", "replant_item_template_id", lookupSql: herbPlantItemTemplateLookupSql);
        Add("herb_growth_stage_configs", "herb_template_id", lookupSql: herbTemplateLookupSql);
        Add("herb_growth_stage_configs", "stage", enumType: typeof(HerbGrowthStage));
        Add("herb_harvest_outputs", "herb_template_id", lookupSql: herbTemplateLookupSql);
        Add("herb_harvest_outputs", "required_stage", enumType: typeof(HerbGrowthStage));
        Add("herb_harvest_outputs", "output_type", enumType: typeof(HerbHarvestOutputType));
        Add("herb_harvest_outputs", "result_item_template_id", lookupSql: itemTemplateLookupSql);

        Add("potential_stat_upgrade_tiers", "target_stat", enumType: typeof(PotentialAllocationTarget));

        Add("player_items", "player_id", headerText: "Nhan Vat", lookupSql: characterLookupSql);
        Add("player_items", "item_template_id", headerText: "Item Template", lookupSql: itemTemplateLookupSql);
        Add("player_items", "location_type", headerText: "Vi Tri Item", enumType: typeof(ItemLocationType));

        Add("player_equipments", "player_item_id", headerText: "Item Instance", lookupSql: playerEquipmentItemLookupSql);
        Add("player_equipments", "equipped_slot", headerText: "Dang Mac O", enumType: typeof(EquipmentSlot));

        Add("player_equipment_stat_bonuses", "player_item_id", headerText: "Item Instance", lookupSql: playerEquipmentItemLookupSql);
        Add("player_equipment_stat_bonuses", "stat_type", headerText: "Chi So", enumType: typeof(CharacterStatType));
        Add("player_equipment_stat_bonuses", "value_type", headerText: "Kieu Gia Tri", enumType: typeof(CombatValueType));
        Add("player_equipment_stat_bonuses", "source_type", headerText: "Nguon Bonus", enumType: typeof(EquipmentBonusSourceType));

        return result;
    }
}

using Npgsql;

namespace AdminDesignerTool;

internal sealed record AdminDependencyCheck(
    string Description,
    string Sql,
    string MissingMessage);

internal sealed record AdminDependencyEvaluation(
    bool CanAddRow,
    IReadOnlyList<string> MissingMessages);

internal static class AdminDependencyCatalog
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<AdminDependencyCheck>> ChecksByTable =
        BuildChecks();

    public static async Task<AdminDependencyEvaluation> EvaluateAsync(string connectionString, AdminTableLoadRequest request)
    {
        if (!ChecksByTable.TryGetValue(request.Resource.TableName, out var checks) || checks.Count == 0)
            return new AdminDependencyEvaluation(true, Array.Empty<string>());

        var missingMessages = new List<string>();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (var check in checks)
        {
            await using var command = new NpgsqlCommand(check.Sql, connection);
            var raw = await command.ExecuteScalarAsync();
            var count = raw is null or DBNull ? 0L : Convert.ToInt64(raw);
            if (count <= 0)
                missingMessages.Add(check.MissingMessage);
        }

        return new AdminDependencyEvaluation(missingMessages.Count == 0, missingMessages);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<AdminDependencyCheck>> BuildChecks()
    {
        static AdminDependencyCheck Check(string description, string sql, string message) =>
            new(description, sql, message);

        return new Dictionary<string, IReadOnlyList<AdminDependencyCheck>>(StringComparer.OrdinalIgnoreCase)
        {
            ["martial_art_stages"] =
            [
                Check("Cong phap", "select count(*) from public.martial_arts;", "Can tao it nhat 1 Cong Phap truoc khi them Tang Cong Phap.")
            ],
            ["martial_art_stage_stat_bonuses"] =
            [
                Check("Tang cong phap", "select count(*) from public.martial_art_stages;", "Can tao it nhat 1 Tang Cong Phap truoc khi them Bonus Tang Cong Phap.")
            ],
            ["skill_effects"] =
            [
                Check("Skill", "select count(*) from public.skills;", "Can tao it nhat 1 Skill truoc khi them Skill Effects.")
            ],
            ["martial_art_skills"] =
            [
                Check("Cong phap", "select count(*) from public.martial_arts;", "Can tao it nhat 1 Cong Phap truoc khi them Unlock Skill Tu Cong Phap."),
                Check("Skill", "select count(*) from public.skills;", "Can tao it nhat 1 Skill truoc khi them Unlock Skill Tu Cong Phap.")
            ],
            ["martial_art_skill_scalings"] =
            [
                Check("Unlock skill", "select count(*) from public.martial_art_skills;", "Can tao unlock skill trong Cong Phap truoc khi them Scaling Skill Theo Cong Phap.")
            ],
            ["equipment_templates"] =
            [
                Check("Equipment item template", "select count(*) from public.item_templates where item_type = 1;", "Can tao it nhat 1 Item Template co loai Equipment truoc khi them Equipment Templates.")
            ],
            ["equipment_template_stats"] =
            [
                Check("Equipment template", "select count(*) from public.equipment_templates;", "Can tao it nhat 1 Equipment Template truoc khi them Equipment Template Stats.")
            ],
            ["player_items"] =
            [
                Check("Character", "select count(*) from public.characters;", "Can tao it nhat 1 Character truoc khi them Player Items."),
                Check("Item template", "select count(*) from public.item_templates;", "Can tao it nhat 1 Item Template truoc khi them Player Items.")
            ],
            ["player_equipments"] =
            [
                Check("Equipment item instance", """
                    select count(*)
                    from public.player_items pi
                    inner join public.equipment_templates et on et.item_template_id = pi.item_template_id;
                    """, "Can tao it nhat 1 Player Item co item_template la Equipment truoc khi them Player Equipments.")
            ],
            ["player_equipment_stat_bonuses"] =
            [
                Check("Player equipment", "select count(*) from public.player_equipments;", "Can tao it nhat 1 Player Equipment truoc khi them Player Equipment Stat Bonuses.")
            ],
            ["martial_art_book_templates"] =
            [
                Check("Martial art book item", "select count(*) from public.item_templates where item_type = 5;", "Can tao it nhat 1 Item Template co loai MartialArtBook truoc khi them Martial Art Books."),
                Check("Martial art", "select count(*) from public.martial_arts;", "Can tao it nhat 1 Cong Phap truoc khi them Martial Art Books.")
            ],
            ["craft_recipe_requirements"] =
            [
                Check("Craft recipe", "select count(*) from public.craft_recipes;", "Can tao it nhat 1 Craft Recipe truoc khi them Craft Requirements."),
                Check("Item template", "select count(*) from public.item_templates;", "Can tao it nhat 1 Item Template truoc khi them Craft Requirements.")
            ],
            ["craft_recipe_mutation_bonuses"] =
            [
                Check("Craft recipe", "select count(*) from public.craft_recipes;", "Can tao it nhat 1 Craft Recipe truoc khi them Craft Mutation Bonuses.")
            ],
            ["pill_templates"] =
            [
                Check("Pill item template", "select count(*) from public.item_templates where item_type in (2, 10);", "Can tao Item Template cho dan duoc truoc khi them Pill Templates. Goi y: item_type = Consumable hoac HerbMaterial.")
            ],
            ["pill_effects"] =
            [
                Check("Pill template", "select count(*) from public.pill_templates;", "Can tao it nhat 1 Pill Template truoc khi them Pill Effects.")
            ],
            ["pill_recipe_templates"] =
            [
                Check("Recipe book item", "select count(*) from public.item_templates where item_type = 8;", "Can tao it nhat 1 Item Template co loai PillRecipeBook truoc khi them Pill Recipe Templates."),
                Check("Result pill item", "select count(*) from public.item_templates where item_type in (2, 10);", "Can tao Item Template cho vien dan ket qua truoc khi them Pill Recipe Templates.")
            ],
            ["pill_recipe_inputs"] =
            [
                Check("Pill recipe", "select count(*) from public.pill_recipe_templates;", "Can tao it nhat 1 Pill Recipe Template truoc khi them Pill Recipe Inputs."),
                Check("Item template", "select count(*) from public.item_templates;", "Can tao it nhat 1 Item Template truoc khi them Pill Recipe Inputs.")
            ],
            ["pill_recipe_mastery_stages"] =
            [
                Check("Pill recipe", "select count(*) from public.pill_recipe_templates;", "Can tao it nhat 1 Pill Recipe Template truoc khi them Pill Recipe Mastery.")
            ],
            ["soil_templates"] =
            [
                Check("Soil item template", "select count(*) from public.item_templates where item_type = 11;", "Can tao it nhat 1 Item Template co loai Soil truoc khi them Soil Templates.")
            ],
            ["herb_templates"] =
            [
                Check("Herb seed item template", "select count(*) from public.item_templates where item_type = 9;", "Can tao it nhat 1 Item Template co loai HerbSeed truoc khi them Herb Templates."),
                Check("Herb plant item template", "select count(*) from public.item_templates where item_type = 12;", "Can tao it nhat 1 Item Template co loai HerbPlant truoc khi them Herb Templates de support nho cay non/cay song vao tui roi trong lai.")
            ],
            ["herb_growth_stage_configs"] =
            [
                Check("Herb template", "select count(*) from public.herb_templates;", "Can tao it nhat 1 Herb Template truoc khi them Herb Growth Stages.")
            ],
            ["herb_harvest_outputs"] =
            [
                Check("Herb template", "select count(*) from public.herb_templates;", "Can tao it nhat 1 Herb Template truoc khi them Herb Harvest Outputs.")
            ],
            ["game_random_entries"] =
            [
                Check("Game random table", "select count(*) from public.game_random_tables;", "Can tao it nhat 1 Game Random Table truoc khi them Game Random Entries.")
            ],
            ["game_random_entry_tags"] =
            [
                Check("Game random entry", "select count(*) from public.game_random_entries;", "Can tao it nhat 1 Game Random Entry truoc khi them Game Random Entry Tags.")
            ],
            ["game_random_fortune_tags"] =
            [
                Check("Game random table", "select count(*) from public.game_random_tables;", "Can tao it nhat 1 Game Random Table truoc khi them Game Random Fortune Tags.")
            ],
            ["enemy_template_skills"] =
            [
                Check("Enemy template", "select count(*) from public.enemy_templates;", "Can tao it nhat 1 Enemy Template truoc khi them Enemy Skills."),
                Check("Skill", "select count(*) from public.skills;", "Can tao it nhat 1 Skill truoc khi them Enemy Skills.")
            ],
            ["enemy_reward_rules"] =
            [
                Check("Enemy template", "select count(*) from public.enemy_templates;", "Can tao it nhat 1 Enemy Template truoc khi them Enemy Reward Rules."),
                Check("Random table", "select count(*) from public.game_random_tables;", "Can tao it nhat 1 Game Random Table truoc khi them Enemy Reward Rules.")
            ],
            ["map_enemy_spawn_groups"] =
            [
                Check("Map template", "select count(*) from public.map_templates;", "Can tao it nhat 1 Map Template truoc khi them Enemy Spawn Groups.")
            ],
            ["map_enemy_spawn_entries"] =
            [
                Check("Spawn group", "select count(*) from public.map_enemy_spawn_groups;", "Can tao it nhat 1 Enemy Spawn Group truoc khi them Spawn Entries."),
                Check("Enemy template", "select count(*) from public.enemy_templates;", "Can tao it nhat 1 Enemy Template truoc khi them Spawn Entries.")
            ],
            ["map_instance_configs"] =
            [
                Check("Map template", "select count(*) from public.map_templates;", "Can tao it nhat 1 Map Template truoc khi them Map Instance Configs.")
            ],
            ["map_zone_slots"] =
            [
                Check("Map template", "select count(*) from public.map_templates;", "Can tao it nhat 1 Map Template truoc khi them Map Zone Slots."),
                Check("Spiritual energy template", "select count(*) from public.spiritual_energy_templates;", "Can tao it nhat 1 Spiritual Energy Template truoc khi them Map Zone Slots.")
            ]
        };
    }
}

namespace AdminDesignerTool;

internal sealed class MasterDetailWorkspaceControl : UserControl
{
    private readonly IReadOnlyDictionary<string, AdminResourceDefinition> _resourcesByKey;
    private readonly Label _titleLabel;
    private readonly Label _descriptionLabel;
    private readonly TextBox _helpTextBox;
    private readonly SplitContainer _splitContainer;
    private readonly TableEditorControl _masterEditor;
    private readonly TabControl _detailTabs;
    private readonly Dictionary<string, TableEditorControl> _detailEditors;

    private WorkspaceDefinition? _definition;

    public MasterDetailWorkspaceControl(
        string connectionString,
        IReadOnlyDictionary<string, AdminResourceDefinition> resourcesByKey)
    {
        _resourcesByKey = resourcesByKey;
        _detailEditors = new Dictionary<string, TableEditorControl>(StringComparer.OrdinalIgnoreCase);

        Dock = DockStyle.Fill;
        Padding = new Padding(12);

        _titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            Text = "Designer Workspace"
        };

        _descriptionLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Color.DimGray,
            AutoEllipsis = true
        };

        _helpTextBox = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 82,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.WhiteSmoke,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular)
        };

        _splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal
        };

        _masterEditor = new TableEditorControl(connectionString, showResourceHelpBox: false);
        _masterEditor.SelectedRowChanged += MasterEditorOnSelectedRowChanged;

        _detailTabs = new TabControl
        {
            Dock = DockStyle.Fill
        };

        _splitContainer.Panel1.Controls.Add(_masterEditor);
        _splitContainer.Panel2.Controls.Add(_detailTabs);

        Controls.Add(_splitContainer);
        Controls.Add(_descriptionLabel);
        Controls.Add(_titleLabel);
    }

    public async Task LoadWorkspaceAsync(AdminResourceDefinition workspaceResource)
    {
        _definition = WorkspaceDefinitionCatalog.Build(workspaceResource.Key, _resourcesByKey);
        _titleLabel.Text = workspaceResource.DisplayName;
        _descriptionLabel.Text = workspaceResource.Description;
        _helpTextBox.Text = workspaceResource.HelpText;

        BuildDetailTabs(_definition);
        _splitContainer.SplitterDistance = Math.Max(250, Math.Min(360, Height / 2));

        await _masterEditor.LoadRequestAsync(_definition.MasterRequest);
        await RefreshDetailsAsync();
    }

    private async void MasterEditorOnSelectedRowChanged(object? sender, EventArgs e)
    {
        await RefreshDetailsAsync();
    }

    private void BuildDetailTabs(WorkspaceDefinition definition)
    {
        _detailTabs.TabPages.Clear();
        _detailEditors.Clear();

        foreach (var child in definition.Children)
        {
            var editor = new TableEditorControl(_masterEditor.ConnectionString, showResourceHelpBox: false);
            _detailEditors[child.TabTitle] = editor;

            var page = new TabPage(child.TabTitle);
            page.Controls.Add(editor);
            _detailTabs.TabPages.Add(page);
        }
    }

    private async Task RefreshDetailsAsync()
    {
        if (_definition is null)
            return;

        if (!_masterEditor.TryGetSelectedInt("id", out var parentId))
        {
            foreach (var child in _definition.Children)
            {
                await _detailEditors[child.TabTitle].LoadRequestAsync(child.BuildEmptyRequest());
            }

            return;
        }

        foreach (var child in _definition.Children)
        {
            await _detailEditors[child.TabTitle].LoadRequestAsync(child.BuildRequest(parentId));
        }
    }

    private sealed record WorkspaceDefinition(
        AdminTableLoadRequest MasterRequest,
        IReadOnlyList<WorkspaceChildDefinition> Children);

    private sealed record WorkspaceChildDefinition(
        string TabTitle,
        Func<int, AdminTableLoadRequest> BuildRequest,
        Func<AdminTableLoadRequest> BuildEmptyRequest);

    private static class WorkspaceDefinitionCatalog
    {
        public static WorkspaceDefinition Build(
            string workspaceKey,
            IReadOnlyDictionary<string, AdminResourceDefinition> resourcesByKey)
        {
            return workspaceKey switch
            {
                "martial_art_workspace" => BuildMartialArtWorkspace(resourcesByKey),
                "craft_recipe_workspace" => BuildCraftRecipeWorkspace(resourcesByKey),
                "equipment_workspace" => BuildEquipmentWorkspace(resourcesByKey),
                "map_workspace" => BuildMapWorkspace(resourcesByKey),
                "pill_recipe_workspace" => BuildPillRecipeWorkspace(resourcesByKey),
                "pill_workspace" => BuildPillWorkspace(resourcesByKey),
                "herb_workspace" => BuildHerbWorkspace(resourcesByKey),
                "game_random_workspace" => BuildGameRandomWorkspace(resourcesByKey),
                _ => throw new InvalidOperationException($"Workspace {workspaceKey} is not supported.")
            };
        }

        private static WorkspaceDefinition BuildMartialArtWorkspace(
            IReadOnlyDictionary<string, AdminResourceDefinition> resourcesByKey)
        {
            var martialArts = resourcesByKey["martial_arts"];
            var stages = resourcesByKey["martial_art_stages"];
            var skillUnlocks = resourcesByKey["martial_art_skills"];

            return new WorkspaceDefinition(
                new AdminTableLoadRequest(
                    martialArts,
                    TitleOverride: "Danh Sach Cong Phap",
                    HelpTextOverride: "Bang cha de game design tao va chon cong phap. Chon 1 dong de edit tang va skill unlock ben duoi."),
                [
                    new WorkspaceChildDefinition(
                        "Tang Cong Phap",
                        parentId => new AdminTableLoadRequest(
                            stages,
                            SelectSql: $"""
                                select *
                                from public.martial_art_stages
                                where martial_art_id = {parentId}
                                order by stage_level;
                                """,
                            DescriptionOverride: $"Chi hien thi cac tang cua cong phap id = {parentId}.",
                            HelpTextOverride: "Khi bam Them Dong, tool se tu dien martial_art_id theo cong phap dang chon.",
                            NewRowDefaults: new Dictionary<string, object?> { ["martial_art_id"] = parentId }),
                        () => BuildEmptyRequest(stages, "Chon mot cong phap o bang tren de xem cac tang.")),
                    new WorkspaceChildDefinition(
                        "Skill Unlocks",
                        parentId => new AdminTableLoadRequest(
                            skillUnlocks,
                            SelectSql: $"""
                                select *
                                from public.martial_art_skills
                                where martial_art_id = {parentId}
                                order by unlock_stage, id;
                                """,
                            DescriptionOverride: $"Chi hien thi skill unlock cua cong phap id = {parentId}.",
                            HelpTextOverride: "Khi bam Them Dong, tool se tu dien martial_art_id theo cong phap dang chon.",
                            NewRowDefaults: new Dictionary<string, object?> { ["martial_art_id"] = parentId }),
                        () => BuildEmptyRequest(skillUnlocks, "Chon mot cong phap o bang tren de xem skill unlock.")),
                ]);
        }

        private static WorkspaceDefinition BuildCraftRecipeWorkspace(
            IReadOnlyDictionary<string, AdminResourceDefinition> resourcesByKey)
        {
            var recipes = resourcesByKey["craft_recipes"];
            var requirements = resourcesByKey["craft_recipe_requirements"];
            var mutationBonuses = resourcesByKey["craft_recipe_mutation_bonuses"];

            return new WorkspaceDefinition(
                new AdminTableLoadRequest(
                    recipes,
                    TitleOverride: "Danh Sach Craft Recipe",
                    HelpTextOverride: "Bang cha de game design tao va chon recipe. Chon 1 dong de edit requirement va mutation bonus."),
                [
                    new WorkspaceChildDefinition(
                        "Nguyen lieu",
                        parentId => new AdminTableLoadRequest(
                            requirements,
                            SelectSql: $"""
                                select *
                                from public.craft_recipe_requirements
                                where craft_recipe_id = {parentId}
                                order by id;
                                """,
                            DescriptionOverride: $"Chi hien thi requirement cua recipe id = {parentId}.",
                            HelpTextOverride: "Khi bam Them Dong, tool se tu dien craft_recipe_id theo recipe dang chon.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["craft_recipe_id"] = parentId,
                                ["required_quantity"] = 1,
                                ["consume_mode"] = 1,
                                ["is_optional"] = false,
                                ["mutation_bonus_rate"] = 0d
                            }),
                        () => BuildEmptyRequest(requirements, "Chon mot recipe o bang tren de xem requirement.")),
                    new WorkspaceChildDefinition(
                        "Mutation Bonuses",
                        parentId => new AdminTableLoadRequest(
                            mutationBonuses,
                            SelectSql: $"""
                                select *
                                from public.craft_recipe_mutation_bonuses
                                where craft_recipe_id = {parentId}
                                order by id;
                                """,
                            DescriptionOverride: $"Chi hien thi mutation bonus cua recipe id = {parentId}.",
                            HelpTextOverride: "Khi bam Them Dong, tool se tu dien craft_recipe_id theo recipe dang chon.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["craft_recipe_id"] = parentId,
                                ["stat_type"] = 1,
                                ["value_type"] = 1
                            }),
                        () => BuildEmptyRequest(mutationBonuses, "Chon mot recipe o bang tren de xem mutation bonus.")),
                ]);
        }

        private static WorkspaceDefinition BuildEquipmentWorkspace(
            IReadOnlyDictionary<string, AdminResourceDefinition> resourcesByKey)
        {
            var itemTemplates = resourcesByKey["item_templates"];
            var equipmentTemplates = resourcesByKey["equipment_templates"];
            var equipmentStats = resourcesByKey["equipment_template_stats"];

            return new WorkspaceDefinition(
                new AdminTableLoadRequest(
                    itemTemplates,
                    SelectSql: """
                        select *
                        from public.item_templates
                        where item_type = 1
                        order by id;
                        """,
                    TitleOverride: "Equipment Item Templates",
                    DescriptionOverride: "Chi hien thi item template co item_type = Equipment.",
                    HelpTextOverride: "Bang cha de tao item template cho trang bi. Chon 1 dong de edit equipment core va base stat ben duoi.",
                    NewRowDefaults: new Dictionary<string, object?>
                    {
                        ["item_type"] = 1,
                        ["rarity"] = 1,
                        ["max_stack"] = 1,
                        ["is_tradeable"] = true,
                        ["is_droppable"] = true,
                        ["is_destroyable"] = true
                    }),
                [
                    new WorkspaceChildDefinition(
                        "Equipment Core",
                        parentId => new AdminTableLoadRequest(
                            equipmentTemplates,
                            SelectSql: $"""
                                select *
                                from public.equipment_templates
                                where item_template_id = {parentId};
                                """,
                            DescriptionOverride: $"Chi hien thi equipment core cua item_template_id = {parentId}.",
                            HelpTextOverride: "Moi item template equipment se co toi da 1 dong core. Them dong moi se tu dien item_template_id.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["item_template_id"] = parentId,
                                ["slot_type"] = 1,
                                ["equipment_type"] = 1,
                                ["level_requirement"] = 1
                            }),
                        () => BuildEmptyRequest(equipmentTemplates, "Chon mot item template equipment o bang tren de xem equipment core.")),
                    new WorkspaceChildDefinition(
                        "Base Stats",
                        parentId => new AdminTableLoadRequest(
                            equipmentStats,
                            SelectSql: $"""
                                select ets.*
                                from public.equipment_template_stats ets
                                where ets.equipment_template_id = {parentId}
                                order by ets.id;
                                """,
                            DescriptionOverride: $"Chi hien thi base stat cua equipment item_template_id = {parentId}.",
                            HelpTextOverride: "Khi bam Them Dong, tool se tu dien equipment_template_id theo item template dang chon.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["equipment_template_id"] = parentId,
                                ["stat_type"] = 1,
                                ["value_type"] = 1
                            }),
                        () => BuildEmptyRequest(equipmentStats, "Chon mot item template equipment o bang tren de xem base stat.")),
                ]);
        }

        private static WorkspaceDefinition BuildMapWorkspace(
            IReadOnlyDictionary<string, AdminResourceDefinition> resourcesByKey)
        {
            var maps = resourcesByKey["map_templates"];
            var zoneSlots = resourcesByKey["map_zone_slots"];

            return new WorkspaceDefinition(
                new AdminTableLoadRequest(
                    maps,
                    TitleOverride: "Danh Sach Map",
                    HelpTextOverride: "Bang cha de tao va chon map. Chon 1 dong de sua cac zone slot ben duoi."),
                [
                    new WorkspaceChildDefinition(
                        "Zone Slots",
                        parentId => new AdminTableLoadRequest(
                            zoneSlots,
                            SelectSql: $"""
                                select *
                                from public.map_zone_slots
                                where map_template_id = {parentId}
                                order by zone_index;
                                """,
                            DescriptionOverride: $"Chỉ hiển thị zone slot của map id = {parentId}.",
                            HelpTextOverride: "Khi bấm Thêm Dòng, tool sẽ tự điền map_template_id theo map đang chọn.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["map_template_id"] = parentId
                            }),
                        () => BuildEmptyRequest(zoneSlots, "Chọn một map ở bảng trên để xem zone slot.")),
                ]);
        }

        private static WorkspaceDefinition BuildPillRecipeWorkspace(
            IReadOnlyDictionary<string, AdminResourceDefinition> resourcesByKey)
        {
            var recipes = resourcesByKey["pill_recipe_templates"];
            var inputs = resourcesByKey["pill_recipe_inputs"];
            var masteryStages = resourcesByKey["pill_recipe_mastery_stages"];

            return new WorkspaceDefinition(
                new AdminTableLoadRequest(
                    recipes,
                    TitleOverride: "Danh Sach Dan Phuong",
                    HelpTextOverride: "Bang cha de tao va chon dan phuong. Chon 1 dong de sua input va mastery ben duoi."),
                [
                    new WorkspaceChildDefinition(
                        "Nguyen Lieu",
                        parentId => new AdminTableLoadRequest(
                            inputs,
                            SelectSql: $"""
                                select *
                                from public.pill_recipe_inputs
                                where pill_recipe_template_id = {parentId}
                                order by id;
                                """,
                            DescriptionOverride: $"Chỉ hiển thị nguyên liệu của đan phương id = {parentId}.",
                            HelpTextOverride: "Khi bấm Thêm Dòng, tool sẽ tự điền pill_recipe_template_id theo đan phương đang chọn.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["pill_recipe_template_id"] = parentId,
                                ["required_quantity"] = 1,
                                ["is_optional"] = false,
                                ["success_rate_bonus"] = 0d,
                                ["mutation_bonus_rate"] = 0d
                            }),
                        () => BuildEmptyRequest(inputs, "Chọn một đan phương ở bảng trên để xem nguyên liệu.")),
                    new WorkspaceChildDefinition(
                        "Mastery",
                        parentId => new AdminTableLoadRequest(
                            masteryStages,
                            SelectSql: $"""
                                select *
                                from public.pill_recipe_mastery_stages
                                where pill_recipe_template_id = {parentId}
                                order by required_total_craft_count, id;
                                """,
                            DescriptionOverride: $"Chỉ hiển thị mốc mastery của đan phương id = {parentId}.",
                            HelpTextOverride: "Khi bấm Thêm Dòng, tool sẽ tự điền pill_recipe_template_id theo đan phương đang chọn.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["pill_recipe_template_id"] = parentId,
                                ["required_total_craft_count"] = 10,
                                ["success_rate_bonus"] = 0d
                            }),
                        () => BuildEmptyRequest(masteryStages, "Chọn một đan phương ở bảng trên để xem các mốc mastery.")),
                ]);
        }

        private static WorkspaceDefinition BuildPillWorkspace(
            IReadOnlyDictionary<string, AdminResourceDefinition> resourcesByKey)
        {
            var pills = resourcesByKey["pill_templates"];
            var effects = resourcesByKey["pill_effects"];

            return new WorkspaceDefinition(
                new AdminTableLoadRequest(
                    pills,
                    TitleOverride: "Danh Sach Pill",
                    HelpTextOverride: "Bang cha de tao va chon pill template. Chon 1 dong de sua cac effect ben duoi."),
                [
                    new WorkspaceChildDefinition(
                        "Effects",
                        parentId => new AdminTableLoadRequest(
                            effects,
                            SelectSql: $"""
                                select *
                                from public.pill_effects
                                where pill_template_id = {parentId}
                                order by order_index, id;
                                """,
                            DescriptionOverride: $"Chỉ hiển thị effect của pill id = {parentId}.",
                            HelpTextOverride: "Khi bấm Thêm Dòng, tool sẽ tự điền pill_template_id theo pill đang chọn.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["pill_template_id"] = parentId,
                                ["order_index"] = 1
                            }),
                        () => BuildEmptyRequest(effects, "Chọn một pill template ở bảng trên để xem effect.")),
                ]);
        }

        private static WorkspaceDefinition BuildHerbWorkspace(
            IReadOnlyDictionary<string, AdminResourceDefinition> resourcesByKey)
        {
            var herbs = resourcesByKey["herb_templates"];
            var growthStages = resourcesByKey["herb_growth_stage_configs"];
            var harvestOutputs = resourcesByKey["herb_harvest_outputs"];

            return new WorkspaceDefinition(
                new AdminTableLoadRequest(
                    herbs,
                    TitleOverride: "Danh Sach Herb",
                    HelpTextOverride: "Bang cha de tao va chon herb template. Chon 1 dong de sua growth stage va harvest output ben duoi."),
                [
                    new WorkspaceChildDefinition(
                        "Growth Stages",
                        parentId => new AdminTableLoadRequest(
                            growthStages,
                            SelectSql: $"""
                                select *
                                from public.herb_growth_stage_configs
                                where herb_template_id = {parentId}
                                order by required_growth_seconds, id;
                                """,
                            DescriptionOverride: $"Chỉ hiển thị growth stage của herb id = {parentId}.",
                            HelpTextOverride: "Khi bấm Thêm Dòng, tool sẽ tự điền herb_template_id theo herb đang chọn.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["herb_template_id"] = parentId,
                                ["required_growth_seconds"] = 0
                            }),
                        () => BuildEmptyRequest(growthStages, "Chọn một herb template ở bảng trên để xem growth stage.")),
                    new WorkspaceChildDefinition(
                        "Harvest Outputs",
                        parentId => new AdminTableLoadRequest(
                            harvestOutputs,
                            SelectSql: $"""
                                select *
                                from public.herb_harvest_outputs
                                where herb_template_id = {parentId}
                                order by required_stage, id;
                                """,
                            DescriptionOverride: $"Chỉ hiển thị harvest output của herb id = {parentId}.",
                            HelpTextOverride: "Khi bấm Thêm Dòng, tool sẽ tự điền herb_template_id theo herb đang chọn.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["herb_template_id"] = parentId,
                                ["required_stage"] = 1,
                                ["result_quantity"] = 1,
                                ["output_chance"] = 1d
                            }),
                        () => BuildEmptyRequest(harvestOutputs, "Chọn một herb template ở bảng trên để xem harvest output.")),
                ]);
        }

        private static WorkspaceDefinition BuildGameRandomWorkspace(
            IReadOnlyDictionary<string, AdminResourceDefinition> resourcesByKey)
        {
            var tables = resourcesByKey["game_random_tables"];
            var entries = resourcesByKey["game_random_entries"];
            var fortuneTags = resourcesByKey["game_random_fortune_tags"];

            return new WorkspaceDefinition(
                new AdminTableLoadRequest(
                    tables,
                    TitleOverride: "Danh Sach Random Table",
                    HelpTextOverride: "Bang cha de tao va chon random table. Chon 1 dong de sua entries va fortune tags ben duoi."),
                [
                    new WorkspaceChildDefinition(
                        "Entries",
                        parentId => new AdminTableLoadRequest(
                            entries,
                            SelectSql: $"""
                                select *
                                from public.game_random_entries
                                where game_random_table_id = {parentId}
                                order by order_index, id;
                                """,
                            DescriptionOverride: $"Chỉ hiển thị entries của random table id = {parentId}.",
                            HelpTextOverride: "Khi bấm Thêm Dòng, tool sẽ tự điền game_random_table_id theo bảng random đang chọn.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["game_random_table_id"] = parentId,
                                ["order_index"] = 1,
                                ["chance_parts_per_million"] = 100000,
                                ["is_none"] = false
                            }),
                        () => BuildEmptyRequest(entries, "Chọn một random table ở bảng trên để xem entries.")),
                    new WorkspaceChildDefinition(
                        "Fortune Tags",
                        parentId => new AdminTableLoadRequest(
                            fortuneTags,
                            SelectSql: $"""
                                select *
                                from public.game_random_fortune_tags
                                where game_random_table_id = {parentId}
                                order by id;
                                """,
                            DescriptionOverride: $"Chỉ hiển thị fortune tags của random table id = {parentId}.",
                            HelpTextOverride: "Khi bấm Thêm Dòng, tool sẽ tự điền game_random_table_id theo bảng random đang chọn.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["game_random_table_id"] = parentId
                            }),
                        () => BuildEmptyRequest(fortuneTags, "Chọn một random table ở bảng trên để xem fortune tags.")),
                ]);
        }

        private static AdminTableLoadRequest BuildEmptyRequest(AdminResourceDefinition resource, string helpText)
        {
            return new AdminTableLoadRequest(
                resource,
                SelectSql: $"select * from public.{resource.TableName} where 1 = 0;",
                DescriptionOverride: "Chưa có bản ghi cha đang được chọn.",
                HelpTextOverride: helpText);
        }
    }
}

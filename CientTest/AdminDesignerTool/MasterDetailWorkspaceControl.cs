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
            Height = 34,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            Text = "Designer Workspace"
        };

        _descriptionLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 52,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = Color.DimGray
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

        _masterEditor = new TableEditorControl(connectionString);
        _masterEditor.SelectedRowChanged += MasterEditorOnSelectedRowChanged;

        _detailTabs = new TabControl
        {
            Dock = DockStyle.Fill
        };

        _splitContainer.Panel1.Controls.Add(_masterEditor);
        _splitContainer.Panel2.Controls.Add(_detailTabs);

        Controls.Add(_splitContainer);
        Controls.Add(_helpTextBox);
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
            var editor = new TableEditorControl(_masterEditor.ConnectionString);
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

        private static AdminTableLoadRequest BuildEmptyRequest(AdminResourceDefinition resource, string helpText)
        {
            return new AdminTableLoadRequest(
                resource,
                SelectSql: $"select * from public.{resource.TableName} where 1 = 0;",
                HelpTextOverride: helpText);
        }
    }
}

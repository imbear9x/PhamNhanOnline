using System.Data;
using System.Globalization;
using Npgsql;

namespace AdminDesignerTool;

internal sealed class MasterDetailWorkspaceControl : UserControl
{
    private const string MapWorkspaceKey = "map_workspace";

    private readonly IReadOnlyDictionary<string, AdminResourceDefinition> _resourcesByKey;
    private readonly Label _titleLabel;
    private readonly Label _descriptionLabel;
    private readonly TextBox _helpTextBox;
    private readonly Panel _workspaceActionPanel;
    private readonly Label _cloneSourceLabel;
    private readonly ComboBox _cloneSourceComboBox;
    private readonly Button _cloneMapButton;
    private readonly Label _workspaceActionHintLabel;
    private readonly SplitContainer _splitContainer;
    private readonly TableEditorControl _masterEditor;
    private readonly TabControl _detailTabs;
    private readonly Dictionary<string, TableEditorControl> _detailEditors;

    private WorkspaceDefinition? _definition;
    private string _activeWorkspaceKey = string.Empty;
    private int _workspaceRevision;
    private bool _suspendMasterSelectionChanged;
    private bool _isCloningMapTopology;

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

        _workspaceActionPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            Visible = false
        };

        var workspaceActionFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            AutoSize = false,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        _cloneSourceLabel = new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 8, 8, 0),
            Text = "Clone tu map:"
        };

        _cloneSourceComboBox = new ComboBox
        {
            Width = 320,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        _cloneMapButton = new Button
        {
            AutoSize = true,
            Text = "Clone Spawn Points + Portals"
        };
        _cloneMapButton.Click += CloneMapButtonOnClick;

        workspaceActionFlow.Controls.Add(_cloneSourceLabel);
        workspaceActionFlow.Controls.Add(_cloneSourceComboBox);
        workspaceActionFlow.Controls.Add(_cloneMapButton);

        _workspaceActionHintLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.DimGray,
            Padding = new Padding(0, 6, 0, 0),
            Text = "Clone se copy spawn point va portal tu map mau sang map dang chon. Spawn point se duoc cap nhat theo code de giu lien ket inbound an toan hon; portal cua map hien tai se duoc thay bang map mau."
        };

        _workspaceActionPanel.Controls.Add(_workspaceActionHintLabel);
        _workspaceActionPanel.Controls.Add(workspaceActionFlow);

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
        Controls.Add(_workspaceActionPanel);
        Controls.Add(_descriptionLabel);
        Controls.Add(_titleLabel);
    }

    public async Task LoadWorkspaceAsync(AdminResourceDefinition workspaceResource)
    {
        var revision = unchecked(++_workspaceRevision);
        _suspendMasterSelectionChanged = true;
        _activeWorkspaceKey = workspaceResource.Key;

        try
        {
            _definition = WorkspaceDefinitionCatalog.Build(workspaceResource.Key, _resourcesByKey);
            _titleLabel.Text = workspaceResource.DisplayName;
            _descriptionLabel.Text = workspaceResource.Description;
            _helpTextBox.Text = workspaceResource.HelpText;
            _workspaceActionPanel.Visible = string.Equals(workspaceResource.Key, MapWorkspaceKey, StringComparison.Ordinal);

            BuildDetailTabs(_definition);
            _splitContainer.SplitterDistance = Math.Max(250, Math.Min(360, Height / 2));

            await _masterEditor.LoadRequestAsync(_definition.MasterRequest);
            await RefreshWorkspaceActionsAsync();
        }
        finally
        {
            _suspendMasterSelectionChanged = false;
        }

        await RefreshDetailsAsync(revision);
    }

    private async void MasterEditorOnSelectedRowChanged(object? sender, EventArgs e)
    {
        if (_suspendMasterSelectionChanged)
            return;

        var revision = _workspaceRevision;
        await RefreshWorkspaceActionsAsync();
        await RefreshDetailsAsync(revision);
    }

    private async void CloneMapButtonOnClick(object? sender, EventArgs e)
    {
        if (_isCloningMapTopology)
            return;

        if (!string.Equals(_activeWorkspaceKey, MapWorkspaceKey, StringComparison.Ordinal))
            return;

        if (!TryGetSelectedMapTarget(out var targetMap))
        {
            MessageBox.Show(
                "Hay chon map dich o bang tren truoc khi clone.",
                "Chua chon map dich",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (targetMap.RowState == DataRowState.Added)
        {
            MessageBox.Show(
                "Map dich dang la dong moi chua luu. Hay luu map truoc khi clone spawn point va portal.",
                "Can luu map truoc",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (_cloneSourceComboBox.SelectedItem is not MapCloneOption sourceMap)
        {
            MessageBox.Show(
                "Hay chon map nguon de clone.",
                "Chua chon map nguon",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (sourceMap.MapId == targetMap.MapId)
        {
            MessageBox.Show(
                "Map nguon va map dich dang trung nhau. Hay chon map khac de clone.",
                "Map clone khong hop le",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var sourceSummary = await LoadMapCloneSummaryAsync(sourceMap.MapId);
        var targetSummary = await LoadMapCloneSummaryAsync(targetMap.MapId);

        var confirmMessage =
            $"Clone tu map '{sourceMap.DisplayName}' sang map '{targetMap.DisplayName}'?\r\n\r\n" +
            $"- Map mau hien co {sourceSummary.SpawnPointCount} spawn point va {sourceSummary.PortalCount} portal.\r\n" +
            $"- Map dich hien co {targetSummary.SpawnPointCount} spawn point va {targetSummary.PortalCount} portal.\r\n\r\n" +
            "Tool se cap nhat/chen spawn point theo code, thay toan bo portal xuat phat tu map dich theo map mau, va ghi ngay vao DB. " +
            "Sau do workspace se reload de ban chinh sua nhe thong so.";

        if (MessageBox.Show(
                confirmMessage,
                "Xac nhan clone map topology",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question) != DialogResult.OK)
        {
            return;
        }

        try
        {
            SetMapCloneBusy(true);
            await CloneMapTopologyAsync(sourceMap, targetMap);
            await RefreshWorkspaceActionsAsync();
            await RefreshDetailsAsync(_workspaceRevision);

            MessageBox.Show(
                $"Da clone {sourceSummary.SpawnPointCount} spawn point va {sourceSummary.PortalCount} portal tu '{sourceMap.DisplayName}' sang '{targetMap.DisplayName}'.",
                "Clone thanh cong",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Khong the clone map topology.\r\n\r\n{ex.Message}",
                "Clone that bai",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetMapCloneBusy(false);
        }
    }

    private async Task RefreshWorkspaceActionsAsync()
    {
        if (!string.Equals(_activeWorkspaceKey, MapWorkspaceKey, StringComparison.Ordinal))
        {
            _cloneSourceComboBox.DataSource = null;
            SetMapCloneBusy(false);
            _cloneMapButton.Enabled = false;
            return;
        }

        var selectedTargetMapId = TryGetSelectedMapTarget(out var targetMap)
            ? targetMap.MapId
            : (int?)null;
        var selectedSourceMapId = (_cloneSourceComboBox.SelectedItem as MapCloneOption)?.MapId;

        var cloneOptions = await LoadMapCloneOptionsAsync(selectedTargetMapId);
        _cloneSourceComboBox.BeginUpdate();
        try
        {
            _cloneSourceComboBox.DataSource = cloneOptions;
            if (cloneOptions.Count == 0)
            {
                _cloneSourceComboBox.SelectedItem = null;
            }
            else if (selectedSourceMapId.HasValue)
            {
                var preferredOption = cloneOptions.FirstOrDefault(option => option.MapId == selectedSourceMapId.Value);
                _cloneSourceComboBox.SelectedItem = preferredOption ?? cloneOptions[0];
            }
            else
            {
                _cloneSourceComboBox.SelectedItem = cloneOptions[0];
            }
        }
        finally
        {
            _cloneSourceComboBox.EndUpdate();
        }

        _cloneMapButton.Enabled = !_isCloningMapTopology &&
                                  selectedTargetMapId.HasValue &&
                                  _cloneSourceComboBox.Items.Count > 0;
    }

    private async Task<List<MapCloneOption>> LoadMapCloneOptionsAsync(int? selectedTargetMapId)
    {
        var options = new List<MapCloneOption>();

        await using var connection = new NpgsqlConnection(_masterEditor.ConnectionString);
        await connection.OpenAsync();

        const string sql = """
            select id, name
            from public.map_templates
            where (@exclude_map_id is null or id <> @exclude_map_id)
            order by name, id;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("exclude_map_id", selectedTargetMapId.HasValue ? selectedTargetMapId.Value : DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var mapId = reader.GetInt32(0);
            var name = reader.IsDBNull(1) ? $"Map {mapId}" : reader.GetString(1);
            options.Add(new MapCloneOption(mapId, $"{name} (id={mapId})"));
        }

        return options;
    }

    private bool TryGetSelectedMapTarget(out MapCloneTarget target)
    {
        target = default;
        if (!_masterEditor.TryGetSelectedRow(out var row))
            return false;

        var mapId = Convert.ToInt32(row["id"], CultureInfo.InvariantCulture);
        var displayName = Convert.ToString(row["name"], CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = $"Map {mapId}";

        target = new MapCloneTarget(mapId, displayName, row.RowState);
        return true;
    }

    private async Task<MapCloneSummary> LoadMapCloneSummaryAsync(int mapId)
    {
        await using var connection = new NpgsqlConnection(_masterEditor.ConnectionString);
        await connection.OpenAsync();

        const string sql = """
            select
                (select count(*) from public.map_spawn_points where map_template_id = @map_id) as spawn_point_count,
                (select count(*) from public.map_portals where source_map_template_id = @map_id) as portal_count;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("map_id", mapId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return new MapCloneSummary(0, 0);

        return new MapCloneSummary(
            SpawnPointCount: reader.GetInt32(0),
            PortalCount: reader.GetInt32(1));
    }

    private async Task CloneMapTopologyAsync(MapCloneOption sourceMap, MapCloneTarget targetMap)
    {
        await using var connection = new NpgsqlConnection(_masterEditor.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        const string sql = """
            with source_spawn_points as (
                select
                    sp.id as source_spawn_point_id,
                    sp.code,
                    sp.name,
                    sp.spawn_category,
                    sp.pos_x,
                    sp.pos_y,
                    sp.facing_degrees,
                    sp.description
                from public.map_spawn_points sp
                where sp.map_template_id = @source_map_template_id
            ),
            upserted_spawn_points as (
                insert into public.map_spawn_points (
                    map_template_id,
                    code,
                    name,
                    spawn_category,
                    pos_x,
                    pos_y,
                    facing_degrees,
                    description
                )
                select
                    @target_map_template_id,
                    ssp.code,
                    ssp.name,
                    ssp.spawn_category,
                    ssp.pos_x,
                    ssp.pos_y,
                    ssp.facing_degrees,
                    ssp.description
                from source_spawn_points ssp
                on conflict (map_template_id, code) do update
                set
                    name = excluded.name,
                    spawn_category = excluded.spawn_category,
                    pos_x = excluded.pos_x,
                    pos_y = excluded.pos_y,
                    facing_degrees = excluded.facing_degrees,
                    description = excluded.description
                returning id, code
            ),
            spawn_point_map as (
                select
                    ssp.source_spawn_point_id,
                    target_sp.id as target_spawn_point_id
                from source_spawn_points ssp
                inner join public.map_spawn_points target_sp
                    on target_sp.map_template_id = @target_map_template_id
                   and target_sp.code = ssp.code
            ),
            deleted_target_portals as (
                delete from public.map_portals
                where source_map_template_id = @target_map_template_id
                returning id
            )
            insert into public.map_portals (
                source_map_template_id,
                code,
                name,
                source_x,
                source_y,
                interaction_radius,
                interaction_mode,
                target_map_template_id,
                target_spawn_point_id,
                is_enabled,
                order_index,
                description
            )
            select
                @target_map_template_id,
                portal.code,
                portal.name,
                portal.source_x,
                portal.source_y,
                portal.interaction_radius,
                portal.interaction_mode,
                case
                    when portal.target_map_template_id = @source_map_template_id then @target_map_template_id
                    else portal.target_map_template_id
                end,
                case
                    when portal.target_map_template_id = @source_map_template_id then spawn_point_map.target_spawn_point_id
                    else portal.target_spawn_point_id
                end,
                portal.is_enabled,
                portal.order_index,
                portal.description
            from public.map_portals portal
            left join spawn_point_map on spawn_point_map.source_spawn_point_id = portal.target_spawn_point_id
            where portal.source_map_template_id = @source_map_template_id
              and (
                  portal.target_map_template_id <> @source_map_template_id
                  or spawn_point_map.target_spawn_point_id is not null
              )
            order by portal.order_index, portal.id;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("source_map_template_id", sourceMap.MapId);
        command.Parameters.AddWithValue("target_map_template_id", targetMap.MapId);
        await command.ExecuteNonQueryAsync();

        await transaction.CommitAsync();
    }

    private void SetMapCloneBusy(bool isBusy)
    {
        _isCloningMapTopology = isBusy;
        _cloneSourceComboBox.Enabled = !isBusy;
        _cloneMapButton.Enabled = !isBusy &&
                                  string.Equals(_activeWorkspaceKey, MapWorkspaceKey, StringComparison.Ordinal) &&
                                  TryGetSelectedMapTarget(out _) &&
                                  _cloneSourceComboBox.Items.Count > 0;
        _masterEditor.Enabled = !isBusy;
        _detailTabs.Enabled = !isBusy;
    }

    private void BuildDetailTabs(WorkspaceDefinition definition)
    {
        _detailTabs.TabPages.Clear();
        _detailEditors.Clear();

        foreach (var child in definition.Children)
        {
            var editor = new TableEditorControl(_masterEditor.ConnectionString, showResourceHelpBox: false);
            editor.TemplateTokenPicked += DetailEditorOnTemplateTokenPicked;
            _detailEditors[child.TabTitle] = editor;

            var page = new TabPage(child.TabTitle);
            page.Controls.Add(editor);
            _detailTabs.TabPages.Add(page);
        }
    }

    private async Task RefreshDetailsAsync(int revision)
    {
        if (_definition is null || revision != _workspaceRevision)
            return;

        if (!_masterEditor.TryGetSelectedRow(out var parentRow))
        {
            foreach (var child in _definition.Children)
            {
                if (revision != _workspaceRevision)
                    return;

                await _detailEditors[child.TabTitle].LoadRequestAsync(child.BuildEmptyRequest());
            }

            return;
        }

        foreach (var child in _definition.Children)
        {
            if (revision != _workspaceRevision)
                return;

            await _detailEditors[child.TabTitle].LoadRequestAsync(child.BuildRequest(parentRow));
        }
    }

    private void DetailEditorOnTemplateTokenPicked(object? sender, TemplateTokenPickedEventArgs e)
    {
        if (!string.Equals(_activeWorkspaceKey, "skill_workspace", StringComparison.OrdinalIgnoreCase))
            return;

        if (!string.Equals(e.SourceTableName, "skill_effects", StringComparison.OrdinalIgnoreCase))
            return;

        _masterEditor.InsertIntoSelectedLongText("description_template", e.Token);
    }

    private sealed record WorkspaceDefinition(
        AdminTableLoadRequest MasterRequest,
        IReadOnlyList<WorkspaceChildDefinition> Children);

    private sealed record WorkspaceChildDefinition(
        string TabTitle,
        Func<DataRow, AdminTableLoadRequest> BuildRequest,
        Func<AdminTableLoadRequest> BuildEmptyRequest);

    private sealed record MapCloneOption(int MapId, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private readonly record struct MapCloneTarget(
        int MapId,
        string DisplayName,
        DataRowState RowState);

    private readonly record struct MapCloneSummary(
        int SpawnPointCount,
        int PortalCount);

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
                "map_workspace" => BuildMapWorkspaceV2(resourcesByKey),
                "pill_recipe_workspace" => BuildPillRecipeWorkspace(resourcesByKey),
                "pill_workspace" => BuildPillWorkspace(resourcesByKey),
                "skill_workspace" => BuildSkillWorkspace(resourcesByKey),
                "herb_workspace" => BuildHerbWorkspace(resourcesByKey),
                "game_random_workspace" => BuildGameRandomWorkspace(resourcesByKey),
                "enemy_workspace" => BuildEnemyWorkspace(resourcesByKey),
                "enemy_spawn_workspace" => BuildEnemySpawnWorkspace(resourcesByKey),
                "player_inventory_workspace" => BuildPlayerInventoryWorkspace(resourcesByKey),
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
                    SelectSql: """
                        select
                            id,
                            code,
                            name,
                            icon,
                            quality,
                            category,
                            description,
                            description_template,
                            qi_absorption_rate,
                            max_stage,
                            created_at
                        from public.martial_arts
                        order by id;
                        """,
                    TitleOverride: "Danh Sach Cong Phap",
                    HelpTextOverride: "Bang cha de game design tao va chon cong phap. Chon 1 dong de edit tang va skill unlock ben duoi. `description_template` la noi author mo ta hien thi runtime cho cong phap."),
                [
                    new WorkspaceChildDefinition(
                        "Tang Cong Phap",
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
                            stages,
                            SelectSql: $"""
                                select *
                                from public.martial_art_stages
                                where martial_art_id = {parentId}
                                order by stage_level;
                                """,
                            DescriptionOverride: $"Chi hien thi cac tang cua cong phap id = {parentId}.",
                            HelpTextOverride: "Khi bam Them Dong, tool se tu dien martial_art_id theo cong phap dang chon.",
                            NewRowDefaults: new Dictionary<string, object?> { ["martial_art_id"] = parentId });
                        },
                        () => BuildEmptyRequest(stages, "Chon mot cong phap o bang tren de xem cac tang.")),
                    new WorkspaceChildDefinition(
                        "Skill Unlocks",
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
                            skillUnlocks,
                            SelectSql: $"""
                                select *
                                from public.martial_art_skills
                                where martial_art_id = {parentId}
                                order by unlock_stage, id;
                                """,
                            DescriptionOverride: $"Chi hien thi skill unlock cua cong phap id = {parentId}.",
                            HelpTextOverride: "Khi bam Them Dong, tool se tu dien martial_art_id theo cong phap dang chon.",
                            NewRowDefaults: new Dictionary<string, object?> { ["martial_art_id"] = parentId });
                        },
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
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
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
                            });
                        },
                        () => BuildEmptyRequest(requirements, "Chon mot recipe o bang tren de xem requirement.")),
                    new WorkspaceChildDefinition(
                        "Mutation Bonuses",
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
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
                            });
                        },
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
                        select
                            id,
                            code,
                            name,
                            item_type,
                            rarity,
                            max_stack,
                            is_tradeable,
                            is_droppable,
                            is_destroyable,
                            icon,
                            background_icon,
                            description,
                            description_template,
                            created_at
                        from public.item_templates
                        where item_type = 1
                        order by id;
                        """,
                    TitleOverride: "Equipment Item Templates",
                    DescriptionOverride: "Chi hien thi item template co item_type = Equipment.",
                    HelpTextOverride: "Bang cha de tao item template cho trang bi. Chon 1 dong de edit equipment core va base stat ben duoi. `description_template` dung de author mo ta hien thi runtime cua item.",
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
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
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
                            });
                        },
                        () => BuildEmptyRequest(equipmentTemplates, "Chon mot item template equipment o bang tren de xem equipment core.")),
                    new WorkspaceChildDefinition(
                        "Base Stats",
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
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
                            });
                        },
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
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
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
                            });
                        },
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
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
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
                            });
                        },
                        () => BuildEmptyRequest(inputs, "Chọn một đan phương ở bảng trên để xem nguyên liệu.")),
                    new WorkspaceChildDefinition(
                        "Mastery",
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
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
                            });
                        },
                        () => BuildEmptyRequest(masteryStages, "Chọn một đan phương ở bảng trên để xem các mốc mastery.")),
                ]);
        }

        private static WorkspaceDefinition BuildMapWorkspaceV2(
            IReadOnlyDictionary<string, AdminResourceDefinition> resourcesByKey)
        {
            var maps = resourcesByKey["map_templates"];
            var zoneSlots = resourcesByKey["map_zone_slots"];
            var spawnPoints = resourcesByKey["map_spawn_points"];
            var portals = resourcesByKey["map_portals"];

            return new WorkspaceDefinition(
                new AdminTableLoadRequest(
                    maps,
                    TitleOverride: "Danh Sach Map",
                    HelpTextOverride: "Bang cha de tao va chon map. Chon 1 dong de sua zone slot, spawn point va portal ben duoi."),
                [
                    new WorkspaceChildDefinition(
                        "Zone Slots",
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
                            zoneSlots,
                            SelectSql: $"""
                                select *
                                from public.map_zone_slots
                                where map_template_id = {parentId}
                                order by zone_index;
                                """,
                            DescriptionOverride: $"Chi hien thi zone slot cua map id = {parentId}.",
                            HelpTextOverride: "Khi bam Them Dong, tool se tu dien map_template_id theo map dang chon.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["map_template_id"] = parentId
                            });
                        },
                        () => BuildEmptyRequest(zoneSlots, "Chon mot map o bang tren de xem zone slot.")),
                    new WorkspaceChildDefinition(
                        "Spawn Points",
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            var defaultSpawnX = GetOptionalSingle(parentRow, "default_spawn_x") ?? 0f;
                            var defaultSpawnY = GetOptionalSingle(parentRow, "default_spawn_y") ?? 0f;
                            return new AdminTableLoadRequest(
                            spawnPoints,
                            SelectSql: $"""
                                select *
                                from public.map_spawn_points
                                where map_template_id = {parentId}
                                order by id;
                                """,
                            DescriptionOverride: $"Chi hien thi spawn point cua map id = {parentId}.",
                            HelpTextOverride: "Spawn point la diem dat chan khi vao map. Khi bam Them Dong, tool se tu dien map_template_id va toa do mac dinh bang default spawn cua map.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["map_template_id"] = parentId,
                                ["spawn_category"] = 4,
                                ["pos_x"] = defaultSpawnX,
                                ["pos_y"] = defaultSpawnY
                            });
                        },
                        () => BuildEmptyRequest(spawnPoints, "Chon mot map o bang tren de xem spawn point.")),
                    new WorkspaceChildDefinition(
                        "Portals",
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            var defaultSpawnX = GetOptionalSingle(parentRow, "default_spawn_x") ?? 0f;
                            var defaultSpawnY = GetOptionalSingle(parentRow, "default_spawn_y") ?? 0f;
                            return new AdminTableLoadRequest(
                            portals,
                            SelectSql: $"""
                                select *
                                from public.map_portals
                                where source_map_template_id = {parentId}
                                order by order_index, id;
                                """,
                            DescriptionOverride: $"Chi hien thi portal xuat phat tu map id = {parentId}.",
                            HelpTextOverride: "Portal nay nam tren map dang chon va tro den mot spawn point cua map dich. Hay tao spawn point cua map dich truoc, sau do chon target_map_template_id va target_spawn_point_id phu hop. interaction_mode = Touch cho portal dau/cuoi map, Interact cho portal giua map can double click hoac bam skill danh thuong de vao.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["source_map_template_id"] = parentId,
                                ["source_x"] = defaultSpawnX,
                                ["source_y"] = defaultSpawnY,
                                ["interaction_radius"] = 24f,
                                ["interaction_mode"] = 1,
                                ["is_enabled"] = true,
                                ["order_index"] = 0
                            });
                        },
                        () => BuildEmptyRequest(portals, "Chon mot map o bang tren de xem cac portal xuat phat tu map do.")),
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
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "item_template_id");
                            return new AdminTableLoadRequest(
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
                            });
                        },
                        () => BuildEmptyRequest(effects, "Chọn một pill template ở bảng trên để xem effect.")),
                ]);
        }

        private static WorkspaceDefinition BuildSkillWorkspace(
            IReadOnlyDictionary<string, AdminResourceDefinition> resourcesByKey)
        {
            var skills = resourcesByKey["skills"];
            var effects = resourcesByKey["skill_effects"];

            return new WorkspaceDefinition(
                new AdminTableLoadRequest(
                    skills,
                    SelectSql: """
                        select
                            id,
                            code,
                            name,
                            skill_group_code,
                            skill_level,
                            skill_type,
                            skill_category,
                            target_type,
                            cast_range,
                            cast_time_ms,
                            travel_time_ms,
                            cooldown_ms,
                            description,
                            description_template,
                            created_at
                        from public.skills
                        order by id;
                        """,
                    TitleOverride: "Danh Sach Skill",
                    HelpTextOverride: "Bang cha de tao va chon skill. Moi cap skill la mot row rieng trong bang skills va duoc gom bang skill_group_code. `description_template` la noi author mo ta hien thi runtime cua skill."),
                [
                    new WorkspaceChildDefinition(
                        "Skill Effects",
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
                                effects,
                                SelectSql: $"""
                                    select *
                                    from public.skill_effects
                                    where skill_id = {parentId}
                                    order by order_index, id;
                                    """,
                                DescriptionOverride: $"Chi hien thi effect cua skill id = {parentId}.",
                                HelpTextOverride: "Khi bam Them Dong, tool se tu dien skill_id theo skill dang chon. Neu skill hien tai chua co effect va co cap truoc cung nhom, tool se tu clone effect cua cap truoc.",
                                NewRowDefaults: new Dictionary<string, object?>
                                {
                                    ["skill_id"] = parentId
                                });
                        },
                        () => BuildEmptyRequest(effects, "Chon mot skill o bang tren de xem effect.")),
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
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
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
                            });
                        },
                        () => BuildEmptyRequest(growthStages, "Chọn một herb template ở bảng trên để xem growth stage.")),
                    new WorkspaceChildDefinition(
                        "Harvest Outputs",
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
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
                            });
                        },
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
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
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
                            });
                        },
                        () => BuildEmptyRequest(entries, "Chọn một random table ở bảng trên để xem entries.")),
                    new WorkspaceChildDefinition(
                        "Fortune Tags",
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
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
                            });
                        },
                        () => BuildEmptyRequest(fortuneTags, "Chọn một random table ở bảng trên để xem fortune tags.")),
                ]);
        }

        private static WorkspaceDefinition BuildEnemyWorkspace(
            IReadOnlyDictionary<string, AdminResourceDefinition> resourcesByKey)
        {
            var enemies = resourcesByKey["enemy_templates"];
            var skills = resourcesByKey["enemy_template_skills"];
            var rewardRules = resourcesByKey["enemy_reward_rules"];

            return new WorkspaceDefinition(
                new AdminTableLoadRequest(
                    enemies,
                    TitleOverride: "Danh Sach Enemy",
                    HelpTextOverride: "Bang cha de tao va chon enemy/boss. Chon 1 dong de sua skill va reward rule ben duoi."),
                [
                    new WorkspaceChildDefinition(
                        "Skills",
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
                            skills,
                            SelectSql: $"""
                                select *
                                from public.enemy_template_skills
                                where enemy_template_id = {parentId}
                                order by order_index, id;
                                """,
                            DescriptionOverride: $"Chi hien thi skill cua enemy id = {parentId}.",
                            HelpTextOverride: "Khi bam Them Dong, tool se tu dien enemy_template_id theo enemy dang chon.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["enemy_template_id"] = parentId,
                                ["order_index"] = 1
                            });
                        },
                        () => BuildEmptyRequest(skills, "Chon mot enemy o bang tren de xem skill.")),
                    new WorkspaceChildDefinition(
                        "Reward Rules",
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
                            rewardRules,
                            SelectSql: $"""
                                select *
                                from public.enemy_reward_rules
                                where enemy_template_id = {parentId}
                                order by order_index, id;
                                """,
                            DescriptionOverride: $"Chi hien thi reward rule cua enemy id = {parentId}.",
                            HelpTextOverride: "Khi bam Them Dong, tool se tu dien enemy_template_id theo enemy dang chon.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["enemy_template_id"] = parentId,
                                ["delivery_type"] = 1,
                                ["target_rule"] = 1,
                                ["roll_count"] = 1,
                                ["order_index"] = 1
                            });
                        },
                        () => BuildEmptyRequest(rewardRules, "Chon mot enemy o bang tren de xem reward rule.")),
                ]);
        }

        private static WorkspaceDefinition BuildEnemySpawnWorkspace(
            IReadOnlyDictionary<string, AdminResourceDefinition> resourcesByKey)
        {
            var spawnGroups = resourcesByKey["map_enemy_spawn_groups"];
            var spawnEntries = resourcesByKey["map_enemy_spawn_entries"];

            return new WorkspaceDefinition(
                new AdminTableLoadRequest(
                    spawnGroups,
                    TitleOverride: "Danh Sach Spawn Group",
                    HelpTextOverride: "Bang cha de tao va chon spawn group. Chon 1 dong de sua danh sach enemy spawn ben duoi."),
                [
                    new WorkspaceChildDefinition(
                        "Spawn Entries",
                        parentRow =>
                        {
                            var parentId = GetRequiredInt(parentRow, "id");
                            return new AdminTableLoadRequest(
                            spawnEntries,
                            SelectSql: $"""
                                select *
                                from public.map_enemy_spawn_entries
                                where spawn_group_id = {parentId}
                                order by order_index, id;
                                """,
                            DescriptionOverride: $"Chi hien thi spawn entry cua group id = {parentId}.",
                            HelpTextOverride: "Khi bam Them Dong, tool se tu dien spawn_group_id theo group dang chon.",
                            NewRowDefaults: new Dictionary<string, object?>
                            {
                                ["spawn_group_id"] = parentId,
                                ["weight"] = 1,
                                ["order_index"] = 1
                            });
                        },
                        () => BuildEmptyRequest(spawnEntries, "Chon mot spawn group o bang tren de xem spawn entries.")),
                ]);
        }

        private static WorkspaceDefinition BuildPlayerInventoryWorkspace(
            IReadOnlyDictionary<string, AdminResourceDefinition> resourcesByKey)
        {
            var characters = resourcesByKey["characters"];
            var playerItems = resourcesByKey["player_items"];
            var playerEquipments = resourcesByKey["player_equipments"];
            var playerEquipmentBonuses = resourcesByKey["player_equipment_stat_bonuses"];

            return new WorkspaceDefinition(
                new AdminTableLoadRequest(
                    characters,
                    SelectSql: """
                        select *
                        from public.characters
                        order by name, created_at, id;
                        """,
                    TitleOverride: "Danh Sách Nhân Vật",
                    DescriptionOverride: "Chọn một nhân vật để seed item, dữ liệu equipment instance và bonus stat riêng cho từng món.",
                    HelpTextOverride: """
                        Đây là workspace để game design hoặc QA seed item trực tiếp cho từng nhân vật thật.

                        Luồng khuyến nghị:
                        1. Chọn nhân vật ở bảng trên.
                        2. Thêm dòng ở tab Player Items để đưa item vào túi nhân vật.
                        3. Nếu item là Equipment và cần dữ liệu instance, thêm dòng tương ứng ở tab Equipment Instance.
                        4. Nếu món đó có roll riêng hoặc bonus riêng, thêm các dòng ở tab Bonus Stats Theo Món.

                        Mẹo:
                        - Có thể lọc nhanh theo tên nhân vật.
                        - Cột account_id đã có lookup để đọc ra tài khoản cho đỡ phải nhớ UUID.
                        - Một player_item là item instance thật. Bonus stat riêng phải bám theo player_item_id, không bám theo item_template_id.
                        """),
                [
                    new WorkspaceChildDefinition(
                        "Player Items",
                        parentRow =>
                        {
                            var characterId = GetRequiredGuid(parentRow, "id");
                            var characterIdSql = ToSqlLiteral(characterId);
                            var characterName = Convert.ToString(parentRow["name"], CultureInfo.InvariantCulture) ?? "(Khong ten)";

                            return new AdminTableLoadRequest(
                                playerItems,
                                SelectSql: $"""
                                    select *
                                    from public.player_items
                                    where player_id = {characterIdSql}
                                    order by location_type, acquired_at desc, id desc;
                                    """,
                                DescriptionOverride: $"Chỉ hiển thị item instance đang thuộc nhân vật {characterName}.",
                                HelpTextOverride: """
                                    Mỗi dòng là một item instance thật đang thuộc nhân vật đang chọn.

                                    Gợi ý nhập nhanh:
                                    - Item thường trong túi: location_type = Inventory, quantity > 1 nếu stackable.
                                    - Equipment: thường quantity = 1.
                                    - Ground item không nên tạo ở đây vì player_id của item trên đất phải để null.
                                    """,
                                NewRowDefaults: new Dictionary<string, object?>
                                {
                                    ["player_id"] = characterId,
                                    ["location_type"] = 1,
                                    ["quantity"] = 1,
                                    ["is_bound"] = false
                                });
                        },
                        () => BuildEmptyRequest(playerItems, "Chọn một nhân vật ở bảng trên để xem item instance của nhân vật đó.")),
                    new WorkspaceChildDefinition(
                        "Equipment Instance",
                        parentRow =>
                        {
                            var characterId = GetRequiredGuid(parentRow, "id");
                            var characterIdSql = ToSqlLiteral(characterId);
                            var characterName = Convert.ToString(parentRow["name"], CultureInfo.InvariantCulture) ?? "(Khong ten)";

                            return new AdminTableLoadRequest(
                                playerEquipments,
                                SelectSql: $"""
                                    select pe.*
                                    from public.player_equipments pe
                                    inner join public.player_items pi on pi.id = pe.player_item_id
                                    where pi.player_id = {characterIdSql}
                                    order by
                                        case when pe.equipped_slot is null then 1 else 0 end,
                                        pe.equipped_slot,
                                        pe.player_item_id;
                                    """,
                                DescriptionOverride: $"Chỉ hiển thị dữ liệu equipment instance của nhân vật {characterName}.",
                                HelpTextOverride: """
                                    Tab này chỉ dùng cho item Equipment đã có player_item row tương ứng.

                                    Luồng đúng:
                                    - Tạo item ở tab Player Items trước.
                                    - Nếu món đó là equipment, tạo tiếp một dòng ở đây bằng đúng player_item_id của món đó.
                                    - equipped_slot để null nếu chỉ đang nằm trong túi; điền slot nếu muốn seed sẵn trạng thái đang mặc.
                                    """,
                                NewRowDefaults: new Dictionary<string, object?>
                                {
                                    ["enhance_level"] = 0
                                });
                        },
                        () => BuildEmptyRequest(playerEquipments, "Chọn một nhân vật ở bảng trên để xem equipment instance của nhân vật đó.")),
                    new WorkspaceChildDefinition(
                        "Bonus Stats Theo Món",
                        parentRow =>
                        {
                            var characterId = GetRequiredGuid(parentRow, "id");
                            var characterIdSql = ToSqlLiteral(characterId);
                            var characterName = Convert.ToString(parentRow["name"], CultureInfo.InvariantCulture) ?? "(Khong ten)";

                            return new AdminTableLoadRequest(
                                playerEquipmentBonuses,
                                SelectSql: $"""
                                    select pebs.*
                                    from public.player_equipment_stat_bonuses pebs
                                    inner join public.player_items pi on pi.id = pebs.player_item_id
                                    where pi.player_id = {characterIdSql}
                                    order by pebs.player_item_id, pebs.id;
                                    """,
                                DescriptionOverride: $"Chỉ hiển thị bonus stat riêng theo từng món của nhân vật {characterName}.",
                                HelpTextOverride: """
                                    Bonus stat ở đây là bonus riêng của từng item instance.

                                    Dùng tab này cho các case như:
                                    - Kiếm cùng template nhưng một cây +50 ATK, cây khác +60 ATK.
                                    - Đồ biến dị, refine, event bonus, craft bonus.

                                    Gợi ý:
                                    - Nếu muốn test nhanh, cứ dùng value_type = Flat trước.
                                    - source_type giúp phân biệt bonus đến từ drop, craft, mutation hay refine.
                                    """,
                                NewRowDefaults: new Dictionary<string, object?>
                                {
                                    ["stat_type"] = 4,
                                    ["value_type"] = 1,
                                    ["source_type"] = 1
                                });
                        },
                        () => BuildEmptyRequest(playerEquipmentBonuses, "Chọn một nhân vật ở bảng trên để xem bonus stat riêng theo từng món.")),
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

        private static int GetRequiredInt(DataRow row, string columnName)
        {
            return Convert.ToInt32(row[columnName], CultureInfo.InvariantCulture);
        }

        private static Guid GetRequiredGuid(DataRow row, string columnName)
        {
            var rawValue = row[columnName];
            return rawValue switch
            {
                Guid guid => guid,
                string text when Guid.TryParse(text, out var guid) => guid,
                _ => throw new InvalidOperationException($"Column {columnName} does not contain a valid Guid value.")
            };
        }

        private static float? GetOptionalSingle(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName))
                return null;

            var rawValue = row[columnName];
            if (rawValue is null || rawValue == DBNull.Value)
                return null;

            return Convert.ToSingle(rawValue, CultureInfo.InvariantCulture);
        }

        private static string ToSqlLiteral(object? value)
        {
            return value switch
            {
                null or DBNull => "null",
                Guid guid => $"'{guid:D}'",
                string text => $"'{text.Replace("'", "''")}'",
                bool boolValue => boolValue ? "true" : "false",
                short number => number.ToString(CultureInfo.InvariantCulture),
                int number => number.ToString(CultureInfo.InvariantCulture),
                long number => number.ToString(CultureInfo.InvariantCulture),
                decimal number => number.ToString(CultureInfo.InvariantCulture),
                double number => number.ToString(CultureInfo.InvariantCulture),
                float number => number.ToString(CultureInfo.InvariantCulture),
                DateTime dateTime => $"'{dateTime:yyyy-MM-dd HH:mm:ss}'",
                _ => $"'{Convert.ToString(value, CultureInfo.InvariantCulture)?.Replace("'", "''")}'"
            };
        }
    }
}

using System.Data;
using System.Globalization;
using Npgsql;

namespace AdminDesignerTool;

internal sealed class CharacterSkillWorkspaceControl : UserControl
{
    private readonly string _connectionString;
    private readonly Label _titleLabel;
    private readonly Label _descriptionLabel;
    private readonly TextBox _helpTextBox;
    private readonly Label _statusLabel;
    private readonly SplitContainer _splitContainer;
    private readonly DataGridView _characterGrid;
    private readonly DataGridView _skillGrid;
    private readonly Button _reloadCharactersButton;
    private readonly Button _reloadSkillsButton;
    private readonly Label _selectedCharacterLabel;
    private readonly BindingSource _characterBindingSource;
    private readonly BindingSource _skillBindingSource;

    private bool _loadingCharacters;
    private bool _loadingSkills;
    private bool _suppressSkillEvents;
    private bool _toggleInFlight;
    private Guid? _selectedCharacterId;

    public CharacterSkillWorkspaceControl(string connectionString)
    {
        _connectionString = connectionString;

        Dock = DockStyle.Fill;
        Padding = new Padding(12);

        _titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            Text = "Character Skills Workspace"
        };

        _descriptionLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 26,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Color.DimGray,
            AutoEllipsis = true
        };

        _helpTextBox = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 84,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.WhiteSmoke,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular)
        };

        _statusLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Color.DimGray,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 280
        };

        _characterBindingSource = new BindingSource();
        _skillBindingSource = new BindingSource();

        _reloadCharactersButton = new Button
        {
            AutoSize = true,
            Text = "Tai Lai Nhan Vat"
        };
        _reloadCharactersButton.Click += async (_, _) => await ReloadCharactersAsync(keepSelection: true);

        var characterHeaderLabel = new Label
        {
            AutoSize = true,
            Text = "Danh sach nhan vat",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Margin = new Padding(0, 6, 12, 0)
        };

        var characterHeaderPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        characterHeaderPanel.Controls.Add(characterHeaderLabel);
        characterHeaderPanel.Controls.Add(_reloadCharactersButton);

        _characterGrid = BuildGrid();
        _characterGrid.DataSource = _characterBindingSource;
        _characterGrid.SelectionChanged += CharacterGridOnSelectionChanged;

        _splitContainer.Panel1.Controls.Add(_characterGrid);
        _splitContainer.Panel1.Controls.Add(characterHeaderPanel);

        _reloadSkillsButton = new Button
        {
            AutoSize = true,
            Text = "Tai Lai Skill"
        };
        _reloadSkillsButton.Click += async (_, _) =>
        {
            if (_selectedCharacterId.HasValue)
                await ReloadSkillsAsync(_selectedCharacterId.Value);
        };

        _selectedCharacterLabel = new Label
        {
            AutoSize = true,
            Text = "Chua chon nhan vat",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Margin = new Padding(0, 6, 12, 0)
        };

        var skillHeaderPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        skillHeaderPanel.Controls.Add(_selectedCharacterLabel);
        skillHeaderPanel.Controls.Add(_reloadSkillsButton);

        _skillGrid = BuildGrid();
        _skillGrid.DataSource = _skillBindingSource;
        _skillGrid.CurrentCellDirtyStateChanged += SkillGridOnCurrentCellDirtyStateChanged;
        _skillGrid.CellValueChanged += SkillGridOnCellValueChanged;

        _splitContainer.Panel2.Controls.Add(_skillGrid);
        _splitContainer.Panel2.Controls.Add(skillHeaderPanel);

        Controls.Add(_splitContainer);
        Controls.Add(_statusLabel);
        Controls.Add(_helpTextBox);
        Controls.Add(_descriptionLabel);
        Controls.Add(_titleLabel);
    }

    public async Task LoadWorkspaceAsync(AdminResourceDefinition workspaceResource)
    {
        _titleLabel.Text = workspaceResource.DisplayName;
        _descriptionLabel.Text = workspaceResource.Description;
        _helpTextBox.Text = workspaceResource.HelpText;
        await ReloadCharactersAsync(keepSelection: true);
    }

    private async Task ReloadCharactersAsync(bool keepSelection)
    {
        if (_loadingCharacters)
            return;

        _loadingCharacters = true;
        SetStatus("Dang tai danh sach nhan vat...");
        try
        {
            var previousSelection = keepSelection ? _selectedCharacterId : null;
            var table = new DataTable();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                select
                    c.id,
                    c.name,
                    c.created_at,
                    coalesce(c.name, '(Khong ten)') as display_name
                from public.characters c
                order by c.name nulls last, c.created_at, c.id;
                """;

            await using (var command = new NpgsqlCommand(sql, connection))
            await using (var reader = await command.ExecuteReaderAsync())
            {
                table.Load(reader);
            }

            _characterBindingSource.DataSource = table;
            ConfigureCharacterGrid();
            RestoreCharacterSelection(previousSelection);

            if (_characterGrid.Rows.Count == 0)
            {
                _selectedCharacterId = null;
                _selectedCharacterLabel.Text = "Chua chon nhan vat";
                _skillBindingSource.DataSource = CreateEmptySkillTable();
                ConfigureSkillGrid();
                SetStatus("Chua co nhan vat nao trong DB.");
                return;
            }

            if (!_selectedCharacterId.HasValue)
                RestoreCharacterSelection(null);

            if (_selectedCharacterId.HasValue)
            {
                _selectedCharacterLabel.Text = $"Skill cua: {GetSelectedCharacterName()}";
                await ReloadSkillsAsync(_selectedCharacterId.Value);
            }
            else
            {
                SetStatus($"Da tai {_characterGrid.Rows.Count} nhan vat.");
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Tai nhan vat that bai: {ex.Message}", isError: true);
        }
        finally
        {
            _loadingCharacters = false;
        }
    }

    private async void CharacterGridOnSelectionChanged(object? sender, EventArgs e)
    {
        if (_loadingCharacters)
            return;

        var characterId = GetSelectedCharacterId();
        if (!characterId.HasValue)
        {
            _selectedCharacterId = null;
            _selectedCharacterLabel.Text = "Chua chon nhan vat";
            _skillBindingSource.DataSource = CreateEmptySkillTable();
            ConfigureSkillGrid();
            return;
        }

        if (_selectedCharacterId.HasValue && _selectedCharacterId.Value == characterId.Value && !_loadingSkills)
            return;

        _selectedCharacterId = characterId.Value;
        _selectedCharacterLabel.Text = $"Skill cua: {GetSelectedCharacterName()}";
        await ReloadSkillsAsync(characterId.Value);
    }

    private async Task ReloadSkillsAsync(Guid characterId)
    {
        if (_loadingSkills)
            return;

        _loadingSkills = true;
        _suppressSkillEvents = true;
        _reloadSkillsButton.Enabled = false;
        SetStatus("Dang tai danh sach skill...");

        try
        {
            var sourceTable = new DataTable();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                select
                    mas.id as martial_art_skill_id,
                    coalesce(ps.id is not null, false) as is_owned,
                    coalesce(ps.is_active, false) as is_active,
                    s.id as skill_id,
                    s.code as skill_code,
                    s.name as skill_name,
                    ma.id as martial_art_id,
                    ma.code as martial_art_code,
                    ma.name as martial_art_name,
                    mas.unlock_stage,
                    coalesce(pma.current_stage, 0) as player_martial_art_stage,
                    coalesce(
                        string_agg(psl.slot_index::text, ', ' order by psl.slot_index)
                        filter (where psl.slot_index is not null),
                        ''
                    ) as loadout_slots
                from public.martial_art_skills mas
                inner join public.skills s on s.id = mas.skill_id
                inner join public.martial_arts ma on ma.id = mas.martial_art_id
                left join public.player_skills ps
                    on ps.player_id = @player_id
                   and ps.source_martial_art_skill_id = mas.id
                left join public.player_martial_arts pma
                    on pma.player_id = @player_id
                   and pma.martial_art_id = mas.martial_art_id
                left join public.player_skill_loadouts psl
                    on psl.player_id = @player_id
                   and psl.player_skill_id = ps.id
                group by
                    mas.id,
                    ps.id,
                    ps.is_active,
                    s.id,
                    s.code,
                    s.name,
                    ma.id,
                    ma.code,
                    ma.name,
                    mas.unlock_stage,
                    pma.current_stage
                order by ma.id, mas.unlock_stage, mas.id;
                """;

            await using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("player_id", characterId);
                await using var reader = await command.ExecuteReaderAsync();
                sourceTable.Load(reader);
            }

            var table = CreateEditableSkillTable(sourceTable);
            _skillBindingSource.DataSource = table;
            ConfigureSkillGrid();
            SetStatus($"Da tai {table.Rows.Count} skill source cho nhan vat dang chon.");
        }
        catch (Exception ex)
        {
            SetStatus($"Tai skill that bai: {ex.Message}", isError: true);
        }
        finally
        {
            _reloadSkillsButton.Enabled = true;
            _suppressSkillEvents = false;
            _loadingSkills = false;
        }
    }

    private void SkillGridOnCurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (_skillGrid.IsCurrentCellDirty)
            _skillGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private async void SkillGridOnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_suppressSkillEvents || _loadingSkills || _toggleInFlight || e.RowIndex < 0)
            return;

        var column = _skillGrid.Columns[e.ColumnIndex];
        if (!string.Equals(column.DataPropertyName, "is_owned", StringComparison.OrdinalIgnoreCase))
            return;

        if (!_selectedCharacterId.HasValue)
            return;

        var row = _skillGrid.Rows[e.RowIndex];
        if (row.DataBoundItem is not DataRowView rowView)
            return;

        var martialArtSkillId = Convert.ToInt32(rowView["martial_art_skill_id"], CultureInfo.InvariantCulture);
        var isOwned = Convert.ToBoolean(rowView["is_owned"], CultureInfo.InvariantCulture);
        await ToggleSkillOwnershipAsync(_selectedCharacterId.Value, martialArtSkillId, isOwned);
    }

    private async Task ToggleSkillOwnershipAsync(Guid characterId, int martialArtSkillId, bool shouldOwn)
    {
        if (_toggleInFlight)
            return;

        _toggleInFlight = true;
        _skillGrid.Enabled = false;
        _reloadCharactersButton.Enabled = false;
        _reloadSkillsButton.Enabled = false;
        SetStatus(shouldOwn ? "Dang cap skill..." : "Dang go skill...");

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            if (shouldOwn)
            {
                const string ensureMartialArtSql = """
                    insert into public.player_martial_arts (
                        player_id,
                        martial_art_id,
                        current_stage,
                        current_exp,
                        created_at,
                        updated_at
                    )
                    select
                        @player_id,
                        mas.martial_art_id,
                        greatest(mas.unlock_stage, 1),
                        0,
                        now(),
                        now()
                    from public.martial_art_skills mas
                    where mas.id = @martial_art_skill_id
                    on conflict (player_id, martial_art_id) do update
                    set current_stage = greatest(public.player_martial_arts.current_stage, excluded.current_stage),
                        updated_at = now();
                    """;

                await using (var command = new NpgsqlCommand(ensureMartialArtSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("player_id", characterId);
                    command.Parameters.AddWithValue("martial_art_skill_id", martialArtSkillId);
                    await command.ExecuteNonQueryAsync();
                }

                const string grantSkillSql = """
                    insert into public.player_skills (
                        player_id,
                        skill_id,
                        source_martial_art_id,
                        source_martial_art_skill_id,
                        unlocked_at,
                        is_active,
                        created_at,
                        updated_at
                    )
                    select
                        @player_id,
                        mas.skill_id,
                        mas.martial_art_id,
                        mas.id,
                        now(),
                        true,
                        now(),
                        now()
                    from public.martial_art_skills mas
                    where mas.id = @martial_art_skill_id
                    on conflict (player_id, source_martial_art_skill_id) do update
                    set skill_id = excluded.skill_id,
                        source_martial_art_id = excluded.source_martial_art_id,
                        is_active = true,
                        updated_at = now();
                    """;

                await using (var command = new NpgsqlCommand(grantSkillSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("player_id", characterId);
                    command.Parameters.AddWithValue("martial_art_skill_id", martialArtSkillId);
                    await command.ExecuteNonQueryAsync();
                }
            }
            else
            {
                const string clearLoadoutsSql = """
                    delete from public.player_skill_loadouts
                    where player_id = @player_id
                      and player_skill_id in (
                          select id
                          from public.player_skills
                          where player_id = @player_id
                            and source_martial_art_skill_id = @martial_art_skill_id
                      );
                    """;

                await using (var command = new NpgsqlCommand(clearLoadoutsSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("player_id", characterId);
                    command.Parameters.AddWithValue("martial_art_skill_id", martialArtSkillId);
                    await command.ExecuteNonQueryAsync();
                }

                const string revokeSkillSql = """
                    delete from public.player_skills
                    where player_id = @player_id
                      and source_martial_art_skill_id = @martial_art_skill_id;
                    """;

                await using (var command = new NpgsqlCommand(revokeSkillSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("player_id", characterId);
                    command.Parameters.AddWithValue("martial_art_skill_id", martialArtSkillId);
                    await command.ExecuteNonQueryAsync();
                }
            }

            await transaction.CommitAsync();
            SetStatus(shouldOwn ? "Da cap skill cho nhan vat." : "Da go skill khoi nhan vat.");
        }
        catch (Exception ex)
        {
            SetStatus($"Cap nhat skill that bai: {ex.Message}", isError: true);
        }
        finally
        {
            _toggleInFlight = false;
            _skillGrid.Enabled = true;
            _reloadCharactersButton.Enabled = true;
            _reloadSkillsButton.Enabled = true;
            await ReloadSkillsAsync(characterId);
        }
    }

    private Guid? GetSelectedCharacterId()
    {
        if (_characterGrid.CurrentRow?.DataBoundItem is not DataRowView rowView)
            return null;

        return rowView["id"] switch
        {
            Guid guid => guid,
            string text when Guid.TryParse(text, out var guid) => guid,
            _ => null
        };
    }

    private string GetSelectedCharacterName()
    {
        if (_characterGrid.CurrentRow?.DataBoundItem is not DataRowView rowView)
            return "(Khong ten)";

        var name = Convert.ToString(rowView["display_name"], CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(name) ? "(Khong ten)" : name.Trim();
    }

    private void RestoreCharacterSelection(Guid? selectedCharacterId)
    {
        if (_characterGrid.Rows.Count == 0)
            return;

        for (var i = 0; i < _characterGrid.Rows.Count; i++)
        {
            if (_characterGrid.Rows[i].DataBoundItem is not DataRowView rowView)
                continue;

            var rowCharacterId = rowView["id"] switch
            {
                Guid guid => guid,
                string text when Guid.TryParse(text, out var guid) => guid,
                _ => Guid.Empty
            };

            if (selectedCharacterId.HasValue && rowCharacterId != selectedCharacterId.Value)
                continue;

            _characterGrid.ClearSelection();
            _characterGrid.Rows[i].Selected = true;
            _characterGrid.CurrentCell = _characterGrid.Rows[i].Cells[GetFirstVisibleColumnIndex(_characterGrid)];
            _selectedCharacterId = rowCharacterId;
            return;
        }

        _characterGrid.ClearSelection();
        _characterGrid.Rows[0].Selected = true;
        _characterGrid.CurrentCell = _characterGrid.Rows[0].Cells[GetFirstVisibleColumnIndex(_characterGrid)];
        _selectedCharacterId = GetSelectedCharacterId();
    }

    private void ConfigureCharacterGrid()
    {
        ConfigureSharedGridBehavior(_characterGrid);
        SetColumn(_characterGrid, "id", "Character Id", visible: false);
        SetColumn(_characterGrid, "display_name", "Nhan Vat", width: 240);
        SetColumn(_characterGrid, "name", "Ten Goc", visible: false);
        SetColumn(_characterGrid, "created_at", "Tao Luc", width: 180);
    }

    private void ConfigureSkillGrid()
    {
        ConfigureSharedGridBehavior(_skillGrid);
        SetColumn(_skillGrid, "martial_art_skill_id", "Id", width: 60, readOnly: true);
        SetColumn(_skillGrid, "is_owned", "So Huu", width: 70, readOnly: false);
        SetColumn(_skillGrid, "is_active", "Dang Bat", width: 70, readOnly: true);
        SetColumn(_skillGrid, "skill_id", "Skill Id", width: 70, readOnly: true);
        SetColumn(_skillGrid, "skill_code", "Skill Code", width: 140, readOnly: true);
        SetColumn(_skillGrid, "skill_name", "Ten Skill", width: 200, readOnly: true);
        SetColumn(_skillGrid, "martial_art_id", "Cong Phap Id", width: 90, readOnly: true);
        SetColumn(_skillGrid, "martial_art_code", "Cong Phap Code", width: 140, readOnly: true);
        SetColumn(_skillGrid, "martial_art_name", "Ten Cong Phap", width: 200, readOnly: true);
        SetColumn(_skillGrid, "unlock_stage", "Mo O Tang", width: 80, readOnly: true);
        SetColumn(_skillGrid, "player_martial_art_stage", "Tang Hien Tai", width: 90, readOnly: true);
        SetColumn(_skillGrid, "loadout_slots", "O Loadout", width: 100, readOnly: true);
    }

    private static DataGridView BuildGrid()
    {
        return new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoGenerateColumns = true,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            MultiSelect = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
    }

    private static void ConfigureSharedGridBehavior(DataGridView grid)
    {
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.Columns.Cast<DataGridViewColumn>().ToList().ForEach(column => column.SortMode = DataGridViewColumnSortMode.Automatic);
    }

    private static void SetColumn(
        DataGridView grid,
        string columnName,
        string headerText,
        bool visible = true,
        int width = 120,
        bool readOnly = true)
    {
        if (!grid.Columns.Contains(columnName))
            return;

        var column = grid.Columns[columnName];
        column.HeaderText = headerText;
        column.Visible = visible;
        column.Width = width;
        column.ReadOnly = readOnly;
    }

    private static int GetFirstVisibleColumnIndex(DataGridView grid)
    {
        for (var i = 0; i < grid.Columns.Count; i++)
        {
            if (grid.Columns[i].Visible)
                return i;
        }

        return 0;
    }

    private static DataTable CreateEmptySkillTable()
    {
        var table = new DataTable();
        table.Columns.Add("martial_art_skill_id", typeof(int));
        table.Columns.Add("is_owned", typeof(bool));
        table.Columns.Add("is_active", typeof(bool));
        table.Columns.Add("skill_id", typeof(int));
        table.Columns.Add("skill_code", typeof(string));
        table.Columns.Add("skill_name", typeof(string));
        table.Columns.Add("martial_art_id", typeof(int));
        table.Columns.Add("martial_art_code", typeof(string));
        table.Columns.Add("martial_art_name", typeof(string));
        table.Columns.Add("unlock_stage", typeof(int));
        table.Columns.Add("player_martial_art_stage", typeof(int));
        table.Columns.Add("loadout_slots", typeof(string));
        return table;
    }

    private static DataTable CreateEditableSkillTable(DataTable sourceTable)
    {
        var table = CreateEmptySkillTable();
        if (sourceTable == null || sourceTable.Rows.Count == 0)
            return table;

        foreach (DataRow sourceRow in sourceTable.Rows)
        {
            var row = table.NewRow();
            row["martial_art_skill_id"] = Convert.ToInt32(sourceRow["martial_art_skill_id"], CultureInfo.InvariantCulture);
            row["is_owned"] = Convert.ToBoolean(sourceRow["is_owned"], CultureInfo.InvariantCulture);
            row["is_active"] = Convert.ToBoolean(sourceRow["is_active"], CultureInfo.InvariantCulture);
            row["skill_id"] = Convert.ToInt32(sourceRow["skill_id"], CultureInfo.InvariantCulture);
            row["skill_code"] = Convert.ToString(sourceRow["skill_code"], CultureInfo.InvariantCulture) ?? string.Empty;
            row["skill_name"] = Convert.ToString(sourceRow["skill_name"], CultureInfo.InvariantCulture) ?? string.Empty;
            row["martial_art_id"] = Convert.ToInt32(sourceRow["martial_art_id"], CultureInfo.InvariantCulture);
            row["martial_art_code"] = Convert.ToString(sourceRow["martial_art_code"], CultureInfo.InvariantCulture) ?? string.Empty;
            row["martial_art_name"] = Convert.ToString(sourceRow["martial_art_name"], CultureInfo.InvariantCulture) ?? string.Empty;
            row["unlock_stage"] = Convert.ToInt32(sourceRow["unlock_stage"], CultureInfo.InvariantCulture);
            row["player_martial_art_stage"] = sourceRow["player_martial_art_stage"] is null or DBNull
                ? 0
                : Convert.ToInt32(sourceRow["player_martial_art_stage"], CultureInfo.InvariantCulture);
            row["loadout_slots"] = Convert.ToString(sourceRow["loadout_slots"], CultureInfo.InvariantCulture) ?? string.Empty;
            table.Rows.Add(row);
        }

        return table;
    }

    private void SetStatus(string message, bool isError = false)
    {
        _statusLabel.Text = message ?? string.Empty;
        _statusLabel.ForeColor = isError ? Color.Firebrick : Color.DimGray;
    }
}

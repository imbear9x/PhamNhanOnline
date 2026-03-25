using System.Data;
using System.Globalization;
using Npgsql;

namespace AdminDesignerTool;

internal sealed class CharacterMartialArtWorkspaceControl : UserControl
{
    private readonly string _connectionString;
    private readonly Label _titleLabel;
    private readonly Label _descriptionLabel;
    private readonly TextBox _helpTextBox;
    private readonly Label _statusLabel;
    private readonly SplitContainer _splitContainer;
    private readonly DataGridView _characterGrid;
    private readonly DataGridView _martialArtGrid;
    private readonly Button _reloadCharactersButton;
    private readonly Button _reloadMartialArtsButton;
    private readonly Label _selectedCharacterLabel;
    private readonly BindingSource _characterBindingSource;
    private readonly BindingSource _martialArtBindingSource;

    private bool _loadingCharacters;
    private bool _loadingMartialArts;
    private bool _suppressMartialArtEvents;
    private bool _toggleInFlight;
    private Guid? _selectedCharacterId;

    public CharacterMartialArtWorkspaceControl(string connectionString)
    {
        _connectionString = connectionString;

        Dock = DockStyle.Fill;
        Padding = new Padding(12);

        _titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            Text = "Character Martial Arts Workspace"
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
        _martialArtBindingSource = new BindingSource();

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

        _reloadMartialArtsButton = new Button
        {
            AutoSize = true,
            Text = "Tai Lai Cong Phap"
        };
        _reloadMartialArtsButton.Click += async (_, _) =>
        {
            if (_selectedCharacterId.HasValue)
                await ReloadMartialArtsAsync(_selectedCharacterId.Value);
        };

        _selectedCharacterLabel = new Label
        {
            AutoSize = true,
            Text = "Chua chon nhan vat",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Margin = new Padding(0, 6, 12, 0)
        };

        var martialArtHeaderPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        martialArtHeaderPanel.Controls.Add(_selectedCharacterLabel);
        martialArtHeaderPanel.Controls.Add(_reloadMartialArtsButton);

        _martialArtGrid = BuildGrid();
        _martialArtGrid.DataSource = _martialArtBindingSource;
        _martialArtGrid.CurrentCellDirtyStateChanged += MartialArtGridOnCurrentCellDirtyStateChanged;
        _martialArtGrid.CellValueChanged += MartialArtGridOnCellValueChanged;

        _splitContainer.Panel2.Controls.Add(_martialArtGrid);
        _splitContainer.Panel2.Controls.Add(martialArtHeaderPanel);

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
                _martialArtBindingSource.DataSource = CreateEmptyMartialArtTable();
                ConfigureMartialArtGrid();
                SetStatus("Chua co nhan vat nao trong DB.");
                return;
            }

            if (!_selectedCharacterId.HasValue)
                RestoreCharacterSelection(null);

            if (_selectedCharacterId.HasValue)
            {
                _selectedCharacterLabel.Text = $"Cong phap cua: {GetSelectedCharacterName()}";
                await ReloadMartialArtsAsync(_selectedCharacterId.Value);
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
            _martialArtBindingSource.DataSource = CreateEmptyMartialArtTable();
            ConfigureMartialArtGrid();
            return;
        }

        if (_selectedCharacterId.HasValue && _selectedCharacterId.Value == characterId.Value && !_loadingMartialArts)
            return;

        _selectedCharacterId = characterId.Value;
        _selectedCharacterLabel.Text = $"Cong phap cua: {GetSelectedCharacterName()}";
        await ReloadMartialArtsAsync(characterId.Value);
    }

    private async Task ReloadMartialArtsAsync(Guid characterId)
    {
        if (_loadingMartialArts)
            return;

        _loadingMartialArts = true;
        _suppressMartialArtEvents = true;
        _reloadMartialArtsButton.Enabled = false;
        SetStatus("Dang tai danh sach cong phap...");

        try
        {
            var sourceTable = new DataTable();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                select
                    m.id as martial_art_id,
                    coalesce(pma.id is not null, false) as is_owned,
                    coalesce(cbs.active_martial_art_id = m.id, false) as is_active,
                    m.code,
                    m.name,
                    m.category,
                    m.qi_absorption_rate,
                    pma.current_stage,
                    pma.current_exp
                from public.martial_arts m
                left join public.player_martial_arts pma
                    on pma.player_id = @player_id
                   and pma.martial_art_id = m.id
                left join public.character_base_stats cbs
                    on cbs.character_id = @player_id
                order by m.id;
                """;

            await using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("player_id", characterId);
                await using var reader = await command.ExecuteReaderAsync();
                sourceTable.Load(reader);
            }

            var table = CreateEditableMartialArtTable(sourceTable);
            _martialArtBindingSource.DataSource = table;
            ConfigureMartialArtGrid();
            SetStatus($"Da tai {table.Rows.Count} cong phap cho nhan vat dang chon.");
        }
        catch (Exception ex)
        {
            SetStatus($"Tai cong phap that bai: {ex.Message}", isError: true);
        }
        finally
        {
            _reloadMartialArtsButton.Enabled = true;
            _suppressMartialArtEvents = false;
            _loadingMartialArts = false;
        }
    }

    private void MartialArtGridOnCurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (_martialArtGrid.IsCurrentCellDirty)
            _martialArtGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private async void MartialArtGridOnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_suppressMartialArtEvents || _loadingMartialArts || _toggleInFlight || e.RowIndex < 0)
            return;

        var column = _martialArtGrid.Columns[e.ColumnIndex];
        if (!string.Equals(column.DataPropertyName, "is_owned", StringComparison.OrdinalIgnoreCase))
            return;

        if (!_selectedCharacterId.HasValue)
            return;

        var row = _martialArtGrid.Rows[e.RowIndex];
        if (row.DataBoundItem is not DataRowView rowView)
            return;

        var martialArtId = Convert.ToInt32(rowView["martial_art_id"], CultureInfo.InvariantCulture);
        var isOwned = Convert.ToBoolean(rowView["is_owned"], CultureInfo.InvariantCulture);
        await ToggleMartialArtOwnershipAsync(_selectedCharacterId.Value, martialArtId, isOwned);
    }

    private async Task ToggleMartialArtOwnershipAsync(Guid characterId, int martialArtId, bool shouldOwn)
    {
        if (_toggleInFlight)
            return;

        _toggleInFlight = true;
        _martialArtGrid.Enabled = false;
        _reloadCharactersButton.Enabled = false;
        _reloadMartialArtsButton.Enabled = false;
        SetStatus(shouldOwn ? "Dang cap cong phap..." : "Dang go cong phap...");

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            if (shouldOwn)
            {
                const string grantSql = """
                    insert into public.player_martial_arts (
                        player_id,
                        martial_art_id,
                        current_stage,
                        current_exp,
                        created_at,
                        updated_at
                    )
                    values (
                        @player_id,
                        @martial_art_id,
                        1,
                        0,
                        now(),
                        now()
                    )
                    on conflict (player_id, martial_art_id) do nothing;
                    """;

                await using (var command = new NpgsqlCommand(grantSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("player_id", characterId);
                    command.Parameters.AddWithValue("martial_art_id", martialArtId);
                    await command.ExecuteNonQueryAsync();
                }

                const string grantStageOneSkillsSql = """
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
                    where mas.martial_art_id = @martial_art_id
                      and mas.unlock_stage <= 1
                    on conflict (player_id, source_martial_art_skill_id) do nothing;
                    """;

                await using (var command = new NpgsqlCommand(grantStageOneSkillsSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("player_id", characterId);
                    command.Parameters.AddWithValue("martial_art_id", martialArtId);
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
                            and source_martial_art_id = @martial_art_id
                      );
                    """;

                await using (var command = new NpgsqlCommand(clearLoadoutsSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("player_id", characterId);
                    command.Parameters.AddWithValue("martial_art_id", martialArtId);
                    await command.ExecuteNonQueryAsync();
                }

                const string clearPlayerSkillsSql = """
                    delete from public.player_skills
                    where player_id = @player_id
                      and source_martial_art_id = @martial_art_id;
                    """;

                await using (var command = new NpgsqlCommand(clearPlayerSkillsSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("player_id", characterId);
                    command.Parameters.AddWithValue("martial_art_id", martialArtId);
                    await command.ExecuteNonQueryAsync();
                }

                const string clearActiveSql = """
                    update public.character_base_stats
                    set active_martial_art_id = null
                    where character_id = @player_id
                      and active_martial_art_id = @martial_art_id;
                    """;

                await using (var command = new NpgsqlCommand(clearActiveSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("player_id", characterId);
                    command.Parameters.AddWithValue("martial_art_id", martialArtId);
                    await command.ExecuteNonQueryAsync();
                }

                const string revokeSql = """
                    delete from public.player_martial_arts
                    where player_id = @player_id
                      and martial_art_id = @martial_art_id;
                    """;

                await using (var command = new NpgsqlCommand(revokeSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("player_id", characterId);
                    command.Parameters.AddWithValue("martial_art_id", martialArtId);
                    await command.ExecuteNonQueryAsync();
                }
            }

            await transaction.CommitAsync();
            SetStatus(shouldOwn ? "Da cap cong phap cho nhan vat." : "Da go cong phap khoi nhan vat.");
        }
        catch (Exception ex)
        {
            SetStatus($"Cap nhat cong phap that bai: {ex.Message}", isError: true);
        }
        finally
        {
            _toggleInFlight = false;
            _martialArtGrid.Enabled = true;
            _reloadCharactersButton.Enabled = true;
            _reloadMartialArtsButton.Enabled = true;

            await ReloadMartialArtsAsync(characterId);
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

    private void ConfigureMartialArtGrid()
    {
        ConfigureSharedGridBehavior(_martialArtGrid);
        SetColumn(_martialArtGrid, "martial_art_id", "Id", width: 60, readOnly: true);
        SetColumn(_martialArtGrid, "is_owned", "So Huu", width: 70, readOnly: false);
        SetColumn(_martialArtGrid, "is_active", "Chu Tu", width: 70, readOnly: true);
        SetColumn(_martialArtGrid, "code", "Code", width: 160, readOnly: true);
        SetColumn(_martialArtGrid, "name", "Ten Cong Phap", width: 220, readOnly: true);
        SetColumn(_martialArtGrid, "category", "Loai", width: 120, readOnly: true);
        SetColumn(_martialArtGrid, "qi_absorption_rate", "He So Hap Thu", width: 110, readOnly: true);
        SetColumn(_martialArtGrid, "current_stage", "Tang", width: 60, readOnly: true);
        SetColumn(_martialArtGrid, "current_exp", "Exp", width: 90, readOnly: true);
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

    private static DataTable CreateEmptyMartialArtTable()
    {
        var table = new DataTable();
        table.Columns.Add("martial_art_id", typeof(int));
        table.Columns.Add("is_owned", typeof(bool));
        table.Columns.Add("is_active", typeof(bool));
        table.Columns.Add("code", typeof(string));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("category", typeof(string));
        table.Columns.Add("qi_absorption_rate", typeof(decimal));
        table.Columns.Add("current_stage", typeof(int));
        table.Columns.Add("current_exp", typeof(long));
        return table;
    }

    private static DataTable CreateEditableMartialArtTable(DataTable sourceTable)
    {
        var table = CreateEmptyMartialArtTable();
        if (sourceTable == null || sourceTable.Rows.Count == 0)
            return table;

        foreach (DataRow sourceRow in sourceTable.Rows)
        {
            var row = table.NewRow();
            row["martial_art_id"] = Convert.ToInt32(sourceRow["martial_art_id"], CultureInfo.InvariantCulture);
            row["is_owned"] = Convert.ToBoolean(sourceRow["is_owned"], CultureInfo.InvariantCulture);
            row["is_active"] = Convert.ToBoolean(sourceRow["is_active"], CultureInfo.InvariantCulture);
            row["code"] = Convert.ToString(sourceRow["code"], CultureInfo.InvariantCulture) ?? string.Empty;
            row["name"] = Convert.ToString(sourceRow["name"], CultureInfo.InvariantCulture) ?? string.Empty;
            row["category"] = Convert.ToString(sourceRow["category"], CultureInfo.InvariantCulture) ?? string.Empty;
            row["qi_absorption_rate"] = sourceRow["qi_absorption_rate"] is null or DBNull
                ? 0m
                : Convert.ToDecimal(sourceRow["qi_absorption_rate"], CultureInfo.InvariantCulture);
            row["current_stage"] = sourceRow["current_stage"] is null or DBNull
                ? 0
                : Convert.ToInt32(sourceRow["current_stage"], CultureInfo.InvariantCulture);
            row["current_exp"] = sourceRow["current_exp"] is null or DBNull
                ? 0L
                : Convert.ToInt64(sourceRow["current_exp"], CultureInfo.InvariantCulture);
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

using System.Data;
using System.Globalization;
using Npgsql;

namespace AdminDesignerTool;

internal sealed class CharacterItemWorkspaceControl : UserControl
{
    private readonly string _connectionString;
    private readonly Label _titleLabel;
    private readonly Label _descriptionLabel;
    private readonly TextBox _helpTextBox;
    private readonly Label _statusLabel;
    private readonly SplitContainer _splitContainer;
    private readonly DataGridView _characterGrid;
    private readonly DataGridView _itemGrid;
    private readonly Button _reloadCharactersButton;
    private readonly Button _reloadItemsButton;
    private readonly Label _selectedCharacterLabel;
    private readonly BindingSource _characterBindingSource;
    private readonly BindingSource _itemBindingSource;

    private bool _loadingCharacters;
    private bool _loadingItems;
    private bool _suppressItemEvents;
    private bool _toggleInFlight;
    private Guid? _selectedCharacterId;

    public CharacterItemWorkspaceControl(string connectionString)
    {
        _connectionString = connectionString;

        Dock = DockStyle.Fill;
        Padding = new Padding(12);

        _titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            Text = "Character Items Workspace"
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
        _itemBindingSource = new BindingSource();

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

        _reloadItemsButton = new Button
        {
            AutoSize = true,
            Text = "Tai Lai Item"
        };
        _reloadItemsButton.Click += async (_, _) =>
        {
            if (_selectedCharacterId.HasValue)
                await ReloadItemsAsync(_selectedCharacterId.Value);
        };

        _selectedCharacterLabel = new Label
        {
            AutoSize = true,
            Text = "Chua chon nhan vat",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Margin = new Padding(0, 6, 12, 0)
        };

        var itemHeaderPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        itemHeaderPanel.Controls.Add(_selectedCharacterLabel);
        itemHeaderPanel.Controls.Add(_reloadItemsButton);

        _itemGrid = BuildGrid();
        _itemGrid.DataSource = _itemBindingSource;
        _itemGrid.CurrentCellDirtyStateChanged += ItemGridOnCurrentCellDirtyStateChanged;
        _itemGrid.CellValueChanged += ItemGridOnCellValueChanged;
        _itemGrid.DataError += ItemGridOnDataError;

        _splitContainer.Panel2.Controls.Add(_itemGrid);
        _splitContainer.Panel2.Controls.Add(itemHeaderPanel);

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
                _itemBindingSource.DataSource = CreateEmptyItemTable();
                ConfigureItemGrid();
                SetStatus("Chua co nhan vat nao trong DB.");
                return;
            }

            if (!_selectedCharacterId.HasValue)
                RestoreCharacterSelection(null);

            if (_selectedCharacterId.HasValue)
            {
                _selectedCharacterLabel.Text = $"Item cua: {GetSelectedCharacterName()}";
                await ReloadItemsAsync(_selectedCharacterId.Value);
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
            _itemBindingSource.DataSource = CreateEmptyItemTable();
            ConfigureItemGrid();
            return;
        }

        if (_selectedCharacterId.HasValue && _selectedCharacterId.Value == characterId.Value && !_loadingItems)
            return;

        _selectedCharacterId = characterId.Value;
        _selectedCharacterLabel.Text = $"Item cua: {GetSelectedCharacterName()}";
        await ReloadItemsAsync(characterId.Value);
    }

    private async Task ReloadItemsAsync(Guid characterId)
    {
        if (_loadingItems)
            return;

        _loadingItems = true;
        _suppressItemEvents = true;
        _reloadItemsButton.Enabled = false;
        SetStatus("Dang tai danh sach item...");

        try
        {
            var sourceTable = new DataTable();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                select
                    it.id as item_template_id,
                    coalesce(count(pi.id), 0) > 0 as is_owned,
                    it.item_type,
                    it.rarity,
                    it.code,
                    it.name,
                    it.max_stack,
                    coalesce(sum(pi.quantity), 0) as total_quantity,
                    coalesce(count(pi.id), 0) as instance_count,
                    coalesce(bool_or(pe.player_item_id is not null), false) as has_equipment_instance,
                    coalesce(bool_or(ps.player_item_id is not null), false) as has_soil_instance
                from public.item_templates it
                left join public.player_items pi
                    on pi.player_id = @player_id
                   and pi.item_template_id = it.id
                left join public.player_equipments pe
                    on pe.player_item_id = pi.id
                left join public.player_soils ps
                    on ps.player_item_id = pi.id
                group by
                    it.id,
                    it.item_type,
                    it.rarity,
                    it.code,
                    it.name,
                    it.max_stack
                order by it.id;
                """;

            await using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("player_id", characterId);
                await using var reader = await command.ExecuteReaderAsync();
                sourceTable.Load(reader);
            }

            var table = CreateEditableItemTable(sourceTable);
            _itemBindingSource.DataSource = table;
            ConfigureItemGrid();
            SetStatus($"Da tai {table.Rows.Count} item template cho nhan vat dang chon.");
        }
        catch (Exception ex)
        {
            SetStatus($"Tai item that bai: {ex.Message}", isError: true);
        }
        finally
        {
            _reloadItemsButton.Enabled = true;
            _suppressItemEvents = false;
            _loadingItems = false;
        }
    }

    private void ItemGridOnCurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (_itemGrid.IsCurrentCellDirty)
            _itemGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private async void ItemGridOnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_suppressItemEvents || _loadingItems || _toggleInFlight || e.RowIndex < 0)
            return;

        var column = _itemGrid.Columns[e.ColumnIndex];
        if (!string.Equals(column.DataPropertyName, "is_owned", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(column.DataPropertyName, "total_quantity", StringComparison.OrdinalIgnoreCase))
            return;

        if (!_selectedCharacterId.HasValue)
            return;

        var row = _itemGrid.Rows[e.RowIndex];
        if (row.DataBoundItem is not DataRowView rowView)
            return;

        var itemTemplateId = Convert.ToInt32(rowView["item_template_id"], CultureInfo.InvariantCulture);
        var maxStack = Convert.ToInt32(rowView["max_stack"], CultureInfo.InvariantCulture);
        var targetQuantity = ResolveTargetQuantity(rowView, column.DataPropertyName);
        await SyncItemQuantityAsync(_selectedCharacterId.Value, itemTemplateId, targetQuantity, maxStack);
    }

    private void ItemGridOnDataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        e.ThrowException = false;
        SetStatus("Gia tri so luong item khong hop le. Hay nhap so nguyen >= 0.", isError: true);
    }

    private int ResolveTargetQuantity(DataRowView rowView, string changedColumnName)
    {
        var quantity = ReadQuantity(rowView["total_quantity"]);
        var isOwned = Convert.ToBoolean(rowView["is_owned"], CultureInfo.InvariantCulture);

        if (string.Equals(changedColumnName, "is_owned", StringComparison.OrdinalIgnoreCase))
            quantity = isOwned ? Math.Max(1, quantity) : 0;
        else
            quantity = Math.Max(0, quantity);

        var normalizedOwnership = quantity > 0;
        _suppressItemEvents = true;
        try
        {
            rowView["total_quantity"] = quantity;
            rowView["is_owned"] = normalizedOwnership;
        }
        finally
        {
            _suppressItemEvents = false;
        }

        return quantity;
    }

    private async Task SyncItemQuantityAsync(Guid characterId, int itemTemplateId, int targetQuantity, int maxStack)
    {
        if (_toggleInFlight)
            return;

        _toggleInFlight = true;
        _itemGrid.Enabled = false;
        _reloadCharactersButton.Enabled = false;
        _reloadItemsButton.Enabled = false;
        SetStatus(targetQuantity > 0 ? "Dang cap nhat so luong item..." : "Dang go item...");

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var effectiveMaxStack = Math.Max(1, maxStack);
            var existingItems = await LoadExistingPlayerItemsAsync(connection, transaction, characterId, itemTemplateId);
            var remainingQuantity = Math.Max(0, targetQuantity);

            foreach (var existingItem in existingItems)
            {
                var desiredQuantityForRow = remainingQuantity > 0
                    ? Math.Min(remainingQuantity, effectiveMaxStack)
                    : 0;

                if (desiredQuantityForRow <= 0)
                {
                    await DeletePlayerItemAsync(connection, transaction, existingItem.Id);
                    continue;
                }

                if (existingItem.Quantity != desiredQuantityForRow)
                {
                    await using var command = new NpgsqlCommand(
                        """
                        update public.player_items
                        set quantity = @quantity,
                            updated_at = now()
                        where id = @player_item_id;
                        """,
                        connection,
                        transaction);
                    command.Parameters.AddWithValue("quantity", desiredQuantityForRow);
                    command.Parameters.AddWithValue("player_item_id", existingItem.Id);
                    await command.ExecuteNonQueryAsync();
                }

                await EnsureDependentRowsAsync(connection, transaction, existingItem.Id, itemTemplateId);
                remainingQuantity -= desiredQuantityForRow;
            }

            while (remainingQuantity > 0)
            {
                var newStackQuantity = Math.Min(remainingQuantity, effectiveMaxStack);
                var playerItemId = await CreatePlayerItemAsync(connection, transaction, characterId, itemTemplateId, newStackQuantity);
                await EnsureDependentRowsAsync(connection, transaction, playerItemId, itemTemplateId);
                remainingQuantity -= newStackQuantity;
            }

            await transaction.CommitAsync();
            SetStatus(targetQuantity > 0
                ? $"Da cap nhat item ve tong so luong {targetQuantity}."
                : "Da go item khoi nhan vat.");
        }
        catch (Exception ex)
        {
            SetStatus($"Cap nhat item that bai: {ex.Message}", isError: true);
        }
        finally
        {
            _toggleInFlight = false;
            _itemGrid.Enabled = true;
            _reloadCharactersButton.Enabled = true;
            _reloadItemsButton.Enabled = true;
            await ReloadItemsAsync(characterId);
        }
    }

    private static int ReadQuantity(object? value)
    {
        if (value is null or DBNull)
            return 0;

        return Math.Max(0, Convert.ToInt32(value, CultureInfo.InvariantCulture));
    }

    private static async Task<List<PlayerItemInstanceRow>> LoadExistingPlayerItemsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid characterId,
        int itemTemplateId)
    {
        const string sql = """
            select
                pi.id,
                pi.quantity,
                coalesce(pe.equipped_slot is not null, false) as is_equipped,
                coalesce(ps.inserted_plot_id is not null, false) as is_inserted_soil
            from public.player_items pi
            left join public.player_equipments pe
                on pe.player_item_id = pi.id
            left join public.player_soils ps
                on ps.player_item_id = pi.id
            where pi.player_id = @player_id
              and pi.item_template_id = @item_template_id
            order by
                coalesce(pe.equipped_slot is not null, false) desc,
                coalesce(ps.inserted_plot_id is not null, false) desc,
                pi.acquired_at,
                pi.id;
            """;

        var items = new List<PlayerItemInstanceRow>();
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("player_id", characterId);
        command.Parameters.AddWithValue("item_template_id", itemTemplateId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new PlayerItemInstanceRow(
                reader.GetInt64(0),
                reader.GetInt32(1),
                reader.GetBoolean(2),
                reader.GetBoolean(3)));
        }

        return items;
    }

    private static async Task<long> CreatePlayerItemAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid characterId,
        int itemTemplateId,
        int quantity)
    {
        const string sql = """
            insert into public.player_items (
                player_id,
                item_template_id,
                location_type,
                quantity,
                is_bound,
                acquired_at,
                updated_at
            )
            values (
                @player_id,
                @item_template_id,
                1,
                @quantity,
                false,
                now(),
                now()
            )
            returning id;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("player_id", characterId);
        command.Parameters.AddWithValue("item_template_id", itemTemplateId);
        command.Parameters.AddWithValue("quantity", quantity);
        return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task EnsureDependentRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long playerItemId,
        int itemTemplateId)
    {
        const string ensureEquipmentSql = """
            insert into public.player_equipments (
                player_item_id,
                equipped_slot,
                enhance_level,
                durability,
                updated_at
            )
            select
                @player_item_id,
                null,
                0,
                null,
                now()
            where exists (
                select 1
                from public.equipment_templates
                where item_template_id = @item_template_id
            )
            on conflict (player_item_id) do nothing;
            """;

        await using (var command = new NpgsqlCommand(ensureEquipmentSql, connection, transaction))
        {
            command.Parameters.AddWithValue("player_item_id", playerItemId);
            command.Parameters.AddWithValue("item_template_id", itemTemplateId);
            await command.ExecuteNonQueryAsync();
        }

        const string ensureSoilSql = """
            insert into public.player_soils (
                player_item_id,
                total_used_seconds,
                state,
                inserted_plot_id,
                updated_at
            )
            select
                @player_item_id,
                0,
                1,
                null,
                now()
            where exists (
                select 1
                from public.soil_templates
                where item_template_id = @item_template_id
            )
            on conflict (player_item_id) do nothing;
            """;

        await using var soilCommand = new NpgsqlCommand(ensureSoilSql, connection, transaction);
        soilCommand.Parameters.AddWithValue("player_item_id", playerItemId);
        soilCommand.Parameters.AddWithValue("item_template_id", itemTemplateId);
        await soilCommand.ExecuteNonQueryAsync();
    }

    private static async Task DeletePlayerItemAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long playerItemId)
    {
        const string clearPlotsSql = """
            update public.player_garden_plots
            set current_soil_player_item_id = null,
                updated_at = now()
            where current_soil_player_item_id = @player_item_id;
            """;

        await using (var command = new NpgsqlCommand(clearPlotsSql, connection, transaction))
        {
            command.Parameters.AddWithValue("player_item_id", playerItemId);
            await command.ExecuteNonQueryAsync();
        }

        const string clearEquipmentBonusesSql = """
            delete from public.player_equipment_stat_bonuses
            where player_item_id = @player_item_id;
            """;

        await using (var command = new NpgsqlCommand(clearEquipmentBonusesSql, connection, transaction))
        {
            command.Parameters.AddWithValue("player_item_id", playerItemId);
            await command.ExecuteNonQueryAsync();
        }

        const string clearEquipmentSql = """
            delete from public.player_equipments
            where player_item_id = @player_item_id;
            """;

        await using (var command = new NpgsqlCommand(clearEquipmentSql, connection, transaction))
        {
            command.Parameters.AddWithValue("player_item_id", playerItemId);
            await command.ExecuteNonQueryAsync();
        }

        const string clearSoilsSql = """
            delete from public.player_soils
            where player_item_id = @player_item_id;
            """;

        await using (var command = new NpgsqlCommand(clearSoilsSql, connection, transaction))
        {
            command.Parameters.AddWithValue("player_item_id", playerItemId);
            await command.ExecuteNonQueryAsync();
        }

        const string deleteItemSql = """
            delete from public.player_items
            where id = @player_item_id;
            """;

        await using var deleteCommand = new NpgsqlCommand(deleteItemSql, connection, transaction);
        deleteCommand.Parameters.AddWithValue("player_item_id", playerItemId);
        await deleteCommand.ExecuteNonQueryAsync();
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

    private void ConfigureItemGrid()
    {
        ConfigureSharedGridBehavior(_itemGrid);
        SetColumn(_itemGrid, "item_template_id", "Id", width: 60, readOnly: true);
        SetColumn(_itemGrid, "is_owned", "So Huu", width: 70, readOnly: false);
        SetColumn(_itemGrid, "item_type", "Loai", width: 70, readOnly: true);
        SetColumn(_itemGrid, "rarity", "Pham Chat", width: 80, readOnly: true);
        SetColumn(_itemGrid, "code", "Code", width: 180, readOnly: true);
        SetColumn(_itemGrid, "name", "Ten Item", width: 240, readOnly: true);
        SetColumn(_itemGrid, "total_quantity", "Tong So Luong", width: 110, readOnly: false);
        SetColumn(_itemGrid, "instance_count", "So Dong", visible: false);
        SetColumn(_itemGrid, "has_equipment_instance", "Co Equipment Row", visible: false);
        SetColumn(_itemGrid, "has_soil_instance", "Co Soil Row", visible: false);
        SetColumn(_itemGrid, "max_stack", "Max Stack", visible: false);
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

    private static DataTable CreateEmptyItemTable()
    {
        var table = new DataTable();
        table.Columns.Add("item_template_id", typeof(int));
        table.Columns.Add("is_owned", typeof(bool));
        table.Columns.Add("item_type", typeof(int));
        table.Columns.Add("rarity", typeof(int));
        table.Columns.Add("code", typeof(string));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("total_quantity", typeof(int));
        table.Columns.Add("instance_count", typeof(int));
        table.Columns.Add("has_equipment_instance", typeof(bool));
        table.Columns.Add("has_soil_instance", typeof(bool));
        table.Columns.Add("max_stack", typeof(int));
        return table;
    }

    private static DataTable CreateEditableItemTable(DataTable sourceTable)
    {
        var table = CreateEmptyItemTable();
        if (sourceTable == null || sourceTable.Rows.Count == 0)
            return table;

        foreach (DataRow sourceRow in sourceTable.Rows)
        {
            var row = table.NewRow();
            row["item_template_id"] = Convert.ToInt32(sourceRow["item_template_id"], CultureInfo.InvariantCulture);
            row["is_owned"] = Convert.ToBoolean(sourceRow["is_owned"], CultureInfo.InvariantCulture);
            row["item_type"] = Convert.ToInt32(sourceRow["item_type"], CultureInfo.InvariantCulture);
            row["rarity"] = Convert.ToInt32(sourceRow["rarity"], CultureInfo.InvariantCulture);
            row["code"] = Convert.ToString(sourceRow["code"], CultureInfo.InvariantCulture) ?? string.Empty;
            row["name"] = Convert.ToString(sourceRow["name"], CultureInfo.InvariantCulture) ?? string.Empty;
            row["total_quantity"] = sourceRow["total_quantity"] is null or DBNull
                ? 0
                : Convert.ToInt32(sourceRow["total_quantity"], CultureInfo.InvariantCulture);
            row["instance_count"] = sourceRow["instance_count"] is null or DBNull
                ? 0
                : Convert.ToInt32(sourceRow["instance_count"], CultureInfo.InvariantCulture);
            row["has_equipment_instance"] = Convert.ToBoolean(sourceRow["has_equipment_instance"], CultureInfo.InvariantCulture);
            row["has_soil_instance"] = Convert.ToBoolean(sourceRow["has_soil_instance"], CultureInfo.InvariantCulture);
            row["max_stack"] = sourceRow["max_stack"] is null or DBNull
                ? 1
                : Convert.ToInt32(sourceRow["max_stack"], CultureInfo.InvariantCulture);
            table.Rows.Add(row);
        }

        return table;
    }

    private void SetStatus(string message, bool isError = false)
    {
        _statusLabel.Text = message ?? string.Empty;
        _statusLabel.ForeColor = isError ? Color.Firebrick : Color.DimGray;
    }

    private readonly record struct PlayerItemInstanceRow(
        long Id,
        int Quantity,
        bool IsEquipped,
        bool IsInsertedSoil);
}

using System.Data;
using Npgsql;

namespace AdminDesignerTool;

internal sealed class TableEditorControl : UserControl
{
    private readonly Label _titleLabel;
    private readonly Label _descriptionLabel;
    private readonly TextBox _helpTextBox;
    private readonly Button _refreshButton;
    private readonly Button _addButton;
    private readonly Button _duplicateButton;
    private readonly Button _deleteButton;
    private readonly Button _saveButton;
    private readonly TextBox _filterTextBox;
    private readonly Button _clearFilterButton;
    private readonly DataGridView _grid;
    private readonly Label _statusLabel;

    private readonly string _connectionString;
    private AdminResourceDefinition? _resource;
    private DataTable? _table;
    private NpgsqlDataAdapter? _adapter;

    public TableEditorControl(string connectionString)
    {
        _connectionString = connectionString;

        Dock = DockStyle.Fill;
        Padding = new Padding(12);

        _titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            Text = "Chon mot resource ben trai"
        };

        _descriptionLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 52,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = Color.DimGray,
            Text = "Tool nay cho phep them/sua/xoa truc tiep cac bang template trong DB."
        };

        _helpTextBox = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 82,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.WhiteSmoke,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Text = "Huong dan resource se hien o day."
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            AutoSize = false,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        _refreshButton = new Button { Text = "Tai Lai", AutoSize = true };
        _addButton = new Button { Text = "Them Dong", AutoSize = true };
        _duplicateButton = new Button { Text = "Nhan Ban Dong", AutoSize = true };
        _deleteButton = new Button { Text = "Xoa Dong", AutoSize = true };
        _saveButton = new Button { Text = "Luu Thay Doi", AutoSize = true };

        _refreshButton.Click += async (_, _) => await ReloadAsync();
        _addButton.Click += (_, _) => AddRow();
        _duplicateButton.Click += (_, _) => DuplicateSelectedRow();
        _deleteButton.Click += (_, _) => DeleteSelectedRows();
        _saveButton.Click += async (_, _) => await SaveChangesAsync();

        buttonPanel.Controls.AddRange([_refreshButton, _addButton, _duplicateButton, _deleteButton, _saveButton]);

        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            AutoSize = false,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        filterPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Loc nhanh:",
            Margin = new Padding(3, 9, 3, 0)
        });

        _filterTextBox = new TextBox
        {
            Width = 280
        };
        _filterTextBox.TextChanged += (_, _) => ApplyFilter();

        _clearFilterButton = new Button
        {
            Text = "Xoa Loc",
            AutoSize = true
        };
        _clearFilterButton.Click += (_, _) =>
        {
            _filterTextBox.Text = string.Empty;
            ApplyFilter();
        };

        filterPanel.Controls.Add(_filterTextBox);
        filterPanel.Controls.Add(_clearFilterButton);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true
        };

        _statusLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            ForeColor = Color.DimGray,
            Text = "San sang."
        };

        Controls.Add(_grid);
        Controls.Add(_statusLabel);
        Controls.Add(filterPanel);
        Controls.Add(buttonPanel);
        Controls.Add(_helpTextBox);
        Controls.Add(_descriptionLabel);
        Controls.Add(_titleLabel);
    }

    public async Task LoadResourceAsync(AdminResourceDefinition resource)
    {
        _resource = resource;
        _titleLabel.Text = $"{resource.DisplayName}  ({resource.TableName})";
        _descriptionLabel.Text = resource.Description;
        _helpTextBox.Text = resource.HelpText;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (_resource is null)
            return;

        if (_resource.TableName == "monster_templates")
        {
            _grid.DataSource = null;
            _statusLabel.Text = "Schema boss/drop chua ton tai. Resource nay la cho mo rong sau.";
            return;
        }

        try
        {
            SetBusy(true, "Dang tai du lieu...");
            var result = await Task.Run(() => LoadTable(_resource), default);
            _adapter = result.Adapter;
            _table = result.Table;
            _grid.DataSource = _table.DefaultView;
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _grid.DataSource = null;
            _statusLabel.Text = $"Tai du lieu that bai: {ex.Message}";
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private async Task SaveChangesAsync()
    {
        if (_adapter is null || _table is null || _resource is null)
            return;

        try
        {
            SetBusy(true, "Dang luu thay doi...");
            await Task.Run(() =>
            {
                using var builder = new NpgsqlCommandBuilder(_adapter)
                {
                    QuotePrefix = "\"",
                    QuoteSuffix = "\""
                };
                _adapter.InsertCommand = builder.GetInsertCommand();
                _adapter.UpdateCommand = builder.GetUpdateCommand();
                _adapter.DeleteCommand = builder.GetDeleteCommand();
                _adapter.Update(_table);
            });

            _statusLabel.Text = $"Da luu thay doi cho {_resource.TableName}.";
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Luu that bai: {ex.Message}";
            MessageBox.Show(
                $"Khong the luu thay doi cho {_resource.TableName}.\r\n\r\n{ex.Message}",
                "Save Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private void AddRow()
    {
        if (_table is null)
            return;

        _table.Rows.Add(_table.NewRow());
        _statusLabel.Text = "Da them mot dong moi. Nhap du lieu roi bam Luu Thay Doi.";
    }

    private void DuplicateSelectedRow()
    {
        if (_table is null || _grid.CurrentRow?.DataBoundItem is not DataRowView sourceView)
            return;

        var newRow = _table.NewRow();
        foreach (DataColumn column in _table.Columns)
        {
            if (column.AutoIncrement)
                continue;

            if (sourceView.Row.RowState == DataRowState.Deleted)
                continue;

            newRow[column.ColumnName] = sourceView.Row[column.ColumnName];
        }

        _table.Rows.Add(newRow);
        _statusLabel.Text = "Da nhan ban dong hien tai. Kiem tra lai id/code truoc khi luu.";
    }

    private void DeleteSelectedRows()
    {
        if (_table is null || _grid.SelectedRows.Count == 0)
            return;

        foreach (DataGridViewRow selectedRow in _grid.SelectedRows)
        {
            if (selectedRow.DataBoundItem is not DataRowView rowView)
                continue;

            rowView.Row.Delete();
        }

        _statusLabel.Text = "Da danh dau xoa cac dong duoc chon. Bam Luu Thay Doi de ap dung.";
    }

    private void SetBusy(bool isBusy, string? statusText)
    {
        _refreshButton.Enabled = !isBusy;
        _addButton.Enabled = !isBusy;
        _duplicateButton.Enabled = !isBusy;
        _deleteButton.Enabled = !isBusy;
        _saveButton.Enabled = !isBusy;
        _filterTextBox.Enabled = !isBusy;
        _clearFilterButton.Enabled = !isBusy;
        _grid.Enabled = !isBusy;
        if (!string.IsNullOrWhiteSpace(statusText))
            _statusLabel.Text = statusText;
    }

    private (NpgsqlDataAdapter Adapter, DataTable Table) LoadTable(AdminResourceDefinition resource)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        using var command = new NpgsqlCommand(resource.SelectSql, connection);
        var adapter = new NpgsqlDataAdapter(command);
        var table = new DataTable(resource.TableName);
        adapter.Fill(table);
        return (adapter, table);
    }

    private void ApplyFilter()
    {
        if (_table?.DefaultView is null)
            return;

        var keyword = _filterTextBox.Text.Trim().Replace("'", "''");
        if (string.IsNullOrWhiteSpace(keyword))
        {
            _table.DefaultView.RowFilter = string.Empty;
            _statusLabel.Text = _resource is null
                ? "San sang."
                : $"Dang hien {_table.DefaultView.Count}/{_table.Rows.Count} dong cua {_resource.TableName}.";
            return;
        }

        var filterParts = _table.Columns
            .Cast<DataColumn>()
            .Where(CanFilterColumn)
            .Select(column => $"Convert([{column.ColumnName}], 'System.String') LIKE '%{keyword}%'")
            .ToArray();

        if (filterParts.Length == 0)
        {
            _statusLabel.Text = "Bang nay khong co cot phu hop de loc nhanh.";
            return;
        }

        try
        {
            _table.DefaultView.RowFilter = string.Join(" OR ", filterParts);
            _statusLabel.Text = _resource is null
                ? $"Dang hien {_table.DefaultView.Count}/{_table.Rows.Count} dong."
                : $"Dang hien {_table.DefaultView.Count}/{_table.Rows.Count} dong cua {_resource.TableName}.";
        }
        catch (EvaluateException)
        {
            _table.DefaultView.RowFilter = string.Empty;
            _statusLabel.Text = "Khong the ap dung bo loc cho bang nay.";
        }
    }

    private static bool CanFilterColumn(DataColumn column)
    {
        var type = column.DataType;
        return type == typeof(string)
            || type == typeof(short)
            || type == typeof(int)
            || type == typeof(long)
            || type == typeof(decimal)
            || type == typeof(double)
            || type == typeof(float)
            || type == typeof(bool)
            || type == typeof(DateTime)
            || type == typeof(Guid);
    }
}

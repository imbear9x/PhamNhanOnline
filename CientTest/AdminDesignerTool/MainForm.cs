namespace AdminDesignerTool;

internal sealed class MainForm : Form
{
    private readonly SplitContainer _splitContainer;
    private readonly TreeView _navigationTree;
    private readonly TableEditorControl? _editorControl;
    private readonly Label _connectionInfoLabel;
    private readonly IReadOnlyDictionary<string, AdminResourceDefinition> _resourcesByKey;

    public MainForm()
    {
        Text = "Pham Nhan Online - Admin Designer Tool";
        Width = 1500;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;
        Load += OnLoad;

        if (!DatabaseConfigResolver.TryResolve(out var connectionString, out var configPath, out var error))
        {
            MessageBox.Show(
                error,
                "Khong tim thay dbConfig",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        _resourcesByKey = AdminResourceCatalog.Build().ToDictionary(x => x.Key);

        _splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill
        };

        _navigationTree = new TreeView
        {
            Dock = DockStyle.Fill,
            HideSelection = false,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
        };
        _navigationTree.AfterSelect += NavigationTreeOnAfterSelect;

        var navHeader = new Label
        {
            Dock = DockStyle.Top,
            Height = 44,
            Text = "Tai Nguyen Config",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            Padding = new Padding(12, 8, 0, 0)
        };

        var navPanel = new Panel { Dock = DockStyle.Fill };
        navPanel.Controls.Add(_navigationTree);
        navPanel.Controls.Add(navHeader);
        _splitContainer.Panel1.Controls.Add(navPanel);

        _connectionInfoLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Color.DimGray,
            Padding = new Padding(12, 8, 0, 0),
            Text = string.IsNullOrWhiteSpace(configPath)
                ? "Chua resolve duoc GameServer/Config/dbConfig.json"
                : $"Dang dung dbConfig: {configPath}"
        };

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _editorControl = new TableEditorControl(connectionString) { Dock = DockStyle.Fill };
            _splitContainer.Panel2.Controls.Add(_editorControl);
        }
        else
        {
            _splitContainer.Panel2.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12f, FontStyle.Regular),
                Text = "Khong the mo admin editor vi chua tim thay dbConfig hop le."
            });
        }

        _splitContainer.Panel2.Controls.Add(_connectionInfoLabel);
        Controls.Add(_splitContainer);

        BuildNavigation();
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        _splitContainer.Panel1MinSize = 280;
        _splitContainer.Panel2MinSize = 600;
        var preferredDistance = 320;
        var maxAllowedDistance = _splitContainer.Width - _splitContainer.Panel2MinSize;
        _splitContainer.SplitterDistance = Math.Max(_splitContainer.Panel1MinSize, Math.Min(preferredDistance, maxAllowedDistance));
    }

    private async void NavigationTreeOnAfterSelect(object? sender, TreeViewEventArgs e)
    {
        var selectedNode = e.Node;
        if (_editorControl is null || selectedNode is null || selectedNode.Tag is not string key)
            return;
        if (!_resourcesByKey.TryGetValue(key, out var resource))
            return;

        await _editorControl.LoadResourceAsync(resource);
    }

    private void BuildNavigation()
    {
        _navigationTree.Nodes.Clear();
        foreach (var group in _resourcesByKey.Values.OrderBy(x => x.Category).ThenBy(x => x.DisplayName).GroupBy(x => x.Category))
        {
            var parent = new TreeNode(group.Key);
            foreach (var resource in group)
            {
                parent.Nodes.Add(new TreeNode(resource.DisplayName)
                {
                    Tag = resource.Key,
                    ToolTipText = resource.Description
                });
            }

            _navigationTree.Nodes.Add(parent);
            parent.Expand();
        }

        if (_navigationTree.Nodes.Count > 0)
        {
            var firstGroup = _navigationTree.Nodes[0];
            if (firstGroup is not null && firstGroup.Nodes.Count > 0)
                _navigationTree.SelectedNode = firstGroup.Nodes[0];
        }
    }
}

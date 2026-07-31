namespace MaskingTool;

public sealed class MainForm : Form
{
    private readonly MappingStore _store = new();
    private List<MappingEntry> _entries = [];

    // 登録タブ
    private readonly TextBox _registerInput = new();
    private readonly ListView _registeredList = new();
    private readonly Button _convertButton = new();

    // 編集タブ
    private readonly TextBox _editInput = new();
    private readonly TextBox _editOutput = new();
    private readonly Button _editButton = new();
    private readonly Button _copyButton = new();

    public MainForm()
    {
        Text = "MaskingTool";
        Width = 560;
        Height = 640;
        MinimumSize = new Size(480, 520);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Yu Gothic UI", 9.5f);

        try
        {
            Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "app.ico"));
        }
        catch
        {
            // アイコンが見つからない場合は既定のアイコンのまま続行する
        }

        _entries = _store.Load();

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildRegisterTab());
        tabs.TabPages.Add(BuildEditTab());
        Controls.Add(tabs);

        RefreshRegisteredList();
    }

    private TabPage BuildRegisterTab()
    {
        var page = new TabPage("登録");

        var label = new Label
        {
            Text = "クライアントID・プロジェクト名など（1行に1件）",
            Dock = DockStyle.Top,
            Height = 24,
            Padding = new Padding(8, 8, 8, 0),
        };

        _registerInput.Multiline = true;
        _registerInput.Dock = DockStyle.Top;
        _registerInput.Height = 120;
        _registerInput.Margin = new Padding(8);
        _registerInput.ScrollBars = ScrollBars.Vertical;
        _registerInput.Font = new Font("Consolas", 9.5f);

        var inputPanel = new Panel { Dock = DockStyle.Top, Height = 130, Padding = new Padding(8, 0, 8, 0) };
        inputPanel.Controls.Add(_registerInput);

        _convertButton.Text = "変換";
        _convertButton.Width = 100;
        _convertButton.Height = 30;
        _convertButton.Click += OnConvertClicked;

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8, 4, 8, 4),
        };
        buttonPanel.Controls.Add(_convertButton);

        var listLabel = new Label
        {
            Text = "登録済み一覧",
            Dock = DockStyle.Top,
            Height = 24,
            Padding = new Padding(8, 8, 8, 0),
        };

        _registeredList.View = View.Details;
        _registeredList.Dock = DockStyle.Fill;
        _registeredList.FullRowSelect = true;
        _registeredList.GridLines = true;
        _registeredList.Columns.Add("元の文字列", 220);
        _registeredList.Columns.Add("変換後", 220);

        var listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 8) };
        listPanel.Controls.Add(_registeredList);

        page.Controls.Add(listPanel);
        page.Controls.Add(listLabel);
        page.Controls.Add(buttonPanel);
        page.Controls.Add(inputPanel);
        page.Controls.Add(label);

        return page;
    }

    private TabPage BuildEditTab()
    {
        var page = new TabPage("編集");

        var inputLabel = new Label
        {
            Text = "コマンド・エラーメッセージを貼り付け",
            Dock = DockStyle.Top,
            Height = 24,
            Padding = new Padding(8, 8, 8, 0),
        };

        _editInput.Multiline = true;
        _editInput.Dock = DockStyle.Fill;
        _editInput.ScrollBars = ScrollBars.Vertical;
        _editInput.Font = new Font("Consolas", 9.5f);

        var inputPanel = new Panel { Dock = DockStyle.Top, Height = 160, Padding = new Padding(8, 0, 8, 0) };
        inputPanel.Controls.Add(_editInput);

        _editButton.Text = "編集";
        _editButton.Width = 100;
        _editButton.Height = 30;
        _editButton.Click += OnEditClicked;

        var editButtonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8, 4, 8, 4),
        };
        editButtonPanel.Controls.Add(_editButton);

        var resultHeaderPanel = new Panel { Dock = DockStyle.Top, Height = 32, Padding = new Padding(8, 4, 8, 0) };
        var resultLabel = new Label
        {
            Text = "変換結果",
            Dock = DockStyle.Left,
            AutoSize = true,
            Padding = new Padding(0, 6, 0, 0),
        };
        _copyButton.Text = "コピー";
        _copyButton.Width = 90;
        _copyButton.Height = 26;
        _copyButton.Dock = DockStyle.Right;
        _copyButton.Click += OnCopyClicked;
        resultHeaderPanel.Controls.Add(_copyButton);
        resultHeaderPanel.Controls.Add(resultLabel);

        _editOutput.Multiline = true;
        _editOutput.Dock = DockStyle.Fill;
        _editOutput.ScrollBars = ScrollBars.Vertical;
        _editOutput.ReadOnly = true;
        _editOutput.Font = new Font("Consolas", 9.5f);
        _editOutput.BackColor = SystemColors.Control;

        var outputPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 8) };
        outputPanel.Controls.Add(_editOutput);

        page.Controls.Add(outputPanel);
        page.Controls.Add(resultHeaderPanel);
        page.Controls.Add(editButtonPanel);
        page.Controls.Add(inputPanel);
        page.Controls.Add(inputLabel);

        return page;
    }

    private void OnConvertClicked(object? sender, EventArgs e)
    {
        var lines = _registerInput.Lines;
        _entries = _store.AddEntries(_entries, lines);
        _store.Save(_entries);
        _registerInput.Clear();
        RefreshRegisteredList();
    }

    private void RefreshRegisteredList()
    {
        _registeredList.Items.Clear();
        foreach (var entry in _entries)
        {
            var item = new ListViewItem(entry.Original);
            item.SubItems.Add(entry.Masked);
            _registeredList.Items.Add(item);
        }
    }

    private void OnEditClicked(object? sender, EventArgs e)
    {
        _editOutput.Text = Masker.Mask(_editInput.Text, _entries);
    }

    private void OnCopyClicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_editOutput.Text))
        {
            Clipboard.SetText(_editOutput.Text);
        }
    }
}

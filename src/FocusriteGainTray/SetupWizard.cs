namespace FocusriteGainTray;

internal sealed class SetupWizard : Form
{
    private readonly Icon _ownedIcon = AppIcons.Load();
    private readonly IReadOnlyList<SetupGuideStep> _steps = SetupGuide.Steps();
    private readonly Dictionary<string, Image> _images = new();
    private readonly Label _title = new() { AutoSize = true, Dock = DockStyle.Fill };
    private readonly Label _position = new() { AutoSize = true, Dock = DockStyle.Fill };
    private readonly RichTextBox _instructions = new() { ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = SystemColors.Control, ScrollBars = RichTextBoxScrollBars.Vertical, WordWrap = true, DetectUrls = false, Dock = DockStyle.Fill, TabStop = false };
    private readonly PictureBox _picture = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(235, 239, 244), Cursor = Cursors.Hand };
    private readonly Label _caption = new() { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };
    private readonly Button _back = new() { AutoSize = true };
    private readonly Button _next = new() { AutoSize = true };
    private int _index;

    public SetupWizard()
    {
        Text = Ui.T("FocGrain — мастер настройки FC2", "FocGrain — FC2 setup wizard");
        Icon = _ownedIcon;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(980, 650);
        MinimumSize = new Size(720, 520);
        StartPosition = FormStartPosition.CenterScreen;
        var family = (SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont).FontFamily;
        _title.Font = new Font(family, 15, FontStyle.Bold);
        _instructions.Font = new Font(family, 10);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 6 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(_title, 0, 0); layout.Controls.Add(_position, 0, 1);
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 63));
        var imagePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12, 0, 0, 0) };
        imagePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); imagePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        imagePanel.Controls.Add(_picture, 0, 0); imagePanel.Controls.Add(_caption, 0, 1);
        content.Controls.Add(_instructions, 0, 0); content.Controls.Add(imagePanel, 1, 0);
        layout.Controls.Add(content, 0, 3);
        layout.Controls.Add(new Label { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 12, 0, 8), Text = Ui.T("Настройки FC2 меняются вручную. Мастер не отправляет команды gain.", "Change FC2 settings manually. The wizard sends no gain commands.") }, 0, 4);
        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var enlarge = new Button { AutoSize = true, Text = Ui.T("Увеличить снимок", "Enlarge screenshot"), Anchor = AnchorStyles.Left };
        enlarge.Click += (_, _) => Enlarge(); _picture.Click += (_, _) => Enlarge();
        footer.Controls.Add(enlarge, 0, 0);
        var navigation = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        var close = new Button { AutoSize = true, Text = Ui.T("Закрыть мастер", "Close wizard"), DialogResult = DialogResult.Cancel };
        close.Click += (_, _) => Close();
        _back.Text = Ui.T("Назад", "Back"); _back.Click += (_, _) => ShowStep(_index - 1);
        _next.Click += (_, _) => { if (_index == _steps.Count - 1) Close(); else ShowStep(_index + 1); };
        navigation.Controls.Add(close); navigation.Controls.Add(_back); navigation.Controls.Add(_next);
        footer.Controls.Add(navigation, 1, 0); layout.Controls.Add(footer, 0, 5);
        Controls.Add(layout); AcceptButton = _next; CancelButton = close;
        Shown += (_, _) =>
        {
            var area = Screen.FromControl(this).WorkingArea;
            MinimumSize = new Size(Math.Min(720, area.Width - 16), Math.Min(520, area.Height - 16));
            Size = new Size(Math.Min(Width, area.Width - 24), Math.Min(Height, area.Height - 24));
        };
        ShowStep(0);
    }

    internal void ShowStep(int index)
    {
        if (index < 0 || index >= _steps.Count) throw new ArgumentOutOfRangeException(nameof(index));
        _index = index;
        var step = _steps[index];
        _title.Text = step.Title;
        _position.Text = Ui.T($"Шаг {index + 1} из {_steps.Count}", $"Step {index + 1} of {_steps.Count}");
        _instructions.Text = step.Instructions; _instructions.SelectionStart = 0;
        _caption.Text = step.Caption;
        if (!_images.TryGetValue(step.Image, out var image))
        {
            using var stream = typeof(SetupWizard).Assembly.GetManifestResourceStream("FocGrain.Setup." + step.Image)
                ?? throw new InvalidOperationException("Missing setup screenshot: " + step.Image);
            using var loaded = Image.FromStream(stream);
            image = new Bitmap(loaded); _images.Add(step.Image, image);
        }
        _picture.Image = image;
        _back.Enabled = index > 0;
        _next.Text = index == _steps.Count - 1 ? Ui.T("Готово", "Finish") : Ui.T("Далее", "Next");
    }

    private void Enlarge()
    {
        var icon = AppIcons.Load();
        var viewer = new Form { Text = _caption.Text, Icon = icon, Size = new Size(950, 740), StartPosition = FormStartPosition.CenterParent, ShowInTaskbar = false, KeyPreview = true };
        viewer.Disposed += (_, _) => icon.Dispose();
        viewer.KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) viewer.Close(); };
        viewer.Controls.Add(new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, Image = _picture.Image });
        viewer.Show(this);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        foreach (var image in _images.Values) image.Dispose();
        _images.Clear(); _ownedIcon.Dispose();
    }
}

internal static class WizardHost
{
    private static SetupWizard? _current;
    public static void Show(IWin32Window? owner = null)
    {
        if (owner is not null)
        {
            using var dialog = new SetupWizard { ShowInTaskbar = false };
            dialog.ShowDialog(owner);
            return;
        }
        if (_current is { IsDisposed: false }) { _current.WindowState = FormWindowState.Normal; _current.Activate(); return; }
        _current = new SetupWizard();
        _current.FormClosed += (_, _) => _current = null;
        _current.Show();
    }
    public static void Close() => _current?.Close();
}

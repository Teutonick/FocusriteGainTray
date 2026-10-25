namespace FocusriteGainTray;

internal sealed class SettingsDialog : Form
{
    private readonly Icon _ownedIcon = AppIcons.Load();
    private readonly NumericUpDown _a1;
    private readonly NumericUpDown _b1;
    private readonly NumericUpDown _a2;
    private readonly NumericUpDown _b2;
    private readonly NumericUpDown _step;
    private readonly HotkeyCaptureBox _toggle;
    private readonly HotkeyCaptureBox _increase;
    private readonly HotkeyCaptureBox _decrease;
    public AppSettings? ResultSettings { get; private set; }

    public SettingsDialog(AppSettings current)
    {
        Text = Ui.T("FocGrain — настройки", "FocGrain — settings");
        ClientSize = new Size(570, 465);
        Icon = _ownedIcon;
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 9,
            AutoSize = true,
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 285));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        Controls.Add(table);

        var input = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190 };
        input.Items.AddRange(new object[] { "Analog 1", "Analog 2" });
        input.SelectedIndex = current.SelectedInput - 1;
        AddRow(table, Ui.T("Управляемый вход", "Controlled input"), input);
        _a1 = AddNumber(table, Ui.T("Analog 1 · минимум (MIDI 0–127)", "Analog 1 · minimum (MIDI 0–127)"), current.Input1.A);
        _b1 = AddNumber(table, Ui.T("Analog 1 · максимум (MIDI 0–127)", "Analog 1 · maximum (MIDI 0–127)"), current.Input1.B);
        _a2 = AddNumber(table, Ui.T("Analog 2 · минимум (MIDI 0–127)", "Analog 2 · minimum (MIDI 0–127)"), current.Input2.A);
        _b2 = AddNumber(table, Ui.T("Analog 2 · максимум (MIDI 0–127)", "Analog 2 · maximum (MIDI 0–127)"), current.Input2.B);
        _step = AddNumber(table, Ui.T("Шаг регулировки (1–32)", "Adjustment step (1–32)"), current.Step, 1, 32);

        _toggle = AddHotkey(table, Ui.T("Переключить A ↔ B", "Toggle MIN ↔ MAX"), current.ToggleHotkey);
        _increase = AddHotkey(table, Ui.T("Прибавить Gain", "Increase gain"), current.IncreaseHotkey);
        _decrease = AddHotkey(table, Ui.T("Убавить Gain", "Decrease gain"), current.DecreaseHotkey);

        var note = new Label
        {
            Left = 17, Top = 325, Width = 535, Height = 42,
            Text = Ui.T("MIDI 0–127 — не дБ. Текущий gain устройства не считывается.\nДля хоткея выберите поле и нажмите комбинацию клавиш.", "MIDI 0–127 is not dB. Hardware gain cannot be read back.\nTo set a shortcut, click its field and press the key combination.")
        };
        Controls.Add(note);
        var version = new Label { Text = $"FocGrain {ProductInfo.VersionText}", Left = 17, Top = 378, Width = 200, Height = 22 };
        var updates = new Button { Text = Ui.T("Проверить обновления…", "Check for updates…"), Left = 17, Top = 411, Width = 215, Height = 30 };
        updates.Click += (_, _) => { using var dialog = new UpdateDialog(); dialog.ShowDialog(this); };
        Controls.Add(version);
        Controls.Add(updates);
        var guide = new LinkLabel { Text = Ui.T("Мастер настройки FC2…", "FC2 setup wizard…"), Left = 290, Top = 378, Width = 250, Height = 22 };
        guide.LinkClicked += (_, _) => WizardHost.Show(this);
        Controls.Add(guide);

        var save = new Button { Text = Ui.T("Сохранить", "Save"), Left = 350, Top = 415, Width = 90 };
        var cancel = new Button { Text = Ui.T("Отмена", "Cancel"), Left = 450, Top = 415, Width = 75, DialogResult = DialogResult.Cancel };
        Controls.Add(save);
        Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;

        save.Click += (_, _) =>
        {
            try
            {
                if (_a1.Value > _b1.Value || _a2.Value > _b2.Value)
                    throw new ArgumentException(Ui.T("Минимум не может быть больше максимума", "Minimum cannot exceed maximum"));
                var bindings = new[] { _toggle.Text, _increase.Text, _decrease.Text };
                if (bindings.Distinct(StringComparer.OrdinalIgnoreCase).Count() != bindings.Length)
                    throw new ArgumentException(Ui.T("Хоткеи должны отличаться друг от друга", "Shortcuts must be different"));
                foreach (string binding in bindings) HotkeyManager.Parse(binding);

                var updated = new AppSettings
                {
                    SelectedInput = input.SelectedIndex + 1,
                    Step = (int)_step.Value,
                    Input1 = new ChannelSettings { A = (int)_a1.Value, B = (int)_b1.Value },
                    Input2 = new ChannelSettings { A = (int)_a2.Value, B = (int)_b2.Value },
                    ToggleHotkey = _toggle.Text,
                    IncreaseHotkey = _increase.Text,
                    DecreaseHotkey = _decrease.Text,
                };
                updated.Validate();
                ResultSettings = updated;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, Ui.T("Ошибка настроек", "Settings error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _ownedIcon.Dispose();
    }

    private static NumericUpDown AddNumber(TableLayoutPanel table, string title, int val, int min = 0, int max = 127)
    {
        var edit = new NumericUpDown { Minimum = min, Maximum = max, Value = Math.Clamp(val, min, max), Width = 90 };
        AddRow(table, title, edit);
        return edit;
    }

    private static HotkeyCaptureBox AddHotkey(TableLayoutPanel table, string title, string val)
    {
        var edit = new HotkeyCaptureBox(val);
        AddRow(table, title, edit);
        return edit;
    }

    private static void AddRow(TableLayoutPanel table, string title, Control editor)
    {
        int row = table.Controls.Count / 2;
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 31));
        table.Controls.Add(new Label { Text = title, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        table.Controls.Add(editor, 1, row);
    }
}

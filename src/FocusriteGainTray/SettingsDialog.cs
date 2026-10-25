namespace FocusriteGainTray;

internal sealed class SettingsDialog : Form
{
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
        Text = "Focusrite Gain Tray — настройки";
        ClientSize = new Size(520, 435);
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
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        Controls.Add(table);

        var input = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190 };
        input.Items.AddRange(new object[] { "Analog 1", "Analog 2" });
        input.SelectedIndex = current.SelectedInput - 1;
        AddRow(table, "Управляемый вход", input);
        _a1 = AddNumber(table, "Analog 1 · минимум (MIDI 0–127)", current.Input1.A);
        _b1 = AddNumber(table, "Analog 1 · максимум (MIDI 0–127)", current.Input1.B);
        _a2 = AddNumber(table, "Analog 2 · минимум (MIDI 0–127)", current.Input2.A);
        _b2 = AddNumber(table, "Analog 2 · максимум (MIDI 0–127)", current.Input2.B);
        _step = AddNumber(table, "Шаг регулировки (1–32)", current.Step, 1, 32);

        _toggle = AddHotkey(table, "Переключить A ↔ B", current.ToggleHotkey);
        _increase = AddHotkey(table, "Прибавить Gain", current.IncreaseHotkey);
        _decrease = AddHotkey(table, "Убавить Gain", current.DecreaseHotkey);

        var note = new Label
        {
            Left = 17, Top = 325, Width = 448, Height = 40,
            Text = "Важно: MIDI 0–127 — не дБ. Приложение не читает текущий Gain из Scarlett.\nДля назначения хоткея кликни в поле и нажми комбинацию клавиш."
        };
        Controls.Add(note);

        var save = new Button { Text = "Сохранить", Left = 305, Top = 390, Width = 90 };
        var cancel = new Button { Text = "Отмена", Left = 400, Top = 390, Width = 75, DialogResult = DialogResult.Cancel };
        Controls.Add(save);
        Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;

        save.Click += (_, _) =>
        {
            try
            {
                if (_a1.Value > _b1.Value || _a2.Value > _b2.Value)
                    throw new ArgumentException("Минимум не может быть больше максимума");
                var bindings = new[] { _toggle.Text, _increase.Text, _decrease.Text };
                if (bindings.Distinct(StringComparer.OrdinalIgnoreCase).Count() != bindings.Length)
                    throw new ArgumentException("Хоткеи должны отличаться друг от друга");
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
                MessageBox.Show(error.Message, "Ошибка настроек", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
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

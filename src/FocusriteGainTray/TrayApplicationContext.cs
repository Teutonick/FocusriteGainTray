namespace FocusriteGainTray;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly WindowsMidiVirtualController _midi = new();
    private readonly HotkeyManager _hotkeys;
    private AppSettings _settings = SettingsStore.Load();
    private readonly GainState _state = new();
    private Icon? _statusIcon;
    private bool _ready;
    private bool _closing;

    public TrayApplicationContext()
    {
        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Focusrite Gain Tray — starting",
            Visible = true
        };
        _tray.MouseClick += (_, args) =>
        {
            if (args.Button == MouseButtons.Left) Toggle();
        };

        _hotkeys = new HotkeyManager(OnHotkey);
        try { _hotkeys.Apply(_settings); }
        catch (Exception e) { Alert("Горячие клавиши: " + e.Message); }

        RebuildMenu();
        // Start after message loop is alive; errors are visible in tray and log.
        Application.Idle += StartOnIdle;
    }

    private void StartOnIdle(object? sender, EventArgs e)
    {
        Application.Idle -= StartOnIdle;
        ConnectMidi();
    }

    private void ConnectMidi()
    {
        _ready = false;
        _state.Reset();
        try
        {
            _midi.Connect();
            _ready = true;
            SetTooltip("MIDI ready — select device in Focusrite Control 2");
            _tray.BalloonTipTitle = "Focusrite Gain Tray";
            _tray.BalloonTipText = "В Focusrite Control 2 → Preferences → Application выбери MIDI Control: Focusrite Gain Tray, канал All channels.";
            _tray.ShowBalloonTip(6000);
        }
        catch (Exception ex)
        {
            _ready = false;
            Log.Write("MIDI connect failed: " + ex);
            SetTooltip("MIDI unavailable — right-click → Reconnect");
            Alert("MIDI недоступен: " + ex.Message);
        }
        RebuildMenu();
    }

    private void SetTooltip(string text)
    {
        // Windows NotifyIcon.Text limit is 63 chars.
        _tray.Text = text.Length > 63 ? text[..63] : text;
    }

    private void OnHotkey(HotkeyAction action)
    {
        switch (action)
        {
            case HotkeyAction.Toggle: Toggle(); break;
            case HotkeyAction.Increase: Adjust(+_settings.Step); break;
            case HotkeyAction.Decrease: Adjust(-_settings.Step); break;
        }
    }

    private void Toggle()
    {
        int input = _settings.SelectedInput;
        ChannelSettings channel = _settings.Channel(input);
        SendGain(input, _state.Next(input, channel));
    }

    private void Adjust(int amount)
    {
        int input = _settings.SelectedInput;
        int baseline = _state.LastSent(input) ?? _settings.Channel(input).B;
        SendGain(input, Math.Clamp(baseline + amount, 0, 127));
    }

    private void SendGain(int input, int midiValue)
    {
        if (!_ready)
        {
            Alert("MIDI ещё не подключён. Проверь Windows MIDI Services и нажми Reconnect MIDI.");
            return;
        }
        try
        {
            _midi.SendGain(input, midiValue);
            _state.Record(input, midiValue);
            SetTooltip($"Analog {input}: MIDI Gain {midiValue}/127 (sent)");
            RebuildMenu();
        }
        catch (Exception e)
        {
            _ready = false;
            _state.Reset();
            RebuildMenu();
            Log.Write("MIDI send failed: " + e);
            Alert(e.Message);
        }
    }

    private void SelectInput(int input)
    {
        _settings.SelectedInput = input;
        PersistSettings();
        RebuildMenu();
    }

    private void PersistSettings()
    {
        try { SettingsStore.Save(_settings); }
        catch (Exception e) { Alert("Не удалось сохранить настройки: " + e.Message); }
    }

    private void Configure()
    {
        using var dialog = new SettingsDialog(_settings);
        if (dialog.ShowDialog() != DialogResult.OK || dialog.ResultSettings is null) return;
        var previous = _settings;
        try
        {
            _hotkeys.Apply(dialog.ResultSettings);
            _settings = dialog.ResultSettings;
            PersistSettings();
            RebuildMenu();
        }
        catch (Exception ex)
        {
            try { _hotkeys.Apply(previous); }
            catch (Exception recoveryEx) { Log.Write("Hotkey rollback failed: " + recoveryEx); }
            Alert("Хоткеи не применены: " + ex.Message);
        }
    }

    private void RebuildMenu()
    {
        UpdateIcon();
        var menu = new ContextMenuStrip();
        var status = new ToolStripMenuItem(_ready ? "● MIDI connected (commands only)" : "○ MIDI disconnected") { Enabled = false };
        menu.Items.Add(status);
        menu.Items.Add(new ToolStripSeparator());

        foreach (int channel in new[] { 1, 2 })
        {
            int captured = channel;
            menu.Items.Add(new ToolStripMenuItem($"Analog {channel}")
            {
                Checked = _settings.SelectedInput == channel,
                CheckOnClick = false
            });
            ((ToolStripMenuItem)menu.Items[^1]).Click += (_, _) => SelectInput(captured);
        }
        menu.Items.Add(new ToolStripSeparator());
        var ch = _settings.Channel(_settings.SelectedInput);
        menu.Items.Add(new ToolStripMenuItem($"Минимум — CC {ch.A}", null,
            (_, _) => SendGain(_settings.SelectedInput, ch.A)));
        menu.Items.Add(new ToolStripMenuItem($"Максимум — CC {ch.B}", null,
            (_, _) => SendGain(_settings.SelectedInput, ch.B)));
        menu.Items.Add(new ToolStripMenuItem($"Переключить A ↔ B ({_settings.ToggleHotkey})", null, (_, _) => Toggle()));
        menu.Items.Add(new ToolStripMenuItem($"+ {_settings.Step} ({_settings.IncreaseHotkey})", null, (_, _) => Adjust(+_settings.Step)));
        menu.Items.Add(new ToolStripMenuItem($"− {_settings.Step} ({_settings.DecreaseHotkey})", null, (_, _) => Adjust(-_settings.Step)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Настройки...", null, (_, _) => Configure()));
        menu.Items.Add(new ToolStripMenuItem("Reconnect MIDI", null, (_, _) => ConnectMidi()));
        menu.Items.Add(new ToolStripMenuItem("Открыть лог", null, (_, _) =>
        {
            var log = Path.Combine(SettingsStore.DirectoryPath, "application.log");
            if (File.Exists(log))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(log) { UseShellExecute = true });
            else Alert("Лог пока пуст.");
        }));
        menu.Items.Add(new ToolStripMenuItem("Выход", null, (_, _) => ExitThread()));

        var oldMenu = _tray.ContextMenuStrip;
        _tray.ContextMenuStrip = menu;
        // ContextMenuStrip should not be disposed while it is actively being shown.
        if (oldMenu is not null)
        {
            if (!oldMenu.Visible) oldMenu.Dispose();
            else oldMenu.Closed += (_, _) => oldMenu.Dispose();
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    private void UpdateIcon()
    {
        int input = _settings.SelectedInput;
        string mode = _ready ? _state.Mode(input, _settings.Channel(input)) : "!";
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(mode == "MIN" ? Color.DarkSlateBlue : mode == "MAX" ? Color.DarkOrange : Color.DimGray);
        using var font = new Font("Segoe UI", mode.Length > 1 ? 8 : 18, FontStyle.Bold);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString(mode, font, Brushes.White, new RectangleF(0, 0, 32, 32), format);
        IntPtr handle = bitmap.GetHicon();
        Icon next;
        try { using var borrowed = Icon.FromHandle(handle); next = (Icon)borrowed.Clone(); }
        finally { DestroyIcon(handle); }
        var previous = _statusIcon;
        _tray.Icon = next;
        _statusIcon = next;
        previous?.Dispose();
        SetTooltip($"Analog {input}: {mode} | last sent: {_state.LastSent(input)?.ToString() ?? "unknown"}");
    }

    private void Alert(string message)
    {
        _tray.BalloonTipTitle = "Focusrite Gain Tray";
        _tray.BalloonTipText = message;
        _tray.ShowBalloonTip(5000);
        Log.Write(message);
    }

    protected override void ExitThreadCore()
    {
        if (_closing) return;
        _closing = true;
        Application.Idle -= StartOnIdle;
        _hotkeys.Dispose();
        _midi.Dispose();
        _tray.Visible = false;
        _tray.ContextMenuStrip?.Dispose();
        _tray.Dispose();
        _statusIcon?.Dispose();
        base.ExitThreadCore();
    }
}

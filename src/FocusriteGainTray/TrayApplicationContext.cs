namespace FocusriteGainTray;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly BoundedWorker<WindowsMidiVirtualController> _midi = new(() => new());
    private readonly HotkeyManager _hotkeys;
    private readonly SetupRequestWindow _setupRequests;
    private readonly bool _showSetupOnStart;
    private AppSettings _settings = SettingsStore.Load();
    private readonly GainState _state = new();
    private Icon? _statusIcon;
    private bool _ready, _busy, _closing;

    public TrayApplicationContext(bool showSetup = false)
    {
        _showSetupOnStart = showSetup;
        _statusIcon = AppIcons.Load("unknown");
        _tray = new NotifyIcon { Icon = _statusIcon, Text = "FocGrain", Visible = true };
        _tray.MouseClick += async (_, args) => { if (args.Button == MouseButtons.Left) await ToggleAsync(); };
        _hotkeys = new HotkeyManager(OnHotkey);
        try { _hotkeys.Apply(_settings); }
        catch (Exception ex) { Alert(Ui.T("Горячие клавиши: ", "Keyboard shortcuts: ") + ex.Message); }
        RebuildMenu();
        _setupRequests = new SetupRequestWindow(() => WizardHost.Show());
        Application.Idle += StartOnIdle;
    }

    private async void StartOnIdle(object? sender, EventArgs e)
    {
        Application.Idle -= StartOnIdle;
        await ConnectAsync();
        if (_showSetupOnStart && !_closing) WizardHost.Show();
    }

    private Task ConnectAsync()
    {
        if (_busy || _closing) return Task.CompletedTask;
        _ready = false;
        _state.Reset();
        return RunMidiAsync(controller => controller.Connect(), TimeSpan.FromSeconds(10), () =>
        {
            _ready = true;
            _tray.BalloonTipTitle = "FocGrain";
            _tray.BalloonTipText = Ui.T("В FC2 выберите FocGrain, канал 1 / All channels и снимите Enable pickup. Пошаговый мастер доступен в меню трея.", "In FC2 select FocGrain, channel 1 / All channels, and turn OFF Enable pickup. Open the setup wizard from the tray menu.");
            _tray.ShowBalloonTip(6000);
        });
    }

    private async Task RunMidiAsync(Action<WindowsMidiVirtualController> action, TimeSpan timeout, Action onSuccess)
    {
        if (_busy || _closing) return;
        _busy = true;
        RebuildMenu();
        try
        {
            await _midi.ExecuteAsync(action, timeout);
            if (!_closing) onSuccess();
        }
        catch (Exception ex)
        {
            Log.Write("MIDI operation failed: " + ex);
            if (!_closing) { _ready = false; _state.Reset(); Alert(ex.Message); }
        }
        finally
        {
            _busy = false;
            if (!_closing) RebuildMenu();
        }
    }

    private void OnHotkey(HotkeyAction action)
    {
        _ = action switch
        {
            HotkeyAction.Toggle => ToggleAsync(),
            HotkeyAction.Increase => AdjustAsync(_settings.Step),
            _ => AdjustAsync(-_settings.Step)
        };
    }

    private Task ToggleAsync()
    {
        int input = _settings.SelectedInput;
        return SendGainAsync(input, _state.Next(input, _settings.Channel(input)));
    }

    private Task AdjustAsync(int amount)
    {
        int input = _settings.SelectedInput;
        int baseline = _state.LastSent(input) ?? _settings.Channel(input).B;
        return SendGainAsync(input, Math.Clamp(baseline + amount, 0, 127));
    }

    private Task SendGainAsync(int input, int value)
    {
        // Never queue clicks/hotkeys while native MIDI is busy: no delayed gain changes.
        if (_busy || _closing) return Task.CompletedTask;
        if (!_ready)
        {
            Alert(_midi.Faulted
                ? Ui.T("MIDI не ответил. Перезапустите FocGrain.", "MIDI timed out. Restart FocGrain.")
                : Ui.T("MIDI-порт не открыт. Выберите «Повторить открытие MIDI-порта» в трее.", "The MIDI port is not open. Select Retry opening MIDI port in the tray."));
            return Task.CompletedTask;
        }
        return RunMidiAsync(controller => controller.SendGain(input, value), TimeSpan.FromSeconds(3), () => _state.Record(input, value));
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
        catch (Exception ex) { Alert(Ui.T("Не удалось сохранить настройки: ", "Could not save settings: ") + ex.Message); }
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
            catch (Exception recovery) { Log.Write("Hotkey rollback failed: " + recovery); }
            Alert(Ui.T("Хоткеи не применены: ", "Shortcuts were not applied: ") + ex.Message);
        }
    }

    private void RebuildMenu()
    {
        UpdateIcon();
        var menu = new ContextMenuStrip();
        string status = _busy ? Ui.T("MIDI: выполняется операция…", "MIDI: operation in progress…") : _ready ? Ui.T("Порт FocGrain открыт (приём FC2 не проверен)", "FocGrain port open (FC2 receipt not verified)") : Ui.T("MIDI-порт не открыт", "MIDI port is not open");
        menu.Items.Add(new ToolStripMenuItem(status) { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        foreach (int channel in new[] { 1, 2 })
        {
            int captured = channel;
            menu.Items.Add(new ToolStripMenuItem($"Analog {channel}", null, (_, _) => SelectInput(captured))
                { Checked = _settings.SelectedInput == channel, Enabled = !_busy });
        }
        menu.Items.Add(new ToolStripSeparator());
        int input = _settings.SelectedInput;
        var ch = _settings.Channel(input);
        void Command(string text, Func<Task> command) => menu.Items.Add(new ToolStripMenuItem(text, null, async (_, _) => await command()) { Enabled = _ready && !_busy });
        Command(Ui.T($"Минимум — MIDI-значение {ch.A}", $"Minimum — MIDI value {ch.A}"), () => SendGainAsync(input, ch.A));
        Command(Ui.T($"Максимум — MIDI-значение {ch.B}", $"Maximum — MIDI value {ch.B}"), () => SendGainAsync(input, ch.B));
        Command(Ui.T($"Переключить MIN ↔ MAX ({_settings.ToggleHotkey})", $"Toggle MIN ↔ MAX ({_settings.ToggleHotkey})"), ToggleAsync);
        Command($"+ {_settings.Step} ({_settings.IncreaseHotkey})", () => AdjustAsync(_settings.Step));
        Command($"− {_settings.Step} ({_settings.DecreaseHotkey})", () => AdjustAsync(-_settings.Step));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem(Ui.T("Настройки…", "Settings…"), null, (_, _) => Configure()) { Enabled = !_busy });
        menu.Items.Add(new ToolStripMenuItem(Ui.T("Мастер настройки Focusrite Control 2…", "Focusrite Control 2 setup wizard…"), null, (_, _) => WizardHost.Show()));
        // Do not tear down/recreate an already-live endpoint while FC2 is selecting it.
        if (!_ready && !_midi.Faulted)
            menu.Items.Add(new ToolStripMenuItem(Ui.T("Повторить открытие MIDI-порта", "Retry opening MIDI port"), null, async (_, _) => await ConnectAsync()) { Enabled = !_busy });
        if (_midi.Faulted)
            menu.Items.Add(new ToolStripMenuItem(Ui.T("MIDI не ответил — перезапустите FocGrain", "MIDI timed out — restart FocGrain")) { Enabled = false });
        menu.Items.Add(new ToolStripMenuItem(Ui.T("Проверить обновления…", "Check for updates…"), null, (_, _) => { using var dialog = new UpdateDialog(); dialog.ShowDialog(); }));
        menu.Items.Add(new ToolStripMenuItem(Ui.T("Открыть лог", "Open log"), null, (_, _) =>
        {
            try
            {
                var path = Path.Combine(SettingsStore.DirectoryPath, "application.log");
                if (File.Exists(path)) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
                else Alert(Ui.T("Лог пока пуст.", "The log is empty."));
            }
            catch (Exception ex) { Alert(ex.Message); }
        }));
        menu.Items.Add(new ToolStripMenuItem($"FocGrain {ProductInfo.VersionText}") { Enabled = false });
        menu.Items.Add(new ToolStripMenuItem(Ui.T("Выход", "Quit"), null, (_, _) => ExitThread()));
        var old = _tray.ContextMenuStrip;
        _tray.ContextMenuStrip = menu;
        if (old is not null)
        {
            if (!old.Visible) old.Dispose();
            else old.Closed += (_, _) => old.Dispose();
        }
    }

    private void UpdateIcon()
    {
        int input = _settings.SelectedInput;
        string mode = _ready ? _state.Mode(input, _settings.Channel(input)) : "!";
        string icon = _busy ? "unknown" : mode switch { "MIN" => "min", "MAX" => "max", "MID" => "mid", "?" => "unknown", _ => "error" };
        var next = AppIcons.Load(icon);
        var previous = _statusIcon;
        _tray.Icon = next;
        _statusIcon = next;
        previous?.Dispose();
        string unknown = Ui.T("неизвестно", "unknown");
        string text = _busy ? Ui.T("FocGrain — MIDI: ожидание…", "FocGrain — waiting for MIDI…") : $"Analog {input}: {mode} | {Ui.T("отправлено", "last sent")}: {_state.LastSent(input)?.ToString() ?? unknown}";
        _tray.Text = text.Length > 63 ? text[..63] : text;
    }

    private void Alert(string message)
    {
        _tray.BalloonTipTitle = "FocGrain";
        _tray.BalloonTipText = message;
        _tray.ShowBalloonTip(5000);
        Log.Write(message);
    }

    protected override async void ExitThreadCore()
    {
        if (_closing) return;
        _closing = true;
        Application.Idle -= StartOnIdle;
        _setupRequests.Dispose();
        WizardHost.Close();
        _hotkeys.Dispose();
        _tray.Visible = false;
        try { await _midi.StopAsync(TimeSpan.FromSeconds(2)); }
        catch (Exception ex) { Log.Write("MIDI shutdown: " + ex); }
        _tray.ContextMenuStrip?.Dispose();
        _tray.Dispose();
        _statusIcon?.Dispose();
        base.ExitThreadCore();
    }
}

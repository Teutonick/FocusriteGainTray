namespace FocusriteGainTray;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly BoundedWorker<WindowsMidiVirtualController> _midi = new(() => new());
    private readonly HotkeyManager _hotkeys;
    private readonly SetupRequestWindow _setupRequests;
    private readonly bool _showSetupOnStart;
    private readonly bool _atLogon;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _starting;
    private bool _recovering;
    private readonly Fc2Startup _fc2Startup = new(FocusriteControl2.Locate, FocusriteControl2.IsRunning, FocusriteControl2.Launch);
    private AppSettings _settings = SettingsStore.Load();
    private readonly GainState _state = new();
    private Icon? _statusIcon;
    private bool _ready, _busy, _closing;

    public TrayApplicationContext(bool showSetup = false, bool atLogon = false)
    {
        _showSetupOnStart = showSetup;
        _atLogon = atLogon;
        _statusIcon = AppIcons.Load("unknown");
        _tray = new NotifyIcon { Icon = _statusIcon, Text = "FocGrain", Visible = true };
        _tray.MouseClick += async (_, args) => { if (args.Button == MouseButtons.Left) await ToggleAsync(); };
        _hotkeys = new HotkeyManager(OnHotkey);
        try { _hotkeys.Apply(_settings); }
        catch (Exception ex) { Alert(Ui.T("Горячие клавиши: ", "Keyboard shortcuts: ") + ex.Message); }
        RebuildMenu();
        _setupRequests = new SetupRequestWindow(() => WizardHost.Show(), () => ExitThread(), ActivateExisting);
        Application.Idle += StartOnIdle;
    }

    private async void StartOnIdle(object? sender, EventArgs e)
    {
        Application.Idle -= StartOnIdle;
        _starting = true;
        RebuildMenu();
        try
        {
            await MidiStartup.RunAsync(_atLogon,
                async () => { await ConnectAsync(); return _ready; },
                () => _midi.Faulted, () => _closing,
                duration => Task.Delay(duration, _lifetime.Token));
        }
        catch (OperationCanceledException) when (_closing) { }
        finally { _starting = false; if (!_closing) RebuildMenu(); }
        if (_showSetupOnStart && !_closing) WizardHost.Show();
    }

    private Task ConnectAsync()
    {
        if (_busy || _closing) return Task.CompletedTask;
        _ready = false;
        _state.Reset();
        return RunMidiAsync(controller => controller.Connect(), TimeSpan.FromSeconds(30), () =>
        {
            _ready = true;
            MidiRecoveryState.Clear();
            TryStartFc2();
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
            if (!_closing)
            {
                _ready = false;
                _state.Reset();
                Alert(ex is TimeoutException
                    ? Ui.T("Windows MIDI Services не ответил. Сохраните работу в MIDI-программах и выберите «Восстановить MIDI без перезагрузки» в меню трея. Служба перезапускается только с вашим подтверждением.",
                        "Windows MIDI Services did not respond. Save work in MIDI applications and choose Repair MIDI without restarting Windows in the tray menu. The service restarts only with your confirmation.")
                    : ex.Message);
            }
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

    private void TryStartFc2()
    {
        try
        {
            var result = _fc2Startup.Run(_settings.StartFocusriteControl2, _ready && !_closing);
            Log.Write("FC2 startup after MIDI readiness: " + result);
            if (result == Fc2StartupResult.NotInstalled)
                Alert(Ui.T("Порт готов, но Focusrite Control 2 не найден. Откройте FC2 вручную или установите его через setup.", "The MIDI port is ready, but Focusrite Control 2 was not found. Open FC2 manually or install it through setup."));
        }
        catch (Exception ex)
        {
            Log.Write("FC2 launch failed (MIDI port remains open): " + ex);
            Alert(Ui.T("Не удалось запустить FC2. Откройте Focusrite Control 2 вручную. ", "Could not start FC2. Open Focusrite Control 2 manually. ") + ex.Message);
        }
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
        if (_busy || _closing || _starting) return Task.CompletedTask;
        if (!_ready)
        {
            Alert(_midi.Faulted
                ? Ui.T("Windows MIDI Services не ответил. Выберите «Восстановить MIDI без перезагрузки» в меню трея.", "Windows MIDI Services did not respond. Choose Repair MIDI without restarting Windows in the tray menu.")
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
        string status = _busy ? Ui.T("MIDI: выполняется операция…", "MIDI: operation in progress…") : _ready ? Ui.T("Готов к управлению gain", "Ready to control gain") : _starting ? Ui.T("Ожидание готовности MIDI…", "Waiting for MIDI readiness…") : Ui.T("MIDI-порт не открыт", "MIDI port is not open");
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
        if (!_ready && !_midi.Faulted && !_starting)
            menu.Items.Add(new ToolStripMenuItem(Ui.T("Повторить открытие MIDI-порта", "Retry opening MIDI port"), null, async (_, _) => await ConnectAsync()) { Enabled = !_busy });
        if (!_ready && !_starting)
            menu.Items.Add(new ToolStripMenuItem(Ui.T("Восстановить MIDI без перезагрузки…", "Repair MIDI without restarting Windows…"), null, (_, _) => ShowMidiRecovery()) { Enabled = !_busy && !_recovering });
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

    private async void ShowMidiRecovery()
    {
        if (_recovering || _closing || _busy) return;
        if (MessageBox.Show(Ui.T(
            "Перезапустить только Windows MIDI Services и затем FocGrain?\n\nЭто временно разорвёт MIDI-соединения других программ. Сохраните работу перед продолжением. Потребуется подтверждение UAC. Если служба зависла, завершится только проверенный выделенный процесс System32/MidiSrv.exe. FC2, аудиодрайверы и настройки gain не изменяются.",
            "Restart only Windows MIDI Services, then FocGrain?\n\nThis temporarily disconnects MIDI connections in other applications. Save your work first. UAC approval is required. If stopping hangs, only the verified dedicated System32/MidiSrv.exe process is terminated. FC2, audio drivers and gain settings are not changed."),
            "FocGrain", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        _recovering = true;
        _ready = false;
        _state.Reset();
        _busy = true;
        RebuildMenu();
        try
        {
            string exe = Environment.ProcessPath!;
            using var helper = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe)
            {
                Arguments = $"--repair-midi-service {Environment.ProcessId}", UseShellExecute = true, Verb = "runas", WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            }) ?? throw new IOException("Recovery helper did not start.");
            // The elevated helper waits for this MIDI owner to exit. The next
            // unelevated process waits for successful service recovery, so no
            // old native object can race the reset and no elevated tray is left.
            string readyId = Guid.NewGuid().ToString("N");
            using var ready = new EventWaitHandle(false, EventResetMode.ManualReset, MidiRecovery.ReadyEventName(readyId));
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe)
            {
                Arguments = $"--after-recovery {helper.Id} {readyId}", UseShellExecute = true,
            })?.Dispose();
            if (!await Task.Run(() => ready.WaitOne(10000))) throw new IOException("Recovery waiter did not become ready; the service helper will refuse while this app is running.");
            ExitThread();
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223) { Log.Write("MIDI recovery cancelled at UAC."); }
        catch (Exception ex) { Alert(ex.Message); }
        finally { _recovering = false; _busy = false; if (!_closing) RebuildMenu(); }
    }

    private void ActivateExisting()
    {
        foreach (Form form in Application.OpenForms)
        {
            if (!form.Visible || form.IsDisposed) continue;
            if (form.WindowState == FormWindowState.Minimized) form.WindowState = FormWindowState.Normal;
            form.Activate();
            return;
        }
        _tray.BalloonTipTitle = "FocGrain";
        _tray.BalloonTipText = Ui.T("FocGrain уже работает в системном трее.", "FocGrain is already running in the system tray.");
        _tray.ShowBalloonTip(3000);
    }

    protected override async void ExitThreadCore()
    {
        if (_closing) return;
        _closing = true;
        _lifetime.Cancel();
        Application.Idle -= StartOnIdle;
        _setupRequests.Dispose();
        WizardHost.Close();
        _hotkeys.Dispose();
        _tray.Visible = false;
        try { await _midi.StopAsync(TimeSpan.FromSeconds(2)); }
        catch (Exception ex) { Log.Write("MIDI shutdown: " + ex); }
        if (_midi.Faulted)
        {
            Environment.ExitCode = MidiRecoveryPolicy.DirtyShutdown;
            MidiRecoveryState.MarkDirty();
            Log.Write("MIDI shutdown incomplete; exit code 31 requires explicit service recovery before upgrade launch.");
        }
        _tray.ContextMenuStrip?.Dispose();
        _tray.Dispose();
        _statusIcon?.Dispose();
        _lifetime.Dispose();
        base.ExitThreadCore();
    }
}

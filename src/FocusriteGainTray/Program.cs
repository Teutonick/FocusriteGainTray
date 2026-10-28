using System.Threading;

namespace FocusriteGainTray;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log.Write($"FATAL unhandled exception; terminating={e.IsTerminating}: {e.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, e) => { Log.Write("Unobserved task exception: " + e.Exception); e.SetObserved(); };
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
        {
            Log.Write("FATAL UI exception; requesting controlled shutdown: " + e.Exception);
            Environment.ExitCode = 1;
            Application.Exit();
        };
        try { return Run(args); }
        catch (Exception ex) { Log.Write("FATAL main exception: " + ex); return 1; }
    }

    private static int Run(string[] args)
    {
        Ui.Initialize();
        if (args.Length == 2 && args[0] == "--check-midi-recovery-access")
        {
            try
            {
                using var service = new WindowsMidiServiceRecovery();
                service.CheckRecoveryAccess();
                File.WriteAllText(args[1], "PASS verified MIDI recovery process access; no service changes");
                return 0;
            }
            catch (Exception ex) { File.WriteAllText(args[1], ex.ToString()); return 1; }
        }
        if (args.Length == 4 && args[0] == "--repair-midi-service" && args[1] == "0" && args[2] == "--report")
            return MidiRecovery.RunElevated(0, Path.GetFullPath(args[3]));
        if (args.Length == 2 && args[0] == "--repair-midi-service" && int.TryParse(args[1], out int recoveryOwner) && recoveryOwner >= 0)
            return MidiRecovery.RunElevated(recoveryOwner);
        if (args.Length == 2 && args[0] == "--shutdown-installation")
            return InstallationShutdown.Run(args[1]);
        if (args.Length == 3 && args[0] == "--shutdown-test-instance" && int.TryParse(args[2], out int testPid) && testPid > 0)
            return InstallationShutdown.Run(args[1], testPid);
        if (args.Length == 2 && args[0] is "--lifecycle-test-host" or "--lifecycle-test-unresponsive")
        {
            // Isolated process-lifecycle QA; no MIDI, tray, startup entry or settings.
            ApplicationConfiguration.Initialize();
            using var context = new ApplicationContext();
            using var window = new SetupRequestWindow(() => { }, () =>
            {
                File.WriteAllText(args[1] + ".closed", "graceful");
                context.ExitThread();
            });
            if (args[0] == "--lifecycle-test-unresponsive")
            {
                window.Dispose();
                File.WriteAllText(args[1], "ready");
                // Keep this no-MIDI test process alive until the shutdown
                // helper exercises its forced-termination path.
                Thread.Sleep(Timeout.Infinite);
                return 0;
            }
            File.WriteAllText(args[1], "ready");
            Application.Run(context);
            return 0;
        }
        if (args.Length == 3 && args[0] == "--guide-preview" && args[1] is "ru" or "en")
        {
            Ui.Initialize(args[1]);
            ApplicationConfiguration.Initialize();
            Directory.CreateDirectory(args[2]);
            using var guide = new SetupWizard { StartPosition = FormStartPosition.Manual, Location = new Point(-10000, -10000), ShowInTaskbar = false };
            guide.Show();
            for (int index = 0; index < SetupGuide.Steps().Count; index++)
            {
                guide.ShowStep(index); guide.PerformLayout(); Application.DoEvents();
                using var bitmap = new Bitmap(guide.Width, guide.Height);
                guide.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                bitmap.Save(Path.Combine(args[2], $"step-{index + 1}.png"), System.Drawing.Imaging.ImageFormat.Png);
            }
            guide.Close();
            return 0;
        }
        if ((args.Length is 2 or 3) && args[0] == "--ui-preview" && args[1] is "ru" or "en")
        {
            Ui.Initialize(args[1]);
            ApplicationConfiguration.Initialize();
            // Explicit developer preview: no tray, MIDI connection, settings load or persistence.
            using var preview = new SettingsDialog(new AppSettings());
            if (args.Length == 3)
            {
                preview.StartPosition = FormStartPosition.Manual;
                preview.Location = new Point(-10000, -10000);
                preview.Show();
                Application.DoEvents();
                using var bitmap = new Bitmap(preview.Width, preview.Height);
                preview.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                bitmap.Save(args[2], System.Drawing.Imaging.ImageFormat.Png);
                preview.Close();
            }
            else Application.Run(preview);
            return 0;
        }
        // Setup invokes this before installation; never initialize tray, hotkeys or MIDI output.
        if ((args.Length == 2 || (args.Length == 4 && args[2] == "--language" && args[3] is "en" or "ru")) && args[0] == "--check-prerequisites")
        {
            if (args.Length == 4) Ui.Initialize(args[3]);
            try { return Task.Run(() => PrerequisiteProbe.Run(args[1])).WaitAsync(TimeSpan.FromSeconds(12)).GetAwaiter().GetResult(); }
            catch { return PrerequisitePolicy.ProbeFailed; }
        }
        bool showSetup = args.Length == 1 && args[0] == "--setup";
        bool atLogon = args.Length == 1 && args[0] == "--autostart";
        bool afterRecovery = args.Length is 2 or 3 && args[0] == "--after-recovery" && int.TryParse(args[1], out int previousPid) && previousPid > 0
            && (args.Length == 2 || Guid.TryParseExact(args[2], "N", out _));
        bool smoke = args.Length == 2 && args[0] == "--midi-smoke";
        bool instanceTest = args.Length == 2 && args[0] == "--instance-test-host";
        if (args.Length != 0 && !showSetup && !atLogon && !smoke && !instanceTest && !afterRecovery) return 64;
        if (afterRecovery)
        {
            if (!MidiRecovery.WaitForRecoveryHelper(int.Parse(args[1]), args.Length == 3 ? args[2] : null)) return 75;
            Log.Write("Recovery helper exit code 0 confirmed in original user process; clearing this user's dirty marker.");
            MidiRecoveryState.Clear();
        }
        if (!instanceTest && MidiRecovery.Active()) return 75;
        SingleInstance? instance;
        bool acquired;
        if (instanceTest)
        {
            // Two-process QA uses the same lock code with test-owned names and no MIDI.
            string directory = Path.GetDirectoryName(Path.GetFullPath(args[1]))!;
            string id = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(directory)))[..24];
            acquired = SingleInstance.TryAcquire(@"Global\FocGrain.Test." + id, @"Local\FocGrain.Test." + id, out instance);
        }
        else acquired = SingleInstance.TryAcquire(out instance);
        if (!acquired)
        {
            if (instanceTest || smoke) return 73;
            if (SetupRequestWindow.SignalExisting(showSetup)) return 0;
            if (!atLogon) MessageBox.Show(Ui.T("FocGrain уже запущен или Windows запретила получение блокировки. Второй экземпляр не запускается.", "FocGrain is already running or Windows denied the instance lock. A second instance will not start."), "FocGrain", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        }
        using var owner = instance!;
        if (!instanceTest) Log.Write($"Process started; version={ProductInfo.VersionText}; runtime={Environment.Version}; os={Environment.OSVersion}; uptimeMs={Environment.TickCount64}; session={System.Diagnostics.Process.GetCurrentProcess().SessionId}; mode={(atLogon ? "autostart" : showSetup ? "setup" : smoke ? "smoke" : "tray")}");
        if (smoke)
        {
            try { return MidiSmoke.RunAsync(args[1]).GetAwaiter().GetResult(); }
            catch { return 29; }
        }
        if (instanceTest)
        {
            ApplicationConfiguration.Initialize();
            using var context = new ApplicationContext();
            using var window = new SetupRequestWindow(() => { }, () => context.ExitThread());
            File.WriteAllText(args[1], "ready");
            Application.Run(context);
            return 0;
        }

        AppDomain.CurrentDomain.ProcessExit += (_, _) => Log.Write($"ProcessExit; exitCode={Environment.ExitCode}");
        string runMarker = Path.Combine(SettingsStore.DirectoryPath, "active-run.txt");
        try
        {
            Directory.CreateDirectory(SettingsStore.DirectoryPath);
            if (File.Exists(runMarker)) Log.Write("Previous tray run has no completed shutdown record (crash, forced termination or power loss possible): " + File.ReadAllText(runMarker));
            File.WriteAllText(runMarker, $"PID={Environment.ProcessId}; started={DateTimeOffset.Now:O}; version={ProductInfo.VersionText}");
        }
        catch (Exception ex) { Log.Write("Run marker write failed: " + ex); }
        ApplicationConfiguration.Initialize();
        // Existing pre-1.0.1 Run entries have no --autostart switch. Cover their
        // early-boot launch too, without changing the user's startup preference.
        Application.Run(new TrayApplicationContext(showSetup, atLogon || Environment.TickCount64 < 120_000));
        if (Environment.ExitCode is 0 or MidiRecoveryPolicy.DirtyShutdown)
        {
            try { File.Delete(runMarker); }
            catch (Exception ex) { Log.Write("Run marker cleanup failed: " + ex); }
        }
        Log.Write($"Application message loop ended; exitCode={Environment.ExitCode}");
        return Environment.ExitCode;
    }
}

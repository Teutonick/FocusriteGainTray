using System.Threading;

namespace FocusriteGainTray;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Ui.Initialize();
        if (args.Length == 2 && args[0] == "--shutdown-installation")
            return InstallationShutdown.Run(args[1]);
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
            if (args[0] == "--lifecycle-test-unresponsive") window.Dispose();
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
        if (args.Length == 2 && args[0] == "--midi-smoke")
        {
            try { return MidiSmoke.RunAsync(args[1]).GetAwaiter().GetResult(); }
            catch { return 29; }
        }
        // Setup invokes this before installation; never initialize tray, hotkeys or MIDI output.
        if ((args.Length == 2 || (args.Length == 4 && args[2] == "--language" && args[3] is "en" or "ru")) && args[0] == "--check-prerequisites")
        {
            if (args.Length == 4) Ui.Initialize(args[3]);
            try { return Task.Run(() => PrerequisiteProbe.Run(args[1])).WaitAsync(TimeSpan.FromSeconds(12)).GetAwaiter().GetResult(); }
            catch { return PrerequisitePolicy.ProbeFailed; }
        }
        bool showSetup = args.Length == 1 && args[0] == "--setup";
        if (args.Length != 0 && !showSetup) return 64;
        using var mutex = new Mutex(initiallyOwned: true, name: @"Local\FocusriteGainTray", out bool firstInstance);
        if (!firstInstance)
        {
            if (showSetup && SetupRequestWindow.SignalExisting()) return 0;
            MessageBox.Show(Ui.T("FocGrain уже запущен в трее.", "FocGrain is already running in the system tray."), "FocGrain",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext(showSetup));
        return 0;
    }
}

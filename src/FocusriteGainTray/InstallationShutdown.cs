using System.Diagnostics;

namespace FocusriteGainTray;

internal static class InstallationShutdown
{
    // Installer supplies its exact installation path. Never stop FC2, MIDI Services,
    // another installation, or a process merely sharing a name.
    public static int Run(string executable)
    {
        string target = Path.GetFullPath(executable);
        foreach (var process in Process.GetProcessesByName("FocGrain"))
        using (process)
        {
            if (process.Id == Environment.ProcessId) continue;
            try
            {
                if (!string.Equals(process.MainModule?.FileName, target, StringComparison.OrdinalIgnoreCase)) continue;
                SetupRequestWindow.RequestClose(process.Id);
                if (!process.WaitForExit(5000))
                {
                    // Explicit installer confirmation authorizes this fallback for old/hung versions.
                    process.Kill();
                    if (!process.WaitForExit(5000)) return 1;
                }
            }
            catch (InvalidOperationException) { /* already exited */ }
            catch { return 1; }
        }
        return 0;
    }
}

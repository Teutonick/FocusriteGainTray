using System.Diagnostics;

namespace FocusriteGainTray;

internal static class InstallationShutdown
{
    // Installer supplies its exact installation path. Never stop FC2, MIDI Services,
    // another installation, or a process merely sharing a name.
    public static int Run(string executable, int? testProcessId = null)
    {
        string target = Path.GetFullPath(executable);
        bool dirty = testProcessId is null && MidiRecoveryState.Required;
        foreach (var process in Process.GetProcessesByName("FocGrain"))
        using (process)
        {
            if (process.Id == Environment.ProcessId) continue;
            if (testProcessId is not null && process.Id != testProcessId) continue;
            bool matched = false;
            try
            {
                if (!string.Equals(process.MainModule?.FileName, target, StringComparison.OrdinalIgnoreCase)) continue;
                matched = true;
                var info = FileVersionInfo.GetVersionInfo(target);
                var version = new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart);
                // Keep a handle before requesting exit. A process discovered by
                // name may otherwise vanish before ExitCode can be queried.
                _ = process.SafeHandle;
                dirty |= version < new Version(1, 0, 4);
                bool forced = false;
                SetupRequestWindow.RequestClose(process.Id);
                if (!process.WaitForExit(5000))
                {
                    // Record before termination: querying ExitCode can race a
                    // disappearing process and must never erase a dirty result.
                    dirty = true;
                    // Explicit installer confirmation authorizes this fallback for old/hung versions.
                    process.Kill();
                    forced = true;
                    if (!process.WaitForExit(5000)) return 1;
                }
                if (!forced) dirty |= MidiRecoveryPolicy.NeedsRecovery(version, process.ExitCode, false);
            }
            catch (InvalidOperationException) { if (matched) dirty = true; }
            catch { return 1; }
        }
        if (dirty && testProcessId is null) MidiRecoveryState.MarkDirty();
        return dirty ? MidiRecoveryPolicy.DirtyShutdown : 0;
    }
}

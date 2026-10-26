using Microsoft.Win32;
using System.Diagnostics;

namespace FocusriteGainTray;

internal static class FocusriteControl2
{
    public static string? Locate()
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using var uninstall = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstall is null) continue;
            foreach (string name in uninstall.GetSubKeyNames())
            {
                try
                {
                    using var item = uninstall.OpenSubKey(name);
                    if (!(item?.GetValue("DisplayName") as string ?? "").StartsWith("Focusrite Control 2 ", StringComparison.OrdinalIgnoreCase)) continue;
                    string location = item?.GetValue("InstallLocation") as string ?? "";
                    if (!Path.IsPathFullyQualified(location)) continue;
                    string exe = Path.Combine(location, "Focusrite Control 2.exe");
                    if (File.Exists(exe)) return exe;
                }
                catch (UnauthorizedAccessException) { }
                catch (System.Security.SecurityException) { }
            }
        }
        return null;
    }

    public static bool IsRunning(string executable)
    {
        foreach (var process in Process.GetProcessesByName("Focusrite Control 2"))
        using (process)
        {
            try
            {
                if (string.Equals(process.MainModule?.FileName, executable, StringComparison.OrdinalIgnoreCase)) return true;
            }
            catch (InvalidOperationException) { /* exited */ }
            catch (System.ComponentModel.Win32Exception)
            {
                // Conservatively skip automatic launch if another FC2's path is protected.
                Log.Write("FC2 process exists; path access denied, automatic launch skipped.");
                return true;
            }
        }
        return false;
    }

    public static void Launch(string executable)
    {
        using var process = Process.Start(new ProcessStartInfo(executable)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(executable)!,
        });
        if (process is null) throw new IOException("Focusrite Control 2 did not start.");
    }
}

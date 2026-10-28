using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace FocusriteGainTray;

internal static class MidiRecovery
{
    private const string LockName = @"Global\FocGrain.MidiRecovery.A762C2E7-FBA2-4CB0-B856-90359B52446A";
    public static bool Active()
    {
        try { using var mutex = Mutex.OpenExisting(LockName); return true; }
        catch (WaitHandleCannotBeOpenedException) { return false; }
        catch (UnauthorizedAccessException) { return true; }
    }
    public static int RunElevated(int ownerPid, string? reportPath = null)
    {
        if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) return 5;
        void Report(string message)
        {
            Log.Write(message);
            if (reportPath is not null)
            {
                try { File.AppendAllText(reportPath, $"[{DateTimeOffset.Now:O}] {message}{Environment.NewLine}", new System.Text.UTF8Encoding(true)); }
                catch (Exception ex) { Log.Write("Recovery report write failed: " + ex); }
            }
        }
        try
        {
            Report("Recovery helper started; executable=" + Environment.ProcessPath);
            using var mutex = new Mutex(true, LockName, out bool first);
            if (!first) return 75;
            if (ownerPid > 0)
            {
                try
                {
                    using var owner = Process.GetProcessById(ownerPid);
                    if (!string.Equals(owner.MainModule?.FileName, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase))
                        throw new IOException("Recovery owner executable did not match.");
                    if (!owner.WaitForExit(20000)) throw new IOException("FocGrain did not quit; MIDI service was not changed.");
                }
                catch (ArgumentException) { /* owner already exited */ }
                catch (InvalidOperationException) { /* owner exited during inspection */ }
            }
            if (!SingleInstance.TryAcquire(out var ownership)) throw new IOException("Another FocGrain owns MIDI; recovery refused.");
            using var deviceOwner = ownership!;
            Report("Explicit elevated MIDI service recovery started.");
            using var service = new WindowsMidiServiceRecovery();
            Report("Initial MIDI service state: " + service.Query());
            MidiRecoveryPolicy.Restart(service, () => Thread.Sleep(500));
            MidiRecoveryState.RecordCompleted();
            Report("MIDI service recovery completed: Running. No gain sent.");
            return 0;
        }
        catch (Exception ex)
        {
            Report($"MIDI service recovery failed; HResult=0x{ex.HResult:X8}: {ex}");
            if (ownerPid > 0) MessageBox.Show(ex.Message, Ui.T("Не удалось восстановить MIDI", "MIDI recovery failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
    public static string ReadyEventName(string id) => @"Local\FocGrain.RecoveryReady." + id;
    public static bool WaitForRecoveryHelper(int pid, string? readyEvent = null)
    {
        try
        {
            // Pin a limited-access handle before the old owner exits. This also
            // works across UAC without requesting VM read or termination rights.
            using var process = OpenProcess(0x00100000 | 0x1000, false, (uint)pid);
            if (process.IsInvalid) return false;
            if (readyEvent is not null)
            {
                using var signal = EventWaitHandle.OpenExisting(ReadyEventName(readyEvent));
                signal.Set();
            }
            return WaitForSingleObject(process, 120000) == 0 && GetExitCodeProcess(process, out uint code) && code == 0;
        }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch { return false; }
    }
    [DllImport("kernel32.dll", SetLastError = true)] private static extern SafeProcessHandle OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll")] private static extern uint WaitForSingleObject(SafeProcessHandle process, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetExitCodeProcess(SafeProcessHandle process, out uint code);
}

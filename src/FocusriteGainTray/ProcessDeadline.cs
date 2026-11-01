using System.Diagnostics;

namespace FocusriteGainTray;

// Last resort for uninterruptible native calls or a blocked UI shutdown.
// Terminates only this process; never FC2, a vendor installer or MIDI Services.
internal static class ProcessDeadline
{
    public static System.Threading.Timer Arm(TimeSpan timeout, int exitCode) => new(_ =>
    {
        // Do not log or run managed ProcessExit callbacks here: either may be
        // the source of the hang. Normal paths report their result themselves.
        using var self = Process.GetCurrentProcess();
        TerminateProcess(self.Handle, (uint)exitCode);
    }, null, timeout, Timeout.InfiniteTimeSpan);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool TerminateProcess(IntPtr process, uint exitCode);

    public static TimeSpan? ForCommand(string[] args) => args.FirstOrDefault() switch
    {
        "--lifecycle-test-deadline" => TimeSpan.FromSeconds(2),
        "--check-prerequisites" or "--check-midi-recovery-access" => TimeSpan.FromSeconds(20),
        "--shutdown-installation" or "--shutdown-test-instance" => TimeSpan.FromSeconds(30),
        "--repair-midi-service" => TimeSpan.FromSeconds(90),
        "--after-recovery" => TimeSpan.FromSeconds(130),
        "--midi-smoke" => TimeSpan.FromSeconds(60),
        _ => null,
    };
}

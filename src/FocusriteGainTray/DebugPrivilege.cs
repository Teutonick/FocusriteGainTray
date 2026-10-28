using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace FocusriteGainTray;

// Scope is confined to the explicit elevated repair helper; restore the original token state.
internal sealed class DebugPrivilege : IDisposable
{
    private readonly SafeAccessTokenHandle _token;
    private readonly Privileges _previous;
    private DebugPrivilege(SafeAccessTokenHandle token, Privileges previous) { _token = token; _previous = previous; }
    public static DebugPrivilege Enable()
    {
        if (!OpenProcessToken(GetCurrentProcess(), 0x20 | 0x8, out var token))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Recovery OpenProcessToken failed");
        try
        {
            if (!LookupPrivilegeValue(null, "SeDebugPrivilege", out var luid))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Recovery LookupPrivilegeValue failed");
            var requested = new Privileges { Count = 1, Luid = luid, Attributes = 2 };
            bool adjusted = AdjustTokenPrivileges(token, false, ref requested, (uint)Marshal.SizeOf<Privileges>(), out var previous, out _);
            int error = Marshal.GetLastWin32Error(); // success may still mean ERROR_NOT_ALL_ASSIGNED
            if (!adjusted || error != 0) throw new Win32Exception(error, "Recovery could not enable SeDebugPrivilege");
            Log.Write("Recovery: SeDebugPrivilege enabled for verified process access.");
            return new DebugPrivilege(token, previous);
        }
        catch { token.Dispose(); throw; }
    }
    public void Dispose()
    {
        var previous = _previous;
        if (!RestoreTokenPrivileges(_token, false, ref previous, 0, IntPtr.Zero, IntPtr.Zero))
            Log.Write("Recovery: token privilege restore failed: " + Marshal.GetLastWin32Error());
        _token.Dispose();
    }
    [StructLayout(LayoutKind.Sequential)] private struct Luid { public uint Low; public int High; }
    [StructLayout(LayoutKind.Sequential)] private struct Privileges { public uint Count; public Luid Luid; public uint Attributes; }
    [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool OpenProcessToken(IntPtr process, uint access, out SafeAccessTokenHandle token);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool LookupPrivilegeValue(string? system, string name, out Luid luid);
    [DllImport("advapi32.dll", EntryPoint = "AdjustTokenPrivileges", SetLastError = true)] private static extern bool RestoreTokenPrivileges(SafeAccessTokenHandle token, bool disableAll, ref Privileges requested, uint size, IntPtr previous, IntPtr length);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool AdjustTokenPrivileges(SafeAccessTokenHandle token, bool disableAll, ref Privileges requested, uint size, out Privileges previous, out uint length);
}

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace FocusriteGainTray;

// Elevated, one-shot repair only. No service name, process id, executable or
// command is accepted from the caller. Never loaded by ordinary MIDI startup.
internal sealed class WindowsMidiServiceRecovery : IMidiServiceRecovery, IDisposable
{
    private readonly IntPtr _manager;
    private readonly IntPtr _service;
    public WindowsMidiServiceRecovery()
    {
        _manager = OpenSCManager(null, null, 0x0001 | 0x0004);
        if (_manager == IntPtr.Zero) throw new Win32Exception();
        _service = OpenService(_manager, "midisrv", 0x0004 | 0x0010 | 0x0020);
        if (_service == IntPtr.Zero) { var error = new Win32Exception(); CloseServiceHandle(_manager); throw error; }
    }
    public MidiServiceState Query()
    {
        if (!QueryServiceStatusEx(_service, 0, out var status, (uint)Marshal.SizeOf<Status>(), out _)) throw new Win32Exception();
        return new(status.State, status.Pid, (status.Type & 0x10) != 0 && (status.Type & 0x20) == 0);
    }
    public void Stop()
    {
        if (!ControlService(_service, 1, out _) && Marshal.GetLastWin32Error() != 1062) throw new Win32Exception();
    }
    public void Start()
    {
        if (!StartService(_service, 0, IntPtr.Zero) && Marshal.GetLastWin32Error() != 1056) throw new Win32Exception();
    }
    public void TerminateVerifiedService(MidiServiceState state)
    {
        using var process = OpenProcess(0x00100000 | 0x1000 | 0x0001, false, state.ProcessId);
        if (process.IsInvalid) throw new Win32Exception();
        var path = new StringBuilder(32768); uint length = (uint)path.Capacity;
        if (!QueryFullProcessImageName(process, 0, path, ref length)) throw new Win32Exception();
        var owners = Owners(state.ProcessId);
        string expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "MidiSrv.exe");
        if (!MidiRecoveryPolicy.CanTerminate(state, path.ToString(), expected, owners)
            || Query() != state) throw new IOException("Unverified/shared/changed MIDI service process; termination refused.");
        Log.Write("Recovery: terminating verified dedicated System32/MidiSrv.exe after stop timeout.");
        if (!TerminateProcess(process, 1)) throw new Win32Exception();
        if (WaitForSingleObject(process, 10000) != 0) throw new IOException("Verified MIDI process did not exit.");
    }
    private IReadOnlyList<string> Owners(uint pid)
    {
        EnumServicesStatusEx(_manager, 0, 0x30, 3, IntPtr.Zero, 0, out uint bytes, out _, IntPtr.Zero, null);
        if (bytes == 0 || bytes > 16 * 1024 * 1024) throw new IOException("Could not verify service process ownership.");
        IntPtr buffer = Marshal.AllocHGlobal((int)bytes);
        try
        {
            if (!EnumServicesStatusEx(_manager, 0, 0x30, 3, buffer, bytes, out _, out uint count, IntPtr.Zero, null)) throw new Win32Exception();
            var names = new List<string>();
            int size = Marshal.SizeOf<Entry>();
            for (int i = 0; i < count; i++)
            {
                var entry = Marshal.PtrToStructure<Entry>(IntPtr.Add(buffer, i * size));
                if (entry.Status.Pid == pid) names.Add(Marshal.PtrToStringUni(entry.Name) ?? "");
            }
            return names;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }
    public void Dispose() { CloseServiceHandle(_service); CloseServiceHandle(_manager); }
    [StructLayout(LayoutKind.Sequential)] private struct Status { public uint Type, State, Controls, Win32Exit, SpecificExit, Checkpoint, WaitHint, Pid, Flags; }
    [StructLayout(LayoutKind.Sequential)] private struct BasicStatus { public uint Type, State, Controls, Win32Exit, SpecificExit, Checkpoint, WaitHint; }
    [StructLayout(LayoutKind.Sequential)] private struct Entry { public IntPtr Name, DisplayName; public Status Status; }
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr OpenSCManager(string? machine, string? database, uint access);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr OpenService(IntPtr manager, string name, uint access);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool QueryServiceStatusEx(IntPtr service, int level, out Status status, uint size, out uint needed);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool ControlService(IntPtr service, uint control, out BasicStatus status);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool StartService(IntPtr service, uint count, IntPtr args);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool EnumServicesStatusEx(IntPtr manager, int level, uint type, uint state, IntPtr buffer, uint size, out uint needed, out uint count, IntPtr resume, string? group);
    [DllImport("advapi32.dll")] private static extern bool CloseServiceHandle(IntPtr handle);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern SafeProcessHandle OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool QueryFullProcessImageName(SafeProcessHandle process, uint flags, StringBuilder name, ref uint size);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateProcess(SafeProcessHandle process, uint code);
    [DllImport("kernel32.dll")] private static extern uint WaitForSingleObject(SafeProcessHandle handle, uint milliseconds);
}

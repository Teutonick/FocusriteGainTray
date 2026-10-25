using System.Runtime.InteropServices;
using System.Text.Json;

namespace FocusriteGainTray;

// Explicit QA mode only: publishes a temporary endpoint, opens/closes its WinMM input,
// then disconnects. Does not call SendGain or send any CC/note to physical hardware.
internal static class MidiSmoke
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MidiInCaps
    {
        public ushort Manufacturer, Product;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Name;
        public uint Support;
    }
    [DllImport("winmm.dll")] private static extern uint midiInGetNumDevs();
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)] private static extern uint midiInGetDevCapsW(nuint device, out MidiInCaps caps, uint size);
    [DllImport("winmm.dll")] private static extern uint midiInOpen(out IntPtr handle, uint device, nuint callback, nuint instance, uint flags);
    [DllImport("winmm.dll")] private static extern uint midiInClose(IntPtr handle);

    public static async Task<int> RunAsync(string output)
    {
        const string name = "FocGrain QA";
        var worker = new BoundedWorker<WindowsMidiVirtualController>(() => new(name));
        int code = 1;
        string phase = "connect";
        string status;
        try
        {
            await worker.ExecuteAsync(controller => controller.Connect(), TimeSpan.FromSeconds(10));
            phase = "winmm-enumeration-open-close";
            await Task.Run(() =>
            {
                var until = DateTime.UtcNow.AddSeconds(6);
                do
                {
                    uint count = midiInGetNumDevs();
                    for (uint id = 0; id < count; id++)
                    {
                        if (midiInGetDevCapsW(id, out var caps, (uint)Marshal.SizeOf<MidiInCaps>()) != 0 || !caps.Name.StartsWith(name, StringComparison.Ordinal)) continue;
                        uint result = midiInOpen(out var handle, id, 0, 0, 0);
                        if (result != 0) throw new IOException($"midiInOpen failed: {result}");
                        result = midiInClose(handle);
                        if (result != 0) throw new IOException($"midiInClose failed: {result}");
                        return;
                    }
                    Thread.Sleep(150);
                } while (DateTime.UtcNow < until);
                throw new IOException("The QA endpoint did not appear as a WinMM MIDI 1.0 input.");
            }).WaitAsync(TimeSpan.FromSeconds(8));
            phase = "disconnect";
            // QA allows extra time to distinguish a slow native close from the UI's shorter wait.
            await worker.ExecuteAsync(controller => controller.Disconnect(), TimeSpan.FromSeconds(15));
            code = 0;
            phase = "complete";
            status = "Temporary MTA endpoint creation, WinMM enumeration/open/close and disconnect succeeded. No gain commands sent. Not a physical Scarlett/FC2 test.";
        }
        catch (Exception ex) { status = ex.ToString(); }
        finally { await worker.StopAsync(TimeSpan.FromSeconds(2)); }
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { code, phase, status }, new JsonSerializerOptions { WriteIndented = true }));
        return code;
    }
}

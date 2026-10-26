using System.Runtime.InteropServices;

namespace FocusriteGainTray;

public static class LegacyMidiInputs
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Caps
    {
        public ushort Manufacturer, Product;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Name;
        public uint Support;
    }
    [DllImport("winmm.dll")] private static extern uint midiInGetNumDevs();
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)] private static extern uint midiInGetDevCapsW(nuint id, out Caps caps, uint size);

    public static bool Matches(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)
        || actual.StartsWith(expected + " (", StringComparison.OrdinalIgnoreCase);

    // Enumeration only. Never opens a MIDI input already used by FC2.
    public static bool Contains(string name)
    {
        uint count = midiInGetNumDevs();
        for (uint id = 0; id < count; id++)
            if (midiInGetDevCapsW(id, out var caps, (uint)Marshal.SizeOf<Caps>()) == 0 && Matches(caps.Name, name)) return true;
        return false;
    }
}

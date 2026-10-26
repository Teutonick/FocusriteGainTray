# Read-only WinMM inventory: never opens a port, creates an endpoint or sends MIDI.
param([string]$OutputPath = '')
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class FocGrainWinmmInventory {
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct InCaps {
        public ushort Manufacturer, Product;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string Name;
        public uint Support;
    }
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct OutCaps {
        public ushort Manufacturer, Product;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string Name;
        public ushort Technology, Voices, Notes, Channels;
        public uint Support;
    }
    [DllImport("winmm.dll")] public static extern uint midiInGetNumDevs();
    [DllImport("winmm.dll")] public static extern uint midiOutGetNumDevs();
    [DllImport("winmm.dll", CharSet=CharSet.Unicode)] public static extern uint midiInGetDevCapsW(UIntPtr id, out InCaps caps, uint size);
    [DllImport("winmm.dll", CharSet=CharSet.Unicode)] public static extern uint midiOutGetDevCapsW(UIntPtr id, out OutCaps caps, uint size);
}
'@
$inputs = @(for($id=0; $id -lt [FocGrainWinmmInventory]::midiInGetNumDevs(); $id++) {
    $caps=[FocGrainWinmmInventory+InCaps]::new()
    $code=[FocGrainWinmmInventory]::midiInGetDevCapsW([UIntPtr]$id,[ref]$caps,[Runtime.InteropServices.Marshal]::SizeOf($caps))
    [pscustomobject]@{Index=$id;Name=$caps.Name;Code=$code}
})
$outputs = @(for($id=0; $id -lt [FocGrainWinmmInventory]::midiOutGetNumDevs(); $id++) {
    $caps=[FocGrainWinmmInventory+OutCaps]::new()
    $code=[FocGrainWinmmInventory]::midiOutGetDevCapsW([UIntPtr]$id,[ref]$caps,[Runtime.InteropServices.Marshal]::SizeOf($caps))
    [pscustomobject]@{Index=$id;Name=$caps.Name;Code=$code}
})
$json=[pscustomobject]@{Inputs=$inputs;Outputs=$outputs} | ConvertTo-Json -Depth 4
if($OutputPath){$json | Set-Content -LiteralPath $OutputPath -Encoding utf8}
$json

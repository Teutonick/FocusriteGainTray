using Microsoft.Win32;
using System.Globalization;

namespace FocusriteGainTray;

internal static class MidiRecoveryState
{
    private const string RecoveryKey = @"SOFTWARE\FocGrain";
    private const string RecoveryValue = "MidiRecoveryCompletedUtcTicks";
    private static string Marker => Path.Combine(SettingsStore.DirectoryPath, "midi-recovery-required");
    public static bool Required
    {
        get
        {
            if (!File.Exists(Marker)) return false;
            try
            {
                // UAC can use another account. Only the elevated helper publishes this machine receipt.
                using var key = Registry.LocalMachine.OpenSubKey(RecoveryKey);
                long completed = key?.GetValue(RecoveryValue) is long ticks ? ticks : 0;
                if (RecoveryReceipt.Supersedes(File.ReadAllText(Marker), completed))
                {
                    Log.Write("Machine MIDI recovery receipt supersedes this user's dirty marker; clearing local gate.");
                    Clear();
                    return false;
                }
            }
            catch (Exception ex) { Log.Write("Reading MIDI recovery receipt failed; port remains blocked: " + ex); }
            return true;
        }
    }
    public static void RecordCompleted()
    {
        using var key = Registry.LocalMachine.CreateSubKey(RecoveryKey, writable: true);
        key.SetValue(RecoveryValue, DateTimeOffset.UtcNow.UtcTicks, RegistryValueKind.QWord);
        Log.Write("Machine MIDI recovery receipt recorded for cross-account UAC handoff.");
        Clear();
    }
    public static void MarkDirty()
    {
        try { Directory.CreateDirectory(SettingsStore.DirectoryPath); File.WriteAllText(Marker, DateTimeOffset.Now.ToString("O")); }
        catch (Exception ex) { Log.Write("Could not record MIDI recovery requirement: " + ex.Message); }
    }
    public static void Clear()
    {
        try { File.Delete(Marker); }
        catch (Exception ex) { Log.Write("Could not clear MIDI recovery requirement: " + ex.Message); }
    }
}

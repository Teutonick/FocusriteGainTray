namespace FocusriteGainTray;

internal static class MidiRecoveryState
{
    private static string Marker => Path.Combine(SettingsStore.DirectoryPath, "midi-recovery-required");
    public static bool Required => File.Exists(Marker);
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

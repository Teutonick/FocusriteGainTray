using System.Text.Json;

namespace FocusriteGainTray;

public sealed class ChannelSettings
{
    // MIDI CC values, not dB. A=low, B=working level.
    public int A { get; set; } = 0;
    public int B { get; set; } = 40;
}

public sealed class AppSettings
{
    public int SelectedInput { get; set; } = 1;
    public int Step { get; set; } = 3;
    public ChannelSettings Input1 { get; set; } = new();
    public ChannelSettings Input2 { get; set; } = new();
    public string ToggleHotkey { get; set; } = "Ctrl+Alt+M";
    public string IncreaseHotkey { get; set; } = "Ctrl+Alt+Up";
    public string DecreaseHotkey { get; set; } = "Ctrl+Alt+Down";
    public bool StartFocusriteControl2 { get; set; } = true;

    public ChannelSettings Channel(int channel) => channel == 2 ? Input2 : Input1;

    public void Validate()
    {
        SelectedInput = Math.Clamp(SelectedInput, 1, 2);
        Step = Math.Clamp(Step, 1, 32);
        Input1 ??= new ChannelSettings();
        Input2 ??= new ChannelSettings();
        foreach (var c in new[] { Input1, Input2 })
        {
            c.A = Math.Clamp(c.A, 0, 127);
            c.B = Math.Clamp(c.B, 0, 127);
            if (c.A > c.B) (c.A, c.B) = (c.B, c.A);
        }
    }
}

internal static class SettingsStore
{
    public static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FocusriteGainTray");
    public static readonly string FilePath = Path.Combine(DirectoryPath, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var result = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                if (result is not null)
                {
                    result.Validate();
                    return result;
                }
            }
        }
        catch (Exception e)
        {
            Log.Write("Loading settings failed: " + e);
        }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        settings.Validate();
        Directory.CreateDirectory(DirectoryPath);
        string temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, FilePath, overwrite: true);
    }
}

internal static class Log
{
    public static void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(SettingsStore.DirectoryPath);
            string path = Path.Combine(SettingsStore.DirectoryPath, "application.log");
            if (File.Exists(path) && new FileInfo(path).Length > 1024 * 1024)
                File.Move(path, path + ".previous", overwrite: true);
            File.AppendAllText(path,
                $"[{DateTimeOffset.Now:O}] [PID {Environment.ProcessId}] {message}{Environment.NewLine}");
        }
        catch { /* logging must never crash audio control */ }
    }
}

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
    private static readonly object Gate = new();
    internal const int FileLimit = 512 * 1024;
    public static void Write(string message)
    {
        try { Append(Path.Combine(SettingsStore.DirectoryPath, "application.log"),
            $"[{DateTimeOffset.Now:O}] [PID {Environment.ProcessId}] [TID {Environment.CurrentManagedThreadId}] {message}{Environment.NewLine}"); }
        catch { /* logging must never crash audio control */ }
    }

    internal static void Append(string path, string record)
    {
        lock (Gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string previous = path + ".previous";
            // Bound old files from earlier versions too, retaining their newest UTF-8 text.
            TrimExisting(path);
            TrimExisting(previous);
            byte[] bytes;
            if (System.Text.Encoding.UTF8.GetByteCount(record) > FileLimit)
            {
                byte[] suffix = System.Text.Encoding.UTF8.GetBytes("\n[log record truncated]\n");
                bytes = new byte[FileLimit];
                System.Text.Encoding.UTF8.GetEncoder().Convert(record.AsSpan(), bytes.AsSpan(0, FileLimit - suffix.Length), true,
                    out _, out int used, out _);
                suffix.CopyTo(bytes, used);
                Array.Resize(ref bytes, used + suffix.Length);
            }
            else bytes = System.Text.Encoding.UTF8.GetBytes(record);
            if (File.Exists(path) && new FileInfo(path).Length + bytes.Length > FileLimit)
                File.Move(path, previous, overwrite: true);
            using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            stream.Write(bytes);
        }
    }

    private static void TrimExisting(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length <= FileLimit) return;
        byte[] tail = new byte[FileLimit];
        using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            input.Seek(-FileLimit, SeekOrigin.End);
            input.ReadExactly(tail);
        }
        int start = 0;
        while (start < tail.Length && (tail[start] & 0xC0) == 0x80) start++;
        File.WriteAllBytes(path, tail.AsSpan(start).ToArray());
    }
}

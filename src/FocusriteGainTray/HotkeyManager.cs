using System.Runtime.InteropServices;

namespace FocusriteGainTray;

internal enum HotkeyAction { Toggle, Increase, Decrease }

internal sealed class HotkeyManager : NativeWindow, IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNoRepeat = 0x4000;
    private readonly Action<HotkeyAction> _callback;
    private readonly List<int> _registered = new();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public HotkeyManager(Action<HotkeyAction> callback)
    {
        _callback = callback;
        CreateHandle(new CreateParams());
    }

    public void Apply(AppSettings settings)
    {
        Clear();
        try
        {
            Add(100, settings.ToggleHotkey);
            Add(101, settings.IncreaseHotkey);
            Add(102, settings.DecreaseHotkey);
        }
        catch
        {
            Clear();
            throw;
        }
    }

    private void Add(int id, string chord)
    {
        var (modifiers, key) = Parse(chord);
        if (!RegisterHotKey(Handle, id, modifiers | ModNoRepeat, (uint)key))
            throw new InvalidOperationException($"Хоткей {chord} занят другой программой или недоступен (Win32 {Marshal.GetLastWin32Error()}).");
        _registered.Add(id);
    }

    public static (uint modifiers, Keys key) Parse(string chord)
    {
        if (string.IsNullOrWhiteSpace(chord)) throw new ArgumentException("Пустой хоткей");
        uint modifiers = 0;
        Keys key = Keys.None;
        foreach (var raw in chord.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (raw.ToUpperInvariant())
            {
                case "CTRL": case "CONTROL": modifiers |= ModControl; break;
                case "ALT": modifiers |= ModAlt; break;
                case "SHIFT": modifiers |= ModShift; break;
                case "WIN": modifiers |= ModWin; break;
                default:
                    if (key != Keys.None || !Enum.TryParse(raw, true, out key) || key == Keys.None)
                        throw new ArgumentException($"Неверный хоткей: {chord}");
                    break;
            }
        }
        if (modifiers == 0 || key == Keys.None || key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
            throw new ArgumentException($"Для {chord} нужна комбинация модификатора и обычной клавиши");
        return (modifiers, key);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey)
        {
            int id = m.WParam.ToInt32();
            if (id is >= 100 and <= 102)
                _callback((HotkeyAction)(id - 100));
        }
        base.WndProc(ref m);
    }

    private void Clear()
    {
        foreach (int id in _registered) UnregisterHotKey(Handle, id);
        _registered.Clear();
    }

    public void Dispose()
    {
        Clear();
        DestroyHandle();
        GC.SuppressFinalize(this);
    }
}

internal sealed class HotkeyCaptureBox : TextBox
{
    public HotkeyCaptureBox(string initial) { ReadOnly = true; Text = initial; Width = 190; }

    protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
    {
        e.IsInputKey = true;
        base.OnPreviewKeyDown(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.SuppressKeyPress = true;
        e.Handled = true;
        if (e.KeyCode is Keys.ShiftKey or Keys.ControlKey or Keys.Menu or Keys.LWin or Keys.RWin) return;
        if (!e.Control && !e.Alt && !e.Shift) return;
        var parts = new List<string>();
        if (e.Control) parts.Add("Ctrl");
        if (e.Alt) parts.Add("Alt");
        if (e.Shift) parts.Add("Shift");
        parts.Add(e.KeyCode.ToString());
        Text = string.Join('+', parts);
        base.OnKeyDown(e);
    }
}

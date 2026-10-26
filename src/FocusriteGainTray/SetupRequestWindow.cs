using System.Runtime.InteropServices;

namespace FocusriteGainTray;

// A second --setup invocation asks the existing tray process to show its wizard.
// No second MIDI host is started; the message carries no paths or executable payload.
internal sealed class SetupRequestWindow : NativeWindow, IDisposable
{
    private const string Caption = "FocGrain.SetupHost.A762C2E7-FBA2-4CB0-B856-90359B52446A";
    private static readonly uint MessageId = RegisterWindowMessage(Caption);
    private readonly Action _show;
    private readonly Action _close;
    private readonly Action _activate;
    public SetupRequestWindow(Action show, Action close, Action? activate = null)
    {
        _show = show;
        _close = close;
        _activate = activate ?? (() => { });
        CreateHandle(new CreateParams { Caption = Caption });
    }
    public static bool SignalExisting(bool showSetup = true)
    {
        var window = FindWindow(null, Caption);
        return MessageId != 0 && window != IntPtr.Zero && PostMessage(window, MessageId, showSetup ? new IntPtr(1) : IntPtr.Zero, IntPtr.Zero);
    }
    protected override void WndProc(ref Message message)
    {
        if (message.Msg == 0x10) { _close(); return; }
        if (MessageId != 0 && message.Msg == MessageId) { if (message.WParam != IntPtr.Zero) _show(); else _activate(); return; }
        base.WndProc(ref message);
    }
    public static void RequestClose(int processId)
    {
        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out uint id);
            var title = new System.Text.StringBuilder(160);
            GetWindowText(window, title, title.Capacity);
            if (id == processId && title.ToString() == Caption) PostMessage(window, 0x10, IntPtr.Zero, IntPtr.Zero);
            return true;
        }, IntPtr.Zero);
    }
    private delegate bool EnumWindowProc(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowProc callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, System.Text.StringBuilder text, int count);
    public void Dispose() { DestroyHandle(); GC.SuppressFinalize(this); }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string name);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string? className, string title);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}

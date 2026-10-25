using System.Runtime.InteropServices;

namespace FocusriteGainTray;

// A second --setup invocation asks the existing tray process to show its wizard.
// No second MIDI host is started; the message carries no paths or executable payload.
internal sealed class SetupRequestWindow : NativeWindow, IDisposable
{
    private const string Caption = "FocGrain.SetupHost.A762C2E7-FBA2-4CB0-B856-90359B52446A";
    private static readonly uint MessageId = RegisterWindowMessage(Caption);
    private readonly Action _show;
    public SetupRequestWindow(Action show)
    {
        _show = show;
        CreateHandle(new CreateParams { Caption = Caption });
    }
    public static bool SignalExisting()
    {
        var window = FindWindow(null, Caption);
        return MessageId != 0 && window != IntPtr.Zero && PostMessage(window, MessageId, IntPtr.Zero, IntPtr.Zero);
    }
    protected override void WndProc(ref Message message)
    {
        if (MessageId != 0 && message.Msg == MessageId) { _show(); return; }
        base.WndProc(ref message);
    }
    public void Dispose() { DestroyHandle(); GC.SuppressFinalize(this); }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string name);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string? className, string title);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}

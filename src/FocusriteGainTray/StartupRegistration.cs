using Microsoft.Win32;

namespace FocusriteGainTray;

internal static class StartupRegistration
{
    private const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public static bool Enabled
    {
        get
        {
            try { using var key = Registry.CurrentUser.OpenSubKey(Key); return key?.GetValue("FocGrain") is string; }
            catch (System.Security.SecurityException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
    }
    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(Key);
        if (enabled) key.SetValue("FocGrain", $"\"{Environment.ProcessPath}\" --autostart");
        else key.DeleteValue("FocGrain", false);
    }
}

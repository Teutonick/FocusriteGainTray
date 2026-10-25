using System.Threading;

namespace FocusriteGainTray;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, name: @"Local\FocusriteGainTray", out bool firstInstance);
        if (!firstInstance)
        {
            MessageBox.Show("Focusrite Gain Tray уже запущен в трее.", "Focusrite Gain Tray",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}

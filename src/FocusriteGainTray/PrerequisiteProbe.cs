using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Devices.Midi2;
using Windows.Devices.Midi2.Transports.Virtual;

namespace FocusriteGainTray;

internal static class PrerequisiteProbe
{
    public static int Run(string outputPath)
    {
        int midiCode = PrerequisitePolicy.ApiUnavailable;
        string midi = Ui.T("Не удалось загрузить MIDI API.", "Could not load the MIDI API.");
        Version? fc2 = null;
        bool driver = false;
        bool os = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 26200)
            && System.Runtime.InteropServices.RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.X64;
        try
        {
            if (os) (midiCode, midi) = CheckMidi();
            else midi = Ui.T("Нужна Windows 11 25H2 x64, build 26200 или новее.", "Windows 11 25H2 x64, build 26200 or later, is required.");
        }
        catch (Exception ex) { midi = Ui.T("MIDI API недоступен: ", "MIDI API unavailable: ") + ex.Message; }

        string fc2Status = Ui.T("Не найден Focusrite Control 2.", "Focusrite Control 2 was not found.");
        try
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var uninstall = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (uninstall is null) continue;
                foreach (string name in uninstall.GetSubKeyNames())
                {
                  try
                  {
                    using var entry = uninstall.OpenSubKey(name);
                    var display = entry?.GetValue("DisplayName") as string ?? "";
                    var location = entry?.GetValue("InstallLocation") as string ?? "";
                    if (display.StartsWith("Focusrite Audio Drivers ", StringComparison.OrdinalIgnoreCase)
                        && Directory.Exists(location)) driver = true;
                    if (!display.StartsWith("Focusrite Control 2 ", StringComparison.OrdinalIgnoreCase)) continue;
                    string exe = Path.Combine(location, "Focusrite Control 2.exe");
                    if (!Path.IsPathFullyQualified(exe) || !File.Exists(exe)) continue;
                    var info = FileVersionInfo.GetVersionInfo(exe);
                    var version = new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
                    if (fc2 is null || version > fc2) fc2 = version;
                  }
                  catch (UnauthorizedAccessException) { /* Unrelated protected uninstall entries are not prerequisites. */ }
                  catch (System.Security.SecurityException) { /* Missing prerequisite evidence still fails closed. */ }
                }
            }
            if (fc2 is not null) fc2Status = $"Focusrite Control 2 {fc2}" +
                (fc2 < PrerequisitePolicy.FocusriteBaseline ? Ui.T(" — требуется обновление.", " — update required.") : Ui.T(" — найден.", " — found."));
        }
        catch (Exception ex)
        {
            fc2 = null;
            driver = false;
            fc2Status = Ui.T("Ошибка проверки FC2: ", "FC2 detection error: ") + ex.Message;
        }

        int code = PrerequisitePolicy.Evaluate(os, midiCode, fc2, driver);
        string Clean(string text) => text.Replace('\r', ' ').Replace('\n', ' ');
        File.WriteAllText(outputPath,
            $"[Prerequisites]\r\nCode={code}\r\nMidiCode={midiCode}\r\nMidi={Clean(midi)}\r\n" +
            $"Focusrite={Clean(fc2Status)}\r\nFocusriteVersion={fc2}\r\nDriver={(driver ? Ui.T("Пакет драйверов Focusrite найден.", "Focusrite driver package found.") : Ui.T("Пакет драйверов Focusrite не найден — установите FC2 с драйвером.", "Focusrite driver package not found — install FC2 with its driver."))}\r\n",
            Encoding.Unicode);
        return code;
    }

    // Keep WinRT activation outside Run's JIT boundary so missing API/native dependencies are caught.
    // No sessions, endpoints or MIDI messages are created during prerequisite detection.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (int Code, string Message) CheckMidi()
    {
        if (!MidiApi.EnsureServiceAvailable())
            return (PrerequisitePolicy.ServiceUnavailable, Ui.T("Windows MIDI Services недоступен. Проверьте обновления Windows и Legacy API Mode.", "Windows MIDI Services is unavailable. Check Windows updates and Legacy API Mode."));
        if (!MidiVirtualDeviceManager.IsTransportAvailable)
            return (PrerequisitePolicy.TransportUnavailable, Ui.T("Virtual Device App Transport недоступен. Проверьте обновления и настройки Windows MIDI Services.", "Virtual Device App Transport is unavailable. Check Windows updates and MIDI Services settings."));
        return (PrerequisitePolicy.Ready, Ui.T("Windows MIDI Services и Virtual Device App Transport доступны.", "Windows MIDI Services and Virtual Device App Transport are available."));
    }
}

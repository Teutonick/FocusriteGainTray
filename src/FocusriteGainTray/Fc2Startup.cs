namespace FocusriteGainTray;

public enum Fc2StartupResult { Disabled, WaitingForMidi, AlreadyHandled, AlreadyRunning, Started, NotInstalled }

// Per-run startup orchestration. No MIDI capability is passed to this class.
public sealed class Fc2Startup(Func<string?> locate, Func<string, bool> running, Action<string> launch)
{
    private bool _handled;
    public Fc2StartupResult Run(bool enabled, bool midiReady)
    {
        if (!enabled) return Fc2StartupResult.Disabled;
        if (!midiReady) return Fc2StartupResult.WaitingForMidi;
        if (_handled) return Fc2StartupResult.AlreadyHandled;
        string? path = locate();
        if (path is null) return Fc2StartupResult.NotInstalled;
        if (running(path)) { _handled = true; return Fc2StartupResult.AlreadyRunning; }
        launch(path);
        _handled = true;
        return Fc2StartupResult.Started;
    }
}

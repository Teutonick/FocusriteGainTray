namespace FocusriteGainTray;

public readonly record struct MidiServiceState(uint State, uint ProcessId, bool OwnProcess);
public interface IMidiServiceRecovery
{
    MidiServiceState Query();
    void Stop();
    void TerminateVerifiedService(MidiServiceState state);
    void Start();
}
public static class MidiRecoveryPolicy
{
    public const int DirtyShutdown = 31;
    public static bool NeedsRecovery(Version version, int exitCode, bool forced) =>
        forced || exitCode != 0 || version < new Version(1, 0, 4);

    public static bool CanTerminate(MidiServiceState state, string image, string expected, IReadOnlyList<string> owners) =>
        state.State == 3 && state.ProcessId > 0 && state.OwnProcess
        && string.Equals(Path.GetFullPath(image), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase)
        && owners.Count == 1 && string.Equals(owners[0], "midisrv", StringComparison.OrdinalIgnoreCase);

    public static void Restart(IMidiServiceRecovery service, Action wait)
    {
        var state = service.Query();
        if (state.State is not (1 or 3)) service.Stop();
        for (int i = 0; i < 20 && (state = service.Query()).State != 1; i++) wait();
        state = service.Query();
        if (state.State != 1)
        {
            if (state.State != 3) throw new IOException("MIDI service did not enter Stop Pending; forced recovery refused.");
            service.TerminateVerifiedService(state);
            for (int i = 0; i < 20 && service.Query().State != 1; i++) wait();
            if (service.Query().State != 1) throw new IOException("MIDI service did not stop.");
        }
        service.Start();
        for (int i = 0; i < 20 && service.Query().State != 4; i++) wait();
        if (service.Query().State != 4) throw new IOException("MIDI service did not return to Running.");
    }
}

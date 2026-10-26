namespace FocusriteGainTray;

// Machine-wide device ownership plus the legacy session lock, so older versions
// in this session cannot publish the same endpoint concurrently. Fail closed.
internal sealed class SingleInstance : IDisposable
{
    private readonly Mutex _machine;
    private readonly Mutex _legacy;
    private SingleInstance(Mutex machine, Mutex legacy) { _machine = machine; _legacy = legacy; }
    public static bool TryAcquire(out SingleInstance? lease) => TryAcquire(
        @"Global\FocGrain.DeviceOwner.A762C2E7-FBA2-4CB0-B856-90359B52446A",
        @"Local\FocusriteGainTray", out lease);

    internal static bool TryAcquire(string machineName, string legacyName, out SingleInstance? lease)
    {
        lease = null;
        Mutex? machine = null, legacy = null;
        bool machineOwned = false, legacyOwned = false;
        try
        {
            machine = new Mutex(true, machineName, out bool first);
            if (!first) return false;
            machineOwned = true;
            legacy = new Mutex(true, legacyName, out bool legacyFirst);
            if (!legacyFirst) return false;
            legacyOwned = true;
            lease = new SingleInstance(machine, legacy);
            return true;
        }
        catch (UnauthorizedAccessException) { return false; }
        catch (System.Security.SecurityException) { return false; }
        catch (IOException) { return false; }
        finally
        {
            if (lease is null)
            {
                if (legacyOwned) legacy!.ReleaseMutex();
                if (machineOwned) machine!.ReleaseMutex();
                legacy?.Dispose(); machine?.Dispose();
            }
        }
    }
    public void Dispose() { _legacy.ReleaseMutex(); _legacy.Dispose(); _machine.ReleaseMutex(); _machine.Dispose(); }
}

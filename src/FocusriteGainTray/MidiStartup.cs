namespace FocusriteGainTray;

// Retries only returned connection failures. A timed-out native call is still running:
// never launch another attempt in that case or replay any gain command.
public static class MidiStartup
{
    public static async Task RunAsync(bool atLogon, Func<Task<bool>> connect,
        Func<bool> faulted, Func<bool> closing, Func<TimeSpan, Task> delay)
    {
        if (atLogon && !closing()) await delay(TimeSpan.FromSeconds(15));
        for (int attempt = 0; attempt < 3 && !closing() && !faulted(); attempt++)
        {
            if (await connect()) return;
            if (attempt < 2 && !closing() && !faulted()) await delay(TimeSpan.FromSeconds(5));
        }
    }
}

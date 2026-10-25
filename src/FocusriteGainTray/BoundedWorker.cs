using System.Collections.Concurrent;

namespace FocusriteGainTray;

// Owns the native object on one background MTA thread. Never invokes UI code from that thread.
// A native call cannot be cancelled: after a timeout, reject every further operation instead of
// racing a second connection or replaying queued gain changes against an unknown state.
public sealed class BoundedWorker<T> where T : class, IDisposable
{
    private readonly BlockingCollection<(Action<T> Work, TaskCompletionSource Done)> _queue = new();
    private readonly TaskCompletionSource _stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _inFlight;
    private int _faulted;
    private int _disposed;

    public BoundedWorker(Func<T> factory)
    {
        var thread = new Thread(() => Run(factory)) { IsBackground = true, Name = "FocGrain MIDI MTA" };
        if (OperatingSystem.IsWindows()) thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();
    }

    public bool Faulted => Volatile.Read(ref _faulted) != 0;

    public async Task ExecuteAsync(Action<T> action, TimeSpan timeout)
    {
        if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(BoundedWorker<T>));
        if (Faulted) throw new InvalidOperationException(Ui.T("MIDI не ответил. Перезапустите FocGrain; повторные подключения заблокированы.", "MIDI did not respond. Restart FocGrain; further operations are blocked."));
        if (Interlocked.CompareExchange(ref _inFlight, 1, 0) != 0)
            throw new InvalidOperationException(Ui.T("Дождитесь завершения текущей MIDI-операции.", "Wait for the current MIDI operation to finish."));
        try
        {
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Add((action, done));
            try { await done.Task.WaitAsync(timeout).ConfigureAwait(false); }
            catch (TimeoutException)
            {
                Interlocked.Exchange(ref _faulted, 1);
                // Observe a late failure even though this request has already timed out.
                _ = done.Task.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                throw new TimeoutException(Ui.T("MIDI-операция не завершилась вовремя. Состояние неизвестно. Перезапустите FocGrain и проверьте обновления Windows MIDI Services.", "The MIDI operation timed out. State is unknown. Restart FocGrain and check Windows MIDI Services updates."));
            }
        }
        finally { Interlocked.Exchange(ref _inFlight, 0); }
    }

    public async Task StopAsync(TimeSpan timeout)
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0) _queue.CompleteAdding();
        try { await _stopped.Task.WaitAsync(timeout).ConfigureAwait(false); }
        catch (TimeoutException) { Interlocked.Exchange(ref _faulted, 1); }
    }

    private void Run(Func<T> factory)
    {
        T? instance = default;
        try
        {
            foreach (var job in _queue.GetConsumingEnumerable())
            {
                try
                {
                    instance ??= factory();
                    job.Work(instance);
                    job.Done.TrySetResult();
                }
                catch (Exception ex) { job.Done.TrySetException(ex); }
            }
        }
        finally
        {
            try { instance?.Dispose(); }
            catch { /* The UI must still be able to quit if the service failed. */ }
            _queue.Dispose();
            _stopped.TrySetResult();
        }
    }
}

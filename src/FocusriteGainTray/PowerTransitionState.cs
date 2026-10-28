namespace FocusriteGainTray;

// UI-thread state: old asynchronous completions must not restore readiness after sleep.
public sealed class PowerTransitionState
{
    public int Generation { get; private set; }
    public bool Suspended { get; private set; }
    public bool Recovering { get; private set; }
    public bool BlocksGain => Suspended || Recovering;

    public int Transition(bool resumed)
    {
        Generation++;
        Suspended = !resumed;
        Recovering = resumed;
        return Generation;
    }

    public bool AcceptsCompletion(int generation) => generation == Generation && !Suspended;
    public void CompleteRecovery(int generation)
    {
        if (generation == Generation) Recovering = false;
    }
}

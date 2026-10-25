namespace FocusriteGainTray;

// Installer contract: codes are also consumed by installer/FocGrain.iss.
public static class PrerequisitePolicy
{
    public const int Ready = 0;
    public const int UnsupportedWindows = 20;
    public const int ApiUnavailable = 21;
    public const int ServiceUnavailable = 22;
    public const int TransportUnavailable = 23;
    public const int FocusriteMissing = 24;
    public const int FocusriteOutdated = 25;
    public const int DriverMissing = 26;
    public const int ProbeFailed = 29;
    // Reviewed FC2 baseline; presence is not evidence of hardware/MIDI compatibility.
    public static readonly Version FocusriteBaseline = new(1, 1210, 0, 0);

    public static int Evaluate(bool supportedWindows, int midiCode, Version? focusriteVersion, bool driverRegistered)
    {
        if (!supportedWindows) return UnsupportedWindows;
        if (midiCode != Ready) return midiCode is ApiUnavailable or ServiceUnavailable or TransportUnavailable ? midiCode : ProbeFailed;
        if (focusriteVersion is null) return FocusriteMissing;
        if (focusriteVersion < FocusriteBaseline) return FocusriteOutdated;
        return driverRegistered ? Ready : DriverMissing;
    }
}

using System.Globalization;

namespace FocusriteGainTray;

public static class RecoveryReceipt
{
    // Malformed/unknown dirty state never grants permission to open MIDI.
    public static bool Supersedes(string marker, long completedUtcTicks) =>
        completedUtcTicks > 0 && completedUtcTicks <= DateTimeOffset.MaxValue.UtcTicks
        && DateTimeOffset.TryParseExact(marker.Trim(), "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dirty)
        && completedUtcTicks > dirty.UtcTicks;
}

namespace FocusriteGainTray;

// Tracks commands accepted by the transport, never physical hardware readback.
public sealed class GainState
{
    private readonly int?[] _sent = new int?[2];
    public int? LastSent(int input) => _sent[Index(input)];
    public int Next(int input, ChannelSettings channel) => LastSent(input) == channel.B ? channel.A : channel.B;
    public void Record(int input, int value)
    {
        if (value is < 0 or > 127) throw new ArgumentOutOfRangeException(nameof(value));
        _sent[Index(input)] = value;
    }
    public string Mode(int input, ChannelSettings channel) => LastSent(input) switch
    {
        null => "?",
        int value when value == channel.A => "MIN",
        int value when value == channel.B => "MAX",
        _ => "MID"
    };
    public void Reset() => Array.Clear(_sent);
    public static byte Controller(int input) => (byte)(12 + Index(input));
    private static int Index(int input) => input is 1 or 2 ? input - 1 : throw new ArgumentOutOfRangeException(nameof(input));
}

namespace FocusriteGainTray;

internal static class AppIcons
{
    // Caller owns the returned icon. The mark and all tray states share the same original artwork.
    public static Icon Load(string state = "app")
    {
        string name = state == "app" ? "FocGrain.ico" : $"FocGrain-{state}.ico";
        using var stream = typeof(AppIcons).Assembly.GetManifestResourceStream("FocGrain.Assets." + name)
            ?? throw new InvalidOperationException("Missing embedded icon: " + name);
        return new Icon(stream);
    }
}

using System.Globalization;

namespace FocusriteGainTray;

public static class Ui
{
    public static string LanguageFor(string cultureName) =>
        cultureName.Equals("ru", StringComparison.OrdinalIgnoreCase) || cultureName.StartsWith("ru-", StringComparison.OrdinalIgnoreCase) ? "ru" : "en";
    public static bool Russian => LanguageFor(CultureInfo.CurrentUICulture.Name) == "ru";
    public static string T(string ru, string en) => Russian ? ru : en;
    public static void Initialize(string? requested = null)
    {
        var language = LanguageFor(requested ?? CultureInfo.CurrentUICulture.Name);
        var culture = CultureInfo.GetCultureInfo(language);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}

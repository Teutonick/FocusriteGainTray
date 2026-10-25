using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FocusriteGainTray;

public sealed record ReleaseInfo(Version Version, Uri Page);

public static class ProductInfo
{
    public const string Repository = "Teutonick/FocusriteGainTray";
    public const string ReleasesUrl = "https://github.com/" + Repository + "/releases";
    public static Version CurrentVersion => typeof(ProductInfo).Assembly.GetName().Version ?? new Version(0, 0, 0);
    public static string VersionText => CurrentVersion.ToString(3);
}

public static class ReleaseParser
{
    public static ReleaseInfo Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var release = doc.RootElement;
        if (release.GetProperty("draft").GetBoolean() || release.GetProperty("prerelease").GetBoolean())
            throw new FormatException("Not a stable published release");
        string tag = release.GetProperty("tag_name").GetString() ?? "";
        if (!Regex.IsMatch(tag, @"^v?(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$", RegexOptions.CultureInvariant)
            || !Version.TryParse(tag.TrimStart('v'), out var version))
            throw new FormatException("Expected a stable vMAJOR.MINOR.PATCH release tag");
        // Build a repository-local URL; never launch an arbitrary html_url from a response.
        return new ReleaseInfo(version, new Uri(ProductInfo.ReleasesUrl + "/tag/" + Uri.EscapeDataString(tag)));
    }
}

public enum UpdateStatus { Available, Current, NoRelease, RateLimited }
public sealed record UpdateResult(UpdateStatus Status, ReleaseInfo? Release = null);

public sealed class UpdateClient
{
    private readonly HttpClient _http;
    public UpdateClient(HttpClient http) => _http = http;

    public async Task<UpdateResult> CheckAsync(Version current, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{ProductInfo.Repository}/releases/latest");
        request.Headers.UserAgent.ParseAdd("FocGrain/" + ProductInfo.VersionText);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound) return new(UpdateStatus.NoRelease);
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests) return new(UpdateStatus.RateLimited);
        response.EnsureSuccessStatusCode();
        var release = ReleaseParser.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        return new(release.Version > new Version(current.Major, current.Minor, current.Build) ? UpdateStatus.Available : UpdateStatus.Current, release);
    }
}

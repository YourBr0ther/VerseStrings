using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using VerseStrings.Core;

namespace VerseStrings.Services;

public sealed class SelfUpdater
{
    public static readonly string ReleasesPageUrl =
        $"https://github.com/{Branding.SelfUpdateRepo}/releases/latest";

    private readonly HttpClient _http;

    public SelfUpdater(HttpClient http)
    {
        _http = http;
    }

    public Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    /// <summary>
    /// Returns the latest tagged version if it is newer than the currently
    /// running assembly, or null when the app is already up to date / the
    /// check failed (offline, rate-limited, etc).
    /// </summary>
    public async Task<Version?> CheckForNewVersionAsync(CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.github.com/repos/{Branding.SelfUpdateRepo}/releases/latest");
        req.Headers.UserAgent.ParseAdd(GithubReleaseClient.UserAgent);
        req.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return null;

        var payload = await resp.Content.ReadFromJsonAsync<ReleasePayload>(cancellationToken: ct);
        var latest = VersionParser.TryParseTag(payload?.TagName);
        if (latest is null) return null;

        return latest.CompareTo(CurrentVersion) > 0 ? latest : null;
    }

    private sealed class ReleasePayload
    {
        [JsonPropertyName("tag_name")] public string? TagName { get; set; }
    }
}

using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace VerseStrings.Services;

public sealed class SelfUpdater
{
    private const string Repo = "YourBr0ther/VerseStrings";

    private readonly HttpClient _http;

    public SelfUpdater(HttpClient http)
    {
        _http = http;
    }

    public Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    public Version? LatestVersion { get; private set; }

    public string LatestReleaseUrl { get; private set; } =
        $"https://github.com/{Repo}/releases/latest";

    public async Task<bool> CheckAsync(CancellationToken ct = default)
    {
        var latest = await FetchLatestAsync(ct);
        if (latest is null) return false;

        LatestVersion = latest.Value.version;
        LatestReleaseUrl = latest.Value.htmlUrl;
        return latest.Value.version.CompareTo(CurrentVersion) > 0;
    }

    private async Task<(Version version, string htmlUrl)?> FetchLatestAsync(CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Repo}/releases/latest");
        req.Headers.UserAgent.ParseAdd("VerseStringsWatcher");
        req.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return null;

        var payload = await resp.Content.ReadFromJsonAsync<ReleasePayload>(cancellationToken: ct);
        if (string.IsNullOrWhiteSpace(payload?.TagName)) return null;

        var version = ParseSemverPrefix(payload.TagName);
        if (version is null) return null;

        var url = string.IsNullOrWhiteSpace(payload.HtmlUrl)
            ? $"https://github.com/{Repo}/releases/latest"
            : payload.HtmlUrl;

        return (version, url);
    }

    private static Version? ParseSemverPrefix(string tag)
    {
        var trimmed = tag.TrimStart('v', 'V');
        var dashIdx = trimmed.IndexOf('-');
        if (dashIdx >= 0) trimmed = trimmed[..dashIdx];
        return Version.TryParse(trimmed, out var v) ? v : null;
    }

    private sealed class ReleasePayload
    {
        [JsonPropertyName("tag_name")] public string? TagName { get; set; }
        [JsonPropertyName("html_url")] public string? HtmlUrl { get; set; }
    }
}

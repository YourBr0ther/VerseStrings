using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using VerseStrings.Core;

namespace VerseStrings.Services;

public sealed class SelfUpdater
{
    private const string Repo = "YourBr0ther/VerseStrings";

    public static readonly string ReleasesPageUrl = $"https://github.com/{Repo}/releases/latest";

    private readonly HttpClient _http;

    public SelfUpdater(HttpClient http)
    {
        _http = http;
    }

    public Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    public Version? LatestVersion { get; private set; }

    public async Task<bool> CheckAsync(CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Repo}/releases/latest");
        req.Headers.UserAgent.ParseAdd(GithubReleaseClient.UserAgent);
        req.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return false;

        var payload = await resp.Content.ReadFromJsonAsync<ReleasePayload>(cancellationToken: ct);
        if (string.IsNullOrWhiteSpace(payload?.TagName)) return false;

        var latest = ParseSemverPrefix(payload.TagName);
        if (latest is null) return false;

        LatestVersion = latest;
        return latest.CompareTo(CurrentVersion) > 0;
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
    }
}

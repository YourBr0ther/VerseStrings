using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace VerseStrings.Core;

public sealed class GithubReleaseClient
{
    private readonly HttpClient _http;
    private readonly string _userAgent;

    public GithubReleaseClient(HttpClient http, string userAgent = "VerseStrings")
    {
        _http = http;
        _userAgent = userAgent;
    }

    public async Task<ReleaseInfo?> GetLatestAsync(string repo, string assetName, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{repo}/releases/latest");
        req.Headers.UserAgent.ParseAdd(_userAgent);
        req.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
            return null;

        var payload = await resp.Content.ReadFromJsonAsync<ReleasePayload>(cancellationToken: ct);
        if (payload is null)
            return null;

        var asset = payload.Assets.FirstOrDefault(a =>
            string.Equals(a.Name, assetName, StringComparison.OrdinalIgnoreCase));
        if (asset is null)
            return null;

        return new ReleaseInfo(
            TagName: payload.TagName ?? "",
            Name: payload.Name ?? payload.TagName ?? "",
            PublishedAt: payload.PublishedAt ?? DateTimeOffset.MinValue,
            AssetName: asset.Name ?? "",
            AssetDownloadUrl: asset.BrowserDownloadUrl ?? "",
            AssetSizeBytes: asset.Size,
            AssetSha256: ExtractSha256(asset.Digest));
    }

    public async Task DownloadAssetAsync(ReleaseInfo release, string destinationPath, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, release.AssetDownloadUrl);
        req.Headers.UserAgent.ParseAdd(_userAgent);

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        await using var fs = File.Create(destinationPath);
        await resp.Content.CopyToAsync(fs, ct);
    }

    private static string? ExtractSha256(string? digest)
    {
        if (string.IsNullOrWhiteSpace(digest)) return null;
        const string prefix = "sha256:";
        return digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? digest[prefix.Length..]
            : digest;
    }

    private sealed class ReleasePayload
    {
        [JsonPropertyName("tag_name")] public string? TagName { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("published_at")] public DateTimeOffset? PublishedAt { get; set; }
        [JsonPropertyName("assets")] public List<AssetPayload> Assets { get; set; } = new();
    }

    private sealed class AssetPayload
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("browser_download_url")] public string? BrowserDownloadUrl { get; set; }
        [JsonPropertyName("size")] public long Size { get; set; }
        [JsonPropertyName("digest")] public string? Digest { get; set; }
    }
}

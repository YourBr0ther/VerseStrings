using System.Reflection;
using VerseStrings.Core;

namespace VerseStrings.Services;

public sealed class SelfUpdater
{
    public static readonly string ReleasesPageUrl =
        $"https://github.com/{Branding.SelfUpdateRepo}/releases/latest";

    private readonly GithubReleaseClient _github;

    public SelfUpdater(GithubReleaseClient github)
    {
        _github = github;
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
        var tag = await _github.GetLatestTagAsync(Branding.SelfUpdateRepo, ct);
        var latest = VersionParser.TryParseTag(tag);
        if (latest is null) return null;

        return latest.CompareTo(CurrentVersion) > 0 ? latest : null;
    }
}

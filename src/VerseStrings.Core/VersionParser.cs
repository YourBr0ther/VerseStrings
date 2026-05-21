namespace VerseStrings.Core;

public static class VersionParser
{
    /// <summary>
    /// Parse a GitHub-style release tag (e.g. "v0.1.0" or "v1.2.3-beta1") into
    /// a Version. Returns null if the tag isn't recognizable as semver. Any
    /// pre-release suffix (anything after the first '-') is dropped before
    /// comparison so that "v0.2.0-beta1" sorts the same as "v0.2.0".
    /// </summary>
    public static Version? TryParseTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;

        var trimmed = tag.TrimStart('v', 'V');
        var dashIdx = trimmed.IndexOf('-');
        if (dashIdx >= 0) trimmed = trimmed[..dashIdx];

        return Version.TryParse(trimmed, out var v) ? v : null;
    }
}

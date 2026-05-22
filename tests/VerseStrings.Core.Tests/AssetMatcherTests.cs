using VerseStrings.Core;
using Xunit;

namespace VerseStrings.Core.Tests;

public class AssetMatcherTests
{
    [Fact]
    public void ExactPattern_MatchesCaseInsensitively()
    {
        var names = new[] { "StarStrings.zip", "OtherFile.zip" };
        Assert.Equal("StarStrings.zip", AssetMatcher.SelectFirst(names, "StarStrings.zip"));
        Assert.Equal("StarStrings.zip", AssetMatcher.SelectFirst(names, "starstrings.zip"));
    }

    [Fact]
    public void ExactPattern_NoMatchReturnsNull()
    {
        var names = new[] { "StarStrings.zip" };
        Assert.Null(AssetMatcher.SelectFirst(names, "NoSuchFile.zip"));
    }

    [Fact]
    public void RegexPattern_MatchesVersionSuffixedAsset()
    {
        var names = new[]
        {
            "ScCompLangPackRemix-4.7.1-LIVE.zip",
            "ScCompLangPackRemix-4.8.0-LIVE.zip",
        };
        var match = AssetMatcher.SelectFirst(names, @"^ScCompLangPackRemix-.*-LIVE\.zip$");

        Assert.NotNull(match);
        Assert.EndsWith("LIVE.zip", match);
    }

    [Fact]
    public void RegexPattern_RejectsPtuAssetByLiveAnchor()
    {
        // Why: BeltaKoda's repo publishes LIVE and PTU assets per release;
        // we only want the LIVE one. The pattern's anchor on "-LIVE.zip$"
        // is what excludes PTU.
        var names = new[] { "ScCompLangPackRemix-4.8.0-PTU.zip" };
        Assert.Null(AssetMatcher.SelectFirst(names, @"^ScCompLangPackRemix-.*-LIVE\.zip$"));
    }

    [Fact]
    public void RegexPattern_InvalidReturnsNull()
    {
        // Unbalanced bracket — Regex throws ArgumentException. We swallow.
        var names = new[] { "anything.zip" };
        Assert.Null(AssetMatcher.SelectFirst(names, @"[unclosed"));
    }

    [Fact]
    public void EmptyPattern_ReturnsNull()
    {
        Assert.Null(AssetMatcher.SelectFirst(new[] { "x.zip" }, ""));
    }
}

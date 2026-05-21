using Xunit;

namespace VerseStrings.Core.Tests;

public class VersionParserTests
{
    [Theory]
    [InlineData("v0.1.0",   0, 1, 0)]
    [InlineData("V0.1.0",   0, 1, 0)]
    [InlineData("0.1.0",    0, 1, 0)]
    [InlineData("v1.2.3",   1, 2, 3)]
    [InlineData("v10.20.30", 10, 20, 30)]
    public void TryParseTag_AcceptsCanonicalReleaseTags(string tag, int major, int minor, int build)
    {
        var v = VersionParser.TryParseTag(tag);
        Assert.NotNull(v);
        Assert.Equal(new Version(major, minor, build), v);
    }

    [Theory]
    [InlineData("v0.2.0-beta1",  0, 2, 0)]
    [InlineData("v1.0.0-rc.2",   1, 0, 0)]
    [InlineData("v2.0.0-alpha",  2, 0, 0)]
    public void TryParseTag_DropsPrereleaseSuffix(string tag, int major, int minor, int build)
    {
        // Why: the prerelease suffix is informational. For "is the remote newer than mine?"
        // we treat 0.2.0-beta1 and 0.2.0 as equivalent ordering keys. The toast still says
        // the actual tag the user will land on when they click through.
        var v = VersionParser.TryParseTag(tag);
        Assert.Equal(new Version(major, minor, build), v);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryParseTag_ReturnsNullForEmptyOrNull(string? tag)
    {
        Assert.Null(VersionParser.TryParseTag(tag));
    }

    [Theory]
    [InlineData("not-a-version")]
    [InlineData("v")]
    [InlineData("vfoo")]
    [InlineData("1")]
    [InlineData("vfoo.bar.baz")]
    public void TryParseTag_ReturnsNullForGarbage(string tag)
    {
        Assert.Null(VersionParser.TryParseTag(tag));
    }

    [Fact]
    public void TryParseTag_HandlesFourPartVersion()
    {
        // System.Version supports up to 4 parts.
        var v = VersionParser.TryParseTag("v1.2.3.4");
        Assert.Equal(new Version(1, 2, 3, 4), v);
    }
}

using VerseStrings.Core;
using Xunit;

namespace VerseStrings.Core.Tests;

public class StartupArgsTests
{
    [Theory]
    [InlineData("StarStrings")]
    [InlineData("ScCompLangPack")]
    [InlineData("ScCompLangPackRemix")]
    [InlineData("ScCompLangPackRemix2")]
    public void Parse_RecognizesAllKnownPacks(string id)
    {
        var args = new[] { $"--pack={id}" };
        var parsed = StartupArgs.Parse(args);

        Assert.Equal(id, parsed.PackHint);
        Assert.False(parsed.IsStandalone);
    }

    [Fact]
    public void Parse_FindsFlagAmongOtherArgs()
    {
        // Why: real argv can include unrelated args (the WPF runtime
        // sometimes adds its own). The flag should still be picked up.
        var args = new[] { "--something-else", "--pack=ScCompLangPack", "/other" };
        var parsed = StartupArgs.Parse(args);

        Assert.Equal("ScCompLangPack", parsed.PackHint);
        Assert.False(parsed.IsStandalone);
    }

    [Theory]
    [InlineData("--pack=BogusPack")]
    [InlineData("--pack=")]
    [InlineData("--pack=starstrings")] // case-sensitive: ids are exact
    public void Parse_RejectsUnknownOrEmptyPackValue(string arg)
    {
        var parsed = StartupArgs.Parse(new[] { arg });
        Assert.Null(parsed.PackHint);
    }

    [Fact]
    public void Parse_RecognizesStandaloneFlag()
    {
        var parsed = StartupArgs.Parse(new[] { "--standalone" });

        Assert.True(parsed.IsStandalone);
        Assert.Null(parsed.PackHint);
    }

    [Fact]
    public void Parse_HandlesStandaloneAndPackTogether()
    {
        // Why: the installer's [Run] line for a standalone-only install
        // passes both flags so the first-run wizard pre-selects the pack
        // AND opens in standalone mode.
        var parsed = StartupArgs.Parse(new[] { "--standalone", "--pack=ScCompLangPackRemix" });

        Assert.True(parsed.IsStandalone);
        Assert.Equal("ScCompLangPackRemix", parsed.PackHint);
    }

    [Fact]
    public void Parse_OrderIndependent()
    {
        var a = StartupArgs.Parse(new[] { "--pack=StarStrings", "--standalone" });
        var b = StartupArgs.Parse(new[] { "--standalone", "--pack=StarStrings" });

        Assert.Equal(a, b);
    }

    [Theory]
    [InlineData("--Standalone")]   // capital S
    [InlineData("--STANDALONE")]
    [InlineData("/standalone")]    // forward-slash convention
    public void Parse_StandaloneFlag_CaseSensitive(string variant)
    {
        // Why: the installer and shortcuts always emit the canonical
        // "--standalone". Accepting variants would invite ambiguity if
        // we later introduced --standalone-something.
        var parsed = StartupArgs.Parse(new[] { variant });
        Assert.False(parsed.IsStandalone);
    }

    [Fact]
    public void Parse_EmptyArgs_BothDefaults()
    {
        var parsed = StartupArgs.Parse(Array.Empty<string>());

        Assert.False(parsed.IsStandalone);
        Assert.Null(parsed.PackHint);
    }

    [Fact]
    public void Parse_UnknownFlagsOnly_BothDefaults()
    {
        var parsed = StartupArgs.Parse(new[] { "--other", "/foo", "bar" });

        Assert.False(parsed.IsStandalone);
        Assert.Null(parsed.PackHint);
    }
}

using VerseStrings.Core;
using Xunit;

namespace VerseStrings.Core.Tests;

public class InstallerArgsTests
{
    [Theory]
    [InlineData("StarStrings")]
    [InlineData("ScCompLangPack")]
    [InlineData("ScCompLangPackRemix")]
    [InlineData("ScCompLangPackRemix2")]
    public void TryGetPackId_RecognizesAllKnownPacks(string id)
    {
        var args = new[] { $"--pack={id}" };
        Assert.Equal(id, InstallerArgs.TryGetPackId(args));
    }

    [Fact]
    public void TryGetPackId_FindsFlagAmongOtherArgs()
    {
        // Why: real argv can include unrelated args (the WPF runtime
        // sometimes adds its own). The flag should still be picked up.
        var args = new[] { "--something-else", "--pack=ScCompLangPack", "/other" };
        Assert.Equal("ScCompLangPack", InstallerArgs.TryGetPackId(args));
    }

    [Theory]
    [InlineData("--pack=BogusPack")]
    [InlineData("--pack=")]
    [InlineData("--pack=starstrings")] // case-sensitive: ids are exact
    public void TryGetPackId_RejectsUnknownOrEmptyValue(string arg)
    {
        Assert.Null(InstallerArgs.TryGetPackId(new[] { arg }));
    }

    [Fact]
    public void TryGetPackId_ReturnsNullWhenFlagMissing()
    {
        var args = new[] { "--other", "/foo" };
        Assert.Null(InstallerArgs.TryGetPackId(args));
    }

    [Fact]
    public void TryGetPackId_ReturnsNullForEmptyArgs()
    {
        Assert.Null(InstallerArgs.TryGetPackId(Array.Empty<string>()));
    }
}

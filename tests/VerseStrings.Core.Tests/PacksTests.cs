using VerseStrings.Core;
using Xunit;

namespace VerseStrings.Core.Tests;

public class PacksTests
{
    [Theory]
    [InlineData("StarStrings")]
    [InlineData("ScCompLangPack")]
    [InlineData("ScCompLangPackRemix")]
    [InlineData("ScCompLangPackRemix2")]
    public void ById_RoundTripsKnownPackIds(string id)
    {
        var pack = Packs.ById(id);

        Assert.NotNull(pack);
        Assert.Equal(id, pack!.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("UnknownPack")]
    [InlineData("starstrings")] // case-sensitive by design — settings stores exact Id
    public void ById_ReturnsNullForUnknownOrMissing(string? id)
    {
        Assert.Null(Packs.ById(id));
    }

    [Fact]
    public void Default_ResolvesToStarStrings()
    {
        Assert.Equal("StarStrings", Packs.Default.Id);
    }

    [Fact]
    public void All_ContainsFourPacks()
    {
        // Why pin the count: adding a 5th pack should be a deliberate decision
        // surfaced by this test failing, not a silent expansion of the menu.
        Assert.Equal(4, Packs.All.Count);
    }

    [Fact]
    public void Label_CombinesNameAndAuthor()
    {
        // Why: three packs start with "ScCompLang..." — UI labels need the
        // author to be distinguishable at a glance.
        Assert.Equal("StarStrings (MrKraken)", Packs.ById("StarStrings")!.Label);
        Assert.Equal("ScCompLangPackRemix (BeltaKoda)", Packs.ById("ScCompLangPackRemix")!.Label);
    }

    [Fact]
    public void All_HaveNonEmptyAuthors()
    {
        Assert.All(Packs.All, p => Assert.False(string.IsNullOrWhiteSpace(p.Author)));
    }
}

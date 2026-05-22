using VerseStrings.Core;
using Xunit;

namespace VerseStrings.Core.Tests;

public class InstallerScopeTests
{
    [Theory]
    [InlineData("user.cfg")]
    [InlineData("USER.cfg")]
    [InlineData("User.Cfg")]
    public void TopLevelUserCfg_Installs(string path)
    {
        Assert.True(Installer.ShouldInstall(path));
    }

    [Theory]
    [InlineData("data/Localization/english/global.ini")]
    [InlineData("Data/Localization/english/global.ini")]
    [InlineData("data\\Localization\\english\\global.ini")]
    [InlineData("data/Localization/english/english.lnk")]
    [InlineData("data/anything/under/here.bin")]
    public void AnythingUnderTopLevelData_Installs(string path)
    {
        // Why both separators: Path.GetRelativePath returns OS-native
        // separators (backslash on Windows). The check must handle either.
        Assert.True(Installer.ShouldInstall(path));
    }

    [Theory]
    [InlineData("readme.md")]                  // StarStrings ships this at zip root
    [InlineData("README.md")]
    [InlineData("LICENSE")]
    [InlineData("docs/something.md")]
    [InlineData("merge-process/internal.txt")] // some upstream repos contain workshop folders
    public void TopLevelOtherFiles_Skipped(string path)
    {
        // Why: our README promises "Two paths under StarCitizen\LIVE\,
        // nothing else." Extra files at the zip root would otherwise be
        // copied into the LIVE folder and pollute it.
        Assert.False(Installer.ShouldInstall(path));
    }

    [Theory]
    [InlineData("")]
    [InlineData("data")]    // a "data" file (no extension) at root, not a directory
    public void EdgeCases_Skipped(string path)
    {
        Assert.False(Installer.ShouldInstall(path));
    }
}

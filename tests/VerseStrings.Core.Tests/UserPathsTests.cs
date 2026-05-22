using VerseStrings.Core;
using Xunit;

namespace VerseStrings.Core.Tests;

public class UserPathsTests
{
    [Fact]
    public void AppDataDir_EndsWithBrand()
    {
        // Why: prefix is environment-dependent (C:\Users\<name>\AppData\Roaming
        // on Windows, ~/.config on Linux test runners). The brand-derived
        // suffix is the part our code controls and the part that matters.
        Assert.EndsWith(Branding.AppName, UserPaths.AppDataDir);
    }

    [Fact]
    public void SettingsFile_LandsInsideAppDataDir()
    {
        Assert.StartsWith(UserPaths.AppDataDir, UserPaths.SettingsFile);
        Assert.EndsWith(Path.Combine(Branding.AppName, "settings.json"), UserPaths.SettingsFile);
    }

    [Fact]
    public void BackupsRoot_LandsInsideAppDataDir()
    {
        Assert.StartsWith(UserPaths.AppDataDir, UserPaths.BackupsRoot);
        Assert.EndsWith(Path.Combine(Branding.AppName, "backups"), UserPaths.BackupsRoot);
    }
}

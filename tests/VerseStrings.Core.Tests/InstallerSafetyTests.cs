using VerseStrings.Core;
using Xunit;

namespace VerseStrings.Core.Tests;

public class InstallerSafetyTests
{
    [Theory]
    [InlineData("user.cfg")]
    [InlineData("data/Localization/english/global.ini")]
    [InlineData("nested/inner/file.txt")]
    public void TryResolveSafeEntryPath_AcceptsEntriesUnderRoot(string entry)
    {
        var root = Path.Combine(Path.GetTempPath(), "verse-zip-safe");
        var ok = Installer.TryResolveSafeEntryPath(root, entry, out var resolved);

        Assert.True(ok);
        Assert.StartsWith(Path.GetFullPath(root), resolved, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("../../escape.txt")]
    [InlineData("data/../../escape.txt")]
    public void TryResolveSafeEntryPath_RejectsEntriesThatEscapeRoot(string entry)
    {
        // Why: zip-slip. A malicious or malformed release could write outside
        // the extraction directory; the install would otherwise touch arbitrary
        // paths under the user's LIVE folder (or beyond it).
        var root = Path.Combine(Path.GetTempPath(), "verse-zip-safe");
        var ok = Installer.TryResolveSafeEntryPath(root, entry, out var resolved);

        Assert.False(ok);
        Assert.Equal(string.Empty, resolved);
    }
}

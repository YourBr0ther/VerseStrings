using VerseStrings.Core;
using Xunit;

namespace VerseStrings.Core.Tests;

public class UserCfgMergerTests
{
    [Fact]
    public void NoExistingFile_ReturnsIncomingWithLanguageLine()
    {
        var incoming = "g_language = english\n";
        var result = UserCfgMerger.Merge(existingContent: null, incomingContent: incoming);
        Assert.Contains("g_language = english", result);
    }

    [Fact]
    public void EmptyExistingFile_ReturnsIncomingWithLanguageLine()
    {
        var result = UserCfgMerger.Merge(existingContent: "", incomingContent: "g_language = english");
        Assert.Contains("g_language = english", result);
    }

    [Fact]
    public void ExistingFileWithoutLanguageLine_AppendsLine()
    {
        var existing = "r_DisplayInfo = 3\nsys_MaxFPS = 144\n";
        var result = UserCfgMerger.Merge(existing, "g_language = english");

        Assert.Contains("r_DisplayInfo = 3", result);
        Assert.Contains("sys_MaxFPS = 144", result);
        Assert.Contains("g_language = english", result);
    }

    [Fact]
    public void ExistingFileWithLanguageLine_LeavesFileUnchanged()
    {
        var existing = "g_language = english\nr_DisplayInfo = 3\n";
        var result = UserCfgMerger.Merge(existing, "g_language = english");
        Assert.Equal(existing, result);
    }

    [Fact]
    public void ExistingFileWithDifferentLanguage_LeavesFileUnchanged()
    {
        // Why: if the user explicitly set a different language, we respect that choice.
        // They opted into a localization pack; if it doesn't display, the in-app diagnostic
        // surfaces the language mismatch rather than us overwriting their preference.
        var existing = "g_language = german\n";
        var result = UserCfgMerger.Merge(existing, "g_language = english");
        Assert.Equal(existing, result);
    }

    [Fact]
    public void CommentedOutLanguageLine_IsNotConsideredPresent()
    {
        var existing = "-- g_language = english\nr_DisplayInfo = 3\n";
        var result = UserCfgMerger.Merge(existing, "g_language = english");
        Assert.Contains("-- g_language = english", result);
        Assert.Contains("r_DisplayInfo = 3", result);

        var lines = result.Split('\n');
        var activeLanguageLines = lines.Count(l =>
            !l.TrimStart().StartsWith("--") &&
            l.Contains("g_language"));
        Assert.Equal(1, activeLanguageLines);
    }

    [Fact]
    public void ExistingFileWithWhitespaceAroundEquals_IsDetected()
    {
        var existing = "  g_language   =   english  \n";
        var result = UserCfgMerger.Merge(existing, "g_language = english");
        Assert.Equal(existing, result);
    }

    [Fact]
    public void AppendedLineSeparatedFromExistingContent()
    {
        var existing = "sys_MaxFPS = 144";
        var result = UserCfgMerger.Merge(existing, "g_language = english");

        Assert.Contains("sys_MaxFPS = 144", result);
        Assert.Contains("g_language = english", result);
        var lines = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
    }
}

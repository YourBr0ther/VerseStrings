namespace VerseStrings.Core;

public static class UserCfgMerger
{
    private const string LanguageKey = "g_language";
    private const string DesiredLine = "g_language = english";

    /// <summary>
    /// Merges the incoming user.cfg content into the existing one without overwriting unrelated settings.
    ///
    /// Why: localization packs explicitly instruct users not to overwrite an existing user.cfg.
    /// Only ensure that g_language = english is present, since that's the one setting required.
    /// </summary>
    public static string Merge(string? existingContent, string incomingContent)
    {
        if (string.IsNullOrWhiteSpace(existingContent))
            return EnsureTrailingNewline(NormalizeIncoming(incomingContent));

        var lines = SplitLines(existingContent);
        var hasLanguageLine = lines.Any(IsLanguageLine);

        if (hasLanguageLine)
            return existingContent;

        var trimmed = existingContent.TrimEnd('\r', '\n');
        return trimmed + Environment.NewLine + DesiredLine + Environment.NewLine;
    }

    private static string NormalizeIncoming(string content)
    {
        var lines = SplitLines(content);
        if (!lines.Any(IsLanguageLine))
            lines.Add(DesiredLine);
        return string.Join(Environment.NewLine, lines);
    }

    private static string EnsureTrailingNewline(string content) =>
        content.EndsWith(Environment.NewLine, StringComparison.Ordinal)
            ? content
            : content + Environment.NewLine;

    private static List<string> SplitLines(string content) =>
        content.Replace("\r\n", "\n").Split('\n').ToList();

    private static bool IsLanguageLine(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("--", StringComparison.Ordinal) ||
            trimmed.StartsWith(";", StringComparison.Ordinal) ||
            trimmed.StartsWith("#", StringComparison.Ordinal))
            return false;

        var equalsIdx = trimmed.IndexOf('=');
        if (equalsIdx < 0) return false;

        var key = trimmed[..equalsIdx].Trim();
        return string.Equals(key, LanguageKey, StringComparison.OrdinalIgnoreCase);
    }
}

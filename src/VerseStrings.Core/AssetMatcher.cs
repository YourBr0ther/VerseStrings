using System.Text.RegularExpressions;

namespace VerseStrings.Core;

public static class AssetMatcher
{
    private static readonly char[] RegexMetacharacters =
        ['^', '$', '.', '*', '+', '?', '(', ')', '[', ']', '{', '}', '|', '\\'];

    /// <summary>
    /// Returns the first name in <paramref name="names"/> that matches
    /// <paramref name="pattern"/>. If the pattern contains regex metacharacters
    /// it's compiled as a regex; otherwise it's a case-insensitive exact match.
    /// Returns null if no match is found or the pattern is unusable.
    /// </summary>
    public static string? SelectFirst(IEnumerable<string> names, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return null;

        if (LooksLikeRegex(pattern))
        {
            Regex regex;
            try { regex = new Regex(pattern, RegexOptions.IgnoreCase); }
            catch (ArgumentException) { return null; }

            return names.FirstOrDefault(n => n is not null && regex.IsMatch(n));
        }

        return names.FirstOrDefault(n =>
            string.Equals(n, pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksLikeRegex(string pattern) =>
        pattern.IndexOfAny(RegexMetacharacters) >= 0;
}

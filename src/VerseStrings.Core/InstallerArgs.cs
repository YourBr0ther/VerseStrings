namespace VerseStrings.Core;

public static class InstallerArgs
{
    private const string PackFlag = "--pack=";

    /// <summary>
    /// Pulls the pack-id hint the installer's <c>[Run]</c> line passes on
    /// first launch (<c>--pack=&lt;id&gt;</c>). Returns the id only if it
    /// resolves to a known pack; <c>null</c> for missing, unknown, or empty.
    /// Public so it can be unit-tested without the full App startup path.
    /// </summary>
    public static string? TryGetPackId(string[] args)
    {
        foreach (var arg in args)
        {
            if (!arg.StartsWith(PackFlag, StringComparison.Ordinal)) continue;
            var value = arg[PackFlag.Length..];
            return Packs.ById(value) is not null ? value : null;
        }
        return null;
    }
}

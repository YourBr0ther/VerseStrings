namespace VerseStrings.Core;

/// <summary>
/// Parsed command-line flags VerseStrings.exe accepts at startup. Sources of
/// these args:
///   - The installer's [Run] line on first launch (passes --pack=&lt;id&gt; and,
///     when the user installed standalone-only, --standalone).
///   - Start-menu shortcuts created by the installer (the standalone shortcut
///     gets --standalone; the tray shortcut has no args).
///   - Direct invocation from a shell.
/// </summary>
public sealed record StartupArgs(bool IsStandalone, string? PackHint)
{
    private const string PackFlag = "--pack=";
    private const string StandaloneFlag = "--standalone";

    /// <summary>
    /// Parses startup args. Unknown flags are ignored — the args list may
    /// contain runtime-added entries from WPF or the OS. <paramref name="args"/>
    /// is typically <c>StartupEventArgs.Args</c>.
    ///
    /// <para>Pack-hint validation: an unknown pack id silently becomes null
    /// (App.OnStartup falls back to the existing pack hint flow). The
    /// <see cref="Packs"/> catalog is consulted at parse time.</para>
    /// </summary>
    public static StartupArgs Parse(string[] args)
    {
        var isStandalone = false;
        string? packHint = null;

        foreach (var arg in args)
        {
            if (string.Equals(arg, StandaloneFlag, StringComparison.Ordinal))
            {
                isStandalone = true;
                continue;
            }
            if (arg.StartsWith(PackFlag, StringComparison.Ordinal))
            {
                var value = arg[PackFlag.Length..];
                if (Packs.ById(value) is not null) packHint = value;
            }
        }

        return new StartupArgs(isStandalone, packHint);
    }
}

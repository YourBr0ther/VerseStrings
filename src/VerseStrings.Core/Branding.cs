namespace VerseStrings.Core;

public static class Branding
{
    /// <summary>
    /// The single source of truth for the brand identifier used in user-facing
    /// filesystem paths (%APPDATA%\VerseStrings, %LOCALAPPDATA%\Programs\VerseStrings),
    /// the autostart Run-key value, and the single-instance mutex name.
    ///
    /// Why this exists: changing the brand name has migration consequences —
    /// settings and backups under the previous name would be orphaned. Forcing
    /// every consumer through this constant makes the blast radius of a rename
    /// visible from a single grep.
    /// </summary>
    public const string AppName = "VerseStrings";
}

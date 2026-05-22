namespace VerseStrings.Core;

/// <summary>
/// Brand-derived filesystem locations under <c>%APPDATA%</c>. The single
/// source of truth for where VerseStrings stores its persistent data, so
/// changing the layout (or the brand) is a one-place edit instead of a
/// repo-wide grep.
/// </summary>
public static class UserPaths
{
    /// <summary><c>%APPDATA%\VerseStrings\</c></summary>
    public static string AppDataDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Branding.AppName);

    /// <summary><c>%APPDATA%\VerseStrings\settings.json</c></summary>
    public static string SettingsFile => Path.Combine(AppDataDir, "settings.json");

    /// <summary><c>%APPDATA%\VerseStrings\backups\</c></summary>
    public static string BackupsRoot => Path.Combine(AppDataDir, "backups");
}

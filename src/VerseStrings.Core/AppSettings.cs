namespace VerseStrings.Core;

public sealed class AppSettings
{
    public string? LiveFolderPath { get; set; }
    public string? LastAppliedSha256 { get; set; }
    public string? LastAppliedReleaseName { get; set; }
    public DateTimeOffset? LastAppliedAt { get; set; }
    public int CheckIntervalMinutes { get; set; } = 15;
    public bool AutostartEnabled { get; set; }
    public bool FirstRunCompleted { get; set; }

    /// <summary>
    /// Stable ID of the selected pack (see <see cref="Packs.All"/>).
    /// Defaults to <see cref="Packs.DefaultId"/> for fresh installs;
    /// for upgrades from &lt;= v0.1.4 the value is inferred from
    /// the legacy <see cref="Repo"/> field by <c>SettingsStore.Load</c>.
    /// </summary>
    public string SelectedPackId { get; set; } = Packs.DefaultId;

    /// <summary>
    /// Legacy field from v0.1.4 and earlier. Read once by SettingsStore.Load
    /// to migrate older settings.json files onto <see cref="SelectedPackId"/>,
    /// then cleared on Save. Will stay in the type until the v0.1.4-direct-
    /// upgrade window has plausibly closed; remove in a future release.
    /// </summary>
    public string? Repo { get; set; }
}

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

    /// <summary>Stable ID of the selected pack (see <see cref="Packs.All"/>).</summary>
    public string SelectedPackId { get; set; } = Packs.DefaultId;
}

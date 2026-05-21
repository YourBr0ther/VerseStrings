namespace VerseStrings.Core;

public sealed class AppSettings
{
    public string? LiveFolderPath { get; set; }
    public string? LastAppliedSha256 { get; set; }
    public string? LastAppliedReleaseName { get; set; }
    public DateTimeOffset? LastAppliedAt { get; set; }
    public DateTimeOffset? LastCheckAt { get; set; }
    public int CheckIntervalMinutes { get; set; } = 15;
    public bool AutostartEnabled { get; set; }
    public bool FirstRunCompleted { get; set; }

    public string Repo { get; set; } = "MrKraken/StarStrings";
}

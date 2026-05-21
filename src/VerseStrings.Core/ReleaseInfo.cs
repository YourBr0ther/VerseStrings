namespace VerseStrings.Core;

public sealed record ReleaseInfo(
    string TagName,
    string Name,
    DateTimeOffset PublishedAt,
    string AssetName,
    string AssetDownloadUrl,
    long AssetSizeBytes,
    string? AssetSha256
);

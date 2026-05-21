namespace VerseStrings.Core;

public sealed record ReleaseInfo(
    string TagName,
    string Name,
    string AssetName,
    string AssetDownloadUrl,
    string AssetSha256
);

namespace VerseStrings.Core;

public sealed record InstallResult(
    string ReleaseName,
    string Sha256,
    int FilesInstalled
);

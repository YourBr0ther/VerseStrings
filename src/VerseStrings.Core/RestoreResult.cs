namespace VerseStrings.Core;

public sealed record RestoreResult(
    int FilesRestored,
    IReadOnlyList<string> FailedFiles
);

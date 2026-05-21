using System.IO.Compression;
using System.Security.Cryptography;

namespace VerseStrings.Core;

public sealed class Installer
{
    private readonly GithubReleaseClient _github;
    private readonly string _backupsRoot;

    public Installer(GithubReleaseClient github, string backupsRoot)
    {
        _github = github;
        _backupsRoot = backupsRoot;
    }

    public async Task<InstallResult> InstallAsync(
        ReleaseInfo release,
        string liveFolderPath,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(liveFolderPath))
            throw new DirectoryNotFoundException($"LIVE folder not found: {liveFolderPath}");

        var tempRoot = Path.Combine(Path.GetTempPath(), $"versestrings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var zipPath = Path.Combine(tempRoot, release.AssetName);
        var extractDir = Path.Combine(tempRoot, "extracted");

        try
        {
            await _github.DownloadAssetAsync(release, zipPath, ct);

            var actualSha = await ComputeSha256Async(zipPath, ct);
            if (!string.Equals(actualSha, release.AssetSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"SHA-256 mismatch on downloaded asset. Expected {release.AssetSha256}, got {actualSha}.");
            }

            SafeExtract(zipPath, extractDir);

            var backupDir = Path.Combine(
                _backupsRoot,
                DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss") + "_" + Sanitize(release.TagName));
            Directory.CreateDirectory(backupDir);

            BackupExistingFiles(liveFolderPath, backupDir);
            var filesWritten = ApplyExtracted(extractDir, liveFolderPath);

            return new InstallResult(
                ReleaseName: release.Name,
                Sha256: actualSha,
                FilesInstalled: filesWritten);
        }
        finally
        {
            TryDelete(tempRoot);
        }
    }

    public void RestoreBackup(string backupFolderPath, string liveFolderPath)
    {
        if (!Directory.Exists(backupFolderPath))
            throw new DirectoryNotFoundException($"Backup folder not found: {backupFolderPath}");

        foreach (var file in Directory.EnumerateFiles(backupFolderPath, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(backupFolderPath, file);
            var dest = Path.Combine(liveFolderPath, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }

    public string? FindMostRecentBackup()
    {
        if (!Directory.Exists(_backupsRoot)) return null;
        return Directory.EnumerateDirectories(_backupsRoot)
            .OrderByDescending(d => d, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static void BackupExistingFiles(string liveFolderPath, string backupDir)
    {
        var existingUserCfg = Path.Combine(liveFolderPath, "user.cfg");
        if (File.Exists(existingUserCfg))
        {
            File.Copy(existingUserCfg, Path.Combine(backupDir, "user.cfg"), overwrite: true);
        }

        var existingLocalization = Path.Combine(liveFolderPath, "data", "Localization");
        if (Directory.Exists(existingLocalization))
        {
            var backupLocalization = Path.Combine(backupDir, "data", "Localization");
            CopyDirectory(existingLocalization, backupLocalization);
        }
    }

    private static int ApplyExtracted(string extractRoot, string liveFolderPath)
    {
        var sourceRoot = ResolveSourceRoot(extractRoot);
        var filesWritten = 0;

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceRoot, file);
            var dest = Path.Combine(liveFolderPath, rel);

            if (string.Equals(Path.GetFileName(rel), "user.cfg", StringComparison.OrdinalIgnoreCase))
            {
                var incoming = File.ReadAllText(file);
                var existing = File.Exists(dest) ? File.ReadAllText(dest) : null;
                var merged = UserCfgMerger.Merge(existing, incoming);
                File.WriteAllText(dest, merged);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(file, dest, overwrite: true);
            }
            filesWritten++;
        }

        return filesWritten;
    }

    private static void SafeExtract(string zipPath, string extractDir)
    {
        Directory.CreateDirectory(extractDir);
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (!TryResolveSafeEntryPath(extractDir, entry.FullName, out var dest))
                throw new InvalidDataException(
                    $"Refusing to extract zip entry that escapes the destination: {entry.FullName}");

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(dest);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            entry.ExtractToFile(dest, overwrite: true);
        }
    }

    /// <summary>
    /// Resolves a zip entry's path against an extraction root and rejects
    /// anything that would land outside it (zip-slip). Public for testing —
    /// the production caller is <see cref="SafeExtract"/>.
    /// </summary>
    public static bool TryResolveSafeEntryPath(string extractRoot, string entryName, out string resolvedPath)
    {
        var rootFull = Path.GetFullPath(extractRoot);
        var rootWithSep = rootFull.EndsWith(Path.DirectorySeparatorChar)
            ? rootFull
            : rootFull + Path.DirectorySeparatorChar;

        var candidate = Path.GetFullPath(Path.Combine(rootFull, entryName));
        if (candidate.Equals(rootFull, StringComparison.Ordinal) ||
            candidate.StartsWith(rootWithSep, StringComparison.Ordinal))
        {
            resolvedPath = candidate;
            return true;
        }

        resolvedPath = string.Empty;
        return false;
    }

    private static string ResolveSourceRoot(string extractRoot)
    {
        if (File.Exists(Path.Combine(extractRoot, "user.cfg")) ||
            Directory.Exists(Path.Combine(extractRoot, "data")))
            return extractRoot;

        var subdirs = Directory.GetDirectories(extractRoot);
        if (subdirs.Length == 1) return subdirs[0];

        return extractRoot;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, file);
            var dest = Path.Combine(destination, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Sanitize(string s)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(s.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    private static void TryDelete(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch { /* best effort cleanup */ }
    }
}

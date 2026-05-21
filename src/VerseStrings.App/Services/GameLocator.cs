using System.Text.Json;

namespace VerseStrings.Services;

public static class GameLocator
{
    public static string? TryDetectLiveFolder()
    {
        var candidates = new[]
        {
            FromRsiLauncherConfig(),
            FromCommonInstallPaths(),
        };

        foreach (var c in candidates)
        {
            if (!string.IsNullOrWhiteSpace(c) && LooksLikeLiveFolder(c))
                return c;
        }
        return null;
    }

    public static bool LooksLikeLiveFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return false;
        var binExe = Path.Combine(path, "Bin64", "StarCitizen.exe");
        return File.Exists(binExe);
    }

    private static string? FromRsiLauncherConfig()
    {
        try
        {
            var launcherDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "rsilauncher");
            if (!Directory.Exists(launcherDir)) return null;

            foreach (var jsonFile in Directory.EnumerateFiles(launcherDir, "*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(jsonFile));
                    var path = ScanForLivePath(doc.RootElement);
                    if (path is not null) return path;
                }
                catch { /* skip malformed files */ }
            }
        }
        catch { /* best effort */ }

        return null;
    }

    private static string? ScanForLivePath(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var nested = ScanForLivePath(prop.Value);
                    if (nested is not null) return nested;
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var nested = ScanForLivePath(item);
                    if (nested is not null) return nested;
                }
                break;
            case JsonValueKind.String:
                var s = element.GetString();
                if (!string.IsNullOrWhiteSpace(s) && s.Contains("LIVE", StringComparison.OrdinalIgnoreCase) &&
                    LooksLikeLiveFolder(s))
                    return s;
                break;
        }
        return null;
    }

    private static string? FromCommonInstallPaths()
    {
        var commonRoots = new[]
        {
            @"C:\Program Files\Roberts Space Industries\StarCitizen\LIVE",
            @"D:\Program Files\Roberts Space Industries\StarCitizen\LIVE",
            @"C:\Roberts Space Industries\StarCitizen\LIVE",
        };

        foreach (var path in commonRoots)
        {
            if (LooksLikeLiveFolder(path)) return path;
        }
        return null;
    }
}

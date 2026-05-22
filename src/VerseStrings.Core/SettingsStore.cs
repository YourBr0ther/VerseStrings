using System.Text.Json;

namespace VerseStrings.Core;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _path;

    public SettingsStore(string settingsFilePath)
    {
        _path = settingsFilePath;
    }

    public static SettingsStore Default()
    {
        Directory.CreateDirectory(UserPaths.AppDataDir);
        return new SettingsStore(UserPaths.SettingsFile);
    }

    public AppSettings Load()
    {
        if (!File.Exists(_path))
            return new AppSettings();

        var json = File.ReadAllText(_path);
        if (string.IsNullOrWhiteSpace(json))
            return new AppSettings();

        return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_path, json);
    }
}

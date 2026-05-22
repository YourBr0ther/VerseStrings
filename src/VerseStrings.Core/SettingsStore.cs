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
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Branding.AppName);
        Directory.CreateDirectory(dir);
        return new SettingsStore(Path.Combine(dir, "settings.json"));
    }

    public AppSettings Load()
    {
        var settings = LoadRaw();
        MigrateLegacyRepoToPackId(settings);
        return settings;
    }

    private AppSettings LoadRaw()
    {
        if (!File.Exists(_path))
            return new AppSettings();

        var json = File.ReadAllText(_path);
        if (string.IsNullOrWhiteSpace(json))
            return new AppSettings();

        return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
    }

    private static void MigrateLegacyRepoToPackId(AppSettings settings)
    {
        // Why: pre-v0.1.5 settings.json had no SelectedPackId — the watcher
        // ran off Repo. The JSON-deserialised default for SelectedPackId is
        // "StarStrings", but if a v0.1.4 user had pointed Repo at one of the
        // other known sources we map it through so they land on the right
        // pack post-upgrade instead of silently being switched back.
        if (string.IsNullOrEmpty(settings.Repo)) return;
        if (settings.SelectedPackId != Packs.DefaultId) return; // already set explicitly

        settings.SelectedPackId = settings.Repo switch
        {
            "MrKraken/StarStrings"          => "StarStrings",
            "ExoAE/ScCompLangPack"          => "ScCompLangPack",
            "BeltaKoda/ScCompLangPackRemix" => "ScCompLangPackRemix",
            _                               => Packs.DefaultId,
        };
    }

    public void Save(AppSettings settings)
    {
        // Why null Repo on save: SelectedPackId is now canonical. Keeping the
        // legacy field populated after migration would let a subsequent load
        // re-migrate and overwrite an explicit user switch. Slated for
        // removal entirely in v0.1.6.
        settings.Repo = null;
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_path, json);
    }
}

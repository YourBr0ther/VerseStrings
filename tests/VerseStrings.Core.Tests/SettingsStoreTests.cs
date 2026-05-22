using Xunit;

namespace VerseStrings.Core.Tests;

public class SettingsStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _settingsPath;
    private readonly SettingsStore _store;

    public SettingsStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"verse-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _settingsPath = Path.Combine(_tempDir, "settings.json");
        _store = new SettingsStore(_settingsPath);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void Load_WhenFileMissing_ReturnsDefaults()
    {
        var settings = _store.Load();

        Assert.Null(settings.LiveFolderPath);
        Assert.Null(settings.LastAppliedSha256);
        Assert.Equal(15, settings.CheckIntervalMinutes);
        Assert.False(settings.AutostartEnabled);
        Assert.False(settings.FirstRunCompleted);
        Assert.Equal("StarStrings", settings.SelectedPackId);
        Assert.Null(settings.Repo); // legacy field is null on fresh installs
    }

    [Fact]
    public void Load_WhenFileEmpty_ReturnsDefaults()
    {
        File.WriteAllText(_settingsPath, "");

        var settings = _store.Load();

        Assert.Equal(15, settings.CheckIntervalMinutes);
    }

    [Fact]
    public void Load_WhenFileWhitespace_ReturnsDefaults()
    {
        File.WriteAllText(_settingsPath, "   \n\t  ");

        var settings = _store.Load();

        Assert.Equal(15, settings.CheckIntervalMinutes);
    }

    [Fact]
    public void SaveLoad_RoundTripsAllFields()
    {
        var original = new AppSettings
        {
            LiveFolderPath = @"C:\Games\StarCitizen\LIVE",
            LastAppliedSha256 = "abc123",
            LastAppliedReleaseName = "Hotfix 11875",
            LastAppliedAt = new DateTimeOffset(2026, 5, 21, 14, 30, 0, TimeSpan.Zero),
            CheckIntervalMinutes = 30,
            AutostartEnabled = true,
            FirstRunCompleted = true,
            SelectedPackId = "ScCompLangPackRemix2",
        };

        _store.Save(original);
        var loaded = _store.Load();

        Assert.Equal(original.LiveFolderPath, loaded.LiveFolderPath);
        Assert.Equal(original.LastAppliedSha256, loaded.LastAppliedSha256);
        Assert.Equal(original.LastAppliedReleaseName, loaded.LastAppliedReleaseName);
        Assert.Equal(original.LastAppliedAt, loaded.LastAppliedAt);
        Assert.Equal(original.CheckIntervalMinutes, loaded.CheckIntervalMinutes);
        Assert.Equal(original.AutostartEnabled, loaded.AutostartEnabled);
        Assert.Equal(original.FirstRunCompleted, loaded.FirstRunCompleted);
        Assert.Equal(original.SelectedPackId, loaded.SelectedPackId);
    }

    [Theory]
    [InlineData("MrKraken/StarStrings",         "StarStrings")]
    [InlineData("ExoAE/ScCompLangPack",         "ScCompLangPack")]
    [InlineData("BeltaKoda/ScCompLangPackRemix","ScCompLangPackRemix")]
    [InlineData("some/unknown",                 "StarStrings")]
    public void Load_MigratesLegacyRepoToSelectedPackId(string legacyRepo, string expectedPackId)
    {
        // Why: pre-v0.1.5 settings.json had no SelectedPackId. On upgrade we
        // need to land the user on the pack their Repo field was pointing at,
        // not silently snap everyone back to StarStrings.
        File.WriteAllText(_settingsPath, $$"""
            {
              "liveFolderPath": "C:\\Games\\SC\\LIVE",
              "checkIntervalMinutes": 15,
              "repo": "{{legacyRepo}}"
            }
            """);

        var settings = _store.Load();

        Assert.Equal(expectedPackId, settings.SelectedPackId);
    }

    [Fact]
    public void Save_ClearsLegacyRepoField()
    {
        // Why: keeping Repo populated after migration would let a subsequent
        // load re-migrate and overwrite an explicit user switch.
        var settings = new AppSettings { Repo = "ExoAE/ScCompLangPack" };
        _store.Save(settings);

        var json = File.ReadAllText(_settingsPath);

        Assert.Contains("\"repo\": null", json);
    }

    [Fact]
    public void Load_WhenJsonHasUnknownFields_IgnoresThem()
    {
        // Why: forward-compatibility. Older app versions must not throw when
        // a settings.json written by a newer version contains fields the older
        // version doesn't recognize.
        File.WriteAllText(_settingsPath, """
            {
              "liveFolderPath": "C:\\Games\\SC\\LIVE",
              "checkIntervalMinutes": 20,
              "futureFieldNotYetInvented": "ignore-me",
              "anotherUnknown": 42
            }
            """);

        var settings = _store.Load();

        Assert.Equal(@"C:\Games\SC\LIVE", settings.LiveFolderPath);
        Assert.Equal(20, settings.CheckIntervalMinutes);
    }

    [Fact]
    public void Load_WhenJsonMalformed_Throws()
    {
        // Why: we deliberately do NOT silently fall back to defaults when the
        // file is malformed — that would mask file corruption and silently
        // overwrite the user's real settings on the next save. Better to surface.
        File.WriteAllText(_settingsPath, "{ this is not json");

        Assert.Throws<System.Text.Json.JsonException>(() => _store.Load());
    }
}

namespace VerseStrings.Core;

/// <summary>
/// The closed set of community localization packs the app can install.
/// Hardcoded by design — adding a pack requires a code change and a release.
/// </summary>
public static class Packs
{
    public const string DefaultId = "StarStrings";

    public static readonly IReadOnlyList<Pack> All = new[]
    {
        new Pack(
            Id: "StarStrings",
            DisplayName: "StarStrings",
            Repo: "MrKraken/StarStrings",
            AssetPattern: "StarStrings.zip"),

        new Pack(
            Id: "ScCompLangPack",
            DisplayName: "ScCompLangPack",
            Repo: "ExoAE/ScCompLangPack",
            AssetPattern: "ScCompLangPack.zip"),

        new Pack(
            Id: "ScCompLangPackRemix",
            DisplayName: "ScCompLangPackRemix",
            Repo: "BeltaKoda/ScCompLangPackRemix",
            // Asset name is version-suffixed (e.g. ScCompLangPackRemix-4.8.0-LIVE.zip).
            // The LIVE anchor avoids picking up the parallel PTU release assets.
            AssetPattern: @"^ScCompLangPackRemix-.*-LIVE\.zip$"),

        new Pack(
            Id: "ScCompLangPackRemix2",
            DisplayName: "ScCompLangPackRemix2",
            Repo: "ExoAE/ScCompLangPack",
            AssetPattern: "ScCompLangPackRemix2.zip"),
    };

    public static Pack? ById(string? id) =>
        string.IsNullOrEmpty(id)
            ? null
            : All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.Ordinal));

    public static Pack Default => All.First(p => p.Id == DefaultId);
}

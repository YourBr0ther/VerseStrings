namespace VerseStrings.Core;

/// <summary>
/// A selectable community localization pack. Hardcoded set lives in
/// <see cref="Packs.All"/>. <paramref name="AssetPattern"/> is matched against
/// release asset names by <see cref="AssetMatcher.SelectFirst"/> — exact
/// (case-insensitive) name when the pattern has no regex metacharacters,
/// regex otherwise. <paramref name="Author"/> is the human/GitHub handle of
/// the upstream maintainer; surfaced next to <paramref name="DisplayName"/>
/// in UI lists so three near-identically-named packs are still distinguishable.
/// </summary>
public sealed record Pack(
    string Id,
    string DisplayName,
    string Author,
    string Repo,
    string AssetPattern
)
{
    /// <summary>"PackName (Author)" — the label shown in pickers and menus.</summary>
    public string Label => $"{DisplayName} ({Author})";
}

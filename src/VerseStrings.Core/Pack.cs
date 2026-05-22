namespace VerseStrings.Core;

/// <summary>
/// A selectable community localization pack. Hardcoded set lives in
/// <see cref="Packs.All"/>. <paramref name="AssetPattern"/> is matched against
/// release asset names by <see cref="AssetMatcher.SelectFirst"/> — exact
/// (case-insensitive) name when the pattern has no regex metacharacters,
/// regex otherwise.
/// </summary>
public sealed record Pack(
    string Id,
    string DisplayName,
    string Repo,
    string AssetPattern
);

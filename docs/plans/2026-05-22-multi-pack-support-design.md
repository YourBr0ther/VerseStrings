# Multi-pack support — design

**Status:** approved 2026-05-22
**Target release:** v0.1.5

## Problem

VerseStrings polls one hardcoded combination — `MrKraken/StarStrings` repo,
`StarStrings.zip` asset. The Star Citizen community actually maintains four
parallel localization packs with different component naming conventions:

| Id | Repo | Asset |
|---|---|---|
| `StarStrings` | `MrKraken/StarStrings` | `StarStrings.zip` |
| `ScCompLangPack` | `ExoAE/ScCompLangPack` | `ScCompLangPack.zip` |
| `ScCompLangPackRemix2` | `ExoAE/ScCompLangPack` | `ScCompLangPackRemix2.zip` |
| `ScCompLangPackRemix` | `BeltaKoda/ScCompLangPackRemix` | `ScCompLangPackRemix-{version}-LIVE.zip` |

Users want to (a) pick one at install time and (b) switch between them later
from the tray.

## Model

New `Pack` record in `VerseStrings.Core`:

```csharp
public sealed record Pack(
    string Id,            // stable identifier persisted in settings
    string DisplayName,   // shown in UI; same as Id today
    string Repo,          // owner/repo
    string AssetPattern   // exact name or regex
);
```

Static `Packs.All` holds the four. Hardcoded — adding a fifth is a code change
and release. No remote registry, no plugin system. Matches the project's
"clean and simple" bar.

`GithubReleaseClient.GetLatestAsync` swaps its `assetName` parameter for an
`assetPattern` and delegates selection to a new `AssetMatcher.SelectFirst`:

- Pattern with no regex metacharacters → case-insensitive exact name match
  (existing behavior for `StarStrings.zip` / `ScCompLangPack.zip` /
  `ScCompLangPackRemix2.zip`).
- Pattern with regex metacharacters → `Regex.IsMatch` over asset names. Used
  for `^ScCompLangPackRemix-.*-LIVE\.zip$`.

`AppSettings` gains:

```csharp
public string SelectedPackId { get; set; } = "StarStrings";
```

`Repo` is retained as a no-op shim for one release, removed in v0.1.6.

## Switching (tray submenu)

Tray menu gets a "Localization pack" submenu with four radio items, names only
(no editorial subtitles — see feedback memory). Clicking a different pack:

1. Persist new `SelectedPackId`.
2. Toast `"Switching to <PackName>"`.
3. Call `orchestrator.SwitchPackAsync`, which clears `LastAppliedSha256` and
   forces an immediate check.
4. Same install path as a regular update; backup behavior unchanged; game-
   running queue unchanged.
5. Toast on completion.

No confirmation dialog — **Restore previous version** already provides one-
click rollback.

All wired through the `SafeInvoke` helpers from v0.1.3.

## Installer dropdown

`installer/VerseStrings.iss` gains a `[Code]` section with a wizard page (after
License, before Ready) built via `CreateInputOptionPage` — four radio buttons.
The choice rides into the app via a command-line arg on the existing `[Run]`
launch line:

```
Parameters: "--pack={code:GetSelectedPackId}"
```

`App.OnStartup` parses `e.Args` for `--pack=<id>`. If present and `FirstRunCompleted == false`, sets
`settings.SelectedPackId` before the first-run wizard opens. The wizard always
shows (safety net for `/SILENT` installs and direct exe launches) but pre-selects
whatever the installer passed.

## Migration from v0.1.4

`SettingsStore.Load` post-processes:

```csharp
if (string.IsNullOrEmpty(settings.SelectedPackId))
    settings.SelectedPackId = settings.Repo switch
    {
        "MrKraken/StarStrings"           => "StarStrings",
        "ExoAE/ScCompLangPack"           => "ScCompLangPack",
        "BeltaKoda/ScCompLangPackRemix"  => "ScCompLangPackRemix",
        _                                => "StarStrings",
    };
```

Unknown IDs (manual edits) toast `"Unknown pack '<x>', falling back to
StarStrings"` and write the default back.

## Tests

Pure-logic only, in `VerseStrings.Core.Tests`:

- `PacksTests` — round-trip `Packs.ById` for all four IDs; unknown ID → null.
- `AssetMatcherTests` — exact pattern, case insensitivity, regex pattern,
  version-suffixed match, PTU asset rejection by Remix's LIVE-only pattern.

OS-touching paths (orchestrator switch, installer wizard, settings dialog
combobox) follow the existing "not worth mocking" policy.

## Files

**New:** `Pack.cs`, `Packs.cs`, `AssetMatcher.cs` (Core); `PacksTests.cs`,
`AssetMatcherTests.cs` (tests).

**Modified:** `AppSettings.cs`, `SettingsStore.cs`, `GithubReleaseClient.cs`,
`UpdateOrchestrator.cs`, `TrayController.cs`, `App.xaml.cs`,
`SettingsWindow.xaml` + `.cs`, `installer/VerseStrings.iss`, `README.md`,
`CHANGELOG.md`.

## Release

One release: **v0.1.5**. Two logical commits — Core + tests, then
App + installer + README — followed by the standard `Release v0.1.5`
promotion commit + tag. Same flow as v0.1.1 → v0.1.4.

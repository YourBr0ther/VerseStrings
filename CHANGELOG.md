# Changelog

All notable changes to VerseStrings are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `VersionParser.TryParseTag` in `VerseStrings.Core` for parsing GitHub release tags (`v0.1.0`, `v0.2.0-beta1`) into `System.Version`. Used by the self-update check.
- `Branding.AppName` constant — single source of truth for the brand identifier used in filesystem paths, registry values, and the single-instance mutex name.
- Targeted unit tests for `SettingsStore` (JSON round-trip, missing/empty/malformed-file behavior) and `VersionParser` (canonical tags, prerelease suffixes, garbage inputs).
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` on the App and Tests projects (Core already had it).

### Changed
- `TrayController` constructor now delegates menu construction to a `BuildMenu()` helper, leaving the ctor focused on dependency wiring and event subscription.
- `ToastService` collapsed from four near-identical methods (`ShowInfo`/`Success`/`Warning`/`Error`) to a single `Show(title, body)` — Windows toast styling is uniform, so the four names suggested visual differentiation that did not exist.
- `ReleaseInfo.AssetSha256` is now non-nullable. `GithubReleaseClient` returns `null` for the whole release when the asset has no digest, removing the redundant nullable checks downstream.
- `GithubReleaseClient` no longer accepts a `userAgent` constructor parameter (no caller overrode the default); the value is a public const shared with `SelfUpdater` so the User-Agent string is consistent across all GitHub API calls.
- `SelfUpdater.LatestReleaseUrl` is now a static — `github.com/<owner>/<repo>/releases/latest` is a permalink, so no per-tag URL tracking is needed.

### Removed
- Dead code surfaced by audit: `AppSettings.LastCheckAt`, `SettingsStore.FilePath`, `InstallResult.BackupFolderPath`, `ReleaseInfo.PublishedAt`, `ReleaseInfo.AssetSizeBytes`, `UpdateOrchestrator._loopTask`, `TrayController._restoreItem` and `_selfUpdateSeparator` fields.
- Redundant `StatusChanged` event invocation at the top of `UpdateOrchestrator.RunCheckAsync` — leftover from when `LastCheckAt` was written there.

### Fixed
- Brand leak: the installer's temp directory prefix was still `starstrings-{Guid}` from before the rebrand. Now `versestrings-`.
- `actions/checkout` bumped from v4 to v5 and `actions/setup-dotnet` from v4 to v6 in the release workflow, resolving the Node 20 deprecation warning from GitHub Actions.

## [0.1.0] — 2026-05-21

Initial release.

### Added
- Tray app that polls a community localization repository on GitHub every 15 minutes (configurable) and installs the latest release into the Star Citizen `LIVE` folder when the game is not running.
- Pending-update queueing: if Star Citizen is running when an update is detected, the watcher shows a "pending — will apply on close" toast and waits for `StarCitizen.exe` to exit before applying.
- Smart `user.cfg` handling: never overwrites the user's existing file. Only appends `g_language = english` when the line is missing.
- Timestamped backups of `data/Localization/english/` and `user.cfg` before each install, with a one-click **Restore previous version** entry in the tray menu.
- First-run wizard that auto-detects the Star Citizen `LIVE` folder via the RSI Launcher config, with manual folder picker fallback.
- Self-update check on startup — surfaces a tray menu entry and toast when a newer VerseStrings release is available on GitHub.
- Inno Setup installer (`VerseStringsSetup-<version>.exe`) — per-user install to `%LOCALAPPDATA%\Programs\VerseStrings\`, no admin required, proper uninstall entry under Apps & Features.
- GitHub Actions release workflow — push a `v*` tag to build the self-contained exe, compile the installer, compute SHA-256, and create a GitHub release with the installer attached.

[Unreleased]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/YourBr0ther/VerseStrings/releases/tag/v0.1.0

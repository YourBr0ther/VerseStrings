# Changelog

All notable changes to VerseStrings are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.1] — 2026-05-21

A cleanup release. No new user-facing features — everything in this version is a refactor, simplification, or correctness fix surfaced by a multi-pass code audit.

### Added
- `VersionParser.TryParseTag` in `VerseStrings.Core` for parsing GitHub release tags (`v0.1.0`, `v0.2.0-beta1`) into `System.Version`. Used by the self-update check.
- `Branding.AppName` constant — single source of truth for the brand identifier used in filesystem paths, registry values, and the single-instance mutex name.
- Targeted unit tests for `SettingsStore` (JSON round-trip, missing/empty/malformed-file behavior) and `VersionParser` (canonical tags, prerelease suffixes, garbage inputs). Test count: 8 → 31.
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` on the App and Tests projects (Core already had it).

### Changed
- `UpdateOrchestrator.StartAsync` → `Start` (void, sync). The previous signature was sync masquerading as async — it kicked off `Task.Run` and immediately returned `Task.CompletedTask`. The intent is now visible at the call site.
- `SelfUpdater.CheckAsync` (returning `bool` + side-effect `LatestVersion` property) → `CheckForNewVersionAsync` returning `Task<Version?>` directly. The previous "call this, then read this property" pattern was brittle.
- `GithubReleaseClient.GetLatestAsync` now validates required JSON fields up-front. Previously it defaulted missing fields to empty strings, which would have produced a `ReleaseInfo` that failed downstream with a cryptic error. Incomplete releases are now rejected with a clean `null` return.
- `ReleaseInfo.AssetSha256` is non-nullable. `GithubReleaseClient` returns `null` for the whole release when the asset has no digest, removing the redundant nullable checks downstream.
- `GithubReleaseClient` no longer accepts a `userAgent` constructor parameter (no caller overrode the default); the value is a public const shared with `SelfUpdater` so the User-Agent string is consistent across all GitHub API calls.
- `SelfUpdater.LatestReleaseUrl` is now a static — `github.com/<owner>/<repo>/releases/latest` is a permalink, so no per-tag URL tracking is needed.
- `ToastService` collapsed from four near-identical methods (`ShowInfo`/`Success`/`Warning`/`Error`) to a single `Show(title, body)` — Windows toast styling is uniform, so the four names suggested visual differentiation that did not exist.
- `TrayController` constructor now delegates menu construction to a `BuildMenu()` helper, leaving the ctor focused on dependency wiring and event subscription.
- README tightened by roughly half. Trust signals and user-facing details preserved; ceremony removed. Also corrected the "configurable to any repo" claim — only the upstream repo path is settable, not the asset name.

### Removed
- Dead code surfaced by audit: `AppSettings.LastCheckAt`, `SettingsStore.FilePath`, `InstallResult.BackupFolderPath`, `ReleaseInfo.PublishedAt`, `ReleaseInfo.AssetSizeBytes`, `UpdateOrchestrator._loopTask`, and the `TrayController._restoreItem` / `_selfUpdateSeparator` fields.
- `ProcessWatcher` no longer checks for `StarCitizen_Launcher` — that process name doesn't exist (the actual launcher is `RSI Launcher.exe`, and we don't need to detect it because it doesn't lock the localization files we touch).
- Three unused `x:Name` attributes in `SettingsWindow.xaml` (`LiveFolderHint`, `CancelButton`, `SaveButton`) that code-behind never referenced.
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

[Unreleased]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.1...HEAD
[0.1.1]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/YourBr0ther/VerseStrings/releases/tag/v0.1.0

# Changelog

All notable changes to VerseStrings are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.5] — 2026-05-22

### Added
- Multi-pack support. The app can now install and update any of four community
  localization packs: **StarStrings** (MrKraken/StarStrings), **ScCompLangPack**
  (ExoAE/ScCompLangPack), **ScCompLangPackRemix** (BeltaKoda/ScCompLangPackRemix),
  and **ScCompLangPackRemix2** (ExoAE/ScCompLangPack). Single tool, four sources.
- Installer wizard page after License lets the user pick their starting pack;
  the choice rides into the first launch via a `--pack=<id>` arg and pre-selects
  in the first-run wizard.
- Tray menu gains a **Localization pack** submenu with the four packs as radio
  items. Picking a different pack persists the choice, backs up the current
  install (existing backup path), and installs the new pack immediately. The
  game-running queue applies — switching while in-game queues until exit.
- `Pack` record, `Packs.All` static catalog, and `AssetMatcher` helper for
  exact-or-regex release-asset selection (BeltaKoda's pack has version-suffixed
  asset names; the others have stable filenames).
- Tests: `PacksTests`, `AssetMatcherTests`, plus migration coverage in
  `SettingsStoreTests`.

### Changed
- `GithubReleaseClient.GetLatestAsync` now takes an asset *pattern* instead of
  an exact name. Behavior is unchanged for stable filenames; the regex path is
  only taken when the pattern contains regex metacharacters.
- `AppSettings.SelectedPackId` (new) is canonical. The previous `Repo` field is
  retained as a no-op shim that `SettingsStore.Load` reads once to migrate
  v0.1.4 users to the equivalent pack ID, then `Save` zeroes it. Slated for
  removal in v0.1.6 once the migration window has closed.

### Fixed
- Tray icon now shows the actual VerseStrings icon on installed machines.
  The previous code read `Assets\icon.ico` from `AppContext.BaseDirectory`,
  which worked under `dotnet run` but not under the installed single-file
  exe — `installer/VerseStrings.iss` only ships `VerseStrings.exe`, no
  sidecar Assets folder, so `File.Exists` returned false and `LoadIcon`
  silently fell back to `SystemIcons.Information`. The icon is now an
  `EmbeddedResource` (`VerseStrings.icon.ico`), loaded from the assembly
  stream — no installer change required, and the behavior is identical
  for dev and installed runs.

## [0.1.4] — 2026-05-21

### Fixed
- Reinstall over a running tray no longer leaves the user with the old
  code in memory and a new exe on disk. `installer/VerseStrings.iss` now
  declares `AppMutex=Global\VerseStrings.SingleInstance` (the same name
  the app creates in `App.xaml.cs`), so Inno Setup detects the running
  process up front and prompts the user to close it before any file
  copy. Previously NTFS would silently let the exe be replaced while
  the running 0.1.x process held it, and the post-install launch hit
  the single-instance mutex and exited — the user appeared to have
  upgraded while still running the old build.

## [0.1.3] — 2026-05-21

Hardened error handling. No new features; existing flows now degrade
gracefully instead of crashing or going silent.

### Added
- Top-level safety net in `App`: `DispatcherUnhandledException`,
  `TaskScheduler.UnobservedTaskException`, and `AppDomain.UnhandledException`
  are wired to a single reporter that toasts the failure. UI-thread and
  unobserved-task exceptions no longer take the app down.
- Friendly startup message when `settings.json` is corrupt — names the file
  path and tells the user to fix or delete it, instead of the WPF crash
  dialog.
- `RestoreResult` (`FilesRestored`, `FailedFiles`) — `Installer.RestoreBackup`
  now reports per-file outcomes so the tray can tell the user e.g. "restored
  118 file(s); 2 could not be written (game running or files locked?)".

### Changed
- `UpdateOrchestrator` loop wraps every iteration in a catch-all. A transient
  `HttpRequestException`, a `JsonException` from `settings.json`, or any
  other unexpected throw now toasts and continues polling instead of killing
  the watcher silently. Interval reload is also defensive — falls back to
  the 15-minute default if `Load` throws.
- `Installer.RestoreBackup` no longer aborts on the first per-file failure.
  Each copy is independently try/catched; the result tells the caller which
  files failed.
- `AutostartService.Sync` returns `bool` indicating whether the registry
  write actually stuck. `TrayController` reverts the menu checkbox and
  toasts the user when Windows refuses the change.
- `ToastService.Show` is defensive — if Windows notifications are disabled
  (Focus Assist, Do Not Disturb, per-app block), the call no longer
  propagates and crashes whichever code path was just trying to inform
  the user.
- `SettingsWindow.OnSaveClicked` shows an error dialog and stays open if
  `settings.json` can't be written (disk full, AV holding the file). The
  previous behavior was to close as if save succeeded.
- All `TrayController` menu handlers are wrapped in `SafeInvoke` /
  `SafeInvokeAsync`. A `Process.Start` failure on "Open backups folder",
  a registry failure on autostart toggle, or any other unexpected exception
  surfaces as a toast rather than crashing the WinForms message loop.

## [0.1.2] — 2026-05-21

A second cleanup pass focused on making the public repo welcoming to outside
contributors. No user-facing behavior changes.

### Added
- `Directory.Build.props` at the repo root, consolidating `Nullable`,
  `ImplicitUsings`, `LangVersion`, and `TreatWarningsAsErrors` so all three
  projects pick them up from one place.
- `.editorconfig` covering indentation, end-of-line handling, and the project's
  existing C# style (file-scoped namespaces, `_camelCase` private fields).
- `.github/dependabot.yml` — weekly NuGet and GitHub Actions update PRs.
- `SECURITY.md` directing vulnerability reports to a private channel.
- `.github/ISSUE_TEMPLATE/` with bug and feature-request forms plus a `config.yml`
  that disables blank issues and points content reports at the upstream pack.
- Zip-slip guard in `Installer`: replaces `ZipFile.ExtractToDirectory` with a
  per-entry safe extraction that rejects entries resolving outside the
  extraction root. Pure path-validation helper covered by `InstallerSafetyTests`.
- `Branding.SelfUpdateRepo` — the GitHub `owner/repo` the app checks for its
  own updates now lives next to `Branding.AppName`, so a fork only needs to
  change one constant. The previous hardcoded value in `SelfUpdater` is gone.
- App icon embedded at the top of the README.

### Changed
- `VerseStrings.App.csproj` `<Version>` and `VerseStrings.iss` `AppVersion`
  defaults are now `0.0.0-dev` (was a stale `0.1.1`). The release workflow
  always overrides both via `-p:Version=` and `/DAppVersion=`, so the previous
  defaults only ever shipped if someone built locally without the override —
  in which case `0.0.0-dev` is the honest answer.
- `installer/README.md` versioning section rewritten to describe the new
  CI-supplied flow instead of the old "kept in sync manually" warning.

### Removed
- Redundant `s.Contains("LIVE")` filter in `GameLocator.ScanForLivePath` —
  `LooksLikeLiveFolder` already requires `Bin64\StarCitizen.exe`, which
  excludes PTU/EPTU/TECH-PREVIEW folders by construction.

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
- `actions/checkout` bumped from v4 to v6 and `actions/setup-dotnet` from v4 to v5 in the release workflow, resolving the Node 20 deprecation warning from GitHub Actions. Both are Node 24-compatible majors and both publish a major-version alias.

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

[Unreleased]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.5...HEAD
[0.1.5]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.4...v0.1.5
[0.1.4]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.3...v0.1.4
[0.1.3]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.2...v0.1.3
[0.1.2]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/YourBr0ther/VerseStrings/releases/tag/v0.1.0

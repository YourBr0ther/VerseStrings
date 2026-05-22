# Changelog

All notable changes to VerseStrings are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.16] — 2026-05-22

Standalone run mode.

### Added
- New **Standalone** run mode for users who don't want a background watcher.
  Opens a single window with a **Sync now** button — click it, the app
  fetches the latest release for the selected pack, runs the same install
  pipeline the tray uses (download, SHA verify, backup, install), toasts
  the result. Closing the window exits the app. No tray icon, no polling,
  no autostart.
- Installer wizard's **Run modes** task selection on the Ready page lets
  the user pick Tray (default), Standalone, or both. At least one must be
  selected (`NextButtonClick` blocks the wizard otherwise).
- README **Run modes** section documenting both flows and how to switch
  between them.

### Changed
- `VerseStrings.exe` startup dispatches on a `--standalone` flag. No-args
  launch (the existing tray-shortcut form, unchanged) preserves v0.1.0–
  v0.1.15 behavior. The new standalone Start-menu shortcut passes
  `--standalone`.
- `SettingsWindow` gains a `Mode` enum (`FirstRun` / `FromTrayMenu` /
  `Standalone`). Standalone mode hides the check-interval row and
  autostart checkbox (both tray-mode-only concepts), shows a Sync +
  Close button row instead of Save + Cancel, and is the app's
  `MainWindow` — its `Closed` event triggers `Application.Shutdown`.
- `UpdateOrchestrator` gains `SyncNowAsync` returning `SyncOutcome`
  (`NoChange` / `Installed` / `GameRunning` / `Failed`). Underneath:
  `RunCheckAsync` was refactored to take a `bool waitForGameExit` and
  return the outcome. The tray loop and `CheckNowAsync` pass `true`
  (existing queue-until-game-closes behavior preserved); standalone's
  Sync passes `false` (returns `GameRunning` immediately so the user
  can close the game and re-click).
- `InstallerArgs` renamed to `StartupArgs` — same source file, same
  parsing role, but now also reads `--standalone`. Returns a record
  with `IsStandalone` + `PackHint`. Test coverage extended from 5 to
  11 cases (standalone-flag variants, case sensitivity, combined flags,
  order independence).

### Fixed
- Installer cleans up the autostart `HKCU\…\Run\VerseStrings` value
  during install if the tray task is unticked. Prevents the corner case
  where a previous tray install enabled autostart and a subsequent
  standalone-only reinstall would otherwise leave an orphaned Run-key
  firing tray mode at every login.

## [0.1.15] — 2026-05-22

### Added
- Win32 version metadata on the app exe and the installer exe. The .NET
  project now declares `<Company>`, `<Product>`, `<AssemblyTitle>`,
  `<Description>`, `<Copyright>`, `<PackageProjectUrl>`, and
  `<RepositoryUrl>`. The Inno Setup script now sets `VersionInfoCompany`,
  `VersionInfoProductName`, `VersionInfoProductVersion`, `VersionInfoVersion`,
  `VersionInfoDescription`, and `VersionInfoCopyright`. Right-click →
  Properties → Details now shows real values instead of empty fields, and
  the binary doesn't read as "unattributed" to AV heuristics that weight
  missing metadata as a weak suspicion signal (motivated by a
  `Trojan:Win32/Wacatac.C!ml` false-positive flag on the unsigned v0.1.14
  installer).

### Changed
- README updated to reflect the current tray menu (the previous list dated
  from v0.1.0 and was missing the pack picker, settings, and open-backups
  entries) and to mention that the app self-checks for new VerseStrings
  releases on launch. "How it works" intro tightened from "a community
  localization repo" to "your selected pack's GitHub repo" so multi-pack
  semantics are clear up front.

## [0.1.14] — 2026-05-22

Public-friendliness polish. No app behavior change.

### Added
- Release workflow now extracts the matching `## [X.Y.Z]` section from
  `CHANGELOG.md` and prepends it to the GitHub release notes under a
  "What's new" header. Anyone landing on the release page sees the actual
  per-version changes alongside the install instructions and SHA-256,
  instead of having to click through to the repo to find the changelog.
- `.github/PULL_REQUEST_TEMPLATE.md` matching the existing issue
  templates. Prompts contributors for summary, Windows version tested
  on, packs tested with, and tests added.
- README badges for CI status, latest release, and total download count.
- Discoverability topics on the GitHub repo (`star-citizen`,
  `localization`, `windows`, `tray-app`, `dotnet`, `inno-setup`, `wpf`).

## [0.1.13] — 2026-05-22

A senior-engineer audit pass. No user-facing behavior change.

### Removed
- `AppSettings.Repo`, `SettingsStore.MigrateLegacyRepoToPackId`, the `Save`-
  clears-`Repo` shim, and the two test classes covering them. The migration
  existed to carry v0.1.4 users' pack choice forward when v0.1.5 introduced
  `SelectedPackId`. The project hadn't gone public when v0.1.4 shipped, so
  no v0.1.4 install ever needed migrating; the shim was dead weight.
- The `var capturedPack = pack` line inside `TrayController.BuildPackMenu`'s
  `foreach`. C# 5 (2012) made `foreach` create a fresh loop variable each
  iteration, so the defensive copy has been redundant for 13 years.

### Changed
- `Branding.SelfUpdateRepo` XML doc cref pointed at the now-deleted
  `AppSettings.Repo`. Updated to point at `Packs.All` and
  `AppSettings.SelectedPackId` — where the localization-pack upstream
  actually lives now.
- `UpdateOrchestrator` field `_settings` renamed to `_settingsStore` for
  consistency with the rest of the codebase. The field is a `SettingsStore`,
  not an `AppSettings`, and every other class spells it `_settingsStore`.
  Constructor parameter renamed to match.
- `Installer` temp directory prefix now uses
  `Branding.AppName.ToLowerInvariant()` instead of the hardcoded literal
  `"versestrings-"`. CLAUDE.md says the brand string belongs in `Branding`,
  full stop; this was the last holdout. Same shape of bug that produced
  the `"starstrings-"` brand-leak fixed in v0.1.1.

### Added
- `Installer.ResolveSourceRoot` is now `public static` (matching `ShouldInstall`
  and `TryResolveSafeEntryPath` — same "public for testing" convention) and
  covered by `InstallerResolveSourceRootTests`: 5 cases pinning down the
  wrapper-subdir peel behavior so the next pack maintainer who ships a
  wrapper layout doesn't silently break the install.

## [0.1.12] — 2026-05-22

### Changed
- Three duplications collapsed; no behavior change.
- `UserPaths` (new, in Core) is the single source of truth for `%APPDATA%\
  VerseStrings\`, `settings.json`, and the backups root. Replaces four
  inline `Path.Combine(Environment.GetFolderPath(ApplicationData), …)`
  computations across `SettingsStore`, `App.OnStartup` (×2), and
  `TrayController.OnOpenBackupsFolder`. Changing the layout (or the brand)
  is now a one-place edit.
- `GithubReleaseClient` gains `GetLatestTagAsync(repo)`, sharing a private
  `FetchLatestPayloadAsync` helper with the existing `GetLatestAsync`. The
  GitHub API URL, headers, and JSON DTO live in one class instead of two.
- `SelfUpdater` rewritten around `GithubReleaseClient` — takes the client
  in its constructor instead of `HttpClient`, drops its own HTTP code and
  its parallel `ReleasePayload` DTO. Shrinks from ~50 lines to ~30 with
  no loss of behavior.
- New `UserPathsTests` (3 cases) confirms the brand-derived path suffixes
  are wired correctly. Test count 85 → 88.

## [0.1.11] — 2026-05-22

### Fixed
- Changing the pack via the Settings dialog now reflects in the tray
  submenu and triggers an immediate switch — same behavior as picking
  the pack from the tray submenu directly. Previously the dialog
  Save persisted the new `SelectedPackId` but the tray's cached radio-
  dot state was never refreshed, and the orchestrator only picked the
  new pack up on the next 15-minute poll. The two UI surfaces now
  agree on what's selected and act consistently when changed.

### Changed
- `TrayController` consolidates menu-state-from-settings into a single
  `RefreshTrayState` method. Every code path that mutates settings —
  the orchestrator's `StatusChanged` event, the Settings dialog close,
  the tray's "Check now" finally, the initial `Show` — funnels through
  it. Removes the bug class where one handler refreshed some menu
  items but forgot others.
- `OnSelectPack` factored: the "already on this pack" early-return
  stays at the tray-click entry, and the actual switch logic is now
  `SwitchToPackAsync` — reused by the Settings dialog post-save path.

## [0.1.10] — 2026-05-22

### Fixed
- Settings ComboBox header (the closed-selection display) now keeps the
  dark theme when focused or while the dropdown is open. The default WPF
  template applies `SystemColors` to that area in those states, which
  overrode our `Background` and made the currently-selected pack name
  hard to read. Provided a full `ComboBox` `ControlTemplate` with explicit
  triggers — accent blue border (`#3d6dd0`) on keyboard focus, a slightly
  lighter gray (`#5a5a66`) on mouse hover, dark gray border otherwise.
  The chevron is a small `Path` triangle so it doesn't drag in system-
  themed `Button` chrome either.

## [0.1.9] — 2026-05-22

### Fixed
- Highlighted/selected pack in the settings ComboBox dropdown was still
  unreadable after the v0.1.8 dark-theme fix. The default `ComboBoxItem`
  `ControlTemplate` overrides `Background` to `SystemColors.HighlightBrush`
  (light blue) when `IsHighlighted` is true, so a style-level `Setter`
  only colored the non-highlighted state — leaving near-white text on
  light blue with no contrast. Added a custom `ControlTemplate` that
  drives both states: dark background for the normal state, accent blue
  (`#3d6dd0`) with white text for highlighted, matching the Save button.

## [0.1.8] — 2026-05-22

### Added
- Pack labels now include the upstream author. The installer wizard, tray
  "Localization pack" submenu, and settings dropdown all show
  `"<PackName> (<Author>)"` — e.g. `ScCompLangPackRemix (BeltaKoda)`. Three
  packs share the `ScCompLang...` prefix, so the author tag is what makes
  them distinguishable at a glance. `Pack` record gains an `Author` field
  and a computed `Label` property; `Packs.All` populated for all four.

### Fixed
- Settings dialog "Localization pack" dropdown was unreadable — items
  rendered near-white on the system default white popup background. Added
  a `ComboBoxItem` style alongside the existing `ComboBox` style so
  dropdown items pick up the dark-theme background, not just the box body.
  WPF's default template doesn't propagate `Background` from the parent
  `ComboBox` to its items.

## [0.1.7] — 2026-05-22

### Changed
- `--pack=<id>` parsing extracted from `App.xaml.cs` into
  `VerseStrings.Core.InstallerArgs.TryGetPackId` so the installer-to-app
  handoff can be unit-tested without a real install run. `InstallerArgsTests`
  covers all four known pack ids, flag-among-unrelated-args, unknown/empty
  values, and missing-flag cases. No behavior change.

### Fixed
- `AppSettings.Repo` XML doc and the v0.1.5 CHANGELOG note both claimed the
  legacy field would be removed in v0.1.6. v0.1.6 deliberately didn't remove
  it (v0.1.5 had barely shipped, so the migration window hadn't started).
  Language now reads "removed in a future release once the v0.1.4-direct-
  upgrade window has plausibly closed" — honest about the timing instead of
  naming a version we already skipped past.

## [0.1.6] — 2026-05-22

### Fixed
- Pack zip extraction now installs only what the README documents: a top-
  level `user.cfg` (case-insensitive) and anything under a top-level
  `data/` directory (case-insensitive). Files at the zip root that aren't
  `user.cfg` — most notably StarStrings' `readme.md`, which has been
  silently landing in users' LIVE folders since v0.1.0 — are now skipped.
  Pure helper `Installer.ShouldInstall(relativePath)` covered by
  `InstallerScopeTests`.

### Note for upgraders
- Existing installs may already have a `readme.md` (and possibly other
  pack-author artifacts) sitting in `StarCitizen\LIVE\`. This release
  stops adding new ones but does not retroactively delete what's already
  there. You can safely remove `readme.md` from your LIVE folder root if
  you'd like; Star Citizen ignores it either way.

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
  v0.1.4 users to the equivalent pack ID, then `Save` zeroes it. Will be
  removed in a future release once the v0.1.4-direct-upgrade window has
  plausibly closed.

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

[Unreleased]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.16...HEAD
[0.1.16]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.15...v0.1.16
[0.1.15]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.14...v0.1.15
[0.1.14]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.13...v0.1.14
[0.1.13]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.12...v0.1.13
[0.1.12]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.11...v0.1.12
[0.1.11]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.10...v0.1.11
[0.1.10]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.9...v0.1.10
[0.1.9]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.8...v0.1.9
[0.1.8]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.7...v0.1.8
[0.1.7]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.6...v0.1.7
[0.1.6]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.5...v0.1.6
[0.1.5]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.4...v0.1.5
[0.1.4]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.3...v0.1.4
[0.1.3]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.2...v0.1.3
[0.1.2]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/YourBr0ther/VerseStrings/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/YourBr0ther/VerseStrings/releases/tag/v0.1.0

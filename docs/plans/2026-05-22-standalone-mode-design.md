# Standalone mode — design

**Status:** approved 2026-05-22
**Target release:** v0.1.16

## Problem

VerseStrings only runs as a system-tray background watcher. Users who don't
want a persistent background process — who'd rather "open the app, click Sync,
close it" on their own schedule — have no supported flow. The install-time
choice should let them pick a tray app, a standalone app, or both.

## Architecture — Approach A (one exe, two shortcuts)

A single `VerseStrings.exe`. Behavior is driven by a startup flag:

| Launched with | Mode | Behavior |
|---|---|---|
| (no args) | **Tray** | Existing v0.1.15 behavior. System tray icon, 15-min polling, autostart-capable. |
| `--standalone` | **Standalone** | No tray icon, no polling, no autostart. Opens a main window. Closing the window exits. |

Tray is the default (no-args) for backward compatibility with v0.1.0–v0.1.15
shortcuts. Standalone is opt-in via a shortcut flag the installer creates.

**One mutex** — `Global\VerseStrings.SingleInstance` — shared between modes.
Only one mode runs at a time. Switching modes is "quit current shortcut, launch
the other." No in-app toggle, no IPC, no mutex name games.

### Why not two separate exes

Two binaries from one solution (separate `VerseStrings.exe` and
`VerseStringsSync.exe`) would give cleaner code-level separation but require
real restructuring: a new `VerseStrings.Shared` project, file moves, two builds,
two SHA-256s, two FP-submission requests on every release. The argv-mode-switch
in a single exe avoids that ceremony for a feature where the two modes legitimately
share ~all the same service composition. Single-exe is also Visual Studio's
own pattern for `devenv.exe /command` versus the GUI launch.

## Installer UX

`installer/VerseStrings.iss` gains a `[Tasks]` block on the Ready page:

```
Run modes:
  ☑ Tray app (background, auto-installs new pack releases)
  ☐ Standalone app (open it manually to sync)

Additional shortcuts:
  ☐ Create a Desktop shortcut
```

- **Tray task** — checked by default. Existing v0.1.15 behavior on upgrade.
- **Standalone task** — unchecked by default. Opt-in.
- Both can be checked (user gets two Start-menu shortcuts).
- At least one must be checked — a `Check:` function in `[Code]` keeps the
  Next button disabled if both are unticked.

### Shortcuts created

```
Start menu\VerseStrings              → VerseStrings.exe               (Tasks: trayshortcut)
Start menu\VerseStrings Standalone   → VerseStrings.exe --standalone  (Tasks: standaloneshortcut)
Desktop\VerseStrings                 → tray or standalone depending on which is installed
```

If only standalone was selected, the desktop shortcut uses `--standalone`.
Otherwise it points at the tray (no-args) launch.

### Post-install launch

The existing "Launch VerseStrings" checkbox in `[Run]` honors the user's choice:

- Tray selected → `VerseStrings.exe --pack=<id>`
- Standalone-only selected → `VerseStrings.exe --standalone --pack=<id>`

### Autostart Run-key cleanup

A new `[Registry]` entry deletes the autostart Run-key value when the tray
task is **unticked**. Prevents the case where a previous tray install left
autostart enabled and a subsequent standalone-only reinstall would otherwise
leave the orphaned Run-key firing tray mode at every login.

```
[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run";
  ValueName: "{#AppName}"; ValueType: none; Flags: deletevalue uninsdeletevalue;
  Tasks: not trayshortcut
```

**AppMutex stays unchanged** — `Global\{#AppName}.SingleInstance`.

## Standalone window

The existing `SettingsWindow` is the basis. New `Mode` enum parameter:

```csharp
public enum SettingsWindowMode { FirstRun, FromTrayMenu, Standalone }
```

### Per-mode chrome and field visibility

| Element | FirstRun | FromTrayMenu | Standalone |
|---|---|---|---|
| Title | "VerseStrings — Setup" | "VerseStrings Settings" | "VerseStrings" |
| LIVE folder field | shown | shown | shown |
| Pack picker | shown | shown | shown |
| Check-interval row | shown | shown | **hidden** (no polling) |
| Autostart checkbox | shown | shown | **hidden** (tray-only feature) |
| Status line | "Welcome…" | "Last installed: X" | "Last installed: X" |
| Bottom buttons | Cancel + Save | Cancel + Save | **Sync + Close** |

### Sync button behavior

Click runs `orchestrator.SyncNowAsync()` — new public method on
`UpdateOrchestrator` that returns:

```csharp
public enum SyncOutcome { NoChange, Installed, GameRunning, Failed }
```

The method:

1. Validates LIVE folder and pack id (same checks `OnSaveClicked` does today).
2. Persists settings to disk.
3. Calls `RunCheckAsync` and observes whether it installed something.
4. Returns the outcome.

UI feedback by outcome:

- `NoChange` → toast "Already up to date — \<PackName\> latest is already installed."
- `Installed` → existing "VerseStrings updated — Installed X, N files. Backup saved." toast.
- `GameRunning` → toast "Star Citizen is running. Close the game and click Sync again."
- `Failed` → existing "VerseStrings update failed: \<message\>" toast.

The Sync button disables itself during the call and shows "Syncing…" so
double-clicks don't re-enter.

### Close behavior

`Close` saves settings to disk (so field edits stick) and exits the app.
Clicking the window's **X** does the same. If a Sync is in flight when the
user closes, the app waits for it to finish (status text reads "Finishing
install, please wait…") before exiting, so installs aren't half-applied.

## Behavior details

### Mode dispatch in `App.OnStartup`

Pre-mode-fork is identical: global exception handlers → mutex check → load
settings → construct shared services (`SettingsStore`, `GithubReleaseClient`,
`Installer`, `ToastService`, `ProcessWatcher`, `AutostartService`,
`UpdateOrchestrator`) → first-run wizard if needed.

After that point:

- **Tray**: `autostart.Sync(settings.AutostartEnabled)` → create `TrayController`
  → `tray.Show()` → `orchestrator.Start()`.
- **Standalone**: skip autostart sync (preserves the setting but doesn't act
  on it — installer already handled the Run-key cleanup at install time),
  skip tray creation, skip `orchestrator.Start()`. Open `SettingsWindow` in
  `Standalone` mode as `Application.MainWindow`.

`SelfUpdater.CheckForNewVersionAsync` runs in both modes — surfaces a tray
menu entry in tray mode, a toast in standalone.

### Mutex collision

User has tray running, double-clicks standalone shortcut: existing "already
running, look for the system tray icon" message box. Standalone process exits.
To switch modes: quit current, launch the other shortcut.

### Pack hint

`--pack=<id>` honored identically in both modes during first-run only. After
`FirstRunCompleted=true`, the flag is ignored. Existing behavior, unchanged.

### Shutdown

`OnExit` disposes the same things in both modes (tray controller if present,
orchestrator, HttpClient, mutex). The orchestrator's `Dispose` already cancels
its CTS and waits — works correctly even if `Start()` was never called.

## Migration / backward compat

- Existing v0.1.0–v0.1.15 shortcuts (no args) keep working — they launch tray
  mode unchanged.
- Settings file schema: no changes.
- Mutex name: unchanged.
- v0.1.16 installer default Tasks selection puts tray as checked, so a user
  who just clicks Next through the wizard gets the same install they had
  before.

## Tests

Pure-logic only, per project policy:

- `StartupArgsTests` — renamed from `InstallerArgsTests`, extended with
  `IsStandalone` parsing cases. ~6 new test cases.

No new tests for: mode dispatch (OS-touching), `SettingsWindow` mode-based
visibility (UI), `SyncNowAsync` (orchestrator is OS-touching).

## Files

**New**: `src/VerseStrings.App/Services/SyncOutcome.cs`.

**Renamed**: `src/VerseStrings.Core/InstallerArgs.cs` → `StartupArgs.cs`;
`tests/VerseStrings.Core.Tests/InstallerArgsTests.cs` → `StartupArgsTests.cs`.

**Modified**: `App.xaml.cs`, `UpdateOrchestrator.cs`, `SettingsWindow.xaml`,
`SettingsWindow.xaml.cs`, `installer/VerseStrings.iss`, `README.md`,
`CHANGELOG.md`.

## Release plan

Single release: **v0.1.16**. Two logical commits matching the multi-pack
pattern from v0.1.5:

1. **Core + tests**: `StartupArgs` rename + `IsStandalone` field + tests.
   `SyncOutcome` enum. `SyncNowAsync` method on `UpdateOrchestrator`.
2. **App + installer + docs**: mode dispatch in `App.OnStartup`,
   `SettingsWindow` mode-aware changes, installer Tasks block + Registry
   cleanup, README "Run modes" section.

Followed by the standard `Release v0.1.16` promotion commit + tag.

Working on main directly per project pattern; no worktree.

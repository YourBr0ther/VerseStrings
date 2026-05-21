<div align="center">

# VerseStrings

A small Windows tray app that keeps your Star Citizen community localization pack up to date — quietly, automatically, and never while you're in the verse.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2B-blue)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)

</div>

## What it does

Watches a community Star Citizen localization repo on GitHub for new releases. When one drops:

- If Star Citizen isn't running, it backs up your current files, applies the update, and shows a Windows toast.
- If you're in the verse, it queues the update and shows a "pending — will apply on close" toast. The moment you quit the game, the new strings drop in.
- It never overwrites your `user.cfg`. It only appends `g_language = english` if the line is missing.
- Every install keeps a timestamped backup. **Restore previous version** is one click away in the tray menu.

The default source is [MrKraken/StarStrings](https://github.com/MrKraken/StarStrings) — the excellent community pack that adds blueprint pool info to contracts, cleans up hauling titles, prefixes component grades, and more. You can point VerseStrings at any GitHub repo that publishes a similar `StarStrings.zip`-shaped asset (a `data/` folder, optionally a `user.cfg`).

## Install

1. Grab the latest `VerseStringsSetup-X.Y.Z.exe` from [Releases](../../releases).
2. Run it. SmartScreen may warn that the publisher is unverified — click **More info** → **Run anyway**.
3. Per-user install to `%LOCALAPPDATA%\Programs\VerseStrings\`. No admin prompt.
4. On first launch, the app auto-detects your `StarCitizen\LIVE` folder via the RSI Launcher config, or asks you to pick it.

The watcher then sits in your system tray. Right-click it to check for updates manually, open settings, restore a backup, or toggle Start with Windows.

## What it touches in your game install

Only two paths under `StarCitizen\LIVE\`:

```
LIVE\
├── user.cfg                          (append-only — your existing settings are preserved)
└── data\Localization\english\        (replaced — previous version is backed up first)
```

Nothing else in your Star Citizen install is read or written. The app never launches, modifies, or talks to the RSI Launcher or the game itself.

## Where things live

| | |
|---|---|
| Installed binary | `%LOCALAPPDATA%\Programs\VerseStrings\VerseStrings.exe` |
| Settings | `%APPDATA%\VerseStrings\settings.json` |
| Backups | `%APPDATA%\VerseStrings\backups\<timestamp>\` |

Uninstall via **Settings → Apps & Features → VerseStrings**, or by running the installer again. Uninstalling does not touch your Star Citizen install.

## Trust & privacy

- **No telemetry.** The app does not phone home.
- **Network access only to `api.github.com` and `github.com`** to check for releases and download the asset.
- **Self-contained.** Ships with the .NET 9 runtime embedded — no background services, no system extensions, no admin rights.
- **Open source.** [MIT](LICENSE)-licensed. Read every line; build it yourself if you prefer (see below).

## Compatibility

- Windows 10 1809+ / Windows 11 (x64 only).
- Star Citizen Alpha 4.x. The app installs whatever the upstream pack publishes; check upstream notes for build compatibility.

## Build from source

Requires .NET 9 SDK on Windows.

```powershell
dotnet test
dotnet publish src/VerseStrings.App -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

To build the installer, see [installer/README.md](installer/README.md).

## Project layout

```
src/
├── VerseStrings.Core/         pure logic, cross-platform (no UI)
└── VerseStrings.App/          WPF tray app, Windows-only
installer/
└── VerseStrings.iss           Inno Setup script
tests/
└── VerseStrings.Core.Tests/   xUnit coverage for the file-touching logic
```

## Acknowledgments

- [MrKraken](https://github.com/MrKraken) for [StarStrings](https://github.com/MrKraken/StarStrings) — the community pack VerseStrings was built to deliver.
- [ExoAE](https://github.com/ExoAE/ScCompLangPack) for the original community localization concept.
- The Star Citizen community for keeping these tools alive.

## Disclaimer

Unofficial fan tool. Not affiliated with Cloud Imperium Games or the Roberts Space Industries family of companies. Custom localization is intended and authorized by CIG ([Spectrum thread](https://robertsspaceindustries.com/spectrum/community/SC/forum/1/thread/star-citizen-community-localization-update)) — VerseStrings simply automates installing community packs that operate within that.

VerseStrings does not modify Star Citizen itself, only the optional localization assets in the locations documented above. If anything goes wrong, **Restore previous version** in the tray menu reverts to the prior state in a single click.

<div align="center">

<img src="art/icon.svg" alt="" width="128" height="128">

# VerseStrings

Auto-installs community localization packs for Star Citizen. Watches GitHub for new releases, applies them when you're not in the verse, backs up the previous version every time.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2B-blue)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)

</div>

## How it works

- Polls a community localization repo every 15 minutes (configurable).
- When a release drops:
  - **Game closed** → backs up your files, applies the update, toast.
  - **Game running** → toast announces pending update; applies when you quit.
- Never overwrites your `user.cfg`. Only appends `g_language = english` if missing.

## Supported packs

Pick one at install time. Switch later from the tray menu (**Localization pack** submenu) — VerseStrings backs up the current pack and installs the new one.

| Pack | Upstream |
|---|---|
| StarStrings | [MrKraken/StarStrings](https://github.com/MrKraken/StarStrings) |
| ScCompLangPack | [ExoAE/ScCompLangPack](https://github.com/ExoAE/ScCompLangPack) |
| ScCompLangPackRemix | [BeltaKoda/ScCompLangPackRemix](https://github.com/BeltaKoda/ScCompLangPackRemix) |
| ScCompLangPackRemix2 | [ExoAE/ScCompLangPack](https://github.com/ExoAE/ScCompLangPack) |

## Install

Grab `VerseStringsSetup-X.Y.Z.exe` from [Releases](../../releases). Per-user install, no admin.

SmartScreen may warn the publisher is unverified — **More info → Run anyway**.

On first launch the app auto-detects your `StarCitizen\LIVE` folder or prompts you to pick it. Tray icon has check-for-updates, restore-backup, and start-with-Windows.

## What it touches

Two paths under `StarCitizen\LIVE\`, nothing else:

```
user.cfg                   ← append-only
data\Localization\english\ ← replaced; previous version backed up first
```

The watcher never talks to the game or the RSI Launcher.

## Where things live

| | |
|---|---|
| Binary | `%LOCALAPPDATA%\Programs\VerseStrings\` |
| Settings | `%APPDATA%\VerseStrings\settings.json` |
| Backups | `%APPDATA%\VerseStrings\backups\<timestamp>\` |

Uninstall via Apps & Features. Doesn't touch your Star Citizen install.

## Trust

No telemetry. Network access only to `api.github.com` and `github.com`. .NET 9 runtime is embedded — no services, no admin, no background scripts. [MIT](LICENSE) licensed.

## Build

```powershell
dotnet test
dotnet publish src/VerseStrings.App -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Installer build: see [installer/README.md](installer/README.md).

## Credits

- [MrKraken](https://github.com/MrKraken) — [StarStrings](https://github.com/MrKraken/StarStrings).
- [ExoAE](https://github.com/ExoAE/ScCompLangPack) — [ScCompLangPack](https://github.com/ExoAE/ScCompLangPack) and ScCompLangPackRemix2.
- [BeltaKoda](https://github.com/BeltaKoda) — [ScCompLangPackRemix](https://github.com/BeltaKoda/ScCompLangPackRemix).

## Disclaimer

Unofficial fan tool. Not affiliated with Cloud Imperium Games. Community localization is intended and authorized by CIG ([Spectrum thread](https://robertsspaceindustries.com/spectrum/community/SC/forum/1/thread/star-citizen-community-localization-update)). Tray → **Restore previous version** undoes the last install if anything goes sideways.

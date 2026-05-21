# Installer

Builds `VerseStringsSetup-<version>.exe` using [Inno Setup](https://jrsoftware.org/isdl.php).

## Build

```powershell
# 1. Publish the app (single-file, self-contained)
dotnet publish src/VerseStrings.App -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# 2. Compile the installer (requires Inno Setup 6.3+ on PATH or full path to ISCC.exe)
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\VerseStrings.iss
```

Output lands in `dist/VerseStringsSetup-<version>.exe`.

## What the installer does

- Per-user install to `%LOCALAPPDATA%\Programs\VerseStrings\` (no admin required).
- Start Menu shortcut.
- Optional Desktop shortcut (unchecked by default).
- Registers an uninstall entry under Settings → Apps & Features.
- Offers to launch the app on completion.

Autostart-at-login is not handled by the installer — the app's first-run wizard manages that, and the tray menu has a toggle.

## Versioning

The release version is supplied by CI from the pushed `v*` tag — `release.yml`
passes `-p:Version=` to `dotnet publish` and `/DAppVersion=` to `ISCC.exe`. The
defaults inside `VerseStrings.iss` and `VerseStrings.App.csproj` are both
`0.0.0-dev`, so a local build that forgets to pass a version is honest about
being a dev build rather than impersonating a stale release.

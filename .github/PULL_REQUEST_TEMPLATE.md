## Summary

<!-- One or two sentences on what this changes and why. -->

## Testing

- [ ] Built locally with `dotnet build VerseStrings.sln -c Release` (0 warnings, 0 errors)
- [ ] `dotnet test` passes
- [ ] Windows version tested on: <!-- e.g. Windows 11 23H2 -->
- [ ] Pack(s) tested with: <!-- StarStrings / ScCompLangPack / ScCompLangPackRemix / ScCompLangPackRemix2 -->
- [ ] Tested with Star Citizen running (for install/restore flows that need it)

## Tests added/updated

<!-- For pure-logic changes in VerseStrings.Core, add or update tests in
     VerseStrings.Core.Tests. OS-touching services (installers, registry,
     network) are left untested by project policy — see CLAUDE.md if you
     have access. -->

## Checklist

- [ ] CHANGELOG.md updated under `[Unreleased]` if user-visible
- [ ] No new hardcoded brand string (uses `Branding.AppName`)
- [ ] No new `Path.Combine(...)` for app-data paths (uses `UserPaths.*`)

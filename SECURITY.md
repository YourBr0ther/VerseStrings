# Security policy

Thanks for taking the time to report a problem responsibly.

## Reporting a vulnerability

Please **do not open a public GitHub issue** for security problems. Email the maintainer instead:

- **yourbr0ther.tv@gmail.com**

Include enough detail to reproduce — affected version, OS, and steps. A working proof of concept is appreciated but not required.

You can expect:

- An acknowledgement within a few days.
- A reasonable best-effort fix on a private branch.
- Credit (or anonymity, your call) in the release notes for the patched version.

## Scope

In scope:

- The VerseStrings installer and tray app (current `main` and the latest released installer).
- The release pipeline in `.github/workflows/`.
- Anything that could let a malicious upstream release tamper with a user's Star Citizen install beyond the documented surface (`user.cfg`, `data\Localization\english\`).

Out of scope:

- Vulnerabilities in `MrKraken/StarStrings` or other community localization repos — report those to their maintainers.
- The Star Citizen game client or RSI Launcher.
- Issues that require an attacker to already have local write access to `%APPDATA%\VerseStrings\` or the LIVE folder.

## Supported versions

Only the most recent release is supported. Older versions get fixes only by upgrading.

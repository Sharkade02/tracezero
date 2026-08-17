<p align="center">
  <img src="src/TraceZero.App/Assets/logo.png" alt="TraceZero" width="96" />
</p>

<h1 align="center">TraceZero</h1>

<p align="center"><em>See what's left. Clean what you choose.</em></p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-green.svg" alt="MIT License" /></a>
  <img src="https://img.shields.io/badge/Windows-10%20%2F%2011-0078D6?logo=windows" alt="Windows 10/11" />
  <img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet" alt=".NET 10" />
  <a href="https://github.com/Sharkade02/tracezero/releases"><img src="https://img.shields.io/github/v/release/Sharkade02/tracezero?label=version" alt="Latest release" /></a>
</p>

<p align="center"><strong>English</strong> · <a href="README.fr.md">Français</a></p>

TraceZero is a Windows **cleaning, privacy, disk-space and maintenance** tool, built to genuinely
compete with CCleaner and PrivaZer — but **local-first, privacy-first, with no ads, no dark patterns
and no misleading claims**.

> **Philosophy:** no displayed value is faked; numbers appear only after a real scan. No deletion happens
> without going through a safety layer that refuses by default. The app **never** runs as administrator.

## Download

- **Portable (no installation):**
  [latest release](https://github.com/Sharkade02/tracezero/releases/latest) → unzip
  `TraceZero-portable.zip`, run `TraceZero.App.exe`.
- **Via winget:** `winget install TraceZero.TraceZero`

> **"Unknown publisher" on first launch?** That's expected: TraceZero is distributed directly (outside the
> Store) and is not yet signed with a paid certificate. Verify the **SHA-256** hash published with each
> release, then *More info → Run anyway*. Details and reasons: [`docs/download.md`](docs/download.md).

## Support the project

TraceZero is **free, open source (MIT)**, and ad-free. Support is **voluntary, pay what you want** — no
feature is locked behind a payment.

➡️ **[paypal.me/sharkadeFR](https://paypal.me/sharkadeFR)** — or the **Support** tab inside the app.

## Features

- **Windows cleanup** — temp files, crash dumps, WER, caches, Recycle Bin (user-scoped rules, real sizes,
  risk-based preview).
- **Privacy** — "what Windows still knows about your activity": recent documents, RunMRU, typed paths,
  searches, UserAssist… each trace explained, registry cleanup allow-listed.
- **Browsers** — Chrome/Edge/Brave/Vivaldi/Chromium/**Opera/Opera GX**/Firefox: cleans **SAFE caches** +
  **history/cookies/sessions** opt-in (never checked by default). Firefox history is removed with a
  **targeted** delete (bookmarks preserved). Passwords and bookmarks are never touched.
- **Disk space** — drive usage, large-file search, send to Recycle Bin (reversible).
- **Duplicates** — reliable detection (size → partial hash → SHA-256), "keep the newest" strategy.
- **Apps & startup** — uninstall via the publisher, reversible startup management.
- **Software updates** — via **winget** (official, signed source), never scraping.
- **System health** — disk health (SMART/WMI) + startup impact measured by Windows, live CPU/RAM,
  top memory consumers, Windows performance index (WinSAT).
- **Drivers** — read-only inventory; updates delegated to Windows Update.
- **Secure erase** — files and free space, with an honest SSD/NVMe warning.
- **Protection / Restore** — back up registry traces before cleaning, reversible restore.
- **NTFS analysis (Expert)** — explained traces, read-only.
- **Automation** — Safe/Privacy profiles via Task Scheduler, headless mode.
- **Multilingual** — full UI in **French, English, German, Spanish** (live switch; on first launch the
  Windows display language is followed automatically).
- **Accessibility** — visible keyboard focus, `AutomationProperties`, never status by color alone.

## Safety — non-negotiable

- Every deletion goes through `ISafePathValidator` (proven refusal of `C:\`, the user profile,
  system/personal folders, wildcards, traversal, UNC, junctions/reparse points) — see `TraceZero.SafetyTests`.
- Enumeration never follows junctions/links; locked files are reported, never forced.
- Reversible deletions (Recycle Bin) where possible; never auto-selected.
- **The app is never admin.** Elevation goes through a separate helper (`TraceZero.Elevated.exe`,
  `requireAdministrator` manifest, single-shot, closed vocabulary) that never trusts the UI.

## Stack & architecture

**.NET 10 · WPF · MVVM** (CommunityToolkit.Mvvm) · Generic Host (DI/logging). Code in English, UI in
French by default. Layered design (see `DECISIONS.md`):

```
Domain       pure models, no dependencies
Application  service interfaces
Engine       scan/clean, ISafePathValidator (portable, testable)
Windows      Windows providers (registry, WMI, EventLog)
Storage      drives, disk health (WMI)
Browsers     browser detection
Persistence  SQLite (history, restore vault, license)
Updater      signed-manifest verification
Elevated     separate admin helper
App          WPF (composition root only)
```

## Requirements

- Windows 10 (19041+) / Windows 11, x64.
- **.NET 10 SDK** (`winget install --id Microsoft.DotNet.SDK.10 -e`).

## Build & run

```powershell
dotnet build -c Release              # must be 0 warnings
dotnet test                          # full suite
dotnet run --project src\TraceZero.App\TraceZero.App.csproj
```

### Portable build

```powershell
build\scripts\publish-portable.ps1   # produces artifacts\portable\TraceZero-portable.zip
```

In portable mode, a `tracezero.portable` marker next to the exe makes all data live in `<folder>\Data` —
no hidden writes elsewhere.

### Release pipeline

```powershell
build\scripts\release.ps1            # restore + Release build/test + publish + SHA-256
```

External gates (Authenticode signing, antivirus scan, VM tests) are listed as manual — never simulated.
See `docs/testing/VM_TEST_MATRIX.md`.

## Tests

`dotnet test` covers: safety (`SafetyTests`, proven refuse-by-default), engine, Windows, browsers,
integration (SQLite, updater, golden dataset), and performance (streaming, cancellation, hashing).

## Project status

See **`PHASE_STATUS.md`** (source of truth for progress), **`DECISIONS.md`** (ADRs),
**`KNOWN_LIMITATIONS.md`** (honest limits), and **`CLAUDE.md`** (context guide). All features are shipped;
what remains depends on **external assets** (signing certificate, update endpoint, VM validation).

## License & distribution

Licensed under **[MIT](LICENSE)** (open source). Local-first, zero telemetry, zero ads. Support (PWYW) is
**voluntary**: cleaning and safety are complete in the free version.

- Disclaimer / no warranty: [`DISCLAIMER.md`](DISCLAIMER.md)
- Privacy: [`PRIVACY.md`](PRIVACY.md)
- Third-party notices: [`THIRD-PARTY-NOTICES.txt`](THIRD-PARTY-NOTICES.txt)
- Distribution strategy (signing, winget, donations): [`docs/distribution-strategy.md`](docs/distribution-strategy.md)
  and [`docs/RELEASE.md`](docs/RELEASE.md)

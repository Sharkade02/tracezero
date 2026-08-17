# Launch kit — TraceZero

Ready-to-post copy for launching TraceZero. Adapt tone per platform, and **read each community's
self-promotion rules first** (many require you to participate before posting, or have a dedicated day).

**Golden rule:** don't spam. Post as a maker sharing something honest, respond to every comment, and never
argue. Goal at day 0 = downloads + GitHub stars + trust. Donations follow adoption.

---

## AlternativeTo.net (highest ROI — do this first)

Add TraceZero as an alternative to **CCleaner**, **PrivaZer**, and **BleachBit**.

- **Name:** TraceZero
- **Tagline:** Local-first, privacy-first Windows cleaner — no ads, no dark patterns.
- **Description:**
  > TraceZero is an open-source (MIT) Windows cleaning, privacy and disk-space tool. It only deletes what
  > you choose, sends nothing to the cloud, and never invents scores or fake numbers. Browser history,
  > cookies and sessions are opt-in and never selected by default. It never runs as administrator.
  > Features: Windows cleanup, privacy-trace inspector, browser cleaning, disk-space manager, duplicate
  > finder, secure erase, driver & system-health view. Free, portable or installer, also on winget.
- **License:** Open Source / Free · **Platform:** Windows · **Tags:** cleaner, privacy, disk-cleanup

---

## Hacker News — "Show HN"

**Title:**
```
Show HN: TraceZero – a local-first, open-source Windows cleaner (no ads, no dark patterns)
```

**Body:**
```
I got tired of "PC cleaners" that show fake numbers, bundle junk, nag you to buy, and quietly send data
out. So I built the opposite.

TraceZero is a Windows cleaning / privacy / disk-space tool that is:
- Local-first: nothing is sent anywhere, no telemetry, no account.
- Honest: no value is shown until a real scan runs; it never invents "scores" or "boosts".
- Safe by construction: every deletion goes through a path validator that refuses by default (C:\, user
  profile, system folders, junctions, etc.), proven by a test suite. The app never runs as admin —
  elevation goes through a separate single-purpose helper.
- Opt-in for anything sensitive: browser history/cookies/sessions are never selected by default; Firefox
  history is cleared with a targeted SQL delete that preserves bookmarks.

It's .NET 10 + WPF, MIT-licensed. Portable zip, an installer, or `winget install TraceZero.TraceZero`.
Unsigned for now (no paid cert yet), so SmartScreen will warn — SHA-256 is published with each release.

Repo (screenshots + downloads): https://github.com/Sharkade02/tracezero

Happy to answer anything about the safety model, the honesty rules, or the architecture.
```

---

## Reddit

Target subreddits (check rules / self-promo days): r/privacy, r/PrivacyGuides, r/degoogle, r/software,
r/windows, r/opensource, r/pcmasterrace.

**Title:**
```
I built an open-source, privacy-first CCleaner alternative for Windows — no ads, no dark patterns [MIT]
```

**Body:**
```
After one too many "cleaners" that fake numbers, bundle extras and phone home, I made TraceZero.

- 100% local, no telemetry, no account, no ads.
- Never shows a number until a real scan runs; never invents "scores".
- Safe by design: refuses to touch system/personal folders (validated + tested); never runs as admin.
- Sensitive stuff (browser history/cookies/sessions) is opt-in, never ticked by default. Firefox history
  is cleared while keeping your bookmarks.
- Free, MIT, portable or installer, and on winget.

It's not signed yet (no paid certificate), so Windows SmartScreen will show a warning — I publish the
SHA-256 of every release so you can verify the download.

GitHub (screenshots, downloads, source): https://github.com/Sharkade02/tracezero

Feedback very welcome — especially on the safety model. If you like it, a GitHub star really helps.
```

---

## Product Hunt

- **Name:** TraceZero
- **Tagline (60 chars max):** `Local-first, honest Windows cleaner — no ads, no dark patterns`
- **Description:**
  > TraceZero cleans, protects your privacy and manages disk space on Windows — 100% locally, open source
  > (MIT), with no ads, no dark patterns and no fake numbers. It only deletes what you choose and never
  > runs as admin.
- **First comment (maker):** short version of the Show HN body above.
- Prepare: the 2 screenshots (`docs/screenshots/`), the logo, and a 30–60s screen recording if possible.

---

## Outreach — YouTubers / blogs (privacy + Windows utilities)

A privacy-first, open-source CCleaner alternative is exactly their content. Keep it short and personal.

**Email/DM template:**
```
Subject: Open-source, privacy-first CCleaner alternative (MIT) — TraceZero

Hi <name>,

I built TraceZero, a free & open-source (MIT) Windows cleaner/privacy tool — local-first, no ads, no dark
patterns, and it never invents fake "scores". It only deletes what the user chooses and never runs as admin.

Given your videos on <topic>, I thought it might be a fit. Repo + downloads + screenshots:
https://github.com/Sharkade02/tracezero

No strings attached — happy to answer anything or send more material. Thanks for considering it!
```

Targets to consider: Chris Titus Tech, The PC Security Channel, and Windows/privacy-focused blogs
(gHacks, MajorGeeks, Softpedia listings).

---

## Awesome-lists (GitHub PRs — free, durable)

Submit TraceZero to relevant curated lists, e.g. `awesome-windows`, `awesome-privacy`,
`awesome-open-source`. One line, following each list's format.

---

## Reminders

- Add a **GitHub star** call-to-action wherever you post (stars → credibility → SignPath eligibility).
- Keep the release notes' SmartScreen explanation handy — it preempts the #1 objection.
- Don't over-post: a couple of well-targeted posts beat spraying every subreddit the same day.

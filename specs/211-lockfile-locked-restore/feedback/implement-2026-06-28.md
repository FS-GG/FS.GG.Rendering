---
phase: implement
date: 2026-06-28
severity: minor            # none | minor | major | blocker
---

## Process friction

Three points, all resolved in-phase (none blocking):

1. **The board item / spec name "NU1603", but the repo's real silent-substitution code is NU1601.**
   The whole feature (FR-004, contract `restore-policy.md`, quickstart C, tasks T006/T010) is written
   around `NU1603`. Empirically (T006), for this repo's **centrally-managed single-version pins** a
   pin below what the feed carries surfaces as **NU1601** ("Dependency specified was X but ended up
   with Y"), and an unavailable **exact** pin is **NU1102** — *not* NU1603. NU1603 proper is a
   transitive-resolution code that central single-version pinning doesn't naturally produce. The
   feature's goal (silent higher-version substitution fails the build) is still fully met — NU1601 was
   observed promoted to error — but the spike script first looked only for the literal string `NU1603`
   and reported "UNDETERMINED" until I generalized it to the NU16xx substitution class. **Lesson: spec
   warning codes that name one member of a family should say "NU1603 (or its NU1601 direct-reference
   analog)"; verification must grep the class, not the literal.**

2. **The R3 empirical question resolved in the *cheaper* direction — props alone suffice.** The plan
   treated "does `TreatWarningsAsErrors` promote restore-phase NU16xx?" as genuinely open, with a
   `-warnaserror:NU1603` gate-step fallback (GR2) on standby. The real restore showed the repo's
   existing `TreatWarningsAsErrors=true` already promotes the substitution warning (`error NU1601:
   Warning As Error`), so **T010 was a no-op** and the gate step stays minimal. Good outcome; worth
   recording that the fallback was *checked and not needed*, not skipped.

3. **The new Build.Tests case tripped FS3261 nullness-as-error on first compile.** The repo's
   `Nullable=enable` + `TreatWarningsAsErrors` makes `DirectoryInfo.Parent` / `Path.GetDirectoryName`
   returns hard errors unless pattern-matched. A `DirectoryInfo` walk-up written the obvious way (`if
   isNull dir`) fails to build. Fixed with `(dir: DirectoryInfo | null)` + `match`. Minor, but any new
   `.fs` test that does filesystem walking in this repo will hit it — worth a one-liner in the
   testing skill.

## Generalizable code

None as library surface — Tier 2, no product `.fs`/`.fsi`. The only code artifact is the deterministic
policy test `tests/Build.Tests/RestoreLockTests.fs` (parses the slnx for the LOCKED set, asserts
lockfile coverage + the excluded-lane boundary + the root props policy). It is repo-specific (hard-codes
the 38-project expectation and the excluded lanes) and intentionally not generalized; the *pattern*
(assert lockfile coverage == solution membership) is the reusable idea if another FS-GG repo adopts
locked restore.

## What went well

The scope boundary was achieved structurally with almost zero per-project work: the 4 standalone
samples already shadow the root `Directory.Build.props`, so the lock policy never reaches them (zero
edits), and only `tests/Package.Tests` needed an explicit opt-out. The "confirm-by-inspection that the
samples shadow root" instruction (T004) made this a 2-minute check instead of a risk.

# Quickstart — Symbology Legibility Linter

Validation/run guide for the M7 linter thread. Details live in
[data-model.md](./data-model.md), [contracts/legibility-api.md](./contracts/legibility-api.md), and
[research.md](./research.md); this file is the runnable path that proves the feature works.

## Prerequisites

- .NET SDK with `net10.0`, repo cloned at `FS.GG.Rendering` root.
- No GL, no window system, no network — the linter is pure CPU logic (fully headless).

## Build

```bash
dotnet build src/Symbology/Symbology.fsproj          # the linter compiles into the existing pure package
# or the whole solution:
dotnet build FS.GG.Rendering.slnx
```

## FSI smoke (the early end-to-end check — do this first, before US1/US2 build-out)

```fsharp
#r "src/Symbology/bin/Debug/net10.0/FS.GG.UI.Scene.dll"
#r "src/Symbology/bin/Debug/net10.0/FS.GG.UI.Symbology.dll"
open FS.GG.UI.Symbology

// a tiny within-capacity board
let board =
    [ { Symbology.defaultToken with R = 30.0; Faction = Ally;  Klass = Heavy; Sigil = Ring }
      { Symbology.defaultToken with R = 30.0; Faction = Enemy; Klass = Scout; Sigil = Fang } ]

let report = Legibility.score board
// expect: report.Verdict = Clean ; report.Findings = [] ; report.Usage has one row per channel

// an overloaded board: > 7 distinct factions
let tooManyFactions =
    [ for i in 0 .. 7 ->
        { Symbology.defaultToken with R = 30.0; Faction = Custom { R = byte i; G = 0uy; B = 0uy; A = 255uy } } ]
Legibility.score tooManyFactions
// expect: one Warning finding on Channel.Faction (8 distinct > capacity 7), Verdict = HasWarnings

// a degenerate unit does not crash
Legibility.score [ { Symbology.defaultToken with R = 0.0 } ]
// expect: one Error finding on Channel.Size for unit 0, no exception
```

> The `Color` shape in the snippet matches `FS.GG.UI.Scene.Color`; adjust to the actual record fields if
> they differ. The point of the smoke is only: surface loads, `score`/`scoreAnimated` run, a `Report`
> comes back.

## Run the tests

```bash
dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj          # core linter behaviour (US1 + edges)
dotnet test tests/SymbologyBoard.Tests/SymbologyBoard.Tests.fsproj # approved roster lints clean (FR-014)
```

## Refresh the surface baseline (Tier 1)

```bash
dotnet fsx scripts/refresh-surface-baselines.fsx
git diff --stat readiness/surface-baselines/
# expect: ONLY readiness/surface-baselines/FS.GG.UI.Symbology.txt changed (gains Legibility.* entries);
#         every other baseline shows zero drift (SC-007).
```

## Skill-parity check (loop integration)

```bash
dotnet fsx scripts/check-agent-skill-parity.fsx
# expect: PASS — the CRITIQUE-step linter guidance is present and consistent across all skill trees (SC-008).
```

## Per-Success-Criterion validation

| SC | How to validate | Where |
|---|---|---|
| **SC-001** reproducible | a determinism test scores the same set twice and asserts equal reports; FSI re-run matches | `LegibilityTests.fs` C7 |
| **SC-002** overload, no false positives | per categorical channel, a crafted over-capacity set yields a finding naming that channel; a within-capacity board yields none | `LegibilityTests.fs` C1–C3 |
| **SC-003** out-of-domain + continues | a set with one out-of-band/zero-size unit yields a finding and the scan completes | `LegibilityTests.fs` C4–C6 |
| **SC-004** edge cases no crash | empty set / all-identical / degenerate each handled (clean, clean, finding) | `LegibilityTests.fs` C9–C10, C5 |
| **SC-005** roster clean | `score (roster \|> List.map Roster.mapUnit)` ⇒ `Clean`, `[]` | `SymbologyBoard.Tests` C13 |
| **SC-006** machine-actionable | a test filters findings by channel + severity and derives `Verdict` without text parsing | `LegibilityTests.fs` C8 |
| **SC-007** zero drift | `git diff` after baseline refresh touches only the symbology baseline; existing symbology tests stay green | baseline refresh + full `dotnet test` |
| **SC-008** skill parity | `check-agent-skill-parity.fsx` PASS; CRITIQUE step references the linter | parity script + skill diff |

## What this feature does **not** add (FR-016)

The Badge/Ring alternative grammars and label text (the other two M7 threads) remain deferred. No new
project, no new third-party dependency, no change to the `token`/`animate`/`gallery`/`filmstrip`
rendering behaviour.

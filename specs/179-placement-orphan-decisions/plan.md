# Implementation Plan: Placement & Orphan Decisions (Code-Health Refactoring Phase 2)

**Branch**: `179-placement-orphan-decisions` | **Date**: 2026-06-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/179-placement-orphan-decisions/spec.md`

## Summary

Three owner-confirmed placement/ownership calls, each carried out to a green build + green test
state and diffed against a captured baseline:

1. **Relocate `Rendering.Harness`** (production CLI, `OutputType=Exe`, ~18.4k lines across 39 files)
   from `tests/Rendering.Harness` → `tools/Rendering.Harness`, rewriting every genuine reference
   (1 `.slnx`, 1 dependent test `ProjectReference`, 4 linked `TestAssertions.fs` includes, 3 helper
   scripts, harness-internal command literals in `Compositor.fs`/`ValidationLanes.fs`/`Live.fs`, 1
   Feature 170 lane-test assertion, 5 FSX evidence scripts, 1 skill doc). **Tier 2** — no package
   surface.
2. **Retire & unpublish `FS.GG.UI.Input`** (`src/Input/`, ~1,852 lines, superseded by
   `src/KeyboardInput/`): delete `src/Input/` + `tests/Input.Tests/`, de-list both from the `.slnx`,
   drop the surface-baseline manifest row and the `FS.GG.UI.Input.txt` baseline file. **Tier 1**
   (constitution) — the single intentional public-package-surface removal.
3. **Retire `src/Color/` while preserving `ColorPolicy`** — research refined US3: `Contrast`
   (carrying the `Role`/`Verdict`/`ContrastResult` types) is a **live** dependency of both
   `ColorPolicy` and `Controls.Tests/Feature108ThemingTests`, so only `Palettes` is truly dead.
   Move `Contrast.fs`/`Contrast.fsi` + `ColorPolicy.fs` into a **new non-packed `src/ColorPolicy`
   project** (`IsPackable=false`, no surface baseline, `InternalsVisibleTo Controls.Tests`),
   delete `Palettes.*` + `tests/Color.Tests/` + `src/Color/`, and update the two policy scripts'
   `#load` paths. **Tier 2** — no shipped package surface (Color never shipped; no baseline existed).

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This feature changes no runtime behavior of the shipped product; it relocates one project and
> removes/relocates unreferenced code. It carries **no defect/root-cause hypothesis to confirm
> against a running app**, so the early-live-smoke clause is N/A. The honest "real evidence" is the
> existing regression machinery: a clean `dotnet build` of the `.slnx` plus the full `dotnet test`
> run, captured as a baseline **before any change** and diffed after each of the three stories.
> `/speckit-tasks` MUST place that baseline capture as the first Foundational task and gate every
> story against it (the two documented package-feed reds stay the only non-green entries).

## Technical Context

**Language/Version**: F# on .NET `net10.0` (exclusive stack per constitution).

**Primary Dependencies**: SkiaSharp (GL backend) — not touched. Test stack: the existing
xUnit-style runners with `Program.fs` entry points across `tests/*`; the `Rendering.Harness` CLI.

**Storage**: N/A. No persisted artifact changes; golden/readiness files stay byte-identical except
the deliberately-removed `readiness/surface-baselines/FS.GG.UI.Input.txt`.

**Testing**: Full `dotnet test` over `FS.GG.Rendering.slnx`. Key regression gates: the surface-drift
check (`SurfaceAreaTests` + `build/Governance/PackageSurface.fs`), the Feature 170 retained-inspection
lane test, the validation-lane/skill-parity/feed-refresh script lanes, and the
`Controls.Tests` color suites (Feature 108 theming-contrast, Feature 127 color-policy, Feature 131
Ant-pattern docs) that pin the relocated `Contrast`/`ColorPolicy` behavior.

**Target Platform**: Linux (primary dev/CI); the refactor is platform-neutral.

**Project Type**: F# UI framework / rendering library — single multi-project solution
(`FS.GG.Rendering.slnx`) with `src/*` packages, `tests/*` test projects, and (new) `tools/*` tooling.

**Performance Goals**: No regression. Pure relocation/removal; no hot path touched.

**Constraints**:
- Exactly **one** public package surface changes — `FS.GG.UI.Input` removed (FR-006, SC-004). No
  other `FS.GG.UI.*` surface baseline changes; `src/ColorPolicy` is `IsPackable=false` with no
  baseline (FR-010).
- Behavior preserved throughout; build + full test suite match baseline after each story (FR-011,
  FR-012, SC-005). Relocated `Contrast`/`ColorPolicy` keep the `FS.GG.UI.Color` namespace so
  consumer code is **edit-free** (byte-identical behavior; SC-006).
- No new project/module cycle. `src/ColorPolicy` depends only on `FS.GG.UI.Scene` (matching the old
  `src/Color` dep), so the package graph gains no edge beyond `Controls.Tests → src/ColorPolicy`
  (replacing the old test-only `Controls.Tests → src/Color`).
- `tests/` ends with **zero** `OutputType=Exe` production CLIs (SC-001); `tools/Rendering.Harness`
  is the only relocated CLI. (Test projects' own `OutputType=Exe` runners are unaffected.)

**Scale/Scope** (from research, verified against the working tree):
- Harness: **39 files / ~18,359 lines**; **~22 genuine reference sites** across 11 files + 5 FSX
  evidence scripts + 1 skill doc (full map in `contracts/harness-path-map.md`).
- Input: `src/Input/` (2 source files, ~1,852 lines) + `tests/Input.Tests/` (3 files); **1** baseline
  file + **1** manifest row; **0** production consumers.
- Color: `src/Color/` 562 lines (Contrast 96+54, Palettes 91+40, ColorPolicy 281). Relocate
  Contrast+ColorPolicy (~431 lines incl. `.fsi`); delete Palettes (~131) + `tests/Color.Tests/`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec → FSI → Semantic Tests → Impl | ✅ | No **new** public surface is designed. `Contrast.fsi` moves verbatim (existing curated signature); `ColorPolicy` stays `module internal` with no `.fsi` (its established Feature 127 shape). The one surface *removal* (`FS.GG.UI.Input`) is exercised by the existing surface-drift gate. Relocated behavior is pinned by the existing Feature 108/127/131 semantic suites. |
| II. Visibility Lives in `.fsi` | ✅ | No `.fs` gains a `private`/`internal`/`public` modifier on a top-level **binding**. `ColorPolicy` keeps its `module internal` modifier (allowed precedent — `Internal/AttrKeys.fs`). `Contrast.fsi` is preserved as-is. The `FS.GG.UI.Input.txt` baseline is **removed** (its package is gone), keeping the gate internally consistent (FR-006). |
| III. Idiomatic Simplicity | ✅ | Pure file/project relocation and deletion. No new abstraction, operator, SRTP, reflection, CE, type provider, or active pattern introduced. |
| IV. Elmish/MVU boundary | ✅ | N/A — no stateful/I/O workflow added or altered. |
| V. Test Evidence | ✅ | Behavior is preserved → evidence = existing suites stay green; the gate is *no new red*. Baseline-capture task makes the green state explicit; each story is diffed against it. `Contrast`'s behavior remains pinned by Feature 108 (ratio/verdict reference values) + Feature 127 (drives `Contrast.ratio`/`compositeOver`/`verdict` through `evaluatePairing`). The granular `tests/Color.Tests` (ContrastTests/PaletteTests) is intentionally removed with the orphan — a **disclosed** coverage reduction; re-homing `ContrastTests.fs` into `Controls.Tests` is a noted bounded follow-up (see research R3). No synthetic evidence introduced. |
| VI. Observability & Safe Failure | ✅ | The relocated harness scripts/lanes preserve their existing fail-loud diagnostics; no critical-path error handling changes. |
| Change Classification | ✅ | **Mixed, all declared in the spec.** US1 (harness move) = Tier 2 (internal placement, no surface). US2 (`FS.GG.UI.Input` removal) = **Tier 1** (removes public package surface) → requires the full chain: spec ✓, plan ✓, surface-baseline update (removal) ✓, migration guidance (the existing `docs/bridge/package-deprecation-notice.md` already lists it), test evidence (gate stays consistent) ✓. US3 (Color/ColorPolicy) = Tier 2 (Color never shipped; relocation keeps everything internal/non-packed). |

**Result: PASS.** No violations; Complexity Tracking left empty.

## Project Structure

### Documentation (this feature)

```text
specs/179-placement-orphan-decisions/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — the three placement/ownership decisions + alternatives
├── data-model.md        # Phase 1 — the four relocated/removed entities + their reference graphs
├── quickstart.md        # Phase 1 — baseline-capture + per-story build/test validation guide
├── contracts/           # Phase 1 — the "contracts" this refactor must hold invariant
│   ├── harness-path-map.md          # every old→new harness reference, by category
│   ├── package-surface-changes.md   # the single allowed surface delta + the gate invariant
│   └── colorpolicy-relocation.md    # Contrast+ColorPolicy new home, IVT, namespace, scripts
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
tools/                                  # NEW top-level dir (first resident)
└── Rendering.Harness/                  # MOVED from tests/Rendering.Harness (verbatim, 39 files)
    ├── Rendering.Harness.fsproj        #   OutputType=Exe; no package surface
    ├── TestAssertions.fs               #   canonical copy linked into 4 test projects
    ├── Compositor.fs / ValidationLanes.fs / Live.fs   # internal command literals rewritten
    └── … (Domain, RunPlan, Evidence, Perf, PackageFeed, SkillParity, Cli, …)

src/
├── Scene/                              # unchanged base (defines Color/Paint used by Contrast)
├── ColorPolicy/                        # NEW non-packed project (IsPackable=false, no baseline)
│   ├── ColorPolicy.fsproj              #   <InternalsVisibleTo Include="Controls.Tests" />
│   ├── Contrast.fsi                    #   MOVED verbatim from src/Color (namespace FS.GG.UI.Color)
│   ├── Contrast.fs                     #   MOVED verbatim
│   └── ColorPolicy.fs                  #   MOVED verbatim (module internal, depends on Contrast)
├── Input/                              # DELETED (FS.GG.UI.Input, ~1,852 lines, orphan)
├── Color/                              # DELETED (Palettes dead; Contrast/ColorPolicy relocated)
├── KeyboardInput/                      # unchanged live keyboard path (SkiaViewer/Controls/Elmish)
└── …

tests/
├── Rendering.Harness.Tests/           # ProjectReference + Feature170 assertion rewritten to tools/
├── Layout.Tests/ Scene.Tests/ SkiaViewer.Tests/ Controls.Tests/   # 4 linked TestAssertions.fs includes → tools/
│   └── Controls.Tests/                #   ProjectReference src/Color → src/ColorPolicy (keeps IVT to ColorPolicy)
├── Input.Tests/                       # DELETED (with src/Input)
└── Color.Tests/                       # DELETED (ContrastTests/PaletteTests — coverage note in research R3)

scripts/
├── check-agent-skill-parity.fsx / run-validation-lanes.fsx / refresh-local-feed-and-samples.fsx
│                                      # harness arg path tests/… → tools/…
├── refresh-surface-baselines.fsx      # drop the "FS.GG.UI.Input","Input" manifest row
├── validate-design-system-template.fsx / generate-policy-report.fsx
│                                      # #load src/Color/{Contrast,ColorPolicy}.fs → src/ColorPolicy/…
└── …

readiness/surface-baselines/
└── FS.GG.UI.Input.txt                 # DELETED (its package is gone; gate stays consistent)

FS.GG.Rendering.slnx                   # harness path → tools/; remove Input+Input.Tests+Color+Color.Tests; add src/ColorPolicy
```

**Structure Decision**: Three placements, each chosen to satisfy "one source of truth + no
unintended package-surface change" (full rationale and alternatives in `research.md`):

- **Harness → `tools/Rendering.Harness`** (FR-001, fixed by the spec). `tools/` is created as the
  first home for executable tooling so `tests/` holds only test projects (SC-001). Every reference
  is rewritten; relative-path includes/`#r` get their depth re-computed per consuming file
  (`contracts/harness-path-map.md`).
- **`FS.GG.UI.Input` → deleted** (clean orphan, zero production consumers). The surface-baseline
  manifest row and `FS.GG.UI.Input.txt` are removed together so the drift gate sees no package
  without a baseline and no baseline without a package (`contracts/package-surface-changes.md`).
- **`Contrast`+`ColorPolicy` → new non-packed `src/ColorPolicy`** (owner-selected). `IsPackable=false`
  + no baseline ⇒ zero shipped surface (FR-010). Keeping the `FS.GG.UI.Color` namespace makes all
  consumers edit-free (Feature 108 `FS.GG.UI.Color.Contrast.*` and Feature 127/131 `ColorPolicy.*`
  resolve unchanged). `InternalsVisibleTo Controls.Tests` follows `ColorPolicy` to its new home so the
  internal module stays reachable; `Palettes.*` + `tests/Color.Tests/` are deleted as dead surface.

## Complexity Tracking

> No constitution violations — section intentionally empty.

## Implementation Status — COMPLETE (2026-06-21)

All 35 tasks complete (`tasks.md` all `[X]`); all three stories shipped on branch
`179-placement-orphan-decisions`, each diffed against the captured baseline.

| Story | Commit | Result |
|-------|--------|--------|
| Setup + Foundational (T001–T004) | — | Baseline captured: 18 projects, 16 green, **2 documented package-feed reds** (Package.Tests, ControlsGallery); early-live-smoke recorded **N/A**; reference inventory re-verified. |
| **US1** — harness → `tools/` (T005–T016) | `179 US1: relocate Rendering.Harness CLI to tools/` | 39 files moved; all genuine refs rewritten; build 0/0; 5 harness-touching test projects = baseline; skill-parity + validation-lanes (`rendering-harness` lane) green; **SC-002 grep clean**. Caught + fixed **one ref the path-map missed** (`Feature168SkillInventoryTests.fs` hardcoded `tests/Rendering.Harness/SkillParity.fsi`). |
| **US2** — retire `FS.GG.UI.Input` (T017–T022) | `179 US2: retire & unpublish FS.GG.UI.Input` | `src/Input` + `tests/Input.Tests` deleted; baseline + manifest row removed together; build 0/0; surface gate `Package.Tests` 8/98/106 = baseline; **only `FS.GG.UI.Input.txt` removed**, manifest now 12 packages. |
| **US3** — retire `src/Color`, keep `ColorPolicy` (T023–T032) | `179 US3: retire src/Color, preserve ColorPolicy` | New non-packed `src/ColorPolicy` (Contrast+ColorPolicy moved verbatim, namespace+IVT preserved); `Palettes`/`src/Color`/`tests/Color.Tests` deleted; build 0/0; Feature 108/127/131 = 25/0; policy reports **byte-identical**; **no Color surface change**. |
| Polish (T033–T035) | — | Post-change baseline: **16 projects, 14 green, 2 red** (same documented pair, byte-identical) — no regression. SC-001…SC-006 all verified in `readiness/post-change.md`. |

**Evidence:** `readiness/baseline.md` (pre-change + foundational annotations) and
`readiness/post-change.md` (post-change + SC verification). Net source reduction **−2,595**
lines of `.fs`/`.fsi`, all unreferenced; no live production code removed.

**One contract deviation, recorded honestly:** `scripts/generate-policy-report.fsx` carries **no**
`#load "src/Color/…"` (it shells out to the Feature 127 env-gated evaluator), so US3/T029 updated
only `scripts/validate-design-system-template.fsx`.

**US3 premise correction (found at merge, fixed before merge).** The plan/research/contract
classified `src/Color` as **Tier 2 — "Color never shipped, no package surface."** That was wrong:
`src/Color/Color.fsproj` was `IsPackable=true` / `PackageId=FS.GG.UI.Color` (shipping `0.1.36-preview.1`),
the local feed holds 38 packed `FS.GG.UI.Color` versions, and **4 package-consuming samples**
(SampleApps, AntShowcase, ControlsGallery, SecondAntShowcase) carried a `FS.GG.UI.Color`
`PackageReference`. (The *surface-drift gate* had no `FS.GG.UI.Color.txt` baseline — a gate gap, not
proof it never shipped.) Investigation showed those 4 pins were **inert**: no sample imports any
`FS.GG.UI.Color` symbol, no surviving `FS.GG.UI.*` package depends on it (no `src/*` referenced
`src/Color`; `FS.GG.UI.Controls`'s nuspec lists no Color dep). So the reconciliation (owner choice:
"fix US3 first") was to **remove the 4 dead `FS.GG.UI.Color` pins**. All 4 samples then build + test
green on restore without the pin (SampleApps 25/0, AntShowcase 88/0, SecondAntShowcase 171/1-skip,
ControlsGallery 2/32 = its documented red), proving the pins were dead and `FS.GG.UI.Color` is now a
genuine zero-consumer orphan. Like `FS.GG.UI.Input`, its already-packed feed `.nupkg`s are retained
(frozen); the source is intentionally gone, so no new version can be packed — acceptable now that no
consumer references it. Re-homing `ContrastTests.fs` into `Controls.Tests` remains the noted
out-of-scope follow-up (research R3).

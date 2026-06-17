# Implementation Plan: Games + Productivity Sample Apps — curated G2 slice

**Branch**: `134-sample-apps-g2` | **Date**: 2026-06-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/134-sample-apps-g2/spec.md`

## Summary

Build a curated, representative slice of the archived games + productivity sample apps — **three games**
(Tetris, Snake, Pong) and **three productivity apps** (Kanban board, Todo/task manager, Calendar
scheduler) — as runnable **`FS.GG.UI.*`-package consumers**, reusing the deterministic seeded-evidence
harness the Controls Gallery (G1, feature 123) established. Each sample is a pure MVU core (`Model`/`Msg`/
`update`/`view` + a seeded `FrameInput` script + an authored expected outcome) wired into the framework's
`InteractiveAppHost`; a thin executable turns any sample into a live window (`interactive`, GL-gated) or a
headless deterministic evidence run (`evidence --seed N`, the CI path) via the public
`ControlsElmish.Perf.runScript` + `Viewer.captureScreenshotEvidence` surfaces. A shared Core harness
supplies the package-only evidence record, a **closure-erased sample registry**, and a **coverage +
backlog report** that lists per-sample control/input coverage and dispositions all **22** archived
game/productivity specs as `adopted` (the six built here) or `deferred` (the ~16 backlog). An Expecto
suite proves: each sample builds and meets its authored acceptance outcome, same-seed runs are
byte-identical, every record carries a non-empty `NotAuthoritativeFor`, a no-GL host degrades-and-
discloses, and the 22-spec backlog is fully accounted.

The new capability over G1 is the **persistent, deterministic game loop**: game time advances only through
injected `FrameInput.Tick` deltas mapped by the host's `Tick` to a step message (gravity / movement), and
all in-game randomness (piece bag, food placement, serve direction) comes from a **pure, `--seed`-driven
PRNG** — no wall-clock, no `System.Random`. Productivity apps add forms with validation that rejects
invalid input and inline-edit that commits to the data model.

This is **Workstream G2 only**. G1 (Controls Gallery) is shipped; G3 (Ant restyle + enterprise templates,
dependency-gated on F/D) and G4 (perf-corpus wiring) are out of scope.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`LangVersion=latest`, `Nullable=enable`,
`TreatWarningsAsErrors=true` including `FS0078`) — same toolchain settings as G1.

**Primary Dependencies** (consumed as packed NuGet packages from `~/.local/share/nuget-local/`, **never**
as project references into `src/`):

- `FS.GG.UI.Controls` — control builders, `Catalog`, `Control.renderTree`
- `FS.GG.UI.Controls.Elmish` — `InteractiveAppHost<'M,'Msg>` (incl. `Init`/`Update`/`View`/`Theme`/
  `MapKey`/`MapPointer`/**`Tick`**/`MapKeyChord`/`OnFrameMetrics`/`Diagnostics`), `runInteractiveApp`,
  `Perf.runScript`, `FrameInput<'Msg>` (`Tick`/`Key`/`Pointer`/`Idle`), `FrameMetrics`
- `FS.GG.UI.SkiaViewer` — `Viewer.captureScreenshotEvidence`, `ViewerOptions`, `ViewerPresentMode`,
  `ScreenshotEvidenceRequest`/`Result`, `ViewerRenderTargetPng`
- `FS.GG.UI.Testing` — `ScreenshotEvidenceResult` disclosure fields
- `FS.GG.UI.KeyboardInput` — `ViewerKey`, `KeyModifiers`
- `FS.GG.UI.Color`, `FS.GG.UI.DesignSystem`, `FS.GG.UI.Themes.Default` — `Color`, `Theme`/`Theming`,
  Light/Dark + accents (the existing-themes-only constraint, FR-015)
- Test-only: `Expecto`, `Microsoft.NET.Test.Sdk`, `YoloDev.Expecto.TestSdk`

All `FS.GG.UI.*` packages are present at `0.1.0-preview.1` in the local feed (verified).

**Storage**: Filesystem only — per-sample evidence under `artifacts/sample-apps/<seed>/<sample-id>/
{run.json,summary.md,frame.png,state.txt}` (gitignored). The committed coverage + backlog report lives
under the sample tree. No database.

**Testing**: Expecto via `dotnet test`, **outside the default test tier** (not added to
`FS.GG.Rendering.slnx`). Suites: build-and-outcome (FR-009/SC-001), determinism (FR-006/SC-002),
degrade-and-disclose (FR-008/SC-003), coverage + backlog honesty (FR-011/FR-012/SC-004/SC-005),
public-surface-only consumption (FR-010/SC-006), and bounded-loop / validation (SC-007). Precedent:
`samples/ControlsGallery/ControlsGallery.Tests/*` (Coverage/Determinism/Degrade suites).

**Target Platform**: Linux (X11/GL) for interactive mode; headless evidence + coverage run anywhere
including no-display/no-GL CI hosts (degrade-and-disclose path), mirroring G1.

**Project Type**: Sample **desktop applications** (consumers of the framework) — one shared pure Core
library + one thin executable + one co-located Expecto test project, in a standalone `samples/SampleApps/`
tree decoupled from the main solution.

**Performance Goals**: Not a hot-path feature. The quantitative gates are **byte-identical evidence across
two same-seed headless runs (SC-002)** and **every game reaching its terminal state in a bounded number of
steps (SC-007)** — no unbounded/hanging loop.

**Constraints**:

- Public package surface only — **no project references into `src/`, no internal access** (FR-010/SC-006).
- Existing controls/themes only — Light/Dark + accents; **no Ant/Fluent/Material dependence**, no new
  controls/themes/kits, **no public product surface change** (FR-015) ⇒ both drift gates untouched.
- Deterministic headless mode — **no wall-clock, no `System.Random`/`Math.random`**; injected `Tick`
  deltas + a pure `--seed`-driven PRNG only (FR-006).
- Headless evidence is the CI-facing path; **the required gate never depends on a display or GL**
  (FR-014). Interactive mode is GL-gated and advisory.
- Imported identifiers rebranded `FS.Skia.UI.*` → `FS.GG.UI.*`, recorded in provenance (FR-013).
- Each game reaches a defined terminal state; each productivity app rejects invalid form input without
  committing it (SC-007).

**Scale/Scope**: 6 curated samples (3 games + 3 productivity), 1 shared harness, 1 coverage+backlog report
dispositioning all 22 archived specs, 0 new packages, **0 public-surface/token-baseline changes**. One
sample tree (Core + App + Tests). **Medium** — the new loop/PRNG determinism and the 6-sample
heterogeneous registry are the only real novelty over G1.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

**Change Classification**: **Tier 2 (additive consumer)** — identical posture to G1. The feature adds a new
sample tree + its tests; it makes **no change to the product public API surface**, adds **no new external
runtime dependency** to the shipped packages, and touches **no `.fsi` or surface-area baseline and no
design-token**. (Spec "Out of Scope" excludes public-surface changes.) Tier 2 requires spec + tests, which
this plan provides.

| Principle | Gate | Status |
|-----------|------|--------|
| **I. Spec → FSI → Semantic Tests → Implementation** | Public surface sketched + validated by use before `.fs` bodies | **PASS (adapted, as G1).** The samples are *consumers*; their "FSI surface" is the framework's already-published `.fsi`. The plan exercises that surface through the headless CLI and Expecto tests as a downstream app would (`runInteractiveApp`, `Perf.runScript`, `captureScreenshotEvidence`, `InteractiveAppHost.Tick`). No new public product surface is sketched. |
| **II. Visibility lives in `.fsi`, not `.fs`** | Every **public** module has a curated `.fsi`; no access modifiers in `.fs` | **PASS (not in scope).** The samples expose **no public package surface** (`IsPackable=false`, FR-010) — their modules are application-internal, so Principle II's `.fsi` requirement does not apply. No `private`/`internal`/`public` modifiers on top-level bindings (keeps `FS0078`-as-error clean). |
| **III. Idiomatic simplicity** | Plainest F#; exotic features justified | **PASS.** Records, unions, lists, pattern matching, pure functions, MVU. The closure-erased registry (research R2) and the LCG PRNG (R4) are plain F# — no SRTP, reflection, type providers, or custom operators. |
| **IV. Elmish/MVU is the boundary for stateful/I-O work** | Stateful/interactive features modeled as `Model`/`Msg`/pure `update` + edge interpreter | **PASS (core fit, strengthened).** Every game and app is MVU; `update` is pure; the persistent game loop is a pure `Tick`→step reduction driven by injected deltas (no wall-clock in `update`). I/O (window, screenshot, file write) happens only at the App edge; headless mode drives the same `update` via pure `Perf.runScript`. |
| **V. Test evidence is mandatory** | Tests fail before / pass after; real evidence preferred; synthetic disclosed | **PASS.** Build-and-outcome, determinism, degrade, and coverage/backlog suites are behavior gates. Real GL screenshot evidence is captured where available; a no-GL host records a **disclosed degrade** (`ProvesScreenshot=false` + reason), never a fabricated pass. The authored expected outcomes (R6) stand in for the absent archive specs and are **disclosed** in PROVENANCE; no `Synthetic`-token test is needed (the outcomes are real, deterministic assertions, not mocks). |
| **VI. Observability and safe failure** | Structured diagnostics; fail-fast or degrade explicitly; GL smoke distinguishes defect vs missing window-system | **PASS.** Every evidence record carries a non-empty `NotAuthoritativeFor` (FR-007); GL/display unavailability is reported via `ScreenshotEvidenceResult.{BlockedStage,UnsupportedHostReason}` and yields a clean, non-hanging exit (FR-008); a seed/script mismatch fails loudly with a disclosed reason rather than silently diverging. |

**Engineering Constraints check**: `net10.0` ✓ · SkiaSharp-over-GL inherited via packages, no Vulkan ✓ ·
consumes packed packages from `~/.local/share/nuget-local/` ✓ · one semantic control set, no per-theme
forks ✓ · package identity untouched (consumer only) ✓ · sample kept outside the default test tier ✓.

**Result: PASS — no violations. Complexity Tracking is empty.**

## Project Structure

### Documentation (this feature)

```text
specs/134-sample-apps-g2/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — resolved decisions (R1–R9)
├── data-model.md        # Phase 1 — entities: SampleEntry, game/app state, script, outcome, record, coverage/backlog
├── quickstart.md        # Phase 1 — build/run/verify guide (V1–V8)
├── contracts/           # Phase 1
│   ├── cli.md                 #   list | interactive | evidence | coverage subcommands + exit codes
│   ├── sample-registry.md     #   the closure-erased SampleEntry shape the Core exposes to tests
│   ├── evidence-record.md     #   per-sample evidence-record schema + the sample-outcome field + disclosure rule
│   └── coverage-backlog.md    #   coverage+backlog report schema, the 22-spec enumeration, honesty-check rules
├── checklists/
│   └── requirements.md  # (created by /speckit-specify)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

The sample is a standalone consumer tree, **not** added to `FS.GG.Rendering.slnx`, built/tested on its own
via a local `nuget.config` pointing at `~/.local/share/nuget-local/` — exactly as G1.

```text
samples/
└── SampleApps/
    ├── nuget.config                      # local feed → ~/.local/share/nuget-local/ (copied from G1)
    ├── PROVENANCE.md                     # FS.Skia.UI.* → FS.GG.UI.* rebrand + authored-outcome disclosure (FR-013)
    ├── README.md                         # what's built, the curated-slice rationale, how to run
    ├── coverage-backlog.md               # committed coverage + 22-spec adopted/deferred report (FR-011/FR-012)
    ├── SampleApps.Core/                  # pure, testable cores + shared harness (no GL, no I/O)
    │   ├── SampleApps.Core.fsproj        #   PackageReference FS.GG.UI.{Controls,Color,Controls.Elmish,SkiaViewer,DesignSystem,Themes.Default,KeyboardInput}
    │   ├── Prng.fs                        #   pure --seed-driven LCG (no System.Random) — R4
    │   ├── SampleTheme.fs                 #   Light/Dark + accents over shipped palettes (R9, as G1)
    │   ├── Evidence.fs                    #   package-only evidence record + run.json/state.txt/summary.md (R5)
    │   ├── Harness.fs                     #   Host bridge + SampleEntry type + evidence runner (R2/R3)
    │   ├── Games/
    │   │   ├── Tetris.fs                  #   MVU core + seeded script + expected outcome + entry
    │   │   ├── Snake.fs
    │   │   └── Pong.fs
    │   ├── Productivity/
    │   │   ├── Kanban.fs
    │   │   ├── Todo.fs
    │   │   └── Calendar.fs
    │   ├── Registry.fs                    #   the closure-erased SampleEntry list (all 6) — R2
    │   └── Coverage.fs                    #   coverage rows + 22-spec backlog + honesty check (R7)
    ├── SampleApps.App/                    # thin executable (the edge / interpreter)
    │   ├── SampleApps.App.fsproj          #   ProjectReference Core; PackageReference Controls.Elmish/SkiaViewer/Testing
    │   ├── Interactive.fs                 #   runInteractiveApp wiring (GL-gated), dispatch by sample id
    │   ├── Evidence.fs                    #   headless: Perf.runScript + captureScreenshotEvidence + record writer
    │   └── Program.fs                     #   CLI dispatch: list | interactive <id> | evidence --seed N | coverage
    └── SampleApps.Tests/                  # Expecto (outside the default test tier)
        ├── SampleApps.Tests.fsproj        #   ProjectReference Core; PackageReference Controls(.Elmish)/SkiaViewer
        ├── BuildOutcomeTests.fs           #   FR-009 / SC-001: each sample builds + meets authored outcome
        ├── DeterminismTests.fs            #   FR-006 / SC-002: byte-identical same-seed; bounded terminal (SC-007)
        ├── DegradeTests.fs                #   FR-008 / SC-003: clean disclosed skip on no-GL
        ├── CoverageBacklogTests.fs        #   FR-011 / FR-012 / SC-004 / SC-005: per-sample coverage + 22-spec accounting
        ├── ValidationTests.fs             #   FR-004 / SC-007: invalid form input rejected; inline-edit commits
        └── Main.fs

artifacts/sample-apps/                     # evidence output (gitignored), per seed/sample
└── <seed>/<sample-id>/{run.json,summary.md,frame.png,state.txt}
```

**Structure Decision**: A **single `samples/SampleApps/` tree with a 3-project split** (one shared pure
`Core` carrying both the harness and all six sample cores, one thin `App` edge, one Expecto `Tests`
project) — not a per-sample 3-project explosion (which would be 18 projects) and not one tree per family
(which would duplicate the harness/registry/evidence machinery). The shared `Core` keeps the deterministic
logic (sample cores, seeded scripts, MVU transitions, coverage/backlog) GL-free and unit-testable, exactly
as G1's `Core`/`App`/`Tests` split satisfied Principle IV. The sample is kept **out of the main `.slnx`**
and wired to the local NuGet feed so building/running it *is* the public-consumer-path proof (SC-006). See
research R1.

## Phased delivery (internal sequencing within this one feature)

- **P-A — Shared harness + first game (US1 MVP)**: `Prng`/`Evidence`/`Harness`/`SampleTheme` + **Tetris**
  end-to-end (pure core, seeded script, expected outcome, host with a non-None `Tick`, evidence run).
  Determinism + build-outcome suites green for Tetris. *Shippable MVP on its own.*
- **P-B — First productivity app (US2)**: **Todo** (or Kanban) — forms with validation, list + inline
  edit, data-state outcome. Validation suite green. Proves the enterprise-pattern half.
- **P-C — Complete the curated slice (US3)**: Snake, Pong; Kanban, Calendar. Registry holds all six;
  build-outcome suite spans the slice.
- **P-D — Determinism + disclosure hardening (US4)**: degrade-and-disclose suite across all six; same-seed
  byte-identity across the slice; bounded-terminal assertions.
- **P-E — Coverage + backlog honesty (US5)**: `coverage-backlog.md` + `Coverage.fs` + the honesty check
  dispositioning all 22 archived specs adopted/deferred and listing per-sample control/input coverage.
  PROVENANCE + README. Optional decision record.

## Complexity Tracking

> No Constitution Check violations. The two novelties over G1 are justified below, not violations.

| Item | Why needed | Simpler alternative rejected because |
|---|---|---|
| **Closure-erased heterogeneous sample registry** (research R2) | Each sample has its own `Model`/`Msg`; a single registry must hold all six without a shared generic param | A unified `Model`/`Msg` union across all samples couples them into one giant type and breaks the "each sample is an independent MVU app" framing; erasing the type params behind `SampleEntry` closures is the plain-F# idiom. |
| **Pure `--seed`-driven PRNG + `Tick`-driven game loop** (R3/R4) | Games need in-game randomness and time progression while staying byte-deterministic (SC-002) | `System.Random`/wall-clock would break determinism (FR-006); a fixed scripted board with no randomness would not exercise a real game loop. A tiny pure LCG seeded by `--seed` plus injected `Tick` deltas keeps both realism and determinism. |

No new package, no project added to `.slnx`, no public-surface/token change introduced.

# Implementation Plan: Controls Gallery Showcase (Light/Dark)

**Branch**: `123-controls-gallery-showcase` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/123-controls-gallery-showcase/spec.md`

## Summary

Deliver the **Controls Gallery** — a runnable sample application that consumes the
framework through its **public `FS.GG.UI.*` package surface only** and renders all
**52 catalog controls** across **exactly 10 navigable pages** on the existing
Light/Dark themes with an accent selector. The gallery ships two modes:

1. **Interactive windowed mode** — a GL-gated MVU app wired through
   `ControlsElmish.runInteractiveApp` with a Dock-style shell (top app bar with
   theme toggle + accent selector, left nav rail, scrolling content, bottom status
   strip).
2. **Headless deterministic evidence mode** — the CI-facing path that replays a
   seeded `FrameInput` script per page via `ControlsElmish.Perf.runScript` for the
   state outcome and `Viewer.captureScreenshotEvidence` for the screenshot, writing
   a per-page evidence record that **discloses what the run is not authoritative
   for** and **degrades cleanly when no display/GL is present**.

A **coverage check** maps every `Catalog.supportedControls` id to exactly one page
and fails on any unreferenced or duplicated control, keeping the gallery honest
against catalog drift.

**Technical approach**: a pure, testable **gallery core library** (page registry,
coverage map, seeded demo state, MVU `Model`/`Msg`/`update`) plus a thin **executable**
that dispatches the three CLI subcommands (`interactive`, `evidence --seed N`,
`coverage-check`). The core is exercised by an Expecto test project. The whole
sample lives in a new `samples/` tree, **outside the main solution and default test
tier**, and references the packed `FS.GG.UI.*` packages from
`~/.local/share/nuget-local/` via a local `nuget.config` — which is itself the proof
that the documented consumer path works end to end.

This is **Workstream G1 only** (Controls Gallery on Light/Dark). Games/productivity
samples (G2), the Ant restyle and enterprise templates (G3), and feeding gallery
runs into the perf corpus (G4) are out of scope and depend on Workstreams D/F.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`LangVersion=latest`, `Nullable=enable`,
`TreatWarningsAsErrors=true` including `FS0078`).

**Primary Dependencies** (consumed as packed NuGet packages, not project references):

- `FS.GG.UI.Controls` — control builders, `Catalog`, `Theme`, `Theming`, `DesignTokens`
- `FS.GG.UI.Controls.Elmish` — `InteractiveAppHost`, `runInteractiveApp`,
  `Perf.runScript`, `FrameInput`, `FrameMetrics`
- `FS.GG.UI.SkiaViewer` — `Viewer.captureScreenshotEvidence`, `Viewer.runForFrames`,
  `Viewer.runtimeCapability`, `ViewerOptions`, `ViewerPresentMode`
- `FS.GG.UI.Testing` — `ScreenshotEvidenceResult` (and its `ProvesScreenshot` /
  `Fallback` / `UnsupportedHostReason` disclosure fields)
- `FS.GG.UI.Color` — `Palettes` (ships the **slate** ramp; indigo/teal accents are
  defined by the gallery as `Color` literals)
- Test-only: `Expecto`, `Microsoft.NET.Test.Sdk`, `YoloDev.Expecto.TestSdk`

All packages are present at `0.1.0-preview.1` in `~/.local/share/nuget-local/`.

**Storage**: Filesystem only — per-page evidence records (`run.json`, `summary.md`,
`frame.png`, `state.txt`) written under `artifacts/controls-gallery/<seed>/<page-id>/`.
No database.

**Testing**: Expecto, run with `dotnet test`. Three suites — coverage (FR-003),
determinism (SC-002), and theme/accessibility invariance (SC-003) — plus a smoke
assertion that the headless CLI degrades-and-discloses on a no-GL host (FR-011).

**Target Platform**: Linux (X11/GL) for interactive mode; headless evidence mode
runs anywhere including CI hosts with no display/GL (degrade-and-disclose path).

**Project Type**: Sample **desktop application** (consumer of the framework) — a core
library + a thin executable + a co-located test project, in a standalone `samples/`
tree decoupled from the main solution.

**Performance Goals**: Not a hot-path feature. The only quantitative gate is
**byte-identical evidence across two same-seed headless runs (100% reproducible,
SC-002)** and **any showcased control reachable in ≤ 2 navigation actions (SC-006)**.

**Constraints**:

- Public package surface only — **no project references into `src/`, no internal
  access** (FR-013, SC-005).
- Only controls/themes that exist today — Light/Dark + accents; **no Ant/Fluent/
  Material dependence** (FR-014).
- Headless mode is deterministic — **no wall-clock, no randomness**; seeded input
  and injected time deltas only (FR-009).
- Headless mode is the CI-facing path; **the required gate never depends on a
  display or GL** (FR-016).
- Imported identifiers rebranded `FS.Skia.UI.*` → `FS.GG.UI.*`, recorded in
  provenance (FR-015).

**Scale/Scope**: 52 controls, exactly 10 pages, 1 cohesive palette ("Indigo & Teal
on Slate") in Light + Dark + accent variants. One sample app, one core library, one
test project.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

**Change Classification**: **Tier 2 (additive consumer)**. The gallery adds a new
sample app and its tests; it makes **no change to the product public API surface**,
adds **no new external runtime dependency** to the shipped packages, and touches no
`.fsi` or surface-area baseline. (Spec "Out of Scope" explicitly excludes public
surface changes.) Tier 2 requires spec + tests, which this plan provides.

| Principle | Gate | Status |
|-----------|------|--------|
| **I. Spec → FSI → Semantic Tests → Implementation** | Public surface sketched and validated by use before `.fs` bodies | **PASS (adapted).** The gallery is a *consumer*; its "FSI surface" is the framework's already-published `.fsi`. The plan exercises that surface through the headless CLI and Expecto tests *before* the shell/host bodies are finalized. Semantic tests call the public functions exactly as a downstream app would (`runInteractiveApp`, `Perf.runScript`, `captureScreenshotEvidence`, `Catalog.supportedControls`). |
| **II. Visibility lives in `.fsi`, not `.fs`** | Every **public** module has a curated `.fsi`; no access modifiers in `.fs` | **PASS (not in scope).** The sample exposes **no public package surface** (`IsPackable=false`, FR-013) — its modules are application-internal and therefore are not "public F# modules" under Principle II, so they require no `.fsi`. No `private`/`internal`/`public` modifiers are used on top-level bindings (keeps `FS0078`-as-error clean). |
| **III. Idiomatic simplicity** | Plainest F#; exotic features justified | **PASS.** Records, lists, pattern matching, pure functions, MVU. No custom operators, SRTP, reflection, type providers, or non-trivial CEs. Nothing requiring justification. |
| **IV. Elmish/MVU is the boundary for stateful/I-O work** | Stateful/interactive features modeled as `Model`/`Msg`/`Effect`/pure `update`/edge interpreter | **PASS (core fit).** The gallery is interactive and stateful → it is modeled as MVU and wired through `InteractiveAppHost` (`Init`/`Update`/`View` + `ViewerEffect`). `update` is pure; I/O (window, screenshot, file write) happens only at the edge (the viewer interpreter / headless evidence writer). Headless mode drives the same `update` via the pure `Perf.runScript`. |
| **V. Test evidence is mandatory** | Tests fail before / pass after; real evidence preferred; synthetic disclosed | **PASS.** Coverage, determinism, and theme-invariance tests are behavior-changing gates. Real GL screenshot evidence is captured where available; on a no-GL host the run records a **disclosed degrade** (`ProvesScreenshot=false` + reason) — never a fabricated pass. Any synthetic stand-in carries the `Synthetic` token + use-site disclosure. |
| **VI. Observability and safe failure** | Structured diagnostics on significant events; fail-fast or degrade explicitly; GL smoke distinguishes defect vs missing window-system | **PASS.** Every evidence record carries a non-empty "not authoritative for" disclosure (FR-010); GL/display unavailability is reported via `Viewer.runtimeCapability` + `ScreenshotEvidenceResult.{BlockedStage,UnsupportedHostReason}` and yields a clean, non-hanging exit (FR-011) — distinguishing a missing desktop from a real defect rather than silently passing. |

**Engineering Constraints check**: `net10.0` ✓ · SkiaSharp-over-GL (inherited via
packages, no Vulkan) ✓ · consumes packed packages from `~/.local/share/nuget-local/`
✓ · one semantic control set, no per-theme control forks ✓ · package identity left
untouched (consumer only) ✓ · samples kept outside the default test tier ✓.

**Result: PASS — no violations. Complexity Tracking is empty.**

## Project Structure

### Documentation (this feature)

```text
specs/123-controls-gallery-showcase/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output — resolved decisions
├── data-model.md        # Phase 1 output — entities + the 10-page coverage map
├── quickstart.md        # Phase 1 output — run/verify guide
├── contracts/           # Phase 1 output
│   ├── cli.md           #   the three CLI subcommands + exit codes
│   ├── page-registry.md #   page/coverage-map shape the core exposes to tests
│   └── evidence-record.md #  per-page evidence-record schema + disclosure rule
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

The sample is a standalone consumer tree, **not** added to `FS.GG.Rendering.slnx`
(so the main solution build never depends on packed output). It builds and tests on
its own via a local `nuget.config` pointing at `~/.local/share/nuget-local/`.

```text
samples/
└── ControlsGallery/
    ├── nuget.config                     # local feed → ~/.local/share/nuget-local/
    ├── ControlsGallery.Core/            # pure, testable core (no GL, no I/O)
    │   ├── ControlsGallery.Core.fsproj  #   PackageReference FS.GG.UI.Controls (+ Color)
    │   ├── Pages.fs                      #   the 10 GalleryPage definitions
    │   ├── CoverageMap.fs                #   control-id → page-id map + coverage check
    │   ├── DemoState.fs                  #   seeded representative content per control
    │   ├── GalleryTheme.fs               #   Light/Dark + Indigo/Teal-on-Slate accents
    │   ├── Model.fs                      #   MVU Model/Msg/init/update (pure)
    │   ├── Shell.fs                      #   View: Size -> Model -> Control (app bar/rail/content/status)
    │   └── Scripts.fs                    #   per-page seeded FrameInput scripts
    ├── ControlsGallery.App/             # thin executable (the edge / interpreter)
    │   ├── ControlsGallery.App.fsproj    #   + FS.GG.UI.Controls.Elmish, SkiaViewer, Testing
    │   ├── Interactive.fs                #   runInteractiveApp wiring (GL-gated)
    │   ├── Evidence.fs                   #   headless: Perf.runScript + captureScreenshotEvidence + record writer
    │   └── Program.fs                    #   CLI dispatch: interactive | evidence | coverage-check
    └── ControlsGallery.Tests/           # Expecto (outside the default test tier)
        ├── ControlsGallery.Tests.fsproj  #   references Core + FS.GG.UI.Controls
        ├── CoverageTests.fs              #   FR-003 / SC-001: 1:1 catalog→page
        ├── DeterminismTests.fs           #   FR-009 / SC-002: byte-identical same-seed
        ├── ThemeInvarianceTests.fs       #   FR-006 / SC-003: behavior+a11y identical across variants
        └── DegradeTests.fs               #   FR-011 / SC-004: clean disclosed skip on no-GL

artifacts/controls-gallery/              # evidence output (gitignored), per seed/page
└── <seed>/<page-id>/{run.json,summary.md,frame.png,state.txt}
```

**Structure Decision**: A **two-project split** (pure `Core` + thin `App` edge)
directly satisfies Principle IV (pure `update`, I/O only at the edge) and makes the
deterministic logic — pages, coverage map, seeded scripts, MVU transitions —
unit-testable without GL. The `App` is the interpreter that turns `Core` into a live
window or a headless evidence run. The sample is deliberately **kept out of the main
`.slnx`** and wired to the local NuGet feed so building/running it *is* the
public-consumer-path proof (SC-005); the spec's plan §10.3 floated an optional
project-reference variant for dev velocity, but FR-013/SC-005 make package
consumption the authoritative choice (see research.md R1).

## Complexity Tracking

> No Constitution Check violations. No entries required.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| _(none)_ | _(none)_ | _(none)_ |

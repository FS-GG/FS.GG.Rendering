# Implementation Plan: Automated Control Pass for the Second AntShowcase

**Branch**: `176-test-antshowcase-controls` | **Date**: 2026-06-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/176-test-antshowcase-controls/spec.md`

## Summary

Build a single **automated, no-human-input control pass** that drives every cataloged control
in the `SecondAntShowcase` sample through its full range of behavior, captures visual fidelity
across both appearances and both representative sizes plus interaction states, and emits exactly
one classified verdict record per control (interactive families fully exercised, display-only
controls visually inspected) with no control left unexercised or unclassified (US1, US2). Every
defect the pass surfaces is fixed and re-verified — sample-local fixes in the showcase, shared
control/framework defects at the shared layer — or explicitly deferred with a tracked rationale
(US3). The exercise's durable output is a comprehensive framework/library report under
`docs/reports/` that separates framework improvements from sample-local fixes, each carrying a
severity, classification, evidence, and recommendation (US4).

The pass is assembled from the repository's **existing** evidence infrastructure rather than a new
bespoke harness: deterministic input scripting (`Rendering.Harness.Input`), control/visual/retained
inspection (`FS.GG.UI.Controls.ControlInspection`, `FS.GG.UI.Scene` artifacts,
`FS.GG.UI.Testing` validation), the live-responsiveness runner, the visual-readiness matrix
(`FS.GG.UI.Testing.VisualCaptureMatrix`/`VisualReadiness`), and environment-limited detection
(`FS.GG.UI.SkiaViewer` window diagnostics, `FS.GG.UI.Diagnostics`). It is exposed as a new headless
CLI subcommand on `SecondAntShowcase.App` (`control-pass`) and asserted by new tests in
`SecondAntShowcase.Tests`.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> Any defect/root-cause hypothesis in this plan is provisional. Deterministic tests can pass
> while the running app stays broken (Feature 175: 15 presses → 15 renders, focus one click behind,
> all while unit tests were green). `/speckit-tasks` MUST schedule an **early live smoke run** in
> the Foundational phase — right after the catalog-coverage map, before any fix — that drives and
> observes the real app through the control-pass runner and either confirms or replaces the defect
> hypotheses surfaced by the first pass. No US3 fix is built on an unverified hypothesis.

**Change classification**: The control-pass runner, its CLI, and its tests are **sample-local
(Tier 2)** — they add no public `FS.GG.UI.*` surface. The exercise is expected to surface defects
in the shared control/framework surface; **fixing those is Tier 1** (FR-011) and each such fix
carries `.fsi` + surface-baseline + test obligations and is recorded with its tier in the finding
log. The feature therefore spans both tiers; the finding log is the per-fix tier register.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`LangVersion=latest`). Public package surfaces are
declared in `.fsi`; any public-surface delta produced by a US3 fix is Tier 1 and updates the
matching surface baseline in the same change.

**Primary Dependencies**: Existing only — `FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`,
`FS.GG.UI.SkiaViewer`, `FS.GG.UI.Themes.AntDesign`, `FS.GG.UI.Testing`, `FS.GG.UI.Scene`,
`FS.GG.UI.Diagnostics`, the `Rendering.Harness` test infrastructure (`Input`, `Perf`,
`ValidationLanes`), `Fable.Elmish`, `Silk.NET.Input/Windowing/OpenGL`, `SkiaSharp`,
`System.Text.Json`, and Expecto/YoloDev for tests. No new dependency is planned; if one becomes
necessary it states need, version-pinning strategy, and owner per the constitution.

**Storage**: Filesystem evidence only. Feature evidence is written under
`specs/176-test-antshowcase-controls/readiness/` (control-pass verdict records, visual-evidence
matrix, finding log, validation summary) and the consolidated report under `docs/reports/`. The
showcase consumes packed `FS.GG.UI.*` packages from `~/.local/share/nuget-local/`. Runtime code
introduces no persistent storage.

**Testing**: Expecto through `dotnet test`. New `SecondAntShowcase.Tests` cover the control-pass
runner contract (one record per cataloged control, classification completeness, deterministic
re-run, environment-limited degradation) and per-control verdict assertions. US3 fixes that reach
shared controls add failing-first semantic tests in `tests/Controls.Tests`, `tests/Elmish.Tests`,
`tests/SkiaViewer.Tests`, and/or `tests/Testing.Tests` as the touched surface dictates. Visible
desktop validation runs the control-pass CLI with live exercise; headless lanes report explicit
`environment-limited` results.

**Target Platform**: Desktop OpenGL/Skia viewer host in a visible, focusable Linux desktop session
for accepted live exercise and visual capture; deterministic/headless paths (the Pure input
backend, offscreen readback, inspection artifacts) for regression shape and environment-limited
reporting.

**Project Type**: Multi-package F# rendering/UI framework plus a package-consuming desktop sample
(`SecondAntShowcase`) with headless CLI evidence commands. The new work lives primarily in the
sample; shared-surface fixes land in `src/`.

**Performance Goals**: No regression to the Feature 174 responsiveness budgets the showcase
already asserts (button-activation follow-up frame median ≤ 150 ms / p95 ≤ 250 ms; page navigation
median ≤ 250 ms / p95 ≤ 500 ms). Continuous-input controls (slider/scroll drag) must show
continuous feedback within the same live-responsiveness target (no catch-up lag, offset tracks
input). All state-change repaints exercised by the pass remain damage-local — the pass both
*requires* and *records* damage-local repaint as evidence (FR-005) and MUST NOT introduce
full-tree frame preparation.

**Constraints**: The control-pass runner is unattended (FR-001, SC-004) — zero human input events
start to finish. Evidence must be deterministic across repeated runs on the same build (FR-007,
SC-005): time/animation surfaces are pinned or explicitly flagged; the Pure input backend and
seeded evidence give byte-stable artifacts where the framework guarantees determinism. Live-only
checks degrade to an explicit `environment-limited` outcome with a well-defined non-zero signal,
never a silent pass/fail (FR-008). Shared-surface fixes honor visibility-in-`.fsi`, surface
baselines, one-semantic-control-set (no per-theme control forks), and test-evidence obligations.
Any synthetic substitute is disclosed at the use site, carries the `Synthetic` token in the test
name, and is listed in the PR (Principle V). The pass MUST NOT remove any control or page or
regress existing passing behavior (FR-012, SC-007).

**Scale/Scope**: The current catalog — **13 catalog/family pages + 6 enterprise template pages
(19 pages)**, **96 cataloged catalog controls** across 13 interaction families, plus the
display-only controls and the controls reachable from template pages. Interactive families span
buttons, text/numeric input, selection/toggles, navigation, overlays (tooltip, dialog, drawer,
popover, popconfirm, tour, toast), data collections (list/tree/grid), and continuous inputs
(slider, scroll). Display-only families span typography, badges/tags, statistics, charts (16
chart types across two pages), and layout primitives. The visual matrix is two appearances
(antLight/antDark) × two representative sizes (preferred 1600×1000 and minimum 1280×800), with
interaction-state captures for interactive controls.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists; the runner/CLI/tests are Tier 2 (no public `FS.GG.UI.*` delta), and each shared-surface US3 fix is classified Tier 1 in the finding log with its `.fsi`/baseline/test obligations. The feature spans both tiers; the finding log is the per-fix tier register. |
| Spec → FSI → semantic tests → implementation | PASS | The runner's contract is drafted as the `control-pass` CLI surface + record schema and exercised by failing-first sample tests before implementation. Any shared-surface fix drafts its `.fsi` delta, fails a semantic test through the packed/prelude FSI surface, then implements `.fs`. |
| Visibility lives in `.fsi` | PASS | Sample code carries no public `.fsi` baselines to drift; shared-surface fixes confine deltas to the listed `.fsi` files with matching baseline updates and no `private`/`internal`/`public` modifiers in `.fs`. |
| Idiomatic simplicity | PASS | The runner composes existing records (`ControlInspectionRequest`, `RetainedInspectionArtifact`, `VisualCaptureTarget`, `InputScript`, `LaneResult`) and existing CLI/evidence writers; no new operators, SRTP, reflection, type providers, or non-trivial computation expressions. Mutation only at disclosed evidence-accumulation/render hot paths. |
| Elmish/MVU boundary | PASS | The pass drives the showcase through its existing pure `SecondAntShowcase.Core` MVU (`Model`/`Msg`/`update`) and the Elmish runtime; input is scripted data, IO (window, readback, file writes) stays at the App/runtime edge. Verdict aggregation is a pure fold over records. |
| Test evidence | PASS | Failing-first sample tests assert the record-per-control contract, classification completeness, deterministic re-run, and environment-limited degradation; visible desktop runs provide accepted live exercise + visual evidence or explicit environment limitation. US3 fixes ship failing-first tests at the touched layer. |
| Observability and safe failure | PASS | The runner reuses `FrameMetrics`/live-responsiveness summaries, runtime diagnostics, and environment-limited classification; live-only checks fail closed to `environment-limited` (never silent pass). Repaints stay damage-local and phase-attributed. |
| Tier 1 obligations | PASS | Every shared-surface fix ships `.fsi` + surface-baseline + tests together and is bound to its re-verification in the finding log. Tier 2 sample work updates no baselines. |

No constitution violations require justification. The Complexity Tracking table is empty.

## Project Structure

### Documentation (this feature)

```text
specs/176-test-antshowcase-controls/
├── plan.md                      # This file (/speckit-plan output)
├── research.md                  # Phase 0 output — runner composition + determinism + env-limit decisions
├── data-model.md                # Phase 1 output — entities, validation, lifecycle transitions
├── quickstart.md                # Phase 1 output — validation/run guide
├── contracts/                   # Phase 1 output
│   ├── control-pass-runner.md           # runner inputs/outputs, CLI surface, determinism, env-limit
│   ├── verdict-record.md                # one classified record per control: schema + completeness rule
│   ├── visual-evidence-matrix.md        # appearance × size × interaction-state evidence + fidelity verdicts
│   └── framework-report.md              # docs/reports report structure, severity/classification taxonomy
├── feedback/                    # per-phase fs-gg-feedback-capture notes
└── readiness/                   # evidence artifacts
    ├── verdict-records/                 # per-control verdict records (or aggregated JSON + md)
    ├── visual-evidence/                 # appearance × size × state captures + contact sheets
    ├── finding-log.md                   # found → fixed-and-re-verified | deferred-with-rationale
    └── validation-summary.md

docs/reports/
└── 2026-06-20-feature-176-second-antshowcase-control-pass-report.md   # US4 framework/library report
```

### Source Code (repository root)

```text
samples/SecondAntShowcase/
├── SecondAntShowcase.Core/
│   ├── PageRegistry.fs / CoverageMap.fs       # control catalog (source of "every control")
│   ├── Pages.fs / Templates.fs                # catalog + template page/control definitions
│   ├── InteractionContracts.fs                # per-control documented behaviors (drives "every behavior")
│   ├── ControlPass.fs / ControlPass.fsi       # NEW: pure pass plan — catalog → behaviors → record skeleton
│   └── Model.fs / Shell.fs / Host.fs          # existing MVU the pass drives
├── SecondAntShowcase.App/
│   ├── Program.fs                             # add `control-pass` subcommand dispatch
│   ├── ControlPassRunner.fs                   # NEW: drive scripted input, capture inspection/visual evidence
│   └── Interactive.fs / Responsiveness.fs     # existing live host + responsiveness evidence (reused)
└── SecondAntShowcase.Tests/
    ├── ControlPassRunnerTests.fs             # NEW: record-per-control, classification, determinism, env-limit
    └── ControlPassCoverageTests.fs           # NEW: catalog completeness vs CoverageMap; behavior completeness

# Shared-surface fixes (only where the pass proves a framework defect — paths confirmed in Phase 0):
src/
├── Controls/        (Pointer, Control, Widgets, ControlRuntime, Inspection, hit-test seam)
├── Controls.Elmish/ (routing, retained repaint)
├── SkiaViewer/      (delivery, repaint, window diagnostics)
└── Testing/         (inspection/readiness helpers, if a helper gap is the defect)

tests/
├── Controls.Tests/ Elmish.Tests/ SkiaViewer.Tests/ Testing.Tests/   # failing-first regression for each Tier 1 fix
```

**Structure Decision**: Keep the **control-pass runner sample-local** (`SecondAntShowcase.Core`
for the pure pass plan, `SecondAntShowcase.App` for the IO-bearing runner, `SecondAntShowcase.Tests`
for assertions) because it is a sample exercise, not a new framework capability — it composes
existing `FS.GG.UI.*` and `Rendering.Harness` surfaces. Land **defect fixes where the defect
lives**: sample-local defects in the showcase, shared control/framework defects in `src/` so all
showcases and products inherit them (no control forked per theme or per sample). No new project,
package, or dependency is planned. If Phase 0 finds the runner needs a capability that belongs in
shared testing infrastructure (e.g. a missing inspection helper), that addition is Tier 1 and
recorded as such.

## Phase 0 Research

See [research.md](./research.md). Planning unknowns to resolve:

- **Catalog as the completeness oracle**: confirm `CoverageMap`/`PageRegistry` (96 catalog
  controls + template-reachable controls) is the authoritative "every control" set the pass
  iterates, and how display-only vs interactive classification is sourced (FR-006, SC-001).
- **Documented-behavior source**: where each interactive control's full behavior set lives
  (`InteractionContracts.fs`) so the pass drives *every* documented behavior, not one
  representative action, and asserts each resulting state change (FR-002, SC-002).
- **Runner composition**: how to wire `Rendering.Harness.Input` (Pure/X11XTest/Uinput backends)
  to drive the showcase MVU/host, and `ControlInspection.inspect`/`inspectRetained` +
  `VisualCaptureMatrix`/`VisualReadiness` to capture per-control evidence — reusing the Feature 175
  "click-here-then-readback" path rather than building new capture plumbing.
- **Interaction-state capture**: how to drive each interactive control into hover/focus/active/
  selected/disabled/error and verify each differs from rest, including overlay/transient surfaces
  (tooltip/popover/drawer/dialog/tour/toast/popconfirm) via their triggers (FR-004, FR-015).
- **Damage-locality evidence**: how `RetainedInspection` + damage-locality validation prove each
  state-change repaint is damage-local and record it (FR-005).
- **Determinism**: how to pin time/animation surfaces (calendar, time-picker, carousel, spinner,
  tour) and seed evidence so repeated runs are byte-stable where guaranteed; what is explicitly
  flagged as time-dependent (FR-007, SC-005).
- **Environment-limited path**: the well-defined non-zero signal and record marking when no live
  window can be presented, reusing `SkiaViewer` window diagnostics + `ValidationLanes`
  `EnvironmentLimited` (FR-008).
- **Provisional defect hypotheses**: from a first dry run, the candidate framework vs sample-local
  defects — explicitly marked unverified pending the early live smoke run.
- **Report conventions**: confirm `docs/reports/` filename + structure (Part-structured findings,
  severity/effort/leverage table, phased roadmap, evidence appendix) to mirror (FR-013, FR-014).

Output: research.md with each unknown resolved as Decision / Rationale / Alternatives.

## Phase 1 Design and Contracts

See [data-model.md](./data-model.md) for entities (Control Verdict Record, Visual Evidence Item,
Finding, Framework/Library Report, plus the Control-Behavior and Interaction-State supporting
types), their validation rules, and lifecycle transitions (record: unexercised → exercised →
classified; finding: found → fixed-and-re-verified | deferred-with-rationale; evidence: captured →
approved | needs-review | blocked).

Observable and internal contracts:

- [Control-Pass Runner](contracts/control-pass-runner.md) — runner inputs/outputs, the `control-pass`
  CLI surface, unattended execution, determinism guarantees, and environment-limited degradation.
- [Verdict Record](contracts/verdict-record.md) — exactly one classified record per cataloged
  control, the record schema (functional + visual verdict), and the "none unclassified" completeness
  rule including display-only classification with reason.
- [Visual Evidence Matrix](contracts/visual-evidence-matrix.md) — appearance × size ×
  interaction-state coverage, fidelity verdicts (approved / needs-review / blocked) with reasons,
  and damage-local repaint evidence.
- [Framework Report](contracts/framework-report.md) — the `docs/reports/` report structure,
  the severity/classification (sample-vs-framework) taxonomy, evidence anchoring, and prioritization.

Validation guide: [quickstart.md](./quickstart.md).

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Contracts keep the runner Tier 2 and record each shared-surface fix as Tier 1 in the finding log; no reclassification hides a public-surface delta. |
| Spec → FSI → semantic tests → implementation | PASS | The runner contract names its sample surface and the failing-first tests that drive it; each shared fix names its `.fsi` seam and failing semantic test. |
| Visibility lives in `.fsi` | PASS | Shared deltas confined to listed `.fsi` files with matching baselines; sample code adds no public surface. |
| Idiomatic simplicity | PASS | Design reuses existing inspection/evidence/input records and writers; mutation only at disclosed evidence/render hot paths. |
| Elmish/MVU boundary | PASS | The pass drives the existing pure showcase MVU; input is data, IO stays at the edge, aggregation is a pure fold. |
| Test evidence | PASS | Quickstart requires failing-first deterministic tests plus visible desktop live + visual evidence, or explicit environment limitation; the early live smoke run gates US3. |
| Observability and safe failure | PASS | Repaints stay damage-local and phase-attributed; environment-limited results report fail-closed with a defined signal. |
| Tier 1 obligations | PASS | Every shared-surface delta ships `.fsi` + baseline + tests together, bound to its finding-log re-verification. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.

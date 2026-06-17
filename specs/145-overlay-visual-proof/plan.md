# Implementation Plan: Overlay Visual Proof

**Branch**: `145-overlay-visual-proof` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/145-overlay-visual-proof/spec.md`

## Summary

Feature 145 closes the remaining Feature 144 readiness caveat by running real visual proof for the
integrated overlay flow on a capable offscreen/display host. The proof must capture inspectable open
and final closed overlay artifacts, correlate those artifacts with the deterministic Feature 144
behavioral evidence, and update readiness so maintainers can tell whether the P5 visual-proof gate is
closed or still environment-gated.

The technical approach is deliberately narrow. Keep product-facing overlay behavior, public control
APIs, scene serialization, browser rendering, compositor work, layout, text, editing, and widget
catalog behavior unchanged. Extend the `Rendering.Harness`/readiness evidence path around the
existing Feature 144 overlay corpus and screenshot validation helpers. If implementation requires a
public `FS.GG.UI.*` contract, package surface, or compatibility change, stop and reclassify the work
as Tier 1 before continuing.

## Technical Context

**Language/Version**: F# on .NET `net10.0`, `LangVersion=latest`, warnings-as-errors.

**Primary Dependencies**: Existing in-repo packages only: `FS.GG.UI.Controls`,
`FS.GG.UI.Controls.Elmish`, `FS.GG.UI.KeyboardInput`, `FS.GG.UI.Scene`,
`FS.GG.UI.SkiaViewer`, `FS.GG.UI.Testing`, `Rendering.Harness`, and
`samples/AntShowcase`. No new runtime dependency is planned.

**Storage**: Filesystem evidence under `specs/145-overlay-visual-proof/readiness/` and
scenario-specific artifact folders. No persistent product storage.

**Testing**: Expecto test projects plus existing screenshot/evidence validation helpers. Focused
coverage belongs in `tests/Rendering.Harness.Tests`, `tests/SkiaViewer.Tests` only if viewer capture
behavior changes, `tests/Testing.Tests` only if evidence helper contracts change, and
`samples/AntShowcase/AntShowcase.Tests` for date-picker scenario correlation.

**Target Platform**: Linux/dev and CI for deterministic behavioral validation. Real visual proof
requires a host with display/offscreen capture support and a GL renderer. Unsupported hosts must
produce environment-limited readiness records, not visual passes.

**Project Type**: F# UI framework/library with declarative controls, retained rendering,
dependency-light scene primitives, Elmish integration, a SkiaSharp/OpenGL viewer host, generated
sample products, and validation harness projects.

**Performance Goals**: The visual-proof run is a readiness validation path, not a performance
feature. Equivalent capable-host runs must keep stable scenario names, evidence labels, pass/fail
decisions, and readiness status. Capture work should stay bounded to the representative Feature 144
overlay flow.

**Constraints**:
- Tier 2 validation/readiness feature. Product behavior, public control APIs, package contracts,
  portable scene format, browser rendering, compositor behavior, layout, text, editing, and widget
  catalog behavior remain unchanged.
- Any unavoidable public contract, `.fsi`, package surface, or compatibility change triggers Tier 1
  reclassification before implementation proceeds.
- Native display/GL work stays at the viewer/harness interpreter edge. Pure overlay coordinator,
  ControlRuntime, product state, and deterministic replay paths remain host-independent.
- Visual success requires real, current-run, non-empty artifacts tied to the current scenario. Blank,
  zero-sized, unreadable, stale, synthetic, deterministic-log-only, or unsupported-host records fail
  or report environment-limited status.
- Readiness diagnostics must distinguish environment failure, capture failure, overlay behavior
  failure, and evidence bookkeeping failure.

**Scale/Scope**:

```text
tests/Rendering.Harness/
|-- Live.fsi / Live.fs              # host capability and visual-proof execution edge
|-- Evidence.fsi / Evidence.fs      # overlay visual evidence records if internal shape changes
`-- Input.fs / RunPlan.fs           # only if the existing live/offscreen tier planner needs routing

tests/Rendering.Harness.Tests/
|-- Feature145OverlayVisualProofTests.fs
`-- Feature144OverlayVisualProofTests.fs       # regression coverage remains

samples/AntShowcase/
|-- AntShowcase.Core/Evidence.fs               # only if record correlation needs product-owned data
`-- AntShowcase.Tests/Feature145*Tests.fs       # date-picker scenario correlation

src/Testing/Testing.fsi / Testing.fs            # only if existing screenshot validation is insufficient
src/SkiaViewer/SkiaViewer.fsi / SkiaViewer.fs   # only if existing capture semantics are insufficient

specs/145-overlay-visual-proof/readiness/
|-- visual-proof.md
|-- unsupported-host.md
|-- correlation.md
|-- test-results.md
`-- artifacts/
```

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 2 (internal validation/readiness)**. The feature records real
visual evidence for an existing overlay flow and must not alter observable product behavior or public
contracts. If the implementation needs a public `.fsi`, package, or compatibility change, it becomes
Tier 1 before implementation continues.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec -> FSI -> Semantic Tests -> Implementation | PASS | The spec defines the validation outcome, scope exclusions, public API impact boundary, and proof approach. No public surface is planned; any unavoidable public change is gated by Tier 1 reclassification. |
| II. Visibility lives in `.fsi` | PASS | Planned work is in harness/readiness tests. If `Testing` or `SkiaViewer` public contracts change, the `.fsi` is designed first and surface baselines are updated as Tier 1 work. |
| III. Idiomatic simplicity | PASS | The design extends existing records and evidence validators. No SRTP, reflection, custom operators, type providers, or non-trivial computation expressions are planned. |
| IV. Elmish/MVU boundary | PASS | Product state and overlay behavior stay in the existing MVU/pure coordinator paths. Native capture remains an edge effect interpreted by the harness/viewer host. |
| V. Test evidence mandatory | PASS | The plan requires capable-host visual artifacts, artifact validation, correlation with behavioral evidence, stable repeated decisions, and explicit unsupported-host limitation records. |
| VI. Observability and safe failure | PASS | Environment, capture, overlay behavior, and evidence bookkeeping failures are classified separately. Unsupported hosts cannot claim visual success. |

**Gate result**: PASS. No unresolved clarification markers remain.

**Post-design re-check**: PASS. Phase 0/1 artifacts keep the feature scoped to validation and
readiness evidence, preserve the Feature 144 behavioral baseline, and explicitly prevent synthetic
or unsupported-host records from satisfying real visual proof.

## Project Structure

### Documentation (this feature)

```text
specs/145-overlay-visual-proof/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- overlay-visual-proof.md
|-- readiness/                    # Created during implementation
`-- tasks.md                      # Created by /speckit-tasks, not by /speckit-plan
```

### Source Code (repository root)

```text
tests/Rendering.Harness/Live.fsi / Live.fs
tests/Rendering.Harness/Evidence.fsi / Evidence.fs
tests/Rendering.Harness.Tests/Feature145OverlayVisualProofTests.fs
tests/Rendering.Harness.Tests/Feature144OverlayVisualProofTests.fs

samples/AntShowcase/AntShowcase.Tests/Feature145OverlayVisualProofTests.fs
samples/AntShowcase/AntShowcase.Core/Evidence.fs              # only if correlation records extend there

src/Testing/Testing.fsi / Testing.fs                          # only if existing validators are insufficient
tests/Testing.Tests/Tests.fs                                  # only if Testing changes
src/SkiaViewer/SkiaViewer.fsi / SkiaViewer.fs                 # only if capture semantics change
tests/SkiaViewer.Tests/Tests.fs                               # only if SkiaViewer changes
```

**Structure Decision**: Single F# solution. Implement the first pass in the harness/readiness layer
and reuse existing `Viewer.captureScreenshotEvidence` and `EvidenceReports.validateScreenshotEvidence`
semantics where possible. Do not introduce product behavior changes or public package changes unless
the feature is reclassified to Tier 1.

## Phase 0: Research Summary

See [research.md](./research.md). Decisions are resolved and no clarification markers remain.

## Phase 1: Design Summary

See [data-model.md](./data-model.md), [contracts/overlay-visual-proof.md](./contracts/overlay-visual-proof.md),
and [quickstart.md](./quickstart.md). The contract centers on host capability, real artifact
acceptance, open/closed overlay proof, behavioral correlation, unsupported-host disclosure, and the
Feature 144 caveat decision.

## Complexity Tracking

No constitution violations require justification.

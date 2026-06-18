# Implementation Plan: Complete P8 Layout Acceptance

**Branch**: `151-complete-p8-layout` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/151-complete-p8-layout/spec.md`

## Summary

Complete the remaining P8/R3b layout acceptance bar after Feature 150's first intrinsic-layout
slice. The plan keeps the existing Yoga-backed `FS.GG.UI.Layout` protocol as the public foundation,
then broadens acceptance through a representative layout and ScrollViewer corpus, measured and
intrinsic reuse/stale-rejection evidence, full/cold/warm/changed incremental parity, broad retained
and default-layout regression validation, package validation, compatibility notes, and one final
P8 readiness summary.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; `LangVersion=latest`; nullable enabled; warnings as
errors including F# visibility drift (`FS0078`).

**Primary Dependencies**: Existing `FS.GG.UI.Layout`, `FS.GG.UI.Scene`, `FS.GG.UI.Controls`,
`FS.GG.UI.Controls.Elmish`, `FS.GG.UI.Testing`, `FS.GG.UI.SkiaViewer`, and rendering harness/test
projects; Yoga.Net `3.2.3`; FSharp.Core `10.1.301`; SkiaSharp `4.147.0-preview.3.1` where viewer or
harness evidence already uses it; Expecto `10.2.2`; FsCheck `3.3.3`; no new runtime dependency is
planned.

**Storage**: Durable planning and readiness artifacts under
`specs/151-complete-p8-layout/`; public surface baselines under `readiness/surface-baselines/` and
`tests/surface-baselines/`; package output under `~/.local/share/nuget-local/`; transient command
output under `artifacts/` only when an existing harness or script writes it.

**Testing**: Expecto/FsCheck via `dotnet test`; focused Feature151 suites in Layout, Controls,
Elmish, Testing, Package, Rendering.Harness, SkiaViewer, and prior Feature regression filters;
semantic FSI/package surface validation through Package.Tests and `scripts/refresh-surface-baselines.fsx`;
full solution build/test and pack validation before accepted readiness.

**Target Platform**: Multi-package F# UI/rendering library on .NET `net10.0`; deterministic layout
and Controls behavior are platform-neutral. SkiaSharp over OpenGL remains the viewer backend for
viewer/compositor evidence, but this feature must not claim new compositor partial-redraw or browser
backend acceptance.

**Project Type**: Multi-package F# rendering/UI library with desktop viewer host, generated product
templates, package validation helpers, and repository validation harnesses.

**Performance Goals**: 100% of accepted corpus cases preserve full/cold/warm/changed incremental
equivalence for bounds, placements, scroll extents, diagnostics, and result identities; repeated
equivalent evaluations record accepted measured and intrinsic reuse; stale measured or intrinsic
entries are never accepted; duplicate measurement in a normal pass is prevented or reported with a
blocking diagnostic.

**Constraints**: `src/Layout` may depend only on Scene and Yoga.Net. Controls consume Layout for
ScrollViewer and control-tree geometry; viewer, keyboard, chart, Controls, and harness dependencies
must not enter `src/Layout`. The default flex model remains Yoga-backed. The feature does not add a
general solver, alter overlay interaction behavior, add text-shaping behavior, claim browser
rendering, or claim new compositor partial-redraw behavior. Missing, unsupported, contradictory, or
environment-limited evidence must fail closed and be visible in readiness.

**Scale/Scope**: Tier 1 contracted change spanning acceptance evidence for the public layout
protocol, Controls ScrollViewer behavior, incremental layout reuse, regression compatibility,
surface/package validation, docs/readiness artifacts, and package readiness. Public surface changes
are allowed only if the corpus exposes a real contract gap; such changes require `.fsi`, semantic
tests, surface baseline refresh, compatibility notes, and migration guidance.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies the work as a Tier 1 contracted change because P8 validates observable layout behavior and package readiness. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Any public contract refinement must be drafted in `.fsi`, covered by package/FSI semantic tests and surface baselines, then implemented. Existing Feature150 surfaces are reused unless the new corpus proves a gap. |
| Visibility lives in `.fsi` | PASS | Public symbols remain declared by curated `.fsi` files; implementation files must not add top-level visibility modifiers. |
| Idiomatic simplicity | PASS | The approach extends records/functions and existing fixtures/evidence ledgers; it introduces no solver hierarchy, reflection, SRTP tricks, custom operators, type providers, or new computation expressions. |
| Elmish/MVU boundary | PASS | Layout evaluation and readiness classification remain pure. Command execution, file writes, and package validation are edge effects recorded in readiness artifacts; Elmish consumes deterministic metrics only. |
| Test evidence is mandatory | PASS | The plan requires representative corpus tests, ScrollViewer cases, reuse/stale-rejection tests, full/incremental parity, broad regression sweeps, package validation, and readiness evidence before acceptance. |
| Observability and safe failure | PASS | Diagnostics and readiness statuses distinguish invalid constraints, unsupported intrinsic queries, stale reuse, fallback bounds, duplicate measurement, environment limits, unrelated failures, and missing evidence. |
| Tier 1 obligations | PASS | Public deltas require `.fsi`, surface baselines, compatibility notes, docs/readiness updates, package validation, and migration impact before accepted P8 status. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/151-complete-p8-layout/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- representative-corpus.md
|   |-- cache-reuse.md
|   |-- regression-evidence.md
|   `-- p8-readiness.md
|-- readiness/
|   |-- validation-summary.md
|   |-- corpus-validation.md
|   |-- scrollviewer-validation.md
|   |-- reuse-validation.md
|   |-- full-incremental-parity.md
|   |-- regression-evidence.md
|   |-- compatibility-ledger.md
|   |-- package-validation.md
|   `-- limitations.md
`-- tasks.md
```

### Source Code (repository root)

```text
src/
|-- Layout/
|   |-- Types.fsi
|   |-- Types.fs
|   |-- Layout.fsi
|   |-- Layout.fs
|   `-- README.md
|-- Controls/
|   |-- Control.fsi
|   |-- Control.fs
|   `-- README.md
|-- Controls.Elmish/
|   |-- ControlsElmish.fsi
|   `-- ControlsElmish.fs
`-- Testing/
    |-- Testing.fsi
    `-- Testing.fs

tests/
|-- Layout.Tests/
|   |-- Feature151CorpusFixtures.fs
|   |-- Feature151RepresentativeCorpusTests.fs
|   |-- Feature151ScrollLayoutProtocolTests.fs
|   |-- Feature151MeasuredReuseTests.fs
|   |-- Feature151IntrinsicReuseTests.fs
|   |-- Feature151FullIncrementalParityTests.fs
|   `-- Feature151DiagnosticsTests.fs
|-- Controls.Tests/
|   |-- Feature151ScrollViewerCorpusTests.fs
|   |-- Feature151LayoutCompatibilityTests.fs
|   `-- Feature151DisabledCacheParityTests.fs
|-- Elmish.Tests/
|   `-- Feature151LayoutRegressionMetricsTests.fs
|-- Testing.Tests/
|   `-- Feature151ReadinessHelperTests.fs
|-- Package.Tests/
|   |-- Feature151CompatibilityLedgerTests.fs
|   |-- Feature151PackageValidationTests.fs
|   |-- FsiTranscriptCoverageTests.fs
|   `-- SurfaceAreaTests.fs
|-- Rendering.Harness.Tests/
|   |-- Feature151RenderAnywhereRegressionTests.fs
|   |-- Feature151TextShapingRegressionTests.fs
|   `-- Feature151CompositorReadinessRegressionTests.fs
`-- SkiaViewer.Tests/
    `-- Feature151RetainedRenderingRegressionTests.fs

docs/
|-- reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md
`-- validation/surface-baseline-review.md
```

**Structure Decision**: Reuse the existing package split and Feature150 public protocol. `Layout`
owns corpus fixtures, measured/intrinsic cache identity, stale rejection, diagnostics, and parity.
`Controls` owns ScrollViewer viewport/extent behavior and default-layout compatibility. `Elmish`
owns metrics regressions only. `Testing` and `Package.Tests` own readiness helper behavior, package
surface checks, compatibility ledger validation, and generated consumer/package readiness. Harness
tests link previous render-anywhere, text-shaping, compositor, and retained rendering evidence
without moving those dependencies into runtime Layout.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Feature151 is final P8 acceptance and evidence completion, not a new layout architecture.
- Feature150's public constraints, intrinsic query/result, content extent, cache entry, diagnostics,
  and readiness helper contracts remain the starting point.
- Representative acceptance is ledger-backed and fixture-backed: every corpus case has expected
  bounds, placements, scroll extents where relevant, diagnostics, and a verdict.
- Measured and intrinsic reuse acceptance requires dependency-key evidence for cold, warm, changed,
  stale, and disabled-cache paths.
- Broad regression evidence is explicit and classified; failed, skipped, synthetic-only, or
  environment-limited results cannot silently count as accepted.
- The checkout has no root `fake.sh`; runnable validation therefore uses the existing `dotnet`
  commands and surface/package scripts, with layout-skill FAKE targets recorded as future wrappers if
  restored.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Public or observable contracts:

- [Representative Corpus Contract](contracts/representative-corpus.md)
- [Cache Reuse Contract](contracts/cache-reuse.md)
- [Regression Evidence Contract](contracts/regression-evidence.md)
- [P8 Readiness Contract](contracts/p8-readiness.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Tier 1 artifact chain | PASS | Contracts require `.fsi` and surface baseline updates for any public delta, plus package-shaped FSI transcript/semantic tests before implementation, compatibility ledger, docs/readiness updates, package validation, and migration notes. |
| Dependency boundaries | PASS | Layout remains limited to Scene and Yoga.Net; broad evidence lives in tests, harnesses, package checks, and readiness documents outside the runtime Layout package. |
| Determinism and parity | PASS | Data model requires deterministic corpus case identity, constraint/input/cache keys, result identities, diagnostics, and full/cold/warm/changed incremental equivalence. |
| Safe failure and diagnostics | PASS | Invalid constraints, contradictory intrinsics, unsupported queries, duplicate measurement, stale cache entries, fallback bounds, missing evidence, and environment limits are recorded and block accepted readiness when required. |
| Real evidence and synthetic disclosure | PASS | Accepted P8 readiness requires real command/test/package evidence. Synthetic fixtures may cover failure paths only when named and cannot replace required acceptance categories. |
| MVU/I/O boundary | PASS | Pure layout/reuse/readiness classification is separated from command execution and readiness file writes; no I/O enters Elmish `update` or the Layout evaluator. |

No constitution violations are introduced by the design.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations require justification.

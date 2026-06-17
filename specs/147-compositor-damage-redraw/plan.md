# Implementation Plan: Compositor Damage Redraw

**Branch**: `147-compositor-damage-redraw` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/147-compositor-damage-redraw/spec.md`

## Summary

Turn the existing retained renderer damage, picture-cache, replay-cache, render-anywhere, and
reference-evidence foundations into a gated compositor path. The implementation first proves that a
real host profile preserves untouched pixels between presents, then enables damage-scissored redraw
only for passed profiles. Stable content promotion, placement-only reuse, and the higher-cost
snapshot tier remain evidence-gated and bounded. Every tier keeps the full-redraw oracle as the
correctness baseline and records readiness evidence before any performance benefit is claimed.

## Technical Context

**Language/Version**: F# on .NET `net10.0`, `LangVersion=latest`, warnings as errors.

**Primary Dependencies**: Existing `FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`,
`FS.GG.UI.SkiaViewer`, `FS.GG.UI.Scene`, `FS.GG.UI.Testing`, and `Rendering.Harness` packages;
existing SkiaSharp/OpenGL/Silk.NET rendering edge; Expecto/FsCheck for tests. No new runtime
dependency is planned unless implementation research proves one is necessary and version-pins it.

**Storage**: Readiness artifacts are persisted under
`specs/147-compositor-damage-redraw/readiness/` with transient run output under `artifacts/`.
Accepted evidence records include present-path proof, parity comparisons, promotion decisions,
snapshot budget/resource state, performance probes, and compatibility notes.

**Testing**: `dotnet test` Expecto/FsCheck suites; semantic FSI-style tests for any new public
surface; SkiaViewer raster/GL tests for present proof and backend resources; `Rendering.Harness`
commands for end-to-end proof/parity/performance/readiness runs; package surface checks and
`scripts/refresh-surface-baselines.fsx` for Tier 1 public deltas.

**Target Platform**: Multi-package F# UI/rendering library on .NET `net10.0`; SkiaSharp over OpenGL
for the live host; deterministic headless harness where possible; real present-path proof only on
capable display/headless GL profiles.

**Project Type**: Multi-package F# rendering/UI library plus desktop viewer host and validation
harness.

**Performance Goals**: 100% target host profiles produce a present verdict before scissored redraw
is considered; 100% visual parity against the full-redraw oracle on the agreed corpus; stable or
moving corpus reduces repeated visual work by at least 30%; simple/churning scenes stay within 5%
of baseline or demote; snapshot tier shows at least 20% frame-cost improvement on expensive stable
scenes before readiness is claimed.

**Constraints**: Damage-scissored redraw is disabled unless the current host profile has a passed
present-path proof; untouched pixels must never be assumed valid from stale, synthetic, or
environment-limited evidence; every unsafe or unsupported condition falls back to full redraw with
diagnostics; all public visibility lives in `.fsi`; any I/O-bearing proof/harness workflow exposes
or wraps a pure model/message/effect boundary; snapshot resources are bounded and deterministically
released or demoted.

**Scale/Scope**: Corpus covers idle, localized update, overlapping damage, movement/scrolling,
resize, theme change, full-frame invalidation, stable promotion, placement-only movement,
content-changing promotion, churn, simple-scene overhead, expensive stable snapshot candidates,
resource-budget pressure, and unsupported-host cases.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies the feature as Tier 1 because compositor behavior, metrics, diagnostics, readiness evidence, and possible public contracts change. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Any public proof, metrics, harness, or readiness surfaces are planned in `.fsi` contracts first, then exercised through semantic tests before implementation bodies. |
| Visibility lives in `.fsi` | PASS | New public modules must have matching `.fsi`; implementation files must not add top-level access modifiers. Internal compositor mechanics can remain behind existing internal modules. |
| Idiomatic simplicity | PASS | The plan reuses retained damage records, `unionArea`, `CachedSubtree`, replay-cache, and harness patterns. No reflection, SRTP, custom operators, type providers, or non-trivial computation expressions are planned. Hot-path mutation is allowed only with a local reason comment. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Present proof, parity runs, performance probes, artifact writing, and readiness packaging are stateful/I/O-bearing and must expose or wrap `Model`, `Msg`, `Effect`, pure `update`, and edge interpreters. |
| Test evidence is mandatory | PASS | Plan requires failing-first proof/parity/promotion/snapshot tests, real host proof where capable, environment-limited disclosure where not, full-redraw oracle parity, public surface checks, package tests, and readiness output. |
| Observability and safe failure | PASS | Diagnostics record proof status, host profile, damage/scissor area, fallback reason, promotion/demotion, reuse hits/misses, snapshot status, parity verdict, and performance deltas. Unsupported and unsafe cases fail closed. |
| Tier 1 obligations | PASS | `.fsi`, semantic tests, surface baseline refresh, compatibility ledger, migration guidance, docs/readiness evidence, and package validation are required for any public or observable surface change. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/147-compositor-damage-redraw/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- present-path-proof.md
|   |-- damage-scissored-redraw.md
|   |-- promotion-and-snapshot-reuse.md
|   `-- readiness-package.md
|-- checklists/
|   `-- requirements.md
`-- readiness/
    |-- present-proof/
    |-- parity/
    |-- perf/
    |-- compatibility-ledger.md
    `-- validation-summary.md
```

### Source Code (repository root)

```text
src/
|-- Controls/
|   |-- RetainedRender.fsi
|   |-- RetainedRender.fs
|   |-- Diagnostics.fsi
|   |-- Diagnostics.fs
|   |-- Control.fsi
|   `-- Control.fs
|-- Controls.Elmish/
|   |-- ControlsElmish.fsi
|   `-- ControlsElmish.fs
|-- SkiaViewer/
|   |-- CompositorProof.fsi
|   |-- CompositorProof.fs
|   |-- SceneRenderer.fs
|   |-- SkiaViewer.fsi
|   |-- SkiaViewer.fs
|   |-- Host/OpenGl.fsi
|   |-- Host/OpenGl.fs
|   |-- PictureReplayCache.fsi
|   `-- PictureReplayCache.fs
`-- Testing/
    |-- Testing.fsi
    `-- Testing.fs

tests/
|-- Controls.Tests/
|   |-- Feature147DamageUnionTests.fs
|   |-- Feature147PromotionReuseTests.fs
|   |-- Feature147SnapshotBudgetTests.fs
|   `-- Controls.Tests.fsproj
|-- Elmish.Tests/
|   |-- Feature147CompositorMetricsTests.fs
|   `-- Elmish.Tests.fsproj
|-- SkiaViewer.Tests/
|   |-- Feature147PresentPathProofTests.fs
|   |-- Feature147ScissorRedrawTests.fs
|   |-- Feature147SnapshotResourceTests.fs
|   `-- SkiaViewer.Tests.fsproj
|-- Rendering.Harness/
|   |-- Compositor.fsi
|   |-- Compositor.fs
|   |-- Evidence.fsi
|   |-- Evidence.fs
|   |-- Perf.fsi
|   |-- Perf.fs
|   |-- Cli.fs
|   `-- Rendering.Harness.fsproj
|-- Rendering.Harness.Tests/
|   |-- Feature147CompositorEvidenceTests.fs
|   |-- Feature147CompositorReadinessTests.fs
|   `-- Rendering.Harness.Tests.fsproj
`-- Package.Tests/
    |-- Feature147CompatibilityLedgerTests.fs
    |-- SurfaceAreaTests.fs
    `-- Package.Tests.fsproj
```

**Structure Decision**: Correctness decisions over retained tree damage, content identity,
placement identity, promotion, and demotion belong in `src/Controls` because that package already
owns `RetainedRender`, `WorkReductionRecord`, `unionArea`, picture cache keys, and control-level
diagnostics. User-visible per-frame metrics belong in `src/Controls.Elmish` where
`FrameMetrics` is already surfaced. Host capability proof, GL scissoring, and snapshot resources
belong in `src/SkiaViewer` because they depend on the OpenGL/Skia presenter. End-to-end corpus
execution, evidence formatting, performance probes, and readiness output belong in
`tests/Rendering.Harness`. Harness compile order keeps `Compositor` as dependency-light shared
contracts, followed by `Evidence` and `Perf`; `Cli.fs` orchestrates those modules so `Compositor`
does not depend on `Perf`.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Present-path proof is a real host-profile capability gate, not a synthetic or static config
  flag.
- Damage-scissored redraw uses existing retained damage union data and remains disabled until proof
  passes.
- Full-redraw oracle parity remains the acceptance test for every compositor tier.
- Promotion and demotion use observed stability, work-reduction, parity, and overhead evidence
  before reuse is accepted.
- Content identity and placement identity are separated so movement can reuse stable content while
  content changes force fresh paint.
- Snapshot reuse is a SkiaViewer-owned bounded offscreen resource tier, not a new Scene contract.
- Performance probes compare against full redraw or the next lower tier and cannot report benefit
  from environment-limited evidence.
- Readiness is one package tying proof, parity, performance, diagnostics, limitations, and
  compatibility impact together.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Public or observable contracts:

- [Present Path Proof Contract](contracts/present-path-proof.md)
- [Damage-Scissored Redraw Contract](contracts/damage-scissored-redraw.md)
- [Promotion and Snapshot Reuse Contract](contracts/promotion-and-snapshot-reuse.md)
- [Readiness Package Contract](contracts/readiness-package.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Tier 1 artifact chain | PASS | Contracts require `.fsi` surfaces for any public proof/metric/harness APIs, semantic tests, surface baseline refresh, compatibility ledger, release notes, and readiness output. |
| Dependency boundaries | PASS | Controls owns pure retained/damage/promotion decisions; SkiaViewer owns GL proof, scissor, and snapshot resources; Rendering.Harness owns artifact I/O and corpus orchestration. |
| Determinism and safe failure | PASS | Data model requires stable scenario ids, same-seed verdicts, deterministic union area/resource budgets, proof freshness checks, and fail-closed fallback to full redraw. |
| Real evidence and synthetic disclosure | PASS | Contracts reject stale, synthetic, environment-limited, or host-mismatched evidence for readiness. Environment-limited runs are recorded but cannot enable scissoring or snapshot claims. |
| MVU/I/O boundary | PASS | Present proof, parity/performance runs, and readiness packaging define model/message/effect responsibilities with edge interpreters for GL, filesystem, process, and artifact I/O. |

No constitution violations are introduced by the design.

## Complexity Tracking

No constitution violations require justification.

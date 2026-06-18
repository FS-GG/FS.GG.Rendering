# Implementation Plan: Intrinsic Layout Protocol

**Branch**: `150-intrinsic-layout-protocol` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/150-intrinsic-layout-protocol/spec.md`

## Summary

Implement the P8/R3b radical layout slice: a constraints-down, sizes-up layout protocol with
explicit intrinsic-size queries, deterministic cache keys, and ScrollViewer content extent derived
from the layout contract instead of descendant bounds inspection. The feature extends the existing
Yoga-backed `FS.GG.UI.Layout` contract, keeps Yoga as the default flex implementation, threads
layout cache/intrinsic dependency evidence through Controls, and publishes readiness artifacts that
prove full/incremental parity, ScrollViewer correctness, compatibility, invalidation, and safe
diagnostics.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; `LangVersion=latest`; warnings as errors.

**Primary Dependencies**: Existing `FS.GG.UI.Layout`, `FS.GG.UI.Scene`,
`FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`, `FS.GG.UI.Testing`, and rendering harness/test
projects; Yoga.Net `3.2.3`; SkiaSharp `4.147.0-preview.3.1` where existing viewer/evidence paths
already use it; Expecto `10.2.2`; FsCheck `3.3.3`. No new runtime dependency is planned.

**Storage**: Durable planning and readiness artifacts under
`specs/150-intrinsic-layout-protocol/`; public surface baselines under
`tests/surface-baselines/`; transient validation output under `artifacts/` when harness commands
or local scripts produce files.

**Testing**: Expecto/FsCheck via `dotnet test`; semantic FSI-style tests for public layout
protocol additions; Layout and Controls focused Feature150 suites; full/incremental parity and
cache invalidation corpora; ScrollViewer extent scenarios; package surface checks plus
`scripts/refresh-surface-baselines.fsx` for Tier 1 public deltas.

**Target Platform**: Multi-package F# UI/rendering library on .NET `net10.0`; SkiaSharp over
OpenGL remains the viewer backend, but this feature is primarily a deterministic layout/Controls
contract change and must not claim new compositor behavior.

**Project Type**: Multi-package F# rendering/UI library with desktop viewer host, generated
product templates, and repository validation harnesses.

**Performance Goals**: A normal layout pass measures each participant at most once for the same
inputs; additional natural-size discovery uses explicit intrinsic queries. 100% of the
representative layout corpus defined in `spec.md` preserves full/incremental bounds, placements,
scroll extents, and diagnostics. ScrollViewer validation covers at least 10 content cases. Cache/invalidation evidence
covers at least 5 layout-affecting input categories without accepted stale measured or intrinsic
results.

**Constraints**: `src/Layout` may depend on Scene and Yoga.Net only; no Controls, viewer,
keyboard, chart, or harness runtime dependency enters the Layout package. The default flex model
remains Yoga-backed. The feature does not add a general constraint solver, change compositor,
overlay, text shaping, or browser rendering behavior, or weaken existing parity/surface gates.
Invalid, contradictory, unavailable, or unsupported intrinsic data fails closed with diagnostics
instead of accepted misleading bounds.

**Scale/Scope**: Tier 1 contracted change spanning public `FS.GG.UI.Layout` signatures,
Controls layout lowering and ScrollViewer geometry, incremental layout cache/invalidation
behavior, package surface baselines, docs, readiness artifacts, and focused regression evidence.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies the work as Tier 1 because public layout contracts and observable layout behavior may change. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Public layout protocol additions are drafted in `.fsi`, exercised through semantic/FSI tests and focused layout tests, then implemented in `.fs`. |
| Visibility lives in `.fsi` | PASS | Public surface changes are confined to curated `.fsi` files; implementation files do not add top-level visibility modifiers. |
| Idiomatic simplicity | PASS | The design uses records/functions over an object-heavy solver model, extends the existing Yoga flex evaluator, and introduces no reflection, SRTP, type providers, custom operators, or new computation expressions. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Core layout remains pure. Readiness/evidence generation and any harness/file output stay at edge interpreters; Controls.Elmish only consumes deterministic layout diagnostics/metrics. |
| Test evidence is mandatory | PASS | Plan requires failing-first semantic tests, Layout/Controls/Elmish focused tests, full/incremental parity, ScrollViewer extent corpus, cache invalidation corpus, surface baseline refresh, docs, and readiness evidence. |
| Observability and safe failure | PASS | Diagnostics cover invalid constraints, unsupported intrinsic queries, stale cache reuse, degradation, fallback, and environment/tooling limitations. |
| Tier 1 obligations | PASS | `.fsi`, surface baselines, compatibility notes, migration guidance, docs/readiness artifacts, package validation, and pack readiness are required for accepted public or observable deltas. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/150-intrinsic-layout-protocol/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- layout-protocol.md
|   |-- intrinsic-cache.md
|   |-- scrollviewer-extent.md
|   `-- readiness-validation.md
`-- readiness/
    |-- compatibility-ledger.md
    |-- scrollviewer-validation.md
    |-- intrinsic-cache-validation.md
    |-- full-incremental-parity.md
    `-- validation-summary.md
```

### Source Code (repository root)

```text
src/
|-- Layout/
|   |-- Types.fsi
|   |-- Types.fs
|   |-- Layout.fsi
|   |-- Layout.fs
|   |-- GraphValidation.fsi
|   |-- GraphValidation.fs
|   `-- README.md
|-- Controls/
|   |-- Control.fsi
|   |-- Control.fs
|   |-- Diagnostics.fsi
|   |-- Diagnostics.fs
|   `-- Widgets/
|       |-- Containers.fsi
|       `-- Containers.fs
|-- Controls.Elmish/
|   |-- ControlsElmish.fsi
|   `-- ControlsElmish.fs
`-- Testing/
    |-- Testing.fsi
    `-- Testing.fs

tests/
|-- Layout.Tests/
|   |-- Feature150Fixtures.fs
|   |-- Feature150IntrinsicProtocolTests.fs
|   |-- Feature150MeasureDeterminismTests.fs
|   |-- Feature150LayoutDiagnosticsTests.fs
|   |-- Feature150IntrinsicCacheTests.fs
|   |-- Feature150IntrinsicInvalidationTests.fs
|   |-- Feature150FullIncrementalParityTests.fs
|   `-- Program.fs
|-- Controls.Tests/
|   |-- Feature150ScrollFixtures.fs
|   |-- Feature150ScrollViewerExtentTests.fs
|   |-- Feature150LayoutCompatibilityTests.fs
|   `-- Feature150LayoutDiagnosticsTests.fs
|-- Elmish.Tests/
|   `-- Feature150LayoutMetricsTests.fs
|-- Package.Tests/
|   |-- FsiTranscriptCoverageTests.fs
|   |-- SurfaceAreaTests.fs
|   `-- Feature150CompatibilityLedgerTests.fs
`-- Testing.Tests/
    `-- Feature150ReadinessHelperTests.fs
```

**Structure Decision**: Extend the existing package split. `Layout` owns the public constraints,
measure/place, intrinsic query, cache-key, validation, and Yoga-backed default flex behavior.
`Controls` owns control-tree lowering, ScrollViewer viewport/extent reporting, and diagnostic
projection from layout results. `Controls.Elmish` only exposes deterministic metrics/diagnostics
that existing Elmish flows already consume. `Testing`, `Package.Tests`, and readiness documents
own consumer validation helpers and review evidence; they must not pull viewer or Controls
dependencies into `Layout`.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Feature 150 is the P8/R3b intrinsic protocol slice, not a general layout-solver feature.
- The new contract extends the existing Yoga-backed `FS.GG.UI.Layout` public surface instead of
  replacing it or adding a parallel layout engine.
- Intrinsic queries are explicit, deterministic, cache-keyed layout inputs and are the only
  accepted second-size-discovery path during a layout pass.
- ScrollViewer uses intrinsic content extent from the layout contract; the descendant-bounds walk
  in `Control.scrollViewport` is removed or reduced to compatibility-only diagnostics.
- Incremental layout cache reuse is valid only when content identity, constraints, intrinsic
  dependencies, and layout-affecting inputs all match.
- Readiness evidence is local, reviewable, and bounded: compatibility, ScrollViewer, intrinsic,
  cache/invalidation, diagnostics, and full/incremental parity.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Public or observable contracts:

- [Layout Protocol Contract](contracts/layout-protocol.md)
- [Intrinsic Cache Contract](contracts/intrinsic-cache.md)
- [ScrollViewer Extent Contract](contracts/scrollviewer-extent.md)
- [Readiness Validation Contract](contracts/readiness-validation.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Tier 1 artifact chain | PASS | Contracts require `.fsi` first for public constraints, measurement, intrinsic, cache, diagnostic, and readiness surfaces; semantic tests; surface baseline refresh; compatibility ledger; documentation; and readiness output. |
| Dependency boundaries | PASS | `Layout` remains limited to Scene and Yoga.Net; Controls consumes Layout for ScrollViewer and control lowering; harness/readiness I/O stays outside runtime packages. |
| Determinism and parity | PASS | Data model requires deterministic constraint normalization, cache keys, intrinsic dependency keys, child placements, diagnostics, and full/incremental equivalence evidence. |
| Safe failure and diagnostics | PASS | Invalid constraints, contradictory min/max, unsupported intrinsic queries, stale cache entries, and fallback/degradation states produce reviewable diagnostics and cannot silently accept misleading bounds. |
| Real evidence and synthetic disclosure | PASS | Contracts require real package/test evidence for accepted claims. Synthetic or fixture-only evidence may cover failure paths but must be named and cannot replace parity/readiness acceptance. |
| MVU/I/O boundary | PASS | Layout evaluation and cache decisions remain pure; readiness artifact writing and any harness orchestration are edge effects only. |

No constitution violations are introduced by the design.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations require justification.

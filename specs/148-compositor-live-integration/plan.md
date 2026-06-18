# Implementation Plan: Compositor Live Integration

**Branch**: `148-compositor-live-integration` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/148-compositor-live-integration/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Complete the remaining P7 compositor work left open by Feature 147. Feature 147 established
deterministic proof contracts, diagnostics, policy helpers, and readiness formatting, but the live
host proof remained environment-limited and no compositor performance benefit was claimed. This
feature implements the live sentinel/readback proof on the real SkiaViewer/OpenGL host, gates
damage-scoped redraw behind matching proof, integrates safe full-frame fallback diagnostics,
separates content identity from placement identity for movement reuse, adds a bounded SkiaViewer
snapshot lifecycle, expands the parity/timing corpus, and records one readiness package that blocks
performance claims unless proof, parity, fallback, resource, timing, and compatibility evidence all
pass.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; `LangVersion=latest`; warnings as errors.

**Primary Dependencies**: Existing `FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`,
`FS.GG.UI.SkiaViewer`, `FS.GG.UI.Scene`, `FS.GG.UI.Testing`, and `Rendering.Harness` projects;
SkiaSharp `4.147.0-preview.3.1`; Silk.NET OpenGL/Windowing `2.23.0`; Yoga.Net `3.2.3`;
Expecto `10.2.2`; FsCheck `3.3.3`; YamlDotNet `18.0.0` where existing harness/evidence code
already uses it. No new runtime dependency is planned.

**Storage**: Durable readiness artifacts under
`specs/148-compositor-live-integration/readiness/`; transient run output under `artifacts/`.
Evidence records cover live proof artifacts, parity/oracle identities, damage/fallback decisions,
reuse decisions, snapshot lifecycle, timing probes, compatibility notes, and validation summaries.

**Testing**: Expecto/FsCheck via `dotnet test`; semantic FSI-style tests for public proof,
diagnostic, harness, or readiness surfaces; SkiaViewer live/simulated GL proof tests; rendering
harness commands for proof, parity, timing, and readiness packaging; package surface checks plus
`scripts/refresh-surface-baselines.fsx` for any Tier 1 public deltas.

**Target Platform**: Multi-package F# UI/rendering library on .NET `net10.0`; SkiaSharp over
OpenGL for the live host; deterministic headless/simulated paths where possible; accepted
damage-scoped readiness only on capable host profiles that pass the live proof.

**Project Type**: Multi-package F# rendering/UI library plus desktop viewer host and validation
harness.

**Performance Goals**: 100% of target host profiles receive a live proof verdict before partial
redraw readiness; 100% accepted damage-scoped frames match the full-frame oracle; placement-only
reuse reduces repeated visual work by at least 30% on the moving/scrolling corpus; simple/churning
scenarios remain within 5% overhead or demote/reject the tier; snapshot reuse improves frame cost
by at least 20% on the expensive-stable corpus before readiness.

**Constraints**: Damage-scoped redraw is unavailable unless a fresh, matching live proof passed
for the active host profile; missing, stale, synthetic-only, failed, environment-limited, or
host-mismatched evidence fails closed to full redraw; scissor/no-clear state must reset between
frames and before readback/full redraw; movement damages old and new covered regions; snapshot
resources are budgeted, refreshed, evicted, disposed, or bypassed before stale output; timing
benefit cannot be claimed from environment-limited evidence.

**Scale/Scope**: Corpus covers capable, failing, and environment-limited host profiles; localized
damage, overlapping damage, edge damage, movement/scrolling, resize, theme/global change, stable
promotion, placement-only movement, content changes, churn, simple overhead, expensive stable
snapshot candidates, over-budget resources, invalid resources, unsupported hosts, failed parity,
and missing timing.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies the feature as Tier 1 because observable rendering behavior, diagnostics, readiness evidence, and performance claims change. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Any public proof, diagnostic, metrics, harness, testing, or readiness surface must be added to `.fsi` first and exercised through semantic/FSI tests before `.fs` implementation bodies are accepted. |
| Visibility lives in `.fsi` | PASS | Public modules keep curated `.fsi`; implementation files must not add top-level visibility modifiers. Internal hot-path helpers can remain omitted from signatures. |
| Idiomatic simplicity | PASS | The plan extends Feature 147's existing contracts, retained damage data, `unionArea`, replay cache, proof model, and harness commands. No reflection, SRTP, custom operators, type providers, or new computation expressions are planned. Hot-path mutation is allowed only with a local reason comment. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Live proof, corpus runs, timing probes, artifact writing, and readiness assembly expose or wrap `Model`, `Msg`, `Effect`, pure `update`, and edge interpreters. |
| Test evidence is mandatory | PASS | Plan requires failing-first local tests, simulated proof failure tests, real capable-host evidence where available, environment-limited disclosure where not, oracle parity, timing probes, surface checks, and readiness artifacts. |
| Observability and safe failure | PASS | Diagnostics record proof verdict, host profile, freshness, scissor/fallback decisions, damage area, reuse/demotion reasons, snapshot lifecycle, timing thresholds, and compatibility impact. Unsafe paths use full redraw or lower tiers. |
| Tier 1 obligations | PASS | `.fsi`, semantic tests, surface baseline refresh, compatibility ledger, migration guidance, docs/readiness evidence, and package validation are required for public or observable deltas. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/148-compositor-live-integration/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- live-preservation-proof.md
|   |-- damage-scoped-redraw-integration.md
|   |-- content-placement-reuse.md
|   |-- snapshot-lifecycle.md
|   `-- timing-readiness-package.md
`-- readiness/
    |-- live-proof/
    |-- parity/
    |-- reuse/
    |-- snapshots/
    |-- timing/
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
|   |-- PictureReplayCache.fsi
|   |-- PictureReplayCache.fs
|   |-- SceneRenderer.fs
|   |-- PresentMode.fsi
|   |-- PresentMode.fs
|   |-- SkiaViewer.fsi
|   `-- SkiaViewer.fs
`-- Testing/
    |-- Testing.fsi
    `-- Testing.fs

tests/
|-- Controls.Tests/
|   |-- Feature147DamageUnionTests.fs
|   |-- Feature147PromotionReuseTests.fs
|   |-- Feature147SnapshotBudgetTests.fs
|   `-- Feature148*.fs
|-- Elmish.Tests/
|   |-- Feature147CompositorMetricsTests.fs
|   `-- Feature148*.fs
|-- SkiaViewer.Tests/
|   |-- Feature147PresentPathProofTests.fs
|   |-- Feature147ScissorRedrawTests.fs
|   |-- Feature147SnapshotResourceTests.fs
|   `-- Feature148*.fs
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
|   `-- Feature148*.fs
`-- Package.Tests/
    |-- Feature147CompatibilityLedgerTests.fs
    |-- FsiTranscriptCoverageTests.fs
    |-- SurfaceAreaTests.fs
    `-- Feature148*.fs
```

**Structure Decision**: Continue Feature 147's package split. `Controls` owns pure retained
damage, promotion, content identity, placement identity, and demotion decisions because it already
owns retained identities, damage union, picture/cache keys, and control diagnostics. `SkiaViewer`
owns live GL sentinel/readback proof, no-clear scissored rendering, framebuffer state, and
snapshot resources because those depend on host capabilities. `Controls.Elmish` owns public
per-frame diagnostics derived from runtime metrics. `Rendering.Harness` owns corpus orchestration,
artifact I/O, parity comparison, timing probes, and readiness package assembly. `Testing` receives
only consumer-facing validation helpers if implementation needs public reusable assertions.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Live proof is implemented through the real SkiaViewer/OpenGL presenter with deterministic
  simulated interpreters for failing/environment-limited tests.
- Proof readiness is keyed to active host profile identity, proof algorithm version, freshness,
  and accepted artifacts.
- Damage-scoped redraw integrates at the host rendering boundary with no-clear scissor state and
  explicit reset/fallback behavior.
- Content identity and placement identity are separated in retained compositor decisions so
  movement can reuse stable content while damaging old and new covered regions.
- Snapshot reuse is a bounded SkiaViewer resource pool with deterministic lifecycle evidence, not
  a new public Scene authoring construct.
- Timing probes run against real host/corpus evidence and compare each tier to the right lower
  tier or full-frame baseline.
- Readiness remains one reviewable package that rejects overclaims from missing, stale,
  synthetic-only, environment-limited, or host-mismatched evidence.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Public or observable contracts:

- [Live Preservation Proof Contract](contracts/live-preservation-proof.md)
- [Damage-Scoped Redraw Integration Contract](contracts/damage-scoped-redraw-integration.md)
- [Content and Placement Reuse Contract](contracts/content-placement-reuse.md)
- [Snapshot Lifecycle Contract](contracts/snapshot-lifecycle.md)
- [Timing and Readiness Package Contract](contracts/timing-readiness-package.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Tier 1 artifact chain | PASS | Contracts require `.fsi` first for public proof/metrics/harness/testing surfaces, semantic tests, surface baseline refresh, compatibility ledger, release notes, readiness output, and package validation. |
| Dependency boundaries | PASS | Pure policy remains in `Controls`; host-dependent proof/scissor/snapshot code remains in `SkiaViewer`; evidence and filesystem/process I/O remain in `Rendering.Harness`; public runtime metrics remain in `Controls.Elmish`. |
| Determinism and safe failure | PASS | Data model requires stable scenario ids, deterministic same-seed verdict fields, proof freshness checks, full-frame fallback, scissor-state reset, deterministic snapshot budgets, and explicit limited/rejected/skipped verdicts. |
| Real evidence and synthetic disclosure | PASS | Contracts allow simulated tests for failure paths but reject synthetic-only or environment-limited evidence for ready tier claims. Real live proof and real timing are required before shipped benefit claims. |
| MVU/I/O boundary | PASS | Live proof, timing probes, and readiness assembly define model/message/effect responsibilities with edge interpreters for GL, filesystem, process, timing, and artifact I/O. |

No constitution violations are introduced by the design.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations require justification.

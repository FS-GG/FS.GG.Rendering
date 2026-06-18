# Implementation Plan: Complete P7 Compositor

**Branch**: `149-complete-compositor-p7` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/149-complete-compositor-p7/spec.md`

## Summary

Finish the remaining P7 compositor work after Features 147 and 148. The implementation converts
the existing deterministic proof/evidence scaffolding into real SkiaViewer/OpenGL host behavior:
live sentinel/readback preservation proof, proof-gated no-clear damage-scoped rendering, bounded
snapshot composition, real timing probes, final public diagnostics, and one consumer-visible
readiness report. P8 intrinsic layout remains explicitly out of scope.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; `LangVersion=latest`; warnings as errors.

**Primary Dependencies**: Existing `FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`,
`FS.GG.UI.SkiaViewer`, `FS.GG.UI.Scene`, `FS.GG.UI.Testing`, and `Rendering.Harness` projects;
SkiaSharp `4.147.0-preview.3.1`; Silk.NET OpenGL/Windowing `2.23.0`; Yoga.Net `3.2.3`;
Expecto `10.2.2`; FsCheck `3.3.3`; YamlDotNet `18.0.0` where existing harness/evidence code
already uses it. No new runtime dependency is planned.

**Storage**: Durable readiness artifacts under
`specs/149-complete-compositor-p7/readiness/`; transient command output under `artifacts/`.
Evidence records cover live proof frame artifacts, pixel-preservation observations,
damage/full-redraw parity, fallback reasons, reuse decisions, snapshot lifecycle, timing probes,
compatibility impact, and final readiness summaries.

**Testing**: Expecto/FsCheck via `dotnet test`; semantic FSI-style tests for public proof,
diagnostic, metric, harness, and testing helper surfaces; SkiaViewer live/simulated GL proof tests;
rendering harness commands for proof, parity, reuse, snapshot, timing, and readiness packaging;
package surface checks plus `scripts/refresh-surface-baselines.fsx` for Tier 1 public deltas.

**Target Platform**: Multi-package F# UI/rendering library on .NET `net10.0`; SkiaSharp over
OpenGL for the live host; deterministic headless/simulated tests where possible; accepted
partial-redraw readiness only on capable host profiles that pass live preservation proof.

**Project Type**: Multi-package F# rendering/UI library plus desktop viewer host and validation
harness.

**Performance Goals**: On capable hosts, at least 3 consecutive live proof runs accepted with zero
stale, blank, or missing-artifact failures; 100% damage-scoped/full-redraw visual parity on the
representative corpus; placement/reuse evidence over at least 5 frame transitions; timing evidence
for full redraw, damage-scoped redraw, and snapshot-assisted redraw over at least 5 representative
scenarios, or an explicit inconclusive verdict with no performance claim.

**Constraints**: Damage-scoped redraw is unavailable unless a fresh, matching live proof passed for
the active host profile; missing, stale, synthetic-only, failed, environment-limited, or
host-mismatched evidence fails closed to full redraw; scissor/no-clear state resets before any full
redraw, readback, or non-scoped frame; snapshot resources are budgeted and invalidated before stale
output; timing benefit cannot be claimed from incomplete or environment-limited evidence; public
surface changes follow Tier 1 `.fsi`, baseline, semantic test, documentation, and compatibility
ledger discipline.

**Scale/Scope**: Completes open P7 exit criteria from the roadmap: live framebuffer sentinel/damage
readback, no-clear damage-scoped renderer integration, full redraw fallback diagnostics, full
content/placement tracking where not already finished, snapshot lifecycle/composition, real timing
implementation in `Perf.fs`, Evidence-module formatters, final public diagnostic surface, and
package validation. P8 intrinsic layout and unrelated rendering architecture work are excluded.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies this as Tier 1 because public diagnostics, readiness evidence, fallback behavior, and package-facing compositor behavior change. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Public proof, diagnostics, metrics, harness, testing, and readiness surfaces are planned in `.fsi` first, then exercised through semantic/FSI tests before implementation bodies. |
| Visibility lives in `.fsi` | PASS | Public modules keep curated `.fsi`; implementation files must not add top-level visibility modifiers. Internal hot-path helpers remain omitted from signatures. |
| Idiomatic simplicity | PASS | The plan extends the Feature 147/148 proof model, retained damage data, damage union helpers, replay cache, and harness commands. No reflection, SRTP, custom operators, type providers, or new computation expressions are planned. Hot-path mutation is allowed only with a local reason comment. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Live proof, corpus runs, timing probes, artifact writing, and readiness assembly expose or wrap `Model`, `Msg`, `Effect`, pure `update`, and edge interpreters. |
| Test evidence is mandatory | PASS | Plan requires failing-first local tests, simulated failure tests with synthetic disclosure, real capable-host evidence where available, environment-limited disclosure where not, oracle parity, timing probes, surface checks, and readiness artifacts. |
| Observability and safe failure | PASS | Diagnostics record proof verdict, host profile, artifact identity, damage/scissor/fallback decisions, reuse/demotion reasons, snapshot lifecycle, timing thresholds, compatibility impact, and limitations. Unsafe paths use full redraw or lower safe tiers. |
| Tier 1 obligations | PASS | `.fsi`, semantic tests, surface baseline refresh, compatibility ledger, release notes/migration guidance, docs/readiness evidence, package validation, and pack evidence are required for public or observable deltas. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/149-complete-compositor-p7/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- live-proof-acceptance.md
|   |-- damage-scoped-frame-rendering.md
|   |-- reuse-snapshot-timing-evidence.md
|   `-- public-diagnostics-readiness.md
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
|   `-- Diagnostics.fs
|-- Controls.Elmish/
|   |-- ControlsElmish.fsi
|   `-- ControlsElmish.fs
|-- SkiaViewer/
|   |-- CompositorProof.fsi
|   |-- CompositorProof.fs
|   |-- PictureReplayCache.fsi
|   |-- PictureReplayCache.fs
|   |-- PresentMode.fsi
|   |-- PresentMode.fs
|   |-- SceneRenderer.fs
|   |-- SkiaViewer.fsi
|   `-- SkiaViewer.fs
`-- Testing/
    |-- Testing.fsi
    `-- Testing.fs

tests/
|-- Controls.Tests/
|   |-- Feature148DamagePlanTests.fs
|   |-- Feature148PlacementReuseTests.fs
|   |-- Feature148SnapshotEligibilityTests.fs
|   `-- Feature149*.fs
|-- Elmish.Tests/
|   |-- Feature148CompositorMetricsTests.fs
|   `-- Feature149*.fs
|-- SkiaViewer.Tests/
|   |-- Feature148LiveProofTests.fs
|   |-- Feature148LiveProofSimulationTests.fs
|   |-- Feature148DamageScopedRedrawTests.fs
|   |-- Feature148SnapshotLifecycleTests.fs
|   `-- Feature149*.fs
|-- Rendering.Harness/
|   |-- Compositor.fsi
|   |-- Compositor.fs
|   |-- Evidence.fsi
|   |-- Evidence.fs
|   |-- Perf.fsi
|   |-- Perf.fs
|   |-- TestAssertions.fs
|   `-- Cli.fs
|-- Rendering.Harness.Tests/
|   |-- Feature148LiveProofEvidenceTests.fs
|   |-- Feature148DamageParityTests.fs
|   |-- Feature148ReuseEvidenceTests.fs
|   |-- Feature148SnapshotEvidenceTests.fs
|   |-- Feature148TimingEvidenceTests.fs
|   |-- Feature148ReadinessPackageTests.fs
|   `-- Feature149*.fs
|-- Package.Tests/
|   |-- FsiTranscriptCoverageTests.fs
|   |-- Feature148CompatibilityLedgerTests.fs
|   |-- SurfaceAreaTests.fs
|   `-- Feature149*.fs
`-- Testing.Tests/
    `-- Feature149*.fs
```

**Structure Decision**: Continue the Feature 147/148 package split. `Controls` owns pure retained
damage, content identity, placement identity, promotion/demotion policy, and control diagnostics.
`SkiaViewer` owns live GL sentinel/readback proof, no-clear scissored rendering, framebuffer state,
and snapshot resources. `Controls.Elmish` owns public per-frame diagnostics derived from runtime
metrics. `Rendering.Harness` owns corpus orchestration, artifact I/O, timing probes, and readiness
package assembly. `Testing` receives only consumer-facing validation helpers; it must not pull broad
implementation projects into generated products.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Feature 149 is a completion slice for the open Feature 148 native/live-renderer work, not a new
  compositor design.
- Live proof acceptance requires real host sentinel/damage readback artifacts; environment-limited
  or synthetic-only evidence cannot mark partial redraw accepted.
- Damage-scoped rendering uses the same proofed no-clear/scissor host path and falls back to full
  redraw with diagnostics whenever preconditions fail.
- Content/placement reuse and snapshot composition remain parity-gated and demote on churn,
  no-benefit, parity failure, unsupported host state, or resource pressure.
- Timing probes must measure real host runs through `Rendering.Harness/Perf.fs` and avoid claims
  when incomplete or inconclusive.
- Public diagnostics are final package contracts, not private readiness-only strings.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Public or observable contracts:

- [Live Proof Acceptance Contract](contracts/live-proof-acceptance.md)
- [Damage-Scoped Frame Rendering Contract](contracts/damage-scoped-frame-rendering.md)
- [Reuse, Snapshot, and Timing Evidence Contract](contracts/reuse-snapshot-timing-evidence.md)
- [Public Diagnostics and Readiness Contract](contracts/public-diagnostics-readiness.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Tier 1 artifact chain | PASS | Contracts require `.fsi` first for public proof, diagnostics, metrics, harness, testing, and readiness surfaces; semantic tests; surface baseline refresh; compatibility ledger; documentation; readiness output; and package validation. |
| Dependency boundaries | PASS | Pure policy remains in `Controls`; host-dependent proof, scissor, and snapshot code remains in `SkiaViewer`; evidence and filesystem/process I/O remain in `Rendering.Harness`; consumer validation helpers remain narrow in `Testing`. |
| Determinism and safe failure | PASS | Data model requires stable scenario ids, deterministic verdict fields, proof freshness checks, full-frame fallback, scissor-state reset, deterministic snapshot budgets, and explicit limited/rejected/skipped verdicts. |
| Real evidence and synthetic disclosure | PASS | Contracts allow simulated tests for failure paths but reject synthetic-only or environment-limited evidence for accepted partial redraw or performance claims. |
| MVU/I/O boundary | PASS | Live proof, timing probes, and readiness assembly define model/message/effect responsibilities with edge interpreters for GL, filesystem, process, timing, and artifact I/O. |

No constitution violations are introduced by the design.

## Complexity Tracking

No constitution violations require justification.

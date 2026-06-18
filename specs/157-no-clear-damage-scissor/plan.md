# Implementation Plan: No-Clear Damage-Scissored Render Path

**Branch**: `157-no-clear-damage-scissor` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/157-no-clear-damage-scissor/spec.md`

## Summary

Implement the real no-clear damage-scissored render path for the GL `DirectToSwapchain` host. The
path clips repaint to the union damage region only when Feature 155 current-host proof is accepted,
the active host profile matches, retained previous frame content is trusted for the current run,
damage is valid, resources are available, and same-frame parity against full redraw passes. Every
missing or unverifiable gate falls back to full redraw with reviewer-visible diagnostics and zero
accepted partial-redraw artifacts.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; repository `LangVersion=latest`; public surface curated
through `.fsi` files.

**Primary Dependencies**: Existing `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Scene`, `FS.GG.UI.Controls`,
`FS.GG.UI.Testing`, and `Rendering.Harness` projects; SkiaSharp `4.147.0-preview.3.1`; Silk.NET
OpenGL/Windowing `2.23.0`; Expecto `10.2.2`; existing Feature 147-156 proof, scissor, parity,
timing, probe, and readiness helpers. No new runtime dependency is planned.

**Storage**: Durable Feature 157 evidence under `specs/157-no-clear-damage-scissor/readiness/`,
including `damage/attempts/`, `damage/fallbacks/`, `damage/parity/`, `damage/unsupported/`, `fsi/`,
`compatibility-ledger.md`, `package-validation.md`, `regression-validation.md`, and
`validation-summary.md`. Transient native artifacts may be written under a caller-provided
`--out` directory before accepted results are copied into readiness.

**Testing**: Expecto through `dotnet test`; SkiaViewer tests for pure eligibility, retained-frame
state, damage validation, fallback reasons, and no-clear/scissor path selection; Rendering.Harness
tests for Feature 157 command routing, scenario inventory, accepted/fallback package rendering, and
readiness summaries; Package/FSI coverage for public or observable surface deltas; unsupported-host
validation with display variables unset; focused regression tests for Feature 155 and Feature 156
claims.

**Target Platform**: Multi-package F# UI/rendering library on .NET `net10.0`; SkiaSharp over
OpenGL for the live viewer host. The accepted target profile remains Feature 155 stable profile
`probe-08a47c01`; unsupported or unavailable presentation environments remain fail-closed.

**Project Type**: Multi-package F# rendering/UI library plus desktop viewer host and validation
harness.

**Performance Goals**: This feature enables the real damage-scoped render path but does not by
itself accept a shipped compositor performance claim. Correctness goals dominate: at least three
fresh accepted current-host attempts across at least five representative scenarios must show
preserved untouched pixels, updated damaged pixels, same-profile parity, and recorded artifact
paths. Unsupported-host validation must complete under 2 minutes with zero accepted partial-redraw
artifacts.

**Constraints**: Damage-scoped repaint is opt-in by eligibility and fail-closed by default. It may
run only when proof, host profile, run identity, retained backing, damage validation, resource
availability, and parity gates all pass. Full redraw remains the fallback for invalid damage,
missing retained backing, resource failure, unsupported host, stale or mismatched proof, resize or
full-frame invalidation, and parity mismatch. Feature 156 remains `performance-not-accepted`; Feature
158, Feature 159, and Feature 161 stay separate follow-up gates.

**Scale/Scope**: Narrow P7 runtime-rendering slice across `src/SkiaViewer/Host/OpenGl.*`,
`src/SkiaViewer/CompositorProof.*` or adjacent public diagnostic contracts if needed,
`tests/Rendering.Harness`, focused SkiaViewer/Package/Testing tests, readiness docs, and surface
baselines if public surface changes. Out of scope: proof readback/timing separation, layer
promotion, content/transform key split, validation throughput, full performance lane ledger, P8
layout, text shaping, overlay behavior, and universal performance acceptance.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies the work as Tier 1 because observable rendering behavior and readiness diagnostics can change. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Public or observable damage-scoped result, fallback, and readiness contracts are planned before implementation and require semantic tests plus FSI transcript coverage when they cross package boundaries. |
| Visibility lives in `.fsi` | PASS | Package-visible `SkiaViewer` or `Testing` additions must be declared in `.fsi`; implementation-only native helpers remain omitted. |
| Idiomatic simplicity | PASS | The plan extends existing GL host scissor helpers, Feature 155 proof gates, and Feature 156 harness/readiness patterns without adding a new rendering framework. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Damage validation and eligibility are pure values; native GL rendering, readback, parity capture, and filesystem writes are modeled as effects interpreted at the host/harness edge. |
| Test evidence is mandatory | PASS | Failing-first tests cover accepted path, invalid damage, stale/cross-profile proof, missing retained backing, resource failure, unsupported host, parity mismatch, and readiness publication. Synthetic evidence is rejection-only. |
| Observability and safe failure | PASS | Every fallback records a reason and full redraw remains available for every frame. Unsupported hosts and unavailable presentation environments record zero accepted partial-redraw artifacts. |
| Tier 1 obligations | PASS | `.fsi`, surface baselines, package validation, compatibility notes, readiness evidence, and regression validation are required for any public or consumer-visible delta. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/157-no-clear-damage-scissor/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- compositor-damage-command.md
|   |-- damage-scissored-render-path.md
|   |-- damage-workflow-effects.md
|   `-- readiness-package.md
`-- readiness/
    |-- damage/
    |   |-- attempts/
    |   |-- fallbacks/
    |   |-- parity/
    |   |-- unsupported/
    |   `-- summary.md
    |-- fsi/
    |-- compatibility-ledger.md
    |-- package-validation.md
    |-- regression-validation.md
    `-- validation-summary.md
```

### Source Code (repository root)

```text
src/
|-- SkiaViewer/
|   |-- CompositorProof.fsi
|   |-- CompositorProof.fs
|   |-- Host/Diagnostics.fsi
|   |-- Host/Diagnostics.fs
|   |-- Host/OpenGl.fsi
|   |-- Host/OpenGl.fs
|   |-- SkiaViewer.fsi
|   `-- SkiaViewer.fs
`-- Testing/
    |-- Testing.fsi
    `-- Testing.fs

tests/
|-- Rendering.Harness/
|   |-- Compositor.fsi
|   |-- Compositor.fs
|   `-- Cli.fs
|-- Rendering.Harness.Tests/
|   |-- Feature157DamageEvidenceTests.fs
|   `-- Feature157ReadinessPackageTests.fs
|-- SkiaViewer.Tests/
|   `-- Feature157NoClearDamageTests.fs
|-- Package.Tests/
|   `-- Feature157CompatibilityTests.fs
`-- Testing.Tests/
    `-- Feature157DamageReadinessHelperTests.fs
```

**Structure Decision**: `SkiaViewer.Host.OpenGl` owns the live no-clear/scissor render decision and
native GL path. `CompositorProof` remains the proof/profile/freshness authority and may gain only
the small diagnostic vocabulary needed to connect accepted proof sets to runtime damage attempts.
`Rendering.Harness` owns `compositor-damage --feature 157`, scenario orchestration, parity capture,
unsupported-host runs, and readiness rendering. `Testing` owns package-facing readiness assertions
only if those assertions are needed by generated products or package validation.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Feature 155 stable profile `probe-08a47c01` remains the accepted correctness proof gate.
- The runtime implementation point is the GL `DirectToSwapchain` path in
  `src/SkiaViewer/Host/OpenGl.fs`; existing pure scissor helpers are not yet the real no-clear path.
- The retained backing gate must prove either trusted current-buffer preservation or explicit
  retained-frame restoration before clipped repaint. Missing or stale backing falls back.
- `compositor-damage --feature 157` is the planned validation command, with
  `compositor-readiness --feature 157` assembling the final package.
- Correctness acceptance requires representative scenarios for static preserved content, localized
  updates, moving content, scrolling or shifted content, and nested retained content.
- Feature 156 timing stays context-only for this feature; the shipped performance claim remains
  `performance-not-accepted` until later gates also pass.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Observable contracts:

- [Compositor Damage Command Contract](contracts/compositor-damage-command.md)
- [Damage-Scissored Render Path Contract](contracts/damage-scissored-render-path.md)
- [Damage Workflow Effects Contract](contracts/damage-workflow-effects.md)
- [Readiness Package Contract](contracts/readiness-package.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Scope and Tier 1 classification remain unchanged after design. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts identify public/observable surfaces before implementation; tasks must put FSI and semantic tests before implementation where package surface changes. |
| Visibility lives in `.fsi` | PASS | Public damage attempt, fallback, and readiness helpers are declared in signature files; GL resource internals stay implementation-only. |
| Idiomatic simplicity | PASS | Design reuses existing `ScissorDecision`, proof readiness, harness feature routing, and markdown readiness renderers. |
| Elmish/MVU boundary | PASS | `damage-workflow-effects.md` defines model, messages, effects, and edge interpreter responsibilities. |
| Test evidence | PASS | Quickstart and contracts require focused tests, real current-host attempts where available, unsupported-host regression, package validation, and broad regression validation. |
| Observability and safe failure | PASS | Rejection reasons, host facts, retained-frame identity, damage status, parity result, artifact paths, and final claim status are required output fields. |
| Tier 1 obligations | PASS | Compatibility, package validation, public-surface drift checks, and readiness closeout are required artifacts. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.

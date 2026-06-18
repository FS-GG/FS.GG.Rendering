# Implementation Plan: Native Proof Capture

**Branch**: `155-native-proof-capture` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/155-native-proof-capture/spec.md`

## Summary

Finish the remaining P7 gap by wiring the native live proof-capture workflow end to end on the
current capable host. The implementation keeps Feature 154's acceptance rules authoritative and
adds the missing runner/interpreter path that detects the host profile, presents sentinel and
damage frames, captures real pixel evidence, writes three current-run attempts, evaluates the
accepted proof set, runs same-profile parity, records the timing decision, and publishes a final
P7 closeout package.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; repository `LangVersion=latest`; warnings as errors.

**Primary Dependencies**: Existing `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Scene`, `FS.GG.UI.Controls`,
`FS.GG.UI.Testing`, and `Rendering.Harness` projects; SkiaSharp `4.147.0-preview.3.1`; Silk.NET
OpenGL/Windowing `2.23.0`; Expecto `10.2.2`; existing harness, PNG, filesystem, process, and
timing helpers. No new runtime dependency is planned.

**Storage**: Durable Feature 155 evidence under `specs/155-native-proof-capture/readiness/`,
including `live-proof/attempts/`, `live-proof/unsupported/`, `parity/`, `timing/`, `fsi/`,
`proof-set.md`, `validation-summary.md`, `compatibility-ledger.md`, `package-validation.md`, and
`regression-validation.md`. Native capture attempts may also write transient harness output under a
caller-provided `--out` directory.

**Testing**: Expecto through `dotnet test`; SkiaViewer tests for proof workflow transitions,
host-readiness classification, artifact quality, and proof-set evaluation; Rendering.Harness tests
for Feature 155 CLI routing and output package assembly; Package/FSI coverage for any public
surface delta; real capable-host quickstart for accepted evidence; unsupported-host regression
with display variables unset.

**Target Platform**: Multi-package F# UI/rendering library on .NET `net10.0`; SkiaSharp over
OpenGL for live viewer proof capture. The current validation target is this X11/GLX-capable host
with direct AMD Mesa rendering, Present/DRI3, and readable graphics device access.

**Project Type**: Multi-package F# rendering/UI library plus desktop viewer host and validation
harness.

**Performance Goals**: Three accepted live proof attempts in one closeout run; 100% accepted
attempts include fresh, decodable, non-blank, non-synthetic sentinel and damage artifacts; 100%
accepted attempts prove damaged-pixel update and undamaged-pixel preservation; unsupported-host
validation completes under 2 minutes with zero accepted artifacts; timing claim remains separate
and accepts no benefit unless same-profile measurements satisfy the declared policy.

**Constraints**: Feature 154 proof-set, parity, timing, and readiness semantics remain
authoritative. Native capture must use an MVU/effect boundary. Missing, stale, blank,
synthetic-only, undecodable, failed-pixel, incomplete, host-mismatched, proof-method-mismatched,
cross-profile, timed-out, or artifact-write-failed evidence fails closed. Partial redraw remains
fallback-gated unless both proof set and same-profile parity are accepted.

**Scale/Scope**: Narrow P7 closeout across `SkiaViewer`, `Rendering.Harness`, readiness docs, and
only the public/package validation surfaces required by observable changes. No new backend, no P8
layout change, no text/overlay behavior change, no proof vocabulary redesign, and no performance
claim from synthetic or environment-limited evidence.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies this as Tier 1 because P7 readiness, diagnostics, fallback behavior, and package evidence are consumer-visible. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Any public proof-capture or readiness surface delta is drafted in `.fsi` and covered by semantic/FSI-style tests before implementation is accepted. |
| Visibility lives in `.fsi` | PASS | Public `SkiaViewer` and `Testing` symbols stay curated by `.fsi`; implementation-only capture helpers remain omitted. |
| Idiomatic simplicity | PASS | The plan reuses Feature 154 contracts and the existing proof workflow instead of adding a new abstraction stack. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Native proof capture exposes workflow state, messages, effects, pure transition, and an edge interpreter for display/readback/filesystem work. |
| Test evidence is mandatory | PASS | Failing-first tests cover transition semantics, interpreter output, artifact rejection, unsupported-host regression, same-profile parity, and readiness closeout. Synthetic tests stay explicitly rejection-only. |
| Observability and safe failure | PASS | Every failed or limited capture path records a reviewer-visible reason and preserves full-redraw fallback. |
| Tier 1 obligations | PASS | Public/observable changes require `.fsi` updates, semantic tests, compatibility notes, readiness evidence, package validation, and broad regression validation. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/155-native-proof-capture/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- native-proof-capture.md
|   |-- proof-workflow-effects.md
|   `-- p7-closeout-summary.md
`-- readiness/
    |-- live-proof/
    |   |-- attempts/
    |   `-- unsupported/
    |-- parity/
    |-- timing/
    |-- fsi/
    |-- proof-set.md
    |-- validation-summary.md
    |-- compatibility-ledger.md
    |-- package-validation.md
    `-- regression-validation.md
```

### Source Code (repository root)

```text
src/
|-- SkiaViewer/
|   |-- CompositorProof.fsi
|   |-- CompositorProof.fs
|   |-- Host/OpenGl.fsi
|   |-- Host/OpenGl.fs
|   |-- SkiaViewer.fsi
|   `-- SkiaViewer.fs
`-- Testing/
    |-- Testing.fsi
    `-- Testing.fs

tests/
|-- SkiaViewer.Tests/
|   |-- Feature155ProofWorkflowTests.fs
|   |-- Feature155NativeCaptureTests.fs
|   `-- Feature155ArtifactRejectionTests.fs
|-- Rendering.Harness/
|   |-- Compositor.fsi
|   |-- Compositor.fs
|   `-- Cli.fs
|-- Rendering.Harness.Tests/
|   `-- Feature155ReadinessTests.fs
|-- Package.Tests/
|   `-- Feature155CompatibilityTests.fs
`-- Testing.Tests/
    `-- Feature155ReadinessHelperTests.fs
```

**Structure Decision**: `SkiaViewer` owns native host profile detection, proof workflow effects,
sentinel/damage presentation, pixel observation, artifact quality, and attempt classification.
`Rendering.Harness` owns CLI routing, multi-attempt orchestration, readiness-package output,
unsupported-host regression output, same-profile parity aggregation, timing decision publication,
and validation summaries. `Testing` changes are optional and only added if package-visible
readiness assertions need a public helper.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- The current machine is capable; the previous environment-limited Feature 154 artifact came from
  an unsupported-host command with display variables intentionally unset.
- The missing work is an executable native proof-capture interpreter, not new acceptance policy.
- Feature 154 exact-three proof-set acceptance remains the gate.
- Accepted P7 correctness requires proof-set acceptance plus same-profile parity; performance
  remains a separate timing claim.
- Unsupported-host output must remain a regression path with zero accepted artifacts.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Observable contracts:

- [Native Proof Capture Contract](contracts/native-proof-capture.md)
- [Proof Workflow Effects Contract](contracts/proof-workflow-effects.md)
- [P7 Closeout Summary Contract](contracts/p7-closeout-summary.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Scope and Tier 1 classification remain unchanged after design. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts identify public/observable surfaces before implementation; tasks will put tests before code. |
| Visibility lives in `.fsi` | PASS | Public symbols stay in existing signature files; private interpreter helpers stay implementation-only. |
| Idiomatic simplicity | PASS | Design reuses existing proof records, readiness renderers, and harness directories. |
| Elmish/MVU boundary | PASS | `proof-workflow-effects.md` defines state/effect/interpreter responsibilities. |
| Test evidence | PASS | Quickstart and tasks require real capable-host acceptance plus unsupported-host regression. |
| Observability and safe failure | PASS | Failure modes are part of the contracts and readiness summary. |
| Tier 1 obligations | PASS | Compatibility, package validation, and readiness closeout are required artifacts. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.

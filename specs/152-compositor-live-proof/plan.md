# Implementation Plan: Compositor Live Proof Acceptance

**Branch**: `152-compositor-live-proof` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/152-compositor-live-proof/spec.md`

## Summary

Close the remaining P7 live compositor acceptance gap recorded after Feature 149. The
implementation validates live partial-redraw acceptance on top of the existing deterministic
proof, parity, timing, diagnostic, and readiness surfaces: three fresh matching capable-host proof
runs are required before damage-scoped redraw can be accepted, same-profile live parity must match
the full-redraw oracle, capable-host timing must either support or reject a performance claim, and
one readiness summary must state whether P7 is accepted, environment-limited, failed, or still
fallback-gated. Feature 149 deterministic readiness and Feature 151 P8 layout acceptance remain
baseline evidence, not reopened scope.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; repository `LangVersion=latest`; warnings as errors.

**Primary Dependencies**: Existing `FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`,
`FS.GG.UI.SkiaViewer`, `FS.GG.UI.Scene`, `FS.GG.UI.Testing`, and `Rendering.Harness` projects;
SkiaSharp `4.147.0-preview.3.1`; Silk.NET OpenGL/Windowing `2.23.0`; Yoga.Net `3.2.3`;
Expecto `10.2.2`; FsCheck `3.3.3`; existing harness/evidence dependencies only. No new runtime
dependency is planned.

**Storage**: Durable evidence under `specs/152-compositor-live-proof/readiness/`, including
`live-proof/`, `parity/`, `timing/`, `compatibility-ledger.md`, and `validation-summary.md`.
Transient harness output remains under command-provided artifact directories.

**Testing**: Expecto/FsCheck through `dotnet test`; semantic FSI-style tests for any public proof,
diagnostic, readiness, and testing-helper deltas; SkiaViewer live and Synthetic-named simulated
failure tests; Rendering.Harness evidence tests and commands for live proof, parity, timing, and
readiness; package surface checks and `scripts/refresh-surface-baselines.fsx` for Tier 1 deltas.

**Target Platform**: Multi-package F# UI/rendering library on .NET `net10.0`; SkiaSharp over
OpenGL for live host proof; deterministic headless/simulated validation for failure and
environment-limited cases; accepted partial-redraw readiness only for capable host profiles with
fresh matching live proof.

**Project Type**: Multi-package F# rendering/UI library plus desktop viewer host and validation
harness.

**Performance Goals**: At least 3 fresh matching capable-host live proof runs before partial
redraw is marked accepted; 100% accepted proof artifacts are present, non-blank, non-synthetic,
and show damaged-region updates plus untouched-region preservation; 100% full-redraw parity for
accepted damage-scoped corpus scenarios; unsupported-host validation completes under 2 minutes
with zero accepted artifacts; timing covers a predeclared threshold/noise policy, at least 5
representative live scenarios, and at least 5 comparable repetitions per scenario before any
performance benefit is accepted.

**Constraints**: Feature 149 deterministic readiness is the baseline; Feature 151 P8 layout
acceptance and unrelated roadmap work are out of scope; missing, stale, blank, synthetic-only,
failed, environment-limited, host-mismatched, proof-method-mismatched, invalid-damage, or
failed-parity evidence fails closed to full redraw; absent or unmet timing threshold/noise policy
rejects or marks the performance claim inconclusive; snapshot/reuse evidence is context-only unless
same-profile live timing also exists; synthetic evidence may test rejection paths but cannot accept
live proof or timing claims; public or observable deltas follow Tier 1 `.fsi`, semantic test,
surface baseline, compatibility ledger, docs, and package-validation discipline.

**Scale/Scope**: Narrow closeout of the P7 live acceptance gate: likely extensions to existing
Feature 149 proof/run aggregation, host readback artifact validation, live parity corpus routing,
timing claim assembly, readiness summary, and compatibility documentation in `SkiaViewer`,
`Controls` diagnostics, `Controls.Elmish` metrics, `Testing` helpers, and `Rendering.Harness`.
No broad compositor redesign, new layout protocol, new widget behavior, or new public Scene
authoring model is included.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies this as Tier 1 because compositor readiness, diagnostics, fallback status, and performance claims can change consumer-visible behavior. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Any public proof, diagnostic, timing, readiness, or testing-helper delta is drafted in `.fsi` and covered by semantic/FSI-style tests before `.fs` bodies are accepted. |
| Visibility lives in `.fsi` | PASS | Existing public modules keep curated `.fsi`; implementation-only helpers stay omitted from signatures. No top-level visibility modifiers are planned in `.fs` files. |
| Idiomatic simplicity | PASS | The plan reuses Feature 149 models, harness commands, and readiness vocabulary. No reflection, SRTP, custom operators, type providers, or new computation expressions are planned. Hot-path mutation remains local and commented when needed. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Live proof aggregation, parity/timing orchestration, artifact writing, and readiness assembly expose or wrap `Model`, `Msg`, `Effect`, pure `update`, and edge interpreters where stateful I/O is introduced or extended. |
| Test evidence is mandatory | PASS | Failing-first tests are required for capable-host acceptance, fail-closed rejection, unsupported-host limitation, same-profile parity, timing decisions, public diagnostics, package validation, and regressions. Synthetic tests are disclosed and cannot satisfy live acceptance. |
| Observability and safe failure | PASS | Proof attempts, accepted proof sets, host profiles, artifact quality, parity status, timing status, fallbacks, compatibility changes, and limitations must be reviewer-visible. Unsafe or unsupported paths retain full redraw. |
| Tier 1 obligations | PASS | `.fsi` updates, semantic tests, surface baseline refresh, compatibility ledger, readiness evidence, docs, focused regression validation, and package checks are required for public or observable changes. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/152-compositor-live-proof/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- live-proof-runset-acceptance.md
|   |-- damage-scoped-live-parity.md
|   |-- timing-claim-decision.md
|   `-- readiness-decision.md
`-- readiness/
    |-- live-proof/
    |-- parity/
    |-- timing/
    |-- compatibility-ledger.md
    `-- validation-summary.md
```

### Source Code (repository root)

```text
src/
|-- Controls/
|   |-- Diagnostics.fsi
|   |-- Diagnostics.fs
|   |-- RetainedRender.fsi
|   `-- RetainedRender.fs
|-- Controls.Elmish/
|   |-- ControlsElmish.fsi
|   `-- ControlsElmish.fs
|-- SkiaViewer/
|   |-- CompositorProof.fsi
|   |-- CompositorProof.fs
|   |-- Host/OpenGl.fsi
|   |-- Host/OpenGl.fs
|   |-- SceneRenderer.fs
|   |-- SkiaViewer.fsi
|   `-- SkiaViewer.fs
`-- Testing/
    |-- Testing.fsi
    `-- Testing.fs

tests/
|-- Controls.Tests/
|   `-- Feature152*.fs
|-- Elmish.Tests/
|   `-- Feature152*.fs
|-- SkiaViewer.Tests/
|   |-- Feature152LiveProofTests.fs
|   |-- Feature152LiveProofSimulationTests.fs
|   `-- Feature152DamageScopedRedrawTests.fs
|-- Rendering.Harness/
|   |-- Compositor.fsi
|   |-- Compositor.fs
|   |-- Evidence.fsi
|   |-- Evidence.fs
|   |-- Perf.fsi
|   |-- Perf.fs
|   `-- Cli.fs
|-- Rendering.Harness.Tests/
|   `-- Feature152*.fs
|-- Package.Tests/
|   `-- Feature152*.fs
`-- Testing.Tests/
    `-- Feature152*.fs
```

**Structure Decision**: Continue the Feature 149 package boundaries. `SkiaViewer` owns live
OpenGL profile detection, sentinel/damage readback, scissor/no-clear host state, and proof
freshness. `Controls` owns retained damage/fallback diagnostics that feed scoped redraw decisions.
`Controls.Elmish` owns public frame-level compositor diagnostics exposed through the Elmish
adapter. `Rendering.Harness` owns corpus orchestration, artifact I/O, timing probes, and readiness
assembly. `Testing` receives only narrow consumer-facing readiness helpers. Feature 152 must reuse
existing Feature 149 contract names where compatible and add only the minimum Feature 152 routing,
run-set aggregation, and readiness fields needed for live acceptance.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Feature 152 is a live acceptance closeout on top of Feature 149, not a new compositor design.
- Accepted live proof requires three fresh matching capable-host runs for the same host profile
  and proof method, with non-blank real artifacts that show damaged updates and untouched
  preservation.
- Unsupported or unavailable presentation environments remain valid evidence only as
  `environment-limited`; they never accept partial redraw or performance claims.
- Damage-scoped parity must be evaluated on the same accepted host profile and compared against
  the full-redraw oracle for every representative live scenario.
- Timing claims require same-profile, repeated, comparable measurements and must be explicitly
  rejected or inconclusive when data is noisy, incomplete, environment-limited, or not beneficial.
- The readiness package is the single review entry point and must include compatibility impact for
  any public diagnostic or package-facing change.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Public or observable contracts:

- [Live Proof Run-Set Acceptance Contract](contracts/live-proof-runset-acceptance.md)
- [Damage-Scoped Live Parity Contract](contracts/damage-scoped-live-parity.md)
- [Timing Claim Decision Contract](contracts/timing-claim-decision.md)
- [Readiness Decision Contract](contracts/readiness-decision.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Tier 1 artifact chain | PASS | Contracts require `.fsi` first for public proof, diagnostics, timing, readiness, and testing surfaces; semantic tests; surface baseline refresh; compatibility ledger; readiness output; and package validation. |
| Dependency boundaries | PASS | Pure damage/fallback policy remains in `Controls`; host-dependent proof/readback/scissor behavior remains in `SkiaViewer`; evidence, filesystem, process, and timing I/O remain in `Rendering.Harness`; consumer validation helpers remain narrow in `Testing`. |
| Determinism and safe failure | PASS | Data model requires stable scenario ids, proof ids, run-set ids, host-profile matching, artifact quality checks, deterministic verdicts, full-redraw fallback, and explicit limited/failed/rejected verdicts. |
| Real evidence and synthetic disclosure | PASS | Synthetic tests are allowed for failure paths but synthetic-only or environment-limited artifacts cannot accept partial redraw or performance benefits. Capable-host proof and timing claims require real live evidence. |
| MVU/I/O boundary | PASS | Live proof run-set aggregation, timing claim assembly, and readiness publication define model/message/effect responsibilities with edge interpreters for GL, filesystem, process, timing, and artifact I/O. |

No constitution violations are introduced by the design.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | N/A | N/A |

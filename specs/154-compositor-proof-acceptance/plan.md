# Implementation Plan: Compositor Proof Acceptance

**Branch**: `154-compositor-proof-acceptance` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/154-compositor-proof-acceptance/spec.md`

## Summary

Close the remaining P7 live partial-redraw acceptance gate using the Feature 153 proof
interpreter and vocabulary. The implementation must attempt a real capable-host proof set,
accept only exactly three fresh matching attempts from one host profile and proof method, run
the same-profile damage-scoped parity corpus, make an explicit timing-claim decision, and publish
one final readiness summary that keeps partial redraw fallback-gated unless proof and parity are
both current and accepted.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; repository `LangVersion=latest`; warnings as errors.

**Primary Dependencies**: Existing `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Controls`,
`FS.GG.UI.Controls.Elmish`, `FS.GG.UI.Scene`, `FS.GG.UI.Testing`, and `Rendering.Harness`
projects; SkiaSharp `4.147.0-preview.3.1`; Silk.NET OpenGL/Windowing `2.23.0`; Expecto
`10.2.2`; FsCheck `3.3.3`; existing X11/Xvfb, GL, PNG, filesystem, process, timing, and harness
helpers only. No new runtime dependency is planned.

**Storage**: Durable evidence under `specs/154-compositor-proof-acceptance/readiness/`,
including `live-proof/attempts/`, `live-proof/unsupported/`, `parity/`, `timing/`, `fsi/`,
`proof-set.md`, `validation-summary.md`, `compatibility-ledger.md`, `package-validation.md`, and
`regression-validation.md`. Transient harness output remains under command-provided `--out`
directories.

**Testing**: Expecto/FsCheck through `dotnet test`; semantic FSI-style tests for public proof,
parity, timing, readiness, diagnostic, or testing-helper deltas; SkiaViewer tests for exact-three
proof-set acceptance and fail-closed proof quality; Rendering.Harness command/output tests for
Feature 154 proof, parity, timing, readiness, unsupported-host, and compatibility artifacts;
Package.Tests surface and compatibility checks for Tier 1 public drift.

**Target Platform**: Multi-package F# UI/rendering library on .NET `net10.0`; SkiaSharp over
OpenGL for capable-host proof; deterministic and Synthetic-named tests only for rejection,
failure, and environment-limited paths; accepted readiness only for current real artifacts from
one capable host profile.

**Project Type**: Multi-package F# rendering/UI library plus desktop viewer host and validation
harness.

**Performance Goals**: Exactly three fresh matching capable-host attempts before accepting the
host proof gate; 100% of accepted attempts include fresh, decodable, non-blank, non-synthetic
sentinel and damage evidence; 100% accepted parity scenarios match full redraw; unsupported-host
validation completes under 2 minutes with zero accepted partial-redraw artifacts; any accepted
performance benefit requires at least five representative live scenarios with at least five
comparable repetitions per scenario and a declared threshold/noise policy.

**Constraints**: Feature 153's proof interpreter, selected-attempt identity, host-readiness
classification, and proof-set vocabulary are authoritative. Missing, stale, blank, synthetic-only,
undecodable, failed-pixel, incomplete, host-mismatched, proof-method-mismatched, cross-profile, or
environment-limited evidence fails closed with reviewer-visible reasons. Same-profile parity is
required before partial redraw can be accepted for a host. Timing evidence can accept, reject, or
mark a performance claim inconclusive, but missing or non-beneficial timing must not block safety
readiness when proof and parity are accepted.

**Scale/Scope**: Narrow P7 closeout across `SkiaViewer`, `Rendering.Harness`, and only the
minimal `Controls`, `Controls.Elmish`, `Testing`, docs, package, and baseline surfaces needed for
observable readiness, fallback, diagnostic, or compatibility changes. No P8 layout change, text
shaping change, overlay behavior change, portable rendering backend, proof vocabulary redesign,
or synthetic performance claim is in scope.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies this as Tier 1 because readiness status, diagnostics, fallback behavior, package-facing claims, and performance claims can be consumer-visible. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Any public proof, parity, timing, readiness, diagnostic, or testing-helper delta must be drafted in `.fsi` and covered by semantic/FSI-style tests before `.fs` bodies are accepted. |
| Visibility lives in `.fsi` | PASS | Existing public modules keep curated `.fsi`; implementation-only harness or interpreter helpers stay omitted from signatures. |
| Idiomatic simplicity | PASS | The plan reuses Feature 153 proof types, renderer vocabulary, and harness patterns. No reflection, SRTP, custom operators, type providers, or new computation expressions are planned. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Proof-set selection, parity corpus orchestration, timing measurement assembly, artifact writing, and readiness publication expose or wrap `Model`, `Msg`, `Effect`, pure `update`, and edge interpreters where stateful I/O is introduced or extended. |
| Test evidence is mandatory | PASS | Failing-first tests are required for accepted proof sets, rejected proof quality, unsupported hosts, same-profile parity, timing decisions, final readiness, compatibility, package validation, and adjacent regressions. Synthetic tests are disclosed and cannot satisfy live acceptance. |
| Observability and safe failure | PASS | Every proof, parity, timing, fallback, compatibility, and limitation decision must record reviewer-visible reasons and safe full-redraw fallback for unsafe or unsupported paths. |
| Tier 1 obligations | PASS | Public/observable changes require `.fsi` updates, semantic tests, surface baseline refresh, compatibility ledger, readiness evidence, docs, focused regression validation, and package checks. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/154-compositor-proof-acceptance/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- capable-host-proof-set.md
|   |-- damage-scoped-parity-corpus.md
|   |-- timing-decision.md
|   `-- p7-readiness-summary.md
`-- readiness/
    |-- live-proof/
    |   |-- attempts/
    |   `-- unsupported/
    |-- parity/
    |-- timing/
    |-- fsi/
    |-- validation-plan.md
    |-- proof-set.md
    |-- validation-summary.md
    |-- compatibility-ledger.md
    |-- package-validation.md
    `-- regression-validation.md
```

Additional repo-level readiness artifacts when public surfaces change:

```text
readiness/
`-- surface-baselines/
    |-- FS.GG.UI.Controls.txt
    |-- FS.GG.UI.Controls.Elmish.txt
    |-- FS.GG.UI.SkiaViewer.txt
    `-- FS.GG.UI.Testing.txt
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
|   |-- Host/Viewer.fsi
|   |-- Host/Viewer.fs
|   |-- SkiaViewer.fsi
|   `-- SkiaViewer.fs
`-- Testing/
    |-- Testing.fsi
    `-- Testing.fs

tests/
|-- Controls.Tests/
|   `-- Feature154*.fs
|-- Elmish.Tests/
|   `-- Feature154*.fs
|-- SkiaViewer.Tests/
|   |-- Feature154ProofSetAcceptanceTests.fs
|   |-- Feature154LiveProofHostTests.fs
|   |-- Feature154DamageScopedParityTests.fs
|   `-- Feature154SyntheticRejectionTests.fs
|-- Rendering.Harness/
|   |-- Compositor.fsi
|   |-- Compositor.fs
|   |-- Cli.fs
|   |-- Evidence.fsi
|   |-- Evidence.fs
|   |-- Perf.fsi
|   `-- Perf.fs
|-- Rendering.Harness.Tests/
|   |-- Feature154ProofAcceptanceTests.fs
|   |-- Feature154ParityCorpusTests.fs
|   |-- Feature154TimingDecisionTests.fs
|   `-- Feature154ReadinessPackageTests.fs
|-- Package.Tests/
|   `-- Feature154*.fs
`-- Testing.Tests/
    `-- Feature154*.fs
```

**Structure Decision**: Continue the Feature 153 package boundaries. `SkiaViewer` owns live
OpenGL host profile detection, sentinel/damage presentation, readback quality, damaged/undamaged
sample validation, proof-attempt classification, and exact-three proof-set evaluation.
`Rendering.Harness` owns Feature 154 command routing, artifact I/O, same-profile proof/parity/timing
aggregation, readiness summaries, compatibility notes, and regression evidence. `Controls` and
`Controls.Elmish` remain sources of retained damage and fallback diagnostics only when the parity
corpus or consumer-visible diagnostics need narrow deltas. `Testing` receives only package-visible
helper changes needed to validate readiness output.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Feature 154 is the acceptance closeout for Feature 153's environment-limited proof gate, not a
  new compositor proof vocabulary.
- Feature 153's `CompositorProof.evaluateProofSet` semantics remain the acceptance baseline:
  exactly three selected, fresh, accepted attempts from one host profile and proof method.
- Unsupported-host runs remain regression evidence with zero accepted partial-redraw artifacts.
- Same-profile damage-scoped parity must cover all required corpus paths and compare against the
  full-redraw reference before partial redraw can be accepted for the host.
- Timing evidence is a separate claim decision and cannot be inferred from proof, parity, reuse, or
  snapshot evidence.
- The readiness package is the single review entry point and must include compatibility and public
  surface validation for all observable drift.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Public or observable contracts:

- [Capable Host Proof Set Contract](contracts/capable-host-proof-set.md)
- [Damage-Scoped Parity Corpus Contract](contracts/damage-scoped-parity-corpus.md)
- [Timing Decision Contract](contracts/timing-decision.md)
- [P7 Readiness Summary Contract](contracts/p7-readiness-summary.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Tier 1 artifact chain | PASS | Contracts require `.fsi` first for public proof, parity, timing, readiness, diagnostics, and testing helper deltas; semantic tests; surface baseline refresh; compatibility ledger; readiness output; and package validation. |
| Dependency boundaries | PASS | Host-dependent proof/readback remains in `SkiaViewer`; evidence, filesystem, process, timing, and readiness I/O remain in `Rendering.Harness`; retained damage diagnostics remain in `Controls`/`Controls.Elmish`; consumer validation helpers remain narrow in `Testing`. |
| Determinism and safe failure | PASS | Data model requires stable proof-set ids, selected attempt ids, same host profile, same proof method, artifact quality checks, parity/fallback reasons, timing policy, and explicit failed/limited/rejected verdicts. |
| Real evidence and synthetic disclosure | PASS | Synthetic tests are allowed only for rejection and environment-limited paths. Synthetic-only, unsupported, stale, or cross-profile artifacts cannot accept proof, parity, or performance claims. |
| MVU/I/O boundary | PASS | Proof-set selection, parity corpus execution, timing assembly, and readiness publication define model/message/effect responsibilities with edge interpreters for GL, filesystem, process, timing, artifact I/O, and environment limits. |

No constitution violations are introduced by the design.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | N/A | N/A |

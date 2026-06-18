# Implementation Plan: Compositor Proof Interpreter

**Branch**: `153-compositor-proof-interpreter` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/153-compositor-proof-interpreter/spec.md`

## Summary

Implement the next P7 compositor readiness slice: a real host-backed live proof interpreter that
runs sentinel and damage-scoped frame attempts, validates the resulting artifacts, classifies each
attempt with the existing Feature 152 proof vocabulary, and publishes a reviewable proof-set
decision. This feature does not enable partial redraw by default and does not accept a compositor
performance claim. It produces the live evidence required before later same-host parity and timing
gates can safely make those decisions.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; repository `LangVersion=latest`; warnings as errors.

**Primary Dependencies**: Existing `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Scene`,
`FS.GG.UI.Testing`, and `Rendering.Harness` projects; SkiaSharp `4.147.0-preview.3.1`;
Silk.NET OpenGL/Windowing `2.23.0`; Expecto `10.2.2`; FsCheck `3.3.3`; existing X11/Xvfb,
GL, PNG, filesystem, and harness helpers only. No new runtime dependency is planned.

**Storage**: Durable evidence under `specs/153-compositor-proof-interpreter/readiness/`,
including `live-proof/`, `live-proof/attempts/`, `live-proof/unsupported/`,
`proof-set.md`, `validation-summary.md`, `compatibility-ledger.md`,
`package-validation.md`, and `regression-validation.md`. Transient harness output remains under
the command-provided `--out` directory.

**Testing**: Expecto/FsCheck through `dotnet test`; semantic FSI-style tests for any public
`CompositorProof`, readiness, or testing-helper deltas; SkiaViewer tests for proof-attempt
classification and damage/undamaged sample validation; Rendering.Harness command/output tests for
capable-host, failure, and unsupported-host evidence; Package.Tests surface and compatibility
checks for Tier 1 public drift.

**Target Platform**: Multi-package F# UI/rendering library on .NET `net10.0`; SkiaSharp over
OpenGL for live proof; deterministic and synthetic-named tests only for fail-closed and
environment-limited paths; accepted proof only for capable host profiles with fresh real
artifacts.

**Project Type**: Multi-package F# rendering/UI library plus desktop viewer host and validation
harness.

**Performance Goals**: Unsupported-host validation completes under 2 minutes with zero accepted
partial-redraw artifacts. Capable-host validation records a complete classification for every
attempt. Accepted attempts require decodable, non-blank, non-synthetic, fresh sentinel and damage
artifacts that show damaged-pixel update and undamaged-pixel preservation. This feature records no
accepted performance benefit.

**Constraints**: Feature 152 proof-set vocabulary is the acceptance language; missing, stale,
blank, synthetic-only, undecodable, failed, environment-limited, host-mismatched, or
proof-method-mismatched evidence fails closed; proof-set acceptance requires an explicit set of
exactly three fresh matching capable-host attempts; unsupported hosts remain
`environment-limited`; partial redraw remains fallback-gated unless this proof set is accepted and
later same-profile parity requirements pass; performance claims remain unaccepted until later
same-profile live timing evidence satisfies a declared threshold and noise policy.

**Scale/Scope**: Narrow live proof interpreter slice across `SkiaViewer` and
`Rendering.Harness`, with `Testing` and package baselines touched only for public or
package-visible readiness/helper changes. No broad compositor redesign, no layout or text-shaping
change, no browser backend, no default partial-redraw enablement, and no timing claim acceptance.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies this as Tier 1 because compositor readiness evidence, diagnostics, and fallback status are consumer-visible. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Any public proof-attempt, proof-set, readiness, or testing-helper delta must be drafted in `.fsi` and covered by semantic/FSI-style tests before `.fs` bodies are accepted. |
| Visibility lives in `.fsi` | PASS | Existing public modules keep curated `.fsi`; implementation-only interpreter helpers stay omitted from signatures. |
| Idiomatic simplicity | PASS | The plan reuses Feature 152 models, tokens, and readiness vocabulary. No reflection, SRTP, custom operators, type providers, or new computation expressions are planned. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Proof attempt execution, artifact validation, proof-set aggregation, and readiness publication expose or wrap `Model`, `Msg`, `Effect`, pure `update`, and edge interpreters where workflow state or I/O is introduced. |
| Test evidence is mandatory | PASS | Failing-first tests are required for accepted attempts, rejected quality, unsupported hosts, proof-set aggregation, readiness output, public drift, and adjacent regression checks. Synthetic tests are disclosed and cannot satisfy acceptance. |
| Observability and safe failure | PASS | Every attempt records host profile, proof method, artifacts, quality, freshness, classification, and reason. Unsafe or unsupported paths retain full redraw and record reviewer-visible reasons. |
| Tier 1 obligations | PASS | Public/observable changes require `.fsi` updates, semantic tests, surface baseline refresh, compatibility ledger, readiness evidence, docs, focused regression validation, and package checks. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/153-compositor-proof-interpreter/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- live-proof-attempt.md
|   |-- proof-set-decision.md
|   `-- readiness-summary.md
`-- readiness/
    |-- live-proof/
    |   |-- attempts/
    |   `-- unsupported/
    |-- fsi/
    |   |-- compositor-proof-interpreter-authoring.fsx
    |   `-- compositor-proof-interpreter-authoring.log
    |-- proof-set.md
    |-- validation-summary.md
    |-- compatibility-ledger.md
    |-- package-validation.md
    `-- regression-validation.md
```

Additional repo-level readiness artifacts:

```text
readiness/
`-- surface-baselines/
    |-- FS.GG.UI.SkiaViewer.txt
    `-- FS.GG.UI.Testing.txt
```

### Source Code (repository root)

```text
src/
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
|-- SkiaViewer.Tests/
|   |-- Feature153LiveProofInterpreterTests.fs
|   |-- Feature153LiveProofHostTests.fs
|   `-- Feature153LiveProofSimulationTests.fs
|-- Rendering.Harness/
|   |-- Compositor.fsi
|   |-- Compositor.fs
|   |-- Cli.fs
|   |-- Live.fsi
|   `-- Live.fs
|-- Rendering.Harness.Tests/
|   |-- Feature153LiveProofEvidenceTests.fs
|   `-- Feature153ReadinessPackageTests.fs
|-- Package.Tests/
|   |-- Feature153CompatibilityLedgerTests.fs
|   `-- FsiTranscriptCoverageTests.fs
`-- Testing.Tests/
    `-- Feature153ReadinessHelperTests.fs
```

**Structure Decision**: Continue the Feature 152 package boundaries. `SkiaViewer` owns live
OpenGL host detection, sentinel/damage presentation, readback, sample comparison, and proof
attempt classification. `Rendering.Harness` owns command routing, artifact I/O, attempt
aggregation, unsupported-host runs, readiness summaries, compatibility notes, and regression
evidence. `Testing` receives only narrow consumer-facing helper deltas if the implementation needs
new package-visible readiness validation. Existing `Controls`, `Controls.Elmish`, layout,
render-anywhere, text-shaping, and overlay behavior remain regression inputs, not primary change
sites.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Feature 153 is a live proof interpreter/evidence slice, not a new P7 policy or performance
  claim.
- Feature 152 proof-set vocabulary remains authoritative for `accepted`, `fallback-gated`,
  `failed`, and `environment-limited` decisions.
- A proof attempt is accepted only from current-run real artifacts that validate damaged-pixel
  update and undamaged-pixel preservation.
- Unsupported host paths are useful validation evidence but record zero accepted partial-redraw
  artifacts.
- An accepted proof set is an explicit set of exactly three fresh matching capable-host attempts.
- Readiness output must keep partial redraw fallback-gated unless proof is accepted and later
  same-profile parity gates pass.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Public or observable contracts:

- [Live Proof Attempt Contract](contracts/live-proof-attempt.md)
- [Proof Set Decision Contract](contracts/proof-set-decision.md)
- [Readiness Summary Contract](contracts/readiness-summary.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Tier 1 artifact chain | PASS | Contracts require `.fsi` first for public proof-attempt, proof-set, readiness, and testing surfaces; semantic tests; surface baseline refresh; compatibility ledger; readiness output; and package validation. |
| Dependency boundaries | PASS | Host-dependent proof execution remains in `SkiaViewer`; evidence, filesystem, process, and readiness I/O remain in `Rendering.Harness`; consumer validation helpers remain narrow in `Testing`. |
| Determinism and safe failure | PASS | Data model requires stable attempt ids, host-profile matching, proof-method matching, artifact quality checks, freshness checks, full-redraw fallback, and explicit limited/failed verdicts. |
| Real evidence and synthetic disclosure | PASS | Synthetic tests are allowed for rejection paths only. Synthetic-only or environment-limited artifacts cannot accept proof attempts or proof sets. |
| MVU/I/O boundary | PASS | Attempt interpretation, proof-set aggregation, and readiness publication define model/message/effect responsibilities with edge interpreters for GL, X11/Xvfb, filesystem, process, and artifact I/O. |

No constitution violations are introduced by the design.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | N/A | N/A |

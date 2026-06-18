# Implementation Plan: Same-Profile Timing Evidence

**Branch**: `156-same-profile-timing` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/156-same-profile-timing/spec.md`

## Summary

Add a Feature 156 timing evidence package that compares full-redraw and damage-scoped redraw on
the same accepted P7 host profile from Feature 155. The implementation adds the canonical
`compositor-performance --feature 156` harness route, records warmup and measured distributions
for at least five representative compositor scenarios, rejects noisy, incomplete, cross-profile,
environment-limited, or non-beneficial evidence, and publishes one reviewer-facing timing summary.
Correctness acceptance remains owned by Feature 155 proof plus same-profile parity, and the shipped
P7 performance claim remains `performance-not-accepted` until the later report-defined gates pass.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; repository `LangVersion=latest`; warnings as errors.

**Primary Dependencies**: Existing `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Scene`, `FS.GG.UI.Controls`,
`FS.GG.UI.Testing`, and `Rendering.Harness` projects; SkiaSharp `4.147.0-preview.3.1`; Silk.NET
OpenGL/Windowing `2.23.0`; Expecto `10.2.2`; existing probe, native host, filesystem, PNG, and
timing helpers. No new runtime dependency is planned.

**Storage**: Durable Feature 156 evidence under `specs/156-same-profile-timing/readiness/`,
including `timing/summary.md`, `timing/scenarios/`, `timing/raw/`, `timing/unsupported/`, `fsi/`,
`compatibility-ledger.md`, `package-validation.md`, `regression-validation.md`, and
`validation-summary.md`. Timing command output may also write transient samples under a
caller-provided `--out` directory.

**Testing**: Expecto through `dotnet test`; Rendering.Harness tests for command routing, policy
evaluation, distribution formatting, cross-profile rejection, and readiness package assembly;
SkiaViewer tests for full-redraw and damage-scoped timing path selection if public viewer behavior
changes; Package/FSI coverage for any public surface delta; unsupported-host regression with
display variables unset.

**Target Platform**: Multi-package F# UI/rendering library on .NET `net10.0`; SkiaSharp over
OpenGL for live viewer and evidence capture. The accepted timing target is the Feature 155 stable
host profile `probe-08a47c01`; timing output must record enough host facts to reject any run from
another profile, display environment, renderer identity, refresh source, package version, scenario
definition, or run identity.

**Project Type**: Multi-package F# rendering/UI library plus desktop viewer host and validation
harness.

**Performance Goals**: At least five representative scenarios; default warmup count `3`; at least
five measured repetitions per path per scenario after warmup; 100% of scenario summaries record
p50, p95, p99, measured sample count, warmup count, declared noise band, confidence decision, and
artifact paths; unsupported-host validation completes under 2 minutes with zero accepted
performance artifacts.

**Constraints**: Feature 155 proof, parity, fallback, and correctness vocabulary stays
authoritative. The Feature 156 policy id is `same-profile-live-threshold-v2`. The scenario noise
band is `max(0.25 ms, 5% of full-redraw p50)`. A scenario is positive only when damage-scoped p50
and p95 are each faster than full redraw by at least the noise band and damage-scoped p99 is not
worse than full-redraw p99 by more than the noise band. Missing, stale, unreadable, duplicated,
incomplete, noisy, cross-profile, environment-limited, or non-beneficial evidence fails closed with
reviewer-visible reasons. Timing results must disclose whether proof readback or validation
overhead is included; if the measured path is readback-dominated or cannot prove separation, the
timing result is limited and cannot support a shipped claim.

**Scale/Scope**: Narrow P7 performance-evidence slice across `Rendering.Harness`, `SkiaViewer`
only where timing path selection needs package-visible support, readiness docs, focused tests, and
package/FSI validation. Out of scope: Feature 157 no-clear damage-scissored renderer, Feature 158
readback separation, Feature 159 layer promotion/key splitting, Feature 160 validation throughput,
Feature 161 full host performance lane ledger, P8 layout, text shaping, overlay behavior, and any
universal performance claim.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies the feature as Tier 1 because performance evidence, readiness summaries, diagnostics, and claim status are consumer-visible. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Any public timing policy, command surface, distribution record, verdict token, or readiness helper is drafted in `.fsi` and covered by semantic/FSI-style tests before implementation is accepted. |
| Visibility lives in `.fsi` | PASS | Public `SkiaViewer`, `Testing`, and harness symbols stay curated by `.fsi`; implementation-only sample collection helpers stay omitted. |
| Idiomatic simplicity | PASS | The plan extends Feature 154/155 readiness vocabulary and harness structure instead of introducing a new benchmark framework. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Timing collection is modeled as pure workflow state/messages/effects with an edge interpreter for probe, render/present, timing, and filesystem work. |
| Test evidence is mandatory | PASS | Failing-first tests cover policy thresholds, sample-count rejection, same-profile enforcement, noisy/non-beneficial evidence, unsupported-host regression, and readiness summary output. Synthetic timing fixtures are rejection-only. |
| Observability and safe failure | PASS | Every rejected or limited timing path records a reason and preserves Feature 155 correctness and full-redraw fallback behavior. |
| Tier 1 obligations | PASS | Public/observable changes require `.fsi` updates, semantic tests, surface baselines if needed, compatibility notes, readiness evidence, package validation, and broad regression validation. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/156-same-profile-timing/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- compositor-performance-command.md
|   |-- same-profile-timing-evidence.md
|   |-- timing-summary-package.md
|   `-- timing-workflow-effects.md
`-- readiness/
    |-- timing/
    |   |-- scenarios/
    |   |-- raw/
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
|   |-- SkiaViewer.fsi
|   |-- SkiaViewer.fs
|   |-- CompositorProof.fsi
|   `-- CompositorProof.fs
`-- Testing/
    |-- Testing.fsi
    `-- Testing.fs

tests/
|-- Rendering.Harness/
|   |-- Compositor.fsi
|   |-- Compositor.fs
|   |-- Cli.fs
|   |-- Perf.fsi
|   `-- Perf.fs
|-- Rendering.Harness.Tests/
|   |-- Feature156TimingEvidenceTests.fs
|   `-- Feature156ReadinessPackageTests.fs
|-- SkiaViewer.Tests/
|   `-- Feature156CompositorTimingTests.fs
|-- Package.Tests/
|   `-- Feature156CompatibilityTests.fs
`-- Testing.Tests/
    `-- Feature156TimingHelperTests.fs
```

**Structure Decision**: `Rendering.Harness` owns command parsing, profile binding, scenario
orchestration, policy evaluation, raw sample output, per-scenario reports, and readiness-package
assembly. `Perf` owns reusable timing primitives and distribution calculation. `SkiaViewer` is
touched only if the implementation must expose or select distinct full-redraw and damage-scoped
viewer paths through package-visible contracts. `Testing` owns the package-facing timing assertion
helper planned for this feature, and its public surface must be validated through `.fsi`, focused
tests, compatibility notes, and surface-baseline review.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Feature 155 stable profile `probe-08a47c01` is the accepted correctness baseline.
- `compositor-performance --feature 156` is the canonical Feature 156 command; earlier
  `compositor-timing` output remains context-only for prior features.
- `same-profile-live-threshold-v2` declares the exact noise band and positive-result rule.
- The minimum positive-decision scenario set is five damage-benefit scenarios:
  `timing/localized-update`, `timing/no-change`, `timing/movement-old-new`,
  `timing/overlap`, and `timing/edge-clipping`.
- Feature 156 records enough host facts for same-profile rejection but does not replace the later
  Feature 161 host performance lane ledger.
- Timing output remains separate from shipped P7 performance acceptance until Features 157, 158,
  159, and 161 also pass.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Observable contracts:

- [Compositor Performance Command Contract](contracts/compositor-performance-command.md)
- [Same-Profile Timing Evidence Contract](contracts/same-profile-timing-evidence.md)
- [Timing Summary Package Contract](contracts/timing-summary-package.md)
- [Timing Workflow Effects Contract](contracts/timing-workflow-effects.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Scope and Tier 1 classification remain unchanged after design. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts identify public/observable surfaces before implementation; tasks will put tests and FSI authoring before code. |
| Visibility lives in `.fsi` | PASS | Public timing records, tokens, and helpers are declared only through signature files; private timing loop details remain implementation-only. |
| Idiomatic simplicity | PASS | Design reuses existing harness directories, `ReadinessModel` style, and distribution formatting patterns. |
| Elmish/MVU boundary | PASS | `timing-workflow-effects.md` defines workflow state, messages, effects, and edge interpreter responsibilities. |
| Test evidence | PASS | Quickstart and contracts require focused tests, real same-profile timing evidence where available, unsupported-host regression, package validation, and broad regression validation. |
| Observability and safe failure | PASS | Rejection reasons, host facts, artifact paths, overhead disclosure, and fallback status are required output fields. |
| Tier 1 obligations | PASS | Compatibility, package validation, public-surface drift checks, and readiness closeout are required artifacts. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.

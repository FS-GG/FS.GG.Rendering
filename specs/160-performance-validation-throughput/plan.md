# Implementation Plan: Performance Validation Throughput

**Branch**: `160-performance-validation-throughput` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/160-performance-validation-throughput/spec.md`

## Summary

Implement Feature 160 by adding a bounded focused P7 performance validation lane that can be run
repeatedly without invoking the broad release suite for each timing iteration. The lane preserves
Feature 158 timing scenario and sample-policy comparability, records accepted and excluded
iterations under a reviewer-readable readiness package, and keeps full solution validation as the
separate release gate. A passing focused throughput result may accept validation throughput, but it
does not accept the shipped compositor performance claim without non-noisy same-profile timing,
Feature 159 net-positive reuse/promotion evidence, and the later Feature 161 host-lane scoping gate.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; repository `LangVersion=latest`; public surface
curated through `.fsi` files.

**Primary Dependencies**: Existing `FS.GG.UI.Controls`, `FS.GG.UI.SkiaViewer`,
`FS.GG.UI.Testing`, and `Rendering.Harness` projects; SkiaSharp `4.147.0-preview.3.1`;
Silk.NET OpenGL/Windowing `2.23.0`; Expecto `10.2.2`; prior Feature 155 proof,
Feature 157 no-clear damage readiness, Feature 158 readback-free measurement policy, and
Feature 159 reuse/promotion readiness. No new runtime dependency is planned.

**Storage**: Durable Feature 160 evidence under
`specs/160-performance-validation-throughput/readiness/`, including `throughput/iterations/`,
`throughput/raw/`, `throughput/excluded/`, `throughput/unsupported/`, `full-validation/`,
`fsi/`, `compatibility-ledger.md`, `package-validation.md`, `regression-validation.md`, and
`validation-summary.md`. Transient harness output may be written to a caller-provided `--out`
directory before accepted records are copied into readiness.

**Testing**: Expecto through `dotnet test`; Rendering.Harness tests for Feature 160 focused-lane
routing, time-bound enforcement, scenario coverage, metadata, unsupported-host behavior, readiness
rendering, and no broad-suite invocation during focused iterations; Testing helper tests if a
package-visible readiness helper is added; Package tests for compatibility and surface drift;
focused preservation checks for Features 155, 157, 158, and 159; broad solution validation remains
the final release gate.

**Target Platform**: Multi-package F# UI/rendering library on .NET `net10.0`; SkiaSharp over
OpenGL for the live viewer host. Accepted throughput targets the same stable host profile used by
Features 155-159: `probe-08a47c01`. Unsupported or unavailable presentation environments fail
closed with zero accepted same-profile performance artifacts.

**Project Type**: Multi-package F# rendering/UI library plus desktop viewer host and validation
harness.

**Performance Goals**: Each accepted focused iteration completes within the declared 10 minute
bound, records the five Feature 158 timing scenarios, preserves warmup `3` and measured
repetitions `5` per path per scenario, and publishes duration, bound, scenario coverage, sample
count, exclusions, host profile, run identity, scenario identity, artifact paths, and inclusion
status. Accepted Feature 160 throughput requires at least three fresh same-profile focused
iterations within the bound. Unsupported-host validation completes within 2 minutes and records
zero accepted performance artifacts.

**Constraints**: Focused performance validation never replaces full solution validation. Timed-out,
canceled, partial, cross-profile, stale, mixed-policy, missing-metadata, unsupported-host,
environment-limited, scenario-coverage-missing, sample-policy-mismatch, run-identity-mismatch,
artifact-unreadable, readback-contaminated, or undocumented iterations cannot be accepted as
throughput evidence. Noisy same-profile timing is reported as a remaining performance-claim gate,
but does not exclude focused throughput by itself. The shipped compositor performance claim remains
`performance-not-accepted` unless all report-defined timing, reuse/promotion, throughput, and
host-lane gates are complete and positive.

**Scale/Scope**: Narrow P7 validation-throughput slice across `tests/Rendering.Harness`,
`tests/Rendering.Harness.Tests`, `src/Testing` and `tests/Testing.Tests` only if package-visible
helpers are needed, `tests/Package.Tests`, readiness docs, and surface baselines if public `.fsi`
changes. Out of scope: changing correctness proof requirements, proof/readback separation,
Feature 159 promotion behavior, Feature 161 host performance lane ledger, broadening host support,
P8 layout, text shaping, overlay behavior, and final shipped compositor performance acceptance by
itself.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies the work as Tier 1 because validation readiness semantics and possible package-facing diagnostics change. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Any public focused-lane status, throughput helper, exclusion reason, readiness result, command output, or artifact contract is specified here before implementation and must be exercised through semantic/FSI tests when package-visible. |
| Visibility lives in `.fsi` | PASS | Package-visible `Testing` or harness-facing additions must be declared in signature files; implementation-only timeout and artifact plumbing stays omitted. |
| Idiomatic simplicity | PASS | The plan extends existing Feature 158 timing and Feature 159 readiness patterns rather than adding a new validation framework or dependency. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Focused iteration orchestration is modeled as pure workflow state, messages, and effects; process, timer, GL, and filesystem work are interpreted at the harness edge. |
| Test evidence is mandatory | PASS | Failing-first tests cover bounded completion, timeout exclusion, partial/canceled exclusion, scenario coverage, metadata completeness, unsupported-host zero acceptance, readiness gating, and full-validation separation. |
| Observability and safe failure | PASS | Every non-accepted iteration records one primary exclusion reason, host/profile facts, duration, bound, and artifact paths; unsupported hosts fail closed. |
| Tier 1 obligations | PASS | `.fsi`, surface baselines, compatibility notes, package validation, readiness evidence, and regression validation are required for any public or consumer-visible delta. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/160-performance-validation-throughput/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- focused-performance-lane-command.md
|   |-- full-validation-release-gate.md
|   |-- readiness-package.md
|   |-- throughput-evidence.md
|   `-- throughput-workflow-effects.md
`-- readiness/
    |-- throughput/
    |   |-- iterations/
    |   |-- raw/
    |   |-- excluded/
    |   |-- unsupported/
    |   `-- summary.md
    |-- full-validation/
    |-- fsi/
    |-- compatibility-ledger.md
    |-- package-validation.md
    |-- regression-validation.md
    `-- validation-summary.md
```

### Source Code (repository root)

```text
src/
`-- Testing/
    |-- Testing.fsi
    `-- Testing.fs

tests/
|-- Rendering.Harness/
|   |-- Compositor.fsi
|   |-- Compositor.fs
|   `-- Cli.fs
|-- Rendering.Harness.Tests/
|   |-- Feature160FocusedLaneTests.fs
|   |-- Feature160ReadinessPackageTests.fs
|   `-- Feature160ReleaseGateSeparationTests.fs
|-- Testing.Tests/
|   `-- Feature160ThroughputReadinessTests.fs
`-- Package.Tests/
    `-- Feature160CompatibilityTests.fs
```

**Structure Decision**: `Rendering.Harness` owns the focused performance lane command, timeout
policy, scenario orchestration, unsupported-host output, and readiness rendering because it already
owns compositor evidence commands for Features 156-159. `Testing` owns package-facing readiness
assertions only if generated products or package validation need stable helper types. Package and
Testing tests cover public surface/compatibility drift, while broad `dotnet test
FS.GG.Rendering.slnx --no-restore` remains a separate release-gate record rather than part of each
focused iteration.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- The focused lane uses `compositor-performance --feature 160 --lane focused` with policy
  `focused-throughput-v1`.
- Accepted iterations preserve the Feature 158 scenario set, warmup `3`, measured repetitions `5`,
  and readback-free timing discipline.
- The declared per-iteration bound is 10 minutes; unsupported-host validation is bounded to
  2 minutes and produces zero accepted artifacts.
- Accepted throughput requires at least three fresh same-profile focused iterations on
  `probe-08a47c01`.
- Full solution validation is recorded separately under `readiness/full-validation/` and remains
  required before release-ready status can be claimed.
- Feature 160 can accept throughput but leaves the shipped compositor performance claim as
  `performance-not-accepted` unless all report-defined gates are satisfied.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Observable contracts:

- [Focused Performance Lane Command](contracts/focused-performance-lane-command.md)
- [Throughput Evidence](contracts/throughput-evidence.md)
- [Full Validation Release Gate](contracts/full-validation-release-gate.md)
- [Throughput Workflow Effects](contracts/throughput-workflow-effects.md)
- [Readiness Package](contracts/readiness-package.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Scope and Tier 1 classification remain unchanged after design. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts identify focused-lane command fields, throughput evidence, release-gate separation, workflow effects, and readiness output before implementation; tasks must put FSI and semantic tests before package-visible implementation. |
| Visibility lives in `.fsi` | PASS | Package-visible status tokens or readiness helpers are declared in signature files; internal timers, process orchestration, and filesystem helpers remain implementation-only. |
| Idiomatic simplicity | PASS | Design reuses existing compositor timing/readiness commands and markdown evidence patterns. |
| Elmish/MVU boundary | PASS | `throughput-workflow-effects.md` defines model, messages, effects, and edge interpreter responsibilities for stateful validation collection. |
| Test evidence | PASS | Quickstart and contracts require focused tests, real same-profile attempts where available, unsupported-host regression, package validation, compatibility evidence, and broad full-validation evidence. |
| Observability and safe failure | PASS | Iteration status, primary exclusion reason, duration, bound, coverage, sample count, host facts, artifact paths, full-validation status, and final claim status are required output fields. |
| Tier 1 obligations | PASS | Compatibility, package validation, public-surface drift checks, and readiness closeout are required artifacts for any public or consumer-visible delta. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.

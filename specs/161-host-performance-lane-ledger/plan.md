# Implementation Plan: Host Performance Lane Ledger

**Branch**: `161-host-performance-lane-ledger` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/161-host-performance-lane-ledger/spec.md`

## Summary

Implement Feature 161 by adding host-lane fact capture, validation, and readiness evidence around
the existing P7 compositor performance path. The feature records display, renderer, direct
rendering, refresh, driver, package, load, environment, profile, policy, scenario, and run facts
for every timing run considered for performance acceptance. A complete ledger can scope a future
performance claim to a named lane, but it never generalizes the current X11 direct OpenGL
AMD/Mesa lane to Wayland, indirect GL, missing-display, software-raster, virtualized, or unknown
lanes. If timing is still noisy or any prior P7 gate remains incomplete, readiness keeps the
shipped claim `performance-not-accepted` while preserving the lane facts for review.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; repository `LangVersion=latest`; public surface
curated through `.fsi` files.

**Primary Dependencies**: Existing `FS.GG.UI.Controls`, `FS.GG.UI.SkiaViewer`,
`FS.GG.UI.Testing`, and `Rendering.Harness` projects; SkiaSharp `4.147.0-preview.3.1`;
Silk.NET OpenGL/Windowing `2.23.0`; Expecto `10.2.2`; prior Feature 155 proof,
Feature 157 damage-scissored readiness, Feature 158 readback-free measurement policy,
Feature 159 reuse/promotion readiness, and Feature 160 focused throughput readiness. No new
runtime dependency is planned.

**Storage**: Durable Feature 161 evidence under
`specs/161-host-performance-lane-ledger/readiness/`, including `lane-ledger/entries/`,
`lane-ledger/excluded/`, `lane-ledger/unsupported/`, `lane-ledger/host-facts/`,
`lane-ledger/summary.md`, `full-validation/`, `fsi/`, `compatibility-ledger.md`,
`package-validation.md`, `regression-validation.md`, and `validation-summary.md`.
Transient harness output may be written to a caller-provided `--out` directory before accepted
records are copied into readiness.

**Testing**: Expecto through `dotnet test`; Rendering.Harness tests for Feature 161 command
routing, lane-fact completeness, missing/contradictory fact rejection, cross-lane rejection,
unsupported-host zero acceptance, readiness rendering, and prior-gate linkage; Testing helper
tests if a package-visible readiness helper is added; Package tests for compatibility and surface
drift; focused preservation checks for Features 155, 157, 158, 159, and 160; broad solution
validation remains the final release gate.

**Target Platform**: Multi-package F# UI/rendering library on .NET `net10.0`; SkiaSharp over
OpenGL for the live viewer host. The currently scoped lane is the report-defined host profile
`probe-08a47c01`: X11 `:1`, direct OpenGL, AMD Radeon/Mesa. Other lanes must remain unaccepted
unless separately measured and scoped.

**Project Type**: Multi-package F# rendering/UI library plus desktop viewer host and validation
harness.

**Performance Goals**: The feature does not try to improve frame time. It makes performance
evidence auditable by requiring 100% of timing runs considered by Feature 161 readiness to have a
complete host fact set or a reviewer-visible exclusion reason; 0 cross-lane artifacts may be
combined into one accepted result; unsupported-host validation records 0 accepted lane-scoped
performance artifacts; a reviewer can interpret the lane scope and non-generalized lanes from one
summary in under 5 minutes.

**Constraints**: Host-lane scoping never replaces correctness proof, readback-free timing,
Feature 159 reuse/promotion evidence, Feature 160 throughput, full solution validation, or safe
full-redraw fallback. Missing, ambiguous, contradictory, stale, unreadable, cross-run, cross-lane,
unsupported, indirect-rendering, software-raster, unknown-renderer, missing-display, stale-package,
or noisy evidence cannot accept a shipped performance claim. The shipped claim remains
`performance-not-accepted` unless same-profile timing is not noisy, Feature 159 reuse and promotion
counters are net-positive, Feature 160 throughput is accepted, and Feature 161 lane facts are
complete for the claimed lane.

**Scale/Scope**: Narrow P7 evidence/readiness slice across `tests/Rendering.Harness`,
`tests/Rendering.Harness.Tests`, `src/Testing` and `tests/Testing.Tests` only if package-visible
helpers are needed, `tests/Package.Tests`, readiness docs, and surface baselines if public `.fsi`
changes. Out of scope: changing compositor rendering behavior, broadening host support, changing
proof/readback separation, changing Feature 159 promotion behavior, changing Feature 160
throughput policy, P8 layout, text shaping, overlay behavior, and accepting noisy timing.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies the work as Tier 1 because performance readiness semantics and possible package-facing diagnostics change. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Any public lane status, helper type, exclusion reason, readiness result, command output, or artifact contract is specified here before implementation and must be exercised through semantic/FSI tests when package-visible. |
| Visibility lives in `.fsi` | PASS | Package-visible `Testing` or harness-facing additions must be declared in signature files; implementation-only probe, process, environment, and filesystem plumbing stays omitted. |
| Idiomatic simplicity | PASS | The plan extends existing Feature 158-160 timing/readiness patterns rather than adding a new validation framework or dependency. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Lane ledger collection is modeled as pure workflow state, messages, and effects; process, timer, GL, display, package, load, and filesystem work are interpreted at the harness edge. |
| Test evidence is mandatory | PASS | Failing-first tests cover complete lane facts, missing fact rejection, contradictory fact rejection, cross-lane rejection, unsupported-host zero acceptance, noisy claim boundary, readiness rendering, and prior-gate preservation. |
| Observability and safe failure | PASS | Every non-accepted ledger entry records one primary exclusion reason, lane facts when available, prior-gate status, diagnostics, and artifact paths; unsupported hosts fail closed. |
| Tier 1 obligations | PASS | `.fsi`, surface baselines, compatibility notes, package validation, readiness evidence, and regression validation are required for any public or consumer-visible delta. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/161-host-performance-lane-ledger/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- host-lane-command.md
|   |-- lane-ledger-evidence.md
|   |-- lane-claim-scope.md
|   |-- lane-workflow-effects.md
|   `-- readiness-package.md
`-- readiness/
    |-- lane-ledger/
    |   |-- summary.md
    |   |-- entries/
    |   |-- host-facts/
    |   |-- excluded/
    |   `-- unsupported/
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
|   |-- Perf.fsi
|   |-- Perf.fs
|   `-- Cli.fs
|-- Rendering.Harness.Tests/
|   |-- Feature161HostLaneFactTests.fs
|   |-- Feature161LaneLedgerTests.fs
|   `-- Feature161ReadinessPackageTests.fs
|-- Testing.Tests/
|   `-- Feature161HostLaneReadinessTests.fs
`-- Package.Tests/
    `-- Feature161CompatibilityTests.fs
```

**Structure Decision**: `Rendering.Harness` owns lane-fact collection, lane ledger command routing,
unsupported-host output, and readiness rendering because it already owns compositor evidence
commands for Features 156-160. `Testing` owns package-facing readiness assertions only if generated
products or package validation need stable helper types. Package and Testing tests cover public
surface/compatibility drift, while broad `dotnet test FS.GG.Rendering.slnx --no-restore` remains a
separate release-gate record.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Feature 161 extends the existing compositor performance/readiness command family instead of
  introducing a new validation framework.
- The accepted host lane is verified from collected facts, not assumed from prior labels.
- Required lane facts include display server, display identity, renderer identity, direct rendering
  status, refresh rate or reason unavailable, driver identity, package version set, CPU/GPU load
  notes, known environment limits, host profile, run identity, scenario identity, timing policy
  identity, collection time, and artifact locations.
- Cross-lane artifacts are never aggregated into one accepted performance result.
- Noisy timing with complete lane facts remains auditable but keeps `performance-not-accepted`.
- Unsupported, missing-display, indirect-rendering, software-raster, and unknown-renderer lanes
  record zero accepted lane-scoped performance artifacts.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Observable contracts:

- [Host Lane Command](contracts/host-lane-command.md)
- [Lane Ledger Evidence](contracts/lane-ledger-evidence.md)
- [Lane Claim Scope](contracts/lane-claim-scope.md)
- [Lane Workflow Effects](contracts/lane-workflow-effects.md)
- [Readiness Package](contracts/readiness-package.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Scope and Tier 1 classification remain unchanged after design. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts identify lane command fields, ledger evidence, claim-scope output, workflow effects, and readiness output before implementation; tasks must put FSI and semantic tests before package-visible implementation. |
| Visibility lives in `.fsi` | PASS | Package-visible status tokens or readiness helpers are declared in signature files; internal environment probing, load sampling, process orchestration, and filesystem helpers remain implementation-only. |
| Idiomatic simplicity | PASS | Design reuses existing compositor timing/readiness commands and markdown evidence patterns. |
| Elmish/MVU boundary | PASS | `lane-workflow-effects.md` defines model, messages, effects, and edge interpreter responsibilities for stateful validation collection. |
| Test evidence | PASS | Quickstart and contracts require focused tests, real same-profile evidence where available, unsupported-host regression, package validation, compatibility evidence, and broad full-validation evidence. |
| Observability and safe failure | PASS | Lane status, primary exclusion reason, host fact completeness, prior-gate status, load notes, environment limits, artifact paths, and final claim status are required output fields. |
| Tier 1 obligations | PASS | Compatibility, package validation, public-surface drift checks, and readiness closeout are required artifacts for any public or consumer-visible delta. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.

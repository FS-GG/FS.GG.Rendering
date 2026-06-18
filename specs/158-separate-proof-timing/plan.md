# Implementation Plan: Separate Proof Readback From Timing

**Branch**: `158-separate-proof-timing` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/158-separate-proof-timing/spec.md`

## Summary

Add a Feature 158 measurement-separation layer that keeps screenshot/readback proof available while
removing forced validation readback from accepted performance timing samples. The implementation
extends the Feature 156 timing lane and Feature 157 readiness package with explicit measurement
policy metadata, proof/probe classification, contaminated-sample exclusion, same-profile
comparability checks, and a reviewer-facing summary that can accept measurement separation without
claiming a shipped compositor speedup.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; repository `LangVersion=latest`; public surface
curated through `.fsi` files.

**Primary Dependencies**: Existing `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Scene`,
`FS.GG.UI.Controls`, `FS.GG.UI.Testing`, and `Rendering.Harness` projects; SkiaSharp
`4.147.0-preview.3.1`; Silk.NET OpenGL/Windowing `2.23.0`; Expecto `10.2.2`; existing Feature
155 proof, Feature 156 timing, and Feature 157 damage readiness helpers. No new runtime dependency
is planned.

**Storage**: Durable Feature 158 evidence under `specs/158-separate-proof-timing/readiness/`,
including `timing/summary.md`, `timing/scenarios/`, `timing/raw/`, `timing/excluded/`,
`timing/unsupported/`, `proof-probes/`, `fsi/`, `compatibility-ledger.md`,
`package-validation.md`, `regression-validation.md`, and `validation-summary.md`. Transient timing
and probe artifacts may be written under a caller-provided `--out` directory before accepted
results are copied into readiness.

**Testing**: Expecto through `dotnet test`; `Rendering.Harness` tests for Feature 158 command
routing, measurement-policy evaluation, readback contamination rejection, explicit probe exclusion,
same-profile matching, unsupported-host output, and readiness rendering; `Perf` tests for sample
policy classification and distribution preservation; `Testing`/Package/FSI coverage for any public
readiness helper or token; focused regression tests for Feature 155, Feature 156, and Feature 157
boundaries.

**Target Platform**: Multi-package F# UI/rendering library on .NET `net10.0`; SkiaSharp over
OpenGL for the live viewer host. Accepted timing remains scoped to Feature 155 stable host profile
`probe-08a47c01`; unsupported or unavailable presentation environments remain fail-closed with
zero accepted proof or performance artifacts.

**Project Type**: Multi-package F# rendering/UI library plus desktop viewer host and validation
harness.

**Performance Goals**: 100% of accepted timing samples declare `readback-free` or
`readback-outside-measurement` policy; 0 proof/probe/readback-contaminated samples are counted in
the accepted performance timing set; every timing artifact records measurement policy, host
profile, scenario id, inclusion status, exclusion reason when applicable, and artifact path. The
representative scenario set remains the Feature 156 five-scenario lane unless a difference is
explicitly documented. Unsupported-host validation completes under 2 minutes with zero accepted
performance artifacts.

**Constraints**: Proof readback remains available for correctness and explicit probe runs, but it
must not occur inside an accepted measured timing interval. Missing, ambiguous, contradictory, or
unverifiable measurement-policy metadata excludes the sample. Timing, proof, and probe evidence
from different host profiles, display environments, renderer identities, package versions, run
identities, or scenario definitions cannot be combined. Feature 158 may accept measurement
separation, but the shipped compositor performance claim remains `performance-not-accepted` until
later report-defined Feature 159 and Feature 161 gates are also satisfied.

**Scale/Scope**: Narrow P7 performance-evidence slice across `tests/Rendering.Harness`,
`tests/Rendering.Harness/Perf.*`, `src/Testing` only if package-facing helpers are needed,
readiness docs, focused tests, and surface baselines if public `.fsi` changes. Out of scope:
layer promotion, content/transform key splitting, performance validation throughput, a full host
performance lane ledger, changing correctness proof requirements, broadening accepted host
support, P8 layout, text shaping, overlay behavior, and universal compositor performance
acceptance.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies the work as Tier 1 because measurement policy, readiness status, diagnostics, and package-facing validation are consumer-visible. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Any public measurement policy, probe classification, exclusion token, readiness helper, or command output is designed in contracts and `.fsi` before implementation and covered by semantic/FSI tests. |
| Visibility lives in `.fsi` | PASS | Public `Testing`, `SkiaViewer`, or harness symbols stay declared in `.fsi`; implementation-only timing-window and readback-detection helpers remain omitted. |
| Idiomatic simplicity | PASS | The plan extends existing Feature 156 timing and Feature 157 readiness structures instead of introducing a new benchmark framework or dependency. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Timing/probe collection is modeled as pure workflow state, messages, and effects; host rendering, readback, timing, and filesystem writes are interpreted at the edge. |
| Test evidence is mandatory | PASS | Failing-first tests cover accepted readback-free samples, explicit probe exclusion, contaminated/missing-policy rejection, same-profile enforcement, unsupported-host regression, and readiness publication. Synthetic fixtures are rejection-only. |
| Observability and safe failure | PASS | Every excluded or limited sample records a primary reason; unsupported hosts and failed proof readback record zero accepted proof or performance artifacts. |
| Tier 1 obligations | PASS | Public/observable deltas require `.fsi` updates, surface baselines when needed, package validation, compatibility notes, readiness evidence, and regression validation. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/158-separate-proof-timing/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- compositor-performance-readback-free-command.md
|   |-- measurement-workflow-effects.md
|   |-- proof-probe-readback-contract.md
|   |-- readback-free-timing-evidence.md
|   `-- readiness-package.md
`-- readiness/
    |-- timing/
    |   |-- scenarios/
    |   |-- raw/
    |   |-- excluded/
    |   |-- unsupported/
    |   `-- summary.md
    |-- proof-probes/
    |-- fsi/
    |-- compatibility-ledger.md
    |-- package-validation.md
    |-- regression-validation.md
    `-- validation-summary.md
```

### Source Code (repository root)

```text
src/
|-- Testing/
|   |-- Testing.fsi
|   `-- Testing.fs
`-- SkiaViewer/
    |-- SkiaViewer.fsi
    `-- SkiaViewer.fs

tests/
|-- Rendering.Harness/
|   |-- Compositor.fsi
|   |-- Compositor.fs
|   |-- Cli.fs
|   |-- Perf.fsi
|   `-- Perf.fs
|-- Rendering.Harness.Tests/
|   |-- Feature158MeasurementPolicyTests.fs
|   `-- Feature158ReadinessPackageTests.fs
|-- Testing.Tests/
|   `-- Feature158TimingSeparationHelperTests.fs
|-- Package.Tests/
|   `-- Feature158CompatibilityTests.fs
`-- SkiaViewer.Tests/
    `-- Feature158TimingProbeTests.fs
```

**Structure Decision**: `Rendering.Harness` owns Feature 158 command parsing, profile binding,
scenario orchestration, accepted/excluded timing publication, probe artifact links, unsupported-host
results, and readiness assembly. `Perf` owns reusable measurement-policy types, timing-window
classification, distribution calculation, and sample inclusion decisions. `Testing` owns
package-facing readiness assertions only if generated products or package validation need stable
helpers; otherwise package validation records that no new helper surface is required. `SkiaViewer`
is touched only if the implementation must expose a package-visible readback-free timing path or
probe option; otherwise compatibility evidence records no viewer public-surface drift.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Feature 158 uses `compositor-performance --feature 158` as the readback-free timing lane and
  keeps Feature 156 evidence as the previous noisy baseline.
- The measurement policy id is `readback-free-timing-v1`; accepted timing samples must be
  `readback-free` or `readback-outside-measurement`.
- Explicit readback probes are allowed through `compositor-performance --feature 158
  --probe-readback` and are always excluded from performance acceptance. No separate live-proof
  command is planned for this feature.
- The required scenario set remains Feature 156's five representative scenarios unless a scenario
  difference is disclosed in readiness.
- Exclusion reasons are first-class reviewer-visible tokens rather than free-form notes.
- Measurement separation can be accepted independently, but the shipped compositor performance
  claim remains `performance-not-accepted`.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Observable contracts:

- [Compositor Performance Readback-Free Command Contract](contracts/compositor-performance-readback-free-command.md)
- [Readback-Free Timing Evidence Contract](contracts/readback-free-timing-evidence.md)
- [Proof/Probe Readback Contract](contracts/proof-probe-readback-contract.md)
- [Measurement Workflow Effects Contract](contracts/measurement-workflow-effects.md)
- [Readiness Package Contract](contracts/readiness-package.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Scope and Tier 1 classification remain unchanged after design. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts identify public/observable policy, probe, exclusion, and readiness surfaces before implementation; tasks must put FSI and semantic tests before package-visible implementation. |
| Visibility lives in `.fsi` | PASS | Public measurement-policy and readiness helpers are declared through signature files; private timing-window detectors stay implementation-only. |
| Idiomatic simplicity | PASS | Design reuses Feature 156 scenario/distribution records, Feature 157 readiness shape, and existing harness command conventions. |
| Elmish/MVU boundary | PASS | `measurement-workflow-effects.md` defines model, messages, effects, and edge interpreter responsibilities for stateful timing/probe I/O. |
| Test evidence | PASS | Quickstart and contracts require focused tests, readback-free accepted evidence where available, explicit probe evidence, unsupported-host regression, package validation, and broad regression validation. |
| Observability and safe failure | PASS | Included/excluded status, primary exclusion reason, measurement policy, host facts, artifact paths, proof/probe links, and final claim status are required output fields. |
| Tier 1 obligations | PASS | Compatibility, package validation, public-surface drift checks, and readiness closeout are required artifacts for any public or consumer-visible delta. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.

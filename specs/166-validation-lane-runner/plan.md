# Implementation Plan: Validation Lane Runner

**Branch**: `166-validation-lane-runner` | **Date**: 2026-06-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/166-validation-lane-runner/spec.md`

## Summary

Harden the existing `Rendering.Harness.ValidationLanes` workflow and
`scripts/run-validation-lanes.fsx` entry point into the repository's documented
validation lane runner. The runner will expose stable named lanes for build
verification, library/package validation, Controls validation, rendering/harness
validation, package-consuming sample validation, and optional aggregate solution
validation. It will add request preflight, readiness roles, no-overwrite run
evidence, progress heartbeats, bounded timeout/no-progress handling,
operator cancellation, unsafe-concurrency prevention, and reviewer-readable plus
structured summaries. This is tooling only and does not change public framework
runtime behavior.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; repository `LangVersion=latest`.
Harness-visible contracts are declared in `.fsi` files before `.fs` bodies.

**Primary Dependencies**: Existing .NET SDK, NuGet, Expecto `10.2.2`, `System.Diagnostics`,
`System.Text.Json`, and repository projects under `tests/Rendering.Harness`,
`tests/Rendering.Harness.Tests`, package tests, Controls tests, and samples. No new
runtime or tooling dependency is planned.

**Storage**: Filesystem evidence only. Each validation session writes a distinct
run directory containing per-lane logs, per-lane structured results, diagnostics,
captured test artifacts, `summary.md`, and `summary.json`. Default transient
output is under `artifacts/validation-lanes/<run-id>/`; readiness runs may pass
`--out specs/166-validation-lane-runner/readiness/lanes` and still receive a
run-id child directory. No database or persistent runtime storage.

**Testing**: Expecto through `dotnet test`. New focused tests in
`tests/Rendering.Harness.Tests` cover Feature 166 lane catalog, request preflight,
status classification, timeout/no-progress/cancellation behavior, evidence
directory failures, summary/readiness rules, run-id no-overwrite behavior, and
unsafe-concurrency scheduling. Existing direct validation commands remain runnable
outside the lane runner.

**Target Platform**: Maintainer CLI workflow for the cross-platform F#/.NET
rendering repository. The runner uses .NET process APIs for child processes,
timeouts, cancellation, and logging; it does not rely on shell-specific `timeout`
wrappers for core behavior.

**Project Type**: Multi-package F# rendering/UI library plus repository validation
harness, maintainer scripts, and package-consuming sample applications.

**Performance Goals**: Unknown lane or configuration errors are reported before
any validation work starts. A final summary is written within 10 seconds after
the last lane completes. Timed-out or no-progress lanes are stopped within their
configured budget plus 30 seconds. The active lane and last visible activity are
reported at least every 60 seconds during long runs.

**Constraints**: Public framework behavior and package runtime behavior must not
change. The aggregate solution lane is optional and cannot hide required-lane
failures. Required lane skips, timeouts, cancellations, infrastructure errors, and
unaccepted environment limitations block readiness. Existing direct commands such
as `dotnet test tests/Controls.Tests/Controls.Tests.fsproj` remain documented and
usable. No run may overwrite previous evidence without an explicit replacement
notice. Lanes sharing generated output locations are serialized, isolated, or
rejected before concurrent execution starts.

**Scale/Scope**: Narrow repository validation slice across
`tests/Rendering.Harness/ValidationLanes.fsi`, `tests/Rendering.Harness/ValidationLanes.fs`,
`tests/Rendering.Harness/Cli.fs`, `scripts/run-validation-lanes.fsx`,
`tests/Rendering.Harness.Tests`, and validation docs. Out of scope: fixing slow
tests, changing CI providers, replacing package-feed validation, changing sample
runtime behavior, adding a new validation framework dependency, or making the
optional full-solution aggregate a required readiness gate.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies this as a Tier 1 contracted validation tooling change with no public `FS.GG.UI.*` runtime package behavior change. The plan treats the harness CLI and `.fsi` surface as a documented repository contract. |
| Spec -> FSI -> semantic tests -> implementation | PASS | The lane runner model, statuses, readiness roles, summaries, and effects are specified here and in contracts before implementation; tasks must update `ValidationLanes.fsi`, exercise the draft surface through F# Interactive or a prelude transcript, and write failing semantic tests before `.fs` bodies. |
| Visibility lives in `.fsi` | PASS | Harness-visible types/functions remain declared in `ValidationLanes.fsi`. Implementation-only process, filesystem, timer, and JSON helpers stay out of the signature. |
| Idiomatic simplicity | PASS | The approach uses existing records, discriminated unions, process execution, filesystem writes, and JSON/Markdown rendering. No custom operators, SRTP, reflection, type providers, or new framework abstractions are planned. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Existing `Model`/`Msg`/`Effect` lane workflow is retained and extended. Pure update logic decides schedules, status transitions, and summary state; the edge interpreter owns process, timer, console, and filesystem effects. |
| Test evidence is mandatory | PASS | Focused tests cover pass, fail, timeout, no-progress, cancellation, unknown lane, duplicate IDs, unsafe concurrency, output failures, summary timing, summary agreement, and optional aggregate separation. Synthetic process fixtures must carry `Synthetic` in test names and comments, include a rationale and real-evidence path, and be listed in PR-description-ready readiness notes. |
| Observability and safe failure | PASS | Every lane result records lane id, readiness role, command, status, timeout budget, elapsed time, last activity, log path, result artifacts, diagnostics, and non-passing reason. Missing evidence or infrastructure errors fail closed. |
| Tier 1 tooling boundaries | PASS | The contracted surface is limited to maintainer validation tooling: `Rendering.Harness.ValidationLanes` and the `validation-lanes` CLI. No public `FS.GG.UI.*` runtime package behavior changes are planned. Tasks include harness contract baseline or equivalent surface-drift evidence. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/166-validation-lane-runner/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- lane-definition-and-schedule.md
|   |-- lane-runner-effects.md
|   |-- validation-lane-cli.md
|   `-- validation-session-record.md
`-- readiness/
    |-- fsi-contract-transcript.md
    |-- synthetic-evidence.md
    |-- tier1-tooling-boundary.md
    |-- feature166-tests.md
    `-- lanes/
        `-- <run-id>/
            |-- summary.md
            |-- summary.json
            `-- <lane-id>/
                |-- log.txt
                |-- result.json
                `-- diagnostics.md
```

### Source Code (repository root)

```text
scripts/
`-- run-validation-lanes.fsx

tests/
|-- Rendering.Harness/
|   |-- ValidationLanes.fsi
|   |-- ValidationLanes.fs
|   `-- Cli.fs
`-- Rendering.Harness.Tests/
    |-- Feature166TestFixtures.fs
    |-- Feature166LaneCatalogTests.fs
    |-- Feature166LaneRunnerPreflightTests.fs
    |-- Feature166LaneStatusTests.fs
    |-- Feature166ValidationSummaryTests.fs
    |-- Feature166CancellationTests.fs
    `-- Feature166SchedulingTests.fs

docs/
`-- validation/
    `-- validation-set.md
```

**Structure Decision**: Keep ownership in `Rendering.Harness` because it already
owns package-feed validation, lane result records, readiness summaries, and the
CLI subcommand used by `scripts/run-validation-lanes.fsx`. The script stays a thin
maintainer-facing wrapper. `docs/validation/validation-set.md` is updated only to
document the lane runner as orchestration; direct validation commands remain
available.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- The feature extends the existing `ValidationLanes` module instead of adding a
  separate runner.
- The default required lane set is build, library/package, package proof,
  Controls, rendering/harness, and package-consuming sample validation.
- The full solution command is an optional aggregate lane and is reported
  separately from required readiness.
- The CLI defaults to the required lane set, supports `--lane` selection and
  `--list`, and rejects unknown or duplicate lane ids before starting work.
- Evidence is organized by run id so re-runs do not silently overwrite prior
  evidence.
- The runner remains sequential by default; concurrency metadata prevents shared
  output races if parallel execution is later enabled or requested.
- No new dependency is required.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state
transitions.

Observable contracts:

- [Validation Lane CLI](contracts/validation-lane-cli.md)
- [Lane Definition and Schedule](contracts/lane-definition-and-schedule.md)
- [Validation Session Record](contracts/validation-session-record.md)
- [Lane Runner Effects](contracts/lane-runner-effects.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Design artifacts preserve the Tier 1 contracted validation tooling boundary and explicitly forbid public `FS.GG.UI.*` runtime package changes. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts define the intended `ValidationLanes.fsi` surface, CLI behavior, summary schemas, and MVU/effect transitions before implementation; tasks add an FSI/prelude transcript before `.fs` implementation. |
| Visibility lives in `.fsi` | PASS | Data model and contracts identify harness-visible records/unions/functions that belong in `ValidationLanes.fsi`; process/filesystem interpreters remain implementation-only. |
| Idiomatic simplicity | PASS | Design uses plain F# data types, deterministic validation functions, process execution, and file artifacts. |
| Elmish/MVU boundary | PASS | `lane-runner-effects.md` maps state transitions and edge effects; cancellation, timeout, progress, and summary writing are effect-driven. |
| Test evidence | PASS | `quickstart.md` lists focused commands and expected outcomes for pass, fail, timeout, no-progress timeout, cancellation, unknown lane, unsafe concurrency, summary timing, and summary agreement; tasks also capture synthetic-evidence disclosure. |
| Observability and safe failure | PASS | `validation-session-record.md` requires last activity, elapsed time, evidence paths, diagnostics, and fail-closed readiness rules in both Markdown and JSON. |
| Tier 1 tooling boundaries | PASS | Documentation keeps the runner as orchestration over existing validation commands, requires direct validation workflows to remain runnable, and keeps public `FS.GG.UI.*` runtime packages unchanged. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.

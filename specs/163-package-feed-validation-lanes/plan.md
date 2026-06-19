# Implementation Plan: Package Feed Validation Lanes

**Branch**: `163-package-feed-validation-lanes` | **Date**: 2026-06-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/163-package-feed-validation-lanes/spec.md`

## Summary

Implement Feature 163 by adding repository validation tooling that discovers current packable
`FS.GG.UI.*` package versions, refreshes the local package feed, checks and optionally updates
selected package-only sample pins, proves deterministic package source selection with an isolated
NuGet cache, and runs named validation lanes with separate logs, result artifacts, timeout and
no-progress handling, and an honest readiness summary. AntShowcase is the first selected sample. The
feature changes repository validation contracts and sample restore behavior; no public UI framework
API change is planned.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; repository `LangVersion=latest`; public or harness-
visible surfaces are curated through `.fsi` files.

**Primary Dependencies**: Existing .NET SDK, NuGet restore/pack behavior, Expecto `10.2.2`, and
repo projects under `tests/Rendering.Harness`, `tests/Rendering.Harness.Tests`,
`tests/Package.Tests`, and package-consuming samples. No new runtime dependency is planned.

**Storage**: Durable Feature 163 evidence under
`specs/163-package-feed-validation-lanes/readiness/`, including package pin/source proof,
per-lane logs and results, lane diagnostics, summary JSON/Markdown, compatibility notes, package
validation, and regression validation. Transient isolated NuGet caches and per-lane build outputs
are caller-provided or written under ignored artifact directories unless explicitly copied into
readiness evidence.

**Testing**: Expecto through `dotnet test`; Rendering.Harness tests for package discovery, package
pin checks, refresh behavior, source-proof classification, lane status classification, timeout/no-
progress behavior, output isolation, and summary gating; Package.Tests for source-controlled
evidence and sample package drift; sample validation commands for AntShowcase. Full solution
validation remains a named lane and is never silently treated as green when skipped, timed out, or
canceled.

**Target Platform**: Multi-package F# UI/rendering repository on .NET `net10.0`; package-consuming
samples restore from the configured local feed `~/.local/share/nuget-local/` plus approved external
sources for third-party packages.

**Project Type**: Multi-package F# rendering/UI library plus repository validation harness,
maintainer scripts, and package-consuming sample applications.

**Performance Goals**: Package-pin checks complete before sample build/test execution begins.
Default validation lanes isolate outputs so concurrently eligible lanes never share result or build
output directories. Timeout or no-progress detection writes one summary update and never counts the
lane as passed. A reviewer can identify package version, selected samples, local feed, cache, lane
statuses, and incomplete evidence from one summary in under 2 minutes.

**Constraints**: Default package proof must not clear global NuGet caches; destructive cache
clearing is opt-in cold-proof mode and must be recorded. `FS.GG.UI.*` packages must resolve only
from the local feed for selected samples, while approved third-party sources remain available.
Selected sample compatibility exceptions are explicit evidence, not silent skips. Aggregate
solution validation remains distinguishable from focused lanes.

**Scale/Scope**: Narrow repository validation slice across `tests/Rendering.Harness`,
`tests/Rendering.Harness.Tests`, `tests/Package.Tests`, thin scripts under `scripts/`, AntShowcase
sample pins/configuration as needed, and Feature 163 readiness docs. Out of scope: external package
publishing, changing versioning policy, fixing existing long-running tests, replacing all CI, and
visual/readiness diagnostics beyond making them runnable lanes.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies the work as Tier 1 because repository validation contracts and sample restore behavior change. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Harness-visible package-feed, lane, result, status, effect, and summary contracts are specified here before implementation and must be declared in `.fsi` files before `.fs` bodies. |
| Visibility lives in `.fsi` | PASS | New reusable harness modules such as package-feed validation and lane running are planned with `.fsi` signatures; scripts are thin entry points. |
| Idiomatic simplicity | PASS | The plan uses existing F#/.NET/NuGet primitives, XML parsing, process execution, and repository harness tests instead of introducing a validation framework dependency. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Package refresh/proof and lane execution are modeled as pure model/message/effect transitions, with NuGet, filesystem, process, timers, and diagnostics interpreted at the edge. |
| Test evidence is mandatory | PASS | Failing-first tests cover stale pins, refresh, missing feed packages, source violations, isolated cache proof, third-party source allowance, lane passed/failed/timed-out/hung/skipped/canceled/not-run/environment-limited statuses, output isolation, and summary gating. |
| Observability and safe failure | PASS | Evidence records package ids, versions, sample paths, feed/cache/source locations, command lines, elapsed time, lane logs, diagnostics, caveats, and non-green statuses; unknown or incomplete proof fails closed. |
| Tier 1 obligations | PASS | `.fsi`, semantic tests, compatibility notes, package validation, source-controlled readiness evidence, and surface-drift validation are required. New harness signatures are repository-validation surfaces, not public UI package surfaces; any package-visible `.fsi` delta still requires refreshed surface baselines. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/163-package-feed-validation-lanes/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- package-feed-command.md
|   |-- package-source-proof.md
|   |-- validation-lane-runner.md
|   |-- validation-summary.md
|   `-- workflow-effects.md
`-- readiness/
    |-- package-proof/
    |-- lanes/
    |-- diagnostics/
    |-- compatibility-ledger.md
    |-- package-validation.md
    |-- regression-validation.md
    `-- validation-summary.md
```

### Source Code (repository root)

```text
scripts/
|-- refresh-local-feed-and-samples.fsx
`-- run-validation-lanes.fsx

tests/
|-- Rendering.Harness/
|   |-- PackageFeed.fsi
|   |-- PackageFeed.fs
|   |-- ValidationLanes.fsi
|   |-- ValidationLanes.fs
|   `-- Cli.fs
|-- Rendering.Harness.Tests/
|   |-- Feature163PackageFeedTests.fs
|   |-- Feature163PackageSourceProofTests.fs
|   |-- Feature163ValidationLaneTests.fs
|   `-- Feature163ValidationSummaryTests.fs
`-- Package.Tests/
    `-- Feature163PackageFeedValidationTests.fs

samples/
`-- AntShowcase/
    |-- nuget.config
    |-- AntShowcase.Core/AntShowcase.Core.fsproj
    |-- AntShowcase.App/AntShowcase.App.fsproj
    `-- AntShowcase.Tests/AntShowcase.Tests.fsproj
```

**Structure Decision**: `Rendering.Harness` owns the reusable package-feed model, source-proof
model, lane runner, status classification, and summary rendering because it is already the
repository validation harness. `scripts/` provides maintainer-friendly entry points with stable
arguments. AntShowcase is the first selected sample and remains package-only; broader sample
coverage is configurable by passing additional `--sample` arguments. `Package.Tests` owns
source-controlled drift/evidence assertions, while lane process execution and NuGet/source proof
behavior are exercised in `Rendering.Harness.Tests`.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Current package versions are discovered from packable `src/*/*.fsproj` files with
  `PackageId` matching `FS.GG.UI.*`; expected versions are package-specific, not assumed to be one
  global version.
- Package pins are checked and refreshed by parsing project XML `PackageReference` entries in
  selected samples. Stale pins fail before sample build/test execution begins.
- Source selection proof uses a generated NuGet config with package source mapping, an isolated
  `NUGET_PACKAGES` cache by default, and an explicit opt-in cold mode for destructive global cache
  clearing.
- Validation lanes use declarative lane definitions, per-lane result/log/build output roots,
  timeout and no-progress policies, and fail-closed status classification.
- The readiness summary separates focused lane success from aggregate full-solution validation and
  never reports fully ready when required evidence is failed, timed out, canceled, skipped, not run,
  hung, or environment-limited without an accepted exception.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Observable contracts:

- [Package Feed Command](contracts/package-feed-command.md)
- [Package Source Proof](contracts/package-source-proof.md)
- [Validation Lane Runner](contracts/validation-lane-runner.md)
- [Validation Summary](contracts/validation-summary.md)
- [Workflow Effects](contracts/workflow-effects.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Scope and Tier 1 classification remain unchanged after design. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts define package-feed, source-proof, lane-runner, workflow-effect, and summary behavior before implementation; tasks must put `.fsi` and failing tests before `.fs` bodies. |
| Visibility lives in `.fsi` | PASS | Harness-visible models and functions are declared in `PackageFeed.fsi` and `ValidationLanes.fsi`; implementation-only XML/process/filesystem helpers remain omitted. |
| Idiomatic simplicity | PASS | Design uses XML/project parsing, NuGet config/source mapping, process execution, and Markdown/JSON evidence with no new framework dependency. |
| Elmish/MVU boundary | PASS | `workflow-effects.md` defines model, messages, effects, and edge interpreter responsibilities for package and lane workflows. |
| Test evidence | PASS | Quickstart and contracts require stale-pin, refresh, source-proof, isolated-cache, lane status, timeout/no-progress, output isolation, mixed-summary, package validation, and regression evidence. |
| Observability and safe failure | PASS | Every package proof, lane result, timeout, no-progress event, cancellation, source violation, exception, and environment limitation has a status, reason, command, path, and diagnostic artifact. |
| Tier 1 obligations | PASS | Compatibility, package validation, surface-drift checks, and readiness closeout are required. Harness-only `.fsi` additions must record zero public UI surface delta unless a package-visible signature also changes, in which case baselines are refreshed in the same change. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.

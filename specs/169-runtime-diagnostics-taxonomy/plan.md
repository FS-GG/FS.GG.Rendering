# Implementation Plan: Runtime Diagnostics Taxonomy

**Branch**: `169-runtime-diagnostics-taxonomy` | **Date**: 2026-06-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/169-runtime-diagnostics-taxonomy/spec.md`

## Summary

Introduce a shared runtime diagnostics taxonomy so sample output, machine
artifacts, and readiness decisions classify the same runtime events consistently.
The implementation will add a dependency-light `FS.GG.UI.Diagnostics` package,
map existing SkiaViewer, Controls, and Controls.Elmish diagnostics into that
contract, aggregate repeated messages, write structured artifacts, and integrate
the summary with validation/readiness tooling. Existing human-readable messages
remain visible, but readiness decisions consume the typed taxonomy instead of
parsing prose.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; repository `LangVersion=latest`.
Public package contracts are drafted in `.fsi` before `.fs` bodies.

**Primary Dependencies**: Existing .NET SDK, Expecto `10.2.2`, `System.IO`,
`System.Text.Json`, existing `FS.GG.UI.*` packages, `Rendering.Harness`
validation tooling, sample app command infrastructure, and Spec Kit artifacts.
No new external NuGet dependency is planned.

**Storage**: Filesystem artifacts only. Diagnostic JSON and Markdown are written
under feature/sample readiness output directories, with run identifiers and
stable file names. No database, remote telemetry, or repository-wide structured
logging provider is introduced.

**Testing**: Expecto through `dotnet test`, FSI/prelude semantic checks, public
surface-baseline refresh, fixture-mode diagnostic classification tests,
artifact JSON/Markdown shape tests, readiness-evaluation tests, and sample CLI
default/verbose output tests.

**Target Platform**: Cross-platform local F#/.NET rendering and sample
workflows. Diagnostics cover headless, GL-capable, and environment-limited local
desktop runs without assuming live GPU access in CI.

**Project Type**: Multi-package F# rendering/UI library with desktop viewer
host, controls runtime, Elmish adapter, generated-product testing helpers,
validation harness, and package-consuming samples.

**Performance Goals**: Aggregating at least 100 identical diagnostics produces
one grouped summary entry with the correct occurrence count. Default console
summary for the representative mixed fixture stays within 12 lines. Diagnostic
artifact generation remains small enough for normal sample/readiness runs and
does not add measurable render-loop work.

**Constraints**: Tier 1 public-surface change. New public modules require `.fsi`
signatures and surface-baseline updates. Runtime packages must not depend on
`FS.GG.UI.Testing`; the shared taxonomy therefore lives in a dependency-light
`FS.GG.UI.Diagnostics` package. Existing diagnostic messages keep their meaning
unless reclassification is documented. Unclassified or partially classified
diagnostics fail closed to review-required status. Artifact write failures are
reported as developer-action warnings. No remote telemetry and no logging
provider selection are in scope.

**Scale/Scope**: Initial producers are `FS.GG.UI.SkiaViewer`,
`FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`, sample evidence commands, and
`Rendering.Harness.ValidationLanes`. Initial consumers are sample console
summaries, feature readiness summaries, structured diagnostic artifacts, and
tests that assert backend-cost and blocker behavior.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies this as Tier 1 diagnostic contract/readiness behavior. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts define the public diagnostics surface and adapters before implementation. Tasks must add `.fsi` signatures, failing semantic tests, and surface-baseline evidence before `.fs` bodies. |
| Visibility lives in `.fsi` | PASS | New `FS.GG.UI.Diagnostics` public surface, plus any adapter additions in SkiaViewer/Controls/Controls.Elmish/Testing, must be declared in `.fsi` files and reflected in `tests/surface-baselines`. |
| Idiomatic simplicity | PASS | Design uses plain records, discriminated unions, pure aggregation/evaluation functions, and filesystem artifact writers. No custom operators, SRTP, reflection discovery, type providers, or logging framework abstraction are planned. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Diagnostic collection/evaluation is pure; artifact and console output are edge effects. If a long-running sample/readiness workflow stores diagnostic state, it must expose model/msg/effect/update boundaries or use the existing harness MVU boundary. |
| Test evidence is mandatory | PASS | Focused tests cover classification fixtures, readiness outcomes, aggregation, artifact write failure reporting, default vs verbose output, FSI semantic use, and package surface drift. |
| Observability and safe failure | PASS | Every diagnostic record carries severity, category, source, message, occurrence count, contexts, action guidance, and readiness interpretation. Missing classification becomes review-required rather than accepted. |
| Tier 1 compatibility | PASS | Plan creates an additive shared package and additive adapter functions. Existing diagnostic constructors remain source-compatible unless tasks document a narrower exception and migration guidance. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/169-runtime-diagnostics-taxonomy/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- console-summary.md
|   |-- diagnostic-artifact.md
|   |-- readiness-evaluation.md
|   `-- runtime-diagnostics-api.md
`-- readiness/
    |-- diagnostics-fixture-summary.json
    |-- diagnostics-fixture-summary.md
    |-- surface-baselines/
    |   |-- FS.GG.UI.Diagnostics.txt
    |   |-- FS.GG.UI.Controls.txt
    |   |-- FS.GG.UI.Controls.Elmish.txt
    |   |-- FS.GG.UI.SkiaViewer.txt
    |   `-- FS.GG.UI.Testing.txt
    |-- feature169-tests.md
    |-- sample-output.md
    `-- validation-log.md
```

### Source Code (repository root)

```text
src/
|-- Diagnostics/
|   |-- Diagnostics.fsproj
|   |-- Diagnostics.fsi
|   `-- Diagnostics.fs
|-- Controls/
|   |-- Diagnostics.fsi
|   `-- Diagnostics.fs
|-- Controls.Elmish/
|   |-- ControlsElmish.fsi
|   `-- ControlsElmish.fs
|-- SkiaViewer/
|   |-- Host/Diagnostics.fsi
|   `-- Host/Diagnostics.fs
`-- Testing/
    |-- Testing.fsi
    `-- Testing.fs

tests/
|-- Diagnostics.Tests/
|   |-- Diagnostics.Tests.fsproj
|   |-- Feature169ClassificationTests.fs
|   |-- Feature169AggregationTests.fs
|   |-- Feature169ArtifactTests.fs
|   |-- Feature169ReadinessTests.fs
|   `-- Program.fs
|-- Controls.Tests/
|   `-- Feature169RuntimeDiagnosticMappingTests.fs
|-- Elmish.Tests/
|   `-- Feature169AdapterDiagnosticMappingTests.fs
|-- SkiaViewer.Tests/
|   `-- Feature169HostDiagnosticMappingTests.fs
|-- Rendering.Harness.Tests/
|   `-- Feature169ValidationDiagnosticsTests.fs
`-- surface-baselines/
    |-- FS.GG.UI.Diagnostics.txt
    |-- FS.GG.UI.Controls.txt
    |-- FS.GG.UI.Controls.Elmish.txt
    |-- FS.GG.UI.SkiaViewer.txt
    `-- FS.GG.UI.Testing.txt

samples/
|-- AntShowcase/
|   |-- AntShowcase.App/
|   `-- AntShowcase.Tests/
|-- ControlsGallery/
`-- SampleApps/

scripts/
|-- refresh-surface-baselines.fsx
`-- run-validation-lanes.fsx
```

**Structure Decision**: Add a new packable `src/Diagnostics` package for the
shared taxonomy because Controls, SkiaViewer, Controls.Elmish, Testing, the
harness, and samples all need the same types, while runtime packages must not
depend on `FS.GG.UI.Testing`. Package-specific adapter functions stay near the
existing diagnostic producers. Artifact rendering and readiness evaluation can
live in the shared package when dependency-light; Testing/Harness code can wrap
it for feature-readiness workflows. Samples consume the shared package through
their existing package-only model after the local feed is refreshed.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- A new dependency-light `FS.GG.UI.Diagnostics` package is the lowest-risk place
  for the shared taxonomy.
- Existing SkiaViewer/Controls/Controls.Elmish diagnostics remain additive and
  map into the new taxonomy through adapter functions.
- Readiness interpretation uses explicit readiness impact derived from category,
  severity, classification completeness, and accepted exceptions.
- Repeated diagnostics are aggregated by stable fingerprint while preserving
  first and last context.
- Artifacts are JSON-first with Markdown summaries for reviewers.
- Default console output is a compact grouped summary; verbose output and
  artifacts preserve individual records.
- Artifact write failures become developer-action warnings and do not erase the
  in-memory summary.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state
transitions.

Observable contracts:

- [Runtime Diagnostics API](contracts/runtime-diagnostics-api.md)
- [Diagnostic Artifact](contracts/diagnostic-artifact.md)
- [Readiness Evaluation](contracts/readiness-evaluation.md)
- [Console Summary](contracts/console-summary.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Design artifacts preserve Tier 1 scope and define public package, adapter, artifact, readiness, and console contracts. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts name intended `.fsi` surfaces, FSI semantic checks, fixtures, and tests before implementation. |
| Visibility lives in `.fsi` | PASS | The design requires `Diagnostics.fsi` plus adapter additions in existing `.fsi` files before bodies, with refreshed surface baselines. |
| Idiomatic simplicity | PASS | Records/unions and pure functions are sufficient; artifact writing uses `System.Text.Json`/filesystem APIs already used elsewhere in the repo. |
| Elmish/MVU boundary | PASS | Persistent readiness/harness flows remain behind existing MVU/effect boundaries; diagnostic aggregation itself is pure. |
| Test evidence | PASS | `quickstart.md` lists focused tests, semantic FSI checks, surface drift checks, package feed/sample validation, and validation-lane evidence. |
| Observability and safe failure | PASS | Contracts require explicit unclassified counts, blocker counts, exception records, write-failure diagnostics, and links to detailed evidence. |
| Tier 1 compatibility | PASS | Compatibility and migration rules are explicit: additive first, documented reclassification only, surface baselines mandatory. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.

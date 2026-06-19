# Implementation Plan: Retained Render Damage Inspection

**Branch**: `170-retained-damage-inspection` | **Date**: 2026-06-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/170-retained-damage-inspection/spec.md`

## Summary

Extend the structured visual inspection model with retained-render and damage-locality evidence. The feature keeps final screen inspection in `FS.GG.UI.Scene`/`FS.GG.UI.Controls`/`FS.GG.UI.Testing`, adds additive retained/damage records instead of changing existing `VisualInspectionArtifact` record fields, emits retained transition evidence from the real `RetainedRender.init`/`RetainedRender.step` path, validates dirty-region locality with union semantics, migrates one AntShowcase visual-shell assertion to structured evidence, and adds a maintained validation-lane entry point for retained inspection readiness.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; repository `LangVersion=latest`. Public package contracts are drafted in `.fsi` before `.fs` bodies.

**Primary Dependencies**: Existing repository dependencies only: `FS.GG.UI.Scene`, `FS.GG.UI.Controls`, `FS.GG.UI.Testing`, `FS.GG.UI.Diagnostics`, `Rendering.Harness`, AntShowcase sample projects, Expecto `10.2.2`, `System.Text.Json`, and pinned SkiaSharp `4.147.0-preview.3.1` where already referenced. No new external NuGet package is planned.

**Storage**: Filesystem readiness artifacts under `specs/170-retained-damage-inspection/readiness/retained-inspection/`, including retained/damage JSON artifacts, generated Markdown summaries, validation-lane output, compatibility notes, command logs, and sample-adoption evidence. No database, telemetry store, or persistent runtime cache is introduced.

**Testing**: Expecto through focused `dotnet test` commands for `Controls.Tests`, `Testing.Tests`, `Rendering.Harness.Tests`, and `samples/AntShowcase/AntShowcase.Tests`; package/surface drift checks through `Package.Tests`; canonical readiness through `dotnet fsi scripts/run-validation-lanes.fsx --lane retained-inspection --out specs/170-retained-damage-inspection/readiness/lanes`.

**Target Platform**: Cross-platform local .NET library and sample validation. Retained/damage inspection must run headless against deterministic retained-render fixtures and must not require a live GL/OpenGL window. Existing screenshot readiness remains a separate visual-readiness workflow.

**Project Type**: Multi-package F# rendering/UI library with retained controls runtime, testing helpers, validation harness, and package-consuming AntShowcase sample.

**Performance Goals**: The retained-inspection lane completes representative retained inspection, damage locality validation, and AntShowcase sample-adoption checks in under 5 minutes on a maintainer workstation, excluding package restore; lane evidence must record pass/review-required status for this threshold. Reviewer summaries expose dirty area percentage, repainted count, shifted count, and affected regions clearly enough to inspect in under 2 minutes, with reviewer-summary field coverage recorded in readiness evidence.

**Constraints**: Tier 1 testing/readiness evidence contract. Public additions require `.fsi` signatures, surface-baseline updates, and compatibility notes. Existing `VisualInspectionArtifact` public record shape remains source-compatible; retained/damage evidence uses additive records or wrappers. `Scene` remains dependency-light and must not reference Controls, Testing, SkiaViewer, SkiaSharp, Layout, Yoga, Elmish, or KeyboardInput. `Controls` must not depend on Testing. `Testing` must not depend on Controls. Unsupported, unavailable, or not-inspected damage facts must be explicit and cannot count as accepted evidence. Dirty area must use true union area, not summed overlapping rectangles.

**Scale/Scope**: Initial scope covers reusable retained/damage model types, Controls emission from retained transition fixtures, Testing validation/readiness/Markdown/JSON helpers, focused retained/damage fixtures, one AntShowcase visual-shell assertion migration, validation-lane registration, and readiness documentation. Supported inspected screens/scopes are the retained-render representative fixtures, the AntShowcase `charts-statistical` full-shell assertion at preferred size in light and dark themes, and retained-inspection validation-lane output. It does not change screenshot capture counts, contact sheets, input scheduling, render-threading, or broad retained-render performance policy.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies this as Tier 1 testing/readiness evidence contract work. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts identify intended `.fsi` surfaces and failing-first tests before implementation. Tasks must draft signatures, semantic tests, and surface baselines before bodies. |
| Visibility lives in `.fsi` | PASS | Public additions are additive signatures in `Scene.fsi`, `Inspection.fsi`, and `Testing.fsi`; implementation internals remain hidden through existing `.fsi` boundaries. |
| Idiomatic simplicity | PASS | Design uses records, discriminated unions, lists/maps, pure validation functions, and existing retained-render helpers. No custom operators, SRTP, reflection discovery, type providers, or new computation expressions are planned. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Retained/damage inspection and validation are pure request/result helpers. File writes and validation-lane execution remain edge effects in Testing/Harness/sample workflows. |
| Test evidence is mandatory | PASS | Focused tests cover retained node status, shifted/repainted separation, dirty-region union area, broad/full-surface findings, unsupported/not-inspected damage, stable ids, sample adoption, and canonical lane output. Synthetic fixtures must carry the repository's required synthetic disclosure. |
| Observability and safe failure | PASS | Evidence records readiness status, command, elapsed time, artifact paths, dirty percentage, affected regions, unsupported reasons, and validation findings. Missing damage data fails closed to unsupported/not-inspected. |
| Tier 1 compatibility | PASS | Plan preserves existing screenshot readiness and existing `VisualInspectionArtifact` shape, adds migration notes, and requires surface-baseline evidence for every public addition. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/170-retained-damage-inspection/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- retained-inspection-api.md
|   |-- retained-damage-artifacts.md
|   |-- damage-locality-validation.md
|   `-- validation-entry-point.md
`-- readiness/
    |-- retained-inspection/
    |   |-- summary.md
    |   |-- summary.json
    |   |-- artifacts/
    |   |-- findings/
    |   |   `-- blocking-findings.md
    |   |-- compatibility.md
    |   |-- validation-log.md
    |   `-- antshowcase-adoption.md
    `-- lanes/
```

### Source Code (repository root)

```text
src/Scene/
|-- Scene.fsi
`-- Scene.fs

src/Controls/
|-- Inspection.fsi
|-- Inspection.fs
|-- RetainedRender.fsi
`-- RetainedRender.fs

src/Testing/
|-- Testing.fsi
`-- Testing.fs

tests/Controls.Tests/
|-- Feature170RetainedInspectionTests.fs
|-- Feature170DamageRegionUnionTests.fs
`-- Controls.Tests.fsproj

tests/Testing.Tests/
|-- Feature170DamageLocalityValidationTests.fs
|-- Feature170RetainedInspectionArtifactTests.fs
`-- Testing.Tests.fsproj

tests/Rendering.Harness/
|-- ValidationLanes.fsi
`-- ValidationLanes.fs

tests/Rendering.Harness.Tests/
|-- Feature170RetainedInspectionLaneTests.fs
`-- Rendering.Harness.Tests.fsproj

samples/AntShowcase/
|-- AntShowcase.Core/
|   |-- Evidence.fsi
|   `-- Evidence.fs
`-- AntShowcase.Tests/
    |-- VisualShellTests.fs
    |-- Feature170VisualInspectionAdoptionTests.fs
    `-- AntShowcase.Tests.fsproj

docs/validation/
`-- validation-set.md

tests/surface-baselines/
|-- FS.GG.UI.Scene.txt
|-- FS.GG.UI.Controls.txt
`-- FS.GG.UI.Testing.txt
```

**Structure Decision**: Keep the dependency-light retained/damage evidence model in `FS.GG.UI.Scene`, because it extends the existing `VisualInspection*` vocabulary without adding package cycles. Emit retained transition artifacts in `FS.GG.UI.Controls`, because only Controls can inspect `RetainedRender` identities, invalidation evidence, bounds, and work-reduction counters. Put validation, readiness aggregation, Markdown/JSON rendering, and artifact helpers in `FS.GG.UI.Testing`, matching Feature 165. Add a `retained-inspection` lane to the existing validation-lane catalog instead of reviving a missing wrapper command.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Retained/damage evidence uses additive records/wrappers rather than changing existing visual inspection records.
- The first data source is the real retained path: `RetainedRender.init`, `RetainedRender.step`, retained invalidation evidence, work-reduction counters, and existing damage-region helpers.
- Dirty area uses existing true-union semantics and reports visible union area plus percentage.
- Locality validation compares dirty regions to declared expected regions/scopes, treats dirty regions outside scope or above a scenario-specific maximum dirty percentage as broad damage, and flags full-surface dirty regions for localized interactions.
- Shifted nodes and repainted nodes are reported as separate facts even when the same node is both shifted and repainted.
- Unsupported, unavailable, first-frame, empty-damage, hidden, clipped, virtualized, and off-screen cases are explicit states.
- The AntShowcase migration target is `VisualShell.theme and current page affordances render in full shell` for the `charts-statistical` page at preferred size in light/dark themes.
- The canonical validation entry point is the existing validation-lane runner with a new `retained-inspection` lane.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Observable contracts:

- [Retained Inspection API](contracts/retained-inspection-api.md)
- [Retained Damage Artifacts](contracts/retained-damage-artifacts.md)
- [Damage Locality Validation](contracts/damage-locality-validation.md)
- [Validation Entry Point](contracts/validation-entry-point.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Design artifacts preserve Tier 1 scope and define retained/damage model, adapter, artifact, validation, sample, and lane contracts. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts name intended `.fsi` additions, API-shape checks, focused tests, and validation evidence before implementation. |
| Visibility lives in `.fsi` | PASS | Public records/modules are additive declarations in `.fsi` files; `RetainedRender` internals stay internal unless explicitly wrapped. |
| Idiomatic simplicity | PASS | Records/unions plus pure helpers are sufficient; retained fixture orchestration uses existing test patterns and validation-lane infrastructure. |
| Elmish/MVU boundary | PASS | No persistent runtime state workflow is added. Lane execution and artifact writing remain existing edge concerns. |
| Test evidence | PASS | `quickstart.md` lists retained/damage tests, sample adoption tests, surface checks, direct focused commands, and canonical lane evidence. |
| Observability and safe failure | PASS | Artifact contracts require command/status/elapsed/artifact paths, dirty percentage, affected nodes/regions, unsupported reasons, and stable findings. |
| Tier 1 compatibility | PASS | Existing visual inspection and screenshot readiness contracts remain additive compatibility paths; migration notes and surface baselines are mandatory. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.

# Implementation Plan: Structured Render/Layout Inspection Metadata

**Branch**: `165-render-layout-inspection` | **Date**: 2026-06-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/165-render-layout-inspection/spec.md`

## Summary

Add a structured visual inspection contract that lets framework tests, samples, and generated products assert layout/text/paint facts as data instead of relying only on screenshot review. The dependency-light inspection model lives with `FS.GG.UI.Scene` so `FS.GG.UI.Controls` can emit it and `FS.GG.UI.Testing` can validate/report it without introducing reverse package dependencies. Controls will provide the first adapter from `Control.renderTree` output; Testing will provide deterministic validation, readiness status aggregation, and reviewer-readable summaries.

## Technical Context

**Language/Version**: F# on .NET `net10.0`, `LangVersion=latest`

**Primary Dependencies**: Existing project dependencies only. `FS.GG.UI.Scene` remains dependency-light. `FS.GG.UI.Controls` already depends on `Scene`, `Layout`, `KeyboardInput`, and `DesignSystem`. `FS.GG.UI.Testing` already depends on `Scene` and pinned `SkiaSharp`; this feature must not add a `Controls` or `Layout` dependency to Testing.

**Storage**: Filesystem readiness artifacts: generated Markdown summaries, generated JSON inspection reports, package-surface evidence, and sample readiness notes. No database or persistent runtime storage.

**Testing**: Expecto tests through `dotnet test tests/Scene.Tests/Scene.Tests.fsproj`, `dotnet test tests/Controls.Tests/Controls.Tests.fsproj`, and `dotnet test tests/Testing.Tests/Testing.Tests.fsproj`; repository gates through `./fake.sh build -t CapabilityCheck`, `./fake.sh build -t PackageSurfaceCheck`, `./fake.sh build -t PackLocal`, and `./fake.sh build -t GeneratedProductCheck`.

**Target Platform**: Cross-platform .NET library packages and headless validation. Inspection must run without a live GL/OpenGL window. Screenshot evidence remains a separate visual-readiness workflow.

**Project Type**: Packable F# library packages with validation helpers and sample-readiness adoption.

**Performance Goals**: Inspection generation and validation are deterministic and practical for the representative sample coverage defined in `spec.md`, completing the planned sample inspection coverage in under two minutes on a maintainer workstation. The validation command output must record elapsed wall-clock time. Validation is linear where possible; pairwise overlap checks are acceptable for the initial bounded corpus and must produce stable findings.

**Constraints**: Public surface is declared through `.fsi` files first. Surface baselines must be updated for every package whose public contract changes. `Scene` must not reference Layout, Controls, SkiaViewer, SkiaSharp, Yoga.Net, Elmish, or KeyboardInput. `Controls` must not depend on Testing. `Testing` must not pull Controls or Layout into generated-product validation consumers. Unsupported or unavailable inspection facts must be explicit and cannot be counted as accepted evidence. Readiness reporting must use the canonical states accepted, blocked, unsupported, environment-limited, not-inspected, and not-run; inspected is a coverage label, not a readiness result.

**Scale/Scope**: Initial feature scope covers reusable model/types, a Controls adapter, validation/report helpers, deterministic defect corpus tests, and selected sample/generate-product evidence. It does not implement input-to-present responsiveness diagnostics, scheduler changes, screenshot capture, contact-sheet composition, or broad visual redesigns found by inspection.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Principle I - Spec -> FSI -> Semantic Tests -> Implementation**: PASS. This is a Tier 1 contract change. The implementation sequence must draft `.fsi` surfaces first, exercise the intended API shape through F# Interactive or an equivalent prelude transcript, add semantic tests against those public surfaces, then implement `.fs` bodies.
- **Principle II - Visibility Lives in `.fsi`, Not in `.fs`**: PASS. New public types/modules will be exposed only through `Scene.fsi`, `Control.fsi` or a new Controls `.fsi`, and `Testing.fsi`. No top-level visibility keywords are planned in `.fs`.
- **Principle III - Idiomatic Simplicity Is the Default**: PASS. The design uses records, discriminated unions, lists/maps, and simple validation functions. No custom operators, SRTP, type providers, reflection, or non-trivial computation expressions are planned.
- **Principle IV - Elmish/MVU Boundary for Stateful or I/O Workflows**: PASS. The shared inspection model and validators are pure request/result helpers. File writing for summaries remains an edge command or caller responsibility, so no new MVU workflow is required for this feature.
- **Principle V - Test Evidence Is Mandatory**: PASS. Failing-first tests will cover the inspection model, Controls-derived artifact generation, text fit, overlap, clipping, paint coverage, unsupported facts, intentional exceptions, stable identities, generated summaries, and legacy layout-evidence compatibility. Any synthetic fixtures must be named with `Synthetic` and disclosed in test comments.
- **Principle VI - Observability and Safe Failure**: PASS. Validation results include severity, finding codes, affected node/region ids, unsupported facts, and diagnostics. Missing or unsupported facts fail closed instead of silently passing.
- **Tier 1 obligations**: PASS. Public API deltas, surface baselines, compatibility notes, package evidence, and migration guidance are planned for Scene, Controls, and Testing if their `.fsi` files change.

## Project Structure

### Documentation (this feature)

```text
specs/165-render-layout-inspection/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- visual-inspection-api.md
|   |-- visual-inspection-artifacts.md
|   `-- visual-inspection-validation.md
`-- tasks.md
```

### Source Code (repository root)

```text
src/Scene/
|-- Scene.fsi
`-- Scene.fs

src/Controls/
|-- Control.fsi
|-- Control.fs
|-- Inspection.fsi          # if a separate public module is clearer than extending Control.fsi
`-- Inspection.fs

src/Testing/
|-- Testing.fsi
`-- Testing.fs

tests/Scene.Tests/
|-- Feature165VisualInspectionTests.fs
`-- Scene.Tests.fsproj

tests/Controls.Tests/
|-- Feature165ControlInspectionTests.fs
`-- Controls.Tests.fsproj

tests/Testing.Tests/
|-- Feature165VisualInspectionValidationTests.fs
`-- Testing.Tests.fsproj

readiness/surface-baselines/
|-- FS.GG.UI.Scene.txt
|-- FS.GG.UI.Controls.txt
`-- FS.GG.UI.Testing.txt
```

**Structure Decision**: Put the dependency-light inspection data model in `FS.GG.UI.Scene`, because both Controls and Testing already reference Scene and Scene already owns the legacy `LayoutEvidenceReport`. Put inspected-Control extraction in Controls, because only Controls can understand control ids, authored keys, layout output, clipping, overlays, and text attributes. Put validation, readiness status, Markdown/JSON summaries, and generated-product assertions in Testing, because Testing already owns validation helper contracts and readiness evidence.

## Complexity Tracking

No constitution violations are planned.

## Phase 0 Research

See [research.md](./research.md). All technical context unknowns are resolved:

- Inspection contract ownership is split across Scene, Controls, and Testing to preserve package boundaries.
- No new dependency is planned.
- The initial Controls adapter derives inspection facts from `Control.renderTree`, layout bounds, control ids, scene nodes, text measurement facts, clip hierarchy, and overlay metadata already available in the Controls package.
- Unsupported facts are explicit findings, not absent data.
- Legacy `LayoutEvidenceReport` remains supported while new inspection evidence is introduced.
- Validation evidence uses focused package tests and package-surface gates.

## Phase 1 Design

See [data-model.md](./data-model.md), [contracts/visual-inspection-api.md](./contracts/visual-inspection-api.md), [contracts/visual-inspection-artifacts.md](./contracts/visual-inspection-artifacts.md), [contracts/visual-inspection-validation.md](./contracts/visual-inspection-validation.md), and [quickstart.md](./quickstart.md).

## Post-Design Constitution Check

- **Principle I**: PASS. Contracts identify the intended `.fsi` surfaces, the F# Interactive/API-shape exercise, and semantic tests before implementation.
- **Principle II**: PASS. Design keeps public surface centralized in `.fsi` files and requires surface-baseline updates.
- **Principle III**: PASS. Design artifacts use plain records, unions, modules, and deterministic validation rules.
- **Principle IV**: PASS. No new stateful or I/O-heavy workflow is introduced; summary writing remains an edge concern.
- **Principle V**: PASS. Quickstart lists focused tests, defect corpus evidence, surface checks, package/local validation, and readiness summary evidence.
- **Principle VI**: PASS. Contracts fail safely for missing facts, unsupported transforms, invalid exceptions, incomplete summaries, and environment-limited evidence.

# Implementation Plan: Shared Visual Readiness Tooling

**Branch**: `164-shared-visual-readiness` | **Date**: 2026-06-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/164-shared-visual-readiness/spec.md`

## Summary

Add reusable visual-readiness helpers to `FS.GG.UI.Testing` so samples and generated products can declare pages, themes, accepted sizes, and output locations, then receive deterministic capture targets, PNG completeness classification, reviewer-classification gates, machine-readable reports, and manual-safe summary updates. AntShowcase is the first adopter and regression target; it keeps page registries, theme selection, screenshot capture, and contact-sheet image composition at the sample/app edge while moving generic evidence and summary behavior into the shared Testing package.

## Technical Context

**Language/Version**: F# on .NET `net10.0`, `LangVersion=latest`

**Primary Dependencies**: Existing `FS.GG.UI.Testing` dependencies: `FS.GG.UI.Scene` and pinned `SkiaSharp` `4.147.0-preview.3.1`; tests use Expecto. AntShowcase consumes `FS.GG.UI.*` packages and uses `FS.GG.UI.SkiaViewer` for screenshot capture.

**Storage**: Filesystem readiness artifacts: PNG screenshots, reviewer Markdown, generated Markdown summaries, generated JSON summaries, contact-sheet paths, and package-surface/readiness reports. No database.

**Testing**: Expecto tests through `dotnet test tests/Testing.Tests/Testing.Tests.fsproj`; AntShowcase sample tests through `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj`; repository gates through `./fake.sh build -t CapabilityCheck`, `./fake.sh build -t PackageSurfaceCheck`, `./fake.sh build -t PackLocal`, and `./fake.sh build -t GeneratedProductCheck`.

**Target Platform**: Cross-platform .NET package and sample tooling. Pure matrix, reviewer, and summary logic must run headless. Real screenshot capture remains host-dependent through the AntShowcase/SkiaViewer app edge.

**Project Type**: Packable F# library package plus sample migration.

**Performance Goals**: Matrix expansion, completeness classification, reviewer parsing, and report generation are deterministic and linear in target count. The AntShowcase preferred run covers 38 required captures, and the minimum-size run covers 12 required captures, without adding runtime cost to rendering paths.

**Constraints**: Public surface is declared in `src/Testing/Testing.fsi`; implementation follows that signature in `src/Testing/Testing.fs`. Surface baseline `readiness/surface-baselines/FS.GG.UI.Testing.txt` must be updated. The shared package must not own sample page rendering, theme resolution, screenshot capture, or SkiaSharp contact-sheet composition. Degraded captures require reasons and cannot be accepted as complete. Summary updates must preserve manual content outside managed markers or fail safely.

**Scale/Scope**: Current regression scope is AntShowcase: 19 pages x 2 themes at preferred size and 6 pages x 2 themes at minimum size. New generated-product scope includes small matrices such as 3 pages x 2 themes x 2 sizes, while the model should remain practical for tens of pages and themes.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Principle I - Spec -> FSI -> Semantic Tests -> Implementation**: PASS. This Tier 1 package contract change will first draft the `Testing.fsi` surface, add semantic tests against the public API, then implement `Testing.fs`.
- **Principle II - Visibility Lives in `.fsi`, Not in `.fs`**: PASS. New public types/modules will be exposed only through `src/Testing/Testing.fsi`; no top-level visibility keywords are planned in `.fs`.
- **Principle III - Idiomatic Simplicity Is the Default**: PASS. The planned model uses records, discriminated unions, lists/maps, and straightforward file/PNG validation. No custom operators, SRTP, type providers, reflection, or non-trivial computation expressions are planned.
- **Principle IV - Elmish/MVU Boundary for Stateful or I/O Workflows**: PASS. Shared readiness decision logic is pure request/result modeling. AntShowcase's existing `VisualReadinessWorkflow` remains the sample-owned MVU boundary for capture workflow state and will call the shared API.
- **Principle V - Test Evidence Is Mandatory**: PASS. Failing-first tests will cover matrix expansion, duplicate detection, PNG completeness, degraded capture reasons, reviewer parsing, summary preservation, and AntShowcase output parity. Synthetic/corrupt fixtures must be disclosed with `Synthetic` in test names and comments where applicable.
- **Principle VI - Observability and Safe Failure**: PASS. Validation results include diagnostics and actionable failure classes for missing, wrong-size, undecodable, degraded, unknown-target, duplicate, malformed, stale, and unsafe-summary cases.
- **Tier 1 obligations**: PASS. The plan includes public API compatibility notes, baseline updates, package evidence, migration guidance, and AntShowcase regression validation.

## Project Structure

### Documentation (this feature)

```text
specs/164-shared-visual-readiness/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- visual-readiness-api.md
|   `-- visual-readiness-artifacts.md
`-- tasks.md
```

### Source Code (repository root)

```text
src/Testing/
|-- Testing.fsi
`-- Testing.fs

tests/Testing.Tests/
|-- Feature164VisualReadinessTests.fs
|-- Testing.Tests.fsproj
`-- Program.fs

readiness/surface-baselines/
`-- FS.GG.UI.Testing.txt

samples/AntShowcase/
|-- AntShowcase.Core/
|   |-- VisualReadinessWorkflow.fsi
|   |-- VisualReadinessWorkflow.fs
|   |-- Evidence.fsi
|   `-- Evidence.fs
|-- AntShowcase.App/
|   |-- VisualReadiness.fsi
|   `-- VisualReadiness.fs
`-- AntShowcase.Tests/
    |-- VisualReadinessTests.fs
    `-- AntShowcase.Tests.fsproj
```

**Structure Decision**: Implement the reusable public contract in `src/Testing` because `FS.GG.UI.Testing` already owns generated-product validation helpers, screenshot evidence validation, SkiaSharp PNG decoding, and package-visible readiness contracts. Keep AntShowcase-specific rendering, page selection, theme aliases, `Viewer.captureScreenshotEvidence`, and contact-sheet image composition under `samples/AntShowcase`.

## Complexity Tracking

No constitution violations are planned.

## Phase 0 Research

See [research.md](./research.md). All technical context unknowns are resolved:

- Public API boundary is additive in `FS.GG.UI.Testing`.
- No new dependency is planned.
- PNG validation uses the existing Testing package SkiaSharp dependency.
- Contact-sheet composition remains adapter/sample-owned.
- Manual summary preservation uses managed section markers with safe-fail semantics.
- AntShowcase migration preserves the current preferred/minimum evidence meanings.

## Phase 1 Design

See [data-model.md](./data-model.md), [contracts/visual-readiness-api.md](./contracts/visual-readiness-api.md), [contracts/visual-readiness-artifacts.md](./contracts/visual-readiness-artifacts.md), and [quickstart.md](./quickstart.md).

## Post-Design Constitution Check

- **Principle I**: PASS. Contracts identify the `.fsi` API before implementation and the semantic test targets.
- **Principle II**: PASS. Public surface remains centralized in `Testing.fsi` and the surface baseline.
- **Principle III**: PASS. Design artifacts select plain records, unions, and modules with no complex F# features.
- **Principle IV**: PASS. Shared API stays pure where possible; file/PNG/report operations are explicit request/result helpers, and AntShowcase remains the MVU workflow owner.
- **Principle V**: PASS. Quickstart lists focused Testing tests, AntShowcase parity checks, package surface validation, and package-local validation evidence.
- **Principle VI**: PASS. Contracts require diagnostics and safe failures for malformed summaries, unknown reviewer targets, stale artifacts, and undecodable screenshots.

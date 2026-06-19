# Implementation Plan: Second Ant Showcase Sample

**Branch**: `171-second-antshowcase-sample` | **Date**: 2026-06-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/171-second-antshowcase-sample/spec.md`

## Summary

Add `samples/SecondAntShowcase` as an independent package-consuming Ant Design showcase beside the existing `samples/AntShowcase`. The sample keeps the one semantic FS.GG control set styled by the shipped Ant theme, maps current controls exactly once across catalog pages, wires every interactive demonstration to visible state changes through a pure MVU core, adds six enterprise Ant page templates, and produces repeatable coverage, interaction, evidence, and visual-readiness artifacts. Final acceptance requires live visual review in Ant light and Ant dark at both accepted sizes with zero unresolved Ant Design findings; environment-limited evidence must be disclosed and cannot be treated as visual acceptance.

## Technical Context

**Language/Version**: F# on .NET `net10.0`, repository `LangVersion=latest`.

**Primary Dependencies**: Existing repository/package surface only: `FS.GG.UI.Controls`, `FS.GG.UI.Scene`, `FS.GG.UI.Themes.AntDesign`, `FS.GG.UI.Testing`, `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Color`, Expecto, and pinned SkiaSharp through existing package references. The sample consumes `FS.GG.UI.*` through the local packed NuGet feed, matching `samples/AntShowcase`. No new external package is planned.

**Storage**: Filesystem artifacts only: sample-local coverage/evidence output under `artifacts/second-ant-showcase/` when run ad hoc, and feature readiness output under `specs/171-second-antshowcase-sample/readiness/` for committed review evidence, FSI authoring notes, sample surface baselines, and documentation-review proof. No database or runtime cache is introduced.

**Testing**: Expecto tests in `samples/SecondAntShowcase/SecondAntShowcase.Tests`; focused commands for FSI/prelude API-shape evidence, sample public-surface baseline drift, coverage, deterministic evidence, interaction scripts, theme invariance, visual readiness target matrix, review finding lifecycle, template behavior, and documentation-review proof. Product package checks are required only if implementation changes `src/` public surfaces; no such changes are planned.

**Target Platform**: Cross-platform .NET sample with a GL-backed interactive desktop path when a live window is available, plus headless deterministic commands for coverage, listing, evidence, and environment-limited visual-readiness reporting.

**Project Type**: Standalone sample application with pure Core library, thin App/interpreter, and sample-specific tests. It is not a new product package and does not become a replacement for `samples/AntShowcase`.

**Performance Goals**: Any page is reachable in no more than two navigation actions. The headless representative review path completes repeatable non-live evidence in under 30 seconds when no live visual environment is available. Visual-readiness matrix expansion and summary generation are deterministic and linear in target count. Interactive rendering should preserve the existing sample expectation of usable inspection at `1600x1000` preferred and `1280x800` minimum.

**Constraints**: Ant is adopted as a design language only: no React, DOM, HTML/CSS, Ant-specific control forks, new product controls, new product themes, or behavior changes solely to make the sample pass. Styling uses existing semantic controls, `DesignTokens`/`DesignTokensExt`, `StyleResolver`, `ColorPolicy`, and `AntTheme.antLight`/`antDark`. Stateful behavior uses a pure MVU model/update in Core; GL, files, screenshots, and persistence remain App edge effects. Live visual acceptance requires a live visual environment and reviewer classification; degraded or synthetic evidence must disclose limitations.

**Scale/Scope**: Current planning snapshot is the existing 96-control catalog represented by 13 catalog pages plus six template pages. The coverage contract reads `Catalog.supportedControls` at implementation time, so growth or shrinkage fails coverage until the second showcase is updated. Visual review covers all 19 pages in both Ant appearances at both accepted sizes: `19 pages x 2 themes x 2 sizes = 76` required targets for this feature.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists, has no clarification markers, and now classifies this as a Tier 1 sample/evidence workflow addition with no planned `FS.GG.UI.*` product public API change. |
| Spec -> FSI -> semantic tests -> implementation | PASS | The sample will inventory every public sample Core module, draft curated `.fsi` files before bodies, record an FSI/prelude authoring transcript, then add semantic tests before implementation. |
| Visibility lives in `.fsi` | PASS | Public sample Core surfaces get matching `.fsi` signatures and a sample surface baseline. App executable helpers remain CLI-edge implementation details unless referenced as a library surface, in which case they must join the `.fsi` inventory before implementation. No product package `.fsi` changes are planned. |
| Idiomatic simplicity | PASS | Records, discriminated unions, lists/maps, straightforward MVU reducers, and existing Testing helpers are sufficient. No custom operators, SRTP, reflection discovery, type providers, or new computation expressions are planned. |
| Elmish/MVU boundary | PASS | Control state, navigation, theme switching, overlays, form validation, and review finding lifecycle are modeled as pure `Model`/`Msg`/`update` in Core. Interactive windowing, file output, screenshot capture, and persisted evidence stay at the App edge. |
| Test evidence is mandatory | PASS | FSI/prelude, surface-baseline, coverage, interaction, template, theme-invariance, deterministic evidence, visual target matrix, review finding lifecycle, documentation-review, CLI failure, and environment-limited evidence tests are planned. Synthetic/headless evidence must use explicit disclosure. |
| Observability and safe failure | PASS | CLI commands emit actionable diagnostics for coverage drift, unknown page/theme/size, missing local package feed, unavailable GL/display, incomplete screenshots, unresolved visual findings, and invalid review summaries. |
| Tier 1 compatibility | PASS | Existing `samples/AntShowcase` remains unchanged and discoverable, with a full-tree unchanged guard. No product package API change is planned; any accidental `src/` public surface change requires `.fsi`, surface baseline, tests, and compatibility notes. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/171-second-antshowcase-sample/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- sample-architecture.md
|   |-- control-coverage.md
|   |-- interaction-contracts.md
|   |-- visual-review.md
|   `-- evidence-artifacts.md
`-- readiness/
    |-- coverage.md
    |-- interaction-review.md
    |-- visual-review-summary.md
    |-- visual-findings.md
    |-- limitations.md
    |-- documentation-review.md
    |-- fsi/
    |   |-- README.md
    |   `-- second-ant-showcase-authoring.fsx
    |-- surface-baselines/
    |   `-- SecondAntShowcase.Core.txt
    |-- preferred/
    `-- minimum/
```

### Source Code (repository root)

```text
samples/SecondAntShowcase/
|-- nuget.config
|-- Directory.Build.props
|-- Directory.Packages.props
|-- PROVENANCE.md
|-- README.md
|-- coverage-report.md
|-- SecondAntShowcase.Core/
|   |-- SecondAntShowcase.Core.fsproj
|   |-- Model.fsi
|   |-- Model.fs
|   |-- DemoState.fsi
|   |-- DemoState.fs
|   |-- AntTheme.fsi
|   |-- AntTheme.fs
|   |-- PageRegistry.fsi
|   |-- PageRegistry.fs
|   |-- CoverageMap.fsi
|   |-- CoverageMap.fs
|   |-- InteractionContracts.fsi
|   |-- InteractionContracts.fs
|   |-- ReviewFindings.fsi
|   |-- ReviewFindings.fs
|   |-- VisualConfig.fsi
|   |-- VisualConfig.fs
|   |-- VisualReadinessWorkflow.fsi
|   |-- VisualReadinessWorkflow.fs
|   |-- Evidence.fsi
|   |-- Evidence.fs
|   |-- Shell.fsi
|   |-- Shell.fs
|   |-- Pages.fsi
|   |-- Pages.fs
|   |-- Templates.fsi
|   `-- Templates.fs
|-- SecondAntShowcase.App/
|   |-- SecondAntShowcase.App.fsproj
|   |-- Program.fs
|   |-- Interactive.fs
|   |-- Evidence.fs
|   |-- VisualReadiness.fs
|   `-- Diagnostics.fs
`-- SecondAntShowcase.Tests/
    |-- SecondAntShowcase.Tests.fsproj
    |-- Main.fs
    |-- FsiSurfaceTests.fs
    |-- PublicSurfaceTests.fs
    |-- CoverageTests.fs
    |-- InteractionTests.fs
    |-- TemplateTests.fs
    |-- ThemeInvarianceTests.fs
    |-- DeterminismTests.fs
    |-- DocumentationReviewTests.fs
    |-- VisualReadinessTests.fs
    |-- ReviewFindingTests.fs
    |-- AntDesignFidelityTests.fs
    `-- VisualTestHelpers.fs
```

**Structure Decision**: Mirror the proven `samples/AntShowcase` Core/App/Tests split while naming the new sample `SecondAntShowcase` so it is independently discoverable. Core owns pure state, page registry, seeded content, coverage, interaction contracts, review finding state, and readiness workflow decisions. Every public Core module in the planned structure has a curated `.fsi`, a pre-implementation FSI/prelude exercise, and sample surface-baseline evidence. App owns executable commands, GL/window interaction, screenshot capture, filesystem evidence, and diagnostics; tests should exercise App behavior through CLI contracts unless an App helper is intentionally promoted to a public library surface and added to the `.fsi` inventory. The sample consumes packed `FS.GG.UI.*` packages through `nuget.config` instead of `src/` project references.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- The sample is a new `samples/SecondAntShowcase` package-consuming sample, not a rename or fork of existing `samples/AntShowcase`.
- The Ant source of truth is the local Ant hub and family pattern docs under `docs/product/ant-design/`, guided by `fs-gg-ant-design`.
- Coverage is a live bijection against `FS.GG.UI.Controls.Catalog.supportedControls`; the current snapshot is 96 controls across 13 catalog pages.
- Interactive behavior is driven by a single pure MVU `DemoState`/`Msg` contract with deterministic seeded content and representative scripts.
- Theme switching changes only `AntTheme.antLight`/`antDark` resolution and preserves current page, entered values, selections, overlays, expanded state, and validation state.
- Visual review covers all pages in both themes at both accepted sizes and uses an explicit finding lifecycle that blocks acceptance until zero unresolved findings remain.
- Evidence distinguishes live visual acceptance from headless or synthetic/environment-limited evidence.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Observable contracts:

- [Sample Architecture](contracts/sample-architecture.md)
- [Control Coverage](contracts/control-coverage.md)
- [Interaction Contracts](contracts/interaction-contracts.md)
- [Visual Review](contracts/visual-review.md)
- [Evidence Artifacts](contracts/evidence-artifacts.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Design artifacts preserve Tier 1 sample/evidence scope and keep product API changes out of scope. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts identify sample `.fsi` surfaces, FSI/prelude evidence, sample surface-baseline checks, and focused tests before implementation tasks. |
| Visibility lives in `.fsi` | PASS | Every planned public Core module has a matching `.fsi`; product package `.fsi` files are not expected to change. |
| Idiomatic simplicity | PASS | Data model uses plain records/unions, deterministic literals, pure reducers, and existing visual-readiness helpers. |
| Elmish/MVU boundary | PASS | Stateful workflows are represented in Core; App edge interprets live GL, file writes, screenshot capture, and CLI effects. |
| Test evidence | PASS | Quickstart lists FSI/prelude, surface-baseline, coverage, interaction, template, theme, determinism, visual-readiness, review lifecycle, documentation-review, and package-consuming build evidence. |
| Observability and safe failure | PASS | Contracts require coverage drift diagnostics, visual finding summaries, environment limitation disclosure, and non-hanging degraded evidence. |
| Tier 1 compatibility | PASS | Existing AntShowcase is preserved through a full-tree unchanged guard; compatibility evidence states whether any product public surface changed. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.

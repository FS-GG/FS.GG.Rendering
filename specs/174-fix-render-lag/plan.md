# Implementation Plan: Fix Render Lag

**Branch**: `174-fix-render-lag` | **Date**: 2026-06-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/174-fix-render-lag/spec.md`

## Summary

Reduce the visible render lag in the Second Ant Showcase by removing repeated full-tree frame-preparation work from interactive retained-render frames while preserving the current rendered output, event routing, accessibility-facing metadata, diagnostics, and package surface. The primary fix is internal to the retained controls render path: keep frame preparation proportional to changed or required visual work, make metadata collection reuse retained state where safe, and keep any unavoidable full work phase-attributed so paint and presentation cost are not misdiagnosed.

The plan reuses the existing Feature 173 live responsiveness runner and the `render-lag-probe` diagnostic command for accepted evidence. Deterministic tests cover retained work scaling and parity; visible desktop runs prove the button activation and page-change budgets. Headless or unsupported environments remain explicit `environment-limited` results.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`LangVersion=latest`). Public package surfaces are declared in `.fsi`; this feature is expected to remain internal and additive-free.

**Primary Dependencies**: Existing `FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`, `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Themes.AntDesign`, `Fable.Elmish`, `Silk.NET.Input/Windowing/OpenGL`, `SkiaSharp`, `System.Diagnostics`, `System.Text.Json`, and Expecto/YoloDev test runner. No new dependency is planned.

**Storage**: Filesystem evidence artifacts only. Feature evidence is written under `specs/174-fix-render-lag/readiness/` for retained work records, live responsiveness summaries, render-lag traces, visual parity, and validation notes. Runtime code does not introduce persistent storage.

**Testing**: Expecto through `dotnet test`. Focused coverage in `tests/Controls.Tests`, `tests/Elmish.Tests`, and `samples/SecondAntShowcase/SecondAntShowcase.Tests` covers retained work scaling, phase attribution, button activation, page navigation, sample CLI evidence, fail-closed live reporting, and visual/interaction parity. Visible desktop validation runs the sample CLI with `--require-live` plus `render-lag-probe` scenarios.

**Target Platform**: Desktop OpenGL/Skia viewer host in a visible, focusable Linux desktop session for accepted latency evidence; deterministic/headless test paths for regression shape and environment-limited reporting.

**Project Type**: Multi-package F# rendering/UI framework plus a package-consuming desktop sample and sample CLI evidence commands.

**Performance Goals**: Button activation follow-up visual frame: median <= 150 ms and p95 <= 250 ms. Page navigation to `text-numeric-input`: median <= 250 ms and p95 <= 500 ms. Largest non-paint preparation contribution reduced by >= 80% from the 2026-06-19 baseline. Initial visible frame preparation reduced by >= 50% where the same bottleneck is present.

**Constraints**: Tier 2 internal performance change. No public API, package identity, dependency, rendering semantics, event routing, accessibility metadata, diagnostic contract, or behavior drift is expected. If a public surface or dependency becomes necessary, reclassify to Tier 1 before implementation. Accepted live evidence requires a real presentation boundary; unsupported hosts may only produce classified limitations.

**Scale/Scope**: Two required representative interactions: button activation on the `buttons` page and page navigation to `text-numeric-input`. Regression scope includes retained render internals, Controls.Elmish frame metrics, SecondAntShowcase responsiveness/probe artifacts, sample coverage, and visual readiness across the existing 19-page showcase.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists, classifies the work as Tier 2, names no expected public API impact, and defines latency/parity evidence. |
| Spec -> FSI -> semantic tests -> implementation | PASS | No public surface is planned. Any internal `.fsi` changes stay assembly-internal and must be driven by failing semantic/regression tests before `.fs` implementation. Public-surface discovery requires Tier 1 reclassification. |
| Visibility lives in `.fsi` | PASS | Public modules remain governed by existing `.fsi` files. No top-level access modifiers or package surface changes are planned. |
| Idiomatic simplicity | PASS | The design uses existing retained records, work-reduction counters, frame metrics, sample CLI artifacts, and JSON/Markdown writers. No new operators, SRTP, reflection, type providers, or dependencies are planned. |
| Elmish/MVU boundary | PASS | Product state and sample interactions continue through `SecondAntShowcase.Core.Model` and `ControlsElmish` host boundaries. The render fix stays in pure retained frame preparation; live IO remains at the viewer/CLI edge. |
| Test evidence | PASS | The quickstart requires automated regression tests, deterministic work-scaling checks, parity checks, and visible desktop latency evidence or explicit environment-limited reporting. |
| Observability and safe failure | PASS | Existing `FrameMetrics`, `ResponsivenessTimingContribution`, live runner summaries, render-lag trace diagnostics, and environment-limited classifications remain the evidence path. |
| Tier 2 obligations | PASS | Internal performance work preserves behavior and public surface. Surface baselines remain unchanged; if not, the plan must be revised before implementation. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/174-fix-render-lag/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- render-lag-evidence.md
|   |-- retained-frame-preparation.md
|   `-- responsiveness-validation.md
|-- feedback/
|   `-- plan-2026-06-20.md
`-- readiness/
    |-- render-lag/
    |-- responsiveness/
    |-- visual-parity/
    `-- validation-summary.md
```

### Source Code (repository root)

```text
src/
|-- Controls/
|   |-- RetainedRender.fsi
|   |-- RetainedRender.fs
|   |-- ControlRuntime.fsi
|   `-- ControlRuntime.fs
|-- Controls.Elmish/
|   |-- ControlsElmish.fsi
|   `-- ControlsElmish.fs
`-- SkiaViewer/
    |-- SkiaViewer.fsi
    `-- SkiaViewer.fs

tests/
|-- Controls.Tests/
|   |-- Feature174RetainedRenderFixtures.fs
|   |-- Feature174RetainedRenderWorkTests.fs
|   |-- Feature174RetainedRenderParityTests.fs
|   |-- Feature174PageNavigationWorkTests.fs
|   `-- Feature174PageNavigationParityTests.fs
|-- Elmish.Tests/
|   |-- Feature174FramePhaseTests.fs
|   `-- Feature174ResponsivenessRegressionTests.fs
`-- SkiaViewer.Tests/

samples/SecondAntShowcase/
|-- SecondAntShowcase.App/
|   |-- RenderLagProbe.fs
|   |-- Responsiveness.fs
|   `-- Program.fs
|-- SecondAntShowcase.Core/
|   |-- Host.fs
|   |-- Model.fs
|   |-- PageRegistry.fs
|   `-- Scripts.fs
`-- SecondAntShowcase.Tests/
    |-- Feature174RenderLagFixtures.fs
    |-- Feature174RenderLagProbeTests.fs
    |-- Feature174ResponsivenessBudgetTests.fs
    |-- Feature174VisualParityTests.fs
    `-- Feature174EnvironmentLimitedTests.fs
```

**Structure Decision**: Keep optimization in framework-owned retained rendering and frame metrics. Keep sample-specific scenario selection, CLI outputs, and readiness artifacts in `samples/SecondAntShowcase`. No new project, package, dependency, or public surface is planned.

## Phase 0 Research

See [research.md](./research.md). All planning unknowns are resolved:

- The feature targets retained frame preparation, not a new live runner.
- Existing Feature 173 CLI artifacts and `render-lag-probe` are the evidence path.
- Metadata and evidence collection must scale from retained state and changed work, not repeated full-tree scans.
- Existing `FrameMetrics` and `ResponsivenessTimingContribution` are sufficient for phase attribution unless implementation proves otherwise.
- Live claims require visible desktop runs; headless runs remain classified limitations.
- No new dependency or public package surface is justified.

## Phase 1 Design and Contracts

See [data-model.md](./data-model.md) for entities, validation rules, and state transitions.

Observable and internal contracts:

- [Retained Frame Preparation](contracts/retained-frame-preparation.md)
- [Render Lag Evidence](contracts/render-lag-evidence.md)
- [Responsiveness Validation](contracts/responsiveness-validation.md)

Validation guide:

- [quickstart.md](./quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Contracts keep the work Tier 2 and define reclassification triggers for public surface, dependency, or behavior drift. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Data model and contracts identify failing work-scaling, parity, and evidence tests before retained implementation changes. |
| Visibility lives in `.fsi` | PASS | Retained internals remain internal; no public `.fsi` surface is added. Surface baseline quickstart verifies zero public delta. |
| Idiomatic simplicity | PASS | Design is records, retained caches, invalidation facts, existing metrics, and bounded artifact writers. |
| Elmish/MVU boundary | PASS | Existing sample/product update loops remain the state boundary; frame preparation remains pure except viewer/live evidence edge effects. |
| Test evidence | PASS | Quickstart requires focused tests plus live desktop evidence or explicit environment limitation. |
| Observability and safe failure | PASS | Evidence contracts require phase attribution, baseline comparison, parity, and fail-closed environment reporting. |
| Tier 2 obligations | PASS | No new dependency, package, public API, or intentional user-visible behavior change is designed. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.

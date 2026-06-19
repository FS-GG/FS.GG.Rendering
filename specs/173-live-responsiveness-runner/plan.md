# Implementation Plan: Live Responsiveness Runner

**Branch**: `173-live-responsiveness-runner` | **Date**: 2026-06-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/173-live-responsiveness-runner/spec.md`

## Summary

Create the accepted live responsiveness path for `samples/SecondAntShowcase` by turning the existing substitute-only responsiveness command into a visible-session runner. The runner will exercise every interactive family from `SecondAntShowcase.Core.InteractionContracts`, collect one live input-to-visible evidence record per representative interaction, write a complete run summary, and fail closed whenever the desktop surface, presentation boundary, timing signal, drag continuity, or artifact write is unavailable.

The technical approach reuses the existing `FS.GG.UI.SkiaViewer` responsiveness vocabulary (`ViewerLatencyRecord`, budgets, stable tokens, JSON/Markdown writers), `Controls.Elmish` timing contributions, and the Feature 172 sample evidence fields. New work is scoped to the sample runner and any additive framework hook needed to attach real live presentation records to the current CLI. Deterministic/headless scripts remain useful regression evidence, but they stay visibly non-accepted.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`LangVersion=latest`). Public package and sample surfaces are declared through `.fsi` before `.fs` implementation when the surface is public.

**Primary Dependencies**: Existing `FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`, `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Themes.AntDesign`, `Fable.Elmish`, `Silk.NET.Input/Windowing/OpenGL`, `SkiaSharp`, `System.Diagnostics`, `System.Text.Json`, and Expecto/YoloDev test runner. No new runtime dependency is planned.

**Storage**: Filesystem evidence artifacts only. Live runs write `records.jsonl`, `summary.json`, `summary.md`, `environment.md`, and optional diagnostic logs under `specs/173-live-responsiveness-runner/readiness/responsiveness/<run-id>/` for feature evidence or caller-selected output roots for ad hoc runs. Partial or failed writes are non-accepted evidence.

**Testing**: Expecto through `dotnet test`. Focused tests in `samples/SecondAntShowcase/SecondAntShowcase.Tests` cover CLI parsing, all-interactive coverage, measured/substitute status separation, artifact shape, budget aggregation, drag continuity classification, and write-failure behavior. Existing `tests/SkiaViewer.Tests`, `tests/Elmish.Tests`, and `tests/Controls.Tests` guard the input queue, retained routing, latency summaries, and interaction semantics. Visible desktop validation runs the sample CLI with `--require-live`.

**Target Platform**: Desktop OpenGL/Skia viewer host in a visible, focusable Linux desktop session. Headless or non-presenting environments may write diagnostics but cannot produce accepted live readiness.

**Project Type**: Multi-package F# rendering/UI framework plus a package-consuming desktop sample and sample CLI evidence command.

**Performance Goals**: Accepted live runs require at least 95% of representative interactions at or below 100 ms input-to-visible latency, no accepted representative interaction above 150 ms, and every value-changing drag classified as continuous visible feedback without delayed catch-up.

**Constraints**: Tier 1 observable behavior and evidence-contract change. Accepted readiness must use measured live evidence only. Missing visible surface, missing presentation boundary, unreliable timestamps, incomplete family coverage, failed artifact write, timeout, substitute evidence, skipped/manual-pending checks, or drag catch-up keeps the run non-accepted. Display-only exclusions remain explicit and do not count as failed timed interactions. Visual redesign is out of scope except preserving existing showcase behavior. Any public framework or sample surface addition must update `.fsi`, surface baselines if applicable, semantic tests, and compatibility notes.

**Scale/Scope**: Initial scope is `samples/SecondAntShowcase` all-interactive responsiveness: 14 representative interaction families in `InteractionContracts.all`, display-only exclusions from `InteractionContracts.displayOnlyReasons`, light and dark themes for final evidence, and regression checks for coverage, navigation/overlay behavior, slider/rating/value changes, visual readiness, and existing framework responsiveness contracts.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists, classifies the work as Tier 1, and names the observable evidence/readiness behavior. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Public or package-visible additions must be drafted in `.fsi` first, exercised by semantic/package-consuming tests, and only then implemented. Existing CLI behavior is covered by failing tests before changing the runner. |
| Visibility lives in `.fsi` | PASS | The likely public surfaces are `SecondAntShowcase.Core.Evidence.fsi`, `InteractionContracts.fsi`, and any additive `SkiaViewer.fsi` or `ControlsElmish.fsi` hook; private runner internals stay out of signatures. |
| Idiomatic simplicity | PASS | The plan uses existing records, discriminated unions, latency writers, JSON serializers, and pure classification helpers. No custom operators, SRTP, reflection, type providers, or new framework abstractions are planned. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Product state stays in `SecondAntShowcase.Core.Model.update`. Runner state is modeled as request/session/action/record/summary values; native input, presentation timing, and filesystem writes remain edge effects. |
| Test evidence is mandatory | PASS | Validation requires automated regression tests plus a visible-session run. Synthetic/headless evidence must remain disclosed and non-accepting. |
| Observability and safe failure | PASS | Contracts require environment diagnostics, missing-boundary status, first failed budget, five slowest interactions, drag continuity classification, write-failure reporting, and explicit non-accepted readiness. |
| Tier 1 obligations | PASS | The plan calls out additive evidence/CLI contract changes, `.fsi` and baseline updates when public surfaces move, compatibility notes, and final readiness evidence. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/173-live-responsiveness-runner/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- live-runner-cli.md
|   |-- live-evidence-artifacts.md
|   |-- interaction-coverage.md
|   `-- readiness-rules.md
`-- readiness/
    |-- responsiveness/
    |   `-- <run-id>/
    |       |-- records.jsonl
    |       |-- summary.json
    |       |-- summary.md
    |       `-- environment.md
    |-- visual-preferred/
    |-- visual-minimum/
    `-- validation-summary.md
```

### Source Code (repository root)

```text
src/
|-- SkiaViewer/
|   |-- SkiaViewer.fsi
|   |-- SkiaViewer.fs
|   `-- Host/
|       |-- OpenGl.fsi
|       `-- OpenGl.fs
|-- Controls.Elmish/
|   |-- ControlsElmish.fsi
|   `-- ControlsElmish.fs
`-- Controls/
    |-- *.fsi
    `-- *.fs

tests/
|-- SkiaViewer.Tests/
|-- Elmish.Tests/
`-- Controls.Tests/

samples/SecondAntShowcase/
|-- SecondAntShowcase.Core/
|   |-- Evidence.fsi
|   |-- Evidence.fs
|   |-- InteractionContracts.fsi
|   |-- InteractionContracts.fs
|   |-- Scripts.fs
|   `-- Model.fs
|-- SecondAntShowcase.App/
|   |-- Responsiveness.fs
|   |-- Interactive.fs
|   `-- Program.fs
`-- SecondAntShowcase.Tests/
    |-- Feature173LiveResponsivenessCliTests.fs
    |-- Feature173LiveResponsivenessArtifactTests.fs
    |-- Feature173LiveResponsivenessBudgetTests.fs
    |-- Feature173LiveResponsivenessFailClosedTests.fs
    |-- Feature173LiveResponsivenessCoverageTests.fs
    `-- Feature173LiveResponsivenessRegressionTests.fs
```

**Structure Decision**: Keep framework-owned timing and presentation facts in `SkiaViewer`/`Controls.Elmish`; keep sample-specific action coverage and artifact orchestration in `samples/SecondAntShowcase`. No new project is planned. The sample remains package-consuming after local package refresh.

## Phase 0 Research

See [research.md](./research.md). All planning unknowns are resolved:

- The live runner extends the existing `SecondAntShowcase responsiveness` command instead of adding a second command.
- The existing deterministic substitute path remains available but is never accepted as live readiness.
- Existing `SkiaViewer` responsiveness records and summary writers are the base contract; sample fields are layered additively.
- `InteractionContracts.all` and `displayOnlyReasons` remain the source of required coverage.
- Missing live prerequisites, timing boundaries, monotonic timestamps, complete writes, or drag continuity fail closed.
- No new dependency is required for Phase 1 design.
- The validation package includes framework tests, sample tests, live CLI evidence, visual readiness, coverage, and manual-review caveat disclosure.

## Phase 1 Design and Contracts

See [data-model.md](./data-model.md) for entities, validation rules, and state transitions.

Observable contracts:

- [Live Runner CLI](contracts/live-runner-cli.md)
- [Live Evidence Artifacts](contracts/live-evidence-artifacts.md)
- [Interaction Coverage](contracts/interaction-coverage.md)
- [Readiness Rules](contracts/readiness-rules.md)

Validation guide:

- [quickstart.md](./quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Contracts define the Tier 1 CLI/evidence behavior and preserve source-compatible default launch paths. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Data model and contracts identify exact public/sample surfaces to test before `.fs` changes. |
| Visibility lives in `.fsi` | PASS | The design distinguishes public record/token additions from private runner orchestration and requires `.fsi` updates for the former. |
| Idiomatic simplicity | PASS | The data model is records, tokens, validation functions, and artifact writers over existing infrastructure. |
| Elmish/MVU boundary | PASS | Runner model and messages are pure values; filesystem and live window/presentation operations are edge effects. |
| Test evidence | PASS | `quickstart.md` requires automated tests plus visible live runs and explicit blocked/environment-limited disclosure. |
| Observability and safe failure | PASS | Artifact and readiness contracts require actionable diagnostics for every blocked, rejected, failed, timed-out, substitute, or skipped state. |
| Tier 1 obligations | PASS | Contract changes require compatibility notes, public surface/baseline updates where applicable, package-consuming sample validation, and final readiness evidence. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.

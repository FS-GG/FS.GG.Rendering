# Implementation Plan: Input/Render Responsiveness

**Branch**: `167-input-render-responsiveness` | **Date**: 2026-06-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/167-input-render-responsiveness/spec.md`

## Summary

Make interactive latency measurable from native pointer/key receipt through visible presentation, then move the live viewer input path from immediate post-input scene recomposition to queued, frame-paced processing. Native input callbacks will normalize and timestamp input envelopes, enqueue them, signal the frame/update loop, and return quickly. The frame/update loop will drain queued input in priority/order, coalesce continuous pointer movement, fold all product messages for each input, recompute the retained scene at most once for a dirty frame, and emit correlated latency records plus readiness summaries.

The design keeps product-facing MVU contracts, Controls.Elmish retained rendering, focus/key routing, and AntShowcase host semantics intact. It adds a Tier 1 diagnostics/scheduler contract around existing `SkiaViewer`, `Controls.Elmish`, and readiness tooling, with AntShowcase as the first representative responsiveness run.

## Technical Context

**Language/Version**: F# on .NET `net10.0`, repository `LangVersion=latest`. Public package surface is declared in `.fsi` files before `.fs` implementation.

**Primary Dependencies**: Existing .NET SDK, Expecto `10.2.2`, Elmish, SkiaSharp, Silk.NET input/windowing, `System.Diagnostics` (`Stopwatch`, `ActivitySource`, `Meter`/metrics), `System.Text.Json`, and existing repository projects under `src/SkiaViewer`, `src/Controls.Elmish`, `src/Controls`, `src/KeyboardInput`, `tests/SkiaViewer.Tests`, `tests/Elmish.Tests`, and `samples/AntShowcase`. No OpenTelemetry or new runtime dependency is planned.

**Storage**: Filesystem readiness artifacts only. Diagnostic runs write JSONL latency records, `summary.json`, `summary.md`, environment notes, and optional raw host logs under `artifacts/responsiveness/<run-id>/` by default, or under `specs/167-input-render-responsiveness/readiness/responsiveness/<run-id>/` for committed feature evidence. No database or persistent runtime storage.

**Testing**: Expecto through `dotnet test`. Focused tests in `tests/SkiaViewer.Tests` cover input queue ordering, receipt duration classification, move coalescing, frame-drain semantics, scheduler dirty state, and callback no-render behavior. Focused tests in `tests/Elmish.Tests` cover responsiveness timing shape, pointer/key activation records, multiple messages folded before recomposition, no-visible-response records, disabled diagnostics behavior, and deterministic `Perf.runScript` compatibility. AntShowcase tests and/or app diagnostics cover representative pointer and keyboard activation evidence.

**Target Platform**: Cross-platform F#/.NET UI runtime with a live SkiaSharp/OpenGL viewer when available. Headless deterministic tests must run without a live GL window; real input-to-present evidence is environment-gated and must report unsupported or environment-limited status when a visible presentation surface is unavailable.

**Project Type**: Multi-package F# rendering/UI library plus desktop viewer host, Controls.Elmish adapter, validation helpers, and package-consuming sample applications.

**Performance Goals**: Representative diagnostic replay reports at least 95% of input receipt callbacks below 4 ms and 100% below 16 ms. For the accepted representative interaction demo, p95 pointer/key input-to-visible-response latency is below 50 ms when a live host can measure presentation. Long frame/present work over 50 ms is always counted and cannot be hidden by fast routing.

**Constraints**: Product `Init`/`Update`/`View` semantics and generated-product host shape must remain stable. Discrete pointer and keyboard input order is preserved; continuous pointer movement may be coalesced and must report coalesced counts. Native input callbacks must not run full retained scene recomposition or presentation work. One discrete input that produces several product messages is folded before scene recomposition. Deterministic `ControlsElmish.Perf.runScript` count/bool goldens remain clock-free; live timing belongs to a separate diagnostics surface. Environment-limited runs are explicit and cannot be reported as accepted readiness without substitute evidence. Resize, close, lifecycle, screenshot/readback, and app-close paths need explicit scheduling policy so shutdown and evidence capture stay correct.

**Scale/Scope**: Initial feature scope covers `src/SkiaViewer/SkiaViewer.fsi`, `src/SkiaViewer/SkiaViewer.fs`, `src/SkiaViewer/Host/OpenGl.fsi`, `src/SkiaViewer/Host/OpenGl.fs`, `src/SkiaViewer/Host/Viewer.fsi`, `src/SkiaViewer/Host/Viewer.fs`, `src/Controls.Elmish/ControlsElmish.fsi`, `src/Controls.Elmish/ControlsElmish.fs`, focused tests in `tests/SkiaViewer.Tests`, `tests/Elmish.Tests`, `tests/Rendering.Harness`, and `tests/Rendering.Harness.Tests`, AntShowcase diagnostic adoption, and readiness evidence. Out of scope: broad Controls rewrite, renderer-thread migration, damage narrowing beyond required dirty-state reporting, text-cache optimization work, package-feed validation, and screenshot/contact-sheet workflow changes.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies this as Tier 1 runtime scheduling and diagnostics work. The plan names public compatibility impact and keeps behavioral compatibility explicit. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Scheduler/diagnostic contracts are planned before implementation. Tasks must draft `.fsi` additions for viewer/adapter diagnostics, exercise the surface through F# Interactive or a prelude transcript, add failing semantic tests, then implement `.fs` bodies. |
| Visibility lives in `.fsi` | PASS | Additive public diagnostics and helper functions belong in `SkiaViewer.fsi` and `ControlsElmish.fsi`; queue storage, host wake/signal state, and GL callback details are implementation details omitted from the `.fsi` surface. |
| Idiomatic simplicity | PASS | The plan uses records, discriminated unions, queues/maps, `Stopwatch`, `ActivitySource`/metrics, JSONL writers, and pure scheduler update functions. No custom operators, SRTP, reflection, type providers, or new framework abstractions are planned. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | The scheduler is modeled as explicit state, messages, and effects: enqueue input, drain frame, render dirty scene, write diagnostics, signal host. Pure update logic decides queue/drain/dirty transitions; edge interpreters own native callbacks, timers, GL presentation, and filesystem writes. |
| Test evidence is mandatory | PASS | Tests cover pointer activation, keyboard activation, ordered discrete bursts, move coalescing, multi-message input, no-state-change input, long-frame reporting, environment-limited reporting, disabled diagnostics, and existing pointer/key behavior. Synthetic fixtures must carry `Synthetic` in test names/comments and readiness notes. |
| Observability and safe failure | PASS | Latency records include sequence id, input kind, queue depth, phase timings, coalesced counts, dirty/changed summary, environment status, and failures. Diagnostic write failures and processing errors are explicit non-green outcomes. |
| Tier 1 obligations | PASS | The feature may add public diagnostic types/options and helper functions; tasks must update `.fsi`, surface baselines, compatibility notes, semantic tests, package evidence, and migration guidance. Existing interaction semantics remain the compatibility baseline. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/167-input-render-responsiveness/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- input-scheduler.md
|   |-- latency-records.md
|   |-- responsiveness-summary.md
|   `-- antshowcase-responsiveness.md
`-- readiness/
    |-- fsi-contract-transcript.md
    |-- compatibility.md
    |-- synthetic-evidence.md
    |-- scheduler-tests.md
    `-- responsiveness/
        `-- <run-id>/
            |-- summary.md
            |-- summary.json
            |-- records.jsonl
            `-- environment.md
```

### Source Code (repository root)

```text
src/
|-- SkiaViewer/
|   |-- SkiaViewer.fsi
|   |-- SkiaViewer.fs
|   `-- Host/
|       |-- OpenGl.fsi
|       |-- OpenGl.fs
|       |-- Viewer.fsi
|       `-- Viewer.fs
`-- Controls.Elmish/
    |-- ControlsElmish.fsi
    `-- ControlsElmish.fs

tests/
|-- SkiaViewer.Tests/
|   |-- Feature167InputQueueTests.fs
|   |-- Feature167SchedulerDrainTests.fs
|   |-- Feature167ReceiptCallbackTests.fs
|   `-- SkiaViewer.Tests.fsproj
|-- Rendering.Harness/
|   |-- ValidationLanes.fsi
|   |-- ValidationLanes.fs
|   `-- Rendering.Harness.fsproj
|-- Rendering.Harness.Tests/
|   |-- Feature167ResponsivenessReadinessTests.fs
|   `-- Rendering.Harness.Tests.fsproj
`-- Elmish.Tests/
    |-- Feature167ResponsivenessMetricsTests.fs
    |-- Feature167InteractionSemanticsTests.fs
    `-- Elmish.Tests.fsproj

samples/
`-- AntShowcase/
    |-- AntShowcase.App/
    |   |-- Interactive.fs
    |   |-- Evidence.fs
    |   `-- Program.fs
    |-- AntShowcase.Core/
    |   |-- Host.fs
    |   `-- Scripts.fs
    `-- AntShowcase.Tests/
        |-- InteractionTests.fs
        `-- VisualEvidenceTests.fs
```

**Structure Decision**: Keep native callback and frame-signal ownership in `SkiaViewer`/`SkiaViewer.Host.OpenGl`, because the viewer owns windowing, GL presentation, and input receipt. Keep retained pointer/key routing and frame-work metrics in `Controls.Elmish`, because it owns `InteractiveAppHost`, `FrameMetrics`, retained routing, focus, and `Perf.runScript`. AntShowcase adds only a diagnostic mode/report over its existing package-consuming host; product code should not manually wire scheduler implementation details.

## Phase 0 Research

See [research.md](./research.md). All planning unknowns are resolved:

- The input queue and frame scheduler live in `SkiaViewer`, behind existing viewer launch functions.
- Public API changes are additive diagnostics/options; product update/view contracts stay source-compatible.
- Timing uses collector-neutral .NET primitives plus JSONL evidence, not a new telemetry dependency.
- `ControlsElmish.Perf.runScript` remains deterministic and clock-free; live responsiveness timing is a separate surface.
- Continuous pointer movement is coalesced at the input queue/frame-drain boundary; discrete pointer/key events stay ordered.
- A dirty-state/frame-drain step folds product messages before a single scene recomposition.
- Long-frame and environment-limited states are explicit readiness blockers or caveats, not hidden passes.
- AntShowcase is the first representative diagnostic run, with Enter/Space key-down as the keyboard baseline.

## Phase 1 Design and Contracts

See [data-model.md](./data-model.md) for entities, validation rules, and state transitions.

Observable contracts:

- [Input Scheduler](contracts/input-scheduler.md)
- [Latency Records](contracts/latency-records.md)
- [Responsiveness Summary](contracts/responsiveness-summary.md)
- [AntShowcase Responsiveness](contracts/antshowcase-responsiveness.md)

Validation guide:

- [quickstart.md](./quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Contracts preserve the Tier 1 runtime boundary and explicitly define compatibility, migration, and readiness evidence. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts identify `.fsi` surface additions, queue/drain behavior, JSON schema, summary rules, and test-first validation before implementation. |
| Visibility lives in `.fsi` | PASS | Only additive diagnostics/options and public helpers are surfaced through `.fsi`; queue storage, host callbacks, timers, and GL details stay as implementation details omitted from the signature surface. |
| Idiomatic simplicity | PASS | Data model uses simple records/unions, ordered queues, pure transition helpers, and JSON/Markdown output. No complexity exceptions are introduced. |
| Elmish/MVU boundary | PASS | `input-scheduler.md` defines scheduler model/messages/effects and keeps native input, timers, rendering, and diagnostics at interpreter edges. |
| Test evidence | PASS | `quickstart.md` lists focused SkiaViewer, Elmish, AntShowcase, package-surface, live-host, environment-limited, and disabled-diagnostics checks. |
| Observability and safe failure | PASS | `latency-records.md` and `responsiveness-summary.md` require phase timings, queue depth, long-frame counts, missing-boundary status, write failures, and first failed budget. |
| Tier 1 obligations | PASS | `antshowcase-responsiveness.md` and `responsiveness-summary.md` require compatibility notes, surface baselines, migration guidance, and package/readiness evidence for public diagnostic additions. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.

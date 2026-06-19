# Implementation Plan: Fix Mouse Interaction Lag

**Branch**: `172-fix-mouse-lag` | **Date**: 2026-06-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/172-fix-mouse-lag/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Mouse interaction in `samples/SecondAntShowcase` still feels laggy after the previous
post-interactive fixes. Close the feature by treating responsiveness as an accepted live
evidence problem: preserve the retained pointer route and input-drain behavior in the
framework hot path, extend the sample responsiveness command to collect visible-session
input-to-present records across every interactive family, and fail closed when a visible
desktop session cannot produce reliable timing. The validation package also reruns the
SecondAntShowcase regression checks for opaque backgrounds, Ant-like navigation, mapped
control coverage, and slider click/drag behavior.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`LangVersion=latest`)

**Primary Dependencies**: `FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`,
`FS.GG.UI.SkiaViewer`, `FS.GG.UI.Themes.AntDesign`, `Fable.Elmish`,
`Silk.NET.Input/Windowing/OpenGL`, `SkiaSharp`, `Expecto`

**Storage**: File-system evidence artifacts only: `records.jsonl`, `summary.json`,
`summary.md`, `environment.md`, screenshots/contact sheets, and feature-local readiness
outputs under `specs/172-fix-mouse-lag/readiness/`

**Testing**: `dotnet test` with Expecto/YoloDev runner, deterministic
`ControlsElmish.Perf.runScript` tests, sample CLI contract tests, package-consuming
SecondAntShowcase tests, and visible desktop responsiveness review

**Target Platform**: Desktop OpenGL/Skia viewer host in a visible Linux desktop session;
headless deterministic paths remain substitute evidence only

**Project Type**: F# UI framework libraries plus a package-consuming desktop sample and
sample CLI evidence commands

**Performance Goals**: At least 95% of measured representative pointer actions show first
visible response within 100 ms; no accepted pointer action exceeds 150 ms; value-changing
drags visibly track pointer movement without delayed catch-up

**Constraints**: Tier 1 observable behavior change; prefer no breaking public API; any
public surface change must be additive or explicitly justified and reflected in `.fsi` and
surface baselines; no new runtime dependency is planned; accepted evidence must come from a
visible desktop session; unavailable or unreliable live timing must report blocked or
environment-limited, not accepted; visual redesign is out of scope except preserving prior
opaque background, navigation, slider, and coverage fixes

**Scale/Scope**: `samples/SecondAntShowcase` with 19 pages, 96 mapped controls, every
interactive control family in `InteractionContracts.all`, and explicit display-only
exclusions

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Design Gate

- **I. Spec -> FSI -> Semantic Tests -> Implementation**: PASS. The feature is specified and
  this plan identifies the public/review contract. Implementation tasks must draft any
  public `.fsi` changes before `.fs` bodies and add semantic tests before the fix.
- **II. Visibility Lives in `.fsi`, Not in `.fs`**: PASS. Existing public modules already use
  `.fsi`; any new public sample/framework surface must update the matching `.fsi` and avoid
  top-level visibility modifiers in `.fs`.
- **III. Idiomatic Simplicity Is the Default**: PASS. The planned fix uses the existing
  retained-routing, input queue, and responsiveness record abstractions. Hot-path mutation is
  acceptable only at the viewer/interpreter edge with a short reason comment.
- **IV. Elmish/MVU Is the Boundary for Stateful or I/O Workflows**: PASS. Product state stays
  in the SecondAntShowcase `Model`/`Msg`/`update` boundary; pointer and evidence I/O remain
  at `ControlsElmish`/`SkiaViewer`/sample CLI edges.
- **V. Test Evidence Is Mandatory**: PASS with explicit live-evidence requirement. Headless
  deterministic scripts may guard behavior and record substitute facts, but they must remain
  disclosed as substitute evidence and cannot satisfy acceptance for live responsiveness.
- **VI. Observability and Safe Failure**: PASS. The plan requires structured latency records,
  environment diagnostics, and fail-closed readiness when the visible session or timing
  boundary is unavailable.
- **Change Classification**: Tier 1. Observable interaction behavior and evidence contracts
  are affected. Preferred public API outcome is additive CLI/evidence surface only; any
  framework `.fsi` change requires baseline and compatibility review.

## Project Structure

### Documentation (this feature)

```text
specs/172-fix-mouse-lag/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── Controls/
│   ├── Controls.fsi
│   └── Controls.fs
├── Controls.Elmish/
│   ├── ControlsElmish.fsi
│   └── ControlsElmish.fs
└── SkiaViewer/
    ├── SkiaViewer.fsi
    ├── SkiaViewer.fs
    └── Host/

tests/
├── Controls.Tests/
├── Elmish.Tests/
└── SkiaViewer.Tests/

samples/SecondAntShowcase/
├── SecondAntShowcase.Core/
│   ├── InteractionContracts.fsi
│   ├── InteractionContracts.fs
│   ├── Evidence.fsi
│   └── Evidence.fs
├── SecondAntShowcase.App/
│   ├── Program.fs
│   ├── Interactive.fs
│   └── Responsiveness.fs
└── SecondAntShowcase.Tests/
```

**Structure Decision**: Use the existing framework and sample projects. No new project is
planned. Framework changes, if needed, stay in `Controls.Elmish` and `SkiaViewer`; sample
evidence orchestration stays in `samples/SecondAntShowcase`.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations are planned.

## Post-Design Constitution Check

- **I. Spec -> FSI -> Semantic Tests -> Implementation**: PASS. Design artifacts define the
  evidence contract and validation sequence before implementation tasks.
- **II. Visibility Lives in `.fsi`, Not in `.fs`**: PASS. The contract prefers sample-layer
  additive evidence output; any framework API addition remains subject to `.fsi` and baseline
  updates.
- **III. Idiomatic Simplicity Is the Default**: PASS. Research selects existing queue,
  retained routing, and summary helpers over new abstractions or dependencies.
- **IV. Elmish/MVU Is the Boundary for Stateful or I/O Workflows**: PASS. Data model keeps
  product state pure and evidence/session I/O at the app/viewer edge.
- **V. Test Evidence Is Mandatory**: PASS. Quickstart requires automated tests plus accepted
  visible-session evidence; substitute evidence is explicitly non-accepting.
- **VI. Observability and Safe Failure**: PASS. Contracts require measured records,
  environment diagnostics, and blocked/environment-limited output when live evidence is not
  reliable.

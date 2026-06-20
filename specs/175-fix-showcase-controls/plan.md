# Implementation Plan: Fix Non-Functional Controls in the Second Ant Showcase

**Branch**: `175-fix-showcase-controls` | **Date**: 2026-06-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/175-fix-showcase-controls/spec.md`

## Summary

Make the live, hand-driven experience of the `SecondAntShowcase` sample match its
interaction contracts. Three observable defects drive the work: (1) the content-region
scrollbar does not scroll under drag/wheel/keyboard and its thumb does not track offset;
(2) interactive controls show no hover or focus affordance under real pointer/keyboard
input; (3) several interactive controls do not visibly change when activated by hand even
though they pass scripted coverage. The corrective pass classifies **every** control as
responsive-interactive or intentionally display-only, records each finding with its fix and
re-verification, and confirms no control is left unclassified or unresponsive.

The root causes reach the shared `FS.GG.UI.*` control surface, not only sample wiring:

- **Scroll** — the viewer already delivers wheel events (`SkiaViewer.fs:2325` →
  `ViewerPointerPhaseKind.Wheel`) and `Pointer.update` emits `Scroll(control, dx, dy, x, y)`
  (`src/Controls/Pointer.fs:242`), but no scroll-offset state is owned by the `scroll-viewer`
  control, content is not translated/clipped by an offset, and `Control.scrollAffordance`
  (`src/Controls/Control.fs:1501`) paints a thumb pinned at the top. The `Scroll` interaction
  is produced but never consumed into a visible content offset + thumb position, and
  hit-testing inside the region does not subtract the offset.
- **Hover/focus** — the infrastructure exists (`HoverChanged`/`FocusChanged` →
  `DispatchControlRuntimeMessage(HoverControl/FocusControl)` at `ControlsElmish.fs:210-215`,
  stamped into a derived `VisualState` by `ControlRuntime.applyRuntimeVisualState` and painted
  by each `*Geom` in `Control.fs`), but the live path does not produce visible feedback for the
  showcase's controls. Phase 0 confirms the exact break (hover repaint trigger, runtime
  visual-state stamping coverage per kind, and the `ghost` nav-button style path).
- **Per-control activation** — a subset of controls respond to scripted `Model.update` but not
  to real input; Phase 0 produces the per-control root-cause map, and most fixes are shared
  (retained pointer routing, offset-aware hit-testing, focus traversal) with a minority being
  sample wiring (e.g. an unbound `OnChanged`).

Because the corrections add or change observable behavior of shared controls (scroll-offset
state, hover/focus repaint, offset-aware hit-testing), this is a **Tier 1** change: `.fsi`
updates, surface-area baseline updates, and test evidence are required for every public-surface
touch. Sample-local fixes that touch no shared control surface remain Tier 2 within the same
feature and are recorded as such in the finding log.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`LangVersion=latest`). Public package surfaces are
declared in `.fsi`; any public-surface delta is Tier 1 and updates the matching surface baseline.

**Primary Dependencies**: Existing `FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`,
`FS.GG.UI.SkiaViewer`, `FS.GG.UI.Themes.AntDesign`, `FS.GG.UI.Testing`, `Fable.Elmish`,
`Silk.NET.Input/Windowing/OpenGL`, `SkiaSharp`, `System.Text.Json`, and Expecto/YoloDev for
tests. No new dependency is planned; if one becomes necessary it states need, version-pinning,
and owner per the constitution.

**Storage**: Filesystem evidence only. Feature evidence is written under
`specs/175-fix-showcase-controls/readiness/` (control-pass finding log, coverage classification,
live-responsiveness summaries, visual review sets for both appearances, validation summary).
Runtime code introduces no persistent storage.

**Testing**: Expecto through `dotnet test`. Coverage spans `tests/Controls.Tests` (scroll-offset
state transitions, offset-aware hit-testing, hover/focus visual-state stamping, parity),
`tests/Elmish.Tests` (hover/focus/scroll routing and retained repaint), `tests/SkiaViewer.Tests`
(wheel/scroll delivery), and `samples/SecondAntShowcase/SecondAntShowcase.Tests` (per-control
live-vs-scripted parity, coverage classification, visual parity in light/dark). Visible desktop
validation runs the sample CLI with the live-responsiveness runner; headless lanes report
explicit `environment-limited` results.

**Target Platform**: Desktop OpenGL/Skia viewer host in a visible, focusable Linux desktop
session for accepted live-interaction and visual evidence; deterministic/headless test paths for
regression shape and environment-limited reporting.

**Project Type**: Multi-package F# rendering/UI framework plus a package-consuming desktop sample
and sample CLI evidence commands.

**Performance Goals**: No regression to the Feature 174 responsiveness budgets (button-activation
follow-up frame median <= 150 ms / p95 <= 250 ms; page navigation median <= 250 ms / p95 <=
500 ms). Scroll, hover, and focus feedback must appear within the same live-responsiveness
target as button activation so the controls do not feel dead. Scroll, hover, and focus repaints
remain damage-local — they MUST NOT reintroduce full-tree frame preparation.

**Constraints**: Tier 1 contracted change to shared control behavior. Every public-surface touch
updates `.fsi` and the matching surface baseline in the same change; no control is forked per
theme (one semantic control set, hover/focus/scroll behavior shared across antLight/antDark).
The fixes MUST NOT remove any control, page, or existing passing behavior (FR-014). Live
evidence requires a real presentation boundary; unsupported hosts may only produce classified
limitations, and any synthetic substitute is disclosed at the use site and in the PR.

**Scale/Scope**: The current catalog — 13 family/catalog pages plus 6 template pages (19 pages),
~96 interactive controls across 13 interaction families, and ~30 display-only controls, all in
`SecondAntShowcase.Core`. The shared-surface scope is the `scroll-viewer` control, the pointer
hover/scroll state machine, retained pointer/focus routing, offset-aware hit-testing, and
paint-time visual-state resolution. Both Ant light and Ant dark appearances and the accepted
review sizes (including the minimum) are the inspection baseline.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists; planning confirms the work reaches shared control behavior and classifies it **Tier 1** (the spec's downgrade-to-Tier-2 condition does not hold — scroll-offset state, hover/focus repaint, and offset-aware hit-testing are shared-surface changes). Sample-local fixes are recorded as Tier 2 within the finding log. |
| Spec → FSI → semantic tests → implementation | PASS | Each shared-surface change drafts its `.fsi` delta first (e.g. `scroll-viewer` offset state, any new hit-test/visual-state seam), is exercised through the packed/prelude FSI surface by a failing semantic test, then implemented in `.fs`. |
| Visibility lives in `.fsi` | PASS | Public modules stay governed by their `.fsi`; no `private`/`internal`/`public` modifiers are added to `.fs`. Surface-area baselines are updated for every public delta and validated by the existing surface-drift checks. |
| Idiomatic simplicity | PASS | Design reuses existing records (`PointerState`, `RetainedRender`, `VisualState`, `FrameMetrics`), the `Scroll` interaction already emitted by `Pointer.update`, and existing scene writers. Mutation is confined to disclosed hot-path render/accumulator sites. No new operators, SRTP, reflection, or type providers are planned. |
| Elmish/MVU boundary | PASS | Scroll offset and interaction state are modeled as data: scroll input becomes a `Msg`/interaction handled in a pure `update`/runtime transition, and IO (wheel delivery, repaint) stays at the viewer/runtime edge. Sample state continues through `SecondAntShowcase.Core.Model`. |
| Test evidence | PASS | Failing-first semantic tests cover scroll-offset transitions, clamp-at-bounds, thumb tracking, offset-aware hit-testing, hover/focus stamping, and live-vs-scripted parity; visible desktop runs provide accepted live evidence or explicit environment-limited results. |
| Observability and safe failure | PASS | Existing `FrameMetrics`, live-responsiveness summaries, and environment-limited classifications remain the evidence path; scroll/hover/focus changes preserve damage-local frame preparation and phase attribution. |
| Tier 1 obligations | PASS | Every public-surface change ships `.fsi` + surface-baseline updates + tests in the same change; the finding log records the tier of each fix and ties it to its re-verification. |

No constitution violations require justification. The Complexity Tracking table is empty.

## Project Structure

### Documentation (this feature)

```text
specs/175-fix-showcase-controls/
├── plan.md                      # This file (/speckit-plan output)
├── research.md                  # Phase 0 output — per-symptom root-cause map + decisions
├── data-model.md                # Phase 1 output — entities, validation, state transitions
├── quickstart.md                # Phase 1 output — validation/run guide
├── contracts/                   # Phase 1 output
│   ├── scroll-interaction.md            # scroll offset, clamp, thumb tracking, offset hit-test
│   ├── interaction-state.md             # hover/focus/active resolution + repaint + overlays
│   └── control-pass-coverage.md         # finding log + classification + live-vs-scripted parity
├── feedback/                    # per-phase fs-gg-feedback-capture notes
└── readiness/                   # evidence artifacts
    ├── finding-log.md
    ├── coverage-classification.md
    ├── responsiveness/
    ├── visual-parity/
    └── validation-summary.md
```

### Source Code (repository root)

```text
src/
├── Controls/
│   ├── Pointer.fs / Pointer.fsi               # Scroll/Hover state machine (Scroll already emitted)
│   ├── Control.fs / Control.fsi               # scrollAffordance + scrollViewerGeom, *Geom VisualState paint
│   ├── Widgets/Containers.fs / Containers.fsi # ScrollViewer control (offset state + OnChanged)
│   ├── ControlRuntime.fs / ControlRuntime.fsi # applyRuntimeVisualState (hover/focus stamping)
│   └── Layout / hit-testing seam              # hitTestComputed — offset-aware inside scroll region
├── Controls.Elmish/
│   └── ControlsElmish.fs / ControlsElmish.fsi # routeInteractivePointer, Scroll/Hover/Focus interpret, retained repaint
└── SkiaViewer/
    └── SkiaViewer.fs / SkiaViewer.fsi         # Wheel/PointerScrolled delivery (already present)

tests/
├── Controls.Tests/        # scroll-offset transitions, clamp, thumb tracking, offset hit-test, visual-state stamping
├── Elmish.Tests/          # hover/focus/scroll routing + retained repaint parity
└── SkiaViewer.Tests/      # wheel/scroll delivery regression

samples/SecondAntShowcase/
├── SecondAntShowcase.Core/    # Shell.fs (content ScrollViewer), InteractionContracts.fs, Model.fs
├── SecondAntShowcase.App/     # Interactive.fs (live host), Responsiveness.fs evidence CLI
└── SecondAntShowcase.Tests/   # per-control live-vs-scripted parity, coverage classification, visual parity
```

**Structure Decision**: Keep behavior fixes in the framework-owned shared controls
(`scroll-viewer` offset state, hover/focus visual-state stamping, offset-aware hit-testing,
retained repaint) so all showcases and products inherit them — no control is forked per theme or
per sample. Keep sample-specific wiring (binding the content `ScrollViewer`'s scroll affordance,
any unbound `OnChanged`, page-level finding records) in `samples/SecondAntShowcase`. No new
project, package, or dependency is planned.

## Phase 0 Research

See [research.md](./research.md). Planning unknowns to resolve:

- **Scroll ownership**: where scroll offset lives (control runtime state keyed by the
  `scroll-viewer` `ControlId`) and how `Scroll` interactions + thumb-drag + keyboard scroll
  update it, clamped to `[0, contentHeight - viewportHeight]`, with the no-overflow case
  presenting no draggable thumb (FR-001/FR-002).
- **Offset-aware hit-testing**: how `Layout.hitTestComputed` (or a wrapping seam) subtracts the
  region's current scroll offset so hover/focus/activation map to the correct control after
  scrolling (FR-009).
- **Hover/focus break**: the exact reason live hover/focus feedback is absent despite existing
  `HoverChanged`/`FocusChanged` → `VisualState` infrastructure — repaint trigger, per-kind
  `applyRuntimeVisualState` coverage, and the `ghost` nav-button path (FR-003/FR-004/FR-005).
- **Per-control root-cause map**: for each of the 13 interaction families and the controls that
  fail under real input, the cause and whether the fix is shared (Tier 1) or sample-local
  (Tier 2), including overlay open/dismiss + focus return (FR-013).
- **Display-only confirmation**: confirm the ~30 display-only controls stay static and present
  no interactive affordance, consistent with their recorded reasons (FR-008).
- **Evidence path**: reuse the existing live-responsiveness runner and visual-readiness paths;
  define the headless `environment-limited` substitute and its disclosure (per Principle V).

Output: research.md with each unknown resolved as Decision / Rationale / Alternatives.

## Phase 1 Design and Contracts

See [data-model.md](./data-model.md) for entities (Control, Interaction contract, Interaction
state, Scroll state, Finding), their validation rules, and state transitions (scroll
offset clamp; hover/focus/active transitions; finding open → fixed → re-verified).

Observable and internal contracts:

- [Scroll Interaction](contracts/scroll-interaction.md) — offset state, drag/wheel/keyboard,
  clamp-at-bounds, thumb tracking, no-overflow affordance, offset-aware hit-testing.
- [Interaction State](contracts/interaction-state.md) — hover/focus/active/combined resolution,
  damage-local repaint, palette roles per appearance, overlay open/dismiss/focus-return.
- [Control-Pass Coverage](contracts/control-pass-coverage.md) — finding log schema, classification
  (interactive vs display-only, none unclassified), and live-vs-scripted parity bar.

Validation guide: [quickstart.md](./quickstart.md).

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Contracts keep the work Tier 1 with explicit per-fix tier recording; reclassification of a fix to Tier 2 is allowed only when it touches no shared control surface. |
| Spec → FSI → semantic tests → implementation | PASS | Each contract names the `.fsi` seam it changes and the failing semantic test that drives it before `.fs` implementation. |
| Visibility lives in `.fsi` | PASS | Surface deltas are confined to listed `.fsi` files with matching baseline updates; no access modifiers in `.fs`. |
| Idiomatic simplicity | PASS | Design reuses existing records, the already-emitted `Scroll` interaction, and existing scene writers; mutation only at disclosed render hot paths. |
| Elmish/MVU boundary | PASS | Scroll/interaction state is data transformed by pure transitions; IO stays at the viewer/runtime edge. |
| Test evidence | PASS | Quickstart requires failing-first deterministic tests plus visible desktop live + visual evidence, or explicit environment limitation. |
| Observability and safe failure | PASS | Repaints stay damage-local and phase-attributed; environment-limited results are reported fail-closed. |
| Tier 1 obligations | PASS | Every public delta ships `.fsi` + baseline + tests together; the finding log binds each fix to its tier and re-verification. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.

# Implementation Plan: Interaction Overlay State (Feature 143)

**Branch**: `143-interaction-overlay-state` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/143-interaction-overlay-state/spec.md`

## Summary

Feature 143 starts P5/R4 from the radical rendering roadmap: real interaction and overlay state for
transient UI surfaces. The core outcome is a deterministic overlay state model that can open, dismiss,
focus, layer, and route pointer/keyboard input for menus, context menus, split-button menus, combo-style
dropdowns, auto-complete suggestions, date-picker calendars, color-picker palettes, and dialog-like modal
overlays.

The technical approach is additive and compatibility-preserving. Keep product-owned open/value state
compatible, but add a runtime overlay coordinator that emits explicit state-change, dismissal, selection,
focus, and diagnostic evidence rather than silently mutating product state. Reuse the existing Feature 140
overlay/layer foundation for painting and hit-test order, the Feature 141 retained/direct parity foundation
for equivalent frames, and the existing `ControlRuntime`, `Focus`, `Pointer`, and Controls.Elmish routing
seams for pure, replayable interaction transitions. The AntShowcase date-picker flow is the reference
end-to-end consumer.

## Technical Context

**Language/Version**: F# on .NET `net10.0`, `LangVersion=latest`, warnings-as-errors.

**Primary Dependencies**: Existing in-repo packages: `FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`,
`FS.GG.UI.KeyboardInput`, `FS.GG.UI.Scene`, `FS.GG.UI.Layout`, `FS.GG.UI.SkiaViewer`, and
`FS.GG.UI.Testing`. Runtime pins remain unchanged. No new runtime dependency is planned.

**Storage**: N/A for persistence. Runtime state is in-memory overlay state, focus state, pointer/key routing
state, retained frame evidence, diagnostics, and deterministic interaction replay logs.

**Testing**: Expecto plus existing property/determinism coverage. Focused tests land primarily in
`tests/Controls.Tests`, with host/routing evidence in `tests/Elmish.Tests`, keyboard evidence in
`tests/KeyboardInput.Tests` if the normalized key contract changes, retained/cache parity in existing audit
suites, and offscreen/pixel evidence through `tests/Rendering.Harness` when GL/presentation support is
available.

**Target Platform**: Linux/dev and CI for deterministic headless tests. GL/offscreen visual evidence remains
environment-sensitive and must disclose unsupported host conditions rather than silently passing.

**Project Type**: F# UI framework/library with declarative Controls, retained rendering, dependency-light
Scene primitives, Yoga-backed layout, Elmish integration, keyboard/pointer routing, and an OpenGL-backed
Skia viewer host.

**Performance Goals**: Overlay routing must stay deterministic and bounded by the number of open surfaces plus
the normal hit-test/focus order for the active frame. Warm retained frames with unchanged overlay state should
preserve existing cache/reuse behavior; overlay interactions must not introduce duplicate product dispatches,
stale hit targets, or unnecessary full-render fallbacks beyond documented compatibility cases.

**Constraints**:
- Tier 1 contracted interaction/runtime feature. Public API, diagnostics, focus behavior, hit-test behavior,
  rendering evidence, or baseline changes require `.fsi`-first design, semantic tests, compatibility notes,
  surface-baseline evidence, and versioning rationale.
- Stateful interaction behavior uses an MVU boundary: overlay model, overlay messages, pure update, effects,
  and host interpreter/routing at the edge.
- Existing product-owned `IsOpen`/value patterns remain compatible. The runtime emits explicit messages and
  evidence; it must not silently take ownership of product state.
- Closed transient controls preserve existing visible output, hit-test behavior, diagnostics, and authoring
  behavior unless an intentional compatibility change is documented.
- Topmost overlay routing consumes eligible Escape, outside pointer, selection, and close actions before lower
  content sees them. Lower content may receive the event only when the dismissal policy explicitly permits it.
- Modal overlays trap focus and block covered pointer/key interaction until dismissed or completed by policy.
- Missing anchors, stale focus targets, blocked dismissals, disabled triggers, and no-fit placements fail safely
  with diagnostics and without stale visible or hit-testable content.
- Scope excludes portable scene serialization, browser rendering, compositor promotion, damage-scissored
  presentation, intrinsic layout, new text shaping behavior, text editing, selection editing, and a complete
  widget catalog redesign.

**Scale/Scope**: One interaction slice across:

```text
src/Controls/
|-- OverlayState.fsi / OverlayState.fs          # planned pure overlay model/update contract
|-- ControlRuntime.fsi / ControlRuntime.fs      # integration effects, diagnostics, focus recovery
|-- Focus.fsi / Focus.fs                        # focus scopes, modal traversal, stale-target recovery
|-- Pointer.fsi / Pointer.fs                    # topmost hit/outside-dismiss routing evidence
|-- Control.fsi / Control.fs                    # transient surface metadata, closed compatibility, dispatch
|-- Composition.fsi / Composition.fs            # Feature 140 layer/portal evidence consumed by overlays
|-- DataEntry2.fsi / DataEntry2.fs              # auto-complete and related data-entry surface metadata
|-- Interactive2.fsi / Interactive2.fs          # calendar/date-picker adjacent surface metadata
`-- Typed* / widget modules as needed           # split button, date picker, color picker, dialog reference paths

src/Controls.Elmish/
`-- ControlsElmish.fsi / ControlsElmish.fs      # retained/direct pointer and key routing through overlay state

src/KeyboardInput/
`-- KeyboardInput.fsi / KeyboardInput.fs        # only if normalized key evidence needs an additive contract

tests/Controls.Tests/
|-- Feature143InteractionOverlayStateTests.fs
|-- Feature143OverlayFocusTests.fs
|-- Feature143OverlayHitTestTests.fs
|-- Feature143ReferenceDatePickerTests.fs
|-- Feature140*Tests.fs / Feature141*Tests.fs / Feature142*Tests.fs
`-- PublicSurfaceTests.fs

tests/Elmish.Tests/
`-- Feature143OverlayRoutingTests.fs

tests/Rendering.Harness/
`-- overlay interaction replay, parity, and visual evidence when host support is available
```

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1 (contracted interaction/runtime change)**. This feature changes observable
control behavior, focus routing, pointer routing, keyboard routing, runtime state ownership, diagnostics,
paint/hit order evidence, and likely public package contracts.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec -> FSI -> Semantic Tests -> Implementation | PASS | The spec defines Tier 1 outcomes, scope exclusions, supported surfaces, compatibility requirements, and verification. Any public/cross-file overlay state surface must be drafted in `.fsi`, exercised through semantic tests, then implemented. |
| II. Visibility lives in `.fsi` | PASS | New or reshaped overlay, runtime, focus, pointer, and routing contracts require paired curated `.fsi` files. No top-level visibility keywords in paired `.fs` files are planned. |
| III. Idiomatic simplicity | PASS | The planned design uses records, closed DUs, pure update functions, deterministic lists/maps, and explicit evidence records. No SRTP, reflection, custom operators, type providers, or non-trivial computation expressions are planned. |
| IV. Elmish/MVU boundary | PASS | Overlay interaction is stateful user workflow, so it is modeled as `OverlayModel`/`OverlayMsg`/`OverlayEffect` plus a pure `update` and host interpreter/routing edge. |
| V. Test evidence mandatory | PASS | Focused surface coverage, keyboard navigation, modal trapping, topmost hit/dismissal, direct/retained/cache parity, byte-identical replay logs, surface baselines, and readiness limitations are required. |
| VI. Observability and safe failure | PASS | Anchor loss, stale focus, no-fit placement, blocked dismissal, disabled triggers, duplicate dispatch prevention, and verification limitations must emit diagnostics or readiness evidence. |

**Gate result**: PASS. No unresolved clarification markers remain.

**Post-design re-check**: PASS. Phase 0/1 artifacts define an MVU overlay state boundary, keep product-owned
visibility compatible, preserve closed-state output by default, and keep serialization, compositor, intrinsic
layout, text editing, and widget-catalog redesign out of scope.

## Project Structure

### Documentation (this feature)

```text
specs/143-interaction-overlay-state/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- interaction-overlay-state.md
`-- tasks.md                         # Created by /speckit-tasks, not by /speckit-plan
```

### Source Code (repository root)

```text
src/Controls/OverlayState.fsi / OverlayState.fs       # new pure overlay state model/update if needed
src/Controls/ControlRuntime.fsi / ControlRuntime.fs   # overlay effects, focus recovery, diagnostics
src/Controls/Focus.fsi / Focus.fs                     # scoped traversal and modal trap helpers
src/Controls/Pointer.fsi / Pointer.fs                 # overlay-aware hit/outside routing helpers
src/Controls/Control.fsi / Control.fs                 # surface metadata, dispatch, closed-state guards
src/Controls/Composition.fsi / Composition.fs         # layer/portal evidence and anchor metadata
src/Controls/DataEntry2.fsi / DataEntry2.fs           # auto-complete and combo metadata
src/Controls/Interactive2.fsi / Interactive2.fs       # calendar/date-picker interaction metadata
src/Controls.Elmish/ControlsElmish.fsi / .fs          # host routing through overlay state
src/KeyboardInput/KeyboardInput.fsi / .fs             # only for additive normalized key evidence

tests/Controls.Tests/Feature143InteractionOverlayStateTests.fs
tests/Controls.Tests/Feature143OverlayFocusTests.fs
tests/Controls.Tests/Feature143OverlayHitTestTests.fs
tests/Controls.Tests/Feature143ReferenceDatePickerTests.fs
tests/Elmish.Tests/Feature143OverlayRoutingTests.fs
tests/KeyboardInput.Tests/                            # only if keyboard public surface changes
tests/Rendering.Harness/                              # interaction replay and visual parity evidence
readiness/ or specs/143-interaction-overlay-state/readiness/
                                                        # surface, baseline, replay, limitation evidence
```

**Structure Decision**: Single F# solution. Keep overlay coordination in Controls as pure model/update data,
with Controls.Elmish interpreting effects and threading product messages. Reuse existing `Focus`, `Pointer`,
`ControlRuntime`, retained hit-testing, and Feature 140 layer/portal behavior. Add a new module only if it keeps
overlay state coherent; otherwise extend the existing runtime modules with `.fsi`-first additive contracts.

## Phase 0: Research Summary

See [research.md](./research.md). Decisions are resolved and no clarification markers remain.

## Phase 1: Design Summary

See [data-model.md](./data-model.md), [contracts/interaction-overlay-state.md](./contracts/interaction-overlay-state.md),
and [quickstart.md](./quickstart.md). The contract centers on overlay state transitions, dismissal policy,
focus scope, topmost routing, product dispatch compatibility, diagnostics, and deterministic replay evidence.

## Complexity Tracking

No constitution violations require justification.

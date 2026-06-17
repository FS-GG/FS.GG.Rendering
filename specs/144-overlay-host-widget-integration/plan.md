# Implementation Plan: Overlay Host Widget Integration

**Branch**: `144-overlay-host-widget-integration` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/144-overlay-host-widget-integration/spec.md`

## Summary

Feature 144 completes the remaining P5/R4 host and widget integration after Feature 143. Feature 143
delivered the pure `OverlayState` coordinator, overlay diagnostics, replay evidence, and the initial
ControlRuntime bridge. This feature turns that coordinator into live transient-surface behavior by adding
metadata to supported widgets, routing pointer/keyboard/focus through overlay decisions, preserving explicit
product-owned visibility, and making AntShowcase's date picker the reference end-to-end flow.

The technical approach is additive and bounded. Existing `OverlayState`, diagnostics, Feature 140 layer/portal
ordering, retained/direct rendering parity, `Pointer`, `Focus`, `ControlRuntime`, and Controls.Elmish routing
seams remain the foundation. New work must expose or derive complete transient metadata for the eight supported
surface categories, interpret overlay effects in the host without silently mutating product state, record
deterministic interaction evidence, and provide real offscreen visual proof when the host environment supports it.

## Technical Context

**Language/Version**: F# on .NET `net10.0`, `LangVersion=latest`, warnings-as-errors.

**Primary Dependencies**: Existing in-repo packages only: `FS.GG.UI.Controls`,
`FS.GG.UI.Controls.Elmish`, `FS.GG.UI.KeyboardInput`, `FS.GG.UI.Scene`, `FS.GG.UI.Layout`,
`FS.GG.UI.SkiaViewer`, `FS.GG.UI.Testing`, and the `samples/AntShowcase` projects. No new runtime
dependency is planned.

**Storage**: N/A for persistence. Runtime state remains in memory: overlay state, product-owned open/value
state, pointer state, focus state, retained frame state, diagnostics, and deterministic replay/evidence records.

**Testing**: Expecto test projects plus existing surface-baseline and rendering-harness validation. Focused
coverage belongs in `tests/Controls.Tests`, `tests/Elmish.Tests`, `tests/KeyboardInput.Tests` when normalized
key evidence changes, `tests/Rendering.Harness.Tests`, and `samples/AntShowcase/AntShowcase.Tests`.

**Target Platform**: Linux/dev and CI for deterministic headless tests. OpenGL/offscreen visual proof is
environment-sensitive and must record unsupported-host limitations instead of treating missing host support as a
pass.

**Project Type**: F# UI framework/library with declarative controls, retained rendering, dependency-light scene
primitives, Yoga-backed layout, Elmish integration, keyboard/pointer routing, and an OpenGL-backed Skia viewer
host.

**Performance Goals**: Overlay routing must be deterministic and bounded by open-surface count plus current-frame
hit/focus traversal. Equivalent overlay scripts must remain byte-stable across direct rendering, first retained
frame, warm retained frame, cache-enabled mode, and cache-disabled mode. Unchanged overlay state should preserve
retained reuse and cache behavior.

**Constraints**:
- Tier 1 contracted interaction/package change. Observable control behavior, public API, diagnostics, surface
  baselines, host routing, or compatibility changes require `.fsi`-first design, semantic tests, migration notes,
  surface-baseline evidence, and versioning rationale.
- Stateful interaction uses an MVU boundary: product model owns visibility/value state, `OverlayState.update` is
  pure, and host/runtime interpreters emit explicit product-visible requests and effects.
- Closed controls must preserve existing visible output, hit-test behavior, diagnostics, and authoring behavior
  unless an intentional compatibility change is documented.
- Topmost overlay routing consumes eligible Escape, outside pointer, modal blocking, selection, and close actions
  before lower content sees them. Lower content receives the original input only when policy allows pass-through.
- Missing anchors, stale focus targets, disabled triggers, blocked dismissals, no-fit placement, and duplicate
  dispatch attempts fail safely with diagnostics or readiness failures.
- Scope excludes portable scene serialization, browser rendering, compositor promotion, damage-scissored
  presentation, intrinsic layout, new text shaping behavior, text editing, selection editing, and widget-catalog
  redesign.

**Scale/Scope**:

```text
src/Controls/
|-- Control.fsi / Control.fs                  # transient metadata and product-owned dispatch mapping
|-- ControlRuntime.fsi / ControlRuntime.fs    # overlay effect interpretation bridge
|-- Pointer.fsi / Pointer.fs                  # topmost hit, outside-dismiss, modal blocking evidence
|-- Focus.fsi / Focus.fs                      # focus scopes, modal traversal, stale-target recovery
|-- Composition.fsi / Composition.fs          # Feature140 layer/portal anchor evidence helpers
|-- DataEntry2.fsi / DataEntry2.fs            # auto-complete metadata
|-- Widgets/CollectionsWidgets.fsi / .fs      # combo dropdown metadata
|-- Widgets/Pickers.fsi / Pickers.fs          # date-picker and color-picker metadata
`-- Widgets/*.fsi / *.fs                      # menu, context-menu, split-button, dialog metadata

src/Controls.Elmish/ControlsElmish.fsi / .fs  # route pointer/key/focus through overlay state
src/KeyboardInput/KeyboardInput.fsi / .fs      # only for additive key evidence changes
src/SkiaViewer/ and tests/Rendering.Harness*   # visual proof and unsupported-host disclosure

samples/AntShowcase/
|-- AntShowcase.Core/Scripts.fs
|-- AntShowcase.Core/Pages.fs
|-- AntShowcase.Core/DemoState.fs
|-- AntShowcase.Core/Evidence.fs
|-- AntShowcase.App/Evidence.fs
`-- AntShowcase.Tests/Feature143DatePickerFlowTests.fs or Feature144 successor tests
```

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1 (contracted change)**. This feature changes observable transient-control
behavior, input routing, focus routing, product message dispatch, compatibility guidance, validation evidence,
and potentially public package contracts.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec -> FSI -> Semantic Tests -> Implementation | PASS | The spec defines Tier 1 outcomes, scope exclusions, supported surfaces, compatibility requirements, and validation. Public or cross-module contracts must be drafted in `.fsi` before implementation. |
| II. Visibility lives in `.fsi` | PASS | New/reshaped metadata, runtime, focus, pointer, and host-routing contracts require paired curated `.fsi` files. No top-level visibility keywords in paired `.fs` files are planned. |
| III. Idiomatic simplicity | PASS | The design extends existing records, discriminated unions, pure reducers, and deterministic evidence records. No SRTP, reflection, custom operators, type providers, or non-trivial computation expressions are planned. |
| IV. Elmish/MVU boundary | PASS | Product-owned visibility/value state remains in product models. Overlay interaction uses `OverlayState`/`OverlayMsg`/`OverlayEffect`, pure update, and host/runtime interpretation at the edge. |
| V. Test evidence mandatory | PASS | The plan requires metadata coverage for all eight surface categories, exactly-once dispatch tests, focus/modal routing tests, direct/retained/cache parity, deterministic replay logs, AntShowcase reference evidence, visual proof or disclosed host limitation, and surface baselines. |
| VI. Observability and safe failure | PASS | Disabled triggers, missing anchors, stale focus targets, blocked dismissals, no-fit placements, duplicate dispatch, lower-layer blocking, and unsupported visual hosts are reported through diagnostics or readiness evidence. |

**Gate result**: PASS. No unresolved clarification markers remain.

**Post-design re-check**: PASS. Phase 0/1 artifacts keep the work bounded to P5 integration, reuse the existing
MVU coordinator, preserve product-owned state, and explicitly exclude P6/render-anywhere, compositor, intrinsic
layout, text, editing, and catalog-redesign work.

## Project Structure

### Documentation (this feature)

```text
specs/144-overlay-host-widget-integration/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- overlay-host-widget-integration.md
`-- tasks.md                         # Created by /speckit-tasks, not by /speckit-plan
```

### Source Code (repository root)

```text
src/Controls/Control.fsi / Control.fs
src/Controls/ControlRuntime.fsi / ControlRuntime.fs
src/Controls/Pointer.fsi / Pointer.fs
src/Controls/Focus.fsi / Focus.fs
src/Controls/Composition.fsi / Composition.fs
src/Controls/DataEntry2.fsi / DataEntry2.fs
src/Controls/Widgets/CollectionsWidgets.fsi / CollectionsWidgets.fs
src/Controls/Widgets/Navigation.fsi / Navigation.fs
src/Controls/Widgets/Buttons.fsi / Buttons.fs
src/Controls/Widgets/Overlay.fsi / Overlay.fs
src/Controls/Widgets/Pickers.fsi / Pickers.fs
src/Controls.Elmish/ControlsElmish.fsi / ControlsElmish.fs
src/KeyboardInput/KeyboardInput.fsi / KeyboardInput.fs       # only if public key evidence changes

tests/Controls.Tests/Feature144*Tests.fs
tests/Elmish.Tests/Feature144*Tests.fs
tests/KeyboardInput.Tests/Feature144*Tests.fs                # only if keyboard contract changes
tests/Rendering.Harness.Tests/Feature144*Tests.fs
samples/AntShowcase/AntShowcase.Tests/Feature144*Tests.fs
specs/144-overlay-host-widget-integration/readiness/
```

**Structure Decision**: Single F# solution. Keep pure overlay state in `src/Controls/OverlayState.*` as delivered
by Feature 143. Feature 144 extends widget metadata and host routing around that contract rather than replacing
it. Controls.Elmish interprets overlay effects and dispatches product messages; product code remains the owner of
open/selected state.

## Phase 0: Research Summary

See [research.md](./research.md). Decisions are resolved and no clarification markers remain.

## Phase 1: Design Summary

See [data-model.md](./data-model.md), [contracts/overlay-host-widget-integration.md](./contracts/overlay-host-widget-integration.md),
and [quickstart.md](./quickstart.md). The contract centers on widget-supplied transient metadata, topmost
pointer/key/focus routing, product-visible state-change and selection requests, exactly-once dispatch,
AntShowcase reference evidence, and readiness artifacts.

## Complexity Tracking

No constitution violations require justification.

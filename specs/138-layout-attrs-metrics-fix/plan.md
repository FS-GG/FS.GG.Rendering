# Implementation Plan: Layout Attributes and Metrics Green (Feature 138)

**Branch**: `138-layout-attrs-metrics-fix` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/138-layout-attrs-metrics-fix/spec.md`

## Summary

Feature 138 is the P0 quick win from the active rendering-architecture report: expose the already-wired
Yoga flex layout model through the public Controls authoring surface, make the incremental layout dirty-set
recognize every newly geometry-driving authoring value, prove a shell-chrome screen can pin fixed regions,
and fix the known text-cache metrics accounting defect before larger renderer refactors start.

The technical approach is deliberately conservative. `src/Layout` already has public `LayoutIntent` fields
for padding, margin, gap, alignment, flex grow/shrink/basis, and min/max size, and `Layout.fs` already maps
those fields into Yoga. The implementation should therefore change the Controls boundary: add public
authoring builders, map those attributes in `Control.toLayout`, expand the layout-affecting name guard, and
keep current no-authored-value geometry byte-identical. The metrics fix should count text-cache hits only
when a measurement was resident before the frame's text-measure window began, so a cold frame with repeated
text reports zero hits while the equivalent warm frame reports reuse.

## Technical Context

**Language/Version**: F# on .NET `net10.0`, `LangVersion=latest`.

**Primary Dependencies**: Existing in-repo packages only: `FS.GG.UI.Controls`, `FS.GG.UI.Layout`,
`FS.GG.UI.Controls.Elmish`, `FS.GG.UI.Scene`, `FS.GG.UI.DesignSystem`, and `FS.GG.UI.Themes.Default` in tests.
`Layout` continues to own the Yoga.Net dependency. No new runtime dependency is planned.

**Storage**: N/A. Layout values are transient control attributes lowered into `LayoutIntent`. Text metrics
state stays in the existing retained render text-measure cache carried frame-to-frame.

**Testing**: Expecto/FsCheck through existing test projects. Focused suites should cover
`tests/Controls.Tests/Controls.Tests.fsproj`, `tests/Layout.Tests/Layout.Tests.fsproj`, and
`tests/Elmish.Tests/Elmish.Tests.fsproj`; package-surface validation runs through `PackageSurfaceCheck`.

**Target Platform**: Linux/dev and CI. The planned proofs are deterministic and headless; no GL/window-system
context is required for this feature.

**Project Type**: F# UI framework/library with deterministic host metrics.

**Performance Goals**: No wall-clock target. Required measurable outcomes are semantic: authored layout
values affect bounds in the same frame, unchanged defaults keep current bounds, incremental layout
invalidates only geometry-driving values, and text-cache metrics distinguish cold, warm, style-only, and
idle frames exactly.

**Constraints**:
- Tier 1 contracted change: public Controls authoring surface expands, observable layout changes when a
  consumer opts in, and metrics behavior changes.
- Visibility remains `.fsi`-owned. Public builder additions must be declared in `Attributes.fsi` and any
  typed front-door `.fsi` files that expose the values.
- Existing screens with no authored layout values must keep current geometry, including today's implicit
  Controls-boundary padding/gap behavior.
- Explicit zero values must override compatibility defaults.
- Padding and margin remain uniform values for this feature; edge-specific values are out of scope.
- `src/Layout` should not gain Controls, Viewer, or Elmish dependencies. This feature should not redesign
  Yoga, intrinsic sizing, retained rendering, or the compositor.
- Text-cache metric captures must be byte-deterministic for repeated same-sequence scripts.

**Scale/Scope**: One vertical slice across `src/Controls` public builders and layout lowering,
`src/Controls` retained invalidation/metric accounting, `src/Controls.Elmish` metric propagation if needed,
and focused tests in `Controls.Tests`, `Layout.Tests`, and `Elmish.Tests`. Documentation and surface baselines
are updated as required by the public API change.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1 (contracted change)**. The feature expands public Controls authoring API,
changes observable layout for opted-in screens, and fixes public frame metric behavior. It requires `.fsi`
updates, semantic tests, surface-baseline validation, and documentation/readiness evidence during
implementation.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec -> FSI -> Semantic Tests -> Implementation | PASS | The spec and plan define the public authoring contract before implementation. Tasks must update `.fsi` first, then failing semantic/layout/metric tests, then `.fs` bodies. |
| II. Visibility lives in `.fsi` | PASS | Additive public builders belong in `Attributes.fsi` and any typed widget `.fsi` surfaces. Internal metric/window helpers remain absent from public `.fsi` files unless intentionally exposed. |
| III. Idiomatic simplicity | PASS | Attribute parsing and `LayoutIntent` projection are straightforward records/functions over existing DU values. Any mutation stays in the existing retained-step/perf hot path and should remain locally documented. |
| IV. Elmish/MVU boundary | PASS | Metrics are produced by the existing deterministic `ControlsElmish.Perf.runScript` host path; state stays in retained render model state, and I/O remains at the host edge. No new stateful workflow is introduced. |
| V. Test evidence mandatory | PASS | Required tests cover every authored layout value, no-authored-value compatibility, dirty-set guard parity, shell chrome, cold/warm/style-only/idle metrics, and repeated-script determinism. |
| VI. Observability and safe failure | PASS | Invalid or negative layout values continue through the existing Layout normalization/diagnostic path. Metrics should fail tests loudly on false hits, false misses, or false invalidations. |

**Gate result**: PASS. No unresolved clarifications and no constitution violations.

**Post-design re-check**: PASS. Phase 0/1 artifacts add no dependency, no renderer refactor, and no new
storage. The planned public API additions are additive builder functions over existing lower-level layout
types and remain covered by surface-baseline validation.

## Project Structure

### Documentation (this feature)

```text
specs/138-layout-attrs-metrics-fix/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── layout-authoring-and-metrics.md
├── checklists/
│   └── requirements.md
└── tasks.md                 # Created by /speckit-tasks, not by /speckit-plan
```

### Source Code (repository root)

```text
src/Controls/
├── Attributes.fsi / Attributes.fs         # public Attr builders for layout authoring values
├── Control.fsi / Control.fs               # layoutAffectingAttrNames, toLayout projection, Stack helpers
├── Widgets/Primitives.fsi / .fs           # typed Stack spacing/layout front door, if expanded here
├── Widgets/Containers.fsi / .fs           # typed Wrap/Border/Panel shell layout front doors, if expanded here
└── RetainedRender.fsi / RetainedRender.fs # text metric frame-window accounting, internal

src/Controls.Elmish/
└── ControlsElmish.fsi / ControlsElmish.fs # public FrameMetrics propagation path, if accounting changes surface here

tests/Controls.Tests/
├── Feature138LayoutAttributesTests.fs     # authored bounds and no-authored-value compatibility
├── Feature138ShellChromeTests.fs          # fixed chrome + flexible content proof
├── Feature101LayoutDriftGuardTests.fs     # expanded geometry-name guard
└── Feature117TextCacheTests.fs            # cache oracle still byte-identical

tests/Layout.Tests/
└── Feature138IncrementalLayoutTests.fs    # incremental layout equivalence for new authored layout values

tests/Elmish.Tests/
└── Feature138TextMetricsTests.fs          # cold/warm/style-only/idle public FrameMetrics proof

tests/surface-baselines/
├── FS.GG.UI.Controls.txt
└── FS.GG.UI.Controls.Elmish.txt           # only if public metric surface changes, not expected
```

**Structure Decision**: Single F# solution. The lower-level `Layout` package remains the Yoga-backed engine
and should need no implementation change because its public `LayoutIntent` already contains the required
fields and Yoga mapping. Controls owns the public authoring surface and the conversion into `LayoutIntent`.
Elmish owns the deterministic public frame metrics path. The shell-chrome proof belongs in Controls tests
because it proves authored control layout, not product-template behavior.

## Complexity Tracking

No constitution violations require justification.

## Implementation Review - 2026-06-17

No new runtime dependencies were introduced. `src/Layout` remains the Yoga-backed engine and was not changed;
the layout authoring work is contained at the Controls boundary by projecting public `Attr` values into the
existing `LayoutIntent` fields. The text-cache metrics fix stays in the existing retained-render measurement
window and does not introduce a renderer or compositor refactor.

# Implementation Plan: Embedded Canvas Control

**Branch**: `191-embedded-canvas-control` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/191-embedded-canvas-control/spec.md`

## Implementation Status — 2026-06-25 (US1 + US2 + US3 complete)

All three user stories are implemented and verified green headlessly (Tier-1 discipline: `.fsi`-first,
deliberate surface-baseline updates, fail-before/pass-after semantic tests, no-regression baseline).

| Phase | Tasks | Status |
|---|---|---|
| 1 Setup | T001–T004 | ✅ `FS.GG.UI.Canvas` library + `tests/Canvas.Tests` created and registered in `FS.GG.Rendering.slnx`; T002 baseline captured (`readiness/baseline.md`). |
| 2 Foundational | T005–T009 | ✅ `SceneValue` attr seam + `Canvas.fsi` + `Elements.fsi`/`Loop.fsi` authored `.fsi`-first; the early-spike hypotheses (byte-identity + cache isolation) are confirmed by the passing US1/US2 tests rather than a throwaway probe. |
| 3 US1 (paint) | T010–T019 | ✅ `canvas` kind + `paintLeaf` branch (box-origin translate + clip + viewport + placeholder + zero-area safety); `Canvas` constructor module; 5 tests in `Feature191CanvasTests.fs` (paint/clip/size, fingerprint+byte-identity, placeholder/zero-area, CustomControl no-regression, viewport content-only). |
| 4 US2 (volatile + input) | T020–T031 | ✅ Cache isolation holds via the fingerprint picture cache (0 chrome repaints, SC-003); `volatile'` threaded as always-dirty (excluded from per-node reuse); `Reconcile` now compares `SceneValue` structurally (a static canvas reuses); raw pointer/key forwarding in `routeInteractivePointer`/`routeFocusedKey` (canvas-local coords, focus participation). Tests in `Feature191CanvasTests.fs` (US2) + `Elmish.Tests/Feature191CanvasInputTests.fs`. |
| 5 US3 (library + sample) | T032–T040 | ✅ Pure `Elements` + fixed-timestep `Loop` (clamp `≤0.25`, deterministic) with 17 headless tests; runnable `samples/CanvasDemo` (bouncing-ball, `volatile'` canvas + raw input + `Loop.advance`) — deterministic `evidence` mode verified reproducible. Held-input pattern documented (quickstart + `Game.fs`). New `FS.GG.UI.Canvas.txt` surface baseline. |
| 6 Polish | T041–T044 | ✅ T041 `DrawScope` deferred (combinators sufficient — deferral recorded); T043 final comprehensive baseline diffed against T002 (no test deleted/skipped/weakened/newly-red). |

**Surface-baseline deltas** (regenerated via `scripts/refresh-surface-baselines.fsx`): `FS.GG.UI.Controls.txt`
gains `Canvas` module + `AttrValue+SceneValue`; new `FS.GG.UI.Canvas.txt` (`Elements`, `Loop`, `StepState`).
No raw `PointerSample`/`ViewerKey`/`KeyModifiers` leaked to the public surface (they appear only inside
the `Canvas` constructor signatures). `FS.GG.UI.Controls.Elmish.txt` unchanged (input forwarding is internal).

**Caveats (disclosed):**
- `samples/CanvasDemo` uses **ProjectReferences** in-tree so it is buildable/verifiable now; the FR-015
  package-surface consumer path (PackageReferences against the local feed, like `samples/ControlsGallery`)
  is the publish-time swap once `FS.GG.UI.Canvas` is packed by `scripts/refresh-local-feed-and-samples.fsx`.
  Its deterministic game core touches only the public Canvas/Controls surface, so the swap is mechanical.
- SC-004 (perf/responsiveness lanes with an animating canvas) is **environment-limited** here: live GL
  perf lanes were not run. The determinism guarantees that underpin SC-004 (no wall-clock read in `Loop`
  or paint; fingerprint-gated repaint; cache isolation) are asserted by headless tests.
- T025 (PictureReplayCache per-node bypass) is satisfied **by construction**: a `canvas` is never a
  cacheable picture kind, so it is never emitted as a `CachedSubtree` boundary and never reaches
  `paintBoundary` — there is no record/replay to bypass. The `volatile'` always-dirty guarantee lives in
  `RetainedRender` (reuse exclusion), where it is observable and tested.
- Pre-existing reds at baseline (unrelated to this feature): `tests/Package.Tests` (stale packed-surface
  pins) and `samples/ControlsGallery.Tests` (stale package pins) — recorded in `readiness/baseline.md`.
- The final baseline (`readiness/final.md`) shows one **new** red vs T002: `samples/SecondAntShowcase.Tests`
  → `Feature172 …require-live writes a visible limitation`. **Proven NOT a feature-191 regression**: the
  test fails identically on a clean tree (changes `git stash`-ed), and SecondAntShowcase consumes the
  packed packages (not this branch's `src`). It is an environment-capability flake — the test asserts
  fail-closed *headless* behaviour, but a visible GL surface became available in the runner between the
  two baseline runs, so the headless precondition no longer holds (`environment-limited`).

## Summary

Add a first-class, model-driven **`canvas` control kind** that paints an application-supplied
immutable `Scene` into its laid-out box (clipped, box-origin local coordinates), integrated with the
existing layout / hit-testing / focus model. The framework already has the two hard prerequisites —
an immutable `Scene` display-list IR rasterized by SkiaSharp/GL, and a `~60 fps` `isAnimating`-gated
host tick — so no new drawing primitives or host loop are required. What is missing is the seam that
lets an application's own `Scene` reach the painter: today `CustomControl` declares `Render: unit ->
Scene` but its scene is silently discarded.

The feature is a **new control kind** (not a `CustomControl` repair) that carries its drawing as an
attribute-borne `Scene`, reuses the existing `hashScene` fingerprint for automatic invalidation,
adds an explicit **volatile / no-cache** mode walled behind a repaint boundary (so a per-frame canvas
cannot dirty the static themed chrome around it), forwards **raw pointer + keyboard** input to the
model, and ships a small **pure element library** (`'props -> Scene`) plus a deterministic
**fixed-timestep game-loop helper** and a runnable embedded sample.

**Technical approach** (from the analysis report
`docs/reports/2026-06-17-13-42-embedded-canvas-control-analysis-and-plan.md`, decisions D1–D8):

- **D1/D2 — scene carrier**: add `SceneValue of Scene` to the attribute value DU (`src/Controls`),
  not a new field on `Control<'msg>`. `paintLeaf` gains a `"canvas"` branch *before* the
  `richFamilies` check that translates author-local content to the box origin and clips to the box;
  a placeholder paints when no scene is supplied. Because the painted scene now contains the author's
  nodes, `hashScene` already yields a content-sensitive fingerprint — no fingerprint type change.
- **D3 — no `shouldRepaint`**: invalidation is the existing scene fingerprint, matching the
  SwiftUI/Compose/WPF direction.
- **D4 — volatile/no-cache + repaint boundary**: a per-kind `volatileFamilies`/`volatile'`-attr
  signal threads into fragment assembly so the canvas subtree bypasses record/replay and is treated
  as always-dirty, while sibling/parent picture-cache entries survive.
- **D5 — local coordinates**: content authored at top-left origin; the control applies the
  box translate + box clip; an optional viewport transform attr handles pan/zoom without touching
  layout size.
- **D6/D8 — element library + loop**: pure `'props -> Scene` combinators and a `Loop.advance`
  fixed-timestep accumulator (Glenn Fiedler; clamp `frameTime ≤ 0.25s`; interpolation `alpha`), with a
  documented held-input reconstruction pattern (`Set<ViewerKey>` + edge sets).
- **D7 — raw input**: new `onPointer`/`onKey` canvas event kinds whose `Dispatch` forwards the raw
  `PointerSample` / `ViewerKey` + `KeyModifiers`; the canvas participates in `Focus.order`.

This is a **Tier 1 (contracted) change**: it adds public surface (the `Canvas` constructor module, the
`canvas` catalog kind, the `SceneValue` attribute, the element/loop library). Per Constitution I & II,
the `.fsi` signatures are authored and exercised in FSI **first**, surface-area baselines are updated
deliberately, and semantic tests fail-before/pass-after.

> **Standing assumption — the byte-identity & isolation hypotheses are unverified until the app runs.**
> This feature is mostly additive, but two claims are provisional until exercised against the real
> render path: (1) **determinism** — that an author scene round-trips to byte-identical emitted scenes
> and identical `hashScene` fingerprints; (2) **cache isolation** — that a volatile canvas repainting
> every frame leaves surrounding chrome at `PictureCacheHits` with `RepaintedNodeCount` excluding the
> chrome. `/speckit-tasks` MUST schedule an **early live smoke run** in the Foundational phase (the D2
> decision spike: a static authored scene painted end-to-end through a real host, asserting fingerprint
> sensitivity) **before** US2 cache work is built on the isolation assumption.

## Technical Context

**Language/Version**: F# on .NET `net10.0`.

**Primary Dependencies**: SkiaSharp over **OpenGL (GL)**; `FS.GG.UI.Scene` (immutable display-list IR
+ `hashScene`), `FS.GG.UI.Controls` (control tree, `paintLeaf`, `Catalog`, retained render + picture
cache), `FS.GG.UI.Controls.Elmish` (pointer/key routing), `FS.GG.UI.Elmish` (`Animation.tickSubscription`),
`FS.GG.UI.SkiaViewer` (`PictureReplayCache`, host loop). No new third-party package is introduced
(FR-015); one new **first-party** library project (`FS.GG.UI.Canvas`, see Structure Decision) houses the
pure element/loop helpers.

**Storage**: N/A (in-memory scenes; no persistence).

**Testing**: Expecto + FsCheck via `tests/Controls.Tests` (reaches internals through
`InternalsVisibleTo`); golden-scene byte-identity + `WorkReduction` assertions
(`Feature116PictureCacheTests.fs`, `Feature120FingerprintTests.fs`, `RenderingTests.fs`,
`PointerInteractionTests.fs` are the patterns to mirror); a new `tests/Canvas.Tests` for the pure
element/loop helpers; surface-drift gate in `tests/Package.Tests/SurfaceAreaTests.fs`. GL-dependent
suites run under `DISPLAY=:1` (X11) locally.

**Target Platform**: Linux desktop (X11/GL) for live validation; libraries are platform-neutral.

**Performance Goals**: Sustain the existing `~60 fps` tick cadence with an animating canvas; **0**
surrounding-chrome repaints in frames where only the canvas changes (`WorkReduction.RepaintedNodeCount`
excludes chrome; chrome stays `PictureCacheHits`); no regression to the per-frame allocation / frame-time
budgets on the existing perf/responsiveness lanes (features 160/161/167/173).

**Constraints**: Determinism — identical model → byte-identical emitted `Scene` → identical `hashScene`
fingerprint → reproducible golden frames (FR-011); simulation depends only on injected tick durations /
input events / seed, never wall-clock time. Fail-loud preserved (no swallowed exceptions at paint/route
seams; empty/zero-size canvas degrades to a placeholder, not a crash — FR-013). No new circular module
dependency / producer→consumer back-edge in `src/Controls`. Embedded placement only; full-window host is
out of scope (FR-012).

**Scale/Scope**: One new control kind + one new attribute case + one no-cache boundary signal + two new
event kinds in the existing libraries; one new small pure library (`FS.GG.UI.Canvas`: `Elements`, `Loop`,
optional `DrawScope`); one runnable sample under `samples/`. Three prioritized user stories (US1 paintable
→ US2 interactive/isolated → US3 library + loop + sample).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

This is a **Tier 1 (contracted change)** — it adds public API surface. The full artifact chain applies:
spec → `.fsi` → semantic tests → implementation, plus surface-baseline updates and docs.

| Principle | Status | Notes |
|---|---|---|
| **I. Spec → FSI → Semantic Tests → Implementation** | ✅ | `Canvas.fsi` (constructors, `volatile'`, `onPointer`/`onKey`), the `SceneValue` attribute addition, and `FS.GG.UI.Canvas` (`Elements`/`Loop`) `.fsi` are authored and exercised in FSI **before** the `.fs` bodies. Per-story semantic tests are written against those signatures first. |
| **II. Visibility Lives in `.fsi`, Not in `.fs`** | ✅ | New public symbols are declared by presence in the relevant `.fsi`; no `private`/`internal`/`public` modifiers on top-level `.fs` bindings. Surface-area baselines (`readiness/surface-baselines/FS.GG.UI.Controls.txt`, new `FS.GG.UI.Canvas.txt`, and `…Controls.Elmish.txt` if input surface changes) are updated deliberately and validated by the drift test. |
| **III. Idiomatic Simplicity Is the Default** | ✅ | Plain functions + records + scene combinators; `Element<'props> = 'props -> Scene`; the loop is an explicit accumulator. No new operators/SRTP/reflection/type-providers; the optional `DrawScope` builder, if shipped, is a thin appender over the immutable `Scene` (justified here as the cross-framework-expected ergonomic, not mutation of rendered state). |
| **IV. Elmish/MVU boundary** | ✅ | The game loop is modeled at the MVU boundary: the host tick is a `Msg` carrying `dt`; `Loop.advance` is a **pure** `world -> world` transition consumed inside `update`; raw input arrives as `Msg`; no I/O inside `update`. Canvas paint sits at the existing interpreter edge. |
| **V. Test Evidence Is Mandatory** | ✅ | Golden-scene byte-identity + fingerprint-sensitivity tests, cache-isolation `WorkReduction` tests, input-forwarding tests, and pure `Loop.advance` determinism/clamp tests all fail-before/pass-after. The sample produces repeatable seeded evidence (FR-014). No assertion weakened, no test skipped; any golden delta reviewed, never silent. |
| **VI. Observability and Safe Failure** | ✅ | Existing `retained-step-*` trace spans preserved; paint/route seams stay swallow-free; a missing/empty/zero-size canvas degrades to a visible placeholder (FR-013) rather than crashing or silently blanking. |

**Gate result: PASS** — no violations. The one notable structural choice (a new first-party library
project) is recorded in Complexity Tracking below, not as a violation.

## Project Structure

### Documentation (this feature)

```text
specs/191-embedded-canvas-control/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — decision spike protocol + D1–D8 consolidation
├── data-model.md        # Phase 1 — Canvas control / SceneValue / StepState / InputState entities
├── quickstart.md        # Phase 1 — paint + isolation + seeded-loop validation guide
├── contracts/
│   └── canvas-control.md     # Phase 1 — the `.fsi` surfaces (Canvas module, SceneValue, Elements, Loop)
├── checklists/
│   └── requirements.md       # (created by /speckit-specify)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/Controls/
├── Attributes.fs / Types.fsi       # ADD: SceneValue of Scene attribute case; ControlInternals.sceneAttr
├── Catalog.fs                      # ADD: GENERATED `canvas` kind (category display, events onPointer/onKey)
├── Control.fs                      # ADD: paintLeaf "canvas" branch (translate+clip / placeholder);
│                                   #      volatileFamilies set; default canvas box (honors width/height)
├── Canvas.fs / Canvas.fsi          # NEW: public constructor surface — scene, viewport, volatile',
│                                   #      onPointer, onKey, create
└── RetainedRender.fs               # ADD: thread the volatile/no-cache signal into fragment assembly
                                    #      (always-dirty, no CachedSubtree boundary for the canvas subtree)

src/SkiaViewer/
└── PictureReplayCache.fs           # ADD: per-node volatile bypass at paintBoundary (record/replay skipped)

src/Controls.Elmish/
└── ControlsElmish.fs[i]            # ADD: onPointer raw-sample + onKey raw-key forwarding in
                                    #      routeInteractivePointer / routeFocusedKey; canvas focusable

src/Canvas/                         # NEW first-party library project: FS.GG.UI.Canvas (pure; Scene only)
├── Canvas.Lib.fsproj
├── Elements.fs / Elements.fsi      # rect/sprite/circle/polyline/at/layer/cached — pure 'props -> Scene
├── Loop.fs / Loop.fsi              # StepState<'world>, Loop.advance (fixed timestep + clamp), Loop.alpha
└── DrawScope.fs / DrawScope.fsi    # OPTIONAL ergonomic builder emitting immutable Scene

samples/CanvasDemo/                 # NEW: runnable embedded sample (e.g. bouncing sprites / Pong)
                                    #      exercising canvas + Elements + input + Loop; seeded evidence

tests/Controls.Tests/
└── Feature191CanvasTests.fs        # NEW: paint/clip golden + fingerprint sensitivity (US1);
                                    #      cache-isolation WorkReduction + input forwarding (US2)
tests/Canvas.Tests/                 # NEW: pure Elements golden + Loop.advance determinism/clamp (US3)
tests/Package.Tests/SurfaceAreaTests.fs   # surface-drift gate (expects the deliberate Tier-1 additions)

readiness/surface-baselines/
├── FS.GG.UI.Controls.txt           # UPDATE: + Canvas module, + canvas kind, + SceneValue
├── FS.GG.UI.Controls.Elmish.txt    # UPDATE (if input surface changes)
└── FS.GG.UI.Canvas.txt             # NEW baseline for the element/loop library
```

**Structure Decision**: Single-solution, multi-project (the repo's standard). The **canvas control
kind** and its paint/cache/input plumbing live in the existing `src/Controls`, `src/SkiaViewer`, and
`src/Controls.Elmish` projects (a control kind belongs with the control set; Constitution forbids
per-theme control forks — `canvas` is one semantic kind). The **pure, reusable element library + loop
helper** go in a new dependency-light first-party project `FS.GG.UI.Canvas` (depends only on
`FS.GG.UI.Scene`), keeping game/element content off the core control surface and independently testable
and packable. This resolves the source report's open question 4 in favor of a dedicated library; it is
revisitable in `/speckit-tasks` if folding into `Controls`/`Elmish` proves simpler.

## Complexity Tracking

> One deliberate structural addition (a new first-party project). Not a Constitution violation, but
> recorded here for transparency per the "minimize dependencies/projects" engineering constraint.

| Choice | Why Needed | Simpler Alternative Rejected Because |
|--------|------------|-------------------------------------|
| New `FS.GG.UI.Canvas` library project (Elements + Loop) | Keeps the reusable, pure game/drawing helpers off the core `Controls` public surface; independently testable, packable, and usable headlessly; mirrors report D6 | Folding into `Controls` bloats the control package's surface with game-loop/element APIs unrelated to the control set; folding `Loop` into `Elmish` splits one cohesive library across two packages. Revisit if the project overhead outweighs the separation. |
| New `SceneValue` attribute case (vs `Scene` field on `Control<'msg>`) | Minimally invasive — avoids touching every `Control<'msg>` construction/clone/test site and the `Types.fsi` public record shape; paint + fingerprint behavior identical | A `Scene option` field on the record is a larger public-surface change with no behavioral benefit (report rejected alternative). |

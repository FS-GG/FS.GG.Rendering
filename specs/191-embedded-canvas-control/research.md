# Phase 0 Research: Embedded Canvas Control

**Feature**: 191-embedded-canvas-control | **Date**: 2026-06-25

This phase consolidates the decisions from the prior analysis report
(`docs/reports/2026-06-17-13-42-embedded-canvas-control-analysis-and-plan.md`, which already performed
offline codebase exploration + online comparative research across Flutter, SwiftUI, Compose, HTML5
Canvas2D, Skia, and WPF/Avalonia) and resolves the open questions the spec deferred to planning. No
`NEEDS CLARIFICATION` markers remain.

## Decision log

### D1 — New `canvas` control kind, not a repaired `CustomControl`

- **Decision**: Add a sibling `canvas` kind; leave `CustomControl` functionally intact.
- **Rationale**: A canvas needs a `model -> Scene` (cache-friendly, fingerprintable) shape.
  `CustomControl`'s `unit -> Scene` forces mutable/closure state and cannot be fingerprinted without
  invoking the closure on every diff. Repairing it would yield a worse cache story and no model
  parameter.
- **Alternatives considered**: Repair `CustomControl` (rejected — un-fingerprintable, closure state);
  converging `CustomControl` onto the new machinery later as a thin adapter remains a possible
  follow-up, out of scope here.

### D2 — Carry the scene as an attribute value (`SceneValue of Scene`)

- **Decision**: Add a `SceneValue of Scene` case to the attribute value DU; `paintLeaf` extracts and
  paints it. Do **not** add a `Scene` field to `Control<'msg>`.
- **Rationale**: Minimally invasive — avoids touching every `Control<'msg>` construction/clone/test
  site and the public record shape. The fingerprint already walks the painted scene, so invalidation
  is automatic with no type change.
- **Alternatives considered**: `Scene option` field on `Control<'msg>` (rejected — larger public
  surface change, identical behavior).

### D3 — Invalidation = scene fingerprint; no `shouldRepaint` hook

- **Decision**: Reuse the existing `hashScene` FNV-1a fingerprint for canvas invalidation.
- **Rationale**: Any render-affecting change to the painted scene already changes the fingerprint.
  Matches the SwiftUI/Compose/WPF direction (all designed `shouldRepaint` away); Flutter's own docs
  warn `paint` can run when `shouldRepaint` is false. A hand-written predicate is a footgun and fights
  the immutable-IR + fingerprint engine.

### D4 — Explicit volatile / no-cache mode behind a repaint boundary

- **Decision**: A per-kind `volatileFamilies` set (and a `volatile'` attribute) marks the canvas
  subtree to bypass record/replay and be treated as always-dirty, walled so its per-frame change does
  not dirty sibling/parent picture-cache entries.
- **Rationale**: A scene that changes every frame is a guaranteed cache miss; recording then
  immediately invalidating is pure overhead. Isolation (the `RepaintBoundary` analogue), not caching,
  is the correct tool. `paintBoundary` already short-circuits when caching is disabled — thread a
  per-node signal through the same seam.
- **Default**: volatile is **opt-in** via `volatile'`; a static canvas (no `volatile'`) participates
  in the normal cache so unchanged dashboards stay cache-stable. (Resolves spec open question 2:
  authors that animate add `volatile'`; the framework does not guess.) The spec's "volatile-by-default
  for games" intent is satisfied at the *sample/helper* level — the game-loop sample applies
  `volatile'` — without forcing every canvas to bypass caching.

### D5 — Local coordinate space + optional viewport transform

- **Decision**: Canvas content is authored at origin `(0,0)`, top-left, y-down, logical units. The
  control applies the box-origin translate + box clip. An optional `viewport` transform attr applies a
  pan/zoom to the *content* only, never to layout size.
- **Rationale**: Universal cross-framework convention; keeps element-library output
  position-independent; separates draw transform (`RenderTransform`) from layout size
  (`LayoutTransform`). The existing `PerspectiveNode` (3×3 affine) supplies the raw-matrix path.

### D6 — Element library = pure `'props -> Scene`

- **Decision**: A small library of pure combinators (`rect`, `sprite`, `circle`, `polyline`, `at`,
  `layer`, `cached`) plus an optional `DrawScope`-style builder that *emits* an immutable `Scene`.
- **Rationale**: The F# analogue of `Painter`/`Path2D`/`Drawing`; testable in isolation; usable
  embedded, full-window, or headless. Expensive fragments can be wrapped (`cached key`) so the
  existing `SKPicture` cache keys on identity (Skia's "one picture, many transforms").
- **ECS deferred**: pure `state -> Scene` matches MVU value semantics + fixed-timestep reproducibility;
  a data-oriented `World` + `system: dt -> World -> World` is added only if entity-count cost proves
  it (spec non-goal).

### D7 — Raw input via `onPointer` / `onKey`; app reconstructs held state

- **Decision**: Add canvas event kinds `onPointer` (raw `PointerSample`) and `onKey` (raw `ViewerKey`
  + `KeyModifiers`). The canvas is focusable (`Focus.order`). Applications reconstruct held-input
  state in the model.
- **Rationale**: No polling API exists and none should; mirrors Elm `keyboard-extra`. Edge vs level
  matters — hold a `Set<ViewerKey>` (KeyDown adds, KeyUp removes) plus per-tick edge sets consumed
  once per fixed step and cleared, or a press leaks across substeps. Raw samples are sufficient for v1
  (resolves spec open question 5); higher-level gesture/drag interpretation is a later addition.

### D8 — Fixed-timestep game-loop helper

- **Decision**: `Loop.advance dt integrate frameTime state` runs the accumulator
  (`accumulator += frameTime; while accumulator >= dt { previous = current; current =
  integrate current dt; accumulator -= dt }`), clamping `frameTime ≤ 0.25s`; `Loop.alpha` returns the
  interpolation factor `accumulator / dt`.
- **Rationale**: Glenn Fiedler "Fix Your Timestep!" — never feed wall-clock `dt` into simulation;
  determinism-friendly and slots into Elmish `update`. The clamp kills the spiral of death. Wired via
  the existing `Animation.tickSubscription` (`isAnimating`-gated); the tick `Msg` carries the nominal
  fixed interval, not `Date.now()`, preserving seeded reproducibility.

## Resolved open questions (from the source report §10 / spec Assumptions)

| # | Question | Resolution |
|---|----------|-----------|
| 1 | Scene carrier: attribute vs `Control` field | **Attribute** `SceneValue` (D2). |
| 2 | `volatile'` default | **Opt-in** `volatile'` (D4); the game-loop sample/helper applies it, so games get no-cache behavior without forcing it on static canvases. |
| 3 | `CustomControl` disposition | **Leave intact** this feature; converge-as-adapter is a possible follow-up (D1). |
| 4 | Library home | **New first-party `FS.GG.UI.Canvas` project** for the pure Elements + Loop; the control kind stays in `Controls` (plan Structure Decision / Complexity Tracking). |
| 5 | Input granularity | **Raw `PointerSample` + `ViewerKey`** sufficient for v1 (D7); gestures later. |

## Decision spike protocol (Foundational — runs before US2)

A ≤30-line spike validates the byte-identity + fingerprint-sensitivity hypotheses end-to-end before
the cache-isolation work depends on them:

1. Add `SceneValue` + a hard-coded `paintLeaf "canvas"` branch.
2. Author a static scene (e.g., a red rectangle + circle), place a `canvas` carrying it inside a
   `stack` with sibling chrome, render through a real host (`DISPLAY=:1`).
3. Assert: the scene paints, is translated to the box origin, clipped to the box.
4. Assert fingerprint sensitivity: changing the scene changes `hashScene` (cache miss); an unchanged
   scene keeps the fingerprint (cache hit).

**Exit**: a static authored scene paints inside an embedded canvas; fingerprint reacts to content.

## Risks carried into design

| Risk | Likelihood | Mitigation |
|------|-----------|-----------|
| Cache-bypass plumbing leaks dirtiness into chrome (boundary not tight) | Med | US2 has an explicit isolation test asserting chrome stays `PictureCacheHits` and `RepaintedNodeCount` excludes chrome. |
| Raw pointer/key forwarding needs data the routing layer currently interprets away | Med | Add a raw-sample channel for `canvas` kinds rather than reshaping `PointerInteraction`; keep existing routing intact. |
| Attribute-carried `Scene` inflates attr-list / fingerprint cost for huge scenes | Med | Fingerprint already walks the painted scene regardless of carrier; volatile canvases bypass the cache so no extra hashing; benchmark in the isolation test. |
| Determinism regressions from wall-clock leaking into the loop | Low | `Loop.advance` takes `frameTime` as a parameter; the tick carries the fixed nominal interval; golden tests assert reproducibility. |
| Coordinate-convention confusion (local vs box vs device) | Low | D5 fixes top-left/y-down local authoring; the control applies the box translate; documented once in quickstart. |

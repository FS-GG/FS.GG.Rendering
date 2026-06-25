# Embedded Canvas Control — Delivery Roadmap

**Date:** 2026-06-24
**Status:** Active project (scheduled)
**Supersedes (as the actionable plan):** the analysis & proposal in
[`docs/reports/2026-06-17-13-42-embedded-canvas-control-analysis-and-plan.md`](2026-06-17-13-42-embedded-canvas-control-analysis-and-plan.md)
**Scope:** A first-class, model-driven `canvas` control for games and arbitrary 2D rendering,
embedded inside the existing Ant-themed control tree, plus a reusable pure-`'props -> Scene`
element library and a deterministic fixed-timestep loop helper.

> This roadmap turns the 2026-06-17 proposal into a sequenced, gated delivery plan and
> **re-anchors every edit site against the current tree** (post commits 182–190). Read the
> proposal for the *why* (comparative research §4, design decisions §5, architecture §6); read
> this for the *what/when/in-what-order* and the **two deltas** the refactors introduced.

---

## 1. Currency review (2026-06-24)

The proposal's architectural thesis is **fully intact** — every prerequisite and gap it relies on
still holds after the 182–190 "god-module splits". Two implementation mechanics changed and are
folded into the phases below; all other references are simply renumbered/relocated.

### 1.1 Thesis confirmed (no change to the design)
| Claim | Current anchor |
|---|---|
| Immutable `Scene = { Nodes: SceneNode list }` + full primitive set | `src/Scene/Types.fsi:483` (Paint `:89`) |
| `Control<'msg>` has **no** `Scene` field; `Content` is `string option` | `src/Controls/Types.fsi:358` |
| `CustomControl` discards its `Render`/`Draw` scene, paints placeholder | `src/Controls/CustomControl.fs:34` |
| `hashScene` FNV-1a walks every `SceneNode` (free fingerprint) | `src/Controls/Internal/SceneHash.fs:11` |
| `PictureCacheKey = { Box; Fingerprint }` | `src/Controls/RetainedRender.fs:137` |
| `PictureCacheEnabled` cache-bypass oracle exists | `src/Controls/RetainedRender.fs:180` |
| `paintBoundary` replays SKPicture on fingerprint match; short-circuits when disabled | `src/SkiaViewer/PictureReplayCache.fs:140` |
| `isAnimating`-gated tick returns `Sub.none` (no idle redraw) | `src/Elmish/AnimationTick.fs:40` |
| `FrameRateCap`, `ViewerPointerInput` | `src/SkiaViewer/Viewer.Types.fsi:25,769` |
| `routeInteractivePointer`, `routeFocusedKey` | `src/Controls.Elmish/ControlsElmish.fsi:439,537` |
| `AttrValue` DU is still the edit site for `SceneValue` | `src/Controls/Types.fsi:371` |
| Relocated paint internals: `paintLeaf` | `src/Controls/Internal/NodeAssembly.fs:72` |
| `richFamilies` | `src/Controls/ControlKindRegistry.fs:37` |
| `faithfulContent` | `src/Controls/Internal/ContentRender.fs:16` |
| `emptyState` placeholder | `src/Controls/Internal/ChartGeometry.fs:16` |

### 1.2 DELTA-A — Kind registration moved out of `Catalog.fs`  *(affects proposal §6.2 / Phase 1)*
Commit 183 added `src/Controls/ControlKindRegistry.fs[i]`. Registration is **no longer** "add a
GENERATED block to `Catalog.fs`". A new kind is now registered by:
1. Adding `"canvas"` to the hardcoded `catalogKinds` list (`ControlKindRegistry.fs:222`).
2. Adding matching arms to the registry dispatch functions (`isRich`, `isChart`, `chartSource`,
   `layoutRow`, …) in the same file.
3. Keeping `Catalog.fs` in sync for **schema/docs** (`RequiredAttributes`, `Events`) — it is no
   longer the dispatch SSOT but still drives validation/introspection.

A completeness test (**SC-001**) asserts `catalogKinds` equals the live catalog in *both*
directions, so a half-registration fails CI. This is a hard gate on Phase 1.

### 1.3 DELTA-B — Event payload is now typed  *(affects proposal §6.5 / Phase 2)*
Commit 184 removed the stringly `ControlEvent.Payload: string option` and replaced it with a typed
`Nav: NavPayload option` (`src/Controls/Types.fsi:305`; accessors `navValue`/`navText`/`navCell`).
`ControlEventBinding` is now `{ ControlId; EventKind: string; Dispatch: ControlEvent -> 'msg }`
(`Types.fsi:415`). Consequence: forwarding **raw** pointer/keyboard samples to the canvas must add
a **new typed `ControlEvent` case (or a dedicated raw-sample channel)** — the proposal's "extend
the `Dispatch` payload" wording (Risk #3) is now the *mandatory* path, not the preferred one.

---

## 2. Objectives & success criteria

**O1 — Paintable embedded canvas.** An application-supplied `Scene` renders into a laid-out,
clipped `canvas` control embedded among themed chrome. *Done when:* a golden-scene test shows an
authored scene translated to the box origin and clipped to the box, laid out inside a `stack`.

**O2 — Cache-isolated volatility.** A per-frame canvas does not picture-cache itself and does not
dirty surrounding chrome. *Done when:* a `WorkReduction` test shows chrome stays
`PictureCacheHits` while the canvas repaints every frame, and the canvas fragment reports
always-dirty / no cache entry.

**O3 — Raw interactivity.** Raw pointer + keyboard reach a focusable canvas via typed events.
*Done when:* pointer move/press/release/wheel and focused key events dispatch raw samples to the
model through a typed `ControlEvent` case.

**O4 — Deterministic real-time.** A reusable fixed-timestep loop + element library produce a
runnable, byte-reproducible embedded mini-game. *Done when:* `Loop.advance` unit tests prove
determinism + spiral-of-death clamp, and a headless sample renders identically across runs.

**Cross-cutting:** determinism preserved end to end (identical model → identical scene → identical
fingerprint); no new public-surface drift beyond the documented additions; SC-001 catalog↔registry
parity stays green.

---

## 3. Milestones & sequencing

```
M0 Decision gate ─▶ M1 Paintable canvas ─▶ M2 Cache isolation ─┬─▶ M3 Interactivity ─▶ M4 Loop+elements ─▶ M5 Polish/docs
        (½d)              (2–3d)                  (1–2d)        │         (2d)               (3–5d)             (1–2d)
                                                                └── M2/M3 may run in parallel after M1
```

**Critical path:** M0 → M1 → M2 → M4 → M5. M3 (interactivity) depends only on M1 and can overlap
M2. M4's element library depends on M1; its loop helper has no control-layer dependency and can
start any time after M0.

Rough total **~10–15 working days**; **M1 (~3d) is the minimum viable embedded canvas.**

---

## 4. Phase plan (gated, each independently shippable)

### M0 — Decision gate & spike  *(½ day)* — **gate before any production code**
Resolve the five open questions (§7) — at minimum the three that change edit sites:
- **G1 (scene carrier):** approve `AttrValue.SceneValue` (D2) over a `Control` field. *Default: yes.*
- **G2 (volatile default):** is `canvas` volatile/no-cache **by default** (opt-in caching for
  static canvases), or cached by default? *Default: volatile-by-default.*
- **G3 (raw-input shape):** new typed `ControlEvent` case vs. a side raw-sample channel (DELTA-B).

**Spike (T0.1):** ~30 lines — add `SceneValue`, a hard-coded `canvas` paint branch in
`NodeAssembly.paintLeaf`, render a static red rectangle end-to-end via an existing sample/test
host; verify fingerprint sensitivity (change scene → cache miss; same scene → hit).
**Exit:** spike renders; G1–G3 decided and recorded in this doc's changelog.

### M1 — The `canvas` control (paintable, embedded)  *(2–3 days)*
| Task | Edit site (current) |
|---|---|
| T1.1 Add `SceneValue of Scene` to `AttrValue`; `ControlInternals.sceneAttr` accessor | `src/Controls/Types.fsi:371`, `Attributes.*` |
| T1.2 **Register `canvas`** in `ControlKindRegistry` (`catalogKinds` + dispatch arms) **and** keep `Catalog.fs` schema in sync — **satisfy SC-001 parity** *(DELTA-A)* | `ControlKindRegistry.fs:222`, `Catalog.fs` |
| T1.3 `paintLeaf` branch: translate author-local scene into box + `RectClip`; design-time placeholder when no scene | `src/Controls/Internal/NodeAssembly.fs:72` |
| T1.4 Default sizing honoring explicit `width`/`height` | registry/layout dispatch (was `Control.fs:505`) |
| T1.5 Public surface `src/Controls/Canvas.fs[i]` (`scene`, `viewport`, `create`) | new module |
| T1.6 `StyleResolver` `canvas` case (optional bg/border, else inherit) | `src/DesignSystem/StyleResolver.fs` |

**Tests:** golden-scene (authored scene appears, clipped + translated); layout inside `stack`/`panel`;
**SC-001 parity green.**
**Exit (O1):** embedded canvas paints arbitrary authored scenes; determinism test green.

### M2 — Cache discipline (volatile / no-cache boundary)  *(1–2 days)*
| Task | Edit site |
|---|---|
| T2.1 `noCacheFamilies`/`volatile'` plumbing into the cache boundary; canvas subtree bypasses record/replay and is marked always-dirty | `RetainedRender.fs` fragment assembly + `PictureReplayCache.fs:140` |
| T2.2 Repaint-boundary isolation proof | Feature116-style `WorkReduction` test |

**Exit (O2):** chrome stays `PictureCacheHits` while canvas repaints every frame.

### M3 — Interactivity (raw pointer + keyboard)  *(2 days; may overlap M2)*
| Task | Edit site |
|---|---|
| T3.1 **New typed `ControlEvent` case (or raw-sample channel)** for canvas raw input *(DELTA-B)* | `src/Controls/Types.fsi:305/415` |
| T3.2 `onPointer` forwarding — raw `PointerSample` to bound dispatch; hit-test honors canvas box | `ControlsElmish.fsi:439` |
| T3.3 `onKey` forwarding + canvas focusability (`Focus.order`) | `ControlsElmish.fsi:537` |

**Tests:** `PointerInteractionTests` pattern — move/press/release/wheel dispatch raw samples; focused
key events reach the canvas.
**Exit (O3):** interactive canvas with surrounding UI cache-stable.

### M4 — Element library + fixed-timestep loop  *(3–5 days)*
| Task | Edit site |
|---|---|
| T4.1 `FS.GG.UI.Canvas` library: `Elements` (rect/sprite/circle/polyline/at/layer/cached) as pure `Scene` combinators | new project |
| T4.2 Optional `DrawScope` builder emitting immutable `Scene` | new module |
| T4.3 `Loop.advance`/`Loop.alpha` fixed-timestep accumulator + interpolation; clamp `frameTime ≤ 0.25s` | new module |
| T4.4 Documented input-state pattern (`Set<ViewerKey>` + edge sets) + a `samples/` mini-game (bouncing sprites / Pong) | `samples/` |

**Tests:** `Loop.advance` determinism (same seed+inputs → identical world) + spiral-of-death clamp;
element golden scenes; headless sample renders deterministically.
**Exit (O4):** runnable embedded mini-game; reusable element kit; deterministic loop.

### M5 — Polish & docs  *(1–2 days)*
Catalog/gallery entry; pattern doc under `docs/`; `CustomControl` disposition (proposal §8 — ship
`canvas`, then re-base `CustomControl.create` onto it as a thin adapter); accessibility defaults;
a "when to mark `volatile'`" performance note (see §6).

---

## 5. Dependencies & decision gates
- **G1–G3 (M0)** block M1/M3 edit sites — must be resolved first.
- **DELTA-A (SC-001 parity)** is a CI gate inside M1 — registry and catalog must agree both ways.
- **DELTA-B (typed event)** gates M3 — the raw-input case must land before forwarding.
- M4 element library depends on M1's paint path; M4 loop helper is independent (starts after M0).
- **CustomControl re-base (M5)** depends on `canvas` shipping (M1–M4); deferred by design.

---

## 6. Performance envelope (what to expect, and what to validate)
The design is an **immediate-mode island inside a retained UI**. It isolates per-frame cost to the
canvas raster and decouples simulation from frame rate, but does **not** remove the per-frame
whole-tree `view` rebuild + reconcile. Net per frame ≈ `O(entire control tree)` reconcile +
`O(canvas scene)` rebuild/raster, bounded by `FrameRateCap` (default 60), single-threaded,
CPU-translated, **no vsync lock**.

- **Eliminated:** picture-cache thrash on the canvas; fingerprint hashing of the volatile scene
  (cache bypassed); chrome re-raster (stays cache-hit).
- **Residual tax:** full `view` rebuild + whole-tree reconcile each tick (scales with total UI,
  not canvas); immutable-`Scene` allocation churn (GC pressure ∝ scene size); scene→Skia translation.
- **Ceiling:** timer-driven present (jitter vs. display refresh), single-threaded loop; the
  fixed-timestep accumulator decouples sim rate but does not parallelize.
- **Fit:** excellent for data-viz / diagrams / light interactive surfaces and casual 2D games
  (Pong/bouncing sprites); **not** a path to a demanding real-time engine.
- **Validate with real numbers:** the M0 spike + a `WorkReduction` instrumentation pass yield
  actual `PictureCacheHits`/`RepaintedNodeCount`/frame timings before committing to M2–M4. All
  figures above are architectural reasoning, **not** benchmarks.

---

## 7. Open questions (resolve at M0)
1. **Scene carrier (G1):** approve `AttrValue.SceneValue` over a `Control<'msg>` field? *(plan assumes yes)*
2. **`volatile'` default (G2):** volatile/no-cache by default with opt-in caching, or cached by default?
   *(plan leans volatile-by-default; static dashboards may prefer cached)*
3. **Raw-input shape (G3):** new typed `ControlEvent` case vs. dedicated raw-sample channel *(DELTA-B)*.
4. **Library home:** new `FS.GG.UI.Canvas` project, or fold `Elements`/`Loop` into existing libs?
5. **Input granularity:** is raw `PointerSample` + `ViewerKey` enough for v1, or also surface
   gesture/drag (`DragBegin/Move/End`) to the canvas?

---

## 8. Risks & mitigations (carried from proposal §9, updated)
| Risk | Likelihood | Mitigation |
|---|---|---|
| Attribute-carried `Scene` inflates fingerprint cost for huge scenes | Med | Volatile canvases bypass the cache → no extra hashing; static canvases use `cached` identity keys; benchmark in M2 |
| Cache-bypass leaks dirtiness into chrome | Med | M2 isolation test asserts chrome stays `PictureCacheHits` |
| **SC-001 catalog↔registry parity fails** *(new, DELTA-A)* | Med | Register in `ControlKindRegistry` AND `Catalog.fs` together; run SC-001 in M1 |
| **Typed-event plumbing for raw samples** *(new, DELTA-B)* | Med | Add a dedicated typed `ControlEvent` case/raw channel; don't reshape `PointerInteraction` |
| Per-frame whole-tree `view` rebuild cost | Low–Med | Reconciler diffs static chrome cheaply (identity-stable); validate via `RepaintedNodeCount` |
| Wall-clock leaking into the loop breaks determinism | Low | `Loop` takes `frameTime` param; tick carries fixed `interval`; golden tests assert reproducibility |
| Stale `file:line` anchors as the tree evolves | Med | §1 re-anchored 2026-06-24; re-verify before editing |

---

## 9. Out of scope (this iteration)
Full-window/no-chrome game host; GPU shader/compute beyond `Scene` + Skia; an ECS runtime (revisit
only if entity counts make per-frame record-tree updates measurably hot); over-extending
`CustomControl` beyond the M5 re-base.

---

## 10. Changelog
- **2026-06-24** — Roadmap created from the 2026-06-17 proposal; currency review re-anchored all
  references; DELTA-A (ControlKindRegistry) and DELTA-B (typed `ControlEvent`) folded into M1/M3.
  *(G1–G3 decisions to be recorded here at M0.)*

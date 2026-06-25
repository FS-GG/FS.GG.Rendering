# Phase 1 Data Model: Embedded Canvas Control

**Feature**: 191-embedded-canvas-control | **Date**: 2026-06-25

Entities are described at the design level (fields + rules + relationships), not as final `.fs` code.
The concrete `.fsi` signatures live in [contracts/canvas-control.md](./contracts/canvas-control.md).

## 1. Canvas control (a `Control<'msg>` of kind `"canvas"`)

A leaf control in the existing control tree. Reuses `Control<'msg> = { Kind; Key; Attributes; Children;
Content; Accessibility }` unchanged — the canvas is expressed entirely through `Kind = "canvas"` plus
attributes (no new record field, per D2).

| Aspect | Value / Rule |
|--------|--------------|
| Kind | `"canvas"` (registered in `Catalog.fs`, category `display`, events `onPointer`/`onKey`). |
| Drawing | Carried as a `SceneValue` attribute (§2). Absent ⇒ design-time placeholder. |
| Volatile | Optional `volatile'` attribute (or membership in `volatileFamilies`) ⇒ no-cache + repaint boundary. Default: cached like any leaf. |
| Viewport | Optional `viewport` transform attribute applied to *content*, not layout. |
| Size | Honors explicit `width`/`height` attributes; otherwise a sensible default box. |
| Focus | Participates in `Focus.order` so a focused canvas receives `onKey`. |
| Children | None (leaf); content is the supplied scene, not child controls. |

**Validation / safe-failure rules**

- No `SceneValue` ⇒ paint a clear placeholder (never crash, never silent blank). (FR-013)
- Zero-area / unmeasured box ⇒ paint nothing; no error.
- Supplied content larger than the box ⇒ clipped to the box; must not bleed onto siblings.

## 2. SceneValue (attribute value)

A new case on the attribute value DU (`src/Controls`).

| Field | Type | Notes |
|-------|------|-------|
| (payload) | `FS.GG.UI.Scene.Scene` | The author's immutable display list, authored in canvas-local coords (top-left origin, y-down). |

- **Relationship**: read by `paintLeaf` for `"canvas"` kinds via `ControlInternals.sceneAttr`; its nodes
  flow into the painted scene, so `hashScene` fingerprints it automatically (D3).
- **Rule**: identical `Scene` value ⇒ identical fingerprint ⇒ cache hit / no repaint. Any
  render-affecting change ⇒ changed fingerprint ⇒ repaint. (FR-003, FR-011)

## 3. Drawing element (`Element<'props>`)

| Aspect | Value / Rule |
|--------|--------------|
| Shape | `Element<'props> = 'props -> Scene` (a pure function). |
| Purity | Same `'props` ⇒ identical `Scene` (referentially transparent). (FR-008) |
| Composition | Combined with `at` (translate), `layer` (group), and nesting; position-independent. |
| Caching | `cached key scene` wraps an expensive fragment so the existing `SKPicture` cache keys on identity. |

Provided combinators (library `FS.GG.UI.Canvas.Elements`): `rect`, `sprite`, `circle`, `polyline`,
`at`, `layer`, `cached`. All return `Scene`; none mutate.

## 4. StepState<'world> (fixed-timestep loop state)

| Field | Type | Notes |
|-------|------|-------|
| `Current` | `'world` | Latest integrated simulation state. |
| `Previous` | `'world` | Prior step's state, for interpolation. |
| `Accumulator` | `float` | Unconsumed time (seconds) carried into the next frame. |

**Transitions** (`Loop.advance dt integrate frameTime state`):

1. `frameTime' = min frameTime 0.25` (clamp — spiral-of-death guard). (FR-009)
2. `acc = state.Accumulator + frameTime'`.
3. While `acc >= dt`: `previous ← current`; `current ← integrate current dt`; `acc ← acc - dt`.
4. Result `{ Current = current; Previous = previous; Accumulator = acc }`.

- **Determinism rule**: output depends only on `(dt, integrate, frameTime, state)` — no wall-clock
  read. Same inputs ⇒ identical `StepState`. (FR-011)
- **`Loop.alpha dt state` = `state.Accumulator / dt`** ∈ `[0,1)`, the render interpolation factor
  between `Previous` and `Current`.
- **Step-count rule**: the number of `integrate` calls is `floor((Accumulator + clamp(frameTime)) / dt)`
  — a deterministic function of accumulated time.

## 5. Input state (application-side, documented pattern — not framework code)

Held-input state the application reconstructs in its model from raw `onPointer`/`onKey` messages.

| Field | Type | Notes |
|-------|------|-------|
| `PressedKeys` | `Set<ViewerKey>` | Level state: `KeyDown` adds, `KeyUp` removes. |
| `EdgeDown` | `Set<ViewerKey>` | Pressed-this-tick edges; consumed once per fixed step, then cleared. |
| `Pointer` | `PointerSample option` | Latest raw pointer (position, buttons, wheel) in canvas-local space. |

- **Rule**: edge sets are cleared after each fixed step or one press leaks across substeps; continuous
  deltas (e.g., wheel) are distributed across substeps (÷ substep count) on multi-step frames. (D7)

## Entity relationships

```
Control "canvas" ──carries──> SceneValue(Scene) ──hashed by──> hashScene ──drives──> picture cache / repaint
      │                                   ▲
      │ onPointer/onKey                   │ view builds Scene from world (lerp Previous→Current by alpha)
      ▼                                   │
   Msg (raw input) ──> update ──> Loop.advance ──> StepState<'world> ──> world
      │                                   ▲
      └──> InputState (PressedKeys/EdgeDown/Pointer) ──fed into──> integrate
```

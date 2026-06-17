# Phase 1 Data Model — Render Blockers (Clipping, Overlay & Scroll)

This feature is a rendering/correctness fix; the "entities" are the shared composition rule and the render
structures. Field shapes are advisory — the binding declaration is each module's `.fsi`.

## 1. Container clip composition (the single shared rule)

`ControlInternals.composeContainerScene` — the ONE function every paint-assembly site calls.

- **Input**: `box: Rect option` (the node's evaluated bounds), `own: Scene list` (the node's own paint),
  `childScenes: Scene list` (the assembled children).
- **Rule**: `Some b, (_ :: _) → own @ [ Scene.clipped (RectClip b) (Scene.group childScenes) ]`; otherwise
  `own @ childScenes` (a leaf, or a node without a box, composes flat — byte-identical to pre-137 `own @
  children`).
- **Invariant**: identical output regardless of which site assembled it.

**Call sites (all six MUST route through it):**

| Site | Location | Was |
|---|---|---|
| Full render | `Control.renderTree` `paint` | `paintNode @ children` |
| Retained build | `RetainedRender.build` | `own @ children SubtreeScene` |
| Retained build-fresh | `RetainedRender.buildFresh` | same |
| Retained carry | `RetainedRender.carry` | same |
| Retained child-insert/replace fallback | `RetainedRender` (the 4th `let subtree` site) | same |
| **Retained emit walk** | **`RetainedRender.assemble` (`:1269`)** | **`own @ (children \|> collect assemble)` — the 136 MISS** |

## 2. Picture-cache parity (unchanged machinery, made consistent)

- `hashScene`/`pictureKeyOf`/`Fragment.Fingerprint`, `PictureReplayCache`, and the SKPicture record/replay
  path are **unchanged**.
- A cacheable boundary (`isCacheablePicture`, only `data-grid-row`) is a **leaf**, so `composeContainerScene`
  does not alter its `SubtreeScene` or fingerprint → hit counts/effectiveness unchanged.
- **Invariant (the gate)**: with clipping enabled, `Audit_PictureCache` holds — `flat off.Render = flat
  on.Render`, hits `=3`, misses `=0`, steady-state effectiveness margin preserved.

## 3. Overlay group (render order)

- A node is **overlay/transient** when built on the existing `Overlay` container (or a transient surface
  routed into it).
- `Control.renderTree` (and the retained emit) produce two ordered groups: **in-flow** then **overlay**; the
  final scene is `inFlow @ overlay`.
- Overlay subtrees paint at their true coordinates and are **NOT** wrapped by ancestor container clips (they
  are collected out of the in-flow clip hierarchy).
- Hit-testing (`nearestAuthored`/`hitTest`) consults the overlay group first (topmost wins).
- **Invariants**: an open transient surface's drawn area is never overprinted by an in-flow sibling; a page
  with an empty overlay group renders byte-identically to a pure in-flow pass; full ≡ retained holds with
  overlays present.

## 4. ScrollViewer viewport

- A `ScrollViewer` clips its content to its box (the R1 container-clip model), carries a **scroll offset**,
  and renders a **scroll affordance**.
- Content taller than the viewport is clipped (scrollable), not spilled.
- **Invariant**: nothing inside a `ScrollViewer` paints outside its box.

## 5. Rebaseline ledger row

See `contracts/rebaseline-ledger.md`. One disclosed record per changed baseline: baseline id/path, the
defect/fix that changed it, before/after note, theme(s), and intended-confirmation.

## 6. Defect ↔ remediation-layer matrix (continuing 136 §8)

| Defect class | Remediation layer | Where |
|---|---|---|
| control-overlap / region-overlap / spill | framework (renderer/control) | R1 — `composeContainerScene` at all 6 sites |
| overlay-overprint | framework (renderer/control) | R2 — deferred overlay group + hit-test |
| unbounded-content / no-scroll | framework (control/layout) + sample | R3 — viewport + Shell region sizing |
| (cache parity correctness) | framework (retained render) | R1 — the `assemble` emit-walk fix |

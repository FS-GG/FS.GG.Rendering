# Phase 1 Data Model: Frame Pipeline Entities

This phase introduces **no new persisted data** and **no new public type**. It (a) promotes one
existing private record to `internal`, (b) adds one new internal carrier record, and (c) names the
explicit value each stage consumes/produces. All types are `internal` (off the public package surface).

---

## 1. `FrameState` (existing — promoted `private` → `internal`)

**Source**: `RetainedRender.fs:1291` (introduced feature 186). The per-frame mutable accumulator,
threaded by reference through all four stages.

**Change**: `type private FrameState` → `type internal FrameState`, and declared in `RetainedRender.fsi`
so `Controls.Tests` can construct crafted instances to unit-test stages in isolation (FR-003). No field
change; the `// mutable: hot path` disclosures are retained (Constitution III).

**Fields** (unchanged): `Tc: TextMeasureCache`, `TextHits/TextMisses: int`, `NextId: uint64`,
`Recomputed/ChangedBound/Shifted: int`, `Memo: MemoCache`, `MemoHits/MemoMisses: int`,
`MetadataVisited: int`, `VirtualMaterialized/VirtualTotal: int`,
`PcEntries: Map<RetainedId, int*PictureCacheKey>`, `PcClock: int`, `PictureHits/PictureMisses: int`,
`ReplaySkippedNodes/ReplayNativeBytes: int`, `RepaintedBoxes: ResizeArray<Rect>`.

**Validation / invariants**:
- Seeded from `prev` for `step` (caches carried forward, work counters zero) and cold (empty caches,
  `NextId=0`) for `init` — the **cold-vs-steady distinction is a seed-config parameter**, not a code
  fork (spec edge case "cold start vs steady state").
- No accumulator may be double-counted or dropped across a stage seam (spec edge case
  "FrameState threading"): the same instance is threaded; stages mutate in the current order.
- Final field values after `assemblyStage` MUST equal today's values for the same input (the byte-
  identity check projects these into `WorkReductionRecord`).

---

## 2. `FrameContext` (NEW — internal input carrier)

The per-frame **immutable** inputs the stages share, lifted out of today's `step` closure environment
(R2). One record so stage signatures stay short and threading is uniform.

**Fields** (illustrative; final shape fixed by the R6 compile probe):
- `Theme: Theme` — the per-frame theme.
- `Size: FS.GG.UI.Scene.Size` — frame size (for `frameArea`, render-result).
- `Prev: RetainedRender<'msg>` — the previous retained structure (caches, enabled oracles, prior tree).
- `ThemeChanged: bool` — `prev.Theme <> theme`, computed once in layoutStage and read in paintStage.

`boundsById` and `layoutResult`/`root` are **produced by** layoutStage (not in `FrameContext`); they
flow stage→stage as explicit outputs (§3), keeping `FrameContext` to the genuinely frame-global inputs.

**Validation / invariants**: immutable; carries no accumulator (those live in `FrameState`); generic
over `'msg` exactly as the retained types are.

---

## 3. Stage inputs/outputs (the explicit values crossing each seam)

Each stage is a pure-ish function over `(FrameContext, FrameState, …explicit inputs) → …explicit
outputs` (mutating only the threaded `FrameState`). Names preserved from today's locals.

| Stage | Consumes | Produces |
|---|---|---|
| **diffStage** | `ctx.Prev.Root.Control`, `next: Control<'msg>` | `DiffResult` = `{ Patch; Diagnostics }` (the `Reconcile.diff` result), `dirty: Set<string>` (`layoutDirtySet`), `invalidated: int` |
| **layoutStage** | `ctx`, `st`, `next`, `dirty` | `root: LayoutNode`, `boundsById`, `layoutResult: LayoutResult`, `remeasured: int`, `themeChanged: bool` (seeds `st` caches from `ctx.Prev`) |
| **paintStage** | `ctx`, `st`, `result.Patch`, `boundsById`, `themeChanged`, `next` | `newRoot: RetainedNode<'msg>` (mutates `st`: ids, recomputed/shifted/changed, memo, repainted boxes, metadata visits) |
| **assemblyStage** | `ctx`, `st`, `newRoot`, `next`, `result.Diagnostics`, `layoutResult`, `root`, `dirty`/`invalidated`, `remeasured` | `RetainedRenderStep<'msg>` = `{ Retained; Render; Diagnostics; WorkReduction }` |

**`DiffResult`**: a thin alias for the existing `Reconcile.diff` return (`Patch` +
`Diagnostics`); may be the reconcile result type directly rather than a new record (probe decides).

---

## 4. Unchanged public/result types (the surface contract — FR-004)

These keep their names, shapes, and call sites exactly (no `.fsi` change beyond the new `val internal`
stage entries and `FrameState`/`FrameContext` types):

- `RetainedRender<'msg>`, `RetainedNode<'msg>`, `RenderFragment`, `RetainedMetadata<'msg>`,
  `RetainedUiState`, `AnimationClock`, the cache types (`MemoCache`, `PictureCache`, `TextMeasureCache`).
- `RetainedRenderStep<'msg>` (the `step` result), `RetainedInit<'msg>` (the `init` result),
  `WorkReductionRecord` (40 fields, populated unchanged by assemblyStage).
- The public-ish `val`s: `step`, `init`, `retainedHitTest`, `hitTestLayout`, `authoredControlIds`
  (call shapes unchanged).

The relocated **feature-159 / feature-147** types (`CompositorDamageRegion*`, `Feature159*`,
`PromotionDecision*`, `SnapshotResourceVerdict`, `DamageSetInputs`, `PromotionInputs`) move file but
**not** namespace, so unqualified references resolve unchanged; internal qualified references update
to the new module (`CompositorPolicy.*`).

---

## 5. State transitions

No new state machine. The only "transition" is the cold-vs-steady seed of `FrameState`:

```
init:  FrameState.coldSeed   →  layoutStage(full evaluate) → paintStage(seed: build w/o reuse) → assemblyStage
step:  FrameState.fromPrev   →  diffStage → layoutStage(incremental) → paintStage(reuse walk)   → assemblyStage
```

US2 (conditional) makes `init` reuse paintStage/assemblyStage in their cold configuration (full layout,
seed paint, no prior-fragment reuse) instead of its parallel `build`/`seedPictures` copy — gated on a
real reduction (FR-007/FR-016).

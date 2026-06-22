# Contracts: Frame Pipeline Stage Signatures (`RetainedRender.fsi` additions)

The "interface" this internal library exposes for this feature is the set of `val internal` stage
entry points and `type internal` carriers added to **`src/Controls/RetainedRender.fsi`**. They are the
testability contract (FR-003) — reached by `Controls.Tests` via `InternalsVisibleTo`, **never** on the
public package surface (`readiness/surface-baselines/FS.GG.UI.Controls.txt` stays unchanged, FR-004/
FR-014). Final parameter shapes are confirmed by the R6 compile probe; the contract below is the target.

> Visibility is declared **here in the `.fsi`** (Constitution II) — no access modifier is added to the
> `.fs` bindings. Everything below is `internal`, so the public surface diff is empty (no bump).

## Types added to `RetainedRender.fsi`

```fsharp
/// Feature 190 (Pattern C, promoted from `type private`): the per-frame mutable accumulator threaded
/// through every stage. Listed here so the stage suites can construct a crafted instance and assert a
/// single stage's mutations in isolation (FR-003). Fields and `// mutable: hot path` discipline are
/// unchanged from feature 186.
type internal FrameState =
    { mutable Tc: TextMeasureCache
      mutable TextHits: int
      mutable TextMisses: int
      mutable NextId: uint64
      mutable Recomputed: int
      mutable ChangedBound: int
      mutable Shifted: int
      mutable Memo: MemoCache
      mutable MemoHits: int
      mutable MemoMisses: int
      mutable MetadataVisited: int
      mutable VirtualMaterialized: int
      mutable VirtualTotal: int
      mutable PcEntries: Map<RetainedId, int * PictureCacheKey>
      mutable PcClock: int
      mutable PictureHits: int
      mutable PictureMisses: int
      mutable ReplaySkippedNodes: int
      mutable ReplayNativeBytes: int
      RepaintedBoxes: ResizeArray<FS.GG.UI.Scene.Rect> }

/// Feature 190: the immutable per-frame inputs shared by the stages (lifted out of the former `step`
/// closure environment). Generic over 'msg exactly as the retained types are.
type internal FrameContext<'msg> =
    { Theme: Theme
      Size: FS.GG.UI.Scene.Size
      Prev: RetainedRender<'msg>
      ThemeChanged: bool }
```

## Stage `val internal` signatures added to `module internal RetainedRender`

```fsharp
    /// Feature 190 — Stage 1 (diff). Total; never throws; duplicate keys surface a `KeyCollision`
    /// diagnostic in the result (FR-010). Pure over (prev tree, next). Produces the reconcile result,
    /// the layout dirty set (`LayoutNodeId` domain), and its pre-propagation size.
    val internal diffStage:
        prev: RetainedRender<'msg> ->
        next: Control<'msg> ->
            Reconcile.DiffResult<'msg> * Set<string> * int

    /// Feature 190 — Stage 2 (layout). Seeds `st` caches from `ctx.Prev` (or cold for `init`), runs the
    /// INCREMENTAL evaluator over `dirty` (full evaluate in the cold/init configuration), and reports
    /// the re-measured count and theme-change flag. Mutates only the threaded `st`.
    val internal layoutStage:
        ctx: FrameContext<'msg> ->
        st: FrameState ->
        next: Control<'msg> ->
        dirty: Set<string> ->
            LayoutStageResult<'msg>   // { Root; BoundsById; LayoutResult; Remeasured; ThemeChanged }

    /// Feature 190 — Stage 3 (paint). The reuse-driven reconciliation walk (Keep/Replace/Update + child
    /// ops), routing memoizable sites through the memo seam and contributing each repainted node's box
    /// to the damage set. Mutates only `st`. Independently testable with a crafted (ctx, st, patch,
    /// boundsById, next). `init` calls this in its cold/seed configuration (US2).
    val internal paintStage:
        ctx: FrameContext<'msg> ->
        st: FrameState ->
        patch: Reconcile.NodePatch<'msg> ->
        boundsById: BoundsById ->            // the layoutStage output type
        next: Control<'msg> ->
            RetainedNode<'msg>

    /// Feature 190 — Stage 4 (assembly). The read-only post-build walks (virtualization tally, damage
    /// reduce, picture/replay cache, offscreen diagnostics, UI-state/clock collect, scene assembly,
    /// render result) and the `WorkReductionRecord` + `RetainedRenderStep` construction. Mutates only
    /// `st` (the cache/replay tallies). Produces the public step result.
    val internal assemblyStage:
        ctx: FrameContext<'msg> ->
        st: FrameState ->
        layout: LayoutStageResult<'msg> ->
        diff: Reconcile.DiffResult<'msg> ->
        dirtyInvalidated: int ->
        newRoot: RetainedNode<'msg> ->
        next: Control<'msg> ->
            RetainedRenderStep<'msg>
```

*(`Reconcile.DiffResult<'msg>`, `LayoutStageResult<'msg>`, `BoundsById` are named in the probe; they may
resolve to existing types — e.g. the reconcile result and `ControlInternals` bounds map — rather than
new declarations. The contract is the **stage decomposition**, not the exact carrier names.)*

## Behavioral contract (asserted by `Feature190StagePipelineTests.fs`)

| ID | Contract | Test shape |
|---|---|---|
| C-DIFF | `diffStage prev next` returns the same patch/diagnostics + dirty set the inline code does today; duplicate keys → `KeyCollision` (FR-010). | crafted prev/next incl. duplicate-key tree |
| C-LAYOUT | `layoutStage` over a crafted dirty set re-measures exactly the post-propagation boundary; `Remeasured`/`ThemeChanged` match; an empty dirty set re-measures nothing (idle frame). | crafted `dirty`, asserts `LayoutResult.Invalidated` |
| C-PAINT | `paintStage` produces a `RetainedNode` whose ids/reuse decisions and `st` mutations (Recomputed/Shifted/ChangedBound/Memo*/RepaintedBoxes) match the inline build for a crafted patch. | Keep/Replace/Update + ChildInsert/Move/Remove cases |
| C-ASM | `assemblyStage` builds a `WorkReductionRecord` whose 40 fields equal today's for the same `(st, newRoot, layout, diff)`. | golden record comparison |
| C-COMPOSE | `step = diffStage >> layoutStage >> paintStage >> assemblyStage` is byte-identical to the pre-change `step` over the scene corpus (scenes + `hashScene`). | corpus golden-hash zero-delta |
| C-TRACE | every `retained-step-*` span present today is still emitted with `FS_GG_RENDER_LAG_TRACE=1` (FR-008). | trace-capture assertion |
| C-GATE | an injected regression (reordered accumulation / dropped damage box) turns the gate RED; the real decomposition is GREEN (FR-015/SC-008). | mutation-style negative test |
| C-SURFACE | `tests/Package.Tests/SurfaceAreaTests.fs` diff is empty after the change (no public bump). | surface-drift gate |

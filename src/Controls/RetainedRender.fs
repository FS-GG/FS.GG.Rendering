namespace FS.GG.UI.Controls

open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

// Feature 091 (E2) — wiring the parked keyed reconciler (feature 067) onto the live render path.
// This is NOT a new algorithm: it consumes `Reconcile.diff`'s patch and drives the next frame
// from `ControlInternals.evaluateLayout` + `paintNode` (the SAME measure/paint `Control.renderTree`
// uses), reusing cached fragments for unchanged + unshifted subtrees. The render output is
// therefore byte-for-byte identical to a full rebuild of `next` BY CONSTRUCTION (FR-005, C2):
// a reused fragment is reused only when its paint inputs (the node's own data + its computed box)
// are provably unchanged, so it equals what re-painting would have produced.

type internal RetainedId = RetainedId of uint64

type internal RenderFragment =
    { OwnScene: FS.GG.UI.Scene.Scene list
      SubtreeScene: FS.GG.UI.Scene.Scene list
      Box: FS.GG.UI.Scene.Rect option
      // Feature 120 (US3): the structural fingerprint of `SubtreeScene` (computed when painted, carried on Keep).
      Fingerprint: uint64 }

type internal RetainedNode<'msg> =
    { Identity: RetainedId
      Control: Control<'msg>
      Fragment: RenderFragment
      Children: RetainedNode<'msg> list }

// Feature 099 (R4) / 103 (R6): the per-identity animation clock. `Anim` is the feature-073
// `Animation` shape, but the LIVE channel is the opacity tween only — `applyAt` samples
// opacity/transform and never recolors by the `Color` tween, so the visual-state cross-fade is NOT a
// standalone color tween. It is the two-snapshot composite (`From` fading out under the next
// own-scene fading in) realized in `sampleOnPaint`. `Elapsed` is the accumulated injected delta;
// `Target` is the `VisualState` the clock animates toward; `From` is the prior state's static
// own-scene snapshot, captured at transition start, composited under the next own-scene (empty ⇒ a
// plain fade-in). Generalizes the 091 transform-only carried slot.
type internal AnimationClock =
    { Anim: FS.GG.UI.Scene.Animation
      Elapsed: System.TimeSpan
      Target: VisualState
      From: FS.GG.UI.Scene.Scene list }

type internal RetainedUiState =
    { Animation: AnimationClock option
      Text: TextInputModel option }

// Feature 113 (Phase 5): the control-internal memoization seam types. `Dependency` is boxed so a
// single uniform cache holds heterogeneous sites; reuse is decided by F# structural `=`, never object
// identity (FR-005). `Subtree` is the stored `Scene list` (a reference type, so a Hit returns the same
// instance). Specialized to `Scene list` this rung (the DataGrid projection is the sole site).
type internal MemoOutcome =
    | Hit
    | Miss

type internal MemoEntry =
    { Dependency: obj
      Subtree: FS.GG.UI.Scene.Scene list }

type internal MemoCache = Map<ControlId, MemoEntry>

// Feature 116 (Phase 7): the picture cache's COMPLETE correctness key for one cacheable boundary —
// the node's box + a structural digest of its painted subtree (which embeds every render-affecting
// input). Compared by F# structural `=`.
type internal PictureCacheKey =
    { Box: FS.GG.UI.Scene.Rect option
      // Feature 120 (US3): the collision-resistant structural fingerprint (replaces the 116 `sprintf "%A"`).
      Fingerprint: uint64 }

// Feature 116 (Phase 7): the bounded cross-frame picture cache — a fixed-cap LRU over cacheable
// picture identities, each holding its last-seen key + a monotonic access stamp advanced by the
// frame's deterministic traversal order (no wall-clock). Over the cap the least-recently-accessed
// entry is dropped; a dropped identity re-misses when next needed.
type internal PictureCache =
    { Entries: Map<RetainedId, int * PictureCacheKey>
      Clock: int }

// Feature 117 (Phase 8, FR-002): the text-measure cache key — every input `Scene.measureText` reads.
type internal TextMeasureKey =
    { Text: string
      Family: string option
      Size: float
      Weight: int option }

// Feature 117 (Phase 8, FR-003): the bounded cross-frame text-measure cache — a fixed-cap LRU over
// measured text identities, each holding its measured `TextMetrics` + a monotonic access stamp advanced
// by measurement order (no wall-clock). Over the cap the least-recently-accessed entry is dropped; a
// dropped key re-misses when next needed.
type internal TextMeasureCache =
    { Entries: Map<TextMeasureKey, int * FS.GG.UI.Scene.TextMetrics>
      Clock: int }

type internal RetainedRender<'msg> =
    { Root: RetainedNode<'msg>
      NextId: uint64
      StateByIdentity: Map<RetainedId, RetainedUiState>
      Theme: Theme
      // Feature 113 (Phase 5): the per-identity memo store carried frame-to-frame (FR-003/FR-004).
      Memo: MemoCache
      // Feature 113 (Phase 5): the always-miss switch (FR-008); `true` on the live path.
      MemoEnabled: bool
      // Feature 097 (R2): previous frame's full LayoutResult — the measure/bounds cache (FR-002).
      Layout: FS.GG.UI.Layout.LayoutResult
      // Feature 116 (Phase 7): the bounded cross-frame picture cache (FR-009/FR-010).
      PictureCache: PictureCache
      // Feature 116 (Phase 7): the picture-cache always-miss switch (FR-007); `true` on the live path.
      PictureCacheEnabled: bool
      // Feature 117 (Phase 8): the bounded cross-frame text-measure cache (FR-001/FR-003).
      TextCache: TextMeasureCache
      // Feature 117 (Phase 8): the text-cache always-miss switch (FR-004); `true` on the live path.
      TextCacheEnabled: bool }

type internal WorkReductionRecord =
    { BaselineNodeCount: int
      RecomputedNodeCount: int
      ChangedSubtreeBound: int
      ShiftedNodeCount: int
      // Feature 097 (R2, FR-006): nodes actually re-measured this frame (post-propagation dirty set).
      RemeasuredNodeCount: int
      // Feature 113 (Phase 5, FR-009/FR-010): memoizable-control reuse outcomes this frame.
      MemoHits: int
      MemoMisses: int
      // Feature 114 (Phase 6, FR-013): materialized data-grid-row nodes + logical row total this frame.
      VirtualMaterialized: int
      VirtualTotal: int
      // Feature 116 (Phase 7, FR-001/FR-004): the damage set — repainted-node count, distinct dirty-rect
      // count, summed integer dirty area.
      RepaintedNodeCount: int
      DirtyRectCount: int
      DirtyArea: int
      // Feature 116 (Phase 7, FR-005/FR-009/FR-010): picture-cache hits, misses, and live entry count.
      PictureCacheHits: int
      PictureCacheMisses: int
      PictureCacheEntryCount: int
      // Feature 117 (Phase 8, FR-001/FR-005): per-frame text-measure cache hits + misses.
      TextMeasureCacheHits: int
      TextMeasureCacheMisses: int
      // Feature 117 (Phase 8, FR-006): the pre-pinning layout dirty-set size (<= RemeasuredNodeCount).
      LayoutInvalidatedNodeCount: int
      // Feature 120 (US3, FR-014): backend replay-cache per-frame outcomes (deterministic model).
      ReplayHits: int
      ReplayMisses: int
      ReplayRecords: int
      ReplaySkippedNodes: int
      ReplayCacheNativeBytes: int }

type internal RetainedRenderStep<'msg> =
    { Retained: RetainedRender<'msg>
      Render: ControlRenderResult<'msg>
      Diagnostics: ControlDiagnostic list
      WorkReduction: WorkReductionRecord }

type internal RetainedInit<'msg> =
    { Retained: RetainedRender<'msg>
      Render: ControlRenderResult<'msg>
      Diagnostics: ControlDiagnostic list }

module internal RetainedRender =

    let childPath (path: string) (index: int) = path + "." + string index

    // ---------------------------------------------------------------------------------------------
    // Feature 113 (Phase 5) — the control-internal memoization seam. Pure + total + deterministic:
    // reuse is decided ONLY by F# structural equality on the boxed dependency value (never object
    // identity, never a clock). A Hit returns the stored subtree instance without running `compute`.
    // ---------------------------------------------------------------------------------------------

    let memoize
        (id: ControlId)
        (dependency: obj)
        (compute: unit -> Scene list)
        (cache: MemoCache)
        : Scene list * MemoCache * MemoOutcome =
        match Map.tryFind id cache with
        // Hit: an entry exists AND its dependency compares EQUAL (structural `=`); reuse the stored
        // subtree instance WITHOUT running `compute` (contract C1/C3).
        | Some entry when entry.Dependency = dependency -> entry.Subtree, cache, Hit
        // Miss: no entry, or an unequal/unknown dependency — run `compute`, store keyed by id + dep
        // (contract C2/C3). Never reuses across an unequal dependency (FR-001/FR-005).
        | _ ->
            let result = compute ()
            result, Map.add id { Dependency = dependency; Subtree = result } cache, Miss

    /// Feature 113 (Phase 5): the sole memoized site this rung — the DataGrid row/column projection
    /// (`Control.fs` `gridGeom`), reached as a `data-grid` LEAF node's own paint. A node is memoizable
    /// iff it is a childless `data-grid`. The dependency captures every input that projection reads —
    /// the theme, the evaluated box, and the resolved cells (`ControlInternals.dataGridCells`) — so an
    /// equal dependency guarantees a byte-identical projection (FR-006) and any real input change shifts
    /// it to a Miss (FR-007). Boxed to `obj` at the seam boundary; compared by structural `=`.
    let private isMemoizable (c: Control<'msg>) =
        c.Kind = "data-grid" && List.isEmpty c.Children

    let private memoDependency
        (theme: Theme)
        (boundsById: Map<string, FS.GG.UI.Layout.LayoutBounds>)
        (path: string)
        (c: Control<'msg>)
        : obj =
        // A tuple is a reference type, so the upcast yields a non-null `obj` (satisfies nullness); it is
        // compared only by structural `=` in `memoize`, never by reference.
        (theme, ControlInternals.nodeBox boundsById path c, ControlInternals.dataGridCells c) :> obj

    // ---------------------------------------------------------------------------------------------
    // Feature 116 (Phase 7) — the bounded picture cache + the offscreen-effect detector. The picture
    // cache is the data-grid-ROW analog of feature 113's data-grid-only memo cache: each materialized
    // row is one cacheable picture. The bounded LRU + hit/miss counting OBSERVE the row pictures the
    // step already built — they are DECOUPLED from scene emission, so the emitted `SubtreeScene`, the
    // 091–114 reuse behaviour, and every prior work-reduction count are untouched (additive only,
    // byte-identical at rest).
    // ---------------------------------------------------------------------------------------------

    /// The fixed picture-cache entry cap (FR-009). Sits above a small grid's stable-row count and below
    /// the eviction-pressure scenario (320 distinct rows = 1.25 × cap), so the bound is exercised
    /// without spuriously evicting the small scenes.
    let PictureCacheCap = 256

    /// Feature 117 (Phase 8, FR-003): the fixed text-measure-cache entry cap (aligned with
    /// `PictureCacheCap`). `TextCache.Entries.Count` never exceeds this; the eviction-pressure scenario
    /// drives more distinct strings than the cap to prove bounded memory + deterministic LRU eviction.
    let TextMeasureCacheCap = 256

    // Feature 117 (Phase 8, FR-001/FR-002/FR-003/FR-004): the pure, total text-measure cache lookup.
    // `Scene.measureText` is a pure function of `(text, font)`, so the cached value EQUALS the un-cached
    // value for every key (research R5) — the cache is a transparent accelerator. A resident key returns
    // its stored `TextMetrics` WITHOUT re-invoking `Scene.measureText` (a hit), bumps its recency stamp,
    // and returns the advanced cache; an absent/evicted key measures fresh, inserts (evicting the
    // least-recently-used entry deterministically over the cap), and returns it (a miss). When `enabled`
    // is `false` (the always-miss oracle, FR-004) every request re-measures and is a miss, never
    // consulting/populating the cache — proving cache-on ≡ cache-off. Returns `(metrics, advanced cache,
    // wasHit)`. Deterministic: the recency stamp (`Clock`) advances by measurement order, never a clock.
    let internal measureTextCached
        (cache: TextMeasureCache)
        (enabled: bool)
        (text: string)
        (font: FS.GG.UI.Scene.FontSpec)
        : FS.GG.UI.Scene.TextMetrics * TextMeasureCache * bool =
        if not enabled then
            FS.GG.UI.Scene.Scene.measureText text font, cache, false
        else
            let key: TextMeasureKey =
                { Text = text; Family = font.Family; Size = font.Size; Weight = font.Weight }

            match Map.tryFind key cache.Entries with
            | Some(_, metrics) ->
                let clock = cache.Clock + 1
                metrics, { cache with Entries = Map.add key (clock, metrics) cache.Entries; Clock = clock }, true
            | None ->
                let metrics = FS.GG.UI.Scene.Scene.measureText text font
                let clock = cache.Clock + 1
                let mutable entries = Map.add key (clock, metrics) cache.Entries

                while entries.Count > TextMeasureCacheCap do
                    let lruKey, _ = entries |> Map.toSeq |> Seq.minBy (fun (_, (stamp, _)) -> stamp)
                    entries <- Map.remove lruKey entries

                metrics, { cache with Entries = entries; Clock = clock }, false

    // A cacheable picture boundary: a materialized data-grid row (the row analog of the 113 data-grid
    // memo site). Each row's painted picture is cached and reused when its full correctness key is
    // unchanged AND its entry is still resident.
    let private isCacheablePicture (c: Control<'msg>) = c.Kind = "data-grid-row"

    // Feature 120 (US3, FR-008/FR-010): the collision-resistant structural fingerprint of a painted
    // scene. An FNV-1a fold over a deterministic walk: the UNBOUNDED dimension (the node tree AND every
    // node's intrinsic list payload — points, vertices, chart values) is recursed/iterated element by
    // element, so NOTHING is truncated (the defect of the superseded `sprintf "%A"` over the whole
    // subtree, which truncates sequences past ~100 elements and so collides on long lists). Each small
    // fixed leaf payload (Color/Rect/Paint/Font) is folded via `%A` (complete for a bounded record). A
    // distinct tag per case keeps structurally-different shapes apart. Pure, total, deterministic — equal
    // scenes hash equal, any render-affecting change flips the value. Exhaustive over `SceneNode`.
    let hashScene (scenes: FS.GG.UI.Scene.Scene list) : uint64 =
        let mutable h = 0xcbf29ce484222325UL // mutable: hot path / FNV-1a accumulator
        let prime = 0x100000001b3UL
        let mix (x: uint64) = h <- (h ^^^ x) * prime
        let bits (d: float) = uint64 (System.BitConverter.DoubleToInt64Bits d)

        let mixStr (s: string) =
            mix (uint64 s.Length)
            for c in s do
                mix (uint64 (uint16 c))

        let mixA (v: 'a) = mixStr (sprintf "%A" v)
        let mixTag (t: int) = mix (uint64 (uint32 t))

        let rec goNodes (nodes: SceneNode list) =
            mixTag 0xA1
            mix (uint64 (List.length nodes))
            nodes |> List.iter goNode
            mixTag 0xA2

        and goScene (s: Scene) = goNodes s.Nodes

        and goNode (node: SceneNode) =
            match node with
            | Empty -> mixTag 1
            | Group scenes ->
                mixTag 2
                mix (uint64 (List.length scenes))
                scenes |> List.iter goScene
            | Rectangle(b, c) ->
                mixTag 3
                mixA b
                mixA c
            | PaintedRectangle(r, p) ->
                mixTag 4
                mixA r
                mixA p
            | Circle(ctr, rad, fill) ->
                mixTag 5
                mixA ctr
                mix (bits rad)
                mixA fill
            | FilledEllipse(b, fill) ->
                mixTag 6
                mixA b
                mixA fill
            | Ellipse(r, p) ->
                mixTag 7
                mixA r
                mixA p
            | Line(a, b, p) ->
                mixTag 8
                mixA a
                mixA b
                mixA p
            | Path(spec, p) ->
                mixTag 9
                mixA spec
                mixA p
            | Points(pts, p) ->
                mixTag 10
                mix (uint64 (List.length pts))
                pts |> List.iter mixA
                mixA p
            | Vertices(m, vs, p) ->
                mixTag 11
                mixA m
                mix (uint64 (List.length vs))
                vs |> List.iter mixA
                mixA p
            | Arc(r, sa, ea, p) ->
                mixTag 12
                mixA r
                mix (bits sa)
                mix (bits ea)
                mixA p
            | Text((x, y), t, c) ->
                mixTag 13
                mix (bits x)
                mix (bits y)
                mixStr t
                mixA c
            | TextRun run ->
                mixTag 14
                mixStr run.Text
                mixA run.Position
                mixA run.Font
                mixA run.Paint
            | Image((x, y, w, ht), src) ->
                mixTag 15
                mix (bits x)
                mix (bits y)
                mix (bits w)
                mix (bits ht)
                mixStr src
            | ClipNode(clip, scene) ->
                mixTag 16
                mixA clip
                goScene scene
            | RegionNode(region, p) ->
                mixTag 17
                mixA region
                mixA p
            | ColorSpaceNode(cs, scene) ->
                mixTag 18
                mixA cs
                goScene scene
            | PerspectiveNode(t, scene) ->
                mixTag 19
                mixA t
                goScene scene
            | PictureNode picture ->
                mixTag 20
                mixStr picture.Name
                goScene picture.Scene
            | Chart values ->
                mixTag 21
                mix (uint64 (List.length values))
                values |> List.iter (bits >> mix)
            | Translate((dx, dy), scene) ->
                mixTag 22
                mix (bits dx)
                mix (bits dy)
                goScene scene
            | SizedText((x, y), t, size, c) ->
                mixTag 23
                mix (bits x)
                mix (bits y)
                mixStr t
                mix (bits size)
                mixA c
            // Feature 120 (FR-007): transparent — hash the wrapped subtree's content.
            | CachedSubtree boundary ->
                mixTag 24
                goScene boundary.Scene

        mix (uint64 (List.length scenes))
        scenes |> List.iter goScene
        h

    // The COMPLETE correctness key for a cacheable boundary: the node's evaluated box + the
    // collision-resistant structural fingerprint of its painted subtree (feature 120, FR-008 — replacing
    // the feature-116 truncation-prone `sprintf "%A"`). It embeds EVERY render-affecting input (theme
    // colours, clip, opacity, transform, font/text, visual-state) by construction, so an equal key proves
    // a hit is byte-identical to a fresh paint and any single changed input forces a miss. The fingerprint
    // is the one already memoized on the fragment (cost ∝ damage, not tree size).
    let private pictureKeyOf (n: RetainedNode<'msg>) : PictureCacheKey =
        { Box = n.Fragment.Box
          Fingerprint = n.Fragment.Fingerprint }

    // Feature 120 (US4, FR-015): the integer area of the UNION of a set of damage rectangles (no longer
    // the sum), clamped to the frame area. Coordinate-compression over the distinct boxes: each elementary
    // cell of the x/y grid is counted once iff any box covers it — so overlapping damage is not
    // double-counted and the result never exceeds the frame. `n` is the small dirty-rect count; integer
    // control geometry → deterministic.
    let unionArea (boxes: FS.GG.UI.Scene.Rect list) (frameArea: int) : int =
        match boxes with
        | [] -> 0
        | boxes ->
            let xs = boxes |> List.collect (fun b -> [ b.X; b.X + b.Width ]) |> List.distinct |> List.sort
            let ys = boxes |> List.collect (fun b -> [ b.Y; b.Y + b.Height ]) |> List.distinct |> List.sort
            let mutable area = 0.0 // mutable: hot path / union accumulator

            for i in 0 .. xs.Length - 2 do
                let x0, x1 = xs.[i], xs.[i + 1]

                for j in 0 .. ys.Length - 2 do
                    let y0, y1 = ys.[j], ys.[j + 1]

                    let covered =
                        boxes
                        |> List.exists (fun b -> b.X <= x0 && x1 <= b.X + b.Width && b.Y <= y0 && y1 <= b.Y + b.Height)

                    if covered then
                        area <- area + (x1 - x0) * (y1 - y0)

            min (int area) frameArea

    /// Feature 116 (FR-011): scan a node's own painted scene for an effect that forces OFFSCREEN
    /// composition (a separate layer + composite). In THIS renderer that is: a drop-shadow / image
    /// filter (`DropShadow`, lowered to `SKImageFilter.CreateDropShadow`); a path clip (`PathClip` — a
    /// `RectClip` lowers to the cheap `canvas.ClipRect` with no layer, and is the ubiquitous label clip,
    /// so it is intentionally NOT flagged); or a non-opaque paint over a multi-node group (which a
    /// layered backend composites through a `SaveLayer`). Returns the effect name (for the advisory
    /// message) or `None`. Pure; reads only the lowered scene. Advisory only — never alters output.
    let offscreenEffect (ownScene: FS.GG.UI.Scene.Scene list) : string option =
        let mutable sawPathClip = false
        let mutable sawShadow = false
        let mutable sawLowOpacity = false
        let mutable painted = 0

        let rec go (nodes: SceneNode list) =
            for node in nodes do
                match node with
                | ClipNode(clip, s) ->
                    (match clip with
                     | PathClip _ -> sawPathClip <- true
                     | RectClip _ -> ())
                    go s.Nodes
                | Group ss -> ss |> List.iter (fun s -> go s.Nodes)
                | Translate(_, s)
                | ColorSpaceNode(_, s)
                | PerspectiveNode(_, s) -> go s.Nodes
                | PictureNode pic -> go pic.Scene.Nodes
                | PaintedRectangle(_, p)
                | Ellipse(_, p)
                | Line(_, _, p)
                | Path(_, p)
                | Points(_, p)
                | Vertices(_, _, p)
                | Arc(_, _, _, p)
                | RegionNode(_, p) ->
                    painted <- painted + 1

                    match p.ImageFilter with
                    | DropShadow _ -> sawShadow <- true
                    | _ -> ()

                    if p.Opacity < 1.0 then
                        sawLowOpacity <- true
                | _ -> ()

        ownScene |> List.iter (fun s -> go s.Nodes)

        if sawShadow then Some "drop-shadow"
        elif sawPathClip then Some "path clip"
        elif sawLowOpacity && painted > 1 then Some "opacity group"
        else None

    // ---------------------------------------------------------------------------------------------
    // Feature 099 (R4) — the per-identity animation clock core. Pure + total + deterministic: every
    // function below depends ONLY on its arguments (no `Date.now`, no randomness, resume-safe). The
    // feature-073 `Animation`/`applyAt`/`isSettled` primitives are REUSED, not re-implemented.
    // ---------------------------------------------------------------------------------------------

    /// The single pinned framework default transition (research §R4 / data-model constant): a short
    /// 150 ms `EaseOut` settle on the opacity channel. A fixed value, not a per-control knob, so the
    /// determinism goldens reach the settled end after the same fixed frame count.
    let defaultTransitionDuration = System.TimeSpan.FromMilliseconds 150.0

    // The longest tween duration carried by an animation (the point past which it is settled).
    let clockDuration (anim: FS.GG.UI.Scene.Animation) : System.TimeSpan =
        [ anim.Opacity |> Option.map (fun t -> t.Duration)
          anim.Transform |> Option.map (fun t -> t.Duration)
          anim.Color |> Option.map (fun t -> t.Duration) ]
        |> List.choose id
        |> function
            | [] -> System.TimeSpan.Zero
            | ds -> List.max ds

    // The default fade-in animation: opacity travels from `startOpacity` to fully-shown (1.0) over
    // the framework default, eased out. End = 1.0 means a settled clock samples to opacity 1.0, so
    // `applyAt`'s identity-at-rest lowering makes the converged frame byte-identical to the static
    // render of the (now-stamped) state — FR-005 holds by construction.
    let fadeAnimation (startOpacity: float) : FS.GG.UI.Scene.Animation =
        { FS.GG.UI.Scene.Animation.empty with
            Opacity =
                Some
                    { Start = startOpacity
                      End = 1.0
                      Duration = defaultTransitionDuration
                      Easing = FS.GG.UI.Scene.EaseOut } }

    // Feature 103 (R6): the prior-snapshot fade-OUT — opacity travels 1.0 → 0.0 over the same
    // framework default + easing as the fade-in, so the two layers cross at the eased midpoint. Drives
    // the `From` snapshot UNDER the next own-scene in `sampleOnPaint`. Because both layers share the
    // eased curve and lerp is linear, the fade-out is exactly the complement of the fade-in.
    let private fadeOutAnimation: FS.GG.UI.Scene.Animation =
        { FS.GG.UI.Scene.Animation.empty with
            Opacity =
                Some
                    { Start = 1.0
                      End = 0.0
                      Duration = defaultTransitionDuration
                      Easing = FS.GG.UI.Scene.EaseOut } }

    // The clock's current sampled opacity (the displayed value a mid-flight retarget continues from).
    let currentOpacity (clock: AnimationClock) : float =
        match clock.Anim.Opacity with
        | Some tween -> FS.GG.UI.Scene.Tween.sample FS.GG.UI.Scene.Animation.lerpFloat clock.Elapsed tween
        | None -> 1.0

    let clockActive (clock: AnimationClock) : bool =
        not (FS.GG.UI.Scene.Animation.isSettled clock.Elapsed clock.Anim)

    let advance (delta: System.TimeSpan) (clock: AnimationClock) : AnimationClock =
        // Non-positive delta is a designed no-op — never rewinds (the host never emits these). A
        // positive delta accumulates Elapsed CLAMPED to the duration, so a very-large delta settles
        // at the end (no overshoot) and the settled state is canonical (determinism of state, FR-006).
        if delta <= System.TimeSpan.Zero then
            clock
        else
            let dur = clockDuration clock.Anim
            let e = clock.Elapsed + delta
            { clock with Elapsed = (if e > dur then dur else e) }

    let advanceStateClocks (delta: System.TimeSpan) (state: Map<RetainedId, RetainedUiState>) : Map<RetainedId, RetainedUiState> =
        // Feature 121 (US2, FR-004): only rebuild the per-identity map when at least one clock is active.
        // An all-inactive state is returned reference-equal — an idle live tick allocates nothing (the
        // prior `Map.map` allocated a fresh map every tick regardless). Active clocks advance exactly as
        // `advance` (features 099/103 unchanged).
        if state |> Map.exists (fun _ s -> s.Animation |> Option.exists clockActive) then
            state
            |> Map.map (fun _ s -> { s with Animation = s.Animation |> Option.map (advance delta) })
        else
            state

    let updateClockForState (desired: VisualState) (priorOwn: FS.GG.UI.Scene.Scene list) (carried: AnimationClock option) : AnimationClock option =
        // Compare the desired (stamped) VisualState against the carried clock's Target (contract C2).
        let triggered =
            match carried, desired with
            // At rest and staying at rest: no clock.
            | None, Normal -> None
            // Same state as the clock is already animating toward: advance-only (no retarget). A
            // settled same-state clock is KEPT (Target ≠ Normal) so a held state does not re-fire. The
            // existing `From` snapshot is retained (the layer the next own-scene is still crossing from).
            | Some c, d when d = c.Target -> Some c
            // The state changed (or first entry into a non-Normal state). Mid-flight ⇒ retarget from
            // the current sampled value (no snap to start); a settled/absent clock ⇒ a fresh fade-in.
            // Feature 103 (R6): `From = priorOwn` — the matched prior node's own-scene snapshot. On a
            // fresh transition this is the prior state's static paint; on a mid-flight retarget it is
            // the previous target's static paint (the layer that was fading in becomes the one fading
            // out), so the cross-fade never snaps to a stale at-rest endpoint (FR-001/FR-007).
            | _ ->
                let startOpacity =
                    match carried with
                    | Some c when clockActive c -> currentOpacity c
                    | _ -> 0.0

                Some
                    { Anim = fadeAnimation startOpacity
                      Elapsed = System.TimeSpan.Zero
                      Target = desired
                      From = priorOwn }

        // A settled return-to-Normal clock is DROPPED so the identity returns to byte-identical
        // at-rest output (resolves the FR-003 vs FR-005 interaction); a settled non-Normal clock is
        // kept (its sampled opacity 1.0 lowers byte-identically via `applyAt`, and keeping it
        // suppresses a spurious re-fire while the state is held).
        match triggered with
        | Some c when (not (clockActive c)) && c.Target = Normal -> None
        | other -> other

    let sampleOnPaint (clock: AnimationClock) (ownScene: FS.GG.UI.Scene.Scene list) : FS.GG.UI.Scene.Scene list =
        // Feature 103 (R6): a genuine cross-fade — composite two opacity-driven layers via the public
        // feature-073 `Animation.applyAt` (paint-level only; opacity, never layout). The prior state's
        // static `From` snapshot fades OUT (1→0) UNDER this frame's static `ownScene` fading IN (via
        // the clock's own opacity tween). For a region painted in both states the source-over composite
        // displays a colour STRICTLY BETWEEN the two endpoints (SC-001) — not the old fade-in from
        // transparent (which can only grow paint). `From = []` (first entry / no prior paint)
        // degenerates to the plain next-fades-in case — a safe degenerate, not a special path.
        let priorLayer =
            match clock.From with
            | [] -> []
            | nodes -> [ FS.GG.UI.Scene.Animation.applyAt clock.Elapsed fadeOutAnimation (FS.GG.UI.Scene.Scene.group nodes) ]

        let nextLayer =
            match ownScene with
            | [] -> []
            | nodes -> [ FS.GG.UI.Scene.Animation.applyAt clock.Elapsed clock.Anim (FS.GG.UI.Scene.Scene.group nodes) ]

        match priorLayer @ nextLayer with
        | [] -> []
        | layers -> [ { Nodes = layers } ]

    // FR-009: detect duplicate sibling keys present in the FIRST tree, mirroring the collision the
    // 067 `Reconcile.diff` reports from frame 1 (same shape/message), so a malformed first frame is
    // reported on frame 0 instead of a frame late. First occurrence wins; later dups are collisions.
    let private firstFrameCollisions (control: Control<'msg>) : ControlDiagnostic list =
        let diags = ResizeArray<ControlDiagnostic>()

        let rec walk (c: Control<'msg>) =
            let seen = System.Collections.Generic.HashSet<ControlId>()

            for child in c.Children do
                match child.Key with
                | Some k ->
                    if not (seen.Add k) then
                        diags.Add
                            { ControlId = Some k
                              ControlKind = c.Kind
                              Code = KeyCollision
                              Severity = ControlDiagnosticSeverity.Warning
                              Message =
                                sprintf "Duplicate key '%s' within the children of a '%s' node; first occurrence wins." k c.Kind
                              EvidencePath = None }
                | None -> ()

            for child in c.Children do
                walk child

        walk control
        List.ofSeq diags

    let init (theme: Theme) (size: FS.GG.UI.Scene.Size) (control: Control<'msg>) : RetainedInit<'msg> =
        let layoutRoot, boundsById, layoutResult = ControlInternals.evaluateLayout size control

        let mutable nextId = 0UL

        let mint () =
            let id = RetainedId nextId
            nextId <- nextId + 1UL
            id

        // Feature 113 (Phase 5): seed the memo cache on the first frame. Every memoizable node is a
        // cold miss here (an empty cache), so the projection runs once and is stored; subsequent
        // `step` frames consult it. The first frame reports no metrics (init carries no work record).
        let mutable memo: MemoCache = Map.empty

        let paintOwn (path: string) (nc: Control<'msg>) : Scene list =
            if isMemoizable nc then
                let dep = memoDependency theme boundsById path nc
                let id = nc.Key |> Option.defaultValue path
                let subtree, memo', _ = memoize id dep (fun () -> ControlInternals.paintNode theme boundsById path nc) memo
                memo <- memo'
                subtree
            else
                ControlInternals.paintNode theme boundsById path nc

        let rec build (path: string) (nc: Control<'msg>) : RetainedNode<'msg> =
            let own = paintOwn path nc
            let children = nc.Children |> List.mapi (fun i child -> build (childPath path i) child)
            let subtree = own @ (children |> List.collect (fun c -> c.Fragment.SubtreeScene))

            { Identity = mint ()
              Control = nc
              Fragment =
                { OwnScene = own
                  SubtreeScene = subtree
                  Box = ControlInternals.nodeBox boundsById path nc
                  Fingerprint = hashScene subtree }
              Children = children }

        let root = build "0" control

        // Feature 116 (Phase 7): seed the bounded picture cache from the first frame's cacheable
        // boundaries (every data-grid row) — all cold here, so a subsequent `step` whose row pictures
        // are unchanged finds them resident and reports hits. Bounded from creation (FR-009): a
        // first tree with more than the cap of cacheable rows evicts LRU (by deterministic
        // first-seen/traversal order) immediately.
        let mutable pcEntries: Map<RetainedId, int * PictureCacheKey> = Map.empty
        let mutable pcClock = 0

        let rec seedPictures (n: RetainedNode<'msg>) =
            if isCacheablePicture n.Control then
                pcClock <- pcClock + 1
                pcEntries <- Map.add n.Identity (pcClock, pictureKeyOf n) pcEntries

                while pcEntries.Count > PictureCacheCap do
                    let lruId, _ = pcEntries |> Map.toSeq |> Seq.minBy (fun (_, (stamp, _)) -> stamp)
                    pcEntries <- Map.remove lruId pcEntries

            n.Children |> List.iter seedPictures

        seedPictures root

        // Paint the first frame ONCE: the Scene IS the root's pre-order SubtreeScene (the same list
        // `Control.renderTree`'s `paint "0"` builds), so this `Render` is byte-identical to a full
        // rebuild — the adapter reuses it instead of calling `Control.renderTree` a second time.
        let render: ControlRenderResult<'msg> =
            { Scene = root.Fragment.SubtreeScene |> Scene.group
              Layout = layoutRoot
              Bounds = ControlInternals.collectBoundsWith boundsById control
              Diagnostics = Control.diagnostics control
              EventBindings = ControlInternals.eventBindingsOf control
              BoundIds = ControlInternals.boundIdsOf control
              NodeCount = Control.count control }

        { Retained =
            { Root = root
              NextId = nextId
              StateByIdentity = Map.empty
              Theme = theme
              Memo = memo
              MemoEnabled = true
              Layout = layoutResult
              PictureCache = { Entries = pcEntries; Clock = pcClock }
              PictureCacheEnabled = true
              // Feature 117 (Phase 8): seed the text-measure cache EMPTY. `init` measures uncached (no
              // hook installed), byte-identical to pre-117, so the FIRST `step` starts cold (misses) and a
              // subsequent unchanged-text `step` reports hits (cold → warm, SC-001/SC-002).
              TextCache = { Entries = Map.empty; Clock = 0 }
              TextCacheEnabled = true }
          Render = render
          Diagnostics = firstFrameCollisions control }

    /// Feature 097 (R2, contract C2/C3): derive the layout-dirty set from the reconcile patch, in the
    /// `LayoutNodeId` (`Key |> defaultValue path`) domain `toLayout`/`evaluateIncremental` use. A node
    /// is self-dirty iff its `Update` sets/removes an `AttrCategory.Layout` attribute, sets/removes a
    /// geometry-driving NAME in `ControlInternals.layoutAffectingAttrNames`, OR carries a non-`Keep`
    /// child op (`ChildInsert`/`ChildRemove`/`ChildMove`); a `Replace` re-measures fresh. That name set
    /// is a SEPARATE hot-path `Set` from the names `toLayout` actually reads — not auto-derived from
    /// them (feature 101 / R7): the two are kept in lock-step by the behavioral-probe equality gate in
    /// `tests/Controls.Tests/Feature101LayoutDriftGuardTests.fs`, which fails the build the instant they
    /// drift in either direction. The `AttrCategory.Layout` channel here is honoured independently of
    /// the name set. Pure walk over (prev, patch, next) in parallel; conservative flex-line /
    /// fixed-size-ancestor propagation then happens inside `Layout.evaluateIncremental` (FR-004).
    let internal layoutDirtySet (prev: Control<'msg>) (patch: Reconcile.NodePatch<'msg>) (next: Control<'msg>) : Set<string> =
        let acc = System.Collections.Generic.HashSet<string>()

        let isLayout (c: AttrCategory) = c = AttrCategory.Layout

        let rec walk (path: string) (p: Control<'msg>) (patch: Reconcile.NodePatch<'msg>) (n: Control<'msg>) =
            let id = n.Key |> Option.defaultValue path

            match patch with
            | Reconcile.NodePatch.Keep -> ()
            | Reconcile.NodePatch.Replace _ ->
                // A Kind/Key change replaces the subtree: re-measure it fresh under its boundary.
                acc.Add id |> ignore
            | Reconcile.NodePatch.Update u ->
                let isGeometry (name: string) =
                    Set.contains name ControlInternals.layoutAffectingAttrNames

                let attrDirty =
                    u.AttrChanges
                    |> List.exists (fun ch ->
                        match ch with
                        // Geometry-driving NAME (the single source `toLayout` reads) OR a Layout-tagged
                        // category (future-proof). A content/style/state/visual-state change is neither,
                        // so it does not dirty measure (SC-004).
                        | Reconcile.AttrSet attr -> isLayout attr.Category || isGeometry attr.Name
                        | Reconcile.AttrRemoved name ->
                            isGeometry name
                            // Category recovered from the PREV node's attribute (the removed one).
                            || (p.Attributes |> List.exists (fun a -> a.Name = name && isLayout a.Category)))

                let childOpDirty =
                    u.Children
                    |> List.exists (fun op ->
                        match op with
                        | Reconcile.ChildKeep _ -> false
                        | _ -> true)

                if attrDirty || childOpDirty then
                    acc.Add id |> ignore

                // Recurse into the producing ops (every op except ChildRemove), zipped with next order.
                let producing =
                    u.Children
                    |> List.filter (fun op ->
                        match op with
                        | Reconcile.ChildRemove _ -> false
                        | _ -> true)

                List.map2 (fun op c -> op, c) producing n.Children
                |> List.iteri (fun i (op, c) ->
                    let cp = childPath path i

                    match op with
                    | Reconcile.ChildKeep(j, cpatch) -> walk cp p.Children.[j] cpatch c
                    | Reconcile.ChildMove(f, _, cpatch) -> walk cp p.Children.[f] cpatch c
                    | Reconcile.ChildInsert(_, _) -> acc.Add(n.Key |> Option.defaultValue path) |> ignore
                    | Reconcile.ChildRemove _ -> ())

        walk "0" prev patch next
        Set.ofSeq acc

    let step
        (theme: Theme)
        (size: FS.GG.UI.Scene.Size)
        (prev: RetainedRender<'msg>)
        (next: Control<'msg>)
        : RetainedRenderStep<'msg> =
        // (1) the diff — total; never throws; duplicate keys -> KeyCollision diagnostic (C1/C4).
        let result = Reconcile.diff prev.Root.Control next

        // (2) layout of `next` via the INCREMENTAL evaluator (R2, FR-005): re-measure only the
        //     patch-derived dirty set (conservatively propagated to its flex line / fixed-size
        //     ancestor) and reuse the previous frame's cached bounds for everything else. The result
        //     `Bounds` are byte-identical to a full `evaluateLayout` (INV-1), so the reuse-driven paint
        //     walk below (`box = pr.Fragment.Box`) and the surfaced Bounds are unchanged.
        let dirty = layoutDirtySet prev.Root.Control result.Patch next
        // FR-006: the size of the layout dirty set fed into incremental layout this frame (the
        // patch-derived self-dirty nodes BEFORE fixed-size-ancestor propagation). Distinct from the
        // post-pinning `remeasured` below; `invalidated <= remeasured` because propagation expands each
        // dirty node to its first fixed-size ancestor's whole subtree (a superset). `0` on an idle /
        // style-only / visual-state-only frame (no layout-affecting attr changed → empty dirty set).
        let invalidated = Set.count dirty

        // Feature 117 (Phase 8, FR-001/FR-003/FR-004): install the per-frame text-measure cache over THIS
        // frame's layout + paint measurement. A working copy of the carried cache + per-frame hit/miss
        // counters; the closure consults the cache (a resident key → hit, reused without re-invoking
        // `Scene.measureText`; an absent/evicted key → miss, measured once, stored, LRU-evicted over the
        // cap), or — when the `TextCacheEnabled` oracle is `false` — always re-measures and counts a miss
        // (never consulting/populating the cache), proving cache-on ≡ cache-off (FR-004). The cached value
        // EQUALS the un-cached value by construction (`Scene.measureText` is pure, research R5), so layout
        // boxes / fitted sizes / emitted scene are byte-identical. Interpreter-edge mutation confined to the
        // step (constitution III), exactly like the id/work counters and the 116 picture cache.
        let mutable tc = prev.TextCache
        let mutable textHits = 0
        let mutable textMisses = 0

        let measureCached (text: string) (font: FS.GG.UI.Scene.FontSpec) : FS.GG.UI.Scene.TextMetrics =
            let metrics, tc', wasHit = measureTextCached tc prev.TextCacheEnabled text font
            tc <- tc'
            if wasHit then textHits <- textHits + 1 else textMisses <- textMisses + 1
            metrics

        // Active for the WHOLE measurement window of this frame — the incremental layout pass AND the
        // reuse-driven paint walk below (`build` → `paintNode`/geom). Cleared right after `build` (nothing
        // past it measures text). `step` is total (the diff/layout/paint paths never throw), so the
        // explicit clear always runs; `ThreadStatic` isolates concurrent test `step`s.
        ControlInternals.setMeasureTextHook (Some measureCached)

        let root, boundsById, layoutResult = ControlInternals.evaluateLayoutIncremental size next prev.Layout dirty
        // FR-006: nodes actually re-measured this frame = the honest post-propagation set.
        let remeasured = layoutResult.Invalidated |> List.length

        // FR-008: a fragment caches paint produced under a specific theme. When the per-loop theme
        // changes between frames, NO cached fragment may be reused (it would show stale-theme
        // paint); every node repaints under the new theme. Theme is uniform per frame, so one
        // top-level comparison suffices — no per-fragment theme storage.
        let themeChanged = prev.Theme <> theme

        // Mutation confined to this interpreter-edge step (constitution III): a monotonic id
        // counter and the measured work counters. The consumer `view`/`update` stay pure.
        let mutable nextId = prev.NextId
        let mutable recomputed = 0
        let mutable changedBound = 0
        // FR-007: nodes recomputed ONLY because an upstream change relaid a structurally-unchanged
        // subtree out (a shifted `Keep`) or a theme change forced a repaint — counted distinctly
        // from genuinely-changed work so `RecomputedNodeCount = ChangedSubtreeBound + ShiftedNodeCount`.
        let mutable shifted = 0
        // Feature 113 (Phase 5): the memo cache carried from `prev`, advanced as memoizable nodes are
        // (re)painted this frame, plus the frame's hit/miss tally (FR-009/FR-010).
        let mutable memo = prev.Memo
        let mutable memoHits = 0
        let mutable memoMisses = 0
        // Feature 116 (Phase 7): the per-frame DAMAGE set — each repainted node (every `paintFresh`)
        // contributes its evaluated box (FR-001/FR-002). `RepaintedNodeCount` = repaint count;
        // `DirtyRectCount` = distinct boxes; `DirtyArea` = summed integer w*h over distinct boxes. An
        // idle (all-`Keep`) frame repaints nothing → `0/0/0`; a theme switch repaints every node.
        let repaintedBoxes = ResizeArray<Rect>()

        let mint () =
            let id = RetainedId nextId
            nextId <- nextId + 1UL
            id

        // Feature 113 (Phase 5): paint a node's OWN scene, routing the sole memoized site (the DataGrid
        // projection) through the memo seam. A HIT reuses the stored projection (its theme/box/cells
        // dependency was unchanged) without recomputing; a MISS recomputes and stores it. With
        // `MemoEnabled = false` (the always-miss oracle, FR-008) every node paints directly — nothing is
        // reused — so the rendered scene is byte-identical to the seam-active build (memo-on ≡ memo-off).
        let paintOwn (path: string) (nc: Control<'msg>) : FS.GG.UI.Scene.Scene list =
            if prev.MemoEnabled && isMemoizable nc then
                let dep = memoDependency theme boundsById path nc
                let id = nc.Key |> Option.defaultValue path
                let subtree, memo', outcome = memoize id dep (fun () -> ControlInternals.paintNode theme boundsById path nc) memo
                memo <- memo'

                match outcome with
                | Hit -> memoHits <- memoHits + 1
                | Miss -> memoMisses <- memoMisses + 1

                subtree
            else
                ControlInternals.paintNode theme boundsById path nc

        let paintFresh (path: string) (nc: Control<'msg>) : FS.GG.UI.Scene.Scene list =
            recomputed <- recomputed + 1
            // FR-001: a repainted node contributes its evaluated box to the damage set (`None` boxes
            // contribute no rectangle).
            match ControlInternals.nodeBox boundsById path nc with
            | Some b -> repaintedBoxes.Add b
            | None -> ()

            paintOwn path nc

        // Build a brand-new subtree (Replace / ChildInsert / fallback): mint fresh ids, paint
        // every node. Used where there is no matched prev node — so no false identity is retained.
        let rec buildFresh (path: string) (nc: Control<'msg>) : RetainedNode<'msg> =
            let own = paintFresh path nc
            let children = nc.Children |> List.mapi (fun i child -> buildFresh (childPath path i) child)
            let subtree = own @ (children |> List.collect (fun c -> c.Fragment.SubtreeScene))

            { Identity = mint ()
              Control = nc
              Fragment =
                { OwnScene = own
                  SubtreeScene = subtree
                  Box = ControlInternals.nodeBox boundsById path nc
                  Fingerprint = hashScene subtree }
              Children = children }

        // Recompute a structurally-identical subtree whose box SHIFTED (a `Keep` relaid out by an
        // upstream change) while CARRYING every node's prior identity — it is the same node.
        let rec carry (path: string) (pr: RetainedNode<'msg>) (nc: Control<'msg>) : RetainedNode<'msg> =
            shifted <- shifted + 1
            let own = paintFresh path nc

            let children =
                List.map2 (fun p c -> p, c) pr.Children nc.Children
                |> List.mapi (fun i (p, c) -> carry (childPath path i) p c)

            let subtree = own @ (children |> List.collect (fun c -> c.Fragment.SubtreeScene))

            { Identity = pr.Identity
              Control = nc
              Fragment =
                { OwnScene = own
                  SubtreeScene = subtree
                  Box = ControlInternals.nodeBox boundsById path nc
                  Fingerprint = hashScene subtree }
              Children = children }

        // The reuse-driven walk: produce the next retained node for `nc` under `patch`, matched
        // against the prev retained node `pr`.
        let rec build (path: string) (pr: RetainedNode<'msg>) (patch: Reconcile.NodePatch<'msg>) (nc: Control<'msg>) : RetainedNode<'msg> =
            match patch with
            | Reconcile.NodePatch.Keep ->
                let box = ControlInternals.nodeBox boundsById path nc

                if box = pr.Fragment.Box && not themeChanged then
                    // unchanged AND unshifted AND same theme: reuse the cached subtree verbatim
                    // (identity-at-rest: zero re-measure/re-paint, zero id churn, same RetainedId).
                    { pr with Control = nc }
                else
                    // an upstream layout change shifted this subtree, or the theme changed (FR-008):
                    // recompute under the new theme/box, carrying identities (the node is the same).
                    carry path pr nc

            | Reconcile.NodePatch.Replace _ ->
                // Kind/Key changed -> a different node. Mint a fresh identity; the old identity
                // (and its UI state) is dropped — no false identity across a Replace (SC-001 -).
                changedBound <- changedBound + Control.count nc
                buildFresh path nc

            | Reconcile.NodePatch.Update u ->
                let box = ControlInternals.nodeBox boundsById path nc

                // This node's OWN paint is unchanged when its own data (attrs/content) did not
                // change, its leaf/container shape did not flip, and its box did not move — then
                // `paintNode` would reproduce the cached `OwnScene` exactly, so reuse it.
                let ownUnchanged =
                    List.isEmpty u.AttrChanges
                    && u.ContentChange = Reconcile.Unchanged
                    && (List.isEmpty nc.Children = List.isEmpty pr.Control.Children)
                    && box = pr.Fragment.Box
                    && not themeChanged

                let own =
                    if ownUnchanged then
                        pr.Fragment.OwnScene
                    else
                        changedBound <- changedBound + 1
                        paintFresh path nc

                // Producing ops (every op except ChildRemove) are emitted one-per-next-child in
                // next order, so they zip with `nc.Children`.
                let producing =
                    u.Children
                    |> List.filter (fun op ->
                        match op with
                        | Reconcile.ChildRemove _ -> false
                        | _ -> true)

                let children =
                    List.map2 (fun op c -> op, c) producing nc.Children
                    |> List.mapi (fun i (op, c) ->
                        let cp = childPath path i

                        match op with
                        | Reconcile.ChildKeep (j, p) -> build cp pr.Children.[j] p c
                        | Reconcile.ChildMove (f, _, p) -> build cp pr.Children.[f] p c
                        | Reconcile.ChildInsert (_, node) ->
                            changedBound <- changedBound + Control.count node
                            buildFresh cp node
                        // Unreachable (ChildRemove is filtered out of `producing`); kept total —
                        // paint the next child fresh rather than throw.
                        | Reconcile.ChildRemove _ -> buildFresh cp c)

                let subtree = own @ (children |> List.collect (fun c -> c.Fragment.SubtreeScene))

                { Identity = pr.Identity
                  Control = nc
                  Fragment =
                    { OwnScene = own
                      SubtreeScene = subtree
                      Box = box
                      Fingerprint = hashScene subtree }
                  Children = children }

        let newRoot = build "0" prev.Root result.Patch next

        // Feature 117 (Phase 8): the measurement window is closed — nothing past the paint walk measures
        // text (the virtualization tally and picture-cache pass below read counts/digests only). Clear the
        // hook so any later `Control.renderTree` on this thread measures uncached again.
        ControlInternals.setMeasureTextHook None

        // Feature 114 (Phase 6, FR-013/FR-014): tally the frame's virtualization counts by a read-only
        // walk of the lowered `next` tree (no render effect). `VirtualMaterialized` counts materialized
        // `data-grid-row` nodes (the realized window); `VirtualTotal` sums the logical `Total` carried on
        // each `data-grid` node's `visibleRange` attr. Both stay 0 when no `data-grid` is present, and
        // aggregate across multiple grids in a frame.
        let mutable virtualMaterialized = 0
        let mutable virtualTotal = 0

        let rec countVirtual (c: Control<'msg>) =
            if c.Kind = "data-grid-row" then
                virtualMaterialized <- virtualMaterialized + 1
            elif c.Kind = "data-grid" then
                c.Attributes
                |> List.tryFind (fun a -> a.Name = AttrKeys.nameOf AttrKeys.VisibleRange)
                |> Option.iter (fun a ->
                    match a.Value with
                    | UntypedValue(:? VisibleRange as vr) -> virtualTotal <- virtualTotal + vr.Total
                    | _ -> ())

            c.Children |> List.iter countVirtual

        countVirtual next

        // Feature 116 (Phase 7, FR-001/FR-004): reduce the accumulated damage set to its three integer
        // carriers — repainted-node count, count of DISTINCT repainted boxes, and summed integer area
        // over the distinct boxes. Deterministic (integer geometry → reproducible across runs).
        let repaintedNodeCount = recomputed
        let distinctBoxes = repaintedBoxes |> Seq.distinct |> List.ofSeq
        let dirtyRectCount = List.length distinctBoxes
        // Feature 120 (US4, FR-015): the damage area is the area of the UNION of the distinct damage
        // rectangles (no longer the sum), so overlapping damage is counted once and the value never
        // exceeds the frame area. Computed by coordinate-compression over the distinct boxes (n is the
        // small dirty-rect count, integer control geometry → deterministic), then clamped to the frame.
        let frameArea = size.Width * size.Height
        let dirtyArea = unionArea distinctBoxes frameArea

        // Feature 116 (Phase 7, FR-005/FR-006/FR-007/FR-009/FR-010): the bounded picture cache. A
        // read-only walk over the new retained tree visits each cacheable boundary (a data-grid row)
        // and consults the cross-frame LRU carried from `prev`: a HIT is an identity whose entry is
        // resident AND whose full correctness key is unchanged (and the oracle is enabled); everything
        // else is a MISS (a changed key, a cold identity, or an evicted entry). Each visit refreshes
        // recency and may evict the least-recently-accessed entry over the cap. This OBSERVES the row
        // pictures the step already built — it never changes the emitted scene (byte-identical at rest)
        // nor any 091–114 work count.
        let mutable pcEntries = prev.PictureCache.Entries
        let mutable pcClock = prev.PictureCache.Clock
        let mutable pictureHits = 0
        let mutable pictureMisses = 0
        // Feature 120 (US3, FR-007/FR-012/FR-014): the backend replay cache is the load-bearing
        // realization of this same picture cache, so its hit/miss/record counts coincide with the
        // picture-cache outcomes by construction (same boundaries, same residency + new structural
        // fingerprint). A HIT is a reuse-stable boundary (resident with an unchanged key) — exactly the
        // FR-012 prior-frame-stability gate — and is the boundary we emit as a `CachedSubtree` so the
        // backend replays it; `replaySkippedNodes` sums the painted-node count of every replayed
        // boundary's subtree (the draw-call walk avoided); `replayNativeBytes` is the deterministic
        // model native-byte estimate of resident recorded pictures.
        let bytesPerNode = 64
        let replayHitIds = System.Collections.Generic.HashSet<RetainedId>()
        let mutable replaySkippedNodes = 0
        let mutable replayNativeBytes = 0

        let countNodes (scenes: FS.GG.UI.Scene.Scene list) =
            scenes |> List.sumBy (fun s -> List.length (FS.GG.UI.Scene.Scene.describe s))

        let rec walkPictures (n: RetainedNode<'msg>) =
            if isCacheablePicture n.Control then
                pcClock <- pcClock + 1
                let key = pictureKeyOf n

                let isHit =
                    prev.PictureCacheEnabled
                    && (match Map.tryFind n.Identity pcEntries with
                        | Some(_, prevKey) -> prevKey = key
                        | None -> false)

                if isHit then
                    pictureHits <- pictureHits + 1
                    // Reuse-stable boundary → emit + replay; tally the skipped painted nodes (SC-004).
                    replayHitIds.Add n.Identity |> ignore
                    replaySkippedNodes <- replaySkippedNodes + countNodes n.Fragment.SubtreeScene
                else
                    pictureMisses <- pictureMisses + 1

                // Native-byte model: every cacheable boundary resident after this frame holds a recorded
                // picture proportional to its subtree node count (bounded by the cap).
                if prev.PictureCacheEnabled then
                    replayNativeBytes <- replayNativeBytes + countNodes n.Fragment.SubtreeScene * bytesPerNode

                pcEntries <- Map.add n.Identity (pcClock, key) pcEntries

                while pcEntries.Count > PictureCacheCap do
                    let lruId, _ = pcEntries |> Map.toSeq |> Seq.minBy (fun (_, (stamp, _)) -> stamp)
                    pcEntries <- Map.remove lruId pcEntries

            n.Children |> List.iter walkPictures

        walkPictures newRoot
        let pictureEntryCount = pcEntries.Count
        // Bound the modeled native bytes by the cap (residency never exceeds PictureCacheCap entries).
        let replayCacheNativeBytes = min replayNativeBytes (PictureCacheCap * bytesPerNode * 64)
        let pictureCache: PictureCache = { Entries = pcEntries; Clock = pcClock }

        // Feature 116 (Phase 7, FR-011): the advisory offscreen-effect diagnostic. A read-only walk
        // surfaces, per node whose own paint forces offscreen composition, an advisory
        // `ControlDiagnostic` (Info) naming the control + the effect — appended to the step's existing
        // `Diagnostics` channel like `KeyCollision`. Never fails a build, never alters output.
        let offscreenDiags = ResizeArray<ControlDiagnostic>()

        let rec collectOffscreen (n: RetainedNode<'msg>) =
            match offscreenEffect n.Fragment.OwnScene with
            | Some effect ->
                offscreenDiags.Add
                    { ControlId = n.Control.Key
                      ControlKind = n.Control.Kind
                      Code = OffscreenComposition
                      Severity = ControlDiagnosticSeverity.Info
                      Message =
                        sprintf
                            "Control '%s' requires offscreen composition (%s); it allocates a separate layer + composite (a real backend cost and a cache-defeating boundary)."
                            n.Control.Kind
                            effect
                      EvidencePath = None }
            | None -> ()

            n.Children |> List.iter collectOffscreen

        collectOffscreen newRoot

        // Re-key UI state to the STABLE identities still live this frame AND compute this frame's
        // animation clocks (R4). Walking `newRoot` is the GC: only live identities carry state, so a
        // removed identity's clock/text is dropped with the rest of its state (FR-007, no new GC
        // code). For each live identity, the carried clock (already advanced by the host Tick wrapper)
        // is started/retargeted/dropped from the stamped `VisualState` via `updateClockForState`
        // (R1 → R4 trigger); carried text is preserved unchanged.
        // Feature 103 (R6): index the PREVIOUS frame's own-scene snapshot by stable identity, so a
        // fresh transition / retarget can capture the prior state's static paint as the clock's `From`
        // (the layer it cross-fades FROM). A node minted fresh this frame has no prior identity ⇒ no
        // `From` ⇒ a plain fade-in.
        let priorOwnById = System.Collections.Generic.Dictionary<RetainedId, Scene list>()

        let rec indexPriorOwn (n: RetainedNode<'msg>) =
            priorOwnById.[n.Identity] <- n.Fragment.OwnScene
            n.Children |> List.iter indexPriorOwn

        indexPriorOwn prev.Root

        let rec collect (n: RetainedNode<'msg>) (acc: Map<RetainedId, RetainedUiState>) : Map<RetainedId, RetainedUiState> =
            let carried = Map.tryFind n.Identity prev.StateByIdentity
            let carriedClock = carried |> Option.bind (fun s -> s.Animation)
            let carriedText = carried |> Option.bind (fun s -> s.Text)

            let priorOwn =
                match priorOwnById.TryGetValue n.Identity with
                | true, own -> own
                | _ -> []

            let desired = ControlInternals.visualStateOf n.Control.Attributes
            let clock = updateClockForState desired priorOwn carriedClock

            let acc =
                match clock, carriedText with
                | None, None -> acc
                | _ -> Map.add n.Identity { Animation = clock; Text = carriedText } acc

            n.Children |> List.fold (fun a c -> collect c a) acc

        let stateById = collect newRoot Map.empty

        // Assemble the painted scene, overlaying any ACTIVE animation clock onto its identity's own
        // (static) paint — paint-level only, scoped to that subtree (FR-002/FR-010). When NO clock is
        // active the fast path returns the cached `SubtreeScene` verbatim, so an at-rest frame is
        // byte-identical to the pre-R4 golden and costs nothing extra (FR-005, identity-at-rest). The
        // overlay always wraps the cached STATIC `OwnScene` (fragments never store animated paint), so
        // the reuse/caching invariants are untouched and a settled/absent clock paints unchanged.
        let anyActive =
            stateById |> Map.exists (fun _ s -> s.Animation |> Option.exists clockActive)

        // Feature 120 (US3, FR-007/FR-012): emit a `CachedSubtree` replay boundary around each
        // reuse-stable cacheable subtree (`replayHitIds`, the prior-frame-stability gate) so the backend
        // painter replays its recorded picture instead of re-walking it. TRANSPARENT to pixels (the
        // painter sees through when replay is disabled, replays the identical content when enabled) and to
        // every IR consumer (`describe`/diagnostics/`measure` recurse into the wrapped scene). A
        // reuse-stable boundary is at rest, so its wrapped content is exactly `Fragment.SubtreeScene`.
        let needsEmitWalk = anyActive || replayHitIds.Count > 0

        let sceneList =
            if not needsEmitWalk then
                newRoot.Fragment.SubtreeScene
            else
                let rec assemble (n: RetainedNode<'msg>) : Scene list =
                    if replayHitIds.Contains n.Identity then
                        let (RetainedId cacheId) = n.Identity

                        [ { Nodes =
                              [ CachedSubtree
                                    { CacheId = cacheId
                                      Fingerprint = n.Fragment.Fingerprint
                                      Scene = Scene.group n.Fragment.SubtreeScene } ] } ]
                    else
                        let ownStatic = n.Fragment.OwnScene

                        let own =
                            match Map.tryFind n.Identity stateById |> Option.bind (fun s -> s.Animation) with
                            | Some c when clockActive c -> sampleOnPaint c ownStatic
                            | _ -> ownStatic

                        own @ (n.Children |> List.collect assemble)

                assemble newRoot

        // Byte-identical to `Control.renderTree theme size next` AT REST: `SubtreeScene` is the
        // pre-order concatenation of `paintNode` over every node — the same list `renderTree`'s paint
        // builds. An active clock contributes a paint-level overlay scoped to its own identity.
        let render: ControlRenderResult<'msg> =
            { Scene = sceneList |> Scene.group
              Layout = root
              Bounds = ControlInternals.collectBoundsWith boundsById next
              Diagnostics = Control.diagnostics next
              EventBindings = ControlInternals.eventBindingsOf next
              BoundIds = ControlInternals.boundIdsOf next
              NodeCount = Control.count next }

        { Retained =
            { Root = newRoot
              NextId = nextId
              StateByIdentity = stateById
              Theme = theme
              Memo = memo
              MemoEnabled = prev.MemoEnabled
              Layout = layoutResult
              PictureCache = pictureCache
              PictureCacheEnabled = prev.PictureCacheEnabled
              // Feature 117 (Phase 8): carry the advanced text-measure cache forward (the working copy the
              // hook populated this frame); the always-miss oracle flag threads through unchanged.
              TextCache = tc
              TextCacheEnabled = prev.TextCacheEnabled }
          Render = render
          Diagnostics = result.Diagnostics @ List.ofSeq offscreenDiags
          WorkReduction =
            { BaselineNodeCount = Control.count next
              RecomputedNodeCount = recomputed
              ChangedSubtreeBound = changedBound
              ShiftedNodeCount = shifted
              RemeasuredNodeCount = remeasured
              MemoHits = memoHits
              MemoMisses = memoMisses
              VirtualMaterialized = virtualMaterialized
              VirtualTotal = virtualTotal
              RepaintedNodeCount = repaintedNodeCount
              DirtyRectCount = dirtyRectCount
              DirtyArea = dirtyArea
              PictureCacheHits = pictureHits
              PictureCacheMisses = pictureMisses
              PictureCacheEntryCount = pictureEntryCount
              TextMeasureCacheHits = textHits
              TextMeasureCacheMisses = textMisses
              LayoutInvalidatedNodeCount = invalidated
              // Feature 120 (US3, FR-014): replay hits/misses/records coincide with the picture-cache
              // outcomes (the replay cache is its load-bearing realization); the node-skip + native-byte
              // model are the new signals.
              ReplayHits = pictureHits
              ReplayMisses = pictureMisses
              ReplayRecords = pictureMisses
              ReplaySkippedNodes = replaySkippedNodes
              ReplayCacheNativeBytes = replayCacheNativeBytes } }

    let retainedHitTest (x: float) (y: float) (retained: RetainedRender<'msg>) : RetainedId option =
        // The deepest node whose cached box contains the point. Each node — including unkeyed
        // same-kind siblings — carries a distinct identity and its own box, so this resolves to a
        // per-node identity with no collision (the defect the `ControlId` hitTest path has).
        let contains (box: Rect option) =
            match box with
            | Some(b: Rect) -> x >= b.X && x <= b.X + b.Width && y >= b.Y && y <= b.Y + b.Height
            | None -> false

        let rec go (n: RetainedNode<'msg>) : RetainedId option =
            // children first (deepest-wins); fall back to self when the point is in this node's own
            // area but in a gap between its children.
            match n.Children |> List.tryPick go with
            | Some _ as hit -> hit
            | None -> if contains n.Fragment.Box then Some n.Identity else None

        go retained.Root

    let authoredControlIds (boundIds: Set<ControlId>) (retained: RetainedRender<'msg>) : Map<RetainedId, ControlId> =
        // Feature 110 (FR-003): reproduce `Control.nearestAuthored`'s climb from retained identity.
        // A node is AUTHORED when it is keyed (`canonical <> path`, since `canonical = Key ?? path`)
        // OR its canonical id is bound (`canonical ∈ boundIds`) — the exact predicate feature 098 uses
        // (`node.Id <> path || node.Id ∈ BoundIds`). Each node maps to the nearest authored ancestor
        // INCLUDING itself; a node with no authored ancestor gets no entry (the oracle's `None` →
        // `MapPointer` case). The `parent + "." + index` path (root "0") matches `nearestAuthored`/
        // `collectBoundsWith`/`eventBindingsOf`, so the resolved id is byte-identical.
        let rec go (path: string) (nearest: ControlId option) (n: RetainedNode<'msg>) (acc: Map<RetainedId, ControlId>) : Map<RetainedId, ControlId> =
            let canonical = n.Control.Key |> Option.defaultValue path

            let authoredHere =
                if canonical <> path || Set.contains canonical boundIds then
                    Some canonical
                else
                    None

            let effective =
                match authoredHere with
                | Some _ -> authoredHere
                | None -> nearest

            let acc =
                match effective with
                | Some authored -> Map.add n.Identity authored acc
                | None -> acc

            n.Children
            |> List.mapi (fun i c -> i, c)
            |> List.fold (fun a (i, c) -> go (childPath path i) effective c a) acc

        go "0" None retained.Root Map.empty

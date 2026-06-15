namespace FS.GG.UI.Controls
open FS.GG.UI.DesignSystem

/// Feature 091 (E2) — the retained render structure that wires the parked keyed reconciler
/// (`module internal Reconcile`, feature 067) onto the live render path. Each frame holds the
/// previous lowered `Control<'msg>` tree paired with its cached render fragments and a stable,
/// diff-conferred identity per node; the next frame is produced by `Reconcile.diff`-ing against
/// it and reusing the unchanged subtrees' cached fragments.
///
/// This whole surface is `internal` — assembly-internal accessibility, genuinely unreachable
/// from package consumers (mirrors `module internal Reconcile` / `module internal SceneRenderer`;
/// zero public-surface baseline delta, 067 SC-005). The Expecto/FsCheck property tests reach it
/// via `[<assembly: InternalsVisibleTo("Controls.Tests")>]`. It is a contract between framework
/// internals and the property tests, NOT a consumer API: it exposes no mutable view-model, no
/// data binding, and no dependency/attached property (permanent roadmap non-goals).

/// The stable identity the diff confers on a matched node. Monotonic within a host loop; NOT the
/// path-derived `ControlId` (which is unstable across a positional shift — the very reason
/// focus/text state resets today). Per-control state (focus, animation clock, text model) re-keys
/// to this so it survives an unrelated re-render. Minted deterministically from a per-host
/// counter (no clock/randomness), so identical frame sequences mint identical ids (SC-005).
type internal RetainedId = RetainedId of uint64

/// The cached, reusable unit of measure + paint for one retained node. `OwnScene` is the node's
/// own painted contribution (`Control.renderTree`'s per-node `here`); `SubtreeScene` is the
/// pre-order painted scene of the node AND its descendants (reused verbatim when the whole subtree
/// is unchanged AND unshifted); `Box` is the node's evaluated absolute box (the reuse key).
type internal RenderFragment =
    { OwnScene: FS.GG.UI.Scene.Scene list
      SubtreeScene: FS.GG.UI.Scene.Scene list
      Box: FS.GG.UI.Scene.Rect option
      /// Feature 120 (US3, FR-008): the collision-resistant structural fingerprint of `SubtreeScene`,
      /// computed via `hashScene` when the fragment is (re)painted and carried unchanged on a `Keep`
      /// reuse (cost ∝ damage, not tree size). The backend replay key and the `CachedSubtree.Fingerprint`.
      Fingerprint: uint64 }

/// One retained control node: its stable identity, the lowered control it was built from, its
/// cached render fragment, and its retained children (mirroring `Control.Children` order).
type internal RetainedNode<'msg> =
    { Identity: RetainedId
      Control: Control<'msg>
      Fragment: RenderFragment
      Children: RetainedNode<'msg> list }

/// Feature 099 (R4) / Feature 103 (R6) — the per-identity animation clock. Generalizes the
/// feature-091 carried slot (transform-only, never written) to the feature-073 paint carrier.
/// `Anim` is the reused feature-073 `Animation`, but the LIVE channel is the OPACITY tween only: the
/// next layer's fade-in (`0→1`). `Animation.applyAt` samples opacity/transform and NEVER recolors by
/// the `Color` tween, so R6 does **not** realize the visual-state cross-fade with a standalone
/// `Color` tween (which `applyAt` would never honor, and which a single tween could not express
/// against the multi-channel `Foreground`/`Fill`/`Stroke` paint `Style.resolve` produces anyway).
/// Instead the paint cross-fade is the two-snapshot composite (`sampleOnPaint`): the prior state's
/// `From` snapshot fading OUT (`1→0`) under the next state's own-scene fading in, both driven by the
/// public opacity sampler. `Elapsed` is the accumulated INJECTED delta (sole time coordinate — no
/// wall-clock); `Target` is the `VisualState` this clock animates toward (used to detect a retarget
/// when the stamped state flips); `From` is the prior state's static own-scene snapshot captured at
/// transition start (a `Scene list` to match `RenderFragment.OwnScene` verbatim; empty ⇒ nothing to
/// fade from ⇒ a plain fade-in). `None` on the slot ⇒ the identity is at rest and paints
/// byte-identically to the static render (FR-004/FR-005).
type internal AnimationClock =
    { Anim: FS.GG.UI.Scene.Animation
      Elapsed: System.TimeSpan
      Target: VisualState
      From: FS.GG.UI.Scene.Scene list }

/// Feature 113 (Phase 5) — what a single `memoize` call resolved to: a `Hit` reused the
/// previously-lowered subtree for the identity (the dependency compared equal, the thunk did NOT
/// run); a `Miss` recomputed it (a changed or cold dependency). Aggregated per frame into the two
/// `FrameMetrics` memo counts.
type internal MemoOutcome =
    | Hit
    | Miss

/// Feature 113 (Phase 5) — one cached memoized projection for a control identity. `Dependency` is the
/// deterministic dependency value the site supplied last frame, BOXED to `obj` so a single uniform
/// cache holds entries from heterogeneous sites; reuse is decided by `=` on the boxed value (F#
/// STRUCTURAL equality, never object identity — FR-005). `Subtree` is the previously-lowered `Scene
/// list` fragment, a reference type, so a `Hit` returns the SAME instance stored last frame (FR-004).
/// Specialized to `Scene list` this rung because the DataGrid row/column projection is the sole
/// memoized site; widening the stored subtree type travels with the deferred `Style.resolve` site.
type internal MemoEntry =
    { Dependency: obj
      Subtree: FS.GG.UI.Scene.Scene list }

/// Feature 113 (Phase 5) — the per-frame memo store, keyed by the control's stable `ControlId`.
/// Carried frame-to-frame in the retained structure; an absent key is a cold miss.
type internal MemoCache = Map<ControlId, MemoEntry>

/// Per-control UI state keyed by the STABLE `RetainedId` rather than the path-derived `ControlId`,
/// so it survives a positional shift (FR-003). `Animation` is the per-control clock proving
/// FR-003 survival; under feature 099 (R4) it is the live `AnimationClock` advanced by the host
/// tick and sampled on paint (091 only carried it; nothing wrote it). `Text` is re-keyed text-input
/// state. Focus itself stays in the consumer model's `ControlRuntime.FocusedControl`; 091 only
/// remaps the lookup to `RetainedId`.
type internal RetainedUiState =
    { Animation: AnimationClock option
      Text: TextInputModel option }

/// Feature 116 (Phase 7, FR-006): the picture cache's COMPLETE correctness key for one cacheable
/// boundary. `Box` is the node's evaluated absolute box (explicit for attribution); `Picture` is a
/// structural digest of the node's painted subtree (`Fragment.SubtreeScene`) — which embeds EVERY
/// render-affecting input (theme colours, clip, opacity, transform, font/text, visual-state) by
/// construction, so equality on this key proves a hit is byte-identical to a fresh paint and any
/// single changed input forces a miss (no input can be omitted). Compared by F# structural `=`.
type internal PictureCacheKey =
    { Box: FS.GG.UI.Scene.Rect option
      /// Feature 120 (US3, FR-008): the collision-resistant structural fingerprint of the boundary's
      /// painted subtree, replacing the feature-116 truncation-prone `sprintf "%A"` digest. Two subtrees
      /// that stringify identically under the old truncating key but differ structurally produce different
      /// fingerprints, so no stale hit can cross a render-affecting change. Compared by `=`.
      Fingerprint: uint64 }

/// Feature 116 (Phase 7, FR-009/FR-010): the bounded cross-frame picture cache. A fixed-cap LRU over
/// cacheable picture identities (`RetainedId`), each holding its last-seen `PictureCacheKey` and a
/// monotonic access stamp (`Clock`, advanced deterministically by the frame's traversal order — NO
/// wall-clock). On overflow the least-recently-accessed entry is dropped; a dropped identity re-misses
/// when next needed (never a stale hit). `Entries.Count <= PictureCacheCap` at all times.
type internal PictureCache =
    { Entries: Map<RetainedId, int * PictureCacheKey>
      Clock: int }

/// Feature 117 (Phase 8, FR-002): the text-measure cache's COMPLETE correctness key for one measurement.
/// Every input `Scene.measureText` reads is keyed — the text string and the full `FontSpec` value
/// (`Family`, `Size`, `Weight`) — so two requests differing in ANY field are distinct entries and no
/// stale hit can cross a differing input (the edge case "changing only font weight/family/size MUST
/// miss"). The available-space CONSTRAINT (the `fittedFontSize` box) is deliberately NOT keyed: it does
/// not change `measureText`'s output, only which candidate sizes the search probes, and each candidate
/// size is already a distinct key via `Size` (research R2). Compared by F# structural `=`.
type internal TextMeasureKey =
    { Text: string
      Family: string option
      Size: float
      Weight: int option }

/// Feature 117 (Phase 8, FR-003): the bounded cross-frame text-measure cache, mirroring the 116
/// `PictureCache` discipline. A fixed-cap LRU over measured text identities (`TextMeasureKey`), each
/// holding its measured `TextMetrics` and a monotonic access stamp (`Clock`, advanced deterministically
/// by measurement order — NO wall-clock). On overflow the least-recently-accessed entry is dropped; a
/// dropped key re-misses (re-measures, re-stores) when next needed (never a stale hit).
/// `Entries.Count <= TextMeasureCacheCap` at all times.
type internal TextMeasureCache =
    { Entries: Map<TextMeasureKey, int * FS.GG.UI.Scene.TextMetrics>
      Clock: int }

/// The per-frame retained root plus the monotonic identity counter, the identity-keyed UI
/// state map, and the theme this structure was painted under. Lives in the host loop's existing
/// mutable-ref state (the interpreter edge). 092: `Theme` is the fragment-reuse key — a theme
/// change between `step` calls invalidates all cached fragments so they repaint (FR-008), and
/// the live host now READS/WRITES `StateByIdentity` (091 only carried it; the host ignored it).
type internal RetainedRender<'msg> =
    { Root: RetainedNode<'msg>
      NextId: uint64
      StateByIdentity: Map<RetainedId, RetainedUiState>
      Theme: Theme
      /// Feature 113 (Phase 5): the per-identity memoization store carried frame-to-frame — the
      /// DataGrid row/column projection's reuse cache (keyed by stable `ControlId`). Seeded by `init`
      /// (all cold misses); each `step` consults it for a memoizable node and advances it.
      Memo: MemoCache
      /// Feature 113 (Phase 5): the always-miss switch (FR-008). `true` on the live path (the seam is
      /// active); a parity test flips it `false` to BYPASS the seam — `memoize` is not called at all, so
      /// nothing is reused and both `MemoHits`/`MemoMisses` stay 0/0 (NOT "every node a miss") — proving the
      /// rendered scene is byte-identical with the seam disabled.
      MemoEnabled: bool
      /// Feature 097 (R2): the previous frame's full `LayoutResult` — the per-frame measure/bounds
      /// cache (FR-002). `step` threads it into `Layout.evaluateIncremental` so an unchanged subtree's
      /// bounds survive across frames and are reused without re-measuring. Seeded by `init` with a full
      /// `evaluate`; advanced each `step` to the incremental result.
      Layout: FS.GG.UI.Layout.LayoutResult
      /// Feature 116 (Phase 7, FR-009/FR-010): the bounded cross-frame picture cache carried frame-to-frame.
      /// Seeded by `init` (every first-frame cacheable boundary a cold miss); each `step` consults it for a
      /// hit/miss outcome per cacheable identity, refreshes recency, and evicts LRU over the cap. An absent
      /// or evicted identity is a miss.
      PictureCache: PictureCache
      /// Feature 116 (Phase 7, FR-007): the picture-cache always-miss switch (mirrors `MemoEnabled`).
      /// `true` on the live path; a parity test flips it `false` to force every cacheable boundary down the
      /// miss path (`PictureCacheHits = 0`), proving the rendered scene is byte-identical with the cache
      /// disabled (cache-on ≡ cache-off).
      PictureCacheEnabled: bool
      /// Feature 117 (Phase 8, FR-001/FR-003): the bounded cross-frame text-measure cache carried
      /// frame-to-frame. Seeded EMPTY by `init` (so the first `step` is a cold population — misses); each
      /// `step` consults it for a hit/miss per measured `(text, font)`, refreshes recency, and evicts LRU
      /// over the cap. An absent or evicted key is a miss.
      TextCache: TextMeasureCache
      /// Feature 117 (Phase 8, FR-004): the text-cache always-miss switch (mirrors `MemoEnabled` /
      /// `PictureCacheEnabled`). `true` on the live path; a parity test flips it `false` to force every
      /// measurement to re-measure via `Scene.measureText` (`TextMeasureCacheHits = 0`), proving the
      /// rendered scene and layout are byte-identical with the cache disabled (cache-on ≡ cache-off).
      TextCacheEnabled: bool }

/// Measured per-frame work reduction (SC-003). `BaselineNodeCount` is what a full rebuild
/// re-measures/re-paints (== N); `RecomputedNodeCount` is what the wired path actually
/// recomputed; `ChangedSubtreeBound` is the genuinely-changed work (Replace/own-change/insert);
/// `ShiftedNodeCount` (092) is work recomputed ONLY because an upstream change relaid a
/// structurally-unchanged subtree out (a `Keep` whose box moved, or a theme repaint). For any
/// localized change:
///   `RecomputedNodeCount = ChangedSubtreeBound + ShiftedNodeCount`
///   `RecomputedNodeCount < BaselineNodeCount`
/// (091 documented `RecomputedNodeCount ≤ ChangedSubtreeBound`, which a sibling-shifting change
/// violates — the shifted work was recomputed but uncounted; FR-007 splits it out.)
type internal WorkReductionRecord =
    { BaselineNodeCount: int
      RecomputedNodeCount: int
      ChangedSubtreeBound: int
      ShiftedNodeCount: int
      /// Feature 097 (R2, FR-006): nodes actually RE-MEASURED this frame (the post-propagation dirty
      /// set `Layout.evaluateIncremental` reports in `Invalidated`). For a localized update this is
      /// strictly below `BaselineNodeCount`; for a genuine whole-tree relayout it equals it; for an
      /// empty patch it is 0. Measures partial MEASURE work, distinct from partial PAINT above.
      RemeasuredNodeCount: int
      /// Feature 113 (Phase 5, FR-009/FR-010): memoizable-control reuse outcomes while building this
      /// frame — `MemoHits` reused a stored subtree (its dependency was unchanged), `MemoMisses`
      /// recomputed one (a changed or cold dependency). Summed over every memoized site evaluated
      /// this frame; both 0 on a frame that evaluates no memoizable control. Surfaced as the public
      /// `FrameMetrics.MemoHitCount` / `MemoMissCount`.
      MemoHits: int
      MemoMisses: int
      /// Feature 114 (Phase 6, FR-013): the virtualization counts read off the lowered tree this frame —
      /// `VirtualMaterialized` is the number of materialized `data-grid-row` nodes (the realized window),
      /// `VirtualTotal` is the sum of the logical `Total` over every `data-grid` node present. The walk is
      /// read-only (render output unchanged); both `0` on a frame with no virtualized control. Aggregated
      /// across multiple virtualized controls. Surfaced as the public `FrameMetrics.VirtualItemsMaterialized`
      /// / `VirtualItemsTotal`.
      VirtualMaterialized: int
      VirtualTotal: int
      /// Feature 116 (Phase 7, FR-001/FR-002/FR-003/FR-004): the per-frame DAMAGE set, accumulated from
      /// the step's own repaint decisions (each `paintFresh` — `carry`/`buildFresh`/`Update`-own-repaint —
      /// contributes the repainted node's evaluated `Fragment.Box`). `RepaintedNodeCount` is the count of
      /// repainted nodes (the changed node(s) + genuinely-shifted nodes); `DirtyRectCount` is the count of
      /// DISTINCT repainted boxes (deduped; `None` boxes contribute none); `DirtyArea` is the summed integer
      /// `w*h` over the distinct boxes. A localized hover reports a small region, a theme switch
      /// frame-spanning, an idle frame `0/0/0`. Deterministic (integer geometry). Surfaced as the public
      /// `FrameMetrics.RepaintedNodeCount` / `DirtyRectCount` / `DirtyArea`.
      RepaintedNodeCount: int
      DirtyRectCount: int
      DirtyArea: int
      /// Feature 116 (Phase 7, FR-005/FR-006/FR-007/FR-009/FR-010): the bounded picture cache's per-frame
      /// reuse outcomes over the cacheable picture boundary (a `data-grid-row` identity, the natural analog
      /// of the feature-113 data-grid-only memo cache). `PictureCacheHits` reused a cached picture whose
      /// full correctness key (theme/box/clip/opacity/transform/font-text/visual-state, by construction the
      /// painted picture's structural digest) was unchanged AND whose entry was still resident (not evicted);
      /// `PictureCacheMisses` recomputed a picture (a changed key, a cold identity, or an evicted entry).
      /// `PictureCacheEntryCount` is the live bounded-LRU entry count after this frame (`<= PictureCacheCap`).
      /// All three `0` on a frame with no cacheable picture; with the `PictureCacheEnabled` oracle `false`
      /// every picture re-misses (`PictureCacheHits = 0`). Surfaced as the public
      /// `FrameMetrics.PictureCacheHitCount` / `PictureCacheMissCount` / `PictureCacheEntryCount`.
      PictureCacheHits: int
      PictureCacheMisses: int
      PictureCacheEntryCount: int
      /// Feature 117 (Phase 8, FR-001/FR-005): the per-frame text-measure cache reuse outcomes over every
      /// `(text, font)` measured while laying out and painting this frame. `TextMeasureCacheHits` reused a
      /// resident measurement without re-invoking `Scene.measureText`; `TextMeasureCacheMisses` measured
      /// fresh (a cold, changed, or evicted key) and stored it. Both `0` on a frame that measures no text;
      /// under the `TextCacheEnabled` oracle `false` every measurement re-misses (`TextMeasureCacheHits =
      /// 0`). Surfaced as the public `FrameMetrics.TextMeasureCacheHitCount` / `TextMeasureCacheMissCount`.
      TextMeasureCacheHits: int
      TextMeasureCacheMisses: int
      /// Feature 117 (Phase 8, FR-006): the size of the layout dirty set fed into incremental layout this
      /// frame — `Set.count` of `layoutDirtySet` (the patch-derived self-dirty nodes BEFORE
      /// fixed-size-ancestor propagation). Distinct from `RemeasuredNodeCount`, which is the POST-pinning
      /// set `Layout.evaluateIncremental` actually re-measured (the dirty nodes' boundary subtrees). Because
      /// propagation expands a self-dirty node up to its first fixed-size ancestor's whole subtree,
      /// `LayoutInvalidatedNodeCount <= RemeasuredNodeCount` (the pre-pinning set is a subset of the
      /// re-measured boundary subtrees). `0` on an idle / style-only / visual-state-only frame (no
      /// layout-affecting attribute changed, so the dirty set is empty).
      LayoutInvalidatedNodeCount: int
      /// Feature 120 (US3, FR-014): the backend replay cache's per-frame reuse outcomes over the
      /// `CachedSubtree` replay boundaries emitted this frame (prior-frame-stable cacheable subtrees,
      /// FR-012), modeled deterministically with the same cross-frame LRU + new structural fingerprint as
      /// `PictureCache`. `ReplayHits` replayed a recorded picture (resident + matching fingerprint + enabled);
      /// `ReplayMisses` (re)recorded one (cold / changed fingerprint / evicted); `ReplayRecords` equals the
      /// misses (one record per miss); `ReplaySkippedNodes` sums the painted node count of every replayed
      /// boundary's subtree (the draw-call walk avoided); `ReplayCacheNativeBytes` is the deterministic
      /// model native-byte estimate of resident recorded pictures (bounded by the cap). All `0` on a frame
      /// with no replay boundary or under the replay-disable oracle. Surfaced as the public
      /// `FrameMetrics.ReplayHitCount` / `ReplayMissCount` / `ReplayRecordCount` / `ReplaySkippedNodeCount` /
      /// `ReplayCacheNativeBytes`.
      ReplayHits: int
      ReplayMisses: int
      ReplayRecords: int
      ReplaySkippedNodes: int
      ReplayCacheNativeBytes: int }

/// The result of one wired frame: the next retained structure, the render result (byte-identical
/// to a full rebuild of `next`), the diagnostics surfaced from the diff (e.g. `KeyCollision`), and
/// the measured work reduction.
type internal RetainedRenderStep<'msg> =
    { Retained: RetainedRender<'msg>
      Render: ControlRenderResult<'msg>
      Diagnostics: ControlDiagnostic list
      WorkReduction: WorkReductionRecord }

/// The first-frame result (092, FR-009): the seeded retained structure, the render result it
/// painted (so the adapter paints the first frame ONCE instead of also calling
/// `Control.renderTree`), and any first-frame diagnostics (e.g. a duplicate-key `KeyCollision`
/// present in the very first tree — 091 only diffed from frame 1, so it surfaced a frame late).
type internal RetainedInit<'msg> =
    { Retained: RetainedRender<'msg>
      Render: ControlRenderResult<'msg>
      Diagnostics: ControlDiagnostic list }

module internal RetainedRender =

    /// Feature 113 (Phase 5) — the control-internal memoization seam (contract C1–C4). Given a stable
    /// `ControlId`, a deterministic `dependency` value (boxed; compared by F# structural `=`, never
    /// object identity), a `compute` thunk that lowers the subtree, and the prior `cache`: a HIT (an
    /// entry exists for `id` AND its dependency compares EQUAL) returns the stored `Scene list` WITHOUT
    /// running `compute`; a MISS (no entry, or an unequal dependency) runs `compute` and stores the
    /// result keyed by `id` + `dependency`. Returns the resolved subtree, the advanced cache, and the
    /// `MemoOutcome`. Never reuses across an unequal/unknown dependency (FR-001/FR-005). Pure, total,
    /// deterministic.
    val internal memoize:
        id: ControlId ->
        dependency: obj ->
        compute: (unit -> FS.GG.UI.Scene.Scene list) ->
        cache: MemoCache ->
            FS.GG.UI.Scene.Scene list * MemoCache * MemoOutcome

    /// Feature 116 (Phase 7, FR-009): the fixed picture-cache entry cap. `PictureCacheEntryCount` never
    /// exceeds this; the eviction-pressure scenario drives 320 distinct cacheable rows (1.25 × cap).
    val internal PictureCacheCap: int

    /// Feature 120 (US3, FR-008): the collision-resistant structural fingerprint of a painted `Scene list`.
    /// Folds every render-affecting input of the subtree (geometry, color, path, text, font, opacity,
    /// transform, clip, and node shape) into a 64-bit hash via a deterministic FNV-1a-style mix — NO
    /// truncation, unlike the superseded `sprintf "%A"` digest, so a structural difference that the old key
    /// would have collided on yields a different fingerprint. Pure, total, deterministic; identical scenes
    /// hash identically and any single render-affecting change flips the value (FR-010). The replay key.
    val internal hashScene: scenes: FS.GG.UI.Scene.Scene list -> uint64

    /// Feature 120 (US4, FR-015): the integer area of the UNION of a set of damage rectangles, clamped to
    /// the frame area. Overlapping rectangles are counted once (never the sum), and the result never
    /// exceeds `frameArea`. Pure, total, deterministic (coordinate-compression over integer geometry).
    val internal unionArea: boxes: FS.GG.UI.Scene.Rect list -> frameArea: int -> int

    /// Feature 117 (Phase 8, FR-003): the fixed text-measure-cache entry cap (aligned with
    /// `PictureCacheCap`). `TextCache.Entries.Count` never exceeds this; the eviction-pressure scenario
    /// drives more than this many distinct strings to prove bounded memory + deterministic LRU eviction.
    val internal TextMeasureCacheCap: int

    /// Feature 117 (Phase 8, FR-001/FR-002/FR-003/FR-004): the pure, total text-measure cache lookup.
    /// A resident key `(text, family, size, weight)` returns its stored `TextMetrics` WITHOUT re-invoking
    /// `Scene.measureText` (a HIT) and bumps recency; an absent/evicted key measures fresh, inserts
    /// (evicting the least-recently-used entry over the cap), and returns it (a MISS). Two requests
    /// differing in ANY keyed input are distinct entries (no stale hit, FR-002). With `enabled = false`
    /// (the always-miss oracle) every request re-measures and is a miss (`wasHit = false`), never
    /// consulting/populating the cache, proving cache-on ≡ cache-off (FR-004). The cached value EQUALS the
    /// un-cached `Scene.measureText` value for every key (research R5). Returns `(metrics, advanced cache,
    /// wasHit)`. Deterministic + total; reached by the test assemblies.
    val internal measureTextCached:
        cache: TextMeasureCache ->
        enabled: bool ->
        text: string ->
        font: FS.GG.UI.Scene.FontSpec ->
            FS.GG.UI.Scene.TextMetrics * TextMeasureCache * bool

    /// Feature 116 (Phase 7, FR-011): the pure offscreen-effect detector. Returns the name of the first
    /// offscreen-composition-forcing effect in a node's painted scene (a drop-shadow/image-filter, a
    /// `PathClip`, or a non-opaque paint over a multi-node group) or `None`. A `RectClip` (the cheap
    /// ubiquitous label clip, lowered to `canvas.ClipRect` with no layer) is deliberately NOT flagged.
    /// Advisory only — reads the lowered scene, never alters output. Reached by the test assemblies.
    val internal offscreenEffect: ownScene: FS.GG.UI.Scene.Scene list -> string option

    /// Feature 099 (R4): the single pinned framework default transition — exactly 150 ms, `EaseOut`,
    /// on the opacity channel — applied when a tween is started/retargeted. A fixed constant (not a
    /// per-control consumer knob) so the determinism goldens reach the settled end after the same
    /// fixed frame count for the same injected-delta sequence. Reached by the test assemblies.
    val internal defaultTransitionDuration: System.TimeSpan

    /// Feature 099 (R4): advance a clock by an INJECTED delta. Total + pure (no wall-clock): a
    /// non-positive delta is a no-op (never rewinds); a positive delta accumulates `Elapsed`,
    /// CLAMPED to the animation's duration (so a very-large delta settles at the end with no
    /// overshoot, and replaying an identical delta sequence reproduces identical state — FR-006).
    val internal advance: delta: System.TimeSpan -> clock: AnimationClock -> AnimationClock

    /// Feature 099 (R4): true while the clock is still in flight (not every present tween has
    /// reached its `Duration`). A settled clock is NOT sampled — it paints byte-identically to the
    /// static render (FR-005), so only active clocks contribute a per-frame change.
    val internal clockActive: clock: AnimationClock -> bool

    /// Feature 121 (US2, FR-004): advance every per-identity animation clock by `delta`; when NO clock
    /// is active the state map is returned reference-equal (no allocation), so an idle live tick makes
    /// no garbage. Active clocks advance exactly as `advance`. Exposed for the no-alloc test (T011).
    val internal advanceStateClocks:
        delta: System.TimeSpan -> state: Map<RetainedId, RetainedUiState> -> Map<RetainedId, RetainedUiState>

    /// Feature 099 (R4) / 103 (R6): the pure transition trigger (contract C2). Given the `desired`
    /// VisualState stamped by `ControlRuntime.applyRuntimeVisualState` (R1), the matched prior node's
    /// own-scene snapshot `priorOwn`, and the carried (already-advanced) clock, decide the frame's
    /// clock: START a fade-in for a fresh state change (from a settled/no clock), RETARGET from the
    /// current sampled value for a mid-flight change (no snap to start), advance-only when the state is
    /// unchanged, and DROP a settled return-to-`Normal` clock so the identity is byte-identical at rest
    /// (FR-003/FR-005). On a fresh transition or a mid-flight retarget the new clock's `From = priorOwn`
    /// (the snapshot it cross-fades from); an advance-only/kept clock retains its existing `From`.
    val internal updateClockForState: desired: VisualState -> priorOwn: FS.GG.UI.Scene.Scene list -> carried: AnimationClock option -> AnimationClock option

    /// Feature 099 (R4) / 103 (R6): composite an ACTIVE clock onto an identity's own painted scene
    /// (paint-level only — opacity, never layout). A genuine cross-fade of two opacity-driven layers
    /// via the public feature-073 `Animation.applyAt`: the clock's `From` prior snapshot fading OUT
    /// (`1→0`) UNDER `ownScene` (this frame's cached static own paint) fading IN (the clock's opacity
    /// tween). For a region painted in both states the composite displays a colour strictly between the
    /// endpoints (SC-001). `From = []` degenerates to the plain fade-in. Used only for active clocks —
    /// a settled/absent clock paints `ownScene` unchanged (the settle path is untouched, so the final
    /// frame stays byte-identical, FR-005).
    val internal sampleOnPaint: clock: AnimationClock -> ownScene: FS.GG.UI.Scene.Scene list -> FS.GG.UI.Scene.Scene list

    /// Build the initial retained structure from the first frame's lowered tree, painting it
    /// ONCE. The returned `Render` is byte-identical to `Control.renderTree theme size control`
    /// (so the adapter reuses it rather than re-painting), and `Diagnostics` carries any
    /// first-frame duplicate-key `KeyCollision` (FR-009). Total; never throws.
    val init: theme: Theme -> size: FS.GG.UI.Scene.Size -> control: Control<'msg> -> RetainedInit<'msg>

    /// Produce the next frame from the retained `prev` and the next lowered tree, by
    /// `Reconcile.diff`-ing and reusing/recomputing fragments under the patch.
    ///
    /// Guarantees (asserted by the promoted 067 suite on the WIRED path):
    ///   - totality:         never throws for any (prev, next); duplicate keys -> KeyCollision diagnostic
    ///   - determinism:      identical (prev, next) -> identical Render + identical minted RetainedIds
    ///   - identity-at-rest: next structurally equal to prev.Root.Control -> Keep no-op, no re-measure
    ///   - round-trip:       Render is byte-identical to `Control.renderTree theme size next`
    val step:
        theme: Theme ->
        size: FS.GG.UI.Scene.Size ->
        prev: RetainedRender<'msg> ->
        next: Control<'msg> ->
            RetainedRenderStep<'msg>

    /// Resolve a point to the stable identity of the control under it (092, FR-004): the deepest
    /// retained node whose cached `Fragment.Box` contains `(x, y)`, else `None` (a true gap /
    /// outside the root). Because every node — INCLUDING unkeyed same-kind siblings — carries a
    /// distinct `RetainedId` and its own evaluated box, this returns a per-node identity with no
    /// collision, unlike the `ControlId` `hitTest`/`nearestAuthored` path (which collapses unkeyed
    /// same-kind siblings). Focus-on-click resolves through this. Reuses the boxes already computed
    /// by `init`/`step`; total and deterministic.
    val retainedHitTest: x: float -> y: float -> retained: RetainedRender<'msg> -> RetainedId option

    /// Feature 110 (FR-003): the retained-id → authored-control-id lookup. For every node in the
    /// retained tree, maps its stable `RetainedId` to the authored `ControlId` whose binding must
    /// fire for a hit on it — the nearest ancestor (including self) that is KEYED (`Key ?? path <>
    /// path`) OR whose canonical id (`Key ?? path`) is in `boundIds`. Built from the retained node
    /// tree + the frame's `BoundIds`, re-deriving each node's `parent + "." + index` path (root
    /// "0") so it reproduces, from retained identity, exactly the climb `Control.nearestAuthored`
    /// performs over a freshly rendered tree (feature 098 keyed-OR-in-`BoundIds` scheme). Lets the
    /// retained pointer route (feature 110) dispatch the SAME authored binding as the full-render
    /// oracle — including composite controls whose binding is authored above the hit node — without
    /// re-rendering. A node with no authored ancestor has no entry. Pure / total / deterministic.
    val authoredControlIds: boundIds: Set<ControlId> -> retained: RetainedRender<'msg> -> Map<RetainedId, ControlId>

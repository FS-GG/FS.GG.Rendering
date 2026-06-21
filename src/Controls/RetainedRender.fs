namespace FS.GG.UI.Controls

open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

module private RetainedRenderTrace =
    let private enabled =
        System.String.Equals(
            System.Environment.GetEnvironmentVariable("FS_GG_RENDER_LAG_TRACE"),
            "1",
            System.StringComparison.Ordinal
        )

    let emit eventName fields =
        if enabled then
            let fieldsText =
                fields
                |> List.map (fun (name, value) -> $"{name}={value}")
                |> String.concat " "

            let suffix = if System.String.IsNullOrWhiteSpace fieldsText then "" else " " + fieldsText
            let ts = System.DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
            let ticks = System.Diagnostics.Stopwatch.GetTimestamp()
            System.Console.Error.WriteLine($"FS_GG_RENDER_LAG_TRACE ts={ts} ticks={ticks} event={eventName}{suffix}")

    let time eventName fields (work: unit -> 'a) : 'a =
        if enabled then
            let sw = System.Diagnostics.Stopwatch.StartNew()
            let result = work ()
            sw.Stop()
            emit
                eventName
                (("durationMs", sw.Elapsed.TotalMilliseconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))
                 :: fields)
            result
        else
            work ()

// Feature 091 (E2) — wiring the parked keyed reconciler (feature 067) onto the live render path.
// This is NOT a new algorithm: it consumes `Reconcile.diff`'s patch and drives the next frame
// from `ControlInternals.evaluateLayout` + `paintNode` (the SAME measure/paint `Control.renderTree`
// uses), reusing cached fragments for unchanged + unshifted subtrees. The render output is
// therefore byte-for-byte identical to a full rebuild of `next` BY CONSTRUCTION (FR-005, C2):
// a reused fragment is reused only when its paint inputs (the node's own data + its computed box)
// are provably unchanged, so it equals what re-painting would have produced.

type internal RetainedId = RetainedId of uint64

type internal RetainedInvalidationDecision =
    | Reused
    | Rebuilt
    | Discarded
    | FreshFallback

type internal RetainedInvalidationReason =
    | InitialAssembly
    | StableInputs
    | VisualInput
    | LayoutInput
    | ModifierLayerInput
    | TextProofInput
    | ExplicitIdentity
    | ChildOrdering
    | ChildRemoval
    | ChildInsertion
    | ThemeInput
    | CacheBoundaryInput
    | UnsafeReuse

type internal RetainedInvalidationEvidence =
    { Decision: RetainedInvalidationDecision
      Reason: RetainedInvalidationReason
      FingerprintBefore: uint64 option
      FingerprintAfter: uint64 option
      BoxBefore: FS.GG.UI.Scene.Rect option
      BoxAfter: FS.GG.UI.Scene.Rect option }

type internal RenderFragment =
    { OwnScene: FS.GG.UI.Scene.Scene list
      // Feature 141 (R1b): owner-produced assembly result. Retained rendering stores and reuses this
      // result instead of carrying independently constructible in-flow/overlay composition fields.
      Assembly: ControlInternals.CurrentNodeAssemblyResult
      Box: FS.GG.UI.Scene.Rect option
      InvalidationEvidence: RetainedInvalidationEvidence list }

type internal RetainedMetadata<'msg> =
    { InFlowBounds: (ControlId * FS.GG.UI.Scene.Rect) list
      OverlayBounds: (ControlId * FS.GG.UI.Scene.Rect) list
      Diagnostics: ControlDiagnostic list
      EventBindings: ControlEventBinding<'msg> list
      BoundIds: Set<ControlId>
      KeyedNodes: (ControlId * ControlKind) list
      NodeCount: int }

type internal RetainedNode<'msg> =
    { Identity: RetainedId
      Control: Control<'msg>
      Fragment: RenderFragment
      Metadata: RetainedMetadata<'msg>
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
      Weight: int option
      MeasurementVersionBucket: string }

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
      MetadataVisitedNodeCount: int
      MetadataFallbackCount: int
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
      ReplayCacheNativeBytes: int
      AvoidedContentWork: int
      PlacementOnlyReuseCount: int
      ContentRecordCount: int
      ContentRerecordCount: int
      PromotionCount: int
      DemotionCount: int
      FallbackCount: int
      PromotionOverhead: int
      NetSavedWork: int }

type internal CompositorDamageRegion =
    { DamageX: int
      DamageY: int
      DamageWidth: int
      DamageHeight: int }

type internal CompositorDamageRegionSet =
    { FrameWidth: int
      FrameHeight: int
      Regions: CompositorDamageRegion list
      UnionArea: int
      FullFrameInvalidation: bool
      Cause: string }

type internal CompositorFallbackReason =
    | MissingProof
    | FailedProof of reason: string
    | EnvironmentLimited of reason: string
    | FullFrameInvalidation
    | EmptyDamage
    | UnsafeDamage of reason: string

type internal CompositorTier =
    | NoCompositorTier
    | RetainedTier
    | ReplayTier
    | SnapshotTier
    | DemotedTier

type internal PromotionDecisionKind =
    | Promote
    | Keep
    | Demote
    | Reject
    | Observe

type internal PromotionDecision =
    { BoundaryId: string
      Decision: PromotionDecisionKind
      Reason: string
      ObservedStabilityFrames: int
      ExpectedSavedWork: int
      MeasuredOverhead: int
      Tier: CompositorTier }

[<RequireQualifiedAccess>]
type internal Feature159Reason =
    | Instability
    | LowCost
    | OverheadExceedsSavedWork
    | StaleContentIdentity
    | StalePlacementIdentity
    | AmbiguousIdentity
    | CrossProfileEvidence
    | MissingRetainedContent
    | ResourceLimited
    | UnsupportedHost
    | ParityMismatch
    | NonBeneficialCounters
    | RunIdentityMismatch
    | ScenarioDefinitionMismatch
    | MissingPolicy
    | EnvironmentLimited

[<RequireQualifiedAccess>]
type internal Feature159PromotionStatus =
    | Promoted
    | Observing
    | Kept
    | Demoted
    | Rejected
    | Bypassed
    | NonBeneficial
    | FallbackOnly
    | PromotionEnvironmentLimited

[<RequireQualifiedAccess>]
type internal Feature159ReuseStatus =
    | ContentReusedPlacementUpdated
    | ContentRecorded
    | ContentRerecorded
    | FallbackFullRedraw
    | ReuseRejected
    | ReuseEnvironmentLimited

[<RequireQualifiedAccess>]
type internal Feature159RetainedLayerState =
    | Recorded
    | Reused
    | Refreshed
    | Bypassed
    | Evicted
    | Demoted
    | Invalid
    | Unavailable

type internal Feature159ContentIdentity =
    { BoundaryId: string
      ContentId: uint64
      LocalContentFingerprint: uint64
      AlgorithmVersion: string
      RunId: string
      ArtifactPath: string option }

type internal Feature159PlacementIdentity =
    { BoundaryId: string
      PlacementId: uint64
      Box: FS.GG.UI.Scene.Rect option
      ScrollOffsetX: float
      ScrollOffsetY: float
      Scale: float
      Coverage: FS.GG.UI.Scene.Rect list
      AlgorithmVersion: string }

type internal Feature159ReuseCounters =
    { AvoidedContentWork: int
      PlacementOnlyReuseCount: int
      ContentRecordCount: int
      ContentRerecordCount: int
      PromotionCount: int
      DemotionCount: int
      FallbackCount: int
      ReplayHits: int
      ReplayMisses: int
      ReplayRecords: int
      PromotionOverhead: int
      NetSavedWork: int }

type internal Feature159ReuseDecision =
    { BoundaryId: string
      Status: Feature159ReuseStatus
      PrimaryReason: Feature159Reason option
      PriorContentIdentity: Feature159ContentIdentity option
      CurrentContentIdentity: Feature159ContentIdentity option
      PriorPlacementIdentity: Feature159PlacementIdentity option
      CurrentPlacementIdentity: Feature159PlacementIdentity option
      CounterDelta: Feature159ReuseCounters
      ArtifactPaths: string list }

type internal Feature159PromotionCandidate =
    { BoundaryId: string
      ScenarioId: string
      HostProfileId: string
      ObservationWindow: int
      ObservedStabilityFrames: int
      ExpectedSavedWork: int
      MeasuredOverhead: int
      ReductionPercent: float
      ContentStable: bool
      ParityPassed: bool
      ResourceLimited: bool
      CurrentTier: CompositorTier }

type internal Feature159ParityResult =
    { ScenarioId: string
      AttemptId: string
      Verdict: string
      OutsideDamageDriftCount: int
      ArtifactPaths: string list
      Diagnostics: string list }

type internal Feature159PromotionDecision =
    { BoundaryId: string
      Status: Feature159PromotionStatus
      PrimaryReason: Feature159Reason option
      ObservedStabilityFrames: int
      ExpectedSavedWork: int
      MeasuredOverhead: int
      ReductionPercent: float
      TargetTier: CompositorTier
      Parity: Feature159ParityResult option
      ArtifactPaths: string list }

type internal Feature159RetainedLayer =
    { LayerId: string
      BoundaryId: string
      ContentIdentity: Feature159ContentIdentity
      LastPlacementIdentity: Feature159PlacementIdentity
      HostProfileId: string
      RunId: string
      State: Feature159RetainedLayerState
      ResourceEstimate: int
      Diagnostics: string list }

type internal SnapshotResourceVerdict =
    | SnapshotReady
    | SnapshotDemoted of reason: string
    | SnapshotLimited of reason: string

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

    // Feature 139 (R1a): retained nodes consume the shared current-node assembly owner instead of
    // re-implementing container clipping plus overlay splitting locally.
    let assembleRetainedNode
        (nc: Control<'msg>)
        (box: FS.GG.UI.Scene.Rect option)
        (own: FS.GG.UI.Scene.Scene list)
        (children: RetainedNode<'msg> list)
        : ControlInternals.CurrentNodeAssemblyResult =
        RetainedRenderTrace.time
            "retained-build-assemble-node"
            [ "kind", nc.Kind
              "children", string children.Length
              "ownScenes", string own.Length ]
            (fun () ->
                let childAssemblies: ControlInternals.CurrentNodeAssemblyResult list =
                    children
                    |> List.map (fun child -> child.Fragment.Assembly)

                ControlInternals.assembleCurrentNode nc box own childAssemblies)

    let private evidence
        (decision: RetainedInvalidationDecision)
        (reason: RetainedInvalidationReason)
        (before: RenderFragment option)
        (after: ControlInternals.CurrentNodeAssemblyResult)
        (afterBox: Rect option)
        : RetainedInvalidationEvidence =
        { Decision = decision
          Reason = reason
          FingerprintBefore = before |> Option.map (fun fragment -> fragment.Assembly.Fingerprint)
          FingerprintAfter = Some after.Fingerprint
          BoxBefore = before |> Option.bind (fun fragment -> fragment.Box)
          BoxAfter = afterBox }

    let private retainedFragment
        (own: Scene list)
        (assembly: ControlInternals.CurrentNodeAssemblyResult)
        (box: Rect option)
        (evidence: RetainedInvalidationEvidence)
        : RenderFragment =
        { OwnScene = own
          Assembly = assembly
          Box = box
          InvalidationEvidence = [ evidence ] }

    let private retainedMetadata
        (path: string)
        (control: Control<'msg>)
        (box: Rect option)
        (children: RetainedNode<'msg> list)
        : RetainedMetadata<'msg> =
        let controlId: ControlId = control.Key |> Option.defaultValue path

        let bounds =
            children
            |> List.map (fun child ->
                ({ InFlowBounds = child.Metadata.InFlowBounds
                   OverlayBounds = child.Metadata.OverlayBounds }
                 : ControlInternals.CurrentNodeBoundsResult))
            |> ControlInternals.assembleCurrentNodeBounds control path box

        let eventBindings = ControlInternals.eventBindings path control
        let boundIds =
            children
            |> List.fold (fun acc child -> Set.union acc child.Metadata.BoundIds) Set.empty
            |> fun acc -> if List.isEmpty eventBindings then acc else Set.add controlId acc

        let keyedNodes =
            match control.Key with
            | Some key -> (key, control.Kind) :: (children |> List.collect (fun child -> child.Metadata.KeyedNodes))
            | None -> children |> List.collect (fun child -> child.Metadata.KeyedNodes)

        { InFlowBounds = bounds.InFlowBounds
          OverlayBounds = bounds.OverlayBounds
          Diagnostics =
            ControlInternals.controlDiagnostics control
            @ (children |> List.collect (fun child -> child.Metadata.Diagnostics))
          EventBindings =
            eventBindings
            @ (children |> List.collect (fun child -> child.Metadata.EventBindings))
          BoundIds = boundIds
          KeyedNodes = keyedNodes
          NodeCount = 1 + (children |> List.sumBy (fun child -> child.Metadata.NodeCount)) }

    let private duplicateKeyDiagnostics (metadata: RetainedMetadata<'msg>) =
        metadata.KeyedNodes
        |> List.groupBy fst
        |> List.collect (fun (key, rows) ->
            if rows.Length > 1 then
                rows |> List.tail |> List.map (fun (_, kind) -> Diagnostics.keyCollision key kind)
            else
                [])

    let private renderFromRetainedMetadata
        (theme: Theme)
        (size: Size)
        (layout: FS.GG.UI.Layout.LayoutNode)
        (sceneList: Scene list)
        (metadata: RetainedMetadata<'msg>)
        : ControlRenderResult<'msg> =
        { Scene = sceneList |> ControlInternals.sceneWithViewportBackground theme size
          Layout = layout
          Bounds = metadata.InFlowBounds @ metadata.OverlayBounds
          Diagnostics = metadata.Diagnostics @ duplicateKeyDiagnostics metadata
          EventBindings = metadata.EventBindings
          BoundIds = metadata.BoundIds
          NodeCount = metadata.NodeCount }

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

    let classifyModifierEffect effect = Composition.classify effect

    // Feature 117 (Phase 8, FR-001/FR-002/FR-003/FR-004): the pure, total text-measure cache lookup.
    // `Scene.measureText` is a pure function of `(text, font)`, so the cached value EQUALS the un-cached
    // value for every key (research R5) — the cache is a transparent accelerator. A resident key returns
    // its stored `TextMetrics` WITHOUT re-invoking `Scene.measureText` (a hit), bumps its recency stamp,
    // and returns the advanced cache; an absent/evicted key measures fresh, inserts (evicting the
    // least-recently-used entry deterministically over the cap), and returns it (a miss). When `enabled`
    // is `false` (the always-miss oracle, FR-004) every request re-measures and is a miss, never
    // consulting/populating the cache — proving cache-on ≡ cache-off. Returns `(metrics, advanced cache,
    // wasHit)`. Deterministic: the recency stamp (`Clock`) advances by measurement order, never a clock.
    let internal measureTextCachedWithBucket
        (bucket: string)
        (cache: TextMeasureCache)
        (enabled: bool)
        (text: string)
        (font: FS.GG.UI.Scene.FontSpec)
        : FS.GG.UI.Scene.TextMetrics * TextMeasureCache * bool =
        if not enabled then
            // Feature 136 (R2): resolve through the real measurer when installed, else the pure
            // heuristic (byte-identical to pre-136 when none installed). Keeps cache-on ≡ cache-off.
            FS.GG.UI.Scene.Scene.measureTextResolved text font, cache, false
        else
            let key: TextMeasureKey =
                { Text = text
                  Family = font.Family
                  Size = font.Size
                  Weight = font.Weight
                  MeasurementVersionBucket = bucket }

            match Map.tryFind key cache.Entries with
            | Some(_, metrics) ->
                let clock = cache.Clock + 1
                metrics, { cache with Entries = Map.add key (clock, metrics) cache.Entries; Clock = clock }, true
            | None ->
                let metrics = FS.GG.UI.Scene.Scene.measureTextResolved text font
                let clock = cache.Clock + 1
                let mutable entries = Map.add key (clock, metrics) cache.Entries

                while entries.Count > TextMeasureCacheCap do
                    let lruKey, _ = entries |> Map.toSeq |> Seq.minBy (fun (_, (stamp, _)) -> stamp)
                    entries <- Map.remove lruKey entries

                metrics, { cache with Entries = entries; Clock = clock }, false

    let internal measureTextCached
        (cache: TextMeasureCache)
        (enabled: bool)
        (text: string)
        (font: FS.GG.UI.Scene.FontSpec)
        : FS.GG.UI.Scene.TextMetrics * TextMeasureCache * bool =
        measureTextCachedWithBucket
            (FS.GG.UI.Scene.Scene.textMeasurementVersionBucket ())
            cache
            enabled
            text
            font

    // A cacheable picture boundary: a materialized data-grid row (the row analog of the 113 data-grid
    // memo site). Each row's painted picture is cached and reused when its full correctness key is
    // unchanged AND its entry is still resident.
    let private isCacheablePicture (c: Control<'msg>) = c.Kind = "data-grid-row"

    // Feature 120/141: compatibility alias over the owner-side structural Scene fingerprint.
    let hashScene scenes = ControlInternals.hashScene scenes

    // The COMPLETE correctness key for a cacheable boundary: the node's evaluated box + the
    // collision-resistant structural fingerprint of its painted subtree (feature 120, FR-008 — replacing
    // the feature-116 truncation-prone `sprintf "%A"`). It embeds EVERY render-affecting input (theme
    // colours, clip, opacity, transform, font/text, visual-state) by construction, so an equal key proves
    // a hit is byte-identical to a fresh paint and any single changed input forces a miss. The fingerprint
    // is the one already memoized on the fragment (cost ∝ damage, not tree size).
    let private pictureKeyOf (n: RetainedNode<'msg>) : PictureCacheKey =
        { Box = n.Fragment.Box
          Fingerprint = n.Fragment.Assembly.Fingerprint }

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

    let private rectOfDamage (damage: CompositorDamageRegion) : Rect =
        { X = float damage.DamageX
          Y = float damage.DamageY
          Width = float damage.DamageWidth
          Height = float damage.DamageHeight }

    let private damageOfRect frameWidth frameHeight (rect: Rect) : CompositorDamageRegion option =
        let clamp lo hi value = min hi (max lo value)
        let x0 = clamp 0 frameWidth (int (System.Math.Floor rect.X))
        let y0 = clamp 0 frameHeight (int (System.Math.Floor rect.Y))
        let x1 = clamp 0 frameWidth (int (System.Math.Ceiling(rect.X + rect.Width)))
        let y1 = clamp 0 frameHeight (int (System.Math.Ceiling(rect.Y + rect.Height)))
        let width = x1 - x0
        let height = y1 - y0

        if width <= 0 || height <= 0 then
            None
        else
            Some
                { DamageX = x0
                  DamageY = y0
                  DamageWidth = width
                  DamageHeight = height }

    let damageRegionSet frameWidth frameHeight fullFrameInvalidation cause boxes =
        let frameArea = max 0 frameWidth * max 0 frameHeight

        let regions =
            if fullFrameInvalidation then
                if frameArea = 0 then
                    []
                else
                    [ { DamageX = 0
                        DamageY = 0
                        DamageWidth = max 0 frameWidth
                        DamageHeight = max 0 frameHeight } ]
            else
                boxes
                |> List.choose (damageOfRect frameWidth frameHeight)
                |> List.distinct

        { FrameWidth = frameWidth
          FrameHeight = frameHeight
          Regions = regions
          UnionArea = unionArea (regions |> List.map rectOfDamage) frameArea
          FullFrameInvalidation = fullFrameInvalidation
          Cause = cause }

    let placementDamage frameWidth frameHeight oldBox newBox =
        damageRegionSet frameWidth frameHeight false "placement-only movement" [ oldBox; newBox ]

    let classifyDamageFallback proofReady (proofReason: string option) (damage: CompositorDamageRegionSet) =
        match proofReady, proofReason, damage.FullFrameInvalidation, damage.Regions with
        | false, Some reason, _, _ when reason.Contains("environment", System.StringComparison.OrdinalIgnoreCase) -> Some(CompositorFallbackReason.EnvironmentLimited reason)
        | false, Some reason, _, _ -> Some(FailedProof reason)
        | false, None, _, _ -> Some MissingProof
        | true, _, true, _ -> Some FullFrameInvalidation
        | true, _, false, [] -> Some EmptyDamage
        | true, _, false, _ when damage.UnionArea > damage.FrameWidth * damage.FrameHeight -> Some(UnsafeDamage "damage exceeds frame area")
        | _ -> None

    let promotionDecision boundaryId observedStabilityFrames observationWindow expectedSavedWork measuredOverhead parityPassed =
        if not parityPassed then
            { BoundaryId = boundaryId
              Decision = Reject
              Reason = "parity failed"
              ObservedStabilityFrames = observedStabilityFrames
              ExpectedSavedWork = expectedSavedWork
              MeasuredOverhead = measuredOverhead
              Tier = NoCompositorTier }
        elif observedStabilityFrames < observationWindow then
            { BoundaryId = boundaryId
              Decision = Observe
              Reason = "stability window incomplete"
              ObservedStabilityFrames = observedStabilityFrames
              ExpectedSavedWork = expectedSavedWork
              MeasuredOverhead = measuredOverhead
              Tier = NoCompositorTier }
        elif expectedSavedWork <= 0 then
            { BoundaryId = boundaryId
              Decision = Reject
              Reason = "no expected saved work"
              ObservedStabilityFrames = observedStabilityFrames
              ExpectedSavedWork = expectedSavedWork
              MeasuredOverhead = measuredOverhead
              Tier = NoCompositorTier }
        elif measuredOverhead >= expectedSavedWork then
            { BoundaryId = boundaryId
              Decision = Demote
              Reason = "bookkeeping overhead exceeds saved work"
              ObservedStabilityFrames = observedStabilityFrames
              ExpectedSavedWork = expectedSavedWork
              MeasuredOverhead = measuredOverhead
              Tier = DemotedTier }
        else
            { BoundaryId = boundaryId
              Decision = Promote
              Reason = "stable and beneficial"
              ObservedStabilityFrames = observedStabilityFrames
              ExpectedSavedWork = expectedSavedWork
              MeasuredOverhead = measuredOverhead
              Tier = ReplayTier }

    let feature159ReasonToken reason =
        match reason with
        | Feature159Reason.Instability -> "instability"
        | Feature159Reason.LowCost -> "low-cost"
        | Feature159Reason.OverheadExceedsSavedWork -> "overhead-exceeds-saved-work"
        | Feature159Reason.StaleContentIdentity -> "stale-content-identity"
        | Feature159Reason.StalePlacementIdentity -> "stale-placement-identity"
        | Feature159Reason.AmbiguousIdentity -> "ambiguous-identity"
        | Feature159Reason.CrossProfileEvidence -> "cross-profile-evidence"
        | Feature159Reason.MissingRetainedContent -> "missing-retained-content"
        | Feature159Reason.ResourceLimited -> "resource-limited"
        | Feature159Reason.UnsupportedHost -> "unsupported-host"
        | Feature159Reason.ParityMismatch -> "parity-mismatch"
        | Feature159Reason.NonBeneficialCounters -> "non-beneficial-counters"
        | Feature159Reason.RunIdentityMismatch -> "run-identity-mismatch"
        | Feature159Reason.ScenarioDefinitionMismatch -> "scenario-definition-mismatch"
        | Feature159Reason.MissingPolicy -> "missing-policy"
        | Feature159Reason.EnvironmentLimited -> "environment-limited"

    let feature159PromotionStatusToken status =
        match status with
        | Feature159PromotionStatus.Promoted -> "promoted"
        | Feature159PromotionStatus.Observing -> "observing"
        | Feature159PromotionStatus.Kept -> "kept"
        | Feature159PromotionStatus.Demoted -> "demoted"
        | Feature159PromotionStatus.Rejected -> "rejected"
        | Feature159PromotionStatus.Bypassed -> "bypassed"
        | Feature159PromotionStatus.NonBeneficial -> "non-beneficial"
        | Feature159PromotionStatus.FallbackOnly -> "fallback-only"
        | Feature159PromotionStatus.PromotionEnvironmentLimited -> "environment-limited"

    let feature159ReuseStatusToken status =
        match status with
        | Feature159ReuseStatus.ContentReusedPlacementUpdated -> "content-reused-placement-updated"
        | Feature159ReuseStatus.ContentRecorded -> "content-recorded"
        | Feature159ReuseStatus.ContentRerecorded -> "content-re-recorded"
        | Feature159ReuseStatus.FallbackFullRedraw -> "fallback-full-redraw"
        | Feature159ReuseStatus.ReuseRejected -> "reuse-rejected"
        | Feature159ReuseStatus.ReuseEnvironmentLimited -> "environment-limited"

    let private feature159Hash (parts: string list) =
        let mutable hash = 0xcbf29ce484222325UL // mutable: compact deterministic FNV-1a fold.
        for part in parts do
            for ch in part do
                hash <- hash ^^^ uint64 (int ch)
                hash <- hash * 1099511628211UL
            hash <- hash ^^^ uint64 (int '|')
            hash <- hash * 1099511628211UL
        hash

    let private rectToken (rect: Rect option) =
        match rect with
        | None -> "none"
        | Some r -> sprintf "%.3f,%.3f,%.3f,%.3f" r.X r.Y r.Width r.Height

    let feature159ContentIdentity boundaryId runId localContentFingerprint artifactPath =
        { BoundaryId = boundaryId
          ContentId = feature159Hash [ boundaryId; runId; string localContentFingerprint; "content-v1" ]
          LocalContentFingerprint = localContentFingerprint
          AlgorithmVersion = "content-identity-v1"
          RunId = runId
          ArtifactPath = artifactPath }

    let feature159PlacementIdentity boundaryId box scrollOffsetX scrollOffsetY scale coverage =
        let coverageToken =
            coverage
            |> List.map (Some >> rectToken)
            |> String.concat ";"

        { BoundaryId = boundaryId
          PlacementId =
            feature159Hash
                [ boundaryId
                  rectToken box
                  sprintf "%.3f" scrollOffsetX
                  sprintf "%.3f" scrollOffsetY
                  sprintf "%.3f" scale
                  coverageToken
                  "placement-v1" ]
          Box = box
          ScrollOffsetX = scrollOffsetX
          ScrollOffsetY = scrollOffsetY
          Scale = scale
          Coverage = coverage
          AlgorithmVersion = "placement-identity-v1" }

    let private zeroFeature159Counters =
        { AvoidedContentWork = 0
          PlacementOnlyReuseCount = 0
          ContentRecordCount = 0
          ContentRerecordCount = 0
          PromotionCount = 0
          DemotionCount = 0
          FallbackCount = 0
          ReplayHits = 0
          ReplayMisses = 0
          ReplayRecords = 0
          PromotionOverhead = 0
          NetSavedWork = 0 }

    let private feature159ReuseDecision
        status
        reason
        (priorContent: Feature159ContentIdentity option)
        (currentContent: Feature159ContentIdentity)
        (priorPlacement: Feature159PlacementIdentity option)
        (currentPlacement: Feature159PlacementIdentity)
        counters =
        { BoundaryId = currentContent.BoundaryId
          Status = status
          PrimaryReason = reason
          PriorContentIdentity = priorContent
          CurrentContentIdentity = Some currentContent
          PriorPlacementIdentity = priorPlacement
          CurrentPlacementIdentity = Some currentPlacement
          CounterDelta = counters
          ArtifactPaths = [] }

    let feature159ClassifyReuse
        (priorContent: Feature159ContentIdentity option)
        (currentContent: Feature159ContentIdentity)
        (priorPlacement: Feature159PlacementIdentity option)
        (currentPlacement: Feature159PlacementIdentity)
        retainedResident
        sameProfile
        parityPassed
        resourceLimited =
        let recordCounters =
            { zeroFeature159Counters with
                ContentRecordCount = 1
                ReplayMisses = 1
                ReplayRecords = 1
                NetSavedWork = -1 }

        let rerecordCounters =
            { zeroFeature159Counters with
                ContentRerecordCount = 1
                ReplayMisses = 1
                ReplayRecords = 1
                NetSavedWork = -1 }

        if resourceLimited then
            feature159ReuseDecision Feature159ReuseStatus.FallbackFullRedraw (Some Feature159Reason.ResourceLimited) priorContent currentContent priorPlacement currentPlacement { zeroFeature159Counters with FallbackCount = 1 }
        elif not sameProfile then
            feature159ReuseDecision Feature159ReuseStatus.ReuseRejected (Some Feature159Reason.CrossProfileEvidence) priorContent currentContent priorPlacement currentPlacement zeroFeature159Counters
        elif not parityPassed then
            feature159ReuseDecision Feature159ReuseStatus.ReuseRejected (Some Feature159Reason.ParityMismatch) priorContent currentContent priorPlacement currentPlacement zeroFeature159Counters
        elif not retainedResident then
            feature159ReuseDecision Feature159ReuseStatus.FallbackFullRedraw (Some Feature159Reason.MissingRetainedContent) priorContent currentContent priorPlacement currentPlacement { zeroFeature159Counters with FallbackCount = 1 }
        else
            match priorContent, priorPlacement with
            | None, _ ->
                feature159ReuseDecision Feature159ReuseStatus.ContentRecorded None priorContent currentContent priorPlacement currentPlacement recordCounters
            | Some prior, _ when prior.ContentId <> currentContent.ContentId ->
                feature159ReuseDecision Feature159ReuseStatus.ContentRerecorded None priorContent currentContent priorPlacement currentPlacement rerecordCounters
            | Some _, None ->
                feature159ReuseDecision Feature159ReuseStatus.ContentRecorded (Some Feature159Reason.StalePlacementIdentity) priorContent currentContent priorPlacement currentPlacement recordCounters
            | Some _, Some placement when placement.PlacementId <> currentPlacement.PlacementId ->
                let counters =
                    { zeroFeature159Counters with
                        AvoidedContentWork = 1
                        PlacementOnlyReuseCount = 1
                        ReplayHits = 1
                        NetSavedWork = 1 }

                feature159ReuseDecision Feature159ReuseStatus.ContentReusedPlacementUpdated None priorContent currentContent priorPlacement currentPlacement counters
            | Some _, Some _ ->
                let counters =
                    { zeroFeature159Counters with
                        AvoidedContentWork = 1
                        ReplayHits = 1
                        NetSavedWork = 1 }

                feature159ReuseDecision Feature159ReuseStatus.ContentReusedPlacementUpdated None priorContent currentContent priorPlacement currentPlacement counters

    let feature159EvaluatePromotion (candidate: Feature159PromotionCandidate) parity =
        let status, reason =
            if candidate.ResourceLimited then
                Feature159PromotionStatus.Bypassed, Some Feature159Reason.ResourceLimited
            elif not candidate.ParityPassed then
                Feature159PromotionStatus.Rejected, Some Feature159Reason.ParityMismatch
            elif not candidate.ContentStable then
                Feature159PromotionStatus.Demoted, Some Feature159Reason.Instability
            elif candidate.ObservedStabilityFrames < candidate.ObservationWindow then
                Feature159PromotionStatus.Observing, Some Feature159Reason.Instability
            elif candidate.ExpectedSavedWork <= candidate.MeasuredOverhead then
                Feature159PromotionStatus.NonBeneficial, Some Feature159Reason.OverheadExceedsSavedWork
            elif candidate.ReductionPercent < 30.0 then
                Feature159PromotionStatus.Kept, Some Feature159Reason.LowCost
            else
                Feature159PromotionStatus.Promoted, None

        { BoundaryId = candidate.BoundaryId
          Status = status
          PrimaryReason = reason
          ObservedStabilityFrames = candidate.ObservedStabilityFrames
          ExpectedSavedWork = candidate.ExpectedSavedWork
          MeasuredOverhead = candidate.MeasuredOverhead
          ReductionPercent = candidate.ReductionPercent
          TargetTier = ReplayTier
          Parity = parity
          ArtifactPaths = [] }

    let feature159CountersFromWork (work: WorkReductionRecord) =
        { AvoidedContentWork = work.AvoidedContentWork
          PlacementOnlyReuseCount = work.PlacementOnlyReuseCount
          ContentRecordCount = work.ContentRecordCount
          ContentRerecordCount = work.ContentRerecordCount
          PromotionCount = work.PromotionCount
          DemotionCount = work.DemotionCount
          FallbackCount = work.FallbackCount
          ReplayHits = work.ReplayHits
          ReplayMisses = work.ReplayMisses
          ReplayRecords = work.ReplayRecords
          PromotionOverhead = work.PromotionOverhead
          NetSavedWork = work.NetSavedWork }

    let snapshotVerdict supported (byteEstimate: int64) (byteBudget: int64) (benefitPercent: float) (thresholdPercent: float) =
        if not supported then
            SnapshotLimited "snapshot host unsupported"
        elif byteEstimate > byteBudget then
            SnapshotDemoted "snapshot budget exceeded"
        elif benefitPercent < thresholdPercent then
            SnapshotDemoted "snapshot benefit below threshold"
        else
            SnapshotReady

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
        let layoutRoot, boundsById, layoutResult =
            RetainedRenderTrace.time "retained-init-layout" [] (fun () -> ControlInternals.evaluateLayout size control)

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
            RetainedRenderTrace.time
                "retained-build-paint-own"
                [ "kind", nc.Kind
                  "memoizable", string (isMemoizable nc) ]
                (fun () ->
                    if isMemoizable nc then
                        let dep = memoDependency theme boundsById path nc
                        let id = nc.Key |> Option.defaultValue path
                        let subtree, memo', _ = memoize id dep (fun () -> ControlInternals.paintNode theme boundsById path nc) memo
                        memo <- memo'
                        subtree
                    else
                        ControlInternals.paintNode theme boundsById path nc)

        let rec build (path: string) (nc: Control<'msg>) : RetainedNode<'msg> =
            let own = paintOwn path nc
            let children = nc.Children |> List.mapi (fun i child -> build (childPath path i) child)
            let box = ControlInternals.nodeBox boundsById path nc
            // Feature 137 (US1/US2): clip children to the node box + collect the overlay contribution.
            let assembly = assembleRetainedNode nc box own children

            { Identity = mint ()
              Control = nc
              Fragment = retainedFragment own assembly box (evidence FreshFallback InitialAssembly None assembly box)
              Metadata = retainedMetadata path nc box children
              Children = children }

        let root =
            RetainedRenderTrace.time
                "retained-init-build"
                [ "nodeCount", string (Control.count control) ]
                (fun () -> build "0" control)

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

        RetainedRenderTrace.time "retained-init-seed-pictures" [] (fun () -> seedPictures root)

        let render: ControlRenderResult<'msg> =
            // Feature 137 (US2): in-flow first, then the deferred z-top overlay group (empty ⇒ unchanged).
            RetainedRenderTrace.time
                "retained-init-render-result"
                [ "metadataNodeCount", string root.Metadata.NodeCount ]
                (fun () ->
                    let sceneList = root.Fragment.Assembly.InFlowScene @ root.Fragment.Assembly.OverlayScene
                    renderFromRetainedMetadata theme size layoutRoot sceneList root.Metadata)

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
        let result =
            RetainedRenderTrace.time "retained-step-diff" [] (fun () -> Reconcile.diff prev.Root.Control next)

        // (2) layout of `next` via the INCREMENTAL evaluator (R2, FR-005): re-measure only the
        //     patch-derived dirty set (conservatively propagated to its flex line / fixed-size
        //     ancestor) and reuse the previous frame's cached bounds for everything else. The result
        //     `Bounds` are byte-identical to a full `evaluateLayout` (INV-1), so the reuse-driven paint
        //     walk below (`box = pr.Fragment.Box`) and the surfaced Bounds are unchanged.
        let dirty =
            RetainedRenderTrace.time "retained-step-layout-dirty-set" [] (fun () -> layoutDirtySet prev.Root.Control result.Patch next)
        // FR-006: the size of the layout dirty set fed into incremental layout this frame (the
        // patch-derived self-dirty nodes BEFORE fixed-size-ancestor propagation). Distinct from the
        // post-pinning `remeasured` below; `invalidated <= remeasured` because propagation expands each
        // dirty node to its first fixed-size ancestor's whole subtree (a superset). `0` on an idle /
        // style-only / visual-state-only frame (no layout-affecting attr changed → empty dirty set).
        let invalidated = Set.count dirty

        // Feature 117/138: install the per-frame text-measure cache over THIS frame's layout + paint
        // measurement. The cache still reuses same-frame inserts, but metric hits mean prior-frame reuse:
        // only keys resident before this frame began count as hits. A cold repeated text therefore records
        // the first insert as a miss and the same-frame reuse as neither a hit nor another miss.
        let mutable tc = prev.TextCache
        let frameStartTextKeys = prev.TextCache.Entries |> Map.toSeq |> Seq.map fst |> Set.ofSeq
        let mutable textHits = 0
        let mutable textMisses = 0

        let measureCached (text: string) (font: FS.GG.UI.Scene.FontSpec) : FS.GG.UI.Scene.TextMetrics =
            let key: TextMeasureKey =
                { Text = text
                  Family = font.Family
                  Size = font.Size
                  Weight = font.Weight
                  MeasurementVersionBucket = FS.GG.UI.Scene.Scene.textMeasurementVersionBucket () }

            let metrics, tc', wasHit = measureTextCached tc prev.TextCacheEnabled text font
            tc <- tc'
            if not prev.TextCacheEnabled then
                textMisses <- textMisses + 1
            elif wasHit then
                if Set.contains key frameStartTextKeys then
                    textHits <- textHits + 1
            else
                textMisses <- textMisses + 1
            metrics

        // Active for the WHOLE measurement window of this frame — the incremental layout pass AND the
        // reuse-driven paint walk below (`build` → `paintNode`/geom). Cleared right after `build` (nothing
        // past it measures text). `step` is total (the diff/layout/paint paths never throw), so the
        // explicit clear always runs; `ThreadStatic` isolates concurrent test `step`s.
        ControlInternals.setMeasureTextHook (Some measureCached)

        let root, boundsById, layoutResult =
            RetainedRenderTrace.time
                "retained-step-layout-incremental"
                [ "dirtyCount", string invalidated ]
                (fun () -> ControlInternals.evaluateLayoutIncremental size next prev.Layout dirty)
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
        // Feature 174: retained render-result metadata visits. Reused `Keep` subtrees keep their
        // snapshot and add no per-frame metadata work.
        let mutable metadataVisited = 0

        let mint () =
            let id = RetainedId nextId
            nextId <- nextId + 1UL
            id

        let metadataFor path control box children =
            metadataVisited <- metadataVisited + 1
            retainedMetadata path control box children

        // Feature 113 (Phase 5): paint a node's OWN scene, routing the sole memoized site (the DataGrid
        // projection) through the memo seam. A HIT reuses the stored projection (its theme/box/cells
        // dependency was unchanged) without recomputing; a MISS recomputes and stores it. With
        // `MemoEnabled = false` (the always-miss oracle, FR-008) every node paints directly — nothing is
        // reused — so the rendered scene is byte-identical to the seam-active build (memo-on ≡ memo-off).
        let paintOwn (path: string) (nc: Control<'msg>) : FS.GG.UI.Scene.Scene list =
            RetainedRenderTrace.time
                "retained-build-paint-own"
                [ "kind", nc.Kind
                  "memoizable", string (prev.MemoEnabled && isMemoizable nc) ]
                (fun () ->
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
                        ControlInternals.paintNode theme boundsById path nc)

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
        let rec buildFresh (reason: RetainedInvalidationReason) (path: string) (nc: Control<'msg>) : RetainedNode<'msg> =
            let own = paintFresh path nc
            let children = nc.Children |> List.mapi (fun i child -> buildFresh reason (childPath path i) child)
            let box = ControlInternals.nodeBox boundsById path nc
            // Feature 137 (US1/US2): clip children to the node box + collect the overlay contribution.
            let assembly = assembleRetainedNode nc box own children

            { Identity = mint ()
              Control = nc
              Fragment = retainedFragment own assembly box (evidence FreshFallback reason None assembly box)
              Metadata = metadataFor path nc box children
              Children = children }

        // Recompute a structurally-identical subtree whose box SHIFTED (a `Keep` relaid out by an
        // upstream change) while CARRYING every node's prior identity — it is the same node.
        let rec carry (path: string) (pr: RetainedNode<'msg>) (nc: Control<'msg>) : RetainedNode<'msg> =
            shifted <- shifted + 1
            let own = paintFresh path nc

            let children =
                List.map2 (fun p c -> p, c) pr.Children nc.Children
                |> List.mapi (fun i (p, c) -> carry (childPath path i) p c)

            let box = ControlInternals.nodeBox boundsById path nc
            // Feature 137 (US1/US2): clip children to the node box + collect the overlay contribution.
            let assembly = assembleRetainedNode nc box own children
            let reason = if themeChanged then ThemeInput else LayoutInput

            { Identity = pr.Identity
              Control = nc
              Fragment = retainedFragment own assembly box (evidence Rebuilt reason (Some pr.Fragment) assembly box)
              Metadata = metadataFor path nc box children
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
                    { pr with
                        Control = nc
                        Fragment =
                            { pr.Fragment with
                                InvalidationEvidence =
                                    [ evidence Reused StableInputs (Some pr.Fragment) pr.Fragment.Assembly box ] } }
                else
                    // an upstream layout change shifted this subtree, or the theme changed (FR-008):
                    // recompute under the new theme/box, carrying identities (the node is the same).
                    carry path pr nc

            | Reconcile.NodePatch.Replace _ ->
                // Kind/Key changed -> a different node. Mint a fresh identity; the old identity
                // (and its UI state) is dropped — no false identity across a Replace (SC-001 -).
                changedBound <- changedBound + Control.count nc
                buildFresh ExplicitIdentity path nc

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
                            buildFresh ChildInsertion cp node
                        // Unreachable (ChildRemove is filtered out of `producing`); kept total —
                        // paint the next child fresh rather than throw.
                        | Reconcile.ChildRemove _ -> buildFresh ChildRemoval cp c)

                // Feature 137 (US1/US2): clip children to the node box + collect the overlay contribution.
                let assembly = assembleRetainedNode nc box own children
                let hasChildInsertion =
                    u.Children
                    |> List.exists (function
                        | Reconcile.ChildInsert _ -> true
                        | _ -> false)

                let hasChildRemoval =
                    u.Children
                    |> List.exists (function
                        | Reconcile.ChildRemove _ -> true
                        | _ -> false)

                let hasChildMove =
                    u.Children
                    |> List.exists (function
                        | Reconcile.ChildMove _ -> true
                        | _ -> false)

                let hasLayoutAttrChange =
                    u.AttrChanges
                    |> List.exists (fun change ->
                        match change with
                        | Reconcile.AttrSet attr ->
                            attr.Category = AttrCategory.Layout
                            || Set.contains attr.Name ControlInternals.layoutAffectingAttrNames
                        | Reconcile.AttrRemoved name ->
                            Set.contains name ControlInternals.layoutAffectingAttrNames
                            || (pr.Control.Attributes |> List.exists (fun attr -> attr.Name = name && attr.Category = AttrCategory.Layout)))

                let reason =
                    if hasChildRemoval then ChildRemoval
                    elif hasChildInsertion then ChildInsertion
                    elif hasChildMove then ChildOrdering
                    elif hasLayoutAttrChange then LayoutInput
                    elif not ownUnchanged then VisualInput
                    else StableInputs

                { Identity = pr.Identity
                  Control = nc
                  Fragment = retainedFragment own assembly box (evidence Rebuilt reason (Some pr.Fragment) assembly box)
                  Metadata = metadataFor path nc box children
                  Children = children }

        let newRoot =
            RetainedRenderTrace.time "retained-step-build" [] (fun () -> build "0" prev.Root result.Patch next)

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

        RetainedRenderTrace.time "retained-step-count-virtual" [] (fun () -> countVirtual next)

        // Feature 116 (Phase 7, FR-001/FR-004): reduce the accumulated damage set to its three integer
        // carriers — repainted-node count, count of DISTINCT repainted boxes, and summed integer area
        // over the distinct boxes. Deterministic (integer geometry → reproducible across runs).
        let repaintedNodeCount = recomputed
        let distinctBoxes, dirtyRectCount, dirtyArea =
            RetainedRenderTrace.time
                "retained-step-damage-reduce"
                [ "repaintedBoxes", string repaintedBoxes.Count ]
                (fun () ->
                    let distinctBoxes = repaintedBoxes |> Seq.distinct |> List.ofSeq
                    let dirtyRectCount = List.length distinctBoxes
                    // Feature 120 (US4, FR-015): the damage area is the area of the UNION of the distinct damage
                    // rectangles (no longer the sum), so overlapping damage is counted once and the value never
                    // exceeds the frame area. Computed by coordinate-compression over the distinct boxes (n is the
                    // small dirty-rect count, integer control geometry → deterministic), then clamped to the frame.
                    let frameArea = size.Width * size.Height
                    distinctBoxes, dirtyRectCount, unionArea distinctBoxes frameArea)

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
                    replaySkippedNodes <- replaySkippedNodes + countNodes n.Fragment.Assembly.InFlowScene
                else
                    pictureMisses <- pictureMisses + 1

                // Native-byte model: every cacheable boundary resident after this frame holds a recorded
                // picture proportional to its subtree node count (bounded by the cap).
                if prev.PictureCacheEnabled then
                    replayNativeBytes <- replayNativeBytes + countNodes n.Fragment.Assembly.InFlowScene * bytesPerNode

                pcEntries <- Map.add n.Identity (pcClock, key) pcEntries

                while pcEntries.Count > PictureCacheCap do
                    let lruId, _ = pcEntries |> Map.toSeq |> Seq.minBy (fun (_, (stamp, _)) -> stamp)
                    pcEntries <- Map.remove lruId pcEntries

            n.Children |> List.iter walkPictures

        RetainedRenderTrace.time "retained-step-picture-walk" [] (fun () -> walkPictures newRoot)
        let pictureEntryCount = pcEntries.Count
        // Bound the modeled native bytes by the cap (residency never exceeds PictureCacheCap entries).
        let replayCacheNativeBytes = min replayNativeBytes (PictureCacheCap * bytesPerNode * 64)
        let pictureCache: PictureCache = { Entries = pcEntries; Clock = pcClock }
        let avoidedContentWork = max 0 (Control.count next - recomputed) + replaySkippedNodes
        let promotionOverhead = pictureHits + pictureMisses
        let netSavedWork = avoidedContentWork - promotionOverhead

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

        RetainedRenderTrace.time "retained-step-offscreen-diagnostics" [] (fun () -> collectOffscreen newRoot)

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

        RetainedRenderTrace.time "retained-step-index-prior-own" [] (fun () -> indexPriorOwn prev.Root)

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

        let stateById =
            RetainedRenderTrace.time "retained-step-state-collect" [] (fun () -> collect newRoot Map.empty)

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
        // reuse-stable boundary is at rest, so its wrapped content is exactly `Fragment.Assembly.InFlowScene`.
        let needsEmitWalk = anyActive || replayHitIds.Count > 0

        let sceneList =
            RetainedRenderTrace.time
                "retained-step-scene-assembly"
                [ "anyActive", string anyActive
                  "replayBoundaryCount", string replayHitIds.Count ]
                (fun () ->
                    if not needsEmitWalk then
                        // Feature 137 (US2): in-flow first, then the deferred z-top overlay group (empty ⇒ unchanged).
                        newRoot.Fragment.Assembly.InFlowScene @ newRoot.Fragment.Assembly.OverlayScene
                    else
                        // Feature 139 (R1a): the emit walk also uses the shared current-node assembly owner, with
                        // each retained fragment supplying the node box and child assemblies.
                        let rec assemble (n: RetainedNode<'msg>) : ControlInternals.CurrentNodeAssemblyResult =
                            if replayHitIds.Contains n.Identity then
                                let (RetainedId cacheId) = n.Identity
                                // A cacheable boundary (data-grid-row) is a leaf with no overlay descendants; carry
                                // its (empty) overlay contribution for completeness.
                                let inFlow =
                                    [ { Nodes =
                                          [ CachedSubtree
                                                { CacheId = cacheId
                                                  Fingerprint = n.Fragment.Assembly.Fingerprint
                                                  Scene = Scene.group n.Fragment.Assembly.InFlowScene } ] } ]
                                let overlay = n.Fragment.Assembly.OverlayScene

                                { InFlowScene = inFlow
                                  OverlayScene = overlay
                                  InFlowFingerprint = ControlInternals.hashScene inFlow
                                  OverlayFingerprint = ControlInternals.hashScene overlay
                                  Fingerprint = ControlInternals.hashScene (inFlow @ overlay)
                                  Diagnostics = []
                                  ChildContributions = [] }
                            else
                                let ownStatic = n.Fragment.OwnScene

                                let own =
                                    match Map.tryFind n.Identity stateById |> Option.bind (fun s -> s.Animation) with
                                    | Some c when clockActive c -> sampleOnPaint c ownStatic
                                    | _ -> ownStatic

                                let childAssemblies = n.Children |> List.map assemble
                                ControlInternals.assembleCurrentNode n.Control n.Fragment.Box own childAssemblies

                        let assembled = assemble newRoot
                        assembled.InFlowScene @ assembled.OverlayScene)

        // Byte-identical to `Control.renderTree theme size next` AT REST: `SubtreeScene` is the
        // pre-order concatenation of `paintNode` over every node — the same list `renderTree`'s paint
        // builds. An active clock contributes a paint-level overlay scoped to its own identity.
        let render: ControlRenderResult<'msg> =
            RetainedRenderTrace.time
                "retained-step-render-result"
                [ "sceneListCount", string (List.length sceneList)
                  "metadataVisitedNodeCount", string metadataVisited
                  "metadataNodeCount", string newRoot.Metadata.NodeCount ]
                (fun () -> renderFromRetainedMetadata theme size root sceneList newRoot.Metadata)

        let baselineNodeCount =
            RetainedRenderTrace.time "retained-step-work-node-count" [] (fun () -> newRoot.Metadata.NodeCount)

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
            { BaselineNodeCount = baselineNodeCount
              MetadataVisitedNodeCount = metadataVisited
              MetadataFallbackCount = 0
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
              ReplayCacheNativeBytes = replayCacheNativeBytes
              AvoidedContentWork = avoidedContentWork
              PlacementOnlyReuseCount = pictureHits
              ContentRecordCount = pictureMisses
              ContentRerecordCount = if changedBound > 0 then pictureMisses else 0
              PromotionCount = if pictureHits > 0 && netSavedWork > 0 then 1 else 0
              DemotionCount = if pictureHits = 0 && pictureMisses > 0 && netSavedWork <= 0 then 1 else 0
              FallbackCount = 0
              PromotionOverhead = promotionOverhead
              NetSavedWork = netSavedWork } }

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

    /// F2 (Feature 175 FR-009): the single offset-aware queryable layout. `retained.Layout` is the RAW
    /// incremental cache (pre-shifting it would double-shift reused descendant bounds each frame), so the
    /// scroll-offset shift is re-applied HERE — once — for every `LayoutResult` hit-test consumer.
    let hitTestLayout (retained: RetainedRender<'msg>) : FS.GG.UI.Layout.LayoutResult =
        ControlInternals.applyScrollOffsets retained.Root.Control retained.Layout

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

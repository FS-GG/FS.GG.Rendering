namespace FS.GG.UI.SkiaViewer

/// Feature 120 (US3) — internal bounded LRU of recorded `SKPicture`s keyed by `CachedSubtree.CacheId`,
/// validated by `Fingerprint`. Owns native picture lifetime (records on a miss, replays on a valid hit,
/// disposes on eviction/replacement/teardown). The load-bearing backend realization of the controls
/// picture cache — it actually skips the per-primitive draw-call walk on a hit. Not part of the public
/// package surface. A disabled cache is the parity oracle (FR-011): every boundary recurses directly.
module internal PictureReplayCache =

    /// Default capacity (mirrors `RetainedRender.PictureCacheCap = 256`).
    val cap: int

    [<Sealed>]
    type internal Cache

    /// Feature 159: backend replay fallback reasons for split content/placement keys.
    type internal SplitReplayFallbackReason =
        | MissingResidentPicture
        | ChangedRunOrProfile
        | DisabledReplay
        | ResourceLimitedRetention
        | ParityMismatch

    /// Feature 159: pure request shape for content-keyed replay with separate placement evidence.
    type internal SplitReplayRequest =
        { ContentCacheId: uint64
          ContentFingerprint: uint64
          PlacementFingerprint: uint64
          RunProfileMatches: bool
          RetainedResident: bool
          ResourceLimited: bool
          ParityPassed: bool
          ReplayEnabled: bool }

    /// Feature 159: replay decision diagnostics. Placement-only change is true when content matches
    /// and placement differs from the previous request.
    type internal SplitReplayDecision =
        { Status: string
          FallbackReason: SplitReplayFallbackReason option
          ContentKey: uint64
          PlacementOnlyChange: bool
          RecordRequired: bool }

    /// Create an empty cache. `enabled = false` makes every `paintBoundary` recurse directly into the
    /// boundary scene (the always-direct parity oracle) — never recording or replaying.
    val create: enabled: bool -> Cache

    /// Classify a split replay request without touching native resources. A content match with changed
    /// placement is a replay hit only when run/profile, residency, resource, parity, and enabled gates pass.
    val classifySplitReplay:
        previous: SplitReplayRequest option ->
        current: SplitReplayRequest ->
            SplitReplayDecision

    /// Paint a `CachedSubtree` boundary: on a valid hit (resident + matching fingerprint) replay the
    /// recorded picture via `DrawPicture`; otherwise record (`SKPictureRecorder` over the canvas's device
    /// bounds → paint the scene → `EndRecording`), disposing any replaced picture, then draw. Updates the
    /// deterministic min-`Stamp` LRU and disposes evicted pictures. `paintScene` is the direct walk used to
    /// record and (when disabled) to draw.
    val paintBoundary:
        cache: Cache ->
        canvas: SkiaSharp.SKCanvas ->
        paintScene: (SkiaSharp.SKCanvas -> FS.GG.UI.Scene.Scene -> unit) ->
        boundary: FS.GG.UI.Scene.CacheBoundary ->
            unit

    /// Live entry count, native-byte total, and per-lifetime hit/miss/record/skipped-node counters
    /// (FR-013/FR-014) for the non-golden live timing baseline.
    val stats:
        cache: Cache ->
            {| Entries: int
               NativeBytes: int
               Hits: int
               Misses: int
               Records: int
               SkippedNodes: int |}

    /// Reset the per-frame hit/miss/record/skipped counters (the residency + native bytes persist).
    val resetCounters: cache: Cache -> unit

    /// Dispose all resident pictures (teardown).
    val dispose: cache: Cache -> unit

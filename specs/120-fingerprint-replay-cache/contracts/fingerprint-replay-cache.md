# Contract — Structural Fingerprint & Backend Replay Cache (Feature 120)

Internal fingerprint/replay/metrics (pinned via `InternalsVisibleTo`) + the public Scene boundary types
(already baselined) + additive public `FrameMetrics`. Signatures from `RetainedRender.fsi` / `Scene.fsi` /
`PictureReplayCache.fsi` / `OpenGl.fsi` / `ControlsElmish.fsi`; behaviour clauses are what the five suites assert.

## C1 — `hashScene` (the structural fingerprint, internal)

```fsharp
val internal hashScene: scenes: FS.GG.UI.Scene.Scene list -> uint64
```

- Collision-resistant FNV-1a, no truncation. Identical scenes ⇒ equal (deterministic); any single
  render-affecting change (incl. opacity) ⇒ different. The replay key.

*Pins*: FR-008, FR-010. *Used by*: US1.

## C2 — `CachedSubtree` / `CacheBoundary` (public Scene types, already baselined)

```fsharp
| CachedSubtree of CacheBoundary
and CacheBoundary = { CacheId: uint64; Fingerprint: uint64; Scene: Scene }
```

- Transparent to every consumer except the GL/raster backend painter; disabled ⇒ recurse into `Scene`
  identically (parity oracle).

*Pins*: FR-007, FR-012. *Used by*: US2.

## C3 — `PictureReplayCache` (the backend LRU, module internal)

```fsharp
val create: enabled: bool -> Cache
val paintBoundary: cache: Cache -> canvas: SKCanvas -> paintScene: (SKCanvas -> Scene -> unit) -> boundary: CacheBoundary -> unit
val stats: cache: Cache -> {| Entries: int; NativeBytes: int; Hits: int; Misses: int; Records: int; SkippedNodes: int |}
val dispose: cache: Cache -> unit   // + resetCounters, cap
```

- Matching fingerprint ⇒ replay (skip the draw walk); changed/cold/evicted ⇒ re-record (no stale hit);
  `Entries ≤ cap`; disabled ⇒ never record/replay (oracle); cache-on **pixel-identical** to direct (PNG
  readback); `dispose` releases all resident pictures.

*Pins*: FR-007, FR-009, FR-010, FR-011, FR-013. *Used by*: US2.

## C4 — Replay metrics (`FrameMetrics`, additive public)

```fsharp
ReplayHitCount / ReplayMissCount / ReplayRecordCount / ReplaySkippedNodeCount / ReplayCacheNativeBytes : int
PaintDuration / ComposeDuration : System.TimeSpan   // live-only; TimeSpan.Zero on the deterministic path
```

- Stable frame: `ReplayHitCount == PictureCacheHitCount`, records = misses, skipped/native-bytes > 0; idle ⇒
  zero replay work; timing `== TimeSpan.Zero` on the deterministic path.

*Pins*: FR-001, FR-002, FR-014. *Used by*: US3.

## C5 — `unionArea` (the damage union, internal)

```fsharp
val internal unionArea: boxes: FS.GG.UI.Scene.Rect list -> frameArea: int -> int
```

- Union area (overlap once), clamped to `frameArea`; disjoint sums; empty 0; never exceeds the frame.

*Pins*: FR-015. *Used by*: US4.

## C6 — `GlHost.shouldPresent` (idle-skip, public val)

- Present iff first frame / scene changed / size changed.

*Pins*: FR-004, FR-005, FR-006. *Used by*: US2.

## Surface-drift

- **Zero new public-surface-baseline delta** (FR-016): `hashScene`/`unionArea`/`PictureReplayCache`/replay
  metric carriers are `internal`; `CacheBoundary`/`CachedSubtree` are public Scene types **already in the
  committed baseline** (`FS.GG.UI.Scene.txt`); the replay/timing `FrameMetrics` fields are additive on the
  already-baselined public type. Baselines stay byte-unchanged.
</content>

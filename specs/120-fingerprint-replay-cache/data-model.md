# Phase 1 — Data Model: Structural Fingerprint & Backend Replay Cache (Feature 120)

The 120-in-scope entities. Internal except the public Scene types `CacheBoundary`/`CachedSubtree` (already
baselined) and the replay/timing fields additive on the already-baselined public `FrameMetrics`.
`hashScene`/`unionArea` are pure/total/deterministic.

## hashScene (internal)

`Scene list -> uint64` — the collision-resistant FNV-1a structural fingerprint of a painted scene; folds every
render-affecting input with **no truncation**. Identical scenes hash identically; any single render-affecting
change (incl. opacity/alpha) flips it. The replay key. (Replaced 116's `sprintf "%A"` digest.)

## CachedSubtree / CacheBoundary (public Scene types — already baselined)

```fsharp
| CachedSubtree of CacheBoundary
and CacheBoundary = { CacheId: uint64; Fingerprint: uint64; Scene: Scene }
```

- `CacheId`: stable subtree identity (from `RetainedId`). `Fingerprint`: the `hashScene` validation key.
  `Scene`: the wrapped subtree (record source + transparent fallback). Transparent to every consumer except
  the GL/raster backend painter; with replay disabled the painter recurses into `Scene` identically.

## PictureReplayCache (module internal — SkiaViewer)

A bounded LRU of recorded `SKPicture`s keyed by `CacheId`, validated by `Fingerprint`. `cap` mirrors
`PictureCacheCap` (256). API: `create enabled`, `paintBoundary cache canvas paintScene boundary`, `stats`
(`Entries`/`NativeBytes`/`Hits`/`Misses`/`Records`/`SkippedNodes`), `resetCounters`, `dispose`. Matching
fingerprint ⇒ replay (skip the draw walk); changed/cold/evicted ⇒ re-record; `Entries ≤ cap`; disabled ⇒
parity oracle (no record/replay).

## Replay metrics → public FrameMetrics (additive)

`ReplayHits`/`Misses`/`Records`/`SkippedNodes`/`CacheNativeBytes` (internal `FrameMetrics` carriers) →
public `ReplayHitCount`/`ReplayMissCount`/`ReplayRecordCount`/`ReplaySkippedNodeCount`/`ReplayCacheNativeBytes`.
On a stable frame they coincide with the picture-cache counters (`ReplayHitCount == PictureCacheHitCount`),
records = misses, skipped/native-bytes > 0.

## unionArea (internal)

`Rect list -> frameArea:int -> int` — the integer area of the **union** of damage rects (overlap counted once,
clamped to `frameArea`; disjoint sums; empty 0). Corrects 116's sum-of-areas `DirtyArea`. Coordinate-compression
over integer geometry; pure/total/deterministic.

## Present/compose timing + idle-skip (public, additive / public val)

- `FrameMetrics.PaintDuration` / `ComposeDuration: TimeSpan` — live-only; `TimeSpan.Zero` on the deterministic
  path (excluded from the golden surface).
- `GlHost.shouldPresent` — the idle-skip present decision (present iff first frame / scene changed / size changed).

## Relationships

```text
reuse-stable subtree ──hashScene──▶ Fingerprint ──┐
RetainedId ──▶ CacheId ────────────────────────────┤
                                                   ▼
              Scene IR: CachedSubtree (CacheBoundary { CacheId; Fingerprint; Scene })   (transparent to non-backend consumers)
                                                   │  backend painter only
                       PictureReplayCache.paintBoundary ──┬─ Fingerprint matches resident ─▶ replay SKPicture (skip draw walk) ─▶ ReplayHits++ / SkippedNodes
                                                          ├─ changed/cold/evicted ─▶ re-record ─▶ ReplayMisses++ = ReplayRecords++
                                                          ├─ over cap ─▶ evict LRU (Entries <= cap; NativeBytes accounted)
                                                          └─ disabled ─▶ direct walk (parity oracle) ⇒ cache-on ≡ cache-off pixels (SC-003)
   damage rects ──unionArea(frameArea)──▶ DirtyArea (overlap once, clamped)        (corrects 116)
   GlHost.shouldPresent(first|scene-changed|size-changed) ─▶ present | skip (idle)
```
</content>

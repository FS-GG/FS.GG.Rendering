# Phase 1 — Data Model: Picture Cache (LRU) & Damage Set (Feature 116)

The 116-in-scope entities. The cache + carriers are **assembly-internal**; the metrics surface via the
already-baselined public `FrameMetrics`. Modeled deterministically (no live backend).

## PictureCacheKey (internal)

`{ Box: Rect option; Fingerprint: uint64 }` — the complete correctness key for one cacheable boundary.
Equality proves a Hit is byte-identical to a fresh paint; any single changed input ⇒ Miss. *(The `Fingerprint`
is feature 120's FNV `hashScene`, replacing 116's superseded `sprintf "%A"` digest.)*

## PictureCache (internal)

`{ Entries: Map<RetainedId, int * PictureCacheKey>; Clock: int }` — the bounded cross-frame LRU. `Clock` is a
monotonic access stamp advanced by traversal order (no wall-clock); on overflow the least-recently-accessed
entry is dropped; `Entries.Count ≤ PictureCacheCap` at all times.

## PictureCacheCap (internal) — 256

The fixed entry cap. An eviction-pressure scenario drives ~320 distinct rows (1.25 × cap).

## PictureCacheEnabled (internal)

The always-miss / parity oracle switch. `true` on the live path; `false` ⇒ every boundary re-misses
(`PictureCacheHits = 0`), proving cache-on ≡ cache-off byte-identical.

## walkPictures (internal)

The per-frame traversal over cacheable boundaries computing Hit/Miss (refresh recency, evict LRU over cap) and
accumulating the damage set.

## WorkReductionRecord damage + cache counters → public FrameMetrics

| Internal field | Public FrameMetrics field | Meaning |
|---|---|---|
| `RepaintedNodeCount` | `RepaintedNodeCount` | repainted nodes (changed + genuinely-shifted) |
| `DirtyRectCount` | `DirtyRectCount` | distinct **deduped** repainted boxes |
| `DirtyArea` | `DirtyArea` | summed integer area over distinct boxes *(120 corrected this to `unionArea`)* |
| `PictureCacheHits` | `PictureCacheHitCount` | reused boundaries (unchanged key + resident) |
| `PictureCacheMisses` | `PictureCacheMissCount` | recomputed (changed key / cold / evicted) |
| `PictureCacheEntryCount` | `PictureCacheEntryCount` | live bounded-LRU entry count (`≤ cap`) |

## offscreenEffect (internal, advisory)

The pure detector: flags drop-shadow / image-filter / path-clip / non-opaque-over-multi-node-group as
offscreen-forcing; a plain opaque scene and a `RectClip` are not flagged. Surfaced via `step` as an advisory
diagnostic.

## Relationships

```text
cacheable boundary (data-grid-row identity) ──build──▶ PictureCacheKey { Box; Fingerprint }
        │
   walkPictures(PictureCache) ──┬─ key unchanged + resident ─▶ Hit  (reuse picture) ─▶ PictureCacheHits++
                                ├─ changed key / cold / evicted ─▶ Miss (repaint)   ─▶ PictureCacheMisses++
                                └─ over cap ─▶ evict LRU (deterministic; Entries.Count <= cap)
        │
   damage set ─▶ RepaintedNodeCount / DirtyRectCount (deduped) / DirtyArea  ─▶ public FrameMetrics
   PictureCacheEnabled=false ─▶ every boundary misses ⇒ cache-on ≡ cache-off (SC-003)
   offscreenEffect(ownScene) ─▶ advisory diagnostic (drop-shadow/image-filter/path-clip/non-opaque-over-group); RectClip NOT flagged
```
</content>

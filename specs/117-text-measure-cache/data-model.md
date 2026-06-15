# Phase 1 — Data Model: Text-Measure Cache (LRU) (Feature 117)

The 117-in-scope entities. The cache + carriers are **assembly-internal**; the three metric fields are additive
on the already-baselined public `FrameMetrics`. `measureTextCached` is pure/total/deterministic.

## TextMeasureKey (internal)

`{ Text: string; Family: string option; Size: float; Weight: int option }` — the complete correctness key for
one measurement. The available-space constraint is deliberately **not** keyed (Decision 1). Any single
differing field ⇒ Miss.

## TextMeasureCache (internal)

`{ Entries: Map<TextMeasureKey, int * FS.GG.UI.Scene.TextMetrics>; Clock: int }` — the bounded cross-frame LRU,
mirroring the 116 `PictureCache` discipline (fixed cap, monotonic `Clock`, deterministic eviction).
`Entries.Count ≤ TextMeasureCacheCap` always.

## TextMeasureCacheCap (internal) — 256

The fixed cap, aligned with `PictureCacheCap`.

## TextCacheEnabled (internal)

The always-miss / parity oracle switch. `true` on the live path; `false` ⇒ every request re-measures via
`Scene.measureText` (`TextMeasureCacheHits = 0`), proving cache-on ≡ cache-off byte-identical scene + layout.

## measureTextCached (internal)

`cache: TextMeasureCache -> enabled: bool -> text: string -> font: FontSpec -> TextMetrics * TextMeasureCache * bool`.
Returns `(metrics, advanced cache, wasHit)`. A resident key Hits (no `Scene.measureText`); a cold/changed/evicted
key Misses (measured fresh + stored, LRU-evicted over cap). The cached value equals the un-cached measure.

## TextMeasureCacheHits / Misses → FrameMetrics.TextMeasureCacheHitCount / MissCount (public, additive)

Internal per-frame counters on `WorkReductionRecord`, surfaced as additive public `FrameMetrics` fields. Both 0
on a frame that measures no text; under the always-miss oracle, hits = 0.

## LayoutInvalidatedNodeCount (internal → public, additive)

The pre-pinning layout dirty-set size (`Set.count` of feature 097's `layoutDirtySet`), surfaced on
`FrameMetrics`. `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount` (post-pinning, 097's); `0` on
idle / style-only / visual-state-only frames. *(097 owns the dirty-set computation; 117 surfaces this metric.)*

## Relationships

```text
layout + paint measurement of (text, font) ──▶ TextMeasureKey { Text; Family; Size; Weight }
        │
   measureTextCached(cache, enabled) ──┬─ resident key ─▶ Hit  (reuse metrics; no Scene.measureText) ─▶ TextMeasureCacheHits++
                                       ├─ cold/changed/evicted ─▶ Miss (Scene.measureText; store)     ─▶ TextMeasureCacheMisses++
                                       └─ over cap ─▶ evict LRU (deterministic; Entries.Count <= cap)
        │
   enabled=false ─▶ always re-measure ⇒ cache-on ≡ cache-off byte-identical scene + layout (SC-004)
        │
   TextMeasureCacheHits/Misses ─▶ public FrameMetrics.TextMeasureCacheHitCount / MissCount
   layoutDirtySet (097) ─▶ LayoutInvalidatedNodeCount (<= RemeasuredNodeCount; 0 on style-only/idle) ─▶ public FrameMetrics
```
</content>

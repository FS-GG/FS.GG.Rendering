# Phase 0 — Research: Text-Measure Cache (LRU) (Feature 117)

Conformance backfill — recovers the design the imported code embodies. No open `NEEDS CLARIFICATION`.
Reconstructed from `RetainedRender.fsi`/`.fs` (`measureTextCached`/`TextMeasureCache`), `ControlsElmish`, and
the five suites.

## Decision 1 — A complete `(text, family, size, weight)` key; available-space not keyed

- **Decision**: `TextMeasureKey = { Text; Family; Size; Weight }`. The available-space constraint
  (`fittedFontSize` box) is deliberately **not** keyed.
- **Rationale**: `Scene.measureText` depends only on the text + font; available space does not change the
  intrinsic measurement (research R2). Keying only the determinants keeps the key complete *and* minimal — any
  single differing field Misses (FR-002), nothing irrelevant pollutes it.
- **Alternatives considered**: Including the box in the key — rejected: it would needlessly miss on a resize
  that doesn't change the intrinsic text metrics.

## Decision 2 — Cached value equals the un-cached measure by construction

- **Decision**: A Hit returns the stored `TextMetrics`, which equals what `Scene.measureText` would return
  (it is pure, research R5). The always-miss oracle (`TextCacheEnabled = false`) re-measures every request,
  proving cache-on ≡ cache-off byte-identical **scene and layout**.
- **Rationale**: A measure cache is only safe if the cached value is exactly the fresh value; purity makes
  Hit ≡ fresh by construction, so layout boxes / fitted sizes / emitted scene are byte-identical (FR-004/SC-004).
- **Alternatives considered**: Re-deriving an approximate metric — rejected: any divergence corrupts layout.

## Decision 3 — A bounded, deterministic LRU mirroring the 116 picture cache

- **Decision**: `TextMeasureCache` is a fixed-cap (256, aligned with `PictureCacheCap`) LRU with a monotonic
  `Clock`; over the cap the least-recently-used key is evicted; `Entries.Count ≤ cap` always; eviction is
  deterministic and an evicted key re-misses correctly.
- **Rationale**: Bounded memory for a long-running host; reusing the 116 discipline keeps one mental model and
  testable determinism (FR-003/SC-005).
- **Alternatives considered**: An unbounded cache — rejected (leak); a separate eviction policy — rejected
  (needless divergence from 116).

## Decision 4 — Surface text hit/miss + `LayoutInvalidatedNodeCount` as public metrics

- **Decision**: `TextMeasureCacheHits`/`Misses` → public `TextMeasureCacheHitCount`/`MissCount`; and surface
  `LayoutInvalidatedNodeCount` (097's pre-pinning dirty-set size; `≤ RemeasuredNodeCount`; `0` on
  style-only/idle).
- **Rationale**: Reuse + invalidation must be measurable. Surfacing the pre-pinning count alongside 097's
  post-pinning `RemeasuredNodeCount` makes the propagation step auditable (the gap is the propagation cost).
- **Alternatives considered**: Internal-only counters — rejected: warm/cold accrual (SC-001/SC-002) is a
  consumer-visible property.

## Decision 5 — Style-only / visual-state-only frames do zero text/layout work

- **Decision**: A style-only / visual-state-only frame over warm text produces zero text-cache misses and
  `LayoutInvalidatedNodeCount = 0` / `RemeasuredNodeCount = 0`; only a geometry change dirties measure.
- **Rationale**: The caches' value is making common paint-only interactions free of measure/text work; this is
  the precision guard (FR-007/SC-003), and it depends on the feature-101 geometry-name set being unchanged
  (FR-008).
- **Alternatives considered**: Re-measuring on any change — rejected: defeats the cache on hover/focus/style.

## Renderer-mode / evidence honesty

All proofs are deterministic and headless (Hit/Miss outcomes, byte-identical metrics + scene + layout vs the
oracle, bounded-LRU invariants, work-count regimes). Readiness (authored in `/speckit-implement`, since 117
imported without it) makes no pixel/desktop claim.
</content>

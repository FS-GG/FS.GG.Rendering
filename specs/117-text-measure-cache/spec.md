# Feature Specification: Text-Measure Cache (LRU) (Feature 117)

**Feature Branch**: `117-text-measure-cache`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next item in the implementation plan"

## Context

This is a **conformance-backfill** specification — task **C9** in the 2026-06-15 missing-features plan
(Workstream C pattern: 091 / 092 / 093 / 095 / 096 / 099 / 097 / 103 / 110 / 113 / 114 / 116).

Text measurement is a per-frame cost paid during both layout and paint. Feature 117 adds a **bounded
cross-frame text-measure cache** — `measureTextCached` looks up a `(text, family, size, weight)` correctness
key in a fixed-cap (256) LRU; a resident key is a `Hit` (reused without re-invoking `Scene.measureText`), a
cold/changed/evicted key is a `Miss` (measured fresh and stored). The cache **mirrors the 116 picture-cache
discipline** (fixed cap, monotonic `Clock`, deterministic eviction, no stale hit). An always-miss oracle
(`TextCacheEnabled = false`) proves cache-on ≡ cache-off byte-identical (scene **and** layout), because the
cached value equals the un-cached `Scene.measureText` by construction.

117 also **surfaces** the `LayoutInvalidatedNodeCount` metric — the pre-pinning layout dirty-set size — which
is `≤ RemeasuredNodeCount` and `0` on a style-only / visual-state-only / idle frame. (The dirty-set
*computation* is feature 097's incremental layout; 117 owns the **text cache** and the **surfacing** of this
metric.)

The implementation (`TextMeasureKey`/`TextMeasureCache`/`measureTextCached`/`TextMeasureCacheCap` + the
`TextCache`/`TextCacheEnabled` fields + the `TextMeasureCacheHits`/`Misses` counters in
`RetainedRender.fs`/`.fsi`; the public `FrameMetrics.TextMeasureCacheHitCount`/`MissCount`/`LayoutInvalidatedNodeCount`)
and the suites (`Feature117TextCacheTests`, `Feature117CacheBoundTests`, `Feature117LayoutInvalidatedTests` in
`Controls.Tests`; `Feature117MetricsTests` in `Elmish.Tests`; `Audit_TextCache` in `Controls.Tests`)
**already exist** in the imported source. **No Spec Kit spec/plan/tasks describe this work**, and 117 imported
with **no `readiness/`**. This document backfills the contract.

The cache surface is **assembly-internal**; the three new metric fields are additive on the already-baselined
public `FrameMetrics`, so the backfill adds **zero new public-surface-baseline delta** (type-granular).

**Scope boundary.** 117 owns the text-measure cache + the surfacing of `LayoutInvalidatedNodeCount`. The
**picture cache** (116) it mirrors is separate; the **layout dirty-set computation** + `RemeasuredNodeCount`
are feature 097; the geometry-driving-name drift guard is feature 101.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The text-measure cache reuses a measurement byte-identically (Priority: P1)

A `(text, family, size, weight)` measurement is computed once (a cold `Miss`) and reused on the identical key
(a `Hit`) with byte-identical metrics. Any single differing keyed field forces a `Miss`. The cache is a
bounded, deterministic LRU. The always-miss oracle yields a byte-identical scene **and** layout (cache-on ≡
cache-off), because the cached value equals the un-cached `Scene.measureText`.

**Why this priority**: The MVP — a correct, complete-key, bounded text cache that is invisible to output.

**Independent Test**: cold key Miss (un-cached measure); identical key Hit (byte-identical metrics) (FR-001);
perturbing exactly one keyed field misses (FR-002); empty/whitespace text caches without error; a fitted
caption's distinct candidate sizes are distinct keys; bounded under pressure with deterministic eviction and
no stale hit (FR-003/SC-005); the always-miss oracle yields byte-identical scene + layout + `RemeasuredNodeCount`
and `TextMeasureCacheHits = 0` (FR-004/SC-004).

**Acceptance Scenarios**:

1. **Given** a cold key, **When** measured, **Then** it is a Miss (un-cached value); **Given** the identical
   key, **Then** a Hit with byte-identical metrics (FR-001).
2. **Given** any single differing keyed field, **When** measured, **Then** a Miss with correct fresh metrics
   (FR-002); under eviction pressure `Entries.Count ≤ cap`, deterministic survivors, evicted re-misses
   correctly (FR-003/SC-005); the always-miss oracle renders byte-identical scene + layout (FR-004/SC-004).

---

### User Story 2 - Style-only frames do zero layout/text work (Priority: P2)

An idle frame invalidates and re-measures **zero** nodes; a style-only / visual-state-only frame over warm
text produces **zero** text-cache misses and `LayoutInvalidatedNodeCount = 0` / `RemeasuredNodeCount = 0`. A
genuine geometry frame reports bounded `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount`, both `> 0`.

**Why this priority**: P2 — the precision guard. The caches must make common paint-only interactions
(hover/focus/style) free of measure/text work.

**Independent Test**: idle → 0 invalidated, 0 re-measured (FR-006); style-only → 0 invalidated / 0 re-measured
(FR-006/FR-007/SC-003); style-only over warm text → 0 text misses (FR-007/SC-003); geometry frame →
`LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount`, both `> 0` (FR-006/SC-006); the feature-101 drift-guard
attribute set is unchanged (FR-008).

**Acceptance Scenarios**:

1. **Given** an idle or style-only frame, **When** stepped, **Then** `LayoutInvalidatedNodeCount = 0`,
   `RemeasuredNodeCount = 0`, and (warm text) zero text-cache misses (FR-006/FR-007/SC-003).
2. **Given** a geometry frame, **When** stepped, **Then** `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount`,
   both `> 0` and bounded by the node count (FR-006/SC-006).

---

### User Story 3 - Text-cache + layout-invalidated metrics are observable over a host script (Priority: P2)

Over `Perf.runScript`, a cold text-heavy frame reports text misses; the warm frame reports hits + zero misses;
a style-only frame reports zero misses / zero invalidated / zero re-measured; an idle frame reports all three
new fields `0`; a geometry frame reports `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount`. The three new
metrics re-run byte-identically.

**Why this priority**: P2 — the public observability surface for US1/US2.

**Independent Test**: assert the metric regimes (cold misses / warm hits; style-only zeros; idle zeros;
geometry bounded; deterministic re-run) (FR-005/FR-006/FR-010; SC-001/SC-002/SC-003/SC-006).

**Acceptance Scenarios**:

1. **Given** a cold then warm text-heavy frame, **When** scripted, **Then** misses then hits + zero misses
   (SC-001/SC-002).
2. **Given** style-only / idle / geometry frames, **When** scripted, **Then** zeros / zeros /
   `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount`; the three new metrics re-run byte-identically (FR-005/FR-006).

---

### Edge Cases

- **Empty / whitespace text**: caches without error and re-hits.
- **Fitted caption** (distinct candidate sizes): each size is a distinct key (cold sweep misses, warm sweep hits).
- **One differing keyed field** (text/family/size/weight): misses; the base key stays resident.
- **Over the cap**: `Entries.Count ≤ cap`; deterministic eviction; evicted re-misses correctly.
- **Style-only / visual-state-only / idle**: zero invalidated / re-measured / text misses (warm).
- **Always-miss oracle**: every request re-measures; scene + layout byte-identical.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A resident `(text, family, size, weight)` key MUST `Hit` (reused without re-invoking
  `Scene.measureText`); a cold/changed/evicted key MUST `Miss` (measured fresh and stored). A Hit's metrics
  MUST be byte-identical to the un-cached measure.
- **FR-002**: The key MUST be complete — any single differing keyed field MUST `Miss` (no stale hit).
- **FR-003**: The cache MUST be a bounded, deterministic LRU — `Entries.Count ≤ TextMeasureCacheCap` (= 256)
  at all times; eviction deterministic; an evicted key re-misses with a fresh correct measure.
- **FR-004**: The `TextCacheEnabled = false` always-miss oracle MUST yield a **byte-identical scene and
  layout** (cache-on ≡ cache-off; `TextMeasureCacheHits = 0`).
- **FR-005**: The per-frame text hit/miss counts MUST be observable as public `FrameMetrics` fields.
- **FR-006**: `LayoutInvalidatedNodeCount` MUST be the pre-pinning layout dirty-set size, with
  `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount`; `0` on idle / style-only / visual-state-only frames.
- **FR-007**: A style-only / visual-state-only frame over warm text MUST produce **zero** text-cache misses
  and **zero** invalidated / re-measured nodes.
- **FR-008**: The feature-101 drift-guard geometry-name set (`{ width; height; orientation }`) MUST be
  unchanged (no new geometry-driving attribute introduced by 117).
- **FR-010**: The three new metrics MUST be deterministic (re-run byte-identical).
- **FR-011**: The backfill MUST add **zero new public-surface-baseline delta** (the cache is internal; the
  three metric fields are additive on the already-baselined public `FrameMetrics`).

### Key Entities *(include if feature involves data)*

- **TextMeasureKey**: `{ Text; Family; Size; Weight }` — the complete correctness key (the available-space
  constraint is deliberately not keyed).
- **TextMeasureCache**: `{ Entries: Map<TextMeasureKey, int * TextMetrics>; Clock }` — the bounded cross-frame
  LRU (mirrors `PictureCache`).
- **TextMeasureCacheCap**: the fixed cap (256, aligned with `PictureCacheCap`).
- **TextCacheEnabled**: the always-miss / parity oracle switch (internal).
- **measureTextCached**: `cache -> enabled -> text -> font -> TextMetrics * cache * wasHit`.
- **TextMeasureCacheHits / Misses → FrameMetrics.TextMeasureCacheHitCount / MissCount**: per-frame counters.
- **LayoutInvalidatedNodeCount**: the pre-pinning dirty-set size metric (097's dirty-set; 117 surfaces it).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001 / SC-002**: A warm frame reports text hits; a cold frame reports misses, 100% of cases.
- **SC-003**: A style-only / visual-state-only frame does zero text/layout work, 100% of cases.
- **SC-004**: The always-miss oracle yields byte-identical scene + layout (cache-on ≡ cache-off), 100% of cases.
- **SC-005**: The cache is bounded with deterministic eviction and no stale hit after eviction, 100% of cases.
- **SC-006**: A geometry frame reports `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount`, both bounded, 100% of cases.

## Assumptions

- `Scene.measureText` (pure), the retained render `step`, feature 097's incremental layout (`layoutDirtySet` /
  `RemeasuredNodeCount`), and the public `FrameMetrics` already exist. 117 is the **backfilled contract** for
  the text-measure cache + the surfacing of `LayoutInvalidatedNodeCount`, not new-from-scratch construction.
- The cache is **internal**; the three metric fields are additive on the already-baselined public `FrameMetrics`
  ⇒ **zero new** public-surface delta.
- 117 imported with executable suites + an `Audit_TextCache` (Controls.Tests + Elmish.Tests, headless, no GL,
  no FsCheck) but **no `readiness/`** (tests do not self-write); authoring readiness is part of this backfill.
- `LayoutInvalidatedNodeCount` shares the `step` with feature 097 (which owns `layoutDirtySet` /
  `RemeasuredNodeCount`); 117 owns the text cache and the surfacing of this metric.
- This is the **C9** conformance backfill; `/speckit-*` reduce to a conformance pass.
</content>

# Feature Specification: Picture Cache (LRU) & Damage Set (Feature 116)

**Feature Branch**: `116-picture-cache-lru`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next item in the implementation plan"

## Context

This is a **conformance-backfill** specification — task **C8** in the 2026-06-15 missing-features plan
(Workstream C pattern: 091 / 092 / 093 / 095 / 096 / 099 / 097 / 103 / 110 / 113 / 114).

Feature 116 adds two related paint-side mechanisms:

- a **per-frame damage set** — `RepaintedNodeCount`, `DirtyRectCount` (distinct deduped boxes), and
  `DirtyArea` — so the work of a frame is proportional to what actually changed (an idle frame is `0/0/0`, a
  theme switch is frame-spanning);
- a **bounded cross-frame picture cache** — a fixed-cap (`PictureCacheCap = 256`) LRU keyed by a complete
  correctness key (box + structural fingerprint) per cacheable boundary (a `data-grid-row` identity). An
  unchanged, resident boundary is a `Hit` (reused, not repainted); a changed key / cold / evicted boundary is
  a `Miss`. `Entries.Count ≤ cap` at all times; eviction is deterministic and an evicted entry **re-misses**
  with fresh correct paint (never a stale hit). An always-miss oracle (`PictureCacheEnabled = false`) proves
  cache-on ≡ cache-off byte-identical.

A companion advisory `offscreenEffect` detector flags paint that would force an offscreen render pass
(drop-shadow / image-filter / path-clip / non-opaque over a multi-node group), while a plain opaque scene and
a `RectClip` are deliberately **not** flagged.

The implementation (`PictureCacheKey`/`PictureCache`/`PictureCacheCap`/`walkPictures`/`offscreenEffect` + the
`RetainedRender`/`WorkReductionRecord` fields in `RetainedRender.fs`/`.fsi`; the public `FrameMetrics`
damage + cache fields) and the suites (`Feature116DamageTests`, `Feature116PictureCacheTests`,
`Feature116CacheBoundTests`, `Feature116OffscreenDiagTests` in `Controls.Tests`; `Feature116MetricsTests` in
`Elmish.Tests`; `Audit_PictureCache` in `Controls.Tests`) **already exist** in the imported source. **No Spec
Kit spec/plan/tasks describe this work**, and 116 imported with **no `readiness/`**. This document backfills
the contract.

The cache surface is **assembly-internal**; the metrics surface via the already-baselined public
`FrameMetrics`, so the backfill adds **zero new public-surface-baseline delta** (type-granular). All proofs
are deterministic and headless (no live backend). Per the vertical-slice rule the in-assembly tests are the
user-reachable surface.

**Scope boundary.** 116 owns the damage set, the bounded picture cache (model), and the advisory
`offscreenEffect`. The **text-measure cache** (117) mirrors this discipline for text; the **replay cache**
(120) is the load-bearing *backend* realization (it reuses 116's boundary/LRU machinery, replaced 116's
truncating `sprintf "%A"` digest with the FNV `hashScene` fingerprint, and corrected `DirtyArea` to
`unionArea`). The memo seam (113) is the analogous data-grid-only control-internal cache; the layout cache is 097.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The damage set is proportional to the change (Priority: P1)

A frame reports exactly what it repainted: an idle (unchanged) frame is `0/0/0`; a localized content change is
a small region; a theme switch invalidates all paint (frame-spanning). `DirtyRectCount` counts **distinct
deduped** boxes; the integer damage counts are deterministic.

**Why this priority**: Damage proportionality is the foundation — the picture cache and every paint-cost story
rest on it. The MVP.

**Independent Test**: idle → `0/0/0` (FR-003); localized change → small region not frame-spanning (FR-001/FR-002);
theme switch → every node repainted, `DirtyArea > 120*24` (FR-002); damage proportional to the change (SC-001);
re-run byte-identical (FR-004).

**Acceptance Scenarios**:

1. **Given** an unchanged tree, **When** stepped, **Then** the damage is `0/0/0` (FR-003).
2. **Given** a localized content change vs a theme switch, **When** stepped, **Then** the damage is a small
   region vs frame-spanning (SC-001); `DirtyRectCount` is the distinct deduped box count and the integer
   counts re-run byte-identically (FR-004).

---

### User Story 2 - The picture cache reuses an unchanged boundary, byte-identically (Priority: P1)

A cacheable boundary unchanged across two frames is a `Hit` (reused, not repainted), and a `Hit` is
byte-identical to a fresh full rebuild. Perturbing content / box / theme forces a `Miss` on exactly the
affected boundaries; a paint-neutral change still hits. The always-miss oracle renders byte-identically
(cache-on ≡ cache-off).

**Why this priority**: Co-critical with US1. A cache that changes output or serves stale paint is a bug;
correctness-key precision + parity is what makes reuse safe.

**Independent Test**: unchanged boundary → Hit reused (FR-005/SC-002), byte-identical to a rebuild; content/box
change → miss on exactly that row (FR-006); a paint-affecting change misses, a paint-neutral one hits; theme
change → every row misses; always-miss oracle byte-identical (FR-007/SC-003).

**Acceptance Scenarios**:

1. **Given** an unchanged boundary across two frames, **When** stepped, **Then** it is a Hit, reused not
   repainted, byte-identical to a fresh rebuild (FR-005/SC-002).
2. **Given** a content/box/theme change, **When** stepped, **Then** exactly the affected boundaries miss
   (FR-006); the always-miss oracle renders byte-identically (FR-007/SC-003).

---

### User Story 3 - The picture cache is a bounded, deterministic LRU (Priority: P2)

Under the cap, nothing evicts and every row reuses; over the cap, `PictureCacheEntryCount ≤ cap` at all times.
Eviction is deterministic (same input ⇒ same survivors) and an evicted entry **re-misses** with fresh correct
paint — never a stale hit.

**Why this priority**: P2 — the resource guard. An unbounded cache would leak; a stale hit after eviction
would corrupt paint.

**Independent Test**: under the cap → no eviction, all reused (FR-009); over the cap → `EntryCount ≤ cap`
(FR-009/SC-004); deterministic survivors (FR-010); evicted re-miss correct, no stale hit (FR-010/SC-004).

**Acceptance Scenarios**:

1. **Given** a grid over the cap, **When** stepped, **Then** `PictureCacheEntryCount ≤ cap` at all times,
   eviction is deterministic (FR-009/FR-010/SC-004).
2. **Given** an evicted entry, **When** next needed, **Then** it re-misses with fresh correct paint, never a
   stale hit (FR-010/SC-004).

---

### User Story 4 - The offscreen-effect detector is advisory and precise (Priority: P2)

`offscreenEffect` flags paint that would force an offscreen render pass (drop-shadow / image-filter /
path-clip / non-opaque over a multi-node group) and surfaces an advisory diagnostic via `step`; a plain opaque
single-node scene and a `RectClip` are deliberately **not** flagged.

**Why this priority**: P2 — an advisory perf diagnostic; precision (not over-flagging `RectClip`) keeps it useful.

**Independent Test**: drop-shadow / image-filter / path-clip / non-opaque-over-group force offscreen; plain
opaque silent; `RectClip` not flagged; a forcing control surfaces an advisory diagnostic via `step`; a plain
control surfaces none and renders identically (FR-011/SC-005).

**Acceptance Scenarios**:

1. **Given** an offscreen-forcing effect, **When** detected, **Then** it is flagged and surfaces an advisory
   diagnostic via `step` (FR-011/SC-005).
2. **Given** a plain opaque scene or a `RectClip`, **When** detected, **Then** it is **not** flagged and
   renders identically.

---

### User Story 5 - Damage + cache metrics are observable over a host script (Priority: P2)

Over `Perf.runScript`, an idle frame reports damage `0/0/0` and hit/miss `0`; a stable grid's second frame
reuses every row picture with zero damage; a localized change reports small damage + a single picture miss
(rest hit); the entry count is bounded under pressure; the six metrics re-run byte-identically.

**Why this priority**: P2 — the public observability surface for US1–US3.

**Independent Test**: assert the metric regimes (idle 0; stable reuse; localized small damage + single miss;
bounded under pressure; deterministic re-run) (FR-012/FR-013; SC-006/SC-007).

**Acceptance Scenarios**:

1. **Given** idle / stable / localized frames, **When** scripted, **Then** the damage + cache metrics match
   the expected regimes (FR-012).
2. **Given** eviction pressure, **When** scripted, **Then** the entry count is bounded by the cap (SC-007) and
   the six metrics re-run byte-identically (SC-006).

---

### Edge Cases

- **Idle frame**: damage `0/0/0`, hit/miss `0`.
- **Paint-neutral change**: hits (the correctness key is the painted picture, not the raw input).
- **Over the cap**: `EntryCount ≤ cap`; deterministic eviction; evicted re-misses correctly.
- **`RectClip`**: not flagged offscreen; **drop-shadow/image-filter/path-clip/non-opaque-over-group**: flagged.
- **Always-miss oracle**: every boundary re-misses; scene byte-identical to cache-on.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001 / FR-002**: The per-frame damage set MUST be proportional to the change — localized for a localized
  change, frame-spanning for a theme switch.
- **FR-003**: An idle frame MUST report `0/0/0`.
- **FR-004**: `DirtyRectCount` MUST be the count of **distinct deduped** boxes; the integer damage counts MUST
  be deterministic (re-run byte-identical).
- **FR-005**: A cacheable boundary unchanged across frames MUST be a `Hit` (reused, not repainted), and a
  `Hit` MUST be byte-identical to a fresh full rebuild.
- **FR-006**: A changed correctness key (content / box / theme), a cold boundary, or an evicted boundary MUST
  be a `Miss`; the key is the painted picture (a paint-neutral change still hits).
- **FR-007**: The `PictureCacheEnabled = false` always-miss oracle MUST render **byte-identically** (cache-on
  ≡ cache-off).
- **FR-009**: The cache MUST be a bounded LRU — `PictureCacheEntryCount ≤ PictureCacheCap` (= 256) at all
  times.
- **FR-010**: Eviction MUST be deterministic (same input ⇒ same survivors); an evicted entry MUST re-miss
  with fresh correct paint, never a stale hit.
- **FR-011**: `offscreenEffect` MUST flag offscreen-forcing paint (drop-shadow / image-filter / path-clip /
  non-opaque over a multi-node group) and MUST NOT flag a plain opaque scene or a `RectClip`; advisory only.
- **FR-012 / FR-013**: The damage + cache outcomes MUST be observable as public `FrameMetrics` (deterministic,
  re-run byte-identical); the entry count MUST stay bounded under pressure.
- **FR-014**: The backfill MUST add **zero new public-surface-baseline delta** (the cache is internal; the
  metrics are additive on the already-baselined public `FrameMetrics`).

### Key Entities *(include if feature involves data)*

- **PictureCacheKey**: `{ Box; Fingerprint }` — the complete correctness key per cacheable boundary (any
  single changed input ⇒ Miss). *(The `Fingerprint` field is feature 120's FNV `hashScene`, replacing 116's
  superseded `sprintf "%A"` digest.)*
- **PictureCache**: `{ Entries: Map<RetainedId, int * PictureCacheKey>; Clock }` — the bounded cross-frame LRU
  (monotonic `Clock`, no wall-clock; `Entries.Count ≤ cap`).
- **PictureCacheCap**: the fixed cap (256).
- **PictureCacheEnabled**: the always-miss / parity oracle switch (internal).
- **walkPictures**: the per-frame traversal computing Hit/Miss + the damage set.
- **RepaintedNodeCount / DirtyRectCount / DirtyArea** and **PictureCacheHits / Misses / EntryCount**: the
  internal `WorkReductionRecord` counters → public `FrameMetrics` fields.
- **offscreenEffect**: the advisory offscreen-forcing-effect detector.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Damage is proportional to the change (localized small; theme-switch frame-spanning), 100% of cases.
- **SC-002 / SC-003**: A Hit reuses (byte-identical to a rebuild); cache-on ≡ cache-off byte-identical, 100% of cases.
- **SC-004**: The cache is bounded (`EntryCount ≤ cap`) with deterministic eviction and no stale hit after
  eviction, 100% of cases.
- **SC-005**: The offscreen detector flags forcing effects and stays silent on plain/`RectClip` scenes, 100% of cases.
- **SC-006 / SC-007**: The public damage + cache metrics re-run byte-identically and the entry count stays
  bounded under pressure, 100% of cases.

## Assumptions

- The retained render structure with stable `RetainedId`, the `data-grid-row` cacheable boundary, and the
  public `FrameMetrics` already exist. 116 is the **backfilled contract** for the damage set + bounded picture
  cache + offscreen detector, not new-from-scratch construction.
- The cache is **internal**; the metrics are additive on the already-baselined public `FrameMetrics` ⇒ **zero
  new** public-surface delta.
- 116 imported with executable suites + an `Audit_PictureCache` (Controls.Tests + Elmish.Tests, headless, no
  GL, no FsCheck) but **no `readiness/`** (tests do not self-write); authoring readiness is part of this backfill.
- The picture cache is modeled deterministically (no live backend); the **backend** realization (SKPicture
  record/replay) is feature 120's `PictureReplayCache`, which reuses 116's boundary/LRU machinery.
- This is the **C8** conformance backfill; `/speckit-*` reduce to a conformance pass.
</content>

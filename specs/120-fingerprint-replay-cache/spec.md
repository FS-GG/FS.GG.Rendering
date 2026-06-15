# Feature Specification: Structural Fingerprint & Backend Replay Cache (Feature 120)

**Feature Branch**: `120-fingerprint-replay-cache`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next item in the implementation plan"

## Context

This is a **conformance-backfill** specification — task **C10** in the 2026-06-15 missing-features plan
(Workstream C pattern: 091 / 092 / 093 / 095 / 096 / 099 / 097 / 103 / 110 / 113 / 114 / 116 / 117).

Feature 120 is the **backend realization** of the picture cache (116) plus the supporting machinery:

- a collision-resistant **structural fingerprint** `hashScene` — a 64-bit FNV-1a-style fold of every
  render-affecting input, with **no truncation** (it replaced 116's truncation-prone `sprintf "%A"` digest).
  Identical scenes hash identically; any single render-affecting change (geometry, colour, text, **opacity**,
  transform) flips the value;
- a **replay boundary** `CachedSubtree` / `CacheBoundary { CacheId; Fingerprint; Scene }` in the Scene IR —
  transparent to every consumer except the OpenGL backend painter (with replay disabled it recurses into the
  `Scene` identically — the parity oracle);
- the **backend replay cache** `PictureReplayCache` — a bounded LRU of recorded `SKPicture`s keyed by
  `CacheId`, validated by `Fingerprint`; a matching fingerprint replays a recorded picture (skipping the
  per-primitive draw-call walk), a changed/cold/evicted one re-records; cache-on is **pixel-identical** to the
  disabled oracle;
- replay metrics `ReplayHits`/`Misses`/`Records`/`SkippedNodes`/`CacheNativeBytes` (→ public `FrameMetrics`);
- the **damage union** `unionArea` (FR-015) — the integer area of the **union** of damage rects (overlaps
  counted once, clamped to the frame), correcting 116's sum-of-areas `DirtyArea`;
- present/compose **timing** diagnostics (US1, live-only, `TimeSpan.Zero` on the deterministic path) and the
  **idle-skip** present decision `GlHost.shouldPresent` (US2).

The implementation (`hashScene`/`unionArea` + the replay metric fields in `RetainedRender.fs`/`.fsi`; the
public Scene types `CacheBoundary`/`CachedSubtree`; `PictureReplayCache` in `SkiaViewer`; the public
`FrameMetrics` replay + timing fields) and the suites (`Feature120FingerprintTests`, `Audit_Fingerprint` in
`Controls.Tests`; `Feature120MetricsTests` in `Elmish.Tests`; `Feature120ReplayCacheTests`, `Audit_ReplayCache`
in `SkiaViewer.Tests`) **already exist** in the imported source. **No Spec Kit spec/plan/tasks describe this
work**, and 120 imported with **no `readiness/`**. This document backfills the contract.

The fingerprint/replay/metric surface is **assembly-internal** except the public Scene types
`CacheBoundary`/`CachedSubtree` — both **already in the committed baseline** — and the replay/timing fields
additive on the already-baselined public `FrameMetrics`. So the backfill adds **zero new public-surface-baseline
delta** (type-granular).

**Recorded finding (routed to Workstream E3, not fixed here).** `SceneEvidence.renderHash` (a coarse
capability-set hash, **distinct** from 120's `hashScene`) is **alpha-insensitive**: an opacity-only change
does not change its element-marker set, so the hash is unchanged. 120's own `hashScene` **is** fully
alpha-sensitive (proven by the suite). The `renderHash` cleanup is recorded and routed to **Workstream E3**;
it is **not** edited in this doc-only backfill.

**GL note.** Most 120 proofs are deterministic/headless. The backend pixel-parity proofs use a **raster**
`SKSurface` (no GL window): `Feature120ReplayCacheTests` runs on the raster backend, and `Audit_ReplayCache`
**degrades-and-discloses** (a `skiptest` with a tier reason) when an offscreen `SKSurface` is unavailable.

**Scope boundary.** 120 owns the fingerprint, the replay boundary + backend cache, the replay metrics, the
damage union, and the present/idle-skip diagnostics. The **controls-side picture cache model** is feature 116
(120 is its load-bearing backend realization and replaced its digest); the text cache is 117.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The structural fingerprint is collision-resistant and alpha-sensitive (Priority: P1)

`hashScene` folds every render-affecting input into a 64-bit value with no truncation: identical scenes hash
identically (deterministic across calls), and any single render-affecting change — geometry, colour, text,
**opacity/alpha**, transform — flips the value. A long-list difference that the truncating `%A` digest
collides on yields a **different** fingerprint.

**Why this priority**: The fingerprint is the replay cache's correctness key — a collision would serve a stale
picture. The MVP foundation.

**Independent Test**: identical scenes hash identically + deterministic (FR-008); each single change
(incl. alpha) flips the hash (FR-008/FR-010); the `%A`-collision long-list case yields a different fingerprint
(SC-005); ≥500 FsCheck cases — distinct rectangle widths never collide.

**Acceptance Scenarios**:

1. **Given** identical scenes, **When** hashed, **Then** equal + deterministic across calls (FR-008).
2. **Given** any single render-affecting change (incl. opacity), **When** hashed, **Then** the value flips
   (FR-008/FR-010); the `%A`-collision case differs (SC-005); ≥500 FsCheck distinct-width cases never collide.

---

### User Story 2 - The backend replay cache is byte/pixel-identical to the direct walk (Priority: P1)

A `CacheBoundary` whose fingerprint matches a recorded picture **replays** it (skipping the per-primitive
walk); a changed fingerprint re-records (never a stale hit); the disabled cache is the **always-direct parity
oracle**. The cache is a bounded LRU with native-byte accounting and `dispose` teardown. Cache-on is
**pixel-identical** to the disabled oracle.

**Why this priority**: Co-critical with US1. A replay cache that changes pixels or serves stale pictures is a
bug; pixel parity + re-record-on-change is what makes it safe.

**Independent Test**: matching fingerprint Hit (FR-007); changed fingerprint re-records, no stale hit,
`Entries = 1` (replacement) (FR-010/FR-013); LRU bound never exceeded under pressure (FR-013); disabled oracle
never records/replays (FR-011); dispose releases all pictures (FR-013); cache-on ≡ cache-off **pixel readback**
parity (SC-003/FR-009/FR-011). *(Raster-headless; `Audit_ReplayCache` degrades-and-discloses when raster is
unavailable.)*

**Acceptance Scenarios**:

1. **Given** a matching/changed fingerprint, **When** painted, **Then** Hit (replay) / re-record (no stale
   hit), `Entries` bounded (FR-007/010/013).
2. **Given** the disabled oracle vs the warmed replay, **When** read back, **Then** both are pixel-identical to
   the direct walk (SC-003/FR-009/FR-011); dispose releases all pictures (FR-013).

---

### User Story 3 - Replay metrics and skipped-node work reduction are observable (Priority: P2)

Over `Perf.runScript`, a stable grid frame's replay hits/misses **coincide** with the picture-cache hits/misses
(120 is its realization), records = misses, and skipped nodes / native bytes are `> 0`; an idle frame reports
zero replay work. Per-phase present/compose timing is **excluded** from the deterministic golden surface
(`TimeSpan.Zero`).

**Why this priority**: P2 — the observability surface, and the proof that replay is the picture cache's
backend (the counters coincide) and that work is actually skipped (SC-004).

**Independent Test**: stable grid → `ReplayHitCount == PictureCacheHitCount`, `ReplayRecordCount ==
ReplayMissCount`, hits/skipped/native-bytes `> 0` (FR-014/SC-004); idle → zero replay work; timing fields
`== TimeSpan.Zero` on the deterministic path (FR-002/SC-001).

**Acceptance Scenarios**:

1. **Given** a stable grid frame, **When** scripted, **Then** replay counters coincide with the picture-cache
   counters and skipped nodes `> 0` (FR-014/SC-004).
2. **Given** an idle frame / the deterministic path, **When** scripted, **Then** zero replay work / timing
   fields are `TimeSpan.Zero` (FR-002/SC-001).

---

### User Story 4 - Damage area is the union of rects, never the sum (Priority: P2)

`unionArea` reports the integer area of the **union** of a set of damage rectangles — overlapping rects counted
**once** (never the sum), clamped to the frame area; disjoint rects sum; an empty set is 0. This corrects
116's sum-of-areas `DirtyArea`.

**Why this priority**: P2 — the damage-area correctness fix. Overcounting overlap would misreport damage.

**Independent Test**: two overlapping 100×100 → 17500 (not 20000); disjoint → sum; clamps to the frame area;
empty → 0 (FR-015/SC-007).

**Acceptance Scenarios**:

1. **Given** overlapping damage rects, **When** unioned, **Then** the overlap is counted once (`< sum`),
   clamped to the frame (FR-015/SC-007); disjoint rects sum; empty → 0.

---

### Edge Cases

- **Opacity-only change**: flips `hashScene` (the fingerprint is alpha-sensitive). *(`SceneEvidence.renderHash`
  is the separate, alpha-insensitive hash — recorded finding, E3.)*
- **`%A`-colliding long list**: yields a different fingerprint (no false hit).
- **Changed fingerprint**: re-records, `Entries` does not accumulate (replacement); never a stale hit.
- **Over the cap**: `Entries ≤ cap`; bounded LRU.
- **Disabled cache**: never records/replays — the parity oracle.
- **Overlapping / disjoint / empty damage rects**: union once / sum / 0.
- **Raster unavailable**: `Audit_ReplayCache` skips with a tier reason (degrade-and-disclose).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-007**: `CacheBoundary`/`CachedSubtree` MUST be a replay boundary transparent to every Scene consumer
  except the GL/raster backend painter (with replay disabled it recurses into the `Scene` identically).
- **FR-008**: `hashScene` MUST be a collision-resistant FNV-1a-style 64-bit fingerprint (no truncation):
  identical scenes hash identically (deterministic); the replay key.
- **FR-009**: A replayed boundary MUST be **byte/pixel-identical** to the direct walk.
- **FR-010**: Any render-affecting change (incl. opacity/alpha) MUST flip the fingerprint; a changed
  fingerprint MUST re-record (no stale hit).
- **FR-011**: The disabled cache MUST be the always-direct **parity oracle** (no record/replay).
- **FR-012**: Replay boundaries MUST be prior-frame-stable cacheable subtrees.
- **FR-013**: The replay cache MUST be a bounded LRU with native-byte accounting and `dispose` teardown
  (`Entries ≤ cap`; a changed fingerprint replaces, not accumulates).
- **FR-014**: Replay metrics (`ReplayHits`/`Misses`/`Records`/`SkippedNodes`/`CacheNativeBytes`) MUST be
  observable as public `FrameMetrics`, coinciding with the picture-cache counters on a stable frame.
- **FR-015**: `unionArea` MUST be the integer area of the **union** of damage rects (overlaps once, clamped to
  the frame; disjoint sums; empty 0).
- **FR-001 / FR-002**: Present/compose timing diagnostics MUST be live-only and **excluded** from the
  deterministic golden surface (`TimeSpan.Zero` on the deterministic path).
- **FR-004 / FR-005 / FR-006**: `GlHost.shouldPresent` MUST present iff first frame / scene changed / size
  changed (idle-skip).
- **FR-016**: The backfill MUST add **zero new public-surface-baseline delta** (the fingerprint/replay/metric
  surface is internal except `CacheBoundary`/`CachedSubtree`, both already baselined; timing/replay metrics
  additive on the already-baselined public `FrameMetrics`).

### Key Entities *(include if feature involves data)*

- **hashScene**: `Scene list -> uint64` — the collision-resistant FNV-1a structural fingerprint (the replay key).
- **CachedSubtree / CacheBoundary**: `{ CacheId: uint64; Fingerprint: uint64; Scene: Scene }` — the public
  Scene-IR replay boundary (transparent except to the backend painter).
- **PictureReplayCache**: the internal bounded LRU of recorded `SKPicture`s keyed by `CacheId`, validated by
  `Fingerprint`; `cap` mirrors `PictureCacheCap` (256).
- **ReplayHits / Misses / Records / SkippedNodes / CacheNativeBytes → FrameMetrics.Replay*Count**: replay metrics.
- **unionArea**: `Rect list -> frameArea:int -> int` — the union-area of damage rects (overlap once, clamped).
- **GlHost.shouldPresent**: the idle-skip present decision (US2).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Per-phase timing is excluded from the deterministic golden surface (`TimeSpan.Zero`), 100% of cases.
- **SC-003**: Cache-on is **pixel-identical** to the disabled oracle (PNG readback), 100% of cases.
- **SC-004**: Replay hits coincide with picture-cache hits and skip node work (`SkippedNodes > 0`) on a stable
  frame, 100% of cases.
- **SC-005**: A `%A`-colliding long-list difference yields a **different** fingerprint (≥500 FsCheck cases:
  distinct widths never collide), 100% of cases.
- **SC-007**: `unionArea` never exceeds the frame area and counts overlap once, 100% of cases.

## Assumptions

- The Scene IR, the controls-side picture cache (116), the SkiaSharp raster/GL backend, and the public
  `FrameMetrics` already exist. 120 is the **backfilled contract** for the fingerprint + backend replay cache +
  damage union + present/idle-skip diagnostics, not new-from-scratch construction.
- The surface is **internal** except `CacheBoundary`/`CachedSubtree` (public Scene types, **already baselined**)
  and the replay/timing fields additive on the already-baselined public `FrameMetrics` ⇒ **zero new** public-surface delta.
- 120 imported with executable suites + audits (Controls.Tests + Elmish.Tests headless; SkiaViewer.Tests raster
  — `Audit_ReplayCache` degrades-and-discloses when raster is unavailable) but **no `readiness/`** (tests do not
  self-write); authoring readiness is part of this backfill.
- `SceneEvidence.renderHash` (distinct from `hashScene`) is **alpha-insensitive** — a **recorded finding routed
  to Workstream E3**, not fixed in this doc-only backfill. 120's `hashScene` is alpha-sensitive.
- This is the **C10** conformance backfill; `/speckit-*` reduce to a conformance pass.
</content>

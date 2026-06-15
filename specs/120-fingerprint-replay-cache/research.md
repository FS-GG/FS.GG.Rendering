# Phase 0 — Research: Structural Fingerprint & Backend Replay Cache (Feature 120)

Conformance backfill — recovers the design the imported code embodies. No open `NEEDS CLARIFICATION`.
Reconstructed from `Scene.fsi` (`CacheBoundary`/`CachedSubtree`), `RetainedRender.fsi`/`.fs`
(`hashScene`/`unionArea`), `SkiaViewer/PictureReplayCache`, `OpenGl.fsi` (`shouldPresent`), and the five suites.

## Decision 1 — A collision-resistant FNV-1a fingerprint, no truncation

- **Decision**: `hashScene` folds every render-affecting input into a 64-bit value via a deterministic FNV-1a
  mix — no truncation. It replaced 116's `sprintf "%A"` digest, which truncated long lists and could collide.
- **Rationale**: The fingerprint is the replay cache's correctness key; a collision serves a stale picture.
  FNV-1a over the full structure makes identical-scene ⇒ identical-hash and any change ⇒ different-hash
  (FR-008/FR-010), proven over ≥500 FsCheck cases and a `%A`-collision counterexample (SC-005).
- **Alternatives considered**: Keeping the `%A` digest — rejected (truncation collisions); a cryptographic
  hash — rejected (needless cost for a non-adversarial key).

## Decision 2 — A transparent Scene-IR replay boundary

- **Decision**: `CachedSubtree of CacheBoundary { CacheId; Fingerprint; Scene }` marks a reuse-stable subtree.
  It is transparent to every Scene consumer except the GL/raster backend painter; with replay disabled the
  painter recurses into `Scene` identically (the parity oracle).
- **Rationale**: The replay boundary must not change the IR's meaning for any non-backend consumer; only the
  painter consults the cache. Transparency makes disabled ≡ direct walk by construction (FR-007/FR-011).
- **Alternatives considered**: A backend-only side table — rejected: the boundary needs to ride the Scene so
  the painter can find it; a transparent IR node is the cleanest carrier.

## Decision 3 — A bounded backend LRU keyed by CacheId, validated by Fingerprint

- **Decision**: `PictureReplayCache` records `SKPicture`s keyed by `CacheId`, validated by `Fingerprint`: a
  matching fingerprint replays (skipping the per-primitive walk), a changed/cold/evicted one re-records.
  Bounded LRU (`cap` mirrors `PictureCacheCap = 256`) with native-byte accounting and `dispose` teardown.
- **Rationale**: Replaying a recorded picture skips the draw-call walk (the actual GPU win, SC-004); the
  fingerprint validation prevents stale hits; bounded + dispose bounds native memory (FR-013).
- **Alternatives considered**: An unbounded picture cache — rejected (native-memory leak); keying on Scene
  structure each frame — rejected (the fingerprint is the cheap stable key).

## Decision 4 — The disabled cache is the pixel-parity oracle

- **Decision**: With replay disabled the backend walks `Scene` directly; cache-on is **pixel-identical** to
  this oracle (PNG readback comparison). Proven on a **raster** `SKSurface` (no GL window).
- **Rationale**: A replay cache must be invisible to pixels; the disabled walk is the parity counterfactual.
  Using raster keeps the proof headless and deterministic (SC-003/FR-009/FR-011).
- **Alternatives considered**: Comparing recorded vs direct at the Scene level only — rejected: the proof must
  reach actual pixels; raster readback does that without a GL window.

## Decision 5 — Damage area is the union, not the sum

- **Decision**: `unionArea boxes frameArea` is the integer area of the **union** of damage rects (overlap
  counted once, clamped to the frame; disjoint sums; empty 0). It corrects 116's sum-of-areas `DirtyArea`.
- **Rationale**: Summing overlapping rects overcounts damage; the union is the true damaged area, never
  exceeding the frame (FR-015/SC-007). Coordinate-compression keeps it pure/deterministic over integer geometry.
- **Alternatives considered**: Keeping the sum — rejected (overcounts overlap, can exceed the frame area).

## Decision 6 — Present/compose timing is live-only; idle-skip is a pure decision

- **Decision**: `PaintDuration`/`ComposeDuration` are live-only diagnostics, `TimeSpan.Zero` on the
  deterministic path (excluded from the golden surface). `GlHost.shouldPresent` decides present iff first
  frame / scene changed / size changed (idle-skip).
- **Rationale**: Wall-clock timing must never enter the deterministic golden surface (Principle VI); a pure
  present decision keeps idle-skip testable without a live window (FR-001/002/004/005/006, SC-001).
- **Alternatives considered**: Golden-comparing timing — rejected (non-deterministic); reading the clock inside
  the present decision — rejected (untestable).

## Decision 7 — `renderHash` alpha-insensitivity is a separate, deferred finding (E3)

- **Decision**: `SceneEvidence.renderHash` (a coarse capability-set hash, **distinct** from `hashScene`) is
  alpha-insensitive — an opacity-only change does not change its element-marker set. This is **recorded** and
  routed to **Workstream E3**; it is **not** fixed here. 120's `hashScene` **is** alpha-sensitive (proven).
- **Rationale**: `renderHash` is a different hash with a different purpose (capability disclosure, not replay);
  changing it is out of 120's scope and a behavior change. Keeping it as a recorded finding keeps this
  doc-only backfill uniform.
- **Alternatives considered**: Fixing `renderHash` here — rejected: out of scope and would mix a source change
  into a documentation pass.

## Renderer-mode / evidence honesty

The fingerprint/union/metrics proofs are deterministic-headless; the replay pixel-parity proofs use a raster
`SKSurface` (no GL window). `Audit_ReplayCache` degrades-and-discloses (a `skiptest` with a tier reason) when
an offscreen `SKSurface` is unavailable — never a fake pass. Readiness (authored in `/speckit-implement`,
since 120 imported without it) discloses these scopes.
</content>

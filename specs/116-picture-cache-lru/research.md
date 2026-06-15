# Phase 0 — Research: Picture Cache (LRU) & Damage Set (Feature 116)

Conformance backfill — recovers the design the imported code embodies. No open `NEEDS CLARIFICATION`.
Reconstructed from `RetainedRender.fsi`/`.fs` (`walkPictures`/`PictureCache`/`offscreenEffect`),
`ControlsElmish`, and the six suites.

## Decision 1 — A per-frame damage set proportional to the change

- **Decision**: Each `step` accumulates the repainted boxes into `RepaintedNodeCount` / `DirtyRectCount`
  (distinct deduped) / `DirtyArea`; an idle frame is `0/0/0`, a theme switch is frame-spanning. Integer
  geometry ⇒ deterministic.
- **Rationale**: Paint cost should be visible and proportional; the damage set is the substrate every cache
  story rests on.
- **Alternatives considered**: Reporting only a boolean "changed" — rejected: too coarse to reason about
  localized vs frame-spanning damage.

## Decision 2 — A complete correctness key per cacheable boundary

- **Decision**: `PictureCacheKey = { Box; Fingerprint }` is the boundary's full correctness key; equality on
  it proves a Hit is byte-identical to a fresh paint, and any single changed input forces a Miss. The key is
  the *painted picture* (a paint-neutral change still hits).
- **Rationale**: A cache is only safe if its key captures everything that affects the rendered picture; a
  complete structural key makes Hit ≡ fresh-paint by construction (FR-005/FR-006).
- **Alternatives considered**: Keying on raw inputs — rejected: would miss on paint-neutral changes and risk
  hitting on paint-affecting ones; the painted-picture digest is the correct key. *(120 replaced the original
  truncating `%A` digest with the FNV `hashScene` to avoid collisions.)*

## Decision 3 — A bounded, deterministic LRU; evicted entries re-miss correctly

- **Decision**: `PictureCache` is a fixed-cap (256) LRU with a monotonic `Clock` advanced by traversal order
  (no wall-clock); on overflow the least-recently-accessed entry is dropped; `Entries.Count ≤ cap` always;
  an evicted entry re-misses (never a stale hit) and eviction is deterministic.
- **Rationale**: Bounded memory is mandatory for a long-running host; determinism makes the eviction testable;
  re-miss-after-evict is the correctness guard (FR-009/FR-010).
- **Alternatives considered**: An unbounded cache — rejected (leak); a wall-clock LRU — rejected
  (non-deterministic).

## Decision 4 — An always-miss oracle proves cache-on ≡ cache-off

- **Decision**: `PictureCacheEnabled = false` forces every boundary down the miss path (`PictureCacheHits =
  0`), proving the rendered scene is byte-identical with the cache disabled.
- **Rationale**: A cache must be invisible to output; the oracle is the parity counterfactual the tests
  compare against (FR-007/SC-003).
- **Alternatives considered**: Removing the cache to test parity — rejected: the oracle keeps parity a checked
  in-tree invariant.

## Decision 5 — An advisory, precise offscreen-effect detector

- **Decision**: `offscreenEffect` flags paint that forces an offscreen pass (drop-shadow / image-filter /
  path-clip / non-opaque over a multi-node group); a plain opaque scene and a `RectClip` are deliberately not
  flagged. Advisory only (surfaced via `step` as a diagnostic).
- **Rationale**: Offscreen passes are a real perf cost worth surfacing; precision (not flagging cheap
  `RectClip`) keeps the advisory useful rather than noisy (FR-011/SC-005).
- **Alternatives considered**: Flagging any clip — rejected: `RectClip` is cheap and would create noise.

## Renderer-mode / evidence honesty

The picture cache is modeled **deterministically** (no live backend) — all proofs are Hit/Miss outcomes,
scene byte-equality, integer damage counts, and bounded-LRU invariants. The **backend** SKPicture
record/replay realization is feature 120 (`PictureReplayCache`). Readiness (authored in `/speckit-implement`,
since 116 imported without it) makes no pixel/desktop claim.
</content>

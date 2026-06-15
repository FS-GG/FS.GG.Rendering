# Contract — Picture Cache (LRU) & Damage Set (Feature 116)

The **internal** cache seam (pinned via `InternalsVisibleTo`) + the additive public `FrameMetrics`. Signatures
from `RetainedRender.fsi`; behaviour clauses are what the six suites (incl. `Audit_PictureCache`) assert.

## C1 — Damage set → `RepaintedNodeCount` / `DirtyRectCount` / `DirtyArea` (internal → public)

- Idle frame `0/0/0`; localized small; theme switch frame-spanning. `DirtyRectCount` = distinct deduped boxes;
  integer counts deterministic (re-run byte-identical).

*Pins*: FR-001, FR-002, FR-003, FR-004. *Used by*: US1, US5.

## C2 — `PictureCacheKey` + `PictureCache` + `PictureCacheCap` (internal)

```fsharp
type internal PictureCacheKey = { Box: FS.GG.UI.Scene.Rect option; Fingerprint: uint64 }
type internal PictureCache    = { Entries: Map<RetainedId, int * PictureCacheKey>; Clock: int }
val internal PictureCacheCap: int   // = 256
```

- Unchanged key + resident ⇒ Hit (reuse, byte-identical to a rebuild); changed/cold/evicted ⇒ Miss.
- `Entries.Count ≤ PictureCacheCap` always; deterministic LRU eviction; evicted ⇒ re-miss (no stale hit).

*Pins*: FR-005, FR-006, FR-009, FR-010. *Used by*: US2, US3.

## C3 — `PictureCacheEnabled` (always-miss / parity oracle, internal)

- `false` ⇒ every boundary re-misses (`PictureCacheHits = 0`); the rendered scene is byte-identical to
  cache-on (cache-on ≡ cache-off).

*Pins*: FR-007. *Used by*: US2.

## C4 — `offscreenEffect` (advisory detector, internal)

```fsharp
val internal offscreenEffect: ownScene: FS.GG.UI.Scene.Scene list -> string option
```

- `Some` for drop-shadow / image-filter / path-clip / non-opaque-over-multi-node-group; `None` for a plain
  opaque scene and a `RectClip`. Advisory (surfaced via `step`); render output unchanged.

*Pins*: FR-011. *Used by*: US4.

## C5 — Public cache metrics (`FrameMetrics`, additive)

```fsharp
PictureCacheHitCount: int
PictureCacheMissCount: int
PictureCacheEntryCount: int   // <= cap
```

- Idle 0; stable reuse; localized single miss; bounded under pressure; deterministic re-run.

*Pins*: FR-012, FR-013. *Used by*: US5.

## Surface-drift

- **Zero new public-surface-baseline delta** (FR-014): `PictureCacheKey`/`PictureCache`/`PictureCacheCap`/
  `PictureCacheEnabled`/`walkPictures`/`offscreenEffect` + the `WorkReductionRecord` counters are `internal`;
  the damage + cache metrics are additive on the already-baselined public `FrameMetrics`.
  `FS.GG.UI.Controls.txt` / `FS.GG.UI.Controls.Elmish.txt` stay byte-unchanged.
</content>

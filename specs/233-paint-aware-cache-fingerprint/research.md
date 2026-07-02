# Research: paint-aware cache-boundary fingerprint

## R1 — Where the fingerprint is produced and consumed

- **Produced**: `Elements.cached key scene` → `mixScene offsetBasis scene` builds `CacheBoundary.Fingerprint`
  (`src/Canvas/Elements.fs`).
- **Consumed**: `PictureReplayCache.paintBoundary` (`src/SkiaViewer/PictureReplayCache.fs:152-158`)
  — on `entry.Fingerprint = boundary.Fingerprint` under the same `CacheId`, it `DrawPicture`s the
  resident `SKPicture` (skips the per-primitive walk). Fingerprint equality is the only reuse gate.
- **Parity oracle**: a *disabled* cache recurses straight into `boundary.Scene` (no replay), so
  correctness with the cache on must match the cache off (FR-011). The stale-frame bug therefore only
  manifests with the replay cache enabled.

## R2 — What the old fold missed

`mixNode` before this feature:

- Ignored `Paint` on `PaintedRectangle`, `Points`, `Line`, `Path`.
- Recursed into `ClipNode`/`PerspectiveNode`/`ColorSpaceNode` children but never hashed the clip /
  transform / colour space itself.
- Collapsed `FilledEllipse`, `Ellipse`, `Arc`, `Vertices`, `TextRun`, `GlyphRun`, `RegionNode`,
  `PictureNode` to a single constant via `| _ -> step h 18UL`.
- Dropped `Path.FillType` entirely and `ArcTo` `bounds.Height`.

Net effect: many render-affecting mutations produced an equal fingerprint → stale replay.

## R3 — Can Canvas reuse `SceneHash.hashScene`?

No. Project references: `Canvas → Scene`; `Controls → Scene` (+ Layout/KeyboardInput/Diagnostics/
DesignSystem). There is **no** `Canvas → Controls` edge, and `SceneHash` is `module internal` in the
Controls assembly. Options considered:

1. **Mirror the coverage in `Elements.fs`** (chosen) — smallest, lowest-risk fix; keeps Canvas's own
   byte convention; no surface change.
2. **Promote a shared fold into the `Scene` layer** and route both Canvas and Controls through it —
   the ideal end state (single source of truth kills the drift bug class), but it widens `Scene`'s
   public surface and risks Controls' documented byte-identical `hashScene` guarantee (Feature 178/189).
   Deferred as a follow-up candidate.

Both folds share the same FNV-1a constants (`Controls/Internal/Hashing.fs` ≡ Canvas's private
`offsetBasis`/`prime`/`step`), so a future merge is mechanically tractable.

## R4 — Is changing the fingerprint values safe?

Yes. Grep of `tests/` shows fingerprint assertions test only *inequality* and *determinism*
(`ElementsTests.fs` "changed content ⇒ changed fingerprint"), never a literal numeric value. The
boundary fingerprint is an ephemeral in-process cache key, not a persisted/golden artifact. Adding
fields and renumbering tags is therefore non-breaking; equality semantics only get stricter (fewer
false hits, never more).

## R5 — DU name collisions

`BlendMode` defines `Difference`; `PathOperation` also defines `Difference` (and `Union`), and both are
in `FS.GG.UI.Scene`. Bare `Difference` resolves to the last-declared type (`PathOperation`), so
`mixBlendMode` must qualify `BlendMode.*` (as `SceneHash` does). `RegionOperation` uses distinct
`Region*`-prefixed names — no collision.

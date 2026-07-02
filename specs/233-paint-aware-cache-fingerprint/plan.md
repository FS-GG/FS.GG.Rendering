# Implementation Plan: paint-aware cache-boundary fingerprint

**Branch**: `233-paint-aware-cache-fingerprint` · **Spec**: [spec.md](./spec.md) · **Issue**: FS-GG/FS.GG.Rendering#45

## Summary

Replace the paint-blind FNV-1a fold behind `Elements.cached`'s boundary fingerprint
(`src/Canvas/Elements.fs`) with a comprehensive, exhaustive fold that covers every render-affecting
field of every `SceneNode` — mirroring the field coverage of `SceneHash.hashScene` in the Controls
layer, using the same FNV-1a primitive and Canvas's existing byte convention. Drop the wildcard match
arm so the node match is exhaustive (future-proofing the bug class away). No public surface changes.

## Technical context

- **Language / stack**: F# 8 / .NET 10, `FS.GG.UI.Canvas` (`src/Canvas`), depends only on
  `FS.GG.UI.Scene`.
- **Load-bearing consumer**: `PictureReplayCache.paintBoundary` (`src/SkiaViewer`) gates replay on
  `entry.Fingerprint = boundary.Fingerprint`.
- **Reference fold**: `SceneHash.hashScene` (`src/Controls/Internal/SceneHash.fs`) already mixes every
  field. Canvas cannot reference Controls (no project edge), so coverage is mirrored, not reused.
- **Shared primitive**: FNV-1a offset basis `0xcbf29ce484222325`, prime `0x100000001b3`, step
  `(h ^^^ x) * prime` — identical constants to `Controls/Internal/Hashing.fs`; Canvas keeps its private
  `step`/`mixFloat`/`mixString` (UTF-8 byte convention, process-stable).

## Constitution / governance check

- **Purity & determinism (FR-008/FR-011)**: fold stays pure, allocation-conscious (`mutable` accumulator
  only in the existing `mixScene` hot loop), and process-stable (no `String.GetHashCode`). ✔
- **Public surface**: `Elements.cached` signature and `Canvas/Elements.fsi` unchanged; every new mixer is
  `private`. No `.fsi` delta (see `contracts/fsi-surface-deltas.md`). ✔
- **No cross-repo contract touched**: `fs-gg-ui-template` / registry untouched; ships in the next
  batched `fs-gg-ui` coherent-set release, no version bump in this feature merge. ✔

## Approach

1. **Add typed mixers** (private, in `Elements` module) mirroring `SceneHash` coverage: `mixBool`,
   `mixInt`, `mixRect`, generic `mixOption`/`mixList` (tagging `None`/`[]` and list length),
   `mixStringOption`, `mixStroke*`, `mixBlendMode` (qualified `BlendMode.*` — the bare names collide with
   `PathOperation`/`RegionOperation`), `mixShader`, `mixColorFilter`, `mixMaskFilter`, `mixImageFilter`,
   `mixPathEffect`, `mixPaint`, `mixPathFillType`, `mixPathCommand`, `mixPathSpec`, `mixClip`,
   `mixRegion*`, `mixColorSpace`, `mixPerspective`, `mixFont`, `mixTextRun`, `mixVertex*`, `mixGlyphRun`.
2. **Rewrite `mixNode`** to hash the full field set of all 26 `SceneNode` cases; **remove the wildcard**
   so the match is exhaustive. Fix the latent `Path.FillType` and `ArcTo` `bounds.Height` drops.
3. **Regression tests** in `tests/Canvas.Tests/ElementsTests.fs`: paint-only deltas (stroke colour, line
   paint, path dash, path fill-type, arc sweep, glyph-run text) each flip the `cached` fingerprint —
   fail-before/pass-after.
4. **Verify** Canvas + SkiaViewer (replay parity oracle) suites; then the full suite for regressions.

## Files

| File | Change |
|---|---|
| `src/Canvas/Elements.fs` | Comprehensive + exhaustive fingerprint fold (core fix). |
| `tests/Canvas.Tests/ElementsTests.fs` | +1 regression test covering the previously-blind fields. |
| `specs/233-paint-aware-cache-fingerprint/*` | Spec Kit artifacts. |

## Risks & mitigations

- **Fingerprint values change** (new tags / added fields) → **safe**: values are ephemeral in-process
  cache keys; no golden/snapshot asserts a literal fingerprint (verified). Only *equality semantics*
  matter, and they strictly improve (fewer false hits).
- **DU name collisions** (`Difference` in both `BlendMode` and `PathOperation`) → qualify `BlendMode.*`.
- **Drift with `SceneHash`** (two folds could diverge again) → recorded as a follow-up candidate to
  promote a single shared fold into the `Scene` layer; not taken here to avoid widening surface and
  disturbing Controls' byte-identical guarantee.

## Out of scope

- Unifying the Canvas and Controls folds into one shared hasher.
- Any change to `PictureReplayCache`, `RetainedRender`, or `SceneHash`.

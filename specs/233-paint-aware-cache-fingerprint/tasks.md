# Tasks: paint-aware cache-boundary fingerprint

**Feature**: `233-paint-aware-cache-fingerprint` · **Issue**: FS-GG/FS.GG.Rendering#45

Ordered, dependency-aware. `[X]` = done.

## Phase 1 — Investigation

- [X] T001 Confirm the fingerprint producer (`Elements.cached` → `mixScene`) and consumer
  (`PictureReplayCache.paintBoundary` reuse gate). → `research.md` R1.
- [X] T002 Enumerate every field the old fold ignored/collapsed. → `research.md` R2.
- [X] T003 Confirm Canvas cannot reference Controls' `SceneHash`; choose mirror-coverage. → `research.md` R3.
- [X] T004 Confirm no golden/snapshot asserts a literal fingerprint value (change-safety). → `research.md` R4.

## Phase 2 — Implementation (US1, P1)

- [X] T005 Add private typed mixers to `src/Canvas/Elements.fs` mirroring `SceneHash` coverage
  (`mixBool`/`mixInt`/`mixRect`/`mixOption`/`mixList`/`mixStringOption`, stroke, blend mode, shader,
  colour/mask/image filters, path effect, `mixPaint`, path spec, clip, region, colour space,
  perspective, font, text run, vertex, glyph run).
- [X] T006 Rewrite `mixNode` to hash the full field set of all 26 `SceneNode` cases; **remove the
  wildcard arm** (exhaustive match — FR-005). Fix latent `Path.FillType` and `ArcTo` `bounds.Height`
  drops (FR-004).
- [X] T007 Qualify `BlendMode.*` in `mixBlendMode` (name collision with `PathOperation.Difference`).
- [X] T008 Build `src/Canvas/Canvas.Lib.fsproj` clean (0 warnings/errors).

## Phase 3 — Tests & verification

- [X] T009 Add a regression test to `tests/Canvas.Tests/ElementsTests.fs`: paint-only deltas (stroke
  colour, line paint, path dash, path fill-type, arc sweep, glyph-run text) each flip the `cached`
  fingerprint (fail-before/pass-after; SC-001).
- [X] T010 Run `tests/Canvas.Tests` — all green (18/0).
- [X] T011 Run `tests/SkiaViewer.Tests` (replay-cache parity oracle) — all green (213/0; SC-002).
- [X] T012 Run the full solution test suite — 0 new regressions (SC-003).

## Phase 4 — Artifacts & delivery

- [X] T013 Write spec / plan / research / fsi-surface-deltas / tasks.
- [X] T014 Squash-merge to `main`, update the Coordination board item to Done, push. Closes #45.

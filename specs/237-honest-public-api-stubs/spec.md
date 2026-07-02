# Feature Specification: kill success-shaped stubs on the public API

**Feature Branch**: `237-honest-public-api-stubs`

**Created**: 2026-07-02

**Status**: Draft

**Input**: Finding P6 / R2, R4, R5, R10 of the [2026-07-02 repo review](../../docs/reports/2026-07-02-14-07-repo-code-quality-and-architecture-review.md). Resolves **FS-GG/FS.GG.Rendering#49**.

## Context (non-normative)

The repository enforces a strong "honest failure, never success-shaped stubs" principle almost
everywhere (`Evidence.renderPng` is the model idiom: when real output genuinely cannot be produced,
fail loud with a typed diagnostic rather than return a plausible-but-wrong value). Six public
surfaces violated it — each returns a success-shaped result that silently ignores its inputs:

1. **`Path.combine`** (`src/Scene/Scene.fs`) produced the *same* command concatenation for
   `Union`/`Intersect`/`Difference`/`Xor` — the boolean operation was ignored.
2. **`Path.segment`** was a no-op: it returned the whole path unchanged (or empty when the window was
   inverted), never extracting a sub-path.
3. **`Viewer.runBounded`** (`src/SkiaViewer/SkiaViewer.fs`) did `ignore scene` — the window counted
   frames but never drew the scene, so `runUntilFirstFrame`/`runForFrames` reported frame evidence
   for a window that presented nothing of the scene.
4. **`Animation.Color`** was public but dead: `applyAt` composed only opacity + transform; the colour
   tween affected only `isSettled`.
5. **`SceneEvidence.render` with `Format = Png`** wrote the deterministic capability *hash string* to
   a `.png` path — the exact success-shaped non-image feature 221 eliminated on `renderPng`.
6. **`horizontalStack`/`verticalStack`/`dock`** (`src/Layout/Layout.fs`) ignored their `StackConfig`/
   `DockConfig` and `DockPosition`, lowering every child at the origin.

## Requirements (normative)

The fix is per-site: **implement** where the dependency-light layer can honestly produce the result,
otherwise **fail loud with a typed diagnostic** and **disclose** the remaining limitation in the
`.fsi` docs (never leave a success-shaped stub).

- **FR-001 — `Path.combine` honesty.** `Union`/`Xor` ARE expressible by subpath concatenation under
  a fill rule (nonzero-winding resp. even-odd) and return `Ok`. `Intersect`/`Difference` require a
  boolean clipping kernel the Skia-free `Scene` layer does not have, so they fail loud with a typed
  `PathCombineError`. Signature: `Result<PathSpec, PathCombineError>`.
- **FR-002 — `Path.segment` honesty.** Extract the sub-path between two arc-length distances along the
  polyline of the path's vertex-bearing commands (the same points `measure` accumulates length over).
  Disclose the polyline approximation in the `.fsi`; it is no longer a no-op.
- **FR-003 — `Viewer.runBounded` honesty.** Stop `ignore scene`. When the request's `EvidencePath`
  names a `.png`, rasterize the scene to real pixels through the shared CPU painter so image evidence
  depicts the scene. Disclose in the `.fsi` that `FramesRendered` is window/frame-cadence proof, not
  on-screen scene presentation (`run`/`runApp` present on the live GL surface).
- **FR-004 — `Animation.Color` honesty.** Expose `sampleColor : elapsed -> animation -> Color option`
  so the colour tween is genuinely consumable; disclose that `applyAt` composes opacity + transform
  only (the frozen wire format has no scene-wide tint node).
- **FR-005 — `SceneEvidence.render` Png honesty.** `Format = Png` fails loud (a hash string is not a
  PNG); route callers to the byte-returning `renderPng`. No non-image is written to a `.png` path.
- **FR-006 — stack/dock honesty.** `horizontalStack`/`verticalStack` position each child at its
  measured bounds via `Scene.translate`; `dock` consumes edges per `DockPosition` (sized by the
  child's `DesiredWidth`/`DesiredHeight`), `Fill`/`None` taking the remaining rect.
- **FR-007 — no surface regressions.** The public-surface baseline gate stays green: the only added
  exported type is `PathCombineError` (`FS.GG.UI.Scene`); `sampleColor` and the stack/dock changes
  add/adjust functions only.

## Success criteria

- Each site has a real regression test with a DISCRIMINATING assertion (a stub could not pass):
  stack/dock placement offsets, `sampleColor` interpolation + `applyAt` colour-neutrality,
  `render`-Png-fails-loud (and writes no `.png`), `combine` Union/Xor `Ok` + Intersect/Difference
  fail-loud, and `segment` non-empty extraction.
- Full solution builds; the affected test projects (Scene, Layout, Lib, Package/surface, SkiaViewer,
  Controls, Testing) are green.

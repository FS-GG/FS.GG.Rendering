# Implementation Plan: kill success-shaped stubs on the public API (P6 / #49)

## Approach

One focused change per site, following the `Evidence.renderPng` honest-failure idiom: implement
where the dependency-light layer allows, otherwise fail loud with a typed diagnostic and disclose
the residual limitation in the `.fsi`.

## Changes

### Scene (`src/Scene`)

- **`Types.fs` / `Types.fsi`** — add `type PathCombineError = { Operation: PathOperation; Message: string }`.
- **`Scene.fs` / `Scene.fsi`** — `Path.combine` now returns `Result<PathSpec, PathCombineError>`:
  `Union → Winding`, `Xor → EvenOdd` (concatenation + fill rule), `Intersect`/`Difference → Error`.
  `Path.segment` extracts along the polyline of vertex-bearing commands (`polyline`/`lerpPoint`
  helpers), consistent with `measure`'s chord metric; `.fsi` discloses the approximation.
- **`Animation.fs` / `Animation.fsi`** — add `sampleColor`; `applyAt` doc discloses colour is
  sampled, not composited.
- **`Evidence.fs`** — `render` `Format = Png` fails loud (typed `ProductDefect`/renderer-stage error);
  `renderPng` validates via `Format = Hash` (format-independent size/mode rules) since Png now fails.

### Layout (`src/Layout/Layout.fs`)

- `horizontalStack`/`verticalStack` place each child at its `measureHorizontal`/`measureVertical`
  bounds via `Scene.translate` (`placeChild`). `dock` folds an edge-consuming rect honoring
  `DockPosition` and the child `DesiredWidth`/`DesiredHeight`; `Fill`/`None` take the remainder.
  Signatures unchanged.

### SkiaViewer (`src/SkiaViewer/SkiaViewer.fs` / `.fsi`)

- `runBounded` drops `ignore scene`; a `writeRunEvidence` helper routes `.png` evidence paths through
  the shared CPU painter (`writeSceneImageEvidence`) and other paths through the text summary. `.fsi`
  discloses the frame-cadence-vs-presentation distinction for `runBounded`/`runUntilFirstFrame`/
  `runForFrames`.

### Surface baseline

- `readiness/surface-baselines/FS.GG.UI.Scene.txt` gains `FS.GG.UI.Scene.PathCombineError`.

## Tests

- `tests/Scene.Tests/Audit_AnimationSampling.fs` — colour-tween audits (`sampleColor` interpolation;
  `applyAt` colour-neutral, DISCRIMINATING).
- `tests/Scene.Tests/Tests.fs` — `render` `Format = Png` fails loud and writes no `.png`.
- `tests/Layout.Tests/Tests.fs` — stack positioning at measured bounds; dock left-edge consumption.
- `tests/Lib.Tests/Tests.fs` — `combine` Union `Ok` (Winding) / Xor `Ok` (EvenOdd) /
  Intersect+Difference fail-loud; `segment` non-empty.

## Out of scope

- Real GL presentation of the scene inside `runBounded` (overlaps R3's `SkiaViewer` god-module split);
  bounded runs keep window/frame-cadence semantics + CPU image evidence.
- A true Skia boolean-geometry kernel or curve arc-length reparameterisation in the Scene layer.

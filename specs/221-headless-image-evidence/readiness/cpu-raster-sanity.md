# CPU raster sanity (T003)

**Goal**: confirm the existing no-`GRContext` CPU raster path builds and produces a non-blank PNG with
no GPU / OpenGL / X / display.

## Result: ✅ confirmed (real)

- `dotnet build src/SkiaViewer/SkiaViewer.fsproj` (via the test-project build) succeeded — `SkiaSharp`
  CPU raster (`SKSurface.Create(SKImageInfo)`, **no `GRContext`**) loads its native assets in this bare
  container.
- `ReferenceRendering.renderScenePng` (`src/SkiaViewer/ReferenceRendering.fs:119-137`) was exercised
  headlessly through the new `renderScenePngResult` entry: it produced a **decodable 800×600 RGBA PNG**
  with visible content (see `../evidence/representative-game-scene.png`).
- `file` on the artifact reports: `PNG image data, 800 x 600, 8-bit/color RGBA, non-interlaced`.

This validates the plan's core premise: a working no-GL CPU rasterizer already exists; the feature
bridges it into the dependency-light `Scene.Evidence` surface rather than writing a new one.

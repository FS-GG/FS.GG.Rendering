# Early live smoke run (T005)

**Intent**: before building the fix, drive the real viewer with `PresentMode = OffscreenReadback` for the
representative scene to validate the T004 hypotheses against a live GL readback.

## Result: ⚠️ environment-limited (GL route) — substitute recorded

`OffscreenReadback` → `renderSceneToPixels` (`src/SkiaViewer/Host/OpenGl.fs:790-826`) is the **GL-required**
route: it creates `SKSurface.Create(context: GRContext, …)` over a live GL context. This runner is a bare
**no-GL** container (no GPU, no OpenGL, no X/virtual display per the capability baseline), so the live GL
readback path is **environment-limited** here — it is *expected* to be unavailable, exactly the gap the P1
no-GL path exists to fill.

This does **not** block the feature: US1/P1 is explicitly the **no-GL CPU** path, and that path is proven
live (real 800×600 PNG, `../evidence/representative-game-scene.png`).

## Disclosed substitute

In place of a live GL `OffscreenReadback` capture, the T004 hypotheses were validated by:

1. **CPU raster sanity** (`cpu-raster-sanity.md`) — the no-`GRContext` donor produces a real headless PNG.
2. **Passing US1 semantic tests** (`tests/SkiaViewer.Tests/HeadlessImageEvidenceTests.fs`, 6/6 green) —
   determinism, dimensions, non-blank, concurrency through the public `renderPng` surface.

Fact (b) (direct-to-swapchain, no readback on the live path) remains a verified **static** finding; its
live confirmation requires a GL/virtual-display host and is the subject of US2's `../evidence/us2-live-frame.md`
(also `environment-limited` here, with a disclosed substitute).

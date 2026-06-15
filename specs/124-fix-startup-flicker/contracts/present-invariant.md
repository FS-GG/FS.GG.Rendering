# Contract: one present per frame (live `GlHost` path)

The fix is a behavioral invariant, not a new API. It adds no public surface.

## Invariant

For the live `DirectToSwapchain` host (`GlHost`, `src/SkiaViewer/Host/OpenGl.fs`), **each
rendered frame results in exactly one buffer present (`SwapBuffers`).**

- The present routines (`renderFrameDirect`, `representLastGoodFrame`, `renderFrameReadback`)
  call `window.SwapBuffers()` **explicitly**, because the present *decision* (`planPresent`:
  paint / represent / skip) must control when and whether a swap happens.
- Therefore Silk.NET's automatic swap must be **disabled**:
  `options.ShouldSwapAutomatically <- false` in `createWindow`. With the default (`true`),
  `window.DoRender()` swapped a second time after the render callback, presenting an undefined
  back buffer → black flicker.

**Violation symptom:** intermittent black flashes on a compositor (the undrawn second-swap
buffer), worse for longer paints.

## Secondary invariant: render is paced, not per-input-event

Pointer-move events update the model only; they do **not** emit a `RenderFrame` effect. The
paced `RenderTick` (bounded by `ViewerOptions.FrameRateCap`, 60 Hz default) is the sole driver
of presents. This matches the existing key-input path.

**Violation symptom:** a fast mouse floods the loop with full repaints (≫ frame cap), backing
up event processing so input arrives in stutters ("rubberbanding").

## Non-goals / preserved

- `planPresent` / `shouldPresent` / `shouldAdvanceFrame` / `PresentAction` — unchanged
  (features 120/121/122 idle-skip and pacing preserved).
- Headless paths (`captureScreenshotEvidence`, `runForFrames`/`runBounded`) — do not manually
  swap and do not use `createWindow`; byte-identical output preserved (SC-004).
- No `ViewerOptions` / `.fsi` / surface-baseline change.

# Phase 0 — Research: Fix Startup Flicker

> **Note (post-implementation correction).** The original Phase-0 research hypothesised the
> flicker was an *undrawn-swapchain-buffer / startup-priming* problem (a `planStartupPriming`
> capability). That hypothesis was **wrong** and was discarded. This document records the
> actual, empirically-confirmed root cause and the fix. The real cause was found by building
> a minimal renderer probe and climbing from bare GL up to the framework's exact structure.

## R0 — Confirmed root cause: double buffer-swap per frame

- **Decision**: The flicker is a **double `SwapBuffers` per frame** in the live `GlHost`
  present path. Silk.NET's `WindowOptions.ShouldSwapAutomatically` defaults to **`true`**,
  so `window.DoRender()` swaps the buffers automatically *after* the render callback — but
  the framework's present routines (`renderFrameDirect`, `representLastGoodFrame`,
  `renderFrameReadback`) **also** call `window.SwapBuffers()` explicitly. Every frame is
  therefore swapped twice; the second swap presents an **undefined back buffer**, which the
  compositor shows as a **black flash** (longer for bigger paints, since the GPU is mid-draw
  longer). Nothing in the framework set `ShouldSwapAutomatically`, so the default was active.
- **Why interaction "stopped" it / why it varied**: the perceived behavior shifted with the
  present cadence (idle-skip vs. per-event repaints), which is why it first read as "flickers
  until input." The constant underlying defect was the extra swap.

## R1 — How the root cause was found (the probe ladder)

A standalone minimal renderer (`Silk.NET` window + GL/Skia, no MVU, no framework present
logic) was built and complexity added one rung at a time. Each rung was verified by direct
observation on the live X11/KWin (Xwayland) desktop:

| Rung | What it did | Result |
|------|-------------|--------|
| bare GL | `glClear` + `SwapBuffers` every frame | **steady** |
| Skia clear | SkiaSharp wraps FBO 0, `canvas.Clear` + flush + swap | **steady** |
| Skia + draw + `Snapshot()` | adds the per-frame `surface.Snapshot()` (feature-122 cache) | **steady** |
| borderless | Skia clear+swap in a borderless screen-covering window (gallery's window type) | **steady** |
| **framelike** | render via `DoRender()` with `ShouldSwapAutomatically = true` **and** an explicit `SwapBuffers()` | **FLICKERS** |
| framelike-noautoswap | same, but `ShouldSwapAutomatically = false` (single swap) | **steady** |

The only rung that flickered was the one that double-swapped — definitively isolating the
cause. A complementary diagnostic (forcing the clear colour to red) showed the steady image
was red but the **flashes stayed black**, proving the flashes were an *undrawn buffer*, not
our clear — consistent with the extra swap presenting an undefined buffer.

## R2 — Fix

- **Decision**: Set `options.ShouldSwapAutomatically <- false` in `GlHost.createWindow`
  (`src/SkiaViewer/Host/OpenGl.fs`). The present path already swaps explicitly, so exactly
  one present per frame remains. One line; framework-level (fixes every consumer of the
  interactive window, not just the gallery).
- **Alternatives considered**: remove the explicit `SwapBuffers()` and rely on auto-swap —
  rejected because the present logic needs explicit control of *when* it swaps (idle-skip /
  represent decisions in `planPresent`), which the automatic post-`DoRender` swap cannot
  express.

## R3 — Secondary fix: pointer-event repaint flood (input lag)

- **Decision**: In `runPresentedPersistentWindow` (`SkiaViewer.fs`), the `LegacyPointer`
  arm emitted a full `RenderFrame` on **every** pointer-move event, bypassing the
  `FrameRateCap` (renders spiked to ~3× the cap, ~180/s, on a fast mouse) and backing the
  loop up so input arrived in stutters/bursts ("rubberbanding"). Changed it to update the
  model only and let the paced 60 Hz `RenderTick` present — matching what the `LegacyKey`
  arm already did. Framework-level.
- **Evidence**: per-second instrumentation showed renders pinned at ~180/s during mouse
  movement before the change and capped at ~50–55/s after, with input no longer stuttering.

## R4 — Isolation / no collateral damage

- The headless evidence path is structurally separate (`captureScreenshotEvidence` is a CPU
  `SKBitmap` path; `runForFrames` uses its own `runBounded` window). Neither calls
  `GlHost.createWindow` or the explicit-swap present routines, and neither manually swaps —
  so the determinism guarantee (SC-004) is untouched. Verified: gallery determinism + degrade
  suites pass (6/6), SkiaViewer present/pacing suites pass (94/94).

## R5 — Verification approach

The perceptual outcome (no flicker, smooth input) is a compositor-visible artifact not
observable in-process — confirmed by external capture failing (`ffmpeg`/`import` error on the
DRI3 GL surface). Verification is therefore by **observation on the live X11 desktop** (the
spec's stated approach), backed by the controlled probe experiment above and the unchanged
deterministic regression suites.

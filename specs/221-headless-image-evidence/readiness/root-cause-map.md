# Root-cause map (T004)

Three facts, verified against the codebase, and the FR each closes.

## (a) `renderPng` returned a hash, not a PNG — ✅ confirmed

`SceneEvidence.renderPng` (`src/Scene/Evidence.fs`, pre-change L108-118) routed `Format = Png` to
`render`, whose `Png` branch returned `readback.DeterministicHash`, then wrapped it as
`Encoding.UTF8.GetBytes`. The result was a ~64-byte UTF-8 hash string — a success-shaped **non-image**.
**Closed by**: FR-001, FR-002, FR-005, SC-005 → the rewired `renderPng` now sources real pixels from the
injected CPU rasterizer and fails typed when none is available.

## (b) The live viewer presents direct-to-swapchain, no GPU→CPU readback — ✅ confirmed

The GL default (`OpenGl.fs`, `DirectToSwapchain`, ~L828) draws onto the FBO-0 `SKSurface` and presents
via buffer swap with **no readback** — so an external X11 grab reads black. The on-demand readback path
`renderSceneToPixels` (`OpenGl.fs:790-826`) creates a GL surface over a `GRContext` and **requires GL**.
**Closed by**: FR-006 (US2) → documents the supported `OffscreenReadback` → `renderSceneToPixels` route
and states its GL/virtual-display prerequisite.

## (c) `ReferenceRendering.renderScenePng` is a working no-`GRContext` CPU raster — ✅ confirmed

`renderScenePng` (`ReferenceRendering.fs:119-137`) builds `SKSurface.Create(SKImageInfo)` (CPU, no
`GRContext`), runs the shared exhaustive `SceneRenderer.paintNode`, and `image.Encode(Png)`. It runs with
no GPU/GL/display (see `cpu-raster-sanity.md`).
**Closed by**: FR-001, FR-003, FR-004, FR-008 → exposed as `renderScenePngResult` and injected into the
`Scene.Evidence` PNG seam.

## FR → fix index

| FR | Fix |
|---|---|
| FR-001/002/003/004/008 | `renderScenePngResult` (SkiaViewer CPU raster) injected via `SceneEvidence.setRealPngRasterizer`; `renderPng` returns its bytes. |
| FR-005 | Uninjected / unproducible → typed `UnsupportedEnvironment`/`ProductDefect`, never a stub. |
| FR-006 | US2 docs for the GL `OffscreenReadback` live-frame route. |
| FR-007 | `render`/`renderHash`/metadata/evidence-file branches untouched; only `renderPng`'s pixel source changed. |
| FR-009 | `evidence-formats.md`, `capability-baseline.md`, `docs/usage.md` updated. |

## Standing-assumption note

The hypotheses above were static-map hypotheses until run. Fact (c) is now **proven** live (real PNG,
`evidence/`). Fact (b)'s GL `OffscreenReadback` route is **not** live-verifiable on this bare no-GL
runner — recorded `environment-limited` in `smoke-run.md` (it does not gate the P1 deliverable, which is
the no-GL path).

# Phase 0 Research: Headless Image Evidence Path

All Technical Context unknowns were resolvable from a verified codebase map (no open `NEEDS CLARIFICATION`). Decisions below are the basis for Phase 1 and `tasks.md`.

## D1 — Headless rasterization backend: reuse the existing SkiaSharp CPU raster

- **Decision**: Produce headless PNGs with the **existing** SkiaSharp CPU raster path — `SKBitmap`/`SKSurface.Create(SKImageInfo)` (no `GRContext`) driven by the shared `SceneRenderer.paintNode`, then `SKImage.Encode(Png)`. Donors: `ReferenceRendering.renderScenePng` (`src/SkiaViewer/ReferenceRendering.fs:119-137`) and `writeSceneImageEvidence` (`src/SkiaViewer/SkiaViewer.fs:1824-1842`).
- **Rationale**: A working no-GL, no-display Scene→PNG rasterizer already exists and already produces faithful real pixels including text; building a second rasterizer would duplicate the exhaustive painter and invite drift. "Even slow" is acceptable (FR-008); CPU raster meets the <5 s bound (SC-004).
- **Alternatives considered**:
  - *New from-scratch CPU rasterizer in `src/Scene`* — rejected: forces SkiaSharp into the dependency-light Scene package (forbidden, `src/Scene/skill/SKILL.md:53`) and re-implements `paintNode`.
  - *GL offscreen + readback* (`renderSceneToPixels`) — rejected for P1: requires a `GRContext`/display, the exact gap the issue reports. Retained for **US2** only.
  - *A different software rasterizer (e.g. a non-Skia library)* — rejected: a new dependency with no payoff over the Skia CPU surface already linked.

## D2 — Keep `src/Scene` SkiaSharp-free via an injectable seam

- **Decision**: Add `setRealPngRasterizer: (Size -> Scene -> Result<byte[], SceneEvidenceFailure>) -> unit` to `src/Scene/Scene.fsi`, defaulting to a typed `UnsupportedEnvironment` failure; SkiaViewer injects the real CPU rasterizer at the same place it already injects the text measurer (`src/SkiaViewer/Fonts.fs:520`).
- **Rationale**: Mirrors the **established, justified** `setRealTextMeasurer` seam (`Scene.fsi:131`) — Scene stays pure/dependency-light, SkiaSharp stays in SkiaViewer, and the compiler-enforced `.fsi` keeps the surface honest (Principle II). One pattern, two injections.
- **Alternatives considered**:
  - *Add a `ProjectReference` from Scene → SkiaViewer* — rejected: inverts the dependency direction and pulls SkiaSharp into Scene transitively.
  - *Move `renderPng` out of Scene into SkiaViewer* — rejected: breaks the public `Scene.Evidence` surface that consumers/CI already call (FR-007) and would be a larger contract change.

## D3 — Honest failure reuses the existing typed model

- **Decision**: Reuse `SceneEvidenceFailure` / `SceneEvidenceFailureClassification (UnsupportedEnvironment | ProductDefect)` / `EvidenceStage (Scene | Renderer)` at `src/Scene/Evidence.fs:11-49`. `renderPng` returns these and writes nothing on failure; no new failure type unless a real gap appears.
- **Rationale**: The model and its rules (zero/negative → `ProductDefect`; unsupported renderer → `UnsupportedEnvironment`) already exist and already satisfy Principle VI. US3 is mostly *wiring the PNG path into the existing model*, not new design.
- **Alternatives considered**: *New failure DU for image evidence* — rejected as redundant; would fragment the diagnostic taxonomy.

## D4 — Determinism source: bundled fonts + premul Rgba8888 + sorted/stable draw

- **Decision**: Force `SKColorType.Rgba8888`, `SKAlphaType.Premul`, a cleared transparent surface, and **bundled embedded fonts** via `src/SkiaViewer/Fonts.fs` (9 `.ttf`, host-independent). A missing glyph face is **disclosed** (typed advisory/metadata), not silently substituted in a determinism-breaking way.
- **Rationale**: Determinism (FR-003/SC-002) depends only on inputs the product controls; host system fonts are excluded. The existing T0 harness already proves byte-identical re-render with this path (`tools/Rendering.Harness/Tiers.fs:49-54`).
- **Alternatives considered**: *Host system fonts* — rejected: breaks cross-machine determinism. *Disabling text* — rejected: FR-002 requires real text content.

## D5 — US2 live-window capture path is GL-required and documented, not re-engineered

- **Decision**: Document the supported live-frame capture as `ViewerPresentMode.OffscreenReadback` → `renderSceneToPixels` (`src/SkiaViewer/Host/OpenGl.fs:788-826`), explicitly stating the **GL/virtual-display prerequisite**. Add an on-demand request seam only if one is genuinely missing.
- **Rationale**: FR-006 asks for a *documented, supported* path, not a new mechanism; the GL readback already exists. The spec Assumption permits a virtual display for US2. Keeping P1 (no-GL) and US2 (GL) distinct prevents confusing the two routes.
- **Alternatives considered**: *Make US2 headless-no-GL too* — out of scope; P1 already covers no-GL evidence. *External X11 capture of the live window* — rejected: reads black on the direct-to-swapchain present path (the original failure).

## D6 — FR-009 documentation truth-up

- **Decision**: Flip the `no software-renderer fallback` token in the `runtime-limitations.md` contract (`template/base/docs/evidence-formats.md:25-28` + schema source), reconcile `real-image-evidence.md` (`evidence-formats.md:68-72`), and revise `docs/usage.md:182-226` + the T1 row in `docs/harness/capability-baseline.md:27`.
- **Rationale**: These are the in-repo surfaces that currently assert headless image evidence is unobtainable; FR-009 requires they describe the new supported path. (The literal §5.1 note lives in cross-repo epic `FS-GG/.github#74` and is out of this repo's edit scope — referenced, not edited.)
- **Alternatives considered**: *Edit only `docs/usage.md`* — rejected: leaves the generated evidence-format contract asserting the false `no software-renderer fallback` token.

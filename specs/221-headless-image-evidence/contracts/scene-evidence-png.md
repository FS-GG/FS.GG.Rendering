# Contract: Headless Scene→PNG Evidence

**Type**: F# public API surface (library contract). **Classification**: Tier 1 — `.fsi` + surface baseline updates mandatory.

This contract governs the public `Scene.Evidence` PNG surface and the new injection seam. It is the authority `tasks.md` T007/T013/T020 implement against, and what `tests/Package.Tests` (surface gate) and `tests/SkiaViewer.Tests` (behavior) verify.

## C1 — `SceneEvidence.renderPng` (existing signature, changed behavior)

```fsharp
// src/Scene/Evidence.fsi
val renderPng : size: Size -> scene: Scene -> Result<byte[], SceneEvidenceFailure>
```

**Signature**: unchanged (no surface drift on this symbol).

**Behavioral contract (new)**:

| # | Given | Then |
|---|---|---|
| C1.1 | `Size` W>0,H>0; a rasterizer is injected; render succeeds | `Ok bytes` where `bytes` is a valid PNG of exactly W×H with non-blank scene content (FR-001, FR-002). |
| C1.2 | Same `(scene, size)` invoked twice | Both `Ok` results are **byte-for-byte identical** (FR-003). |
| C1.3 | No rasterizer injected, or render cannot complete | `Error { Classification = UnsupportedEnvironment; BlockedStage = Renderer; … }`; **no bytes**, no file written (FR-005). |
| C1.4 | `Size` with W≤0 or H≤0 | `Error { Classification = ProductDefect; … }` (existing rule preserved). |
| C1.5 | Any non-success outcome | The function MUST NOT return a `byte[]` that is not a valid image (no hash/stub masquerade) (FR-005, SC-005). |
| C1.6 | `Format = Hash` or metadata (sibling routines) | Unchanged from today (FR-007). |
| C1.7 | N independent concurrent calls | Each result is unaffected by the others and individually deterministic (Edge Case: concurrency). |

## C2 — `Scene.setRealPngRasterizer` (new seam)

```fsharp
// src/Scene/Scene.fsi
val setRealPngRasterizer : (Size -> Scene -> Result<byte[], SceneEvidenceFailure>) -> unit
```

**Contract**:

| # | Rule |
|---|---|
| C2.1 | `src/Scene` MUST NOT reference SkiaSharp; the seam is the only bridge (mirrors `setRealTextMeasurer`). |
| C2.2 | Default (uninjected) behavior returns the `UnsupportedEnvironment` failure of C1.3 — never a stub or throw. |
| C2.3 | The injected function is supplied by `src/SkiaViewer` (`Fonts.fs` wiring point) and MUST be re-entrant/thread-safe (supports C1.7). |
| C2.4 | Injection is idempotent/last-wins, consistent with `setRealTextMeasurer` semantics. |

## C3 — SkiaViewer CPU rasterizer entry (the injected implementation)

```fsharp
// src/SkiaViewer/ReferenceRendering.fs (or adjacent)
val renderScenePngResult : Size -> Scene -> Result<byte[], SceneEvidenceFailure>
```

**Contract**:

| # | Rule |
|---|---|
| C3.1 | Uses `SKBitmap`/`SKSurface.Create(SKImageInfo(...))` with **no `GRContext`** — runs with no GPU/GL/X/display (FR-004). |
| C3.2 | Rasterizes via the shared `SceneRenderer.paintNode`; deterministic premul Rgba8888; bundled fonts only. |
| C3.3 | A missing font face is **disclosed** (typed advisory / recorded metadata), not silently substituted in a determinism-breaking way (Edge Case: fonts). |
| C3.4 | Encodes PNG via `SKImage.Encode(SKEncodedImageFormat.Png, _)`. |
| C3.5 | Completes a representative scene in < 5 s on a standard CI runner (SC-004). |

## C4 — US2 live-frame capture (documented contract, GL-required)

| # | Rule |
|---|---|
| C4.1 | The supported live-frame capture path is `ViewerPresentMode.OffscreenReadback` → `renderSceneToPixels` → `encodeSnapshot`, documented end-to-end in `docs/usage.md` (FR-006, SC-003). |
| C4.2 | This path **requires a GL context / virtual display** — explicitly stated; it is NOT the no-GL P1 path. |
| C4.3 | Following the documentation alone yields a non-black current-frame image with zero steps requiring binary inspection or trial-and-error (SC-003). |

## Verification map

- C1.1–C1.7, C2.2 → `tests/SkiaViewer.Tests/HeadlessImageEvidenceTests.fs` (T008–T010, T018–T019).
- C2.1, C2.4, signature stability of C1/C2 → `tests/Package.Tests` surface gate (T024).
- C3.* → exercised through C1 (the injected path) + sanity T003 + timing T014.
- C4.* → docs (T016) + live proof (T017).

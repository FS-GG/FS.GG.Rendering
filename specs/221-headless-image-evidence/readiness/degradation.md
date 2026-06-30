# CPU-vs-GL fidelity / degradation disclosure (T022)

**Spec Edge Case**: scene content the headless rasterizer cannot reproduce faithfully (e.g. GPU-only
effects) must still produce an image with a **documented, deterministic** degradation that is **disclosed**,
not silently dropped.

## Key structural fact: the painter is shared and exhaustive

The headless CPU path and the live GL path render through the **same** `SceneRenderer.paintNode`
(`src/SkiaViewer/SceneRenderer.fs:246-418`) — an exhaustive, **no-wildcard** match over every `SceneNode`
kind. They differ only in the *surface* (`SKSurface.Create(SKImageInfo)` CPU vs `SKSurface.Create(GRContext,…)`
GL), not in *what is painted*. Therefore **no `SceneNode` kind is silently dropped** on the headless path —
there is no "GPU-only node" the CPU path skips.

## Enumerated degradations

| Feature | CPU-vs-GL behaviour | Disclosure |
|---|---|---|
| Rasterization / antialiasing / subpixel rounding | CPU Skia and GPU Skia may differ at the pixel level. | **By design, not a defect**: spec Assumption explicitly states exact GPU/CPU pixel parity is **not** required; a deterministic CPU rasterization of the same scene is sufficient evidence. CPU output is byte-deterministic across runs (FR-003). |
| Bundled-font coverage (uncovered code point → fallback/tofu) | A glyph outside the 9 bundled `.ttf` faces resolves to a deterministic fallback / tofu rather than a host font. | **Disclosed** via the existing fallback channel: `SceneRenderer.fallbackEvents` → `Text.fallbackReport()` / `Text.fallbackDiagnostics()`. Asserted in the T022 test ("headless render discloses bundled-font fallback rather than silently substituting"): rendering `"score 中"` yields `SubstitutedCount + TofuCount > 0`. Determinism is preserved (no host-font dependence). |
| GPU-context-dependent paint (e.g. effects requiring a `GRContext`/backend texture) | The painter takes no `GRContext`-only branch; all paint goes through the shared CPU-capable path. | None observed — the shared exhaustive painter has no GL-only node branch. If one were added later, the no-wildcard match would force an explicit CPU handling/disclosure decision at that site (compile-checked). |

## Conclusion

The only material CPU-vs-GL difference is sub-pixel rasterization, which the spec deliberately accepts.
The one deterministic content degradation (font fallback for uncovered glyphs) is disclosed through the
existing typed channel and covered by a test. No silent drops.

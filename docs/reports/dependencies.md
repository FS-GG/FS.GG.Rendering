# Dependency Report

## SkiaSharp.HarfBuzz

- Feature: 142 HarfBuzz text shaping.
- Version: `4.147.0-preview.3.1`, pinned centrally in `Directory.Packages.props`.
- Native assets: `HarfBuzzSharp.NativeAssets.Linux` and `HarfBuzzSharp.NativeAssets.Win32` at `8.3.1.6-preview.3.1`, matching the HarfBuzzSharp dependency brought by `SkiaSharp.HarfBuzz`.
- Owner: `src/SkiaViewer`.
- Rationale: HarfBuzz shaping must live at the SkiaViewer interpreter edge so Scene remains dependency-light while SkiaViewer can produce glyph ids, clusters, positions, advances, and fallback diagnostics from the same shaped result used for drawing.
- Boundary: `src/Scene/Scene.fsproj` does not reference `SkiaSharp`, `SkiaSharp.HarfBuzz`, `HarfBuzzSharp`, `SkiaViewer`, `Controls`, `Elmish`, `Yoga`, or native host packages.
- Migration impact: additive preview dependency for SkiaViewer consumers only; existing pure Scene and no-provider fallback paths remain available.

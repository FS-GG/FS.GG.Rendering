# Feature 142 Dependency Boundary

Status: PASS for source project boundaries.

- `SkiaSharp.HarfBuzz` is pinned centrally in `Directory.Packages.props`.
- `HarfBuzzSharp.NativeAssets.Linux` and `HarfBuzzSharp.NativeAssets.Win32` are pinned centrally to provide the native HarfBuzz assets required by `SkiaSharp.HarfBuzz`.
- `src/SkiaViewer/SkiaViewer.fsproj` owns the `SkiaSharp.HarfBuzz` package reference.
- `src/Scene/Scene.fsproj` remains dependency-light and has no SkiaSharp, HarfBuzzSharp, SkiaViewer, Controls, Elmish, Yoga, or Silk.NET references.
- Validation: `Feature142ShapedTextTests`, `Feature142SurfaceAndDependencyTests`, and `dotnet build FS.GG.Rendering.slnx --no-restore` passed on 2026-06-17.

FSI/prelude transcript note: the new `Scene.buildFallbackShapedText`, `Scene.shapedTextFingerprint`, `Scene.measureShapedText`, and `Scene.glyphRunDataFromShapedText` signatures are exercised by `Feature142ShapedTextTests`.

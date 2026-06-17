# Feature 142 Measure/Draw Parity

Status: implemented for the dependency-light shaped result and SkiaViewer HarfBuzz provider path.

- Scene fallback shaped metrics derive from glyph advances.
- SkiaViewer shaped results store HarfBuzz glyph ids, clusters, positions, advances, and provider evidence.
- `SceneRenderer` paints `GlyphRunData` with stored glyph ids and positions when provider evidence is installed.
- Validation:
  - `dotnet test tests/Scene.Tests/Scene.Tests.fsproj --no-build --logger "console;verbosity=minimal"`: PASS, 49 passed.
  - `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --no-build --logger "console;verbosity=minimal"`: PASS, 107 passed.
  - `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-build --logger "console;verbosity=minimal"`: PASS, 22 passed.

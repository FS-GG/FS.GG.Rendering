# Feature 146 Round-Trip Corpus

## Representative Scene IDs

- `basic-primitives`: rectangles, circles, paths, and text.
- `layered-portal`: nested groups, clips, transforms, regions, and pictures.
- `shaped-text`: glyph-run proof data and provider evidence.
- `image-resource`: image resource manifest and package-local image references.

## Evidence

- `basic-primitives`: covered by `Feature146PortableSceneRoundTripTests` and harness corpus.
- `layered-portal`: covered by `RenderAnywhere.corpus`.
- `shaped-text`: glyph-run proof data preservation covered by
  `Feature146PortableSceneRoundTripTests`.
- `image-resource`: package-local image resource manifest behavior covered by
  `Feature146PortableSceneRoundTripTests`, `Feature146PortableSceneResourceTests`, and
  `Feature146PackageResourceInspectionTests`.

Validation command:

```bash
dotnet test tests/Scene.Tests/Scene.Tests.fsproj --filter Feature146
```

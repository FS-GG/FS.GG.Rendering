# Feature 146 Reference Rendering

The reference oracle is implemented in `FS.GG.UI.SkiaViewer.ReferenceRendering`.

It inspects portable package bytes with `SceneCodec.inspectWith`, rejects incompatible packages
before rendering, paints accepted packages through the existing exhaustive `SceneRenderer`, writes a
PNG artifact, validates that the PNG decodes and contains visible pixels, and records renderer,
protocol, capability, resource, and package identity metadata.

Environment-limited evidence is explicit: it does not include an accepted image artifact and cannot
count as a passed visual oracle.

Validation commands:

```bash
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature146
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- render-anywhere-reference --out specs/146-render-anywhere-protocol/readiness/reference
```

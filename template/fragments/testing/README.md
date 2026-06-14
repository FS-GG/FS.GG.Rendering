# Testing Fragment

Adds testing helper package references and product validation guidance.

Generated evidence can record semantic scene facts such as lander, terrain,
landing pad, and HUD metrics from deterministic-scene-evidence.
deterministic-scene-evidence does not prove semantic object presence in a live
screenshot unless the screenshot artifact is captured from the live viewer
window. A pixel-readback fallback must record `fallback-reason` and
`proves-screenshot=false`.

```fsharp
open FS.Skia.UI.Testing

let expectation =
    { Profile = "governed"
      RequiredFiles = [ "src/Product/Product.fsproj"; "docs/effects-boundary.md" ]
      ForbiddenPrefixes = [ "samples/" ]
      PackageReferences =
        [ { PackageId = "FS.Skia.UI.Scene"; Required = true }
          { PackageId = "FS.Skia.UI.Testing"; Required = true } ] }

let summary = GeneratedProductAssertions.summarize expectation
```

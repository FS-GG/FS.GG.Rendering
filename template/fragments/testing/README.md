# Testing Fragment

> **Not shipped — this is repo-side fragment documentation.**
> `.template.config/template.json` sources nothing from `template/fragments/testing/`, so no generated
> product ever receives this file. The guidance a product actually gets is
> [`template/product-skills/fs-gg-testing/SKILL.md`](../../../template/product-skills/fs-gg-testing/SKILL.md) — the capability's `supplied-by`.
> Recorded as `materializes: none` on the `testing` row of `template/capabilities.yml`, and held there
> by R-FRAG in `tests/Package.Tests/SkillPackageReachTests.fs` (#510).

Adds testing helper package references and product validation guidance.

Generated evidence can record semantic scene facts such as lander, terrain,
landing pad, and HUD metrics from deterministic-scene-evidence.
deterministic-scene-evidence does not prove semantic object presence in a live
screenshot unless the screenshot artifact is captured from the live viewer
window. A pixel-readback fallback must record `fallback-reason` and
`proves-screenshot=false`.

```fsharp
open FS.GG.UI.Testing

let expectation =
    { Profile = "governed"
      RequiredFiles = [ "src/Product/Product.fsproj"; "docs/effects-boundary.md" ]
      ForbiddenPrefixes = [ "samples/" ]
      PackageReferences =
        [ { PackageId = "FS.GG.UI.Scene"; Required = true }
          { PackageId = "FS.GG.UI.Testing"; Required = true } ] }

let summary = GeneratedProductAssertions.summarize expectation
```

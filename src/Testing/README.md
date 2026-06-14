# FS.Skia.UI.Testing

Generated product and package validation helpers for FS.Skia.UI V3 products.

`FS.Skia.UI.Testing` is one of the **FS.Skia.UI** distribution packages — an F# / Elmish UI and 2D
scene-graph framework for .NET 10 desktop, rendered through Vulkan + SkiaSharp.

## Install

```bash
dotnet add package FS.Skia.UI.Testing
```

Or scaffold a full governed project that wires the FS.Skia.UI packages together:

```bash
dotnet new install FS.Skia.UI.Template
dotnet new fs-skia-ui -o MyApp
```

## Usage

```fsharp
open FS.Skia.UI.Testing

// Describe what a generated product is expected to contain.
let expectation =
    { Profile = "default"
      RequiredFiles = [ "Program.fs"; "Directory.Packages.props" ]
      ForbiddenPrefixes = [ "Internal." ]
      PackageReferences =
        [ { PackageId = "FS.Skia.UI"; Required = true } ] }

printfn "%s" (GeneratedProductAssertions.summarize expectation)

// Detect pinned-package drift against the expected local consumer feed.
let expected = [ { PackageId = "FS.Skia.UI"; Version = "3.0.0"; FeedPath = "./feed" } ]
let actual = [ { PackageId = "FS.Skia.UI"; Version = "2.9.0"; FeedPath = "./feed" } ]

for drift in LocalConsumerPackages.classifyDrift expected actual do
    printfn "%s expected %s but found %A — run: %s"
        drift.PackageId drift.ExpectedVersion drift.ActualVersion drift.RemediationCommand
```

## API at a glance

- `GeneratedProductAssertions` — `summarize` a `GeneratedProductExpectation`, plus
  `validateDefaultInteractiveLaunch` and `validateWindowDiagnostics` for launch/window checks.
- `LocalConsumerPackages` — `report` the local consumer feed and `classifyDrift` to surface
  version mismatches with remediation commands.
- `GeneratedConsumerValidation` — verify package resolution and generated tests, `selectVisualEvidence`,
  and `buildValidationContractOutput` to assemble the full generated-product validation contract.
- `GeneratedLayoutValidation` / `DefaultTextGlyphEvidence` — `validate` HUD layout bounds and rendered
  text-glyph coverage from captured evidence.
- `HostWarningClassification.classify` — classify a host warning as benign vs. launch/render/layout/package failure.
- `EvidenceReports` — `build`, `write`, and `validate` evidence reports, including
  `validateScreenshotArtifact` and `validateScreenshotEvidence` for screenshot proofs.
- `PersistentLaunchArtifactValidation` / `ReadinessFileDiscovery` — `validate` persisted launch artifacts
  and required readiness files.

## Versioning

All `FS.Skia.UI.*` libraries share one version and move together. In a generated project a
single `<FsSkiaUiVersion>` in `Directory.Packages.props` pins every package — upgrading is one
edit; see `docs/UPGRADING.md`. Pre-release versions use a `-preview.N` suffix.

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT

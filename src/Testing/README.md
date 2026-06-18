# FS.GG.UI.Testing

Generated product and package validation helpers for FS.GG.UI V3 products.

`FS.GG.UI.Testing` is one of the **FS.GG.UI** distribution packages — an F# / Elmish UI and 2D
scene-graph framework for .NET 10 desktop, rendered through Vulkan + SkiaSharp.

## Install

```bash
dotnet add package FS.GG.UI.Testing
```

Or scaffold a full governed project that wires the FS.GG.UI packages together:

```bash
dotnet new install FS.GG.UI.Template
dotnet new fs-gg-ui -o MyApp
```

## Usage

```fsharp
open FS.GG.UI.Testing

// Describe what a generated product is expected to contain.
let expectation =
    { Profile = "default"
      RequiredFiles = [ "Program.fs"; "Directory.Packages.props" ]
      ForbiddenPrefixes = [ "Internal." ]
      PackageReferences =
        [ { PackageId = "FS.GG.UI"; Required = true } ] }

printfn "%s" (GeneratedProductAssertions.summarize expectation)

// Detect pinned-package drift against the expected local consumer feed.
let expected = [ { PackageId = "FS.GG.UI"; Version = "3.0.0"; FeedPath = "./feed" } ]
let actual = [ { PackageId = "FS.GG.UI"; Version = "2.9.0"; FeedPath = "./feed" } ]

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
- `LayoutReadiness` — validate Feature150-style layout readiness reports that aggregate public
  contract, ScrollViewer, intrinsic/cache, parity, compatibility, diagnostics, evidence links, deltas,
  and limitations.

## Layout Readiness

`LayoutReadiness.validate` is pure: pass a `LayoutReadinessReport` with discovered evidence and
compatibility deltas, and it returns an accepted/missing/blocked status plus diagnostics. It is meant
for package consumers and readiness scripts that need to distinguish accepted evidence from failed,
skipped, synthetic-only, compatibility-blocked, or missing layout evidence.

## Versioning

All `FS.GG.UI.*` libraries share one version and move together. In a generated project a
single `<FsSkiaUiVersion>` in `Directory.Packages.props` pins every package — upgrading is one
edit; see `docs/UPGRADING.md`. Pre-release versions use a `-preview.N` suffix.

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT

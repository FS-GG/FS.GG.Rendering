# FS.GG.UI.Testing

Generated product and package validation helpers for FS.GG.UI V3 products.

`FS.GG.UI.Testing` is one of the **FS.GG.UI** distribution packages — an F# / Elmish UI and 2D
scene-graph framework for .NET 10 desktop, rendered through OpenGL + SkiaSharp.

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

// Validate structured visual inspection evidence produced by Scene/Controls.
let validation =
    VisualInspectionValidation.validate
        inspectionArtifact
        VisualInspectionValidation.defaultRules
        []

printfn "%s" (VisualInspection.statusText validation.ReadinessStatus)
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
- `VisualCaptureMatrix` / `VisualCompleteness` / `VisualReviewerClassifications` /
  `VisualReadiness` / `VisualReadinessMarkdown` — shared visual-readiness helpers for
  screenshot matrices, PNG completeness, reviewer gates, contact-sheet metadata, reports,
  and managed summary sections.
- `VisualInspectionValidation` / `VisualInspectionReadiness` / `VisualInspectionMarkdown` —
  deterministic structured-inspection validation, intentional exception handling, readiness
  aggregation, machine-readable JSON, reviewer Markdown, and managed-section updates over
  `FS.GG.UI.Scene` inspection artifacts.
- `PersistentLaunchArtifactValidation` / `ReadinessFileDiscovery` — `validate` persisted launch artifacts
  and required readiness files.
- `LayoutReadiness` — validate Feature150-style layout readiness reports that aggregate public
  contract, ScrollViewer, intrinsic/cache, parity, compatibility, diagnostics, evidence links, deltas,
  and limitations.

## Visual Readiness

Samples and generated products can keep rendering and screenshot capture at their app edge while
using `FS.GG.UI.Testing` for generic visual-readiness evidence.

```fsharp
open FS.GG.UI.Testing

let pages =
    [ { PageId = "overview"; Title = "Overview"; Order = 0; Required = true } ]

let themes =
    [ { ThemeId = "light"; Title = "Light"; Order = 0 } ]

let sizes =
    [ { Role = "preferred"; Width = 1600; Height = 1000; Order = 0 } ]

let targets =
    VisualCaptureMatrix.expand pages themes sizes (fun page theme size ->
        $"{size.Role}/{theme.ThemeId}/{page.PageId}.png")

match targets with
| Ok targets ->
    let captures, staleDiagnostics = VisualCompleteness.validate "readiness/visual" targets
    let reviewerMarkdown = VisualReviewerClassifications.writeTemplate targets
    let reviewer = VisualReviewerClassifications.parse reviewerMarkdown targets
    let report =
        VisualReadiness.evaluate
            "sample-run"
            "readiness/visual"
            targets
            captures
            reviewer.Classifications
            []
            staleDiagnostics
            []

    printfn "%s" (VisualReadinessMarkdown.renderSummary report)
| Error diagnostics ->
    diagnostics |> List.iter (printfn "visual-readiness: %s")
```

Generated content in human-authored summaries is bounded by managed markers:

```md
<!-- FS.GG VISUAL READINESS START -->
generated visual-readiness content
<!-- FS.GG VISUAL READINESS END -->
```

`VisualReadinessMarkdown.updateManagedSection` inserts missing markers deterministically and
updates exactly one managed section. Multiple, reversed, or one-sided markers return
`SafeToWrite = false` and leave the original text unchanged.

## Structured Visual Inspection

Structured inspection complements screenshot evidence. `FS.GG.UI.Scene` defines the artifact
records, `FS.GG.UI.Controls` can emit them from `Control.renderTree`, and `FS.GG.UI.Testing`
validates rules such as required regions, text containment, clipping intent, paint coverage,
overlay exceptions, identity stability, visual-order stability, and unsupported required facts.

Generated content in inspection summaries is bounded by managed markers:

```md
<!-- FS.GG VISUAL INSPECTION START -->
generated visual-inspection content
<!-- FS.GG VISUAL INSPECTION END -->
```

Unsupported, not-inspected, not-run, and environment-limited scopes stay visible in summaries and
are not collapsed into accepted evidence.

## Layout Readiness

`LayoutReadiness.validate` is pure: pass a `LayoutReadinessReport` with discovered evidence and
compatibility deltas, and it returns an accepted/missing/blocked status plus diagnostics. It is meant
for package consumers and readiness scripts that need to distinguish accepted evidence from failed,
skipped, synthetic-only, compatibility-blocked, or missing layout evidence.

Feature151 uses the same helper shape for final P8 readiness. Required evidence files are validated
with `ReadinessFileDiscovery`, and the aggregate report links corpus, ScrollViewer, reuse, parity,
regression, compatibility, package, and limitation evidence without adding new Testing API.

## Versioning

All `FS.GG.UI.*` libraries share one version and move together. In a generated project a
single `<FsSkiaUiVersion>` in `Directory.Packages.props` pins every package — upgrading is one
edit; see `docs/UPGRADING.md`. Pre-release versions use a `-preview.N` suffix.

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT

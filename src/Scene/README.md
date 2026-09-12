# FS.GG.UI.Scene

Dependency-light scene vocabulary for FS.GG.UI V3 products.

`FS.GG.UI.Scene` is one of the **FS.GG.UI** distribution packages — an F# / Elmish UI and 2D
scene-graph framework for .NET 10 desktop, rendered through OpenGL + SkiaSharp.

## Install

```bash
dotnet add package FS.GG.UI.Scene
```

Or scaffold a full governed project that wires the FS.GG.UI packages together:

```bash
dotnet new install FS.GG.UI.Template
dotnet new fs-gg-ui -o MyApp
```

## Usage

```fsharp
open FS.GG.UI.Scene

// Build a small immutable scene from typed constructors.
let scene =
    Scene.group
        [ Scene.filledRectangle
            { X = 0.0; Y = 0.0; Width = 320.0; Height = 240.0 }
            (Colors.rgb 30uy 30uy 40uy)
          Scene.circle { X = 160.0; Y = 120.0 } 48.0 (Colors.rgb 200uy 60uy 60uy)
          Scene.line
            { X = 0.0; Y = 0.0 }
            { X = 320.0; Y = 240.0 }
            (Paint.stroke Colors.white 2.0)
          Scene.textAt { X = 12.0; Y = 24.0 } "Hello, Skia" Colors.white ]

// Inspect the element kinds without touching a GPU.
let kinds = Scene.describe scene

// Export a deterministic portable package and inspect it before any renderer runs.
let package = SceneCodec.export scene
let report = SceneCodec.inspect package.CanonicalBytes

// Produce deterministic render-readback evidence for a fixed output size.
let evidence =
    Scene.renderReadbackEvidence { Width = 320; Height = 240 } scene

// Build dependency-light structured inspection facts without depending on Controls or Testing.
let finding =
    VisualInspection.finding
        "text-contained-in-owner"
        VisualInspectionSeverity.Blocking
        [ "title" ]
        []
        "title text exceeds its owner"
        "text inside owner bounds"
        "overflow"

// Import only the inert supported SVG subset. Source ids become asset-local.
let imported =
    SvgImport.importXml
        { AssetNamespace = "icons/menu"; DocumentId = "menu-icon"; Limits = SvgDocument.defaultLimits }
        "<svg viewBox=\"0 0 16 16\"><path id=\"outline\" d=\"M 1 2 L 15 2 Z\"/></svg>"

// Group edits into one validated revision and one undo entry.
let committed =
    match imported with
    | Error issues -> Error(SvgAuthoringError.InvalidTransaction issues)
    | Ok document ->
      SvgAuthoring.tryCreate 0 document { Schema = SvgAsset.catalogSchema; Assets = [] } []
      |> Result.bind (fun state ->
        SvgAuthoring.commit
            0
            { Schema = SvgAuthoring.transactionSchema
              Id = "move"
              Operations =
                [ SvgAuthoringOperation.TransformElements(
                    [ "icons-menu--outline" ], SvgAffine.translate 2.0 0.0) ] }
            state)
```

## API at a glance

- `Scene` module — immutable scene constructors (`empty`, `group`, `filledRectangle`, `circle`, `filledEllipse`, `line`, `path`, `textAt`, `image`, `clipped`, `picture`, `chart`) plus inspection helpers (`describe`, `diagnostics`, `measureText`).
- `Colors` module — `Color` constructors and presets (`rgba`, `rgb`, `black`, `white`, `transparent`).
- `Paint` module — builds and refines `Paint` values: start from `fill`/`stroke`, then layer `withOpacity`, `withBlendMode`, `withShader`, and the filter/effect `with*` combinators.
- `Path` module — assembles `PathSpec` geometry (`moveTo`, `lineTo`, `quadTo`, `cubicTo`, `create`) and queries it (`bounds`, `measure`, `segment`, `combine`).
- `Scene` / `SceneNode` types — the core record and discriminated-union scene-graph vocabulary that every constructor produces.
- `SceneCodec` module — exports/imports deterministic portable scene packages, computes package
  identities, inspects protocol/capability/resource compatibility, and compares imported scenes.
- `SceneEvidence` module — renders a `SceneEvidenceRequest` to deterministic evidence, returning `Result` (`render`, `renderHash`, `renderPng`).
- `LayoutEvidence` module — derives and `classify`s `LayoutEvidenceReport` HUD/gameplay layout proofs from render-readback evidence.
- `VisualInspection` module and records — dependency-light structured inspection vocabulary for scopes, nodes, regions, text runs, paint coverage, clipping, unsupported facts, findings, artifacts, summaries, stable status tokens, finding ids, and deterministic artifact diagnostics.
- `SvgImport` — bounded parsing for the inert rectangle/path (`M/L/Q/C/Z`)/gradient/clip/mask/symbol SVG subset. It namespaces ids and rejects DTD/entity, script/event, `foreignObject`, CSS/filter, external URL, malformed, compressed, and over-budget input before returning a validated `SvgDocument`.
- `SvgAsset` and `SvgAssetCatalog` — versioned documents with canonical SHA-256, rights metadata, dependency validation, cycle refusal, accepted prefab revisions, typed overrides, and conflicts.
- `SvgAuthoring` — revision-guarded atomic transactions, grouped previews/cancellation, whole-group undo/redo, explicit shared-instance revision updates, and immutable play snapshots.

The curated Fable package includes `SvgDocument` and `SvgAuthoring`; it has no Game, Skia, native, browser-DOM, or Controls dependency. `fsgg.svg-document/1` remains the canonical typed document format.

## Versioning

All `FS.GG.UI.*` libraries share one version and move together. In a generated project a
single `<FsGgUiVersion>` in `Directory.Packages.props` pins every package — upgrading is one
edit; see `docs/UPGRADING.md`. Pre-release versions use a `-preview.N` suffix.

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT

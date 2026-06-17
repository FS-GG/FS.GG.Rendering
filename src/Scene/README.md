# FS.GG.UI.Scene

Dependency-light scene vocabulary for FS.GG.UI V3 products.

`FS.GG.UI.Scene` is one of the **FS.GG.UI** distribution packages — an F# / Elmish UI and 2D
scene-graph framework for .NET 10 desktop, rendered through Vulkan + SkiaSharp.

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

## Versioning

All `FS.GG.UI.*` libraries share one version and move together. In a generated project a
single `<FsSkiaUiVersion>` in `Directory.Packages.props` pins every package — upgrading is one
edit; see `docs/UPGRADING.md`. Pre-release versions use a `-preview.N` suffix.

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT

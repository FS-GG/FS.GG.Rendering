# FS.Skia.UI.Controls

Declarative Skia controls, rich rendering, chart controls, graph controls, DataGrid, and product-owned control runtime contracts.

`FS.Skia.UI.Controls` is one of the **FS.Skia.UI** distribution packages — an F# / Elmish UI and 2D
scene-graph framework for .NET 10 desktop, rendered through Vulkan + SkiaSharp.

## Install

```bash
dotnet add package FS.Skia.UI.Controls
```

Or scaffold a full governed project that wires the FS.Skia.UI packages together:

```bash
dotnet new install FS.Skia.UI.Template
dotnet new fs-skia-ui -o MyApp
```

## Usage

```fsharp
open FS.Skia.UI.Controls

type Msg =
    | Increment
    | RangeChanged of string

// Declare controls; each `create` takes an `Attr<'msg> list`.
let view =
    Stack.create
        [ Stack.children
            [ Label.create [ Label.text "Quarterly revenue" ]
              Button.create [ Button.text "Add"; Button.onClick Increment ]
              LineChart.create
                  [ LineChart.series
                      [ { Name = "EU"
                          Points = [ { X = 0.0; Y = 12.0; Label = None }
                                     { X = 1.0; Y = 18.0; Label = None } ] } ] ]
              GraphView.create [ GraphView.nodes [ "ingest"; "transform"; "emit" ] ] ] ]

// Render against a built-in theme to get a Scene + layout + diagnostics.
let result : ControlRenderResult<Msg> = Control.render Theme.dark view
```

## API at a glance

- `Control` — core lifecycle: `create` / `standard` / `customControl` build nodes, `render` produces a `ControlRenderResult` (scene, layout, diagnostics, event bindings), and `dispatch` / `diagnostics` inspect a `Control<'msg>`.
- Declarative control modules — `Button`, `Label`, `TextBlock`, `CheckBox`, `Slider`, `TextBox`, `Stack`, `Grid`, `Border`, `Tabs`, `Dialog`, and more, each with `create` plus content/event attributes like `text`, `onClick`, and `children`.
- `Charts` — `LineChart`, `BarChart`, `PieChart`, `ScatterPlot`, and `GraphView`, fed by `ChartSeries` / `ChartPoint` records.
- `DataGrid` — virtualized grid with an Elmish `init` / `update` over `DataGridModel`, `DataGridMsg`, and `DataGridEffect`, plus a declarative `create` using `DataGridColumn` / `DataGridRow`.
- `TextInput` — stateful editor (`init` / `update`) over `TextInputModel`, `TextInputMsg`, and `TextInputEffect`, covering selection, clipboard, composition, and validation.
- `Theme` — built-in `light` / `dark` palettes plus `withDensity`, `withAccent`, and `resolve` for the `Theme` record consumed by `Control.render`.
- `Attr` — low-level attribute builders (`text`, `value`, `children`, `theme`, `validation`, `on` / `onWith`) for composing `Attr<'msg>` values directly.
- `Catalog` — the governed control registry: `supportedControls`, `standardSchema`, and `validate` describe and check the standard control surface.

## Versioning

All `FS.Skia.UI.*` libraries share one version and move together. In a generated project a
single `<FsSkiaUiVersion>` in `Directory.Packages.props` pins every package — upgrading is one
edit; see `docs/UPGRADING.md`. Pre-release versions use a `-preview.N` suffix.

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT

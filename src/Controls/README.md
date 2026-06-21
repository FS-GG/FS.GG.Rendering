# FS.GG.UI.Controls

Declarative Skia controls, rich rendering, chart controls, graph controls, DataGrid, and product-owned control runtime contracts.

`FS.GG.UI.Controls` is one of the **FS.GG.UI** distribution packages — an F# / Elmish UI and 2D
scene-graph framework for .NET 10 desktop, rendered through OpenGL + SkiaSharp.

## Install

```bash
dotnet add package FS.GG.UI.Controls
```

Or scaffold a full governed project that wires the FS.GG.UI packages together:

```bash
dotnet new install FS.GG.UI.Template
dotnet new fs-gg-ui -o MyApp
```

## Usage

```fsharp
open FS.GG.UI.Controls

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

// Inspect the same renderTree path as structured layout/text/paint metadata.
let inspection =
    ControlInspection.inspect
        { Scope = { ScopeId = "dashboard"; Title = "Dashboard"; Required = true }
          Theme = Theme.dark
          OutputSize = { Width = 1280; Height = 800 }
          Control = view
          Presentation = "dark"
          RunId = Some "local-run"
          RelatedVisualEvidence = [] }
```

## API at a glance

- `Control` — core lifecycle: `create` / `standard` / `customControl` build nodes, `render` produces a `ControlRenderResult` (scene, layout, diagnostics, event bindings), and `dispatch` / `diagnostics` inspect a `Control<'msg>`. Rendering also clips every container's children to its bounds (no spill), paints `Overlay`-built transient surfaces last (z-top, escaping ancestor clips — `isOverlaySurface`), and makes `scroll-viewer` a real clipping viewport. `scrollViewport` reads back `ScrollViewport` geometry with content width/height, horizontal/vertical max offsets, extent source, and diagnostics derived from the Layout intrinsic protocol. See `docs/bridge/feature-137-render-blockers.md`.
- `ControlInspection` — additive structured inspection over `Control.renderTree`, emitting stable node ids, final bounds, ownership, visual order, text-fit facts, clip facts, paint coverage, surface roles, diagnostics, and explicit unsupported facts without changing rendered output or event bindings.
- Declarative control modules — `Button`, `Label`, `TextBlock`, `CheckBox`, `Slider`, `TextBox`, `Stack`, `Grid`, `Border`, `Tabs`, `Dialog`, and more, each with `create` plus content/event attributes like `text`, `onClick`, and `children`.
- `Charts` — `LineChart`, `BarChart`, `PieChart`, `ScatterPlot`, and `GraphView`, fed by `ChartSeries` / `ChartPoint` records.
- `DataGrid` — virtualized grid with an Elmish `init` / `update` over `DataGridModel`, `DataGridMsg`, and `DataGridEffect`, plus a declarative `create` using `DataGridColumn` / `DataGridRow`.
- `TextInput` — stateful editor (`init` / `update`) over `TextInputModel`, `TextInputMsg`, and `TextInputEffect`, covering selection, clipboard, composition, and validation.
- `Theme` — built-in `light` / `dark` palettes plus `withDensity`, `withAccent`, and `resolve` for the `Theme` record consumed by `Control.render`.
- `Attr` — low-level attribute builders (`text`, `value`, `children`, `theme`, `validation`, `on` / `onWith`) for composing `Attr<'msg>` values directly. Layout builders include `padding`, `margin`, `gap`, `alignItems`, `alignSelf`, `justifyContent`, `flexGrow`, `flexShrink`, `flexBasis`, `minWidth`, `minHeight`, `maxWidth`, and `maxHeight`; omitted padding/gap keep the Controls compatibility defaults, explicit zero overrides them, and the legacy `spacing` name is treated as a gap alias.
- `Catalog` — the governed control registry: `supportedControls`, `standardSchema`, and `validate` describe and check the standard control surface.

## Compositor diagnostics

Feature147 adds deterministic retained-render policy helpers for damage-union accounting, proof-gated fallback classification, promotion eligibility, placement movement damage, and snapshot-budget verdicts. Feature148 extends the readiness evidence around those helpers: localized/overlap/edge damage, movement old/new regions, content-vs-placement reuse, churn/no-benefit demotion, bounded snapshot eligibility, and timing thresholds are all recorded as reviewable evidence before a compositor tier can claim readiness. The public Controls package keeps the mechanics internal; consumers should inspect the derived `CompositorFrameDiagnostics` helper from `FS.GG.UI.Controls.Elmish` when reviewing proof readiness, damage area, fallback reason, reuse counters, demotions, and snapshot byte estimates.

## ScrollViewer Extent

Feature150 changes ScrollViewer extent readback to use `FS.GG.UI.Layout.Layout.contentExtent`.
Small content reports the viewport as the extent and zero max offsets. Overflowing content reports
the natural intrinsic extent while the viewport bounds remain fixed. `ContentHeight` and `Offset`
remain available as vertical compatibility aliases; new code should prefer
`ContentWidth`, `MaxHorizontalOffset`, `MaxVerticalOffset`, `ExtentSource`, and `Diagnostics`.

Feature151 broadens that acceptance to the full ScrollViewer corpus: empty, small, exact-fit,
overflowing, nested, clipped/layered parent, text-natural-size, dynamic content, and invalid fallback
cases. The accepted path remains `Layout.contentExtent`; no descendant-bounds walk or public
`ScrollViewport` shape change is introduced.

## Versioning

All `FS.GG.UI.*` libraries share one version and move together. In a generated project a
single `<FsSkiaUiVersion>` in `Directory.Packages.props` pins every package — upgrading is one
edit; see `docs/UPGRADING.md`. Pre-release versions use a `-preview.N` suffix.

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT

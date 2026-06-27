# FS.GG.UI.Layout

Pure layout and graph scene builders for FS.GG.UI.

`FS.GG.UI.Layout` is one of the **FS.GG.UI** distribution packages — an F# / Elmish UI and 2D
scene-graph framework for .NET 10 desktop, rendered through Vulkan + SkiaSharp.

## Install

```bash
dotnet add package FS.GG.UI.Layout
```

Or scaffold a full governed project that wires the FS.GG.UI packages together:

```bash
dotnet new install FS.GG.UI.Template
dotnet new fs-gg-ui -o MyApp
```

## Usage

```fsharp
open FS.GG.UI.Layout

// Stack two children into an 800x600 region.
let panel =
    Layout.verticalStack
        (Defaults.stackConfig 800.0 600.0)
        [ Defaults.child headerScene
          Defaults.child bodyScene ]

// Evaluate a node tree, then hit test the computed bounds.
let root = Defaults.layoutNode "root"
let result = Layout.evaluate (Defaults.availableSpace 800.0 600.0) root
let snap = Defaults.pixelSnapPolicy 1.0

match Layout.hitTestComputed snap result 120.0 64.0 with
| Some nodeId -> printfn "hit %s" nodeId
| None -> ()

// Build and probe a directed graph scene.
let graph =
    { Config = Defaults.graphConfig Directed 400.0 300.0
      Nodes = [ { Id = "a"; Label = "A"; Style = None }
                { Id = "b"; Label = "B"; Style = None } ]
      Edges = [ { Source = "a"; Target = "b"; Weight = None; Label = None } ] }

match Graph.layout graph with
| Ok placed -> Graph.hitTest placed 50.0 50.0 |> ignore
| Error issues -> issues |> List.iter (printfn "%A")
```

## API at a glance

- `Layout.evaluate` / `Layout.evaluateIncremental` — compute (or incrementally recompute) a `LayoutResult` for a `LayoutNode` tree against an `AvailableSpace`.
- `Layout.horizontalStack` / `Layout.verticalStack` / `Layout.dock` — build a `Scene` from `LayoutChild` lists using `StackConfig` / `DockConfig`; `measureHorizontal` / `measureVertical` return the child `LayoutBounds`.
- `Layout.renderComputed`, `Layout.snapBounds`, `Layout.hitTestComputed` — turn a computed result into a `Scene`, apply a `PixelSnapPolicy`, and resolve a point to a `LayoutNodeId`.
- `Layout.constraints`, `Layout.measureProtocol`, `Layout.intrinsicQuery`, `Layout.evaluateIntrinsic`, `Layout.cacheEntry`, and `Layout.contentExtent` — Feature150's explicit constraints-down, sizes-up and intrinsic-query contract. These records expose deterministic identities and dependency keys so containers can request natural size without inspecting rendered descendants.
- `Layout.initWorkflow` / `Layout.updateWorkflow` / `Layout.interpretWorkflowEffect` — Elmish-style `LayoutWorkflowModel` / `LayoutWorkflowMsg` / `LayoutWorkflowEffect` loop for host-resize and invalidation handling.
- `Graph.layout` / `Graph.directed` / `Graph.undirected` — lay out or render a `GraphDefinition`, returning a `Result` with `GraphValidationIssue` lists on failure; `Graph.hitTest` resolves a point to a `GraphTarget`.
- `GraphValidation.validate` / `hasCycle` / `disconnectedComponents` — inspect a `GraphDefinition` for duplicate ids, missing endpoints, self-loops, cycles, and connectivity.
- `Defaults` — constructors for the core records (`layoutNode`, `availableSpace`, `pixelSnapPolicy`, `stackConfig`, `dockConfig`, `graphConfig`, `child`, plus default `padding` / `layoutIntent` / `sizing`).

## Intrinsic Layout Protocol

Feature150 keeps Yoga as the default evaluator and adds explicit protocol records around it:
parents create `LayoutConstraints`, participants produce `MeasuredLayoutResult` values with child
placements, and containers that need natural sizes issue `IntrinsicQuery` values. `Layout.contentExtent`
is the reference helper for fixed viewports such as ScrollViewer; it returns content width/height,
max offsets, extent source, diagnostics, and dependency keys.

## P8 Layout Readiness

Feature151 accepts the P8 layout bar without adding a new public surface. The readiness package
under `specs/151-complete-p8-layout/readiness/` records the representative corpus, invalid and
fallback diagnostics, measured and intrinsic dependency identities, stale-rejection evidence, and
full/incremental parity. Consumers should treat the Feature150 protocol helpers as the stable public
entry points for P8 evidence.

## Versioning

All `FS.GG.UI.*` libraries share one version and move together. In a generated project a
single `<FsGgUiVersion>` in `Directory.Packages.props` pins every package — upgrading is one
edit; see `docs/UPGRADING.md`. Pre-release versions use a `-preview.N` suffix.

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT

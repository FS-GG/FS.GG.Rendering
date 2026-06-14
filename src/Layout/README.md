# FS.Skia.UI.Layout

Pure layout and graph scene builders for FS.Skia.UI.

`FS.Skia.UI.Layout` is one of the **FS.Skia.UI** distribution packages — an F# / Elmish UI and 2D
scene-graph framework for .NET 10 desktop, rendered through Vulkan + SkiaSharp.

## Install

```bash
dotnet add package FS.Skia.UI.Layout
```

Or scaffold a full governed project that wires the FS.Skia.UI packages together:

```bash
dotnet new install FS.Skia.UI.Template
dotnet new fs-skia-ui -o MyApp
```

## Usage

```fsharp
open FS.Skia.UI.Layout

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
- `Layout.initWorkflow` / `Layout.updateWorkflow` / `Layout.interpretWorkflowEffect` — Elmish-style `LayoutWorkflowModel` / `LayoutWorkflowMsg` / `LayoutWorkflowEffect` loop for host-resize and invalidation handling.
- `Graph.layout` / `Graph.directed` / `Graph.undirected` — lay out or render a `GraphDefinition`, returning a `Result` with `GraphValidationIssue` lists on failure; `Graph.hitTest` resolves a point to a `GraphTarget`.
- `GraphValidation.validate` / `hasCycle` / `disconnectedComponents` — inspect a `GraphDefinition` for duplicate ids, missing endpoints, self-loops, cycles, and connectivity.
- `Defaults` — constructors for the core records (`layoutNode`, `availableSpace`, `pixelSnapPolicy`, `stackConfig`, `dockConfig`, `graphConfig`, `child`, plus default `padding` / `layoutIntent` / `sizing`).

## Versioning

All `FS.Skia.UI.*` libraries share one version and move together. In a generated project a
single `<FsSkiaUiVersion>` in `Directory.Packages.props` pins every package — upgrading is one
edit; see `docs/UPGRADING.md`. Pre-release versions use a `-preview.N` suffix.

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT

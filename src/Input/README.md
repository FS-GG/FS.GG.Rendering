# FS.Skia.UI.Input

Host-coupled interactive input runtime for FS.Skia.UI: YAML key-binding configuration, modes, sequences, command intents, diagnostics, bigram analysis, and keyboard state-display projection over the SkiaViewer host.

`FS.Skia.UI.Input` is one of the **FS.Skia.UI** distribution packages — an F# / Elmish UI and 2D
scene-graph framework for .NET 10 desktop, rendered through Vulkan + SkiaSharp.

## Install

```bash
dotnet add package FS.Skia.UI.Input
```

Or scaffold a full governed project that wires the FS.Skia.UI packages together:

```bash
dotnet new install FS.Skia.UI.Template
dotnet new fs-skia-ui -o MyApp
```

## Usage

```fsharp
open FS.Skia.UI.Input

// Register the commands your YAML bindings can emit.
let commands : CommandDefinition list =
    [ { Id = "move.left"; DisplayName = "Move Left"; Category = Some "nav" }
      { Id = "move.right"; DisplayName = "Move Right"; Category = Some "nav" } ]

let buildRuntime (yaml: string) =
    result {
        let! registry = KeyboardInput.commandRegistry commands
        let! configuration = KeyboardInput.parseYaml yaml
        let! model = KeyboardInput.validate registry configuration
        let! runtime, _effects = KeyboardInput.init configuration.DefaultLayout model
        return runtime
    }

// Drive the runtime with input messages and read the resulting effects.
let step (runtime: InputRuntime) =
    let runtime', effects = KeyboardInput.update (KeyDown "KeyA") runtime
    let view : LayoutStateView = KeyboardInput.layoutState runtime'
    // Project an on-host keyboard state display scene.
    let scene = KeyboardInput.renderKeyboardStateDisplay KeyboardInput.defaultStateDisplayOptions effects runtime'
    runtime', view, scene
```

## API at a glance

- `KeyboardInput.commandRegistry` / `KeyboardInput.parseYaml` / `KeyboardInput.validate` — build a `CommandRegistry`, parse a YAML `InputConfiguration`, and combine them into a validated `CanonicalInputModel`, each returning `Result<_, InputDiagnostic list>`.
- `KeyboardInput.init` / `KeyboardInput.update` / `KeyboardInput.replay` — create an `InputRuntime` for a starting `LayoutId`, advance it with an `InputMsg` (`KeyDown`, `KeyUp`, `SetLayout`, `Cancel`, `Timeout`, `FocusLost`), and fold a batch of messages, each yielding `InputEffect list`.
- `KeyboardInput.viewerInputMsg` / `KeyboardInput.updateFromViewerEvent` — translate a SkiaViewer `ViewerEvent` into an `InputMsg` and step the runtime directly from host events.
- `KeyboardInput.layoutState` — project the current runtime into a `LayoutStateView` (active mode stack, held modes, pending sequence, active layout and labels).
- `KeyboardInput.keyboardStateDisplay` plus `renderKeyboardStateDisplay` / `renderKeyboardStateDisplayAt` — build a `KeyboardStateDisplayModel` and render it to a `Scene`, configured via `defaultStateDisplayOptions`, `compactStateDisplayOptions`, or `expandedStateDisplayOptions`.
- `KeyboardInput.renderLayoutState` / `renderLayoutStateAt` — render a layout-state `Scene` for the host, optionally at a given position.
- `KeyboardInput.analyzeBigrams` — produce a `BigramReport` (top pairs, `BigramRisk`s, `BigramSuggestion`s) for a layout from a `CanonicalInputModel`.

## Versioning

All `FS.Skia.UI.*` libraries share one version and move together. In a generated project a
single `<FsSkiaUiVersion>` in `Directory.Packages.props` pins every package — upgrading is one
edit; see `docs/UPGRADING.md`. Pre-release versions use a `-preview.N` suffix.

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT

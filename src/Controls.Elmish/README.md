# FS.GG.UI.Controls.Elmish

Elmish command, subscription, and program adapters for Controls and KeyboardInput runtime effects.

`FS.GG.UI.Controls.Elmish` is one of the **FS.GG.UI** distribution packages — an F# / Elmish UI and 2D
scene-graph framework for .NET 10 desktop, rendered through Vulkan + SkiaSharp.

## Install

```bash
dotnet add package FS.GG.UI.Controls.Elmish
```

Or scaffold a full governed project that wires the FS.GG.UI packages together:

```bash
dotnet new install FS.GG.UI.Template
dotnet new fs-gg-ui -o MyApp
```

## Usage

```fsharp
open FS.GG.UI.Controls
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Controls.Elmish

// Lift a fired keyboard command into your own message type.
type Msg =
    | Activate of CommandId
    | Runtime of ControlRuntimeMsg

let init () : Model * AdapterCommand<Msg> = initialModel, []

let update (msg: Msg) (model: Model) : Model * AdapterCommand<Msg> =
    match msg with
    | Activate cmd -> model, [ ReportAdapterDiagnostic(ControlsElmish.diagnostic "update" "activated" (string cmd)) ]
    | Runtime _ -> model, []

let subscribe (_: Model) =
    // Interpret keyboard effects into adapter commands, then expose them as subscriptions.
    let keyboardSubs = [ { Id = "keyboard"; Subscribe = fun () -> ControlsElmish.interpretKeyboardEffect Activate keyboardEffect } ]
    ControlsElmish.subscriptions keyboardSubs []

// Build the adapter program that drives Controls + keyboard runtime effects.
let app : AdapterProgram<Model, Msg> =
    ControlsElmish.program init update view subscribe
```

## Versioning

All `FS.GG.UI.*` libraries share one version and move together. In a generated project a
single `<FsSkiaUiVersion>` in `Directory.Packages.props` pins every package — upgrading is one
edit; see `docs/UPGRADING.md`. Pre-release versions use a `-preview.N` suffix.

## API at a glance

- `ControlsElmish.program` — assembles an `AdapterProgram<'model,'msg>` from `init`, `update`, `view`, and `subscriptions` functions, the entry point for wiring an Elmish loop.
- `ControlsElmish.interpretKeyboardEffect` — turns a `KeyboardEffect` into an `AdapterCommand<'msg>`, mapping each fired `CommandId` through your `mapCommand` function.
- `ControlsElmish.interpretControlEffect` — turns a `ControlRuntimeEffect` into an `AdapterCommand<'msg>`, mapping `ControlRuntimeMsg` values through your `mapRuntime` function.
- `ControlsElmish.subscriptions` — merges keyboard and control `AdapterSubscription<'msg>` lists into one subscription list for the program.
- `ControlsElmish.diagnostic` — builds an `AdapterDiagnostic` (source, code, message) for reporting adapter-level issues.
- `AdapterEffect<'msg>` — the effect union (`DispatchProductMessage`, `DispatchControlRuntimeMessage`, `DispatchKeyboardMessage`, `DispatchHostCommand`, `ReportAdapterDiagnostic`); an `AdapterCommand<'msg>` is a list of these.
- `AdapterProgram<'model,'msg>` / `AdapterSubscription<'msg>` — the program and subscription records that carry `Init`/`Update`/`View`/`Subscriptions` and `Id`/`Subscribe` respectively.

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT

# FS.GG.UI.Elmish

Elmish adapter contracts for FS.GG.UI V3 products.

`FS.GG.UI.Elmish` is one of the **FS.GG.UI** distribution packages — an F# / Elmish UI and 2D
scene-graph framework for .NET 10 desktop, rendered through Vulkan + SkiaSharp.

> **Which adapter?** `FS.GG.UI.Elmish` is the **pure scene adapter** (`ElmishAdapter`): your
> `render` projects the model to a `SceneNode`, and `init`/`update` stay pure, returning the
> next model plus effect *values* interpreted at the host boundary (it bridges viewer messages
> and effects into Elmish envelopes over `Viewer.runInteractiveViewer`). For a product built on
> the semantic control set — buttons, text boxes, grids — use the sibling package
> **`FS.GG.UI.Controls.Elmish`** (`runInteractiveApp`), which every FS.GG.UI sample uses. Both
> are supported; pick by whether your view produces a `SceneNode` or a `Control<'msg>` tree.

## Install

```bash
dotnet add package FS.GG.UI.Elmish
```

Or scaffold a full governed project that wires the FS.GG.UI packages together:

```bash
dotnet new install FS.GG.UI.Template
dotnet new fs-gg-ui -o MyApp
```

## Usage

The adapter wraps your own Elmish `model`/`msg` together with the viewer. It is a **viewer
bridge**, not your MVU runtime: a `ViewerMsg` advances the viewer and rebuilds the scene from
the current user model, while a `UserMsg` is a pass-through — it forwards your message as a
`DispatchUser` effect and leaves the model and scene unchanged. You supply a `render` function
that projects your model to a `SceneNode`; the adapter re-renders it on a `ViewerMsg`, and your
product composes its own `update` around the adapter (interpret `DispatchUser` by running your
`update`, then reflect the next user model back so the following `ViewerMsg` re-renders it).

```fsharp
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Elmish

// Your own Elmish model/msg — and your own pure update (the adapter never folds this).
type Model = { Count: int }
type Msg = Increment

let update (msg: Msg) (model: Model) : Model =
    match msg with
    | Increment -> { model with Count = model.Count + 1 }

// Project the user model into a scene
let render (model: Model) : SceneNode =
    Text((20.0, 40.0), $"Count: {model.Count}", Colors.black)

let options = { Title = "Counter"; InitialSize = { Width = 640; Height = 480 } }

// Initialise the combined adapter model + initial effects
let initial, startupEffects =
    ElmishAdapter.init options { Count = 0 } (render { Count = 0 })

// Pass a user message through the adapter. `UserMsg` is a pass-through: it yields a
// `DispatchUser` effect and leaves `passthrough.UserModel`/scene unchanged (the scene is
// rebuilt via `render` only on a `ViewerMsg`).
let passthrough, effects =
    ElmishAdapter.update render (UserMsg Increment) initial

// Compose YOUR update around the adapter: interpret the `DispatchUser` effect by folding
// your own `update`, then reflect the next user model back so the following `ViewerMsg`
// re-renders the scene from it.
let folded =
    match effects with
    | [ DispatchUser m ] ->
        let userModel' = update m passthrough.UserModel
        { passthrough with UserModel = userModel'; Scene = render userModel' }
    | _ -> passthrough
```

## API at a glance

- `ElmishAdapter.init` — builds the combined `ElmishAdapterModel<'model>` from `ViewerOptions`, your initial user model, and an initial `SceneNode`, returning the model and its startup effects.
- `ElmishAdapter.update` — advances the adapter on an `ElmishAdapterMsg<'msg>`: a `ViewerMsg` steps the viewer and refreshes the scene via `render`, while a `UserMsg` is forwarded verbatim as a `DispatchUser` effect — your user model is **not** folded here, so compose your own `update` around the adapter. Yields the next model plus effects.
- `ElmishAdapterModel<'model>` — the bridged state record holding your `UserModel`, the current `Scene` (a `SceneNode`), and the `ViewerModel`.
- `ElmishAdapterMsg<'msg>` — message envelope: `UserMsg` carries your own messages, `ViewerMsg` carries viewer messages.
- `ElmishAdapterEffect<'msg>` — effect envelope: `DispatchUser` for your messages and `DispatchViewer` for `ViewerEffect`s.

## Versioning

All `FS.GG.UI.*` libraries share one version and move together. In a generated project a
single `<FsGgUiVersion>` in `Directory.Packages.props` pins every package — upgrading is one
edit; see `docs/UPGRADING.md`. Pre-release versions use a `-preview.N` suffix.

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT

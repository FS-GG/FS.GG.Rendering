# FS.Skia.UI.Elmish

Elmish adapter contracts for FS.Skia.UI V3 products.

`FS.Skia.UI.Elmish` is one of the **FS.Skia.UI** distribution packages — an F# / Elmish UI and 2D
scene-graph framework for .NET 10 desktop, rendered through Vulkan + SkiaSharp.

## Install

```bash
dotnet add package FS.Skia.UI.Elmish
```

Or scaffold a full governed project that wires the FS.Skia.UI packages together:

```bash
dotnet new install FS.Skia.UI.Template
dotnet new fs-skia-ui -o MyApp
```

## Usage

The adapter wraps your own Elmish `model`/`msg` together with the viewer, so a single
`update` drives both your state and the rendered scene. You supply a `render` function that
projects your model to a `SceneNode`; the adapter rebuilds the scene on every message.

```fsharp
open FS.Skia.UI.Scene
open FS.Skia.UI.SkiaViewer
open FS.Skia.UI.Elmish

// Your own Elmish model/msg
type Model = { Count: int }
type Msg = Increment

// Project the user model into a scene
let render (model: Model) : SceneNode =
    Text((20.0, 40.0), $"Count: {model.Count}", Colors.black)

let options = { Title = "Counter"; InitialSize = { Width = 640; Height = 480 } }

// Initialise the combined adapter model + initial effects
let initial, effects =
    ElmishAdapter.init options { Count = 0 } (render { Count = 0 })

// Fold a user message through the adapter (re-renders via `render`)
let next, _ =
    ElmishAdapter.update render (UserMsg Increment) initial
```

## API at a glance

- `ElmishAdapter.init` — builds the combined `ElmishAdapterModel<'model>` from `ViewerOptions`, your initial user model, and an initial `SceneNode`, returning the model and its startup effects.
- `ElmishAdapter.update` — folds an `ElmishAdapterMsg<'msg>` into the adapter model, using the supplied `render` function to refresh the scene, and yields the next model plus effects.
- `ElmishAdapterModel<'model>` — the bridged state record holding your `UserModel`, the current `Scene` (a `SceneNode`), and the `ViewerModel`.
- `ElmishAdapterMsg<'msg>` — message envelope: `UserMsg` carries your own messages, `ViewerMsg` carries viewer messages.
- `ElmishAdapterEffect<'msg>` — effect envelope: `DispatchUser` for your messages and `DispatchViewer` for `ViewerEffect`s.

## Versioning

All `FS.Skia.UI.*` libraries share one version and move together. In a generated project a
single `<FsSkiaUiVersion>` in `Directory.Packages.props` pins every package — upgrading is one
edit; see `docs/UPGRADING.md`. Pre-release versions use a `-preview.N` suffix.

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT

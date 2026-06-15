# Elmish Fragment

Adds Elmish adapter package references and generated product Elmish guidance.

Generated products that select Elmish and Controls should reference
`FS.GG.UI.Controls.Elmish` for command, subscription, and program adapter
wiring. Base Controls views remain generic over product messages and return
`Control<'msg>`.

Use `AdapterCommand<'msg>` for commands and `AdapterSubscription<'msg>` for
subscriptions in reusable guidance; generated examples may replace `'msg` with
the product `Msg` type.

```fsharp
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish

let init () : Model * AdapterCommand<Msg> =
    initialModel, []

let update msg model : Model * AdapterCommand<Msg> =
    model, []

let view model : Control<Msg> =
    controlsExampleView model

let subscriptions model : AdapterSubscription<Msg> list =
    ControlsElmish.subscriptions [] []

let program =
    ControlsElmish.program init update view subscriptions
```

Map keyboard and control runtime effects at the product edge:

```fsharp
let keyboardCommands =
    ControlsElmish.interpretKeyboardEffect KeyboardCommandResolved keyboardEffect

let controlCommands =
    ControlsElmish.interpretControlEffect ControlRuntimeMsg controlEffect
```

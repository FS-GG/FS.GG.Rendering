#I "../src/Controls/bin/Debug/net10.0"
#I "../src/Controls.Elmish/bin/Debug/net10.0"
#r "FS.GG.UI.Controls.dll"
#r "FS.GG.UI.Controls.Elmish.dll"

open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish

type Msg =
    | TextChanged of string

let textbox =
    TextBox.create [ TextBox.value "ready"; TextBox.onChanged TextChanged ]

type Model =
    { Value: string }

let init () : Model * AdapterCommand<Msg> =
    { Value = "ready" }, []

let update msg model : Model * AdapterCommand<Msg> =
    match msg with
    | TextChanged value -> { model with Value = value }, []

let view model =
    TextBox.create [ TextBox.value model.Value; TextBox.onChanged TextChanged ]

let subscriptions _ =
    []

let program =
    ControlsElmish.program init update view subscriptions

printfn "elmish control: %A %A" textbox (program.View { Value = "ready" })

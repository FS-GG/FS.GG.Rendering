# KeyboardInput Fragment

Adds keyboard input package references and reducer guidance.

Use `FS.GG.UI.KeyboardInput` for product-owned keyboard runtime state,
messages, pure updates, and emitted effects. Generated Controls screens should
consume keyboard state through Controls or the
`FS.GG.UI.Controls.Elmish` adapter when Elmish program integration is
selected.

Keep chart controls, graph controls, DataGrid, and rich text guidance in the
Controls fragment.

```fsharp
open FS.GG.UI.KeyboardInput

let bindings = [ { Key = "ArrowLeft"; Command = "move-left" }
                 { Key = "Space"; Command = "primary-action" } ]

let model, startupEffects = Keyboard.init bindings

let mapKey (key: ViewerKey) (isDown: bool) : Msg option =
    match key, isDown with
    | ArrowLeft, true -> Some MoveLeft
    | Space, true -> Some PrimaryAction
    | _ -> None
```

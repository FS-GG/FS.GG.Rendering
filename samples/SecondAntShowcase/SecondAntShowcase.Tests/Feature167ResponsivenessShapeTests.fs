module SecondAntShowcase.Tests.Feature167ResponsivenessShapeTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open SecondAntShowcase.Core

[<Tests>]
let tests =
    testList "Feature167 SecondAntShowcase responsiveness shape" [
        test "representative script includes pointer activation, keyboard activation, and no-op input" {
            let script = Scripts.representative "buttons"

            let hasPointerClick =
                script
                |> List.exists (function
                    | FrameInput.Pointer(Click _) -> true
                    | _ -> false)

            let hasEnter =
                script |> List.exists ((=) (FrameInput.Key(Enter, ViewerKeyboard.noModifiers)))

            let hasSpace =
                script |> List.exists ((=) (FrameInput.Key(Space, ViewerKeyboard.noModifiers)))

            let hasEscape =
                script |> List.exists ((=) (FrameInput.Key(Escape, ViewerKeyboard.noModifiers)))

            Expect.isTrue hasPointerClick "script includes pointer activation"
            Expect.isTrue hasEnter "script includes Enter activation"
            Expect.isTrue hasSpace "script includes Space activation"
            Expect.isTrue hasEscape "script includes no-visible-response key"
        }

        test "representative deterministic substitute changes product state through keyboard activation" {
            let metrics = ControlsElmish.Perf.runScript Host.defaultHost SecondAntShowcase.Tests.Feature167ResponsivenessFixtures.size (Scripts.representative "buttons")

            Expect.isTrue (metrics |> List.exists _.ProductModelChanged) "keyboard activation changes product state"
        }
    ]

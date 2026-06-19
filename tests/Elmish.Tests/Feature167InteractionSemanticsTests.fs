module Feature167InteractionSemanticsTests

open Expecto
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish

[<Tests>]
let tests =
    testList "Feature167 interaction semantics" [
        test "continuous pointer movement is deterministic and coalesced" {
            let script =
                [ for i in 0 .. 4 ->
                      FrameInput.Pointer(HoverEnter("btn", float i, float i)) ]

            let frames = Feature167ResponsivenessFixtures.run script

            Expect.equal frames.Length 1 "move burst is one frame"
            Expect.equal frames.[0].PointerSamplesReceived 5 "raw samples are counted"
            Expect.equal frames.[0].PointerMovesProcessed 1 "one move is processed"
        }

        test "Perf.runScript remains clock-free and repeatable" {
            let script = [ FrameInput.Key(Enter, ViewerKeyboard.noModifiers); FrameInput.Key(Escape, ViewerKeyboard.noModifiers) ]
            let a = Feature167ResponsivenessFixtures.run script |> List.map Feature167ResponsivenessFixtures.deterministicShape
            let b = Feature167ResponsivenessFixtures.run script |> List.map Feature167ResponsivenessFixtures.deterministicShape

            Expect.equal a b "deterministic frame metrics shape is stable"
        }
    ]

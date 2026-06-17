module Feature142ControlsTextShapingTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private font: FontSpec = { Family = Some "Inter"; Size = 18.0; Weight = None }

[<Tests>]
let tests =
    testList "Feature142 Controls text shaping seam" [
        test "control text measurement resolves through the Scene text metrics seam" {
            let measured = ControlInternals.measureText "control text" font
            let expected = Scene.measureTextResolved "control text" font

            Expect.equal measured expected "Controls consume the Scene measurement seam"
        }
    ]

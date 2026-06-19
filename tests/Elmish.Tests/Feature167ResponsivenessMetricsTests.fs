module Feature167ResponsivenessMetricsTests

open Expecto
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Controls.Elmish

[<Tests>]
let tests =
    testList "Feature167 responsiveness metrics" [
        test "key activation contributes product/update timing facts" {
            let frame =
                Feature167ResponsivenessFixtures.run [ FrameInput.Key(Enter, ViewerKeyboard.noModifiers) ]
                |> List.exactlyOne

            let contribution = ControlsElmish.responsivenessTimingContribution frame

            Expect.isTrue contribution.ProductModelChanged "Enter changed the product model"
            Expect.equal contribution.ProductMessageCount 1 "one product message contribution"
            Expect.isNone contribution.NoVisibleResponseReason "visible-affecting frame has no no-response reason"
        }

        test "no-op key records an explicit no-visible-response reason" {
            let frame =
                Feature167ResponsivenessFixtures.run [ FrameInput.Key(Escape, ViewerKeyboard.noModifiers) ]
                |> List.exactlyOne

            let contribution = ControlsElmish.responsivenessTimingContribution frame

            Expect.isFalse contribution.ProductModelChanged "Escape does not change the model"
            Expect.isSome contribution.NoVisibleResponseReason "no-visible-response is explicit"
        }
    ]

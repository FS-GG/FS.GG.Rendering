module Feature174ResponsivenessRegressionTests

open System
open Expecto
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Controls.Elmish

[<Tests>]
let tests =
    testList "Feature174 responsiveness regression coverage" [
        test "deterministic required scenarios cover visible and no-visible responses" {
            let visible =
                Feature167ResponsivenessFixtures.run [ FrameInput.Key(Enter, ViewerKeyboard.noModifiers) ]
                |> List.exactlyOne
                |> ControlsElmish.responsivenessTimingContribution

            let noVisible =
                Feature167ResponsivenessFixtures.run [ FrameInput.Key(Escape, ViewerKeyboard.noModifiers) ]
                |> List.exactlyOne
                |> ControlsElmish.responsivenessTimingContribution

            Expect.isTrue visible.ProductModelChanged "button-click-equivalent key changes the product model"
            Expect.isNone visible.NoVisibleResponseReason "visible scenario has no no-response reason"
            Expect.isFalse noVisible.ProductModelChanged "no-op scenario does not change the product model"
            Expect.isSome noVisible.NoVisibleResponseReason "no-visible-response scenario is explicit"
        }

        test "Perf.runScript timing contribution names retained phases without live timing claims" {
            let frame =
                Feature167ResponsivenessFixtures.run [ FrameInput.Key(Enter, ViewerKeyboard.noModifiers) ]
                |> List.exactlyOne
            let contribution = ControlsElmish.responsivenessTimingContribution frame

            Expect.equal frame.FrameDuration TimeSpan.Zero "deterministic path remains clock-free"
            Expect.equal contribution.RoutingDuration TimeSpan.Zero "routing phase is named"
            Expect.equal contribution.UpdateDuration TimeSpan.Zero "update phase is named"
            Expect.equal contribution.RetainedStepDuration TimeSpan.Zero "retained-step phase is named"
            Expect.equal contribution.LayoutDuration TimeSpan.Zero "layout phase is named"
            Expect.equal contribution.TextDuration TimeSpan.Zero "text phase is named"
        }
    ]


module SecondAntShowcase.Tests.Feature172SliderRegressionTests

open Expecto
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model

[<Tests>]
let tests =
    testList "Feature172 slider and value-changing regressions" [
        test "slider and rating messages still change product state" {
            let m = Host.initModel

            Expect.equal (Model.update (PageMsg(SliderChanged 0.75)) m).PageState.SliderValue 0.75 "slider value changes"
            Expect.equal (Model.update (PageMsg(RateChanged 4.0)) m).PageState.RateValue 4.0 "rating value changes"
            Expect.equal m.PageState.ProgressValue DemoState.seed.ProgressValue "progress seeded display value is preserved"
        }

        test "slider-rating contract remains a value-changing pointer action" {
            let contract =
                InteractionContracts.all
                |> List.find (fun contract -> contract.ContractId = "slider-rating")

            Expect.equal contract.ActionType "drag" "slider-rating uses drag action"
            Expect.equal contract.InputKind "pointer-move" "slider-rating uses pointer movement"
            Expect.contains contract.ControlIds "slider" "slider is covered"
            Expect.contains contract.ControlIds "rate" "rating is covered"
            Expect.contains contract.ControlIds "progress-bar" "progress is covered"
        }
    ]

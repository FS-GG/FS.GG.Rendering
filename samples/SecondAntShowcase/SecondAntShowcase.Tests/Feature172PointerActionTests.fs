module SecondAntShowcase.Tests.Feature172PointerActionTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open SecondAntShowcase.Core

[<Tests>]
let tests =
    testList "Feature172 representative pointer actions" [
        test "every interaction contract declares action type and input kind" {
            for contract in InteractionContracts.all do
                Expect.isNonEmpty contract.ActionType (sprintf "%s has action type" contract.ContractId)
                Expect.isNonEmpty contract.InputKind (sprintf "%s has input kind" contract.ContractId)
                Expect.isNonEmpty contract.VisibleEvidence (sprintf "%s has expected visible result" contract.ContractId)
        }

        test "all-interactive script is derived from interaction contracts" {
            let script = Scripts.representativeAllInteractive ()

            let actionCount =
                script
                |> List.filter (function
                    | FrameInput.Pointer _
                    | FrameInput.Key _ -> true
                    | _ -> false)
                |> List.length

            Expect.isGreaterThanOrEqual actionCount InteractionContracts.all.Length "each family contributes pointer or activation coverage"
            Expect.isTrue (script |> List.exists ((=) FrameInput.Idle)) "script has a terminal idle frame"
        }

        test "value-changing families are represented by drag or value-change metadata" {
            let valueFamilies =
                InteractionContracts.all
                |> List.filter (fun contract -> contract.ControlIds |> List.exists (fun id -> id = "slider" || id = "rate" || id = "progress-bar"))

            Expect.isNonEmpty valueFamilies "slider/rating family exists"
            Expect.isTrue (valueFamilies |> List.forall (fun contract -> contract.ActionType = "drag" || contract.ActionType = "value-change")) "value families are not plain display actions"
        }
    ]

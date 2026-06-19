module SecondAntShowcase.Tests.Feature172CoverageRegressionTests

open Expecto
open SecondAntShowcase.Core

[<Tests>]
let tests =
    testList "Feature172 coverage regression preservation" [
        test "mapped-control coverage remains clean" {
            let result = CoverageMap.check ()

            Expect.isEmpty result.Unreferenced "no catalog controls are unreferenced"
            Expect.isEmpty result.Duplicated "no catalog controls are duplicated"
            Expect.isTrue (CoverageMap.isClean result) "coverage map stays clean"
        }

        test "interactive-family coverage and display-only exclusions remain explicit" {
            let coverage = InteractionContracts.coverage ()

            Expect.isEmpty coverage.MissingContractOrReason "every catalog control has a contract or display-only reason"
            Expect.isTrue (InteractionContracts.isClean coverage) "interaction coverage stays clean"
            Expect.equal (InteractionContracts.all |> List.map _.ContractId |> List.distinct |> List.length) InteractionContracts.all.Length "contract ids are unique"
            Expect.isGreaterThan coverage.DisplayOnlyControls.Length 0 "display-only exclusions are visible"
        }

        test "display-only exclusions do not overlap interactive controls" {
            let interactive = InteractionContracts.all |> List.collect _.ControlIds |> Set.ofList
            let displayOnly = InteractionContracts.displayOnlyReasons |> Map.toList |> List.map fst |> Set.ofList

            Expect.isEmpty (Set.intersect interactive displayOnly) "a control is not both interactive and display-only"
        }
    ]

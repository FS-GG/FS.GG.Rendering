module SecondAntShowcase.Tests.Feature174VisualParityTests

open Expecto
open FS.GG.UI.Controls
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model

[<Tests>]
let tests =
    testList "Feature174 visual parity coverage" [
        test "button and dense input pages remain in minimum visual readiness coverage" {
            Expect.contains VisualConfig.minimumRepresentativePageIds "buttons" "button page covered"
            Expect.contains VisualConfig.minimumRepresentativePageIds "text-numeric-input" "dense input page covered"
        }

        test "button and dense input pages are catalog pages with executable views" {
            let buttonPage: Page = PageRegistry.byId "buttons"
            let inputPage: Page = PageRegistry.byId "text-numeric-input"

            Expect.equal buttonPage.Kind Catalog "buttons page kind"
            Expect.equal inputPage.Kind Catalog "text-numeric-input page kind"
            Expect.isGreaterThan (Control.count (buttonPage.View DemoState.seed)) 0 "button page builds a control tree"
            Expect.isGreaterThan (Control.count (inputPage.View DemoState.seed)) 0 "input page builds a control tree"
        }

        test "page-change scenario target is reachable in registry order" {
            let ids = PageRegistry.all |> List.map _.Id
            let buttonIndex = ids |> List.findIndex ((=) "buttons")
            let inputIndex = ids |> List.findIndex ((=) "text-numeric-input")

            Expect.isGreaterThanOrEqual buttonIndex 0 "buttons is present"
            Expect.isGreaterThan inputIndex buttonIndex "text-numeric-input follows buttons in nav order"
        }
    ]

module SecondAntShowcase.Tests.InteractionTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls.Elmish
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model

let private size: Size = { Width = 1024; Height = 768 }

/// FR-014 / interaction-contract.md: a seeded input produces the documented visible state
/// change for one representative control of each interactive family; display-only controls
/// are exempt (navigation never alters interactive state). All transitions are pure.
[<Tests>]
let interactionTests =
    testList "Interaction" [
        test "a seeded keyboard activation changes the product model (responds-proof)" {
            let metrics = ControlsElmish.Perf.runScript Host.defaultHost size (Scripts.forPage "buttons")
            Expect.isTrue
                (metrics |> List.exists (fun m -> m.ProductModelChanged))
                "at least one frame reports ProductModelChanged"
        }

        test "buttons: activation increments the visible counter" {
            let m = Model.update (PageMsg ButtonClicked) Host.initModel
            Expect.equal m.PageState.ButtonClicks 1 "button activation recorded"
        }

        test "each interactive family maps its input to its own field transition" {
            let m = Host.initModel
            // toggles
            Expect.equal (Model.update (PageMsg(SwitchChanged true)) m).PageState.SwitchOn true "switch"
            Expect.equal (Model.update (PageMsg(CheckChanged false)) m).PageState.Checked false "check-box"
            // selection
            Expect.equal (Model.update (PageMsg(SegmentedChanged "List")) m).PageState.SegmentedSelected "List" "segmented"
            Expect.equal (Model.update (PageMsg(ComboChanged "Product")) m).PageState.ComboSelected "Product" "combo"
            Expect.equal (Model.update (PageMsg(CascaderChanged "Europe / France / Paris")) m).PageState.CascaderSelected "Europe / France / Paris" "cascader"
            // text / numeric
            Expect.equal (Model.update (PageMsg(TextChanged "abc")) m).PageState.TextValue "abc" "text"
            Expect.equal (Model.update (PageMsg(SliderChanged 0.25)) m).PageState.SliderValue 0.25 "slider"
            Expect.equal (Model.update (PageMsg(RateChanged 3.0)) m).PageState.RateValue 3.0 "rate"
            // navigation
            Expect.equal (Model.update (PageMsg(StepChanged 2)) m).PageState.StepsCurrent 2 "steps"
            Expect.equal (Model.update (PageMsg(PageChanged 4)) m).PageState.PaginationPage 4 "pagination"
            Expect.equal (Model.update (PageMsg(TabChanged "Activity")) m).PageState.Tab "Activity" "tabs"
            // disclosure
            Expect.equal (Model.update (PageMsg(CollapseToggled "Security")) m).PageState.CollapseOpen "Security" "collapse"
        }

        test "the host key map routes activation keys to a button click" {
            Expect.equal (Host.mapKey FS.GG.UI.KeyboardInput.Enter true) (Some(PageMsg ButtonClicked)) "Enter activates"
            Expect.equal (Host.mapKey FS.GG.UI.KeyboardInput.Space true) (Some(PageMsg ButtonClicked)) "Space activates"
            Expect.equal (Host.mapKey FS.GG.UI.KeyboardInput.Enter false) None "key-up ignored"
        }

        test "display-only controls are exempt: navigation never alters interactive state" {
            let m0 = { Host.initModel with PageState = { DemoState.seed with ButtonClicks = 3 } }
            let m1 = Model.update (NavigateTo "display-typography") m0
            Expect.equal m1.PageState.ButtonClicks 3 "navigating to a display-only page changes no interactive state"
            Expect.equal m1.CurrentPage "display-typography" "page navigation applied"
        }

        test "every catalog control has either an interaction contract or a display-only reason" {
            let coverage = InteractionContracts.coverage ()
            Expect.isEmpty coverage.MissingContractOrReason "all catalog controls are classified"
            Expect.isTrue (InteractionContracts.isClean coverage) "interaction contract coverage is clean"
            Expect.isNonEmpty coverage.ContractedControls "interactive controls are represented"
            Expect.isNonEmpty coverage.DisplayOnlyControls "display-only controls carry reasons"
        }

        test "interaction contracts are theme-invariant and point at known pages" {
            let pageIds = PageRegistry.all |> List.map _.Id |> Set.ofList
            for contract in InteractionContracts.all do
                Expect.isTrue contract.ThemeInvariant (sprintf "%s is theme invariant" contract.ContractId)
                Expect.isTrue (pageIds.Contains contract.PageId) (sprintf "%s references a known page" contract.ContractId)
                Expect.isNonEmpty contract.VisibleEvidence (sprintf "%s has visible evidence" contract.ContractId)
        }
    ]

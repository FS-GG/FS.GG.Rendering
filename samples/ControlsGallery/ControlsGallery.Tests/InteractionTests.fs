module ControlsGallery.Tests.InteractionTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls.Elmish
open ControlsGallery.Core
open ControlsGallery.Core.Model

let private size: Size = { Width = 1024; Height = 768 }

/// FR-012: a seeded script targeting an interactive control produces a visible state
/// change; display-only state is untouched by navigation.
[<Tests>]
let interactionTests =
    testList "Interaction" [
        test "a seeded keyboard activation changes the product model (responds-proof)" {
            let metrics = ControlsElmish.Perf.runScript Host.defaultHost size (Scripts.forPage "buttons")
            Expect.isTrue
                (metrics |> List.exists (fun m -> m.ProductModelChanged))
                "at least one frame reports ProductModelChanged"
        }

        test "pure update: activating the command increments clicks" {
            let m0 = Host.initModel
            let m1 = Model.update (PageMsg ButtonClicked) m0
            Expect.equal m1.PageState.ButtonClicks 1 "button activation recorded"
        }

        test "display-only controls are exempt: navigation never alters interactive state" {
            let m0 = { Host.initModel with PageState = { DemoState.seed with ButtonClicks = 3 } }
            let m1 = Model.update (SelectPage "display-typography") m0
            Expect.equal m1.PageState.ButtonClicks 3 "navigating to a display-only page changes no interactive state"
            Expect.equal m1.CurrentPage "display-typography" "page navigation applied"
        }

        test "each interaction message maps to its own field transition" {
            let m = Host.initModel
            Expect.equal (Model.update (PageMsg(TextChanged "abc")) m).PageState.TextValue "abc" "text"
            Expect.equal (Model.update (PageMsg(SliderChanged 0.25)) m).PageState.SliderValue 0.25 "slider"
            Expect.equal (Model.update (PageMsg(CheckChanged false)) m).PageState.Checked false "checkbox"
            Expect.equal (Model.update (PageMsg(ComboChanged "Teal")) m).PageState.ComboSelected "Teal" "combo"
        }
    ]

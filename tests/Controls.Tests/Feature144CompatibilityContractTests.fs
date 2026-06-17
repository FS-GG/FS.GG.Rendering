module Feature144CompatibilityContractTests

open Expecto
open FS.GG.UI.Controls
open Feature144OverlayFixtures

[<Tests>]
let tests =
    testList "Feature144 compatibility contract" [
        test "ControlRuntimeModel does not own overlay visibility or selection state" {
            let model, _ = ControlRuntime.init ()

            Expect.isNone model.FocusedControl "runtime focus remains independent"
            Expect.isEmpty model.PressedControls "runtime press state remains independent"
            Expect.isEmpty model.Diagnostics "runtime does not seed overlay diagnostics"
        }

        test "overlay bridge dispatch records are derived from effects" {
            let opened, effects = openOne (metadata TransientSurfaceKind.Menu "compat-menu" 10 true true false)
            let bridge = ControlRuntime.attachOverlayEffects opened effects
            let records = ControlRuntime.overlayDispatchRecords bridge

            Expect.exists records (fun record -> record.Kind = "request-open-state-change" && record.ProductVisible) "open request is product-visible"
            Expect.exists records (fun record -> record.Kind = "request-focus" && record.ProductVisible) "focus request is product-visible"
            Expect.isFalse (records |> List.exists (fun record -> record.Kind = "selected-state")) "selection state is not runtime-owned"
        }

        test "metadata surface changes are additive to existing coordinator contract" {
            let item = metadata TransientSurfaceKind.Menu "compat-surface" 10 true true false
            let surface = TransientWidget.toSurface (anchorFor (item.AnchorId)) item

            Expect.equal surface.DismissalPolicy.SelectionCompletion CloseOnSelection "default selection policy preserved"
            Expect.equal surface.Trigger.Enabled true "trigger state preserved"
            Expect.equal surface.Modal false "non-modal metadata stays non-modal"
        }
    ]

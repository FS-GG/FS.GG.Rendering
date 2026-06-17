module AntShowcase.Tests.Feature144DatePickerFlowTests

open Expecto
open FS.GG.UI.Controls
open AntShowcase.Core
open AntShowcase.Core.Model

[<Tests>]
let tests =
    testList "Feature144 AntShowcase date-picker flow" [
        test "reference script opens selects dismisses and recovers focus through product-owned state" {
            let finalModel =
                Scripts.datePickerReferenceFlow
                |> List.fold (fun model msg -> Model.update msg model) Host.initModel

            Expect.equal finalModel.CurrentPage "text-numeric-input" "script navigates to input page"
            Expect.equal finalModel.PageState.DatePickerSelected (Some(System.DateOnly(2026, 6, 17))) "selected date is product-owned"
            Expect.isFalse finalModel.PageState.DatePickerOpen "selection leaves calendar closed"
            Expect.equal finalModel.PageState.DatePickerFocused (Some "date-picker-trigger") "focus recovers to trigger"
        }

        test "input page publishes date-picker transient metadata tied to product open state" {
            let page = PageRegistry.byId "text-numeric-input"
            let openState =
                { DemoState.seed with
                    DatePickerOpen = true
                    DatePickerSelected = Some(System.DateOnly(2026, 6, 17)) }

            let metadata =
                page.View openState
                |> TransientWidget.collect
                |> List.filter (fun item -> item.SurfaceKind = TransientSurfaceKind.DatePickerCalendar)

            Expect.equal metadata.Length 1 "date-picker contributes one calendar surface"
            Expect.equal metadata.Head.SurfaceId "date-picker-calendar" "surface id is stable"
            Expect.equal metadata.Head.TriggerId "date-picker-trigger" "trigger id is stable"
            Expect.isTrue metadata.Head.VisibilityState "open state remains product-owned and visible in metadata"
        }
    ]

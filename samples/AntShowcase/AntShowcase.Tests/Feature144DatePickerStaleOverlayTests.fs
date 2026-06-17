module AntShowcase.Tests.Feature144DatePickerStaleOverlayTests

open Expecto
open FS.GG.UI.Controls
open AntShowcase.Core

[<Tests>]
let tests =
    testList "Feature144 AntShowcase date-picker stale-overlay proof" [
        test "final reference render publishes closed metadata and no stale open surface" {
            let finalModel =
                Scripts.datePickerReferenceFlow
                |> List.fold (fun model msg -> Model.update msg model) Host.initModel
            let page = PageRegistry.byId "text-numeric-input"

            let dateMetadata =
                page.View finalModel.PageState
                |> TransientWidget.collect
                |> List.find (fun item -> item.SurfaceKind = TransientSurfaceKind.DatePickerCalendar)

            Expect.isFalse dateMetadata.VisibilityState "final product state closes the calendar"
            Expect.isEmpty (OverlayState.init ()).OpenSurfaces "no coordinator surface is retained by sample model"
        }

        test "reference evidence records replay focus product messages and no stale overlay" {
            let evidence = Evidence.datePickerReferenceOverlayEvidence ()

            Expect.contains evidence.ReplayLog "select:2026-06-17" "selection is recorded"
            Expect.contains evidence.ProductMessages "DatePickerChanged:2026-06-17" "product value message recorded"
            Expect.contains evidence.FocusTransitions (Some "date-picker-calendar", Some "date-picker-trigger") "focus recovery recorded"
            Expect.isEmpty evidence.Diagnostics "reference flow has no overlay diagnostics"
            Expect.isTrue evidence.NoStaleOverlay "no stale overlay proof is explicit"
        }
    ]

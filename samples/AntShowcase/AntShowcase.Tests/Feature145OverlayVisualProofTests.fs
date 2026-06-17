module AntShowcase.Tests.Feature145OverlayVisualProofTests

open Expecto
open AntShowcase.Core

[<Tests>]
let tests =
    testList "Feature145 AntShowcase overlay visual correlation" [
        test "date-picker reference evidence carries scenario hit focus and dispatch correlation" {
            let evidence = Evidence.datePickerReferenceOverlayEvidence ()

            Expect.equal evidence.ScenarioId "feature144-antshowcase-date-picker-reference" "scenario id matches harness visual proof"
            Expect.equal evidence.InputStep "open:date-picker-calendar" "input step names the open overlay state"
            Expect.equal evidence.ExpectedOverlayState "open" "expected overlay state is explicit"
            Expect.equal evidence.TopmostHitTarget "date-picker-calendar" "topmost hit target is reviewable"
            Expect.equal evidence.FocusState "date-picker-trigger" "final focus recovery is reviewable"
            Expect.stringContains evidence.DispatchSummary "DatePickerChanged:2026-06-17" "dispatch summary includes product selection"
            Expect.isTrue evidence.NoStaleOverlay "closed-state no-stale-overlay claim is explicit"
        }
    ]

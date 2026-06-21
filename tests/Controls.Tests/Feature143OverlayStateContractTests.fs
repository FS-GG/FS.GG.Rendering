module Feature143OverlayStateContractTests

open System.IO
open Expecto
open FS.GG.UI.Controls
open Feature143OverlayFixtures
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private contractPath =
    Path.Combine(repositoryRoot, "src", "Controls", "OverlayState.fsi")

[<Tests>]
let tests =
    testList "Feature143 overlay state contract" [
        test "OverlayState signature exposes MVU state message effect boundary" {
            let text = File.ReadAllText contractPath

            [ "type TransientSurfaceKind"
              "type OverlayState"
              "type OverlayMsg"
              "type OverlayEffect"
              "val init"
              "val update" ]
            |> List.iter (fun token -> Expect.stringContains text token $"contract contains {token}")
        }

        test "supported surface kinds are finite and complete" {
            Expect.equal
                (OverlayState.supportedSurfaceKinds ())
                [ TransientSurfaceKind.Menu; TransientSurfaceKind.ContextMenu; TransientSurfaceKind.SplitButtonMenu; TransientSurfaceKind.ComboDropdown; TransientSurfaceKind.AutoCompleteSuggestions; TransientSurfaceKind.DatePickerCalendar; TransientSurfaceKind.ColorPickerPalette; TransientSurfaceKind.DialogModal ]
                "all eight required transient surface categories are explicit"
        }

        test "init is empty and deterministic" {
            let first = OverlayState.init ()
            let second = OverlayState.init ()

            Expect.equal first second "init is deterministic"
            Expect.isEmpty first.OpenSurfaces "no open surfaces"
            Expect.isNone first.ActiveSurface "no active surface"
            Expect.isEmpty first.ReplayLog.Inputs "no replay inputs"
        }

        test "open emits ordered open focus and consume effects" {
            let state, effects = openOne (surface TransientSurfaceKind.Menu "menu" 10)

            Expect.equal state.ActiveSurface (Some "menu") "opened surface is active"
            Expect.equal state.FocusedControl (Some "menu-item-1") "focus enters initial stop"
            Expect.equal effects [ RequestOpenStateChange("menu", true); RequestFocus(Some "menu-item-1"); ConsumeInput ] "effect order is stable"
        }

        test "invalid messages become diagnostics instead of exceptions" {
            let state, effects = OverlayState.update (DismissRequested(Some "missing", DismissalReason.Escape)) (OverlayState.init ())

            Expect.isEmpty state.OpenSurfaces "invalid dismiss does not create state"
            Expect.exists (OverlayState.diagnostics state) (fun d -> d.Code = InvalidOverlayMessage) "invalid message diagnostic"
            Expect.exists effects (function ReportOverlayDiagnostic d when d.Code = InvalidOverlayMessage -> true | _ -> false) "diagnostic effect emitted"
        }
    ]

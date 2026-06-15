module ControlsControlRuntimeContractTests

open System.IO
open Expecto
open FS.GG.UI.Controls

let repositoryRoot =
    let rec find dir =
        if File.Exists(Path.Combine(dir, "FS.GG.Rendering.slnx")) then
            dir
        else
            (match Directory.GetParent dir |> Option.ofObj with Some p -> find p.FullName | None -> dir)

    find __SOURCE_DIRECTORY__

let contractPath =
    Path.Combine(repositoryRoot, "src", "Controls", "ControlRuntime.fsi")

let contractText () =
    if File.Exists contractPath then
        File.ReadAllText contractPath
    else
        ""

[<Tests>]
let controlRuntimeContractTests =
    testList "Controls ControlRuntime contract" [
        test "ControlRuntime signature file exists under Controls" {
            Expect.isTrue (File.Exists contractPath) "src/Controls/ControlRuntime.fsi exists"
        }

        test "ControlRuntime exposes product-owned model message effect init and pure update" {
            let text = contractText ()

            [ "type ControlRuntimeModel"
              "type ControlRuntimeMsg"
              "type ControlRuntimeEffect"
              "val init"
              "val update" ]
            |> List.iter (fun required ->
                Expect.stringContains text required $"ControlRuntime contract contains {required}")
        }

        test "ControlRuntime contract covers transient state and stale recovery diagnostics" {
            let text = contractText ()

            [ "FocusedControl"
              "HoveredControl"
              "PressedControls"
              "Caret"
              "Selection"
              "Composition"
              "ActiveDrag"
              "StaleTarget"
              "CancelledInteraction"
              "RecoverStaleTarget"
              "ReportControlRuntimeDiagnostic" ]
            |> List.iter (fun required ->
                Expect.stringContains text required $"ControlRuntime contract contains {required}")
        }

        test "ControlRuntime cancellation clears transient text and drag state" {
            let runtime, _ = ControlRuntime.init ()
            let withCaret, _ = ControlRuntime.update (SetCaret(Some { ControlId = "name"; Index = 2 })) runtime
            let withSelection, _ = ControlRuntime.update (SetSelection(Some { ControlId = "name"; Start = 0; End = 2 })) withCaret
            let composing, _ = ControlRuntime.update (StartComposition("name", "a")) withSelection
            let dragging, _ = ControlRuntime.update (StartDrag("name", 0.0, 0.0)) composing
            let cancelled, effects = ControlRuntime.update (CancelInteraction(Some "name")) dragging

            Expect.isNone cancelled.Caret "cancel clears caret"
            Expect.isNone cancelled.Selection "cancel clears selection"
            Expect.isNone cancelled.Composition "cancel clears composition"
            Expect.isNone cancelled.ActiveDrag "cancel clears drag"
            Expect.exists effects (function CancelledInteraction(Some "name") -> true | _ -> false) "cancel effect names control"
        }
    ]

module ControlsElmishAdapterContractTests

open System
open System.IO
open Expecto
open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Elmish

let repositoryRoot =
    // Feature 045 deleted build.fsx; locate the repo root by the solution file instead, and
    // terminate at the filesystem root (the old build.fsx-marker walk looped forever once the
    // marker was gone, hanging this test's module init — and thus Expecto discovery — headless).
    let rec find dir =
        if File.Exists(Path.Combine(dir, "FS.GG.Rendering.slnx")) then
            dir
        else
            match Directory.GetParent dir |> Option.ofObj with
            | Some parent -> find parent.FullName
            | None -> dir

    find __SOURCE_DIRECTORY__

let adapterProject =
    Path.Combine(repositoryRoot, "src", "Controls.Elmish", "Controls.Elmish.fsproj")

let adapterContract =
    Path.Combine(repositoryRoot, "src", "Controls.Elmish", "ControlsElmish.fsi")

let readIfExists path =
    if File.Exists path then File.ReadAllText path else ""

[<Tests>]
let controlsElmishAdapterContractTests =
    testList "Controls Elmish adapter contract" [
        test "dedicated Controls.Elmish package owns command and subscription integration" {
            Expect.isTrue (File.Exists adapterProject) "src/Controls.Elmish/Controls.Elmish.fsproj exists"
            Expect.isTrue (File.Exists adapterContract) "src/Controls.Elmish/ControlsElmish.fsi exists"

            let project = readIfExists adapterProject
            Expect.stringContains project "FS.Skia.UI.Controls.Elmish" "adapter package id is Controls-specific"
            Expect.stringContains project "Fable.Elmish" "adapter owns Fable.Elmish dependency"
            Expect.stringContains project @"..\Controls\Controls.fsproj" "adapter references Controls"
            Expect.stringContains project @"..\KeyboardInput\KeyboardInput.fsproj" "adapter references KeyboardInput"
        }

        test "adapter contract interprets keyboard and control effects without moving Cmd into base Controls" {
            let contract = readIfExists adapterContract
            let controlsContracts =
                [ Path.Combine(repositoryRoot, "src", "Controls", "Types.fsi")
                  Path.Combine(repositoryRoot, "src", "Controls", "Control.fsi")
                  Path.Combine(repositoryRoot, "src", "Controls", "TextInput.fsi") ]
                |> List.map readIfExists
                |> String.concat Environment.NewLine

            [ "interpretKeyboardEffect"
              "interpretControlEffect"
              "subscriptions"
              "program"
              "AdapterDiagnostic"
              "ControlRuntimeMsg"
              "KeyboardMsg" ]
            |> List.iter (fun required ->
                Expect.stringContains contract required $"ControlsElmish contract contains {required}")

            Expect.isFalse (controlsContracts.Contains("Cmd<", StringComparison.Ordinal)) "base Controls contracts do not expose Elmish Cmd"
        }

        test "adapter reports stale control targets as diagnostics" {
            let command =
                ControlsElmish.interpretControlEffect id (StaleTarget "missing-button")

            Expect.exists command (function ReportAdapterDiagnostic diagnostic when diagnostic.Code.Contains "StaleTarget" && diagnostic.Message.Contains "missing-button" -> true | _ -> false) "stale target maps to adapter diagnostic"
        }

        test "068 additive surface present; existing signatures unchanged (US3, FR-002/FR-009/SC-004)" {
            let contract = readIfExists adapterContract

            // Existing contract retained, byte-for-byte (the View field stays Control<'msg>).
            [ "View: 'model -> Control<'msg>"
              "val program:"
              "val interpretKeyboardEffect:"
              "val interpretControlEffect:"
              "val subscriptions:"
              "val diagnostic:"
              "AdapterCommand<'msg> = AdapterEffect<'msg> list" ]
            |> List.iter (fun required ->
                Expect.stringContains contract required $"existing signature retained: {required}")

            // New additive 068 surface declared in the .fsi.
            [ "module AdapterCmd"
              "val widgetView:"
              "val programOfWidget:"
              "val toCmd:"
              "val productMessages:" ]
            |> List.iter (fun added ->
                Expect.stringContains contract added $"additive 068 surface declared: {added}")
        }

        test "base Controls package declares no Fable.Elmish reference — dependency split preserved (US3, FR-006/SC-005)" {
            let controlsProject =
                Path.Combine(repositoryRoot, "src", "Controls", "Controls.fsproj")
            let text = readIfExists controlsProject

            Expect.isTrue (File.Exists controlsProject) "src/Controls/Controls.fsproj exists"
            Expect.isFalse (text.Contains "Fable.Elmish") "base Controls.fsproj does not reference Fable.Elmish"

            // The adapter package, by contrast, owns the Fable.Elmish dependency that supplies Cmd<'msg>.
            let adapter = readIfExists adapterProject
            Expect.stringContains adapter "Fable.Elmish" "adapter package owns the Fable.Elmish dependency"
        }
    ]

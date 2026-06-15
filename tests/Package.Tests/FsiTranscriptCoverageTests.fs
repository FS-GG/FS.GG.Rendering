module FsiTranscriptCoverageTests

open System
open System.IO
open Expecto

let rec findRepositoryRoot (directory: string) =
    if Directory.GetFiles(directory, "*.sln").Length > 0 || File.Exists(Path.Combine(directory, "build.fsx")) then
        directory
    else
        match Directory.GetParent directory |> Option.ofObj with
        | Some parent -> findRepositoryRoot parent.FullName
        | None -> failwithf "Could not locate repository root from %s" directory

let repositoryRoot = findRepositoryRoot AppContext.BaseDirectory

let repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let transcriptPath name =
    repositoryPath $"specs/035-api-discovery-names/readiness/fsi/{name}"

let readTranscript name =
    let path = transcriptPath name
    Expect.isTrue (File.Exists path) $"FSI transcript evidence exists at {path}"
    File.ReadAllText path

[<Tests>]
let fsiTranscriptCoverageTests =
    testList "Package-shaped FSI transcript coverage" [
        test "Scene transcript authors primitives, Paint helpers, and geometry records" {
            let transcript = readTranscript "scene-authoring.fsx"

            [ "#r \"nuget: FS.GG.UI.Scene"
              "open FS.GG.UI.Scene"
              "Rect"
              "Paint"
              "Stroke"
              "TextRun"
              "SceneElementKind.RectangleElement" ]
            |> List.iter (fun required ->
                Expect.stringContains transcript required $"Scene transcript includes {required}")
        }

        test "Viewer and keyboard transcript authors public records and cases" {
            let transcript = readTranscript "viewer-keyboard-authoring.fsx"

            [ "#r \"nuget: FS.GG.UI.SkiaViewer"
              "#r \"nuget: FS.GG.UI.KeyboardInput"
              "ViewerOptions"
              "InitialSize"
              "ViewerWindowPosition.Coordinates"
              "KeyboardModel"
              "KeyDown"
              "KeyUp" ]
            |> List.iter (fun required ->
                Expect.stringContains transcript required $"Viewer/keyboard transcript includes {required}")
        }

        test "Controls-adjacent transcript authors controls without relying on repository source" {
            let transcript = readTranscript "controls-adjacent-authoring.fsx"

            [ "#r \"nuget: FS.GG.UI.Controls"
              "#r \"nuget: FS.GG.UI.Controls.Elmish"
              "FS.GG.UI.Controls.TextBlock.create"
              "FS.GG.UI.Controls.TextBox.onChanged"
              "FS.GG.UI.Controls.DataGrid.create"
              "ControlsElmish" ]
            |> List.iter (fun required ->
                Expect.stringContains transcript required $"Controls transcript includes {required}")

            [ "#load"
              "../src/"
              "Assembly.Load"
              "GetExportedTypes" ]
            |> List.iter (fun forbidden ->
                Expect.isFalse (transcript.Contains(forbidden, StringComparison.OrdinalIgnoreCase)) $"Controls transcript does not use {forbidden}")
        }

        test "FSI transcript run logs are captured as readiness artifacts" {
            [ "scene-authoring.log"
              "viewer-keyboard-authoring.log"
              "controls-adjacent-authoring.log" ]
            |> List.iter (fun logName ->
                let logPath = transcriptPath logName
                Expect.isTrue (File.Exists logPath) $"{logName} exists"
                Expect.stringContains (File.ReadAllText logPath) "FSI transcript PASS" $"{logName} records a passing FSI run")
        }
    ]

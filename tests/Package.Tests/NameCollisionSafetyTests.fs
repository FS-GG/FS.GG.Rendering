module NameCollisionSafetyTests

open System
open System.IO
open Expecto

let rec findRepositoryRoot (directory: string) =
    if Directory.GetFiles(directory, "*.sln").Length > 0 || Directory.GetFiles(directory, "*.slnx").Length > 0 || File.Exists(Path.Combine(directory, "build.fsx")) then
        directory
    else
        match Directory.GetParent directory |> Option.ofObj with
        | Some parent -> findRepositoryRoot parent.FullName
        | None -> failwithf "Could not locate repository root from %s" directory

let repositoryRoot = findRepositoryRoot AppContext.BaseDirectory

let repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let collisionReportPath =
    repositoryPath "specs/035-api-discovery-names/readiness/name-collision-safety.md"

let readCollisionReport () =
    Expect.isTrue (File.Exists collisionReportPath) $"name collision report exists at {collisionReportPath}"
    File.ReadAllText collisionReportPath

let transcriptPath name =
    repositoryPath $"specs/035-api-discovery-names/readiness/fsi/{name}"

let readTranscript name =
    let path = transcriptPath name
    Expect.isTrue (File.Exists path) $"mixed Scene/Controls transcript evidence exists at {path}"
    File.ReadAllText path

[<Tests>]
let nameCollisionSafetyTests =
    testList "Scene and Controls name collision inventory" [
        test "collision safety report contains structured decision records" {
            let report = readCollisionReport ()

            [ "| Name | Scene owner | Controls owner | Symbol kind | Risk | Decision | Guidance | Validation scenario |"
              "|------|-------------|----------------|-------------|------|----------|----------|---------------------|"
              "decision: contract-qualified"
              "decision: consumer-guidance"
              "risk: open-order-sensitive" ]
            |> List.iter (fun required ->
                Expect.stringContains report required $"collision report includes {required}")
        }

        test "identified overlaps include text, geometry, color, event, and builder authoring names" {
            let report = readCollisionReport ()

            [ "`Text`"
              "`Width`"
              "`Height`"
              "`Color`"
              "`Changed`"
              "`children`"
              "`create`" ]
            |> List.iter (fun name ->
                Expect.stringContains report name $"collision inventory includes {name}")
        }

        test "collision guidance uses explicit Scene and Controls qualifications" {
            let report = readCollisionReport ()

            [ "FS.GG.UI.Scene.Rect"
              "FS.GG.UI.Scene.Paint"
              "FS.GG.UI.Scene.TextRun"
              "FS.GG.UI.Controls.TextBlock.create"
              "FS.GG.UI.Controls.TextBox.onChanged"
              "FS.GG.UI.Controls.Stack.children" ]
            |> List.iter (fun sample ->
                Expect.stringContains report sample $"collision guidance includes {sample}")
        }

        test "every inventory row names a validation scenario and compatibility action" {
            let report = readCollisionReport ()

            [ "validation: mixed-scene-controls-open-scene-first"
              "validation: mixed-scene-controls-open-controls-first"
              "compatibility: no-contract-change"
              "compatibility: signature-change-reviewed" ]
            |> List.iter (fun required ->
                Expect.stringContains report required $"collision report includes {required}")
        }

        test "mixed Scene and Controls compile samples pass with both open orders" {
            [ "mixed-scene-controls-open-scene-first.fsx", "mixed-scene-controls-open-scene-first.log"
              "mixed-scene-controls-open-controls-first.fsx", "mixed-scene-controls-open-controls-first.log" ]
            |> List.iter (fun (scriptName, logName) ->
                let script = readTranscript scriptName
                let log = readTranscript logName

                [ "FS.GG.UI.Scene.Rect"
                  "FS.GG.UI.Scene.Paint"
                  "FS.GG.UI.Controls.Stack.children"
                  "FS.GG.UI.Controls.TextBlock.create"
                  "FS.GG.UI.Controls.TextBox.onChanged" ]
                |> List.iter (fun required ->
                    Expect.stringContains script required $"{scriptName} qualifies {required}")

                Expect.stringContains log "FSI transcript PASS" $"{logName} records a passing mixed open-order run")
        }
    ]

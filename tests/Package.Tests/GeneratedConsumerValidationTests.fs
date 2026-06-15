module GeneratedConsumerValidationTests

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

let validationReportPath =
    repositoryPath "specs/035-api-discovery-names/readiness/generated-consumer-validation.md"

let readValidationReport () =
    Expect.isTrue (File.Exists validationReportPath) $"generated consumer validation report exists at {validationReportPath}"
    File.ReadAllText validationReportPath

[<Tests>]
let generatedConsumerValidationTests =
    testList "Generated clean package consumer validation" [
        test "validation report records clean package restore and build artifacts" {
            let report = readValidationReport ()

            [ "consumer-project:"
              "restore-log:"
              "build-log:"
              "local-package-feed:"
              "package-version:"
              "result: pass" ]
            |> List.iter (fun required ->
                Expect.stringContains report required $"generated consumer validation includes {required}")
        }

        test "validation proves package-only consumption without project references or copied source" {
            let report = readValidationReport ()

            [ "project-references: none"
              "copied-src-files: none"
              "repository-source-inspection: false"
              "assembly-reflection-authoring: false"
              "package-references:"
              "FS.GG.UI.Scene"
              "FS.GG.UI.Controls"
              "FS.GG.UI.SkiaViewer" ]
            |> List.iter (fun required ->
                Expect.stringContains report required $"generated consumer validation includes {required}")
        }

        test "validation diagnostics are actionable for restore, copied source, reflection, and compile failures" {
            let report = readValidationReport ()

            [ "failure-class: restore"
              "failure-class: project-reference"
              "failure-class: copied-src"
              "failure-class: reflection-authoring"
              "failure-class: compile"
              "next-action:" ]
            |> List.iter (fun required ->
                Expect.stringContains report required $"generated consumer validation includes diagnostic {required}")
        }
    ]

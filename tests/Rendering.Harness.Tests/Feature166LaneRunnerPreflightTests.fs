module Feature166LaneRunnerPreflightTests

open System.IO
open Expecto
open Rendering.Harness

let validate
    (root: string)
    (request: ValidationLanes.RunRequest)
    : Result<ValidationLanes.LaneRunPlan, ValidationLanes.PreflightDiagnostic list> =
    let runId = request.RunId |> Option.defaultValue "run"
    let runRoot = Path.Combine(request.OutDir, runId)
    let lanes = ValidationLanes.defaultLaneDefinitions root runRoot
    ValidationLanes.validateRequest root lanes { request with RunId = Some runId }

[<Tests>]
let tests =
    testList "Feature166LaneRunnerPreflight" [
        test "default request selects required lanes and excludes optional aggregate" {
            let root = Feature166TestFixtures.createTempRoot "feature166-preflight-required"

            try
                let request = ValidationLanes.defaultRunRequest (Path.Combine(root, "out"))

                match validate root request with
                | Ok plan ->
                    let ids = plan.SelectedLanes |> List.map _.Id
                    Expect.equal plan.SelectionMode ValidationLanes.RequiredSelection "required mode"
                    Expect.contains ids "build" "build"
                    Expect.contains ids "rendering-harness" "harness"
                    Expect.isFalse (ids |> List.contains "aggregate-solution") "aggregate excluded"
                | Error diagnostics -> failtestf "unexpected diagnostics: %A" diagnostics
            finally
                Feature166TestFixtures.deleteTempRoot root
        }

        test "explicit lanes and optional aggregate inclusion are accepted" {
            let root = Feature166TestFixtures.createTempRoot "feature166-preflight-explicit"

            try
                let request =
                    { ValidationLanes.defaultRunRequest (Path.Combine(root, "out")) with
                        RequestedLaneIds = [ "rendering-harness" ]
                        IncludeOptionalLaneIds = [ "aggregate-solution" ] }

                match validate root request with
                | Ok plan ->
                    let ids = plan.SelectedLanes |> List.map _.Id
                    Expect.equal plan.SelectionMode ValidationLanes.ExplicitSelection "explicit mode"
                    Expect.equal ids [ "rendering-harness"; "aggregate-solution" ] "selection order"
                | Error diagnostics -> failtestf "unexpected diagnostics: %A" diagnostics
            finally
                Feature166TestFixtures.deleteTempRoot root
        }

        test "unknown and duplicate requested lanes fail before work starts" {
            let root = Feature166TestFixtures.createTempRoot "feature166-preflight-invalid"

            try
                let request =
                    { ValidationLanes.defaultRunRequest (Path.Combine(root, "out")) with
                        RequestedLaneIds = [ "rendering-harness"; "rendering-harness"; "does-not-exist" ] }

                match validate root request with
                | Ok _ -> failtest "expected preflight errors"
                | Error diagnostics ->
                    Expect.exists diagnostics (fun d -> d.Code = "duplicate-requested-lane") "duplicate"
                    Expect.exists diagnostics (fun d -> d.Code = "unknown-lane") "unknown"
            finally
                Feature166TestFixtures.deleteTempRoot root
        }

        test "existing run id fails unless replacement is explicit" {
            let root = Feature166TestFixtures.createTempRoot "feature166-preflight-replace"

            try
                let out = Path.Combine(root, "out")
                Directory.CreateDirectory(Path.Combine(out, "same-run")) |> ignore

                let request =
                    { ValidationLanes.defaultRunRequest out with
                        RequestedLaneIds = [ "rendering-harness" ]
                        RunId = Some "same-run" }

                match validate root request with
                | Ok _ -> failtest "expected no-overwrite error"
                | Error diagnostics -> Expect.exists diagnostics (fun d -> d.Code = "run-id-exists") "no overwrite"

                match validate root { request with ReplaceRun = true } with
                | Ok plan -> Expect.isSome plan.ReplacementNotice "replacement notice"
                | Error diagnostics -> failtestf "unexpected diagnostics: %A" diagnostics
            finally
                Feature166TestFixtures.deleteTempRoot root
        }
    ]

module Feature169ValidationDiagnosticsTests

open Expecto
open FS.GG.UI.Diagnostics
open Rendering.Harness

let private emptySummary status : DiagnosticSummary =
    { RunId = Some "feature169-validation"
      Status = status
      CountsBySeverity = []
      CountsByCategory = []
      BlockerCount = 0
      UnclassifiedCount = 0
      ReviewRequiredCount = 0
      ExceptionCount = 0
      ArtifactPaths = [ "diagnostics-summary.json" ]
      Groups = []
      Exceptions = []
      ArtifactWriteDiagnostics = [] }

[<Tests>]
let tests =
    testList "Feature169 validation lane typed diagnostics" [
        test "Synthetic lane status derives from diagnostic summary status" {
            Expect.equal (ValidationLanes.laneStatusFromDiagnosticSummary (emptySummary ReadinessDiagnosticStatus.Accepted)) ValidationLanes.Passed "accepted"
            Expect.equal (ValidationLanes.laneStatusFromDiagnosticSummary (emptySummary ReadinessDiagnosticStatus.Blocked)) ValidationLanes.Failed "blocked"
            Expect.equal (ValidationLanes.laneStatusFromDiagnosticSummary (emptySummary ReadinessDiagnosticStatus.ReviewRequired)) ValidationLanes.Failed "review"
            Expect.equal (ValidationLanes.laneStatusFromDiagnosticSummary (emptySummary ReadinessDiagnosticStatus.EnvironmentLimitedStatus)) ValidationLanes.EnvironmentLimited "environment"
        }

        test "Synthetic diagnostics lane is selectable with include semantics" {
            let root = Feature166TestFixtures.createTempRoot "feature169-validation-lane"

            try
                let outDir = System.IO.Path.Combine(root, "out")
                let request =
                    { ValidationLanes.defaultRunRequest outDir with
                        IncludeOptionalLaneIds = [ "diagnostics" ] }

                let runId = request.RunId |> Option.defaultWith ValidationLanes.createRunId
                let lanes = ValidationLanes.defaultLaneDefinitions root (System.IO.Path.Combine(outDir, runId))
                let plan = ValidationLanes.validateRequest root lanes { request with RunId = Some runId }

                match plan with
                | Result.Ok p -> Expect.isTrue (p.SelectedLanes |> List.exists (fun lane -> lane.Id = "diagnostics")) "diagnostics lane selected"
                | Result.Error diagnostics -> failtestf "unexpected diagnostics: %A" diagnostics
            finally
                Feature166TestFixtures.deleteTempRoot root
        }
    ]

module FeatureFeedbackReportSkillTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport
open FsGgFeedbackReportTool

let private repositoryRoot = RepositoryRoot.value

let private validReport =
    let coverage =
        surfaces
        |> List.map (fun surface -> sprintf "| %s | exercised | evidence |" surface)
        |> String.concat Environment.NewLine

    $"""---
feedbackSchema: 2
date: 2026-07-23
workspace: Example
cycle: 001-example
lane: sdd
toolVersion: 0.23.0
commit: abc123
---

## §1 Provenance and confidence
Complete checkpoints.
## §2 What worked
The scaffold built.
## §3 What did not
One retry.
## §4 Findings
#### §4.1 Example friction
- **Kind:** friction
- **Impact:** one avoidable retry
- **Expected:** one pass
- **Observed:** two passes
- **Evidence:** build.log
- **Version:** 0.23.0
- **Owner:** FS-GG/FS.GG.Rendering template
- **Recurrence:** new
- **Avoidable cost:** one retry
- **Disposition:** skill fix
## §5 Did not exercise
None observed.
## §6 Doc-versus-behavior contradictions
None observed.
## §7 Workarounds still in the tree
None observed.
## §8 Friction and avoidable cost
One retry.
## §9 Skill value and gaps
The feedback skill was used.
## §10 Outcome markers
First build in one minute.
## §11 Falsifiable improvements
Remove the retry.
## §12 Development-surface coverage
| Surface | Status | Evidence and result |
|---|---|---|
{coverage}
"""

[<Tests>]
let feedbackReportSkillTests =
    testList
        "fs-gg-feedback-report schema v2"
        [ test "canonical skill ships its deterministic helper" {
              let root =
                  Path.Combine(
                      repositoryRoot,
                      "template",
                      "feedback-report",
                      "skill",
                      "scripts"
                  )

              Expect.isTrue
                  (File.Exists(Path.Combine(root, "FeedbackReportTool.fs")))
                  "testable helper core is in the copied skill payload"

              Expect.isTrue
                  (File.Exists(Path.Combine(root, "feedback-tool.fsx")))
                  "portable FSI entry point is in the copied skill payload"
          }

          test "valid schema-v2 report passes" {
              Expect.isEmpty (validateReportText validReport) "valid report has no errors"
          }

          test "loose finding and incomplete coverage fail" {
              let malformed =
                  validReport
                      .Replace(
                          "#### §4.1 Example friction",
                          "- Filed #24: a loose unstructured finding"
                      )
                      .Replace("| worker-git-pr | exercised | evidence |", "")

              let errors = validateReportText malformed

              Expect.contains
                  errors
                  "findings: use structured §4.n records or write 'None observed.'"
                  "loose findings are rejected"

              Expect.contains
                  errors
                  "coverage: missing surface 'worker-git-pr'"
                  "missing coverage is rejected"
          }

          test "checkpoint append is valid JSONL and preserves the complete event" {
              let root =
                  Path.Combine(
                      Path.GetTempPath(),
                      "fsgg-feedback-test-" + Guid.NewGuid().ToString "N"
                  )

              try
                  let path =
                      appendCheckpoint
                          root
                          "001-example"
                          "first-build"
                          "dependencies-build"
                          "friction"
                          "restore required a retry"
                          "build.log"
                          "one retry"
                          "FS-GG/FS.GG.Rendering template"

                  Expect.isTrue (File.Exists path) "checkpoint file was created"
                  Expect.isEmpty (validateCheckpointFile path) "written checkpoint validates"
                  let line = File.ReadAllText path
                  Expect.stringContains line "restore required a retry" "summary is retained"
                  Expect.stringContains line "dependencies-build" "surface is retained"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)
          } ]


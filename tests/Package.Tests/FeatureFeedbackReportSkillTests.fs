module FeatureFeedbackReportSkillTests

open System
open System.IO
open System.Text.Json
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
- **Evidence:** file:readiness/build.log
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

let private auditJson root reportPath reportText status evidence =
    let report = Path.GetRelativePath(root, reportPath).Replace(Path.DirectorySeparatorChar, '/')

    JsonSerializer.Serialize(
        {| auditSchema = 1
           report = report
           reportSha256 = sha256Text reportText
           criticMode = "fresh-context-subagent"
           criticPromptVersion = "actionability-v1"
           findings =
            [ {| id = "§4.1"
                 status = status
                 missingFacts = [||]
                 checkedEvidence = evidence
                 confidenceLimits = [||] |} ] |}
    )

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

          test "complete actionable audit binds every finding and verified evidence digest" {
              let root =
                  Path.Combine(
                      Path.GetTempPath(),
                      "fsgg-feedback-audit-" + Guid.NewGuid().ToString "N"
                  )

              try
                  Directory.CreateDirectory(Path.Combine(root, "feedback")) |> ignore
                  Directory.CreateDirectory(Path.Combine(root, "readiness")) |> ignore
                  let reportPath = Path.Combine(root, "feedback", "report.md")
                  let evidencePath = Path.Combine(root, "readiness", "build.log")
                  File.WriteAllText(reportPath, validReport)
                  File.WriteAllText(evidencePath, "green")

                  let evidence =
                      [| {| locator = "file:readiness/build.log"
                            result = "verified"
                            sha256 = Some(sha256Text "green") |} |]

                  let audit = auditJson root reportPath validReport "actionable" evidence

                  Expect.isEmpty
                      (validateActionabilityAudit root reportPath validReport audit)
                      "bound actionable audit validates"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)
          }

          test "polished fact-free and circular finding stays incomplete" {
              let report =
                  validReport
                      .Replace("one pass", "the behavior should work")
                      .Replace("two passes", "the behavior should work")
                      .Replace(
                          "file:readiness/build.log",
                          "claim-only:no inspectable evidence"
                      )

              let root = Path.GetTempPath()
              let reportPath = Path.Combine(root, "feedback", "report.md")
              let evidence =
                  [| {| locator = "claim-only:no inspectable evidence"
                        result = "claim-only"
                        sha256 = None |} |]

              let errors =
                  auditJson root reportPath report "incomplete" evidence
                  |> validateActionabilityAudit root reportPath report

              Expect.exists
                  errors
                  (fun error -> error.Contains("remains incomplete"))
                  "an incomplete critic result blocks actionable handoff"
          }

          test "dead source locator and stale digest invalidate an actionable audit" {
              let root =
                  Path.Combine(
                      Path.GetTempPath(),
                      "fsgg-feedback-dead-" + Guid.NewGuid().ToString "N"
                  )

              try
                  Directory.CreateDirectory(Path.Combine(root, "feedback")) |> ignore
                  let reportPath = Path.Combine(root, "feedback", "report.md")

                  let evidence =
                      [| {| locator = "file:readiness/dead.log"
                            result = "verified"
                            sha256 = Some(String.replicate 64 "0") |} |]

                  let report =
                      validReport.Replace(
                          "file:readiness/build.log",
                          "file:readiness/dead.log"
                      )

                  let errors =
                      auditJson root reportPath report "actionable" evidence
                      |> validateActionabilityAudit root reportPath report

                  Expect.exists
                      errors
                      (fun error -> error.Contains("evidence file is missing"))
                      "a dead locator is not evidence"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)
          }

          test "an unrelated live audit locator cannot green the report's dead evidence" {
              let root =
                  Path.Combine(
                      Path.GetTempPath(),
                      "fsgg-feedback-substitution-" + Guid.NewGuid().ToString "N"
                  )

              try
                  Directory.CreateDirectory(Path.Combine(root, "feedback")) |> ignore
                  Directory.CreateDirectory(Path.Combine(root, "readiness")) |> ignore
                  let reportPath = Path.Combine(root, "feedback", "report.md")
                  let livePath = Path.Combine(root, "readiness", "green.log")
                  File.WriteAllText(livePath, "green")

                  let evidence =
                      [| {| locator = "file:readiness/green.log"
                            result = "verified"
                            sha256 = Some(sha256Text "green") |} |]

                  let errors =
                      auditJson root reportPath validReport "actionable" evidence
                      |> validateActionabilityAudit root reportPath validReport

                  Expect.exists
                      errors
                      (fun error -> error.Contains("report evidence has no matching check"))
                      "the dead report locator remains unchecked"

                  Expect.exists
                      errors
                      (fun error -> error.Contains("checked evidence is not declared"))
                      "the unrelated live locator is an unexplained substitution"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)
          }

          test "non-reproducing command cannot support actionable disposition" {
              let root = Path.GetTempPath()
              let reportPath = Path.Combine(root, "feedback", "report.md")
              let report =
                  validReport.Replace(
                      "file:readiness/build.log",
                      "command:dotnet test"
                  )

              let evidence =
                  [| {| locator = "command:dotnet test"
                        result = "non-reproducing"
                        sha256 = None |} |]

              let errors =
                  auditJson root reportPath report "actionable" evidence
                  |> validateActionabilityAudit root reportPath report

              Expect.exists
                  errors
                  (fun error -> error.Contains("cannot be actionable"))
                  "non-reproduction blocks actionable"
          }

          test "critic dispositions preserve invented-root-cause and missed-dedupe findings honestly" {
              let root = Path.GetTempPath()
              let reportPath = Path.Combine(root, "feedback", "report.md")

              for status, result in
                  [ "unsupported", "claim-only"; "duplicate", "verified" ] do
                  let report =
                      validReport.Replace(
                          "file:readiness/build.log",
                          "issue:FS-GG/FS.GG.Rendering#24"
                      )

                  let evidence =
                      [| {| locator = "issue:FS-GG/FS.GG.Rendering#24"
                            result = result
                            sha256 = None |} |]

                  let errors =
                      auditJson root reportPath report status evidence
                      |> validateActionabilityAudit root reportPath report

                  if status = "unsupported" then
                      Expect.exists
                          errors
                          (fun error -> error.Contains("remains unsupported"))
                          "invented root cause cannot become actionable"
                  else
                      Expect.isEmpty errors "existing issue can carry the duplicate"
          }

          test "well-grounded positive pattern requires positive-pattern disposition" {
              let report =
                  validReport
                      .Replace("**Kind:** friction", "**Kind:** positive-pattern")
                      .Replace(
                          "file:readiness/build.log",
                          "command:dotnet test"
                      )
              let root = Path.GetTempPath()
              let reportPath = Path.Combine(root, "feedback", "report.md")
              let evidence =
                  [| {| locator = "command:dotnet test"
                        result = "verified"
                        sha256 = None |} |]

              let audit = auditJson root reportPath report "positive-pattern" evidence

              Expect.isEmpty
                  (validateActionabilityAudit root reportPath report audit)
                  "verified positive pattern remains distinct"
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

          test "expected and observed fields that say the same thing fail" {
              let circular =
                  validReport.Replace(
                      "- **Expected:** one pass",
                      "- **Expected:** two passes"
                  )

              Expect.contains
                  (validateReportText circular)
                  "findings: §4.1 Expected and Observed must describe a delta"
                  "a circular expected/observed pair is not actionable"
          }

          test "Rogue2 green-testing claim without production route stays incomplete" {
              let rogue2 =
                  validReport
                      .Replace("Example friction", "Testing was green")
                      .Replace("one pass", "the player can leave the first room")
                      .Replace("two passes", "the test suite was green")
                      .Replace("file:readiness/build.log", "command:dotnet test")

              let root = Path.GetTempPath()
              let reportPath = Path.Combine(root, "feedback", "rogue2.md")
              let report = Path.GetRelativePath(root, reportPath).Replace(Path.DirectorySeparatorChar, '/')

              let audit =
                  JsonSerializer.Serialize(
                      {| auditSchema = 1
                         report = report
                         reportSha256 = sha256Text rogue2
                         criticMode = "fresh-context-subagent"
                         criticPromptVersion = "actionability-v1"
                         findings =
                          [ {| id = "§4.1"
                               status = "incomplete"
                               missingFacts =
                                [| "user input/reproduction path"
                                   "production route wiring evidence" |]
                               checkedEvidence =
                                [| {| locator = "command:dotnet test"
                                      result = "verified"
                                      sha256 = None |} |]
                               confidenceLimits =
                                [| "green tests do not establish production reachability" |] |} ] |}
                  )

              let errors = validateActionabilityAudit root reportPath rogue2 audit

              Expect.exists
                  errors
                  (fun error -> error.Contains("remains incomplete"))
                  "the missing user route and production wiring prevent actionable handoff"
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

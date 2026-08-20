module FeatureFeedbackReportSkillTests

open System
open System.Diagnostics
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

let private shellQuote (value: string) =
    "'" + value.Replace("'", "'\"'\"'") + "'"

let private runValidateThroughTail root reportPath auditPath =
    let scriptPath =
        Path.Combine(repositoryRoot, "template", "feedback-report", "skill", "scripts", "feedback-tool.fsx")

    let command =
        sprintf
            "dotnet fsi %s -- validate %s --audit %s | tail -1"
            (shellQuote scriptPath)
            (shellQuote reportPath)
            (shellQuote auditPath)

    let startInfo = ProcessStartInfo("bash")
    startInfo.WorkingDirectory <- root
    startInfo.UseShellExecute <- false
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    startInfo.ArgumentList.Add "-c"
    startInfo.ArgumentList.Add command

    match Process.Start startInfo with
    | null -> failwith "could not start the feedback-tool tail fixture"
    | child ->
        use child = child
        let stdout = child.StandardOutput.ReadToEndAsync()
        let stderr = child.StandardError.ReadToEndAsync()
        child.WaitForExit()
        child.ExitCode, stdout.Result.TrimEnd(), stderr.Result

let private runValidate root reportPath auditPath =
    let scriptPath =
        Path.Combine(repositoryRoot, "template", "feedback-report", "skill", "scripts", "feedback-tool.fsx")

    let startInfo = ProcessStartInfo("dotnet")
    startInfo.WorkingDirectory <- root
    startInfo.UseShellExecute <- false
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    [ "fsi"; scriptPath; "--"; "validate"; reportPath; "--audit"; auditPath ]
    |> List.iter startInfo.ArgumentList.Add

    match Process.Start startInfo with
    | null -> failwith "could not start feedback-tool"
    | child ->
        use child = child
        let stdout = child.StandardOutput.ReadToEndAsync()
        let stderr = child.StandardError.ReadToEndAsync()
        child.WaitForExit()
        child.ExitCode, stdout.Result, stderr.Result

let private runProcess workingDirectory executable arguments =
    let startInfo = ProcessStartInfo(executable)
    startInfo.WorkingDirectory <- workingDirectory
    startInfo.UseShellExecute <- false
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    arguments |> List.iter startInfo.ArgumentList.Add

    match Process.Start startInfo with
    | null -> failwithf "could not start %s" executable
    | child ->
        use child = child
        let stdout = child.StandardOutput.ReadToEndAsync()
        let stderr = child.StandardError.ReadToEndAsync()
        child.WaitForExit()
        child.ExitCode, stdout.Result, stderr.Result

let private git root arguments =
    let exitCode, stdout, stderr = runProcess root "git" arguments

    if exitCode <> 0 then
        failwithf "git %s failed: %s" (String.concat " " arguments) stderr

    stdout.Trim()

let private writeFile (root: string) (relative: string) (text: string) =
    let full = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar))

    Path.GetDirectoryName full
    |> Option.ofObj
    |> Option.iter (Directory.CreateDirectory >> ignore)

    File.WriteAllText(full, text)

/// #1243 — the fixture below needs a REAL repository, because the defect it exists for
/// is invisible without one: `check-invalidation --base/--head` derived its changed
/// paths from refs and its audit index from the working tree, and no fixture built out
/// of a directory and a path list can tell those two trees apart. Identity and signing
/// are configured on the repository so the fixture never depends on (or is broken by)
/// the machine's global Git configuration.
let private initFixtureRepository (root: string) =
    git root [ "init"; "-q" ] |> ignore
    git root [ "config"; "user.name"; "fsgg-feedback-test" ] |> ignore
    git root [ "config"; "user.email"; "fsgg-feedback-test@example.invalid" ] |> ignore
    git root [ "config"; "commit.gpgsign"; "false" ] |> ignore

/// Commit the current worktree onto a fresh branch started at `start`, and answer the
/// new commit's object name.
let private commitOnto (root: string) (branch: string) (start: string) (mutate: unit -> unit) =
    git root [ "checkout"; "-q"; "-B"; branch; start ] |> ignore
    mutate ()
    git root [ "add"; "-A" ] |> ignore
    git root [ "commit"; "-q"; "-m"; branch ] |> ignore
    git root [ "rev-parse"; "HEAD" ]

let private runPackagedValidate root reportPath auditPath =
    runProcess
        root
        "dotnet"
        [ "fsi"
          ".agents/skills/fs-gg-feedback-report/scripts/feedback-tool.fsx"
          "--"
          "validate"
          reportPath
          "--audit"
          auditPath ]

let private copyPackagedFeedbackSkill root =
    let destination =
        Path.Combine(root, ".agents", "skills", "fs-gg-feedback-report", "scripts")

    Directory.CreateDirectory destination |> ignore

    for file in [ "FeedbackReportTool.fs"; "feedback-tool.fsx" ] do
        File.Copy(
            Path.Combine(repositoryRoot, "template", "feedback-report", "skill", "scripts", file),
            Path.Combine(destination, file)
        )

let private commitFixture root message =
    git root [ "add"; "." ] |> ignore
    git root [ "commit"; "-q"; "-m"; message ] |> ignore
    git root [ "rev-parse"; "HEAD" ]

let private reportAt commit locator =
    validReport
        .Replace("commit: abc123", "commit: " + commit)
        .Replace("file:readiness/build.log", locator)

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

          test "packaged feedback skill validates file evidence from the report commit, not a dirty checkout" {
              let root = Path.Combine(Path.GetTempPath(), "fsgg-feedback-clean-head-" + Guid.NewGuid().ToString "N")
              let clone name = Path.Combine(Path.GetTempPath(), name + "-" + Guid.NewGuid().ToString "N")
              let created = ResizeArray<string>()
              created.Add root

              let prepareClone name =
                  let destination = clone name
                  git (Path.GetTempPath()) [ "clone"; "-q"; root; destination ] |> ignore
                  created.Add destination
                  destination

              try
                  Directory.CreateDirectory(Path.Combine(root, "readiness")) |> ignore
                  copyPackagedFeedbackSkill root
                  File.WriteAllText(Path.Combine(root, "readiness", "render-baseline.json"), "committed-render-evidence")
                  File.WriteAllText(
                      Path.Combine(root, "readiness", "generate-performance.fsx"),
                      "open System.IO\nFile.WriteAllText(\"readiness/generated-performance.json\", \"generated-performance-evidence\")"
                  )
                  git root [ "init"; "-q" ] |> ignore
                  git root [ "config"; "user.email"; "fixture@example.test" ] |> ignore
                  git root [ "config"; "user.name"; "Fixture" ] |> ignore
                  let reportHead = commitFixture root "seed committed render evidence"
                  let report = reportAt reportHead "file:readiness/render-baseline.json"
                  let reportPath = Path.Combine(root, "feedback", "report.md")
                  let auditPath = Path.Combine(root, "feedback", "report.audit.json")
                  Directory.CreateDirectory(Path.Combine(root, "feedback")) |> ignore
                  File.WriteAllText(reportPath, report)
                  File.WriteAllText(
                      auditPath,
                      auditJson root reportPath report "actionable"
                          [| {| locator = "file:readiness/render-baseline.json"
                                result = "verified"
                                sha256 = Some(sha256Text "committed-render-evidence") |} |]
                  )
                  commitFixture root "add feedback audit" |> ignore

                  let committed = prepareClone "fsgg-feedback-committed"
                  let goodExit, goodOutput, goodError = runPackagedValidate committed "feedback/report.md" "feedback/report.audit.json"
                  Expect.equal goodExit 0 (sprintf "committed evidence validates from a clean checkout: %s" goodError)
                  Expect.stringContains goodOutput "PASS" "the packaged skill reports a successful validation"

                  let untracked = prepareClone "fsgg-feedback-untracked"
                  let untrackedReport = reportAt reportHead "file:readiness/generated-performance.json"
                  File.WriteAllText(Path.Combine(untracked, "feedback", "report.md"), untrackedReport)
                  File.WriteAllText(Path.Combine(untracked, "readiness", "generated-performance.json"), "generated-performance-evidence")
                  File.WriteAllText(
                      Path.Combine(untracked, "feedback", "report.audit.json"),
                      auditJson untracked (Path.Combine(untracked, "feedback", "report.md")) untrackedReport "actionable"
                          [| {| locator = "file:readiness/generated-performance.json"
                                result = "verified"
                                sha256 = Some(sha256Text "generated-performance-evidence") |} |]
                  )
                  let untrackedExit, _, untrackedError = runPackagedValidate untracked "feedback/report.md" "feedback/report.audit.json"
                  Expect.equal untrackedExit 1 "a locally generated but untracked artifact fails"
                  Expect.stringContains untrackedError "untracked at report head" "the diagnostic distinguishes dirty generated evidence"
                  Expect.stringContains untrackedError "command: locator" "the diagnostic gives bounded remediation"

                  let ignored = prepareClone "fsgg-feedback-ignored"
                  File.WriteAllText(Path.Combine(ignored, ".gitignore"), "readiness/ignored-performance.json\n")
                  File.WriteAllText(Path.Combine(ignored, "readiness", "ignored-performance.json"), "ignored")
                  let ignoredReport = reportAt reportHead "file:readiness/ignored-performance.json"
                  File.WriteAllText(Path.Combine(ignored, "feedback", "report.md"), ignoredReport)
                  File.WriteAllText(
                      Path.Combine(ignored, "feedback", "report.audit.json"),
                      auditJson ignored (Path.Combine(ignored, "feedback", "report.md")) ignoredReport "actionable"
                          [| {| locator = "file:readiness/ignored-performance.json"; result = "verified"; sha256 = Some(sha256Text "ignored") |} |]
                  )
                  let ignoredExit, _, ignoredError = runPackagedValidate ignored "feedback/report.md" "feedback/report.audit.json"
                  Expect.equal ignoredExit 1 "an ignored artifact fails"
                  Expect.stringContains ignoredError "ignored at report head" "the diagnostic distinguishes ignored evidence"

                  let absent = prepareClone "fsgg-feedback-absent"
                  let absentReport = reportAt reportHead "file:readiness/absent-performance.json"
                  File.WriteAllText(Path.Combine(absent, "feedback", "report.md"), absentReport)
                  File.WriteAllText(
                      Path.Combine(absent, "feedback", "report.audit.json"),
                      auditJson absent (Path.Combine(absent, "feedback", "report.md")) absentReport "actionable"
                          [| {| locator = "file:readiness/absent-performance.json"; result = "verified"; sha256 = Some(sha256Text "absent") |} |]
                  )
                  let absentExit, _, absentError = runPackagedValidate absent "feedback/report.md" "feedback/report.audit.json"
                  Expect.equal absentExit 1 "an absent artifact fails"
                  Expect.stringContains absentError "absent at report head" "the diagnostic distinguishes an absent path"

                  let command = prepareClone "fsgg-feedback-command"
                  let generatorExit, _, generatorError = runProcess command "dotnet" [ "fsi"; "readiness/generate-performance.fsx" ]
                  Expect.equal generatorExit 0 (sprintf "the clean-checkout generator runs: %s" generatorError)
                  let commandLocator = "command:dotnet fsi readiness/generate-performance.fsx && inspect readiness/generated-performance.json"
                  let commandReport = reportAt reportHead commandLocator
                  File.WriteAllText(Path.Combine(command, "feedback", "report.md"), commandReport)
                  File.WriteAllText(
                      Path.Combine(command, "feedback", "report.audit.json"),
                      auditJson command (Path.Combine(command, "feedback", "report.md")) commandReport "actionable"
                          [| {| locator = commandLocator; result = "verified"; sha256 = None |} |]
                  )
                  let commandExit, commandOutput, commandError = runPackagedValidate command "feedback/report.md" "feedback/report.audit.json"
                  Expect.equal commandExit 0 (sprintf "a command locator can describe generated performance evidence: %s" commandError)
                  Expect.stringContains commandOutput "PASS" "the command-locator fixture remains valid"

                  let nonGit = clone "fsgg-feedback-no-git"
                  created.Add nonGit
                  Directory.CreateDirectory(Path.Combine(nonGit, "feedback")) |> ignore
                  Directory.CreateDirectory(Path.Combine(nonGit, "readiness")) |> ignore
                  copyPackagedFeedbackSkill nonGit
                  let nonGitReport = reportAt "unresolvable-head" "file:readiness/render-baseline.json"
                  File.WriteAllText(Path.Combine(nonGit, "readiness", "render-baseline.json"), "local-only")
                  File.WriteAllText(Path.Combine(nonGit, "feedback", "report.md"), nonGitReport)
                  File.WriteAllText(
                      Path.Combine(nonGit, "feedback", "report.audit.json"),
                      auditJson nonGit (Path.Combine(nonGit, "feedback", "report.md")) nonGitReport "actionable"
                          [| {| locator = "file:readiness/render-baseline.json"; result = "verified"; sha256 = Some(sha256Text "local-only") |} |]
                  )
                  let nonGitExit, _, nonGitError = runPackagedValidate nonGit "feedback/report.md" "feedback/report.audit.json"
                  Expect.equal nonGitExit 1 "an unknown Git workspace fails closed"
                  Expect.stringContains nonGitError "cannot establish Git workspace state" "unknown is not treated as a clean checkout"
              finally
                  for path in created |> Seq.distinct do
                      if Directory.Exists path then Directory.Delete(path, true)
          }

          test "validate emits a terminal PASS or FAIL verdict through tail while retaining stderr detail and exit codes" {
              let root =
                  Path.Combine(Path.GetTempPath(), "fsgg-feedback-verdict-" + Guid.NewGuid().ToString "N")

              try
                  Directory.CreateDirectory(Path.Combine(root, "feedback")) |> ignore
                  Directory.CreateDirectory(Path.Combine(root, "readiness")) |> ignore
                  let validReportPath = Path.Combine(root, "feedback", "valid.md")
                  let validAuditPath = Path.Combine(root, "feedback", "valid.audit.json")
                  let evidencePath = Path.Combine(root, "readiness", "build.log")
                  File.WriteAllText(evidencePath, "green")
                  git root [ "init"; "-q" ] |> ignore
                  git root [ "config"; "user.email"; "fixture@example.test" ] |> ignore
                  git root [ "config"; "user.name"; "Fixture" ] |> ignore
                  let reportHead = commitFixture root "seed verdict evidence"
                  let committedReport = reportAt reportHead "file:readiness/build.log"
                  File.WriteAllText(validReportPath, committedReport)

                  let evidence =
                      [| {| locator = "file:readiness/build.log"
                            result = "verified"
                            sha256 = Some(sha256Text "green") |} |]

                  File.WriteAllText(validAuditPath, auditJson root validReportPath committedReport "actionable" evidence)

                  let successExit, successLastLine, successError =
                      runValidateThroughTail root validReportPath validAuditPath

                  Expect.equal successExit 0 "a valid report keeps its successful pipeline exit code"
                  Expect.stringContains
                      successLastLine
                      "PASS"
                      $"the last stdout line explicitly says PASS. stderr: {successError}"
                  Expect.isEmpty successError "a valid report produces no stderr detail"

                  let invalidReportPath = Path.Combine(root, "feedback", "invalid.md")
                  let invalidAuditPath = Path.Combine(root, "feedback", "invalid.audit.json")
                  File.WriteAllText(invalidReportPath, "deliberately invalid report")
                  File.WriteAllText(invalidAuditPath, "{}")

                  let pipelineExit, failureLastLine, failureDetail =
                      runValidateThroughTail root invalidReportPath invalidAuditPath

                  // `tail` still exits zero: the regression is that its captured line must now carry FAIL.
                  Expect.equal pipelineExit 0 "tail preserves its own successful pipeline exit code"
                  Expect.stringContains failureLastLine "FAIL" "the last stdout line explicitly says FAIL"
                  Expect.stringContains failureDetail "frontmatter:" "per-error validation detail remains on stderr"

                  let directExit, directOutput, directError =
                      runValidate root invalidReportPath invalidAuditPath

                  Expect.equal directExit 1 "the validator's failure exit code remains unchanged"
                  Expect.stringContains directOutput "FAIL" "the direct stdout verdict remains explicit"
                  Expect.stringContains directError "frontmatter:" "direct validation still reports detail on stderr"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)
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

          test "stale audit-binding ledger citation is reported rather than rejected" {
              let root =
                  Path.Combine(Path.GetTempPath(), "fsgg-feedback-ledger-" + Guid.NewGuid().ToString "N")

              try
                  Directory.CreateDirectory(Path.Combine(root, "feedback")) |> ignore
                  Directory.CreateDirectory(Path.Combine(root, "scripts")) |> ignore
                  let reportPath = Path.Combine(root, "feedback", "report.md")
                  let ledgerPath = Path.Combine(root, "scripts", "audit-binding-exceptions.json")
                  let locator = "file:scripts/audit-binding-exceptions.json"
                  let report = validReport.Replace("file:readiness/build.log", locator)
                  File.WriteAllText(reportPath, report)
                  File.WriteAllText(ledgerPath, "changed after the audit")

                  let audit =
                      auditJson root reportPath report "actionable"
                          [| {| locator = locator
                                result = "verified"
                                sha256 = Some(sha256Text "the old ledger bytes") |} |]

                  let result = validateActionabilityAuditDetailed root reportPath report audit
                  Expect.isEmpty result.errors "the self-rewriting ledger has no stable digest to compare"
                  Expect.equal result.notBound.Length 1 "the exemption stays observable"
                  Expect.equal result.notBound.Head.locator locator "the cited locator is reported"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)
          }

          test "symlinked workspace root recognizes the ledger by its resolved path" {
              if not (OperatingSystem.IsLinux()) then
                  skiptest "Linux regression for resolved ledger paths"

              let realRoot =
                  Path.Combine(Path.GetTempPath(), "fsgg-feedback-ledger-real-" + Guid.NewGuid().ToString "N")

              let linkedRoot =
                  Path.Combine(Path.GetTempPath(), "fsgg-feedback-ledger-link-" + Guid.NewGuid().ToString "N")

              try
                  Directory.CreateDirectory(Path.Combine(realRoot, "feedback")) |> ignore
                  Directory.CreateDirectory(Path.Combine(realRoot, "scripts")) |> ignore
                  Directory.CreateSymbolicLink(linkedRoot, realRoot) |> ignore
                  let reportPath = Path.Combine(linkedRoot, "feedback", "report.md")
                  let locator = "file:scripts/audit-binding-exceptions.json"
                  let report = validReport.Replace("file:readiness/build.log", locator)
                  File.WriteAllText(reportPath, report)
                  File.WriteAllText(Path.Combine(realRoot, "scripts", "audit-binding-exceptions.json"), "changed")

                  let audit =
                      auditJson linkedRoot reportPath report "actionable"
                          [| {| locator = locator
                                result = "verified"
                                sha256 = Some(sha256Text "old") |} |]

                  let result = validateActionabilityAuditDetailed linkedRoot reportPath report audit
                  Expect.isEmpty result.errors "canonical root and candidate paths agree"
                  Expect.equal result.notBound.Length 1 "the resolved ledger remains visible"
              finally
                  if Directory.Exists linkedRoot then
                      Directory.Delete(linkedRoot, true)

                  if Directory.Exists realRoot then
                      Directory.Delete(realRoot, true)
          }

          test "stale non-ledger evidence remains fail-closed" {
              let root =
                  Path.Combine(Path.GetTempPath(), "fsgg-feedback-stale-" + Guid.NewGuid().ToString "N")

              try
                  Directory.CreateDirectory(Path.Combine(root, "feedback")) |> ignore
                  Directory.CreateDirectory(Path.Combine(root, "readiness")) |> ignore
                  let reportPath = Path.Combine(root, "feedback", "report.md")
                  File.WriteAllText(reportPath, validReport)
                  File.WriteAllText(Path.Combine(root, "readiness", "build.log"), "changed")

                  let audit =
                      auditJson root reportPath validReport "actionable"
                          [| {| locator = "file:readiness/build.log"
                                result = "verified"
                                sha256 = Some(sha256Text "old") |} |]

                  let result = validateActionabilityAuditDetailed root reportPath validReport audit
                  Expect.exists result.errors (fun error -> error.Contains("evidence digest is stale")) "ordinary evidence stays bound"
                  Expect.isEmpty result.notBound "the narrow exemption does not grow"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)
          }

          test "scheme-prefixed absolute paths and secret arguments fail closed" {
              let root = Path.GetTempPath()
              let reportPath = Path.Combine(root, "feedback", "report.md")

              let locator =
                  "command:dotnet test --results-directory /home/user/private --token=secret-value"

              let report =
                  validReport.Replace("file:readiness/build.log", locator)

              let evidence =
                  [| {| locator = locator
                        result = "verified"
                        sha256 = None |} |]

              let errors =
                  auditJson root reportPath report "actionable" evidence
                  |> validateActionabilityAudit root reportPath report

              Expect.exists
                  errors
                  (fun error -> error.Contains("absolute path or secret material"))
                  "a scheme prefix cannot hide private paths or credentials"
          }

          test "workspace-relative evidence cannot escape through a symlink" {
              if not (OperatingSystem.IsLinux()) then
                  skiptest "Linux regression for realpath containment"

              let root =
                  Path.Combine(
                      Path.GetTempPath(),
                      "fsgg-feedback-symlink-" + Guid.NewGuid().ToString "N"
                  )

              let external =
                  Path.Combine(
                      Path.GetTempPath(),
                      "fsgg-feedback-external-" + Guid.NewGuid().ToString "N"
                  )

              try
                  Directory.CreateDirectory(Path.Combine(root, "feedback")) |> ignore
                  Directory.CreateDirectory external |> ignore
                  let externalEvidence = Path.Combine(external, "outside.log")
                  File.WriteAllText(externalEvidence, "outside")

                  Directory.CreateSymbolicLink(Path.Combine(root, "readiness"), external)
                  |> ignore

                  let reportPath = Path.Combine(root, "feedback", "report.md")

                  let evidence =
                      [| {| locator = "file:readiness/outside.log"
                            result = "verified"
                            sha256 = Some(sha256Text "outside") |} |]

                  let report =
                      validReport.Replace(
                          "file:readiness/build.log",
                          "file:readiness/outside.log"
                      )

                  let errors =
                      auditJson root reportPath report "actionable" evidence
                      |> validateActionabilityAudit root reportPath report

                  Expect.exists
                      errors
                      (fun error -> error.Contains("workspace-relative file"))
                      "an in-workspace symlink cannot validate bytes outside the workspace"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)

                  if Directory.Exists external then
                      Directory.Delete(external, true)
          }

          test "null evidence locator is a validation error rather than an exception" {
              let root = Path.GetTempPath()
              let reportPath = Path.Combine(root, "feedback", "report.md")

              let evidence =
                  [| {| locator = "file:readiness/build.log"
                        result = "verified"
                        sha256 = None |} |]

              let audit =
                  (auditJson root reportPath validReport "actionable" evidence)
                      .Replace("\"locator\":\"file:readiness/build.log\"", "\"locator\":null")

              let errors =
                  validateActionabilityAudit root reportPath validReport audit

              Expect.exists
                  errors
                  (fun error -> error.Contains("evidence locator must not be empty"))
                  "malformed JSON fields fail closed without throwing"
          }

          test "null finding id is a validation error rather than an exception" {
              let root = Path.GetTempPath()
              let reportPath = Path.Combine(root, "feedback", "report.md")

              let evidence =
                  [| {| locator = "file:readiness/build.log"
                        result = "verified"
                        sha256 = Some(sha256Text "green") |} |]

              let audit =
                  (auditJson root reportPath validReport "actionable" evidence)
                      .Replace("\"id\":\"\\u00A74.1\"", "\"id\":null")

              let errors =
                  validateActionabilityAudit root reportPath validReport audit

              Expect.contains
                  errors
                  "audit: finding id must not be empty"
                  "a null finding identity cannot bypass coverage or throw"
          }

          test "null status and result plus malformed digest all fail closed" {
              let root =
                  Path.Combine(
                      Path.GetTempPath(),
                      "fsgg-feedback-malformed-" + Guid.NewGuid().ToString "N"
                  )

              try
                  Directory.CreateDirectory(Path.Combine(root, "feedback")) |> ignore
                  Directory.CreateDirectory(Path.Combine(root, "readiness")) |> ignore
                  let reportPath = Path.Combine(root, "feedback", "report.md")
                  File.WriteAllText(Path.Combine(root, "readiness", "build.log"), "green")

                  let expectedDigest = sha256Text "green"

                  let evidence =
                      [| {| locator = "file:readiness/build.log"
                            result = "verified"
                            sha256 = Some expectedDigest |} |]

                  let audit =
                      (auditJson root reportPath validReport "actionable" evidence)
                          .Replace("\"status\":\"actionable\"", "\"status\":null")
                          .Replace("\"result\":\"verified\"", "\"result\":null")
                          .Replace(
                              $"\"sha256\":\"{expectedDigest}\"",
                              "\"sha256\":\"not-a-digest\""
                          )

                  let errors =
                      validateActionabilityAudit root reportPath validReport audit

                  Expect.exists
                      errors
                      (fun error -> error.Contains("unknown status ''"))
                      "a null critic status is invalid"

                  Expect.exists
                      errors
                      (fun error -> error.Contains("unknown result ''"))
                      "a null evidence result is invalid"

                  Expect.exists
                      errors
                      (fun error -> error.Contains("sha256 must be 64 lowercase hex"))
                      "a malformed evidence digest is invalid"
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

                  Expect.isEmpty
                      (validateCheckpointState root "001-example")
                      "eventful checkpoint state remains valid"

                  let line = File.ReadAllText path
                  Expect.stringContains line "restore required a retry" "summary is retained"
                  Expect.stringContains line "dependencies-build" "surface is retained"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)
          }

          test "valid zero-event activation receipt proves an exercised event-free cycle" {
              let root =
                  Path.Combine(
                      Path.GetTempPath(),
                      "fsgg-feedback-activation-" + Guid.NewGuid().ToString "N"
                  )

              try
                  let path =
                      appendZeroEventActivation
                          root
                          "001-example"
                          [ "scaffold-onboarding"
                            "implementation-test-evidence"
                            "verify-ship-pr" ]
                          [ "command:dotnet test"
                            "file:readiness/ship-summary.json" ]
                          "No reusable friction, gaps, or positive patterns qualified."

                  Expect.isTrue (File.Exists path) "activation receipt was created"

                  Expect.isFalse
                      (File.Exists(Path.Combine(root, "feedback", "checkpoints", "001-example.jsonl")))
                      "zero-event activation does not fabricate a checkpoint event"

                  Expect.isEmpty
                      (validateCheckpointState root "001-example")
                      "valid zero-event activation is a complete checkpoint state"

                  use document = JsonDocument.Parse(File.ReadAllText path)
                  let receipt = document.RootElement

                  Expect.equal
                      (receipt.GetProperty("activationSchema").GetInt32())
                      1
                      "receipt schema is explicit"

                  Expect.equal
                      (receipt.GetProperty("receiptKind").GetString())
                      "zero-event-activation"
                      "receipt cannot masquerade as a finding"

                  Expect.equal
                      (receipt.GetProperty("exercisedPhases").GetArrayLength())
                      3
                      "all exercised phases are retained"

                  Expect.equal
                      (receipt.GetProperty("evidence").GetArrayLength())
                      2
                      "activation evidence is retained"

                  Expect.isFalse
                      (receipt.TryGetProperty("kind") |> fst)
                      "event-only kind is not fabricated"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)
          }

          test "zero-event receipt rejects event-only and arbitrary private fields" {
              let root =
                  Path.Combine(
                      Path.GetTempPath(),
                      "fsgg-feedback-strict-schema-" + Guid.NewGuid().ToString "N"
                  )

              try
                  let path =
                      appendZeroEventActivation
                          root
                          "001-example"
                          [ "verify-ship-pr" ]
                          [ "command:dotnet test" ]
                          "No material event qualified."

                  let mutated =
                      File.ReadAllText(path).TrimEnd().TrimEnd('}')
                      + ""","kind":"defect","owner":"rendering","secret":"--token=leak"}"""

                  File.WriteAllText(path, mutated)
                  let errors = validateCheckpointState root "001-example"

                  for forbidden in [ "kind"; "owner"; "secret" ] do
                      Expect.exists
                          errors
                          (fun error -> error.Contains(sprintf "unknown property '%s'" forbidden))
                          (sprintf "strict schema rejects %s" forbidden)
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)
          }

          test "missing checkpoint state is distinct from a verified zero-event receipt" {
              let root =
                  Path.Combine(
                      Path.GetTempPath(),
                      "fsgg-feedback-missing-" + Guid.NewGuid().ToString "N"
                  )

              let errors = validateCheckpointState root "001-example"

              Expect.exists
                  errors
                  (fun error ->
                      error.Contains(
                          "missing both checkpoint events and a zero-event activation receipt"
                      ))
                  "absence remains fail-closed rather than being interpreted as zero events"
          }

          test "empty checkpoint JSONL cannot impersonate an intentional zero-event cycle" {
              let root =
                  Path.Combine(
                      Path.GetTempPath(),
                      "fsgg-feedback-empty-" + Guid.NewGuid().ToString "N"
                  )

              try
                  let directory = Path.Combine(root, "feedback", "checkpoints")
                  Directory.CreateDirectory directory |> ignore
                  File.WriteAllText(Path.Combine(directory, "001-example.jsonl"), "")
                  let errors = validateCheckpointState root "001-example"

                  Expect.exists
                      errors
                      (fun error -> error.Contains("contains no events"))
                      "an empty event file requires an explicit activation receipt"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)
          }

          test "checkpoint event state cannot escape the workspace through a symlink" {
              if not (OperatingSystem.IsLinux()) then
                  skiptest "Linux regression for realpath containment"

              let root =
                  Path.Combine(
                      Path.GetTempPath(),
                      "fsgg-feedback-event-link-" + Guid.NewGuid().ToString "N"
                  )

              let external =
                  Path.Combine(
                      Path.GetTempPath(),
                      "fsgg-feedback-event-external-" + Guid.NewGuid().ToString "N"
                  )

              try
                  let externalPath =
                      appendCheckpoint
                          external
                          "001-example"
                          "verify"
                          "testing"
                          "friction"
                          "external event"
                          "command:dotnet test"
                          "none"
                          "FS-GG/FS.GG.Rendering"

                  let directory = Path.Combine(root, "feedback", "checkpoints")
                  Directory.CreateDirectory directory |> ignore

                  File.CreateSymbolicLink(
                      Path.Combine(directory, "001-example.jsonl"),
                      externalPath
                  )
                  |> ignore

                  let errors = validateCheckpointState root "001-example"

                  Expect.exists
                      errors
                      (fun error -> error.Contains("resolves outside the workspace"))
                      "event JSONL receives the same containment check as activation receipts"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)

                  if Directory.Exists external then
                      Directory.Delete(external, true)
          }

          test "malformed zero-event activation receipt fails closed" {
              let root =
                  Path.Combine(
                      Path.GetTempPath(),
                      "fsgg-feedback-malformed-" + Guid.NewGuid().ToString "N"
                  )

              try
                  let path = activationReceiptPath root "001-example"
                  Directory.CreateDirectory(Path.Combine(root, "feedback", "checkpoints"))
                  |> ignore
                  File.WriteAllText(path, """{"activationSchema":1,"cycle":""")
                  let errors = validateCheckpointState root "001-example"

                  Expect.exists
                      errors
                      (fun error -> error.Contains("malformed JSON"))
                      "truncated receipt is not accepted as activation evidence"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)
          }

          test "commit-time audit invalidation check is selective, deterministic, and fail-closed" {
              let root = Path.Combine(Path.GetTempPath(), "fsgg-feedback-invalidation-" + Guid.NewGuid().ToString "N")

              try
                  let audits = Path.Combine(root, "feedback", "audits")
                  Directory.CreateDirectory audits |> ignore
                  let reportPath = Path.Combine(root, "feedback", "report.md")
                  let evidence locator = [| {| locator = locator; result = "verified"; sha256 = Some(sha256Text "old") |} |]

                  File.WriteAllText(Path.Combine(audits, "positive.audit.json"), auditJson root reportPath validReport "actionable" (evidence "file:src/Changed.fs"))
                  File.WriteAllText(Path.Combine(audits, "negative.audit.json"), auditJson root reportPath validReport "actionable" (evidence "file:src/Untouched.fs"))
                  File.WriteAllText(Path.Combine(audits, "rename.audit.json"), auditJson root reportPath validReport "actionable" (evidence "file:src/Old.fs"))
                  File.WriteAllText(Path.Combine(audits, "copy.audit.json"), auditJson root reportPath validReport "actionable" (evidence "file:src/Copied.fs"))
                  File.WriteAllText(Path.Combine(audits, "deleted.audit.json"), auditJson root reportPath validReport "actionable" (evidence "file:src/Deleted.fs"))

                  let positive = findInvalidatedAuditBindings root [ "src/Changed.fs" ]
                  Expect.isEmpty positive.errors "a valid path-index scan has no parse errors"
                  Expect.equal positive.invalidated.Length 1 "only the touched citation is selected"
                  Expect.equal positive.invalidated.Head.findingId "§4.1" "diagnostic names the finding"
                  Expect.equal positive.invalidated.Head.report "feedback/report.md" "diagnostic names the merged report"

                  let renameAndDelete =
                      changedPathsFromNameStatus "R100\tsrc/Old.fs\tsrc/Renamed.fs\nC100\tsrc/Source.fs\tsrc/Copied.fs\nD\tsrc/Deleted.fs\n"

                  Expect.sequenceEqual
                      renameAndDelete
                      [ "src/Copied.fs"; "src/Deleted.fs"; "src/Old.fs"; "src/Renamed.fs"; "src/Source.fs" ]
                      "commit name-status input indexes both rename/copy sides and deleted paths"

                  let commitMutation = findInvalidatedAuditBindings root renameAndDelete
                  Expect.isEmpty commitMutation.errors "full name-status derived input remains a valid index query"
                  Expect.sequenceEqual
                      (commitMutation.invalidated |> List.map (fun item -> item.audit, item.report, item.findingId, item.path))
                      [ "feedback/audits/copy.audit.json", "feedback/report.md", "§4.1", "src/Copied.fs"
                        "feedback/audits/deleted.audit.json", "feedback/report.md", "§4.1", "src/Deleted.fs"
                        "feedback/audits/rename.audit.json", "feedback/report.md", "§4.1", "src/Old.fs" ]
                      "rename old side, copy side, and deletion each name their audit/report/finding deterministically"

                  let largeNameStatus = String.replicate 20000 "M\tsrc/Unrelated.fs\n" |> changedPathsFromNameStatus
                  Expect.equal largeNameStatus [ "src/Unrelated.fs" ] "large name-status output drains and deduplicates deterministically"

                  let negative = findInvalidatedAuditBindings root [ "src/Other.fs" ]
                  Expect.isEmpty negative.invalidated "unrelated paths do not revalidate or invalidate audits"

                  File.WriteAllText(Path.Combine(audits, "malformed.audit.json"), "{")
                  let malformed = findInvalidatedAuditBindings root [ "src/Other.fs" ]
                  Expect.exists malformed.errors (fun error -> error.Contains("malformed audit feedback/audits/malformed.audit.json")) "malformed audit metadata fails closed"

                  File.WriteAllText(Path.Combine(audits, "malformed.audit.json"), """{"auditSchema":0,"report":"","reportSha256":"old","findings":[]}""")
                  let malformedStructure = findInvalidatedAuditBindings root [ "src/Other.fs" ]
                  Expect.exists malformedStructure.errors (fun error -> error.Contains("auditSchema must be 1")) "schema-invalid audits cannot render a safe empty index"

                  File.Delete(Path.Combine(audits, "malformed.audit.json"))

                  for index in 1 .. 200 do
                      File.WriteAllText(Path.Combine(audits, sprintf "scale-%03d.audit.json" index), auditJson root reportPath validReport "actionable" (evidence "file:src/Changed.fs"))

                  let scale = findInvalidatedAuditBindings root [ "src/Changed.fs" ]
                  Expect.equal scale.invalidated.Length 201 "all and only indexed citations are selected at scale"
                  Expect.sequenceEqual
                      (scale.invalidated |> List.map (fun item -> item.audit))
                      (scale.invalidated |> List.map (fun item -> item.audit) |> List.sort)
                      "scale diagnostics retain deterministic audit ordering"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)
          }

          test "audit-binding exception ledger is exact, versioned, materialized, and fails closed" {
              let root = Path.Combine(Path.GetTempPath(), "fsgg-feedback-exceptions-" + Guid.NewGuid().ToString "N")

              try
                  let audits = Path.Combine(root, "feedback", "audits")
                  let scripts = Path.Combine(root, "scripts")
                  let replacement = "readiness/replacement.txt"
                  Directory.CreateDirectory audits |> ignore
                  Directory.CreateDirectory scripts |> ignore
                  Directory.CreateDirectory(Path.Combine(root, "readiness")) |> ignore
                  let reportPath = Path.Combine(root, "feedback", "report.md")
                  let priorDigest = sha256Text "old"
                  let replacementDigest = sha256Text "current"
                  let evidence = [| {| locator = "file:src/Changed.fs"; result = "verified"; sha256 = Some priorDigest |} |]
                  File.WriteAllText(Path.Combine(audits, "binding.audit.json"), auditJson root reportPath validReport "actionable" evidence)
                  File.WriteAllText(Path.Combine(root, replacement), "current")

                  let entry =
                      {| id = "replacement-001"
                         audit = "feedback/audits/binding.audit.json"
                         findingId = "§4.1"
                         locator = "file:src/Changed.fs"
                         path = "src/Changed.fs"
                         priorSha256 = priorDigest
                         replacementPath = replacement
                         replacementSha256 = replacementDigest
                         evidenceLocator = "command:dotnet test && inspect readiness/replacement.txt" |}

                  let ledger entries = JsonSerializer.Serialize({| schemaVersion = 1; exceptions = entries |})
                  let ledgerPath = Path.Combine(scripts, "audit-binding-exceptions.json")
                  File.WriteAllText(ledgerPath, ledger [| entry |])

                  let accepted = findInvalidatedAuditBindings root [ "src/Changed.fs" ]
                  Expect.isEmpty accepted.errors "one exact, current exception is valid"
                  Expect.isEmpty accepted.invalidated "the exact exception dispositions its one invalidation"
                  Expect.equal accepted.dispositions.Length 1 "one applied disposition stays observable"
                  Expect.equal accepted.dispositions.Head.id "replacement-001" "the applied receipt names the ledger id"

                  copyPackagedFeedbackSkill root
                  let packagedExit, packagedOutput, packagedError =
                      runProcess root "dotnet"
                          [ "fsi"
                            ".agents/skills/fs-gg-feedback-report/scripts/feedback-tool.fsx"
                            "--"
                            "check-invalidation"
                            "--changed"
                            "src/Changed.fs"
                            "--root"
                            root ]

                  Expect.equal packagedExit 0 (sprintf "the materialized skill accepts the same exact entry: %s" packagedError)
                  Expect.stringContains packagedOutput "applied exception replacement-001" "receiver output reports the applied disposition"

                  if OperatingSystem.IsLinux() then
                      File.Delete(Path.Combine(root, replacement))
                      File.CreateSymbolicLink(Path.Combine(root, replacement), "missing-evidence.txt") |> ignore

                      let dangling = findInvalidatedAuditBindings root [ "src/Changed.fs" ]
                      Expect.exists dangling.errors (fun error -> error.Contains "symbolic link, not a regular file") "the direct route rejects dangling symlink evidence"

                      let danglingExit, _, danglingError =
                          runProcess root "dotnet"
                              [ "fsi"
                                ".agents/skills/fs-gg-feedback-report/scripts/feedback-tool.fsx"
                                "--"
                                "check-invalidation"
                                "--changed"
                                "src/Changed.fs"
                                "--root"
                                root ]

                      Expect.equal danglingExit 1 "the materialized route rejects dangling symlink evidence"
                      Expect.stringContains danglingError "symbolic link, not a regular file" "the materialized diagnostic names the non-regular evidence"

                      File.Delete(Path.Combine(root, replacement))
                      Directory.CreateDirectory(Path.Combine(root, replacement)) |> ignore
                      let directory = findInvalidatedAuditBindings root [ "src/Changed.fs" ]
                      Expect.exists directory.errors (fun error -> error.Contains "path names a directory") "the direct route rejects directory evidence"
                      Directory.Delete(Path.Combine(root, replacement))

                  if File.Exists(Path.Combine(root, replacement)) then
                      File.Delete(Path.Combine(root, replacement))

                  let missing = findInvalidatedAuditBindings root [ "src/Changed.fs" ]
                  Expect.exists missing.errors (fun error -> error.Contains "replacement evidence is missing") "the direct route rejects missing evidence"
                  File.WriteAllText(Path.Combine(root, replacement), "current")

                  File.WriteAllText(ledgerPath, "{")
                  let malformed = findInvalidatedAuditBindings root [ "src/Changed.fs" ]
                  Expect.exists malformed.errors (fun error -> error.Contains "malformed JSON") "malformed JSON fails closed"

                  File.WriteAllText(ledgerPath, (ledger [| entry |]).Replace("\"schemaVersion\":1", "\"schemaVersion\":2"))
                  let unsupported = findInvalidatedAuditBindings root [ "src/Changed.fs" ]
                  Expect.exists unsupported.errors (fun error -> error.Contains "schemaVersion must be 1") "unsupported schema fails closed"

                  let stale =
                      {| entry with replacementSha256 = sha256Text "stale" |}
                  File.WriteAllText(ledgerPath, ledger [| stale |])
                  let staleResult = findInvalidatedAuditBindings root [ "src/Changed.fs" ]
                  Expect.exists staleResult.errors (fun error -> error.Contains "replacement digest is stale") "stale replacement evidence fails closed"

                  let mismatched =
                      {| entry with priorSha256 = sha256Text "different-prior" |}
                  File.WriteAllText(ledgerPath, ledger [| mismatched |])
                  let mismatchedResult = findInvalidatedAuditBindings root [ "src/Changed.fs" ]
                  Expect.exists mismatchedResult.errors (fun error -> error.Contains "is unused") "a mismatched prior digest cannot bind"

                  File.WriteAllText(ledgerPath, ledger [| entry; entry |])
                  let duplicate = findInvalidatedAuditBindings root [ "src/Changed.fs" ]
                  Expect.exists duplicate.errors (fun error -> error.Contains "duplicate id") "duplicate ids fail closed"
                  Expect.exists duplicate.errors (fun error -> error.Contains "duplicate binding") "duplicate bindings fail closed"

                  let unused =
                      {| entry with id = "unused-001"; audit = "feedback/audits/other.audit.json" |}
                  File.WriteAllText(ledgerPath, ledger [| unused |])
                  let unusedResult = findInvalidatedAuditBindings root [ "src/Changed.fs" ]
                  Expect.exists unusedResult.errors (fun error -> error.Contains "is unused") "an entry without an immutable binding fails closed"

                  let overbroad =
                      {| entry with id = "overbroad-001"; locator = "file:src/*"; path = "src/*" |}
                  File.WriteAllText(ledgerPath, ledger [| overbroad |])
                  let overbroadResult = findInvalidatedAuditBindings root [ "src/Changed.fs" ]
                  Expect.exists overbroadResult.errors (fun error -> error.Contains "wildcard paths are overbroad") "wildcard bindings fail explicitly"

                  let unknownField =
                      (ledger [| entry |]).Replace("\"id\":\"replacement-001\"", "\"surprise\":true,\"id\":\"replacement-001\"")
                  File.WriteAllText(ledgerPath, unknownField)
                  let unknown = findInvalidatedAuditBindings root [ "src/Changed.fs" ]
                  Expect.exists unknown.errors (fun error -> error.Contains "unknown property 'surprise'") "unknown fields fail closed"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)
          }

          test "commit-time invalidation indexes audits from the base tree, not the candidate" {
              let root = Path.Combine(Path.GetTempPath(), "fsgg-feedback-base-index-" + Guid.NewGuid().ToString "N")
              Directory.CreateDirectory root |> ignore

              try
                  let reportPath = Path.Combine(root, "feedback", "report.md")
                  let evidence locator = [| {| locator = locator; result = "verified"; sha256 = Some(sha256Text "old") |} |]
                  let mergedAudit = auditJson root reportPath validReport "actionable" (evidence "file:src/Base.fs")
                  let candidateAudit = auditJson root reportPath validReport "actionable" (evidence "file:src/Candidate.fs")

                  initFixtureRepository root
                  writeFile root "feedback/report.md" validReport
                  writeFile root "feedback/audits/merged.audit.json" mergedAudit
                  writeFile root "src/Base.fs" "let baseValue = 1\n"
                  writeFile root "src/Candidate.fs" "let candidateValue = 1\n"
                  git root [ "add"; "-A" ] |> ignore
                  git root [ "commit"; "-q"; "-m"; "base" ] |> ignore
                  let baseSha = git root [ "rev-parse"; "HEAD" ]

                  // The index the base ref supplies: exactly the merged audit. Asserted
                  // BEFORE any pass verdict below, because "the candidate audit was not
                  // selected" and "nothing was ever indexed" are otherwise the same green.
                  let baseIndex = baseTreeAuditIndex root baseSha
                  Expect.isEmpty baseIndex.errors "a resolvable base ref yields no index error"
                  Expect.sequenceEqual
                      (baseIndex.audits |> List.map (fun entry -> entry.auditPath))
                      [ "feedback/audits/merged.audit.json" ]
                      "the base index holds exactly the audits merged into the base tree"
                  Expect.equal baseIndex.subject (sprintf "base ref %s" baseSha) "the index names the tree it read"

                  // AC-001/FR-002 — a candidate that introduces its own audit and edits only
                  // the evidence THAT audit cites. This is #1243's exact reproduction.
                  let selfAuditSha =
                      commitOnto root "candidate-self-audit" baseSha (fun () ->
                          writeFile root "feedback/audits/candidate.audit.json" candidateAudit
                          writeFile root "src/Candidate.fs" "let candidateValue = 2\n")

                  let selfAudit = checkInvalidationBetweenRefs root baseSha selfAuditSha
                  Expect.isEmpty selfAudit.errors "the commit-aware form reports no error on resolvable refs"
                  Expect.isEmpty
                      selfAudit.invalidated
                      "an audit the candidate itself introduces cannot invalidate that candidate"
                  Expect.equal
                      selfAudit.subject
                      (sprintf "base ref %s" baseSha)
                      "the commit-aware verdict names the base ref it indexed"

                  // The same commit through the WORKING-TREE index — the pre-repair answer.
                  // Without this leg the assertion above is equally consistent with a check
                  // that never had anything to find.
                  let selfAuditChanged =
                      match changedPathsBetween root baseSha selfAuditSha with
                      | Ok paths -> paths
                      | Error detail -> failwithf "changed-path derivation failed: %s" detail

                  Expect.sequenceEqual
                      selfAuditChanged
                      [ "feedback/audits/candidate.audit.json"; "src/Candidate.fs" ]
                      "the changed set is still derived from base to head"

                  let workingTreeAnswer = findInvalidatedAuditBindings root selfAuditChanged
                  Expect.equal
                      workingTreeAnswer.invalidated.Length
                      1
                      "the working-tree index does select the candidate's own audit -- the defect being repaired"
                  Expect.equal
                      workingTreeAnswer.subject
                      "the working tree"
                      "the working-tree verdict names the working tree"
                  Expect.equal
                      (baseTreeAuditIndex root selfAuditSha).audits.Length
                      2
                      "the candidate's own tree does contain both audits, so the base index omitted one deliberately"

                  // AC-002/FR-003 — a genuinely merged audit still refuses the candidate,
                  // and the diagnostic still names audit, report, finding and path. The
                  // finding id is non-ASCII, so this also proves the audit survived the pipe.
                  let touchesMergedSha =
                      commitOnto root "candidate-touches-merged" baseSha (fun () ->
                          writeFile root "src/Base.fs" "let baseValue = 2\n")

                  let touchesMerged = checkInvalidationBetweenRefs root baseSha touchesMergedSha
                  Expect.isEmpty touchesMerged.errors "a base-present audit is parsed without error"
                  Expect.sequenceEqual
                      (touchesMerged.invalidated
                       |> List.map (fun item -> item.audit, item.report, item.findingId, item.path))
                      [ "feedback/audits/merged.audit.json", "feedback/report.md", "§4.1", "src/Base.fs" ]
                      "a merged audit still invalidates a candidate that changes its cited evidence"

                  // FR-003/DEC-002 — the exception and replacement are candidate-head facts,
                  // never whatever bytes happen to be in the caller's checkout.
                  let priorDigest = sha256Text "old"
                  let replacementDigest = sha256Text "current"
                  let replacementPath = "readiness/replacement.txt"
                  let headLedger id candidateReplacementPath candidateReplacementDigest =
                      JsonSerializer.Serialize(
                          {| schemaVersion = 1
                             exceptions =
                              [| {| id = id
                                    audit = "feedback/audits/merged.audit.json"
                                    findingId = "§4.1"
                                    locator = "file:src/Base.fs"
                                    path = "src/Base.fs"
                                    priorSha256 = priorDigest
                                    replacementPath = candidateReplacementPath
                                    replacementSha256 = candidateReplacementDigest
                                    evidenceLocator = "command:dotnet test && inspect " + candidateReplacementPath |} |] |}
                      )

                  let ledgerText = headLedger "head-replacement-001" replacementPath replacementDigest

                  let exceptionHeadSha =
                      commitOnto root "candidate-head-exception" baseSha (fun () ->
                          writeFile root "src/Base.fs" "let baseValue = 4\n"
                          writeFile root replacementPath "current"
                          writeFile root "scripts/audit-binding-exceptions.json" ledgerText)

                  // Deliberately make the worktree copy malformed after committing. A checker
                  // that reads disk instead of the named head now fails; the correct one passes.
                  writeFile root "scripts/audit-binding-exceptions.json" "{"
                  let headDisposition = checkInvalidationBetweenRefs root baseSha exceptionHeadSha
                  Expect.isEmpty headDisposition.errors "the candidate head's valid ledger wins over malformed working-tree bytes"
                  Expect.isEmpty headDisposition.invalidated "the candidate-head entry dispositions the touched merged binding"
                  Expect.equal headDisposition.dispositions.Length 1 "one candidate-head disposition is reported"
                  writeFile root "scripts/audit-binding-exceptions.json" ledgerText

                  if OperatingSystem.IsLinux() then
                      let danglingPath = "readiness/dangling.txt"
                      let danglingTarget = "missing-evidence.txt"
                      let danglingSha =
                          commitOnto root "candidate-head-dangling" baseSha (fun () ->
                              writeFile root "src/Base.fs" "let baseValue = 5\n"
                              Directory.CreateDirectory(Path.Combine(root, "readiness")) |> ignore
                              File.CreateSymbolicLink(Path.Combine(root, danglingPath), danglingTarget) |> ignore
                              writeFile root "scripts/audit-binding-exceptions.json" (headLedger "head-dangling-001" danglingPath (sha256Text danglingTarget)))

                      let danglingHead = checkInvalidationBetweenRefs root baseSha danglingSha
                      Expect.exists danglingHead.errors (fun error -> error.Contains "mode 120000 type blob") "the ref-aware route rejects a dangling Git symlink blob"
                      Expect.isEmpty danglingHead.dispositions "a symlink blob never produces an applied disposition"

                  let treePath = "readiness/replacement-tree"
                  let treeSha =
                      commitOnto root "candidate-head-tree" baseSha (fun () ->
                          writeFile root "src/Base.fs" "let baseValue = 6\n"
                          writeFile root (treePath + "/evidence.txt") "current"
                          writeFile root "scripts/audit-binding-exceptions.json" (headLedger "head-tree-001" treePath replacementDigest))

                  let treeHead = checkInvalidationBetweenRefs root baseSha treeSha
                  Expect.exists treeHead.errors (fun error -> error.Contains "mode 040000 type tree") "the ref-aware route rejects a Git tree"
                  Expect.isEmpty treeHead.dispositions "a Git tree never produces an applied disposition"

                  let missingPath = "readiness/missing.txt"
                  let missingSha =
                      commitOnto root "candidate-head-missing" baseSha (fun () ->
                          writeFile root "src/Base.fs" "let baseValue = 7\n"
                          writeFile root "scripts/audit-binding-exceptions.json" (headLedger "head-missing-001" missingPath replacementDigest))

                  let missingHead = checkInvalidationBetweenRefs root baseSha missingSha
                  Expect.exists missingHead.errors (fun error -> error.Contains "replacement evidence is missing") "the ref-aware route rejects a missing candidate-head path"
                  Expect.isEmpty missingHead.dispositions "a missing path never produces an applied disposition"

                  // AC-003 — deleting the durable audit in the candidate must not clear the
                  // refusal. The index is the base tree, so there is nothing on disk to delete.
                  let deletesAuditSha =
                      commitOnto root "candidate-deletes-audit" baseSha (fun () ->
                          writeFile root "src/Base.fs" "let baseValue = 3\n"
                          File.Delete(Path.Combine(root, "feedback", "audits", "merged.audit.json")))

                  let deletesAudit = checkInvalidationBetweenRefs root baseSha deletesAuditSha
                  Expect.equal
                      (deletesAudit.invalidated |> List.map (fun item -> item.audit))
                      [ "feedback/audits/merged.audit.json" ]
                      "deleting the merged audit in the candidate does not clear its binding"
                  Expect.isEmpty
                      (workingTreeAuditIndex root).audits
                      "the candidate's own worktree has no audit left, so only the base index carried the refusal"

                  // AC-004 — rename detection still contributes the old side.
                  let renameSha =
                      commitOnto root "candidate-renames-evidence" baseSha (fun () ->
                          git root [ "mv"; "src/Base.fs"; "src/Renamed.fs" ] |> ignore)

                  let renamed = checkInvalidationBetweenRefs root baseSha renameSha
                  Expect.equal
                      (renamed.invalidated |> List.map (fun item -> item.path))
                      [ "src/Base.fs" ]
                      "a rename's old side is still in the changed set and still detected"

                  // AC-005 — neither ref may fail open, and each names its own fault.
                  let badBase = checkInvalidationBetweenRefs root "fsgg-no-such-ref-1243" touchesMergedSha
                  Expect.exists
                      badBase.errors
                      (fun error -> error.Contains "could not read the audit index at fsgg-no-such-ref-1243")
                      "an unresolvable base ref fails closed against the index it could not read"
                  Expect.isEmpty badBase.invalidated "an unreadable index reports no binding verdict"

                  let badHead = checkInvalidationBetweenRefs root baseSha "fsgg-no-such-head-1243"
                  Expect.exists
                      badHead.errors
                      (fun error -> error.Contains "could not read commit path changes")
                      "an unresolvable head ref fails closed against the diff it could not read"

                  // AC-006 — a malformed audit IN THE BASE TREE fails closed. The malformed
                  // document is committed, so this measures the base index and not the disk.
                  let brokenBaseSha =
                      commitOnto root "broken-base" baseSha (fun () -> writeFile root "feedback/audits/broken.audit.json" "{")

                  let afterBrokenSha =
                      commitOnto root "after-broken" brokenBaseSha (fun () -> writeFile root "src/Other.fs" "let other = 1\n")

                  let broken = checkInvalidationBetweenRefs root brokenBaseSha afterBrokenSha
                  Expect.exists
                      broken.errors
                      (fun error -> error.Contains "malformed audit feedback/audits/broken.audit.json")
                      "a malformed base audit cannot render a safe empty verdict"

                  // FR-004 — the two index faults the filesystem cannot be made to produce on
                  // demand are measured against the pure scan, which is where they are handled.
                  let unreadableIndex =
                      { subject = "synthetic index"
                        audits =
                          [ { auditPath = "feedback/audits/unreadable.audit.json"
                              document = Error "fatal: bad object" } ]
                        errors = [ "invalidation: could not read the audit index at deadbeef: fatal: bad object" ] }

                  let unreadable = findInvalidatedAuditBindingsIn unreadableIndex [ "src/Base.fs" ]
                  Expect.equal unreadable.subject "synthetic index" "the scan carries its index's subject into the verdict"
                  Expect.exists
                      unreadable.errors
                      (fun error -> error.Contains "unreadable audit feedback/audits/unreadable.audit.json")
                      "an audit document that could not be read has its own diagnostic"
                  Expect.exists
                      unreadable.errors
                      (fun error -> error.Contains "could not read the audit index at deadbeef")
                      "an enumeration error survives into the scan's errors rather than yielding an empty index"
                  Expect.isEmpty unreadable.invalidated "a broken index reports no binding verdict"

                  let gitlinkPath = "readiness/replacement-submodule"
                  git root [ "checkout"; "-q"; "-B"; "candidate-head-gitlink"; baseSha ] |> ignore
                  writeFile root "src/Base.fs" "let baseValue = 8\n"
                  writeFile root "scripts/audit-binding-exceptions.json" (headLedger "head-gitlink-001" gitlinkPath replacementDigest)
                  git root [ "add"; "-A" ] |> ignore
                  git root [ "update-index"; "--add"; "--cacheinfo"; sprintf "160000,%s,%s" baseSha gitlinkPath ] |> ignore
                  git root [ "commit"; "-q"; "-m"; "candidate-head-gitlink" ] |> ignore
                  let gitlinkSha = git root [ "rev-parse"; "HEAD" ]
                  let gitlinkHead = checkInvalidationBetweenRefs root baseSha gitlinkSha
                  Expect.exists gitlinkHead.errors (fun error -> error.Contains "mode 160000 type commit") "the ref-aware route rejects a Git submodule entry"
                  Expect.isEmpty gitlinkHead.dispositions "a gitlink never produces an applied disposition"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)
          }

          test "unreadable zero-event activation receipt is not reported as merely missing" {
              let root =
                  Path.Combine(
                      Path.GetTempPath(),
                      "fsgg-feedback-unreadable-" + Guid.NewGuid().ToString "N"
                  )

              try
                  activationReceiptPath root "001-example"
                  |> Directory.CreateDirectory
                  |> ignore

                  let errors = validateCheckpointState root "001-example"

                  Expect.exists
                      errors
                      (fun error -> error.Contains("unreadable"))
                      "an unreadable receipt-shaped path has its own fail-closed diagnostic"
              finally
                  if Directory.Exists root then
                      Directory.Delete(root, true)
          } ]

module Issue1039PerformanceEvidenceTests

open System
open System.Diagnostics
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport
open Rendering.Harness

let private root = RepositoryRoot.value

let private read (relative: string) =
    File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)))

let private occurrences (needle: string) (text: string) =
    let rec loop start count =
        let found = text.IndexOf(needle, start, System.StringComparison.Ordinal)

        if found < 0 then
            count
        else
            loop (found + needle.Length) (count + 1)

    loop 0 0

type private CommandResult =
    { ExitCode: int
      Output: string }

let private runDotnet workingDirectory dotnetHome arguments =
    let argumentsDisplay = String.concat " " arguments
    let startInfo = ProcessStartInfo("dotnet")
    startInfo.WorkingDirectory <- workingDirectory
    startInfo.UseShellExecute <- false
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    startInfo.Environment["DOTNET_CLI_HOME"] <- dotnetHome
    startInfo.Environment["DOTNET_NOLOGO"] <- "1"
    arguments |> List.iter startInfo.ArgumentList.Add

    use proc =
        match Process.Start startInfo with
        | null -> failwith $"could not start dotnet {argumentsDisplay}"
        | started -> started

    let output = proc.StandardOutput.ReadToEndAsync()
    let error = proc.StandardError.ReadToEndAsync()

    if not (proc.WaitForExit(TimeSpan.FromMinutes(5.0))) then
        proc.Kill(true)
        failwith $"dotnet {argumentsDisplay} timed out"

    { ExitCode = proc.ExitCode
      Output = output.Result + Environment.NewLine + error.Result }

let private workloadDigests artifactPath =
    use document = JsonDocument.Parse(File.ReadAllText artifactPath)

    document.RootElement.GetProperty("workloads").EnumerateArray()
    |> Seq.map (fun workload ->
        workload.GetProperty("id").GetString(),
        workload.GetProperty("definitionDigest").GetString())
    |> Seq.map (fun (id, digest) ->
        Option.ofObj id |> Option.defaultWith (fun () -> failwith "workload id was null"),
        Option.ofObj digest |> Option.defaultWith (fun () -> failwith "workload digest was null"))
    |> Map.ofSeq

let private authorWorkloads (digests: Map<string, string>) (source: string) =
    digests
    |> Map.fold (fun (authored: string) (id: string) (digest: string) ->
        let beginMarker = $"// WORKLOAD-SOURCE-BEGIN {id}"
        let endMarker = $"// WORKLOAD-SOURCE-END {id}"
        let start = authored.IndexOf(beginMarker, StringComparison.Ordinal)
        let finish = authored.IndexOf(endMarker, start + beginMarker.Length, StringComparison.Ordinal)

        if start < 0 || finish < 0 then
            failwith $"generated workload source block was missing for {id}"

        let block = authored.Substring(start, finish + endMarker.Length - start)

        let rewritten =
            Regex.Replace(
                block,
                @"Authorship\s*=\s*Placeholder\s+""[^""]*""",
                $"Authorship = Authored \"{digest}\"",
                RegexOptions.CultureInvariant
            )

        if rewritten = block then
            failwith $"generated workload placeholder was missing for {id}"

        authored.Substring(0, start)
        + rewritten
        + authored.Substring(finish + endMarker.Length)) source

[<Tests>]
let performanceEvidenceContract =
    testList
        "issue-1039 expected-workload performance evidence"
        [ test "normal-play verdict fails before a MiniTank-scale node remediation and passes afterward" {
              let budget: Perf.ExpectedWorkloadBudget =
                  { P95Ms = 16.67
                    P99Ms = 25.0
                    MaximumSceneNodes = 4096
                    AllowSustainedCatchUp = false }

              let before =
                  Perf.evaluateExpectedWorkload Perf.NormalPlay (Some budget) None 18.0 28.0 3 6000

              let after =
                  Perf.evaluateExpectedWorkload Perf.NormalPlay (Some budget) None 8.0 14.0 0 3000

              Expect.isFalse
                  before.Passed
                  "thousands of repeated fog/minimap nodes and missed timing budgets fail closed"

              Expect.isTrue after.Passed "row-run/static-subtree-scale remediation passes the same target"
          }

          test "stress evidence cannot be mistaken for the normal-play gate" {
              let verdict =
                  Perf.evaluateExpectedWorkload Perf.Stress None None 100.0 200.0 50 10000

              Expect.isTrue verdict.Passed "stress remains classified informational evidence"
              Expect.stringContains (String.concat " " verdict.Reasons) "non-normal" "classification is explicit"
          }

          test "game scaffold emits the command, artifact contract, workloads and Verify wiring" {
              let source = read "template/base/src/Product/PerformanceEvidence.fs"
              let commands = read "template/base/src/Product/EvidenceCommands.fs"
              let build = read "template/base/build.fsx"

              [ "\"idle\""
                "\"movement-aiming\""
                "\"firing\""
                "\"effects-fog\""
                "\"maximum-content\""
                "\"schemaVersion\", 2"
                "\"definitionDigest\""
                "\"authorship\""
                "\"requiredAuthoringWork\""
                "\"declaredDefinitionDigest\""
                "\"blockingDebt\""
                "\"definition\""
                "\"warmupFrames\""
                "\"p50Ms\""
                "\"p95Ms\""
                "\"p99Ms\""
                "\"updateCount\""
                "\"presentCount\""
                "\"catchUpFrames\""
                "\"droppedFrames\""
                "\"eventCount\""
                "\"pointerEventCount\""
                "\"sceneNodesByLayer\""
                "\"allocatedBytes\""
                "\"packageVersions\""
                "\"hostProfile\""
                "\"measurementCapability\""
                "\"notAuthoritativeFor\""
                "a linked blocking debt permits baseline capture only, never acceptance" ]
              |> List.iter (fun token -> Expect.stringContains source token $"performance artifact carries {token}")

              Expect.stringContains commands "--performance-evidence" "the product command is routed"
              Expect.stringContains build "\"PerformanceEvidence\"" "the explicit build target exists"

              Expect.stringContains
                  build
                  "runPerformanceEvidence ()"
                  "Verify invokes the fail-closed Release measurement"
          }

          test "untouched game scaffold names all five required authoring jobs and cannot pass" {
              let source = read "template/base/src/Product/PerformanceEvidence.fs"

              Expect.equal
                  (occurrences "Authorship = Placeholder " source)
                  5
                  "every required normal-play workload starts as an explicit placeholder"

              [ "representative idle state and messages"
                "simultaneous movement and aiming"
                "combat/firing state and messages"
                "effects/fog state and messages"
                "maximum-expected-content state and messages" ]
              |> List.iter (fun requiredWork ->
                  Expect.stringContains source requiredWork $"fresh scaffold names required work: {requiredWork}")

              Expect.stringContains
                  source
                  "required workload '{workload.Id}' is still a placeholder"
                  "placeholder is a failing verdict rather than executable green evidence"

              Expect.stringContains
                  source
                  "Passed = authorshipVerdict.Passed && budgetVerdict.Passed"
                  "budget success cannot hide an unauthored workload"
          }

          test "authored declaration is digest-bound and representative routes can pass" {
              let source = read "template/base/src/Product/PerformanceEvidence.fs"

              [ "type WorkloadAuthorship ="
                "Authored of definitionDigest: string"
                "let evaluateAuthorship workload"
                "String.Equals(declaredDigest, actualDigest, StringComparison.OrdinalIgnoreCase)"
                "authored declaration is stale for workload"
                "| Some _, Authored _ -> { Passed = true; Reasons = [] }"
                "let mutable model = workload.InitialState()"
                "update (workload.MessageAt frame) model"
                "let scene = view model" ]
              |> List.iter (fun token ->
                  Expect.stringContains source token $"authored workload contract carries {token}")

              Expect.isLessThan
                  (source.IndexOf("| Authored declaredDigest when", System.StringComparison.Ordinal))
                  (source.IndexOf("| Some _, Authored _ -> { Passed = true", System.StringComparison.Ordinal))
                  "stale digest rejection is evaluated before the matching authored pass branch"
          }

          test "linked performance debt preserves a baseline artifact but never acceptance" {
              let source = read "template/base/src/Product/PerformanceEvidence.fs"

              [ "baseline capture requires a linked blocking performance-debt issue"
                "baseline-only-with-linked-debt"
                "captured evidence does not satisfy acceptance" ]
              |> List.iter (fun token -> Expect.stringContains source token $"baseline contract carries {token}")

              let linkedDebtBranch =
                  source.Substring(
                      source.IndexOf("| Some debt ->", System.StringComparison.Ordinal),
                      source.IndexOf("let evaluateAuthorship", System.StringComparison.Ordinal)
                      - source.IndexOf("| Some debt ->", System.StringComparison.Ordinal)
                  )

              Expect.stringContains linkedDebtBranch "Passed = false" "a linked debt cannot turn the gate green"
          }

          test "generated game scaffold executes placeholder, authored, stale-route and debt verdicts" {
              let fixtureRoot =
                  Path.Combine(Path.GetTempPath(), $"fsgg-performance-evidence-{Guid.NewGuid():N}")

              let dotnetHome = Path.Combine(fixtureRoot, "dotnet-home")
              let productRoot = Path.Combine(fixtureRoot, "WorkloadFixture")
              let artifactPath = Path.Combine(productRoot, "readiness", "performance-evidence.json")

              let evidenceSourcePath =
                  Path.Combine(productRoot, "src", "WorkloadFixture", "PerformanceEvidence.fs")

              let runEvidence () =
                  runDotnet
                      productRoot
                      dotnetHome
                      [ "run"
                        "-c"
                        "Release"
                        "--project"
                        "src/WorkloadFixture"
                        "--"
                        "--performance-evidence"
                        "readiness/performance-evidence.json" ]

              Directory.CreateDirectory fixtureRoot |> ignore

              try
                  let install =
                      runDotnet fixtureRoot dotnetHome [ "new"; "install"; root; "--force" ]

                  Expect.equal install.ExitCode 0 $"local template install succeeds:{Environment.NewLine}{install.Output}"

                  let instantiate =
                      runDotnet
                          fixtureRoot
                          dotnetHome
                          [ "new"
                            "fs-gg-ui"
                            "--name"
                            "WorkloadFixture"
                            "--profile"
                            "game"
                            "--lifecycle"
                            "none"
                            "--output"
                            productRoot ]

                  Expect.equal
                      instantiate.ExitCode
                      0
                      $"game scaffold instantiation succeeds:{Environment.NewLine}{instantiate.Output}"

                  let untouched = runEvidence ()
                  Expect.equal untouched.ExitCode 1 "untouched placeholders fail the executable product command"

                  [ "idle"
                    "movement-aiming"
                    "firing"
                    "effects-fog"
                    "maximum-content"
                    "is still a placeholder" ]
                  |> List.iter (fun token ->
                      Expect.stringContains untouched.Output token $"untouched command names required work: {token}")

                  let digests = workloadDigests artifactPath
                  Expect.equal digests.Count 5 "fresh artifact emits one review digest per required workload"

                  let placeholderSource = File.ReadAllText evidenceSourcePath
                  let authoredSource = authorWorkloads digests placeholderSource
                  File.WriteAllText(evidenceSourcePath, authoredSource)

                  let authored = runEvidence ()
                  Expect.equal
                      authored.ExitCode
                      0
                      $"reviewed exact-digest declarations pass:{Environment.NewLine}{authored.Output}"

                  File.WriteAllText(
                      evidenceSourcePath,
                      authoredSource + Environment.NewLine + "// WORKLOAD-SOURCE-BEGIN idle"
                  )

                  let duplicateMarker = runEvidence ()
                  Expect.equal duplicateMarker.ExitCode 1 "duplicate source markers fail authorship"

                  Expect.stringContains
                      duplicateMarker.Output
                      "workload 'idle' has no readable WORKLOAD-SOURCE block"
                      "ambiguous marker ownership receives the fail-closed diagnosis"

                  let missingMarkerSource =
                      authoredSource.Replace(
                          "// WORKLOAD-SOURCE-END idle",
                          "// REMOVED-WORKLOAD-SOURCE-END idle",
                          StringComparison.Ordinal
                      )

                  File.WriteAllText(evidenceSourcePath, missingMarkerSource)
                  let missingMarker = runEvidence ()
                  Expect.equal missingMarker.ExitCode 1 "missing source marker fails authorship"

                  Expect.stringContains
                      missingMarker.Output
                      "workload 'idle' has no readable WORKLOAD-SOURCE block"
                      "unbounded executable source cannot retain an authored acknowledgement"

                  let staleSource =
                      authoredSource.Replace(
                          "MessageAt = (fun _ -> Tick(1.0 / 60.0))",
                          "MessageAt = (fun _ -> Tick(2.0 / 60.0))",
                          StringComparison.Ordinal
                      )

                  Expect.notEqual staleSource authoredSource "fixture changes executable workload routing"
                  File.WriteAllText(evidenceSourcePath, staleSource)

                  let stale = runEvidence ()
                  Expect.equal stale.ExitCode 1 "route change with the old declaration digest fails closed"
                  Expect.stringContains stale.Output "authored declaration is stale" "stale route is diagnosed"
                  Expect.stringContains stale.Output "workload 'idle'" "changed workload is identified"

                  let linkedDebtSource =
                      authoredSource.Replace(
                          "BlockingDebt = None",
                          "BlockingDebt = Some \"FS-GG/Product#123\"",
                          StringComparison.Ordinal
                      )

                  File.WriteAllText(evidenceSourcePath, linkedDebtSource)
                  let linkedDebt = runEvidence ()
                  Expect.equal linkedDebt.ExitCode 1 "linked debt allows baseline capture but not acceptance"

                  Expect.stringContains
                      linkedDebt.Output
                      "baseline-only-with-linked-debt FS-GG/Product#123"
                      "valid owner/repo issue reference is retained in the failing verdict"

                  let invalidDebtSource =
                      authoredSource.Replace(
                          "BlockingDebt = None",
                          "BlockingDebt = Some \"notes#later\"",
                          StringComparison.Ordinal
                      )

                  File.WriteAllText(evidenceSourcePath, invalidDebtSource)
                  let invalidDebt = runEvidence ()
                  Expect.equal invalidDebt.ExitCode 1 "non-issue prose is not accepted as linked blocking debt"

                  Expect.stringContains
                      invalidDebt.Output
                      "baseline capture requires a linked blocking performance-debt issue"
                      "invalid debt syntax receives an actionable fail-closed diagnosis"
              finally
                  if Directory.Exists fixtureRoot then
                      Directory.Delete(fixtureRoot, true)
          }

          test "all four generated-product skills teach one bounded-versus-live recipe" {
              [ "template/base/.agents/skills/fs-gg-project/SKILL.md"
                "template/product-skills/fs-gg-testing/SKILL.md"
                "template/product-skills/fs-gg-scene/SKILL.md"
                "template/product-skills/fs-gg-skiaviewer/SKILL.md" ]
              |> List.iter (fun path ->
                  let skill = read path
                  let prose = Regex.Replace(skill, @"\s+", " ")
                  Expect.stringContains skill "PerformanceEvidence" $"{path} names the target"
                  Expect.stringContains skill "bounded headless" $"{path} discloses the bounded capability"
                  Expect.stringContains skill "live compositor" $"{path} keeps live present proof separate"
                  Expect.stringContains skill "Placeholder" $"{path} teaches fail-closed workload authoring"
                  Expect.stringContains skill "definitionDigest" $"{path} teaches stale-evidence invalidation"
                  Expect.stringContains prose "before feature implementation" $"{path} moves authoring early")
          } ]

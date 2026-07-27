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

let private writeConsumerNugetConfig directory =
    match Environment.GetEnvironmentVariable "FS_GG_PRODUCT_LOCAL_FEED" with
    | null
    | "" -> ()
    | feed ->
        let escapedFeed = System.Security.SecurityElement.Escape feed

        File.WriteAllText(
            Path.Combine(directory, "NuGet.Config"),
            $"""<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="product-local-feed" value="{escapedFeed}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key="product-local-feed">
      <package pattern="FS.GG.UI" />
      <package pattern="FS.GG.UI.*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"""
        )

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

let private performanceIntentBindings artifactPath =
    use document = JsonDocument.Parse(File.ReadAllText artifactPath)

    document.RootElement
        .GetProperty("performanceIntent")
        .GetProperty("workloadDefinitionDigests")
        .EnumerateArray()
    |> Seq.map (fun binding -> binding.GetString())
    |> Seq.map (Option.ofObj >> Option.defaultWith (fun () -> failwith "performance intent binding was null"))
    |> List.ofSeq

let private representativeReady artifactPath =
    use document = JsonDocument.Parse(File.ReadAllText artifactPath)
    document.RootElement.GetProperty("critic").GetProperty("representativeReady").GetBoolean()

let private rewriteWorkloadBlock id rewrite (source: string) =
    let beginMarker = $"// WORKLOAD-SOURCE-BEGIN {id}"
    let endMarker = $"// WORKLOAD-SOURCE-END {id}"
    let start = source.IndexOf(beginMarker, StringComparison.Ordinal)
    let finish = source.IndexOf(endMarker, start + beginMarker.Length, StringComparison.Ordinal)

    if start < 0 || finish < 0 then
        failwith $"generated workload source block was missing for {id}"

    let length = finish + endMarker.Length - start
    source.Substring(0, start) + rewrite (source.Substring(start, length)) + source.Substring(start + length)

let private machineJourneyHelper =
    """
let private machineIssuedJourneyReceipt () =
    let adapter: FS.GG.Game.Harness.ProductionJourney<Model, char, unit, unit, unit, Msg, string> =
        { RouteId = "WorkloadFixture/performance"
          ScenarioId = "fixed-tick"
          TestId = "PERF-JOURNEY-001"
          MaxSteps = 4
          Boot = (fun () -> initialModel)
          MapEvent =
            (fun event _ ->
                match event with
                | FS.GG.Game.Harness.JourneyEvent.FixedTick ->
                    FS.GG.Game.Harness.JourneyDispatch.Mapped [ Tick(1.0 / 60.0) ]
                | FS.GG.Game.Harness.JourneyEvent.KeyInput(key, pressed) ->
                    FS.GG.Game.Harness.JourneyDispatch.Mapped [ ViewerInput(Letter key, pressed) ]
                | _ -> FS.GG.Game.Harness.JourneyDispatch.Mapped [ NoOp ])
          Update = (fun message model -> fst (update message model))
          FixedTick = (fun model -> fst (update (Tick(1.0 / 60.0)) model))
          ApplyEffectResult = (fun _ model -> model)
          IsTerminal = (fun model -> model.Ball.Pos <> initialModel.Ball.Pos)
          Fingerprint = (fun model -> sprintf "%A" model)
          EncodeEvent = sprintf "%A"
          EncodeFingerprint = id }

    (FS.GG.Game.Harness.Journey.runScriptWithIdentity
        "performance-fixed-tick-v1"
        "ball-position-changed-v1"
        adapter
        [ FS.GG.Game.Harness.JourneyEvent.FixedTick ])
        .Receipt

"""

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
                "\"schemaVersion\", 3"
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
                "\"declaredEventCount\""
                "\"observedEventCount\""
                "\"declaredPointerEventCount\""
                "\"observedPointerEventCount\""
                "\"rawInputSampleCount\""
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

          test "generated declaration uses the published Contracts 7.x performance-intent shape" {
              let source = read "template/base/src/Product/PerformanceEvidence.fs"
              let packages = read "template/base/Directory.Packages.props"
              let project = read "template/base/src/Product/Product.fsproj"
              let commands = read "template/base/src/Product/EvidenceCommands.fs"
              let build = read "template/base/build.fsx"

              Expect.stringContains source "open Fsgg.Schemas" "generated source imports the producer contract"

              Expect.stringContains
                  source
                  "performanceIntentSeed: PerformanceIntentDeclaration"
                  "the authoring source is the published record"

              Expect.isFalse
                  (source.Contains("type PerformanceIntentDeclaration", StringComparison.Ordinal))
                  "the template does not invent a parallel intent contract"

              // A FROZEN LITERAL, ON PURPOSE — AND HERE IS THE GATE THAT WILL BREAK IT (#1102 AC 2).
              //
              // THE GATE: `pin-lags-feed`, in scripts/validate-template-payload-pins.fsx. It demands
              // $(FsGgContractsVersion) equal the newest STABLE FS.GG.Contracts on nuget.org. This test
              // demands it equal the literal below. So every upstream FS.GG.Contracts publish makes the
              // two mutually unsatisfiable until a human edits this line. That is EXPECTED AND ROUTINE,
              // not a defect: it is the cost of witnessing the property, and it is paid deliberately.
              //
              // WHY IT STAYS EXACT. "Producer version is exact" is the property under test — the pin is
              // ONE EXACT VERSION, never a floating range. A `7.` prefix match would stop witnessing it,
              // and nothing else in the tree witnesses it. Two things are therefore FORBIDDEN here, and
              // both were considered and rejected on #1102 rather than overlooked:
              //   * DO NOT loosen this to a prefix/StartsWith match. That is the property, gone.
              //   * DO NOT source it from the axis `pin-lags-feed` reads. It removes the manual edit and
              //     collapses two independent witnesses into one source, so neither can catch the other
              //     being wrong. The manual edit IS the second witness.
              //
              // WHAT TO DO WHEN IT FIRES. Moving this literal is step 6 of a SIX-STEP routine, and the
              // other five are invisible to the CI that reported it: `Deterministic gate` exits at the
              // first failing suite, so a Contracts bump reds one gate at a time. #1094 measured the
              // 7.0.0 -> 7.2.0 move at five files across three further gates (the omission ledger, the
              // ledger ceiling, the pre-#782 mirror bridge, the M-PROV stamps, and this line). Work
              // docs/ci/cadence-map.md §4b, "Bumping a component axis", which lists them in order.
              //
              // AND `pin-lags-feed` NO LONGER REDS A PR (#1102). It moved to the scheduled sweep
              // .github/workflows/template-pin-staleness-sweep.yml, which files a tracked item. So this
              // literal is now moved by somebody who CLAIMED that item, with the routine in hand —
              // rather than by whoever happened to have a PR open when Contracts published.
              // #1101 moved it 7.2.0 -> 7.4.0, and for a reason the routine above did not anticipate: not
              // `pin-lags-feed` (already out of the PR lane by then) but the DELETION of the pre-#782
              // `legacyPre782Surfaces` bridge in scripts/refresh-api-surface-mirror.fsx. Reading Contracts
              // like every other package needs a version that PACKS `api-surface/*.fsi`, and 7.4.0 is the
              // first published release that does. So a second trigger for this literal now exists: any
              // change to how the mirror generator sources a package can force the pin, independently of
              // the feed. Steps 1-5 of the routine were all still required.
              Expect.stringContains packages "<FsGgContractsVersion>7.4.0</FsGgContractsVersion>" "producer version is exact"
              Expect.stringContains
                  project
                  "<PackageReference Include=\"FS.GG.Contracts\" />"
                  "generated game consumes the exact producer pin"
              Expect.stringContains commands "\"--performance-intent\"" "the SDD-ready declaration command is routed"
              Expect.stringContains build "\"PerformanceIntent\"" "the generated build exposes the declaration target"
              Expect.stringContains source "writeIntentJson json" "evidence embeds the same projected declaration"
              Expect.stringContains source "let writePerformanceIntentDeclaration _ = 0" "non-game profiles retain a compilable no-op"
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
                  "authorshipVerdict.Passed"
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
              let criticRequestPath = Path.Combine(productRoot, "readiness", "performance-critic-request.json")
              let intentPath = Path.Combine(productRoot, "readiness", "performance-intent.yml")

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

              let runIntent () =
                  runDotnet
                      productRoot
                      dotnetHome
                      [ "run"
                        "-c"
                        "Release"
                        "--project"
                        "src/WorkloadFixture"
                        "--"
                        "--performance-intent"
                        "readiness/performance-intent.yml" ]

              let runCriticRequest () =
                  runDotnet
                      productRoot
                      dotnetHome
                      [ "run"
                        "-c"
                        "Release"
                        "--project"
                        "src/WorkloadFixture"
                        "--"
                        "--performance-critic-request"
                        "readiness/performance-critic-request.json" ]

              Directory.CreateDirectory fixtureRoot |> ignore
              writeConsumerNugetConfig fixtureRoot

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

                  let untouchedIntent = runIntent ()
                  Expect.equal untouchedIntent.ExitCode 1 "untouched placeholders also fail the early intent command"
                  Expect.isTrue
                      (File.Exists intentPath)
                      $"failed early intent still emits the declaration for authoring:{Environment.NewLine}{untouchedIntent.Output}"

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
                      1
                      "authorship alone cannot pass the machine gate without routed facts"
                  Expect.stringContains authored.Output "observed routed count" "declared stimulus must traverse production"
                  Expect.stringContains
                      authored.Output
                      "synthetic-constructed"
                      "direct state construction cannot mint normal-play provenance"

                  let representativePlaceholderSource =
                      placeholderSource
                      |> fun source ->
                          source.Replace(
                              "let expectedWorkloads =",
                              machineJourneyHelper + "let expectedWorkloads =",
                              StringComparison.Ordinal
                          )
                      |> fun source ->
                          Regex.Replace(
                              source,
                              @"Provenance = SyntheticConstructed ""[^""]*""",
                              "Provenance = RunnerIssuedJourney(machineIssuedJourneyReceipt ())",
                              RegexOptions.CultureInvariant
                          )
                      |> rewriteWorkloadBlock
                          "movement-aiming"
                          (fun block ->
                              block
                                  .Replace("PointerEventsPerFrame = 1", "PointerEventsPerFrame = 0")
                                  .Replace("frame % 2 = 0", "frame % 1 = 0"))
                      |> rewriteWorkloadBlock
                          "firing"
                          (fun block ->
                              block
                                  .Replace("PointerEventsPerFrame = 1", "PointerEventsPerFrame = 0")
                                  .Replace("MessageAt = (fun _ -> NoOp)", "MessageAt = (fun _ -> ViewerInput(Letter 'F', true))"))

                  File.WriteAllText(evidenceSourcePath, representativePlaceholderSource)
                  let representativePendingAuthorship = runEvidence ()
                  Expect.equal representativePendingAuthorship.ExitCode 1 "representative routes remain red while unauthored"
                  let representativeDigests = workloadDigests artifactPath
                  let representativeAuthoredSource = authorWorkloads representativeDigests representativePlaceholderSource
                  File.WriteAllText(evidenceSourcePath, representativeAuthoredSource)
                  let representativeMachineGreen = runEvidence ()
                  Expect.equal
                      representativeMachineGreen.ExitCode
                      0
                      $"factory routes, complete coverage, matching observed routing and budget pass the machine gate:{Environment.NewLine}{representativeMachineGreen.Output}"
                  Expect.isFalse
                      (representativeReady artifactPath)
                      "authored source cannot self-attest fresh-context critic independence or representative readiness"

                  let criticRequest = runCriticRequest ()
                  Expect.equal criticRequest.ExitCode 0 "critic request preserves the green machine verdict"
                  use request = JsonDocument.Parse(File.ReadAllText criticRequestPath)
                  let declaredArtifactDigest =
                      request.RootElement.GetProperty("evidenceArtifactDigest").GetString()
                      |> Option.ofObj
                      |> Option.defaultWith (fun () -> failwith "critic request artifact digest was null")
                  let actualArtifactDigest =
                      File.ReadAllBytes artifactPath
                      |> System.Security.Cryptography.SHA256.HashData
                      |> Convert.ToHexString
                      |> _.ToLowerInvariant()
                      |> fun digest -> $"sha256:{digest}"
                  Expect.equal
                      declaredArtifactDigest
                      actualArtifactDigest
                      "critic request binds the exact timing/verdict/capability/package evidence artifact bytes"

                  let authoredIntent = runIntent ()
                  Expect.equal
                      authoredIntent.ExitCode
                      0
                      $"the same authored rows populate SDD early intent:{Environment.NewLine}{authoredIntent.Output}"

                  let authoredBindings = performanceIntentBindings artifactPath
                  let authoredIntentYaml = File.ReadAllText intentPath

                  Expect.equal authoredBindings.Length 5 "intent binds all five required normal-play workloads"
                  Expect.equal (authoredBindings |> List.distinct).Length 5 "intent digest bindings are unique"

                  authoredBindings
                  |> List.iter (fun binding ->
                      Expect.stringContains
                          authoredIntentYaml
                          (JsonSerializer.Serialize binding)
                          $"SDD YAML and evidence carry the exact same binding: {binding}")

                  [ "performanceIntent:"
                    "targetFps: 60"
                    "maximumExpectedScale:"
                    "maxP95Ms: 16.67"
                    "maxP99Ms: 25.0"
                    "maxCatchUpFrames: 0"
                    "structuralCostBudgets:"
                    "requiredCapability:"
                    "liveCompositorRequired: false" ]
                  |> List.iter (fun token ->
                      Expect.stringContains authoredIntentYaml token $"published Contracts 7.x field is projected: {token}")

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

                  let duplicateIdSource =
                      authoredSource.Replace(
                          "// WORKLOAD-SOURCE-BEGIN firing\n      { Id = \"firing\"",
                          "// WORKLOAD-SOURCE-BEGIN firing\n      { Id = \"idle\"",
                          StringComparison.Ordinal
                      )

                  Expect.notEqual duplicateIdSource authoredSource "fixture duplicates a workload identity"
                  File.WriteAllText(evidenceSourcePath, duplicateIdSource)
                  let duplicateId = runEvidence ()
                  Expect.equal duplicateId.ExitCode 1 "duplicate workload identities fail closed"
                  Expect.stringContains duplicateId.Output "duplicate workload ids: idle" "duplicate id is diagnosed"

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

                  let staleIntent = runIntent ()
                  Expect.equal staleIntent.ExitCode 1 "route changes invalidate the old early intent acknowledgement"

                  let changedIntentYaml = File.ReadAllText intentPath
                  let changedBindings = performanceIntentBindings artifactPath
                  Expect.notEqual changedIntentYaml authoredIntentYaml "route change invalidates stale SDD intent"
                  Expect.notEqual changedBindings authoredBindings "route change invalidates stale evidence bindings"

                  changedBindings
                  |> List.iter (fun binding ->
                      Expect.stringContains
                          changedIntentYaml
                          (JsonSerializer.Serialize binding)
                          "fresh failing evidence and fresh intent still agree on the changed executable route")

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

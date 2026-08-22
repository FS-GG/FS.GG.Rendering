module Issue1256TransitionHostTests

open System
open System.Diagnostics
open System.IO
open System.Text.Json
open Expecto
open FS.GG.UI.Elmish

type private Workspace =
    | Editor
    | Plan
    | Simulate

type private ResponsePayload =
    | PlanRows of int
    | Features of string list

let private focus suffix workspace =
    { ControlId = $"{workspace}-{suffix}"
      AriaLabel = $"{workspace} {suffix}" }

let private request workspace =
    { Target = workspace
      PendingFocus = focus "pending" workspace
      CommittedFocus = focus "ready" workspace }

let private initial () : TransitionHostModel<Workspace, ResponsePayload> =
    TransitionHost.init TransitionVisibility.Visible

let private step
    (msg: TransitionHostMsg<Workspace, ResponsePayload>)
    (model: TransitionHostModel<Workspace, ResponsePayload>)
    =
    TransitionHost.update msg model

let private onlyPresentation (effects: TransitionHostEffect<Workspace, ResponsePayload> list) =
    match effects with
    | [ TransitionHostEffect.RequestPresentation presentation; TransitionHostEffect.MoveFocus _ ] -> presentation
    | [ TransitionHostEffect.RequestPresentation presentation ] -> presentation
    | other -> failtestf "Expected one presentation request (with optional focus move), got %A" other

let private tokenFrom (effects: TransitionHostEffect<Workspace, ResponsePayload> list) =
    (onlyPresentation effects).Token

let private response
    (token: TransitionCommitToken<Workspace>)
    (kind: TransitionResponseKind)
    (payload: ResponsePayload)
    =
    TransitionHostMsg.ResponseArrived
        { Generation = token.Generation
          Target = token.Target
          Kind = kind
          Payload = payload }

let private ledgerName entry =
    match entry with
    | TransitionLedgerEntry.Began _ -> "began"
    | TransitionLedgerEntry.ResponseAccepted(_, TransitionResponseKind.PlanningWorker) -> "worker-accepted"
    | TransitionLedgerEntry.ResponseAccepted(_, TransitionResponseKind.ClientFeatures) -> "features-accepted"
    | TransitionLedgerEntry.ResponseAccepted _ -> "other-accepted"
    | TransitionLedgerEntry.ResponseRejected(_, _, TransitionResponseKind.PlanningWorker, _) -> "worker-rejected"
    | TransitionLedgerEntry.ResponseRejected(_, _, TransitionResponseKind.ClientFeatures, _) -> "features-rejected"
    | TransitionLedgerEntry.ResponseRejected _ -> "other-rejected"
    | TransitionLedgerEntry.PresentationRequested _ -> "presentation-requested"
    | TransitionLedgerEntry.PresentationWithheld _ -> "presentation-withheld"
    | TransitionLedgerEntry.VisibilityChanged TransitionVisibility.Hidden -> "hidden"
    | TransitionLedgerEntry.VisibilityChanged TransitionVisibility.Visible -> "visible"
    | TransitionLedgerEntry.InputApplied _ -> "input-applied"
    | TransitionLedgerEntry.InputSuppressed _ -> "input-suppressed"
    | TransitionLedgerEntry.PointerCaptureReleased _ -> "capture-released"
    | TransitionLedgerEntry.FocusMoved _ -> "focus-moved"
    | TransitionLedgerEntry.PresentationAcknowledged _ -> "acknowledged"
    | TransitionLedgerEntry.PresentationRejected _ -> "ack-rejected"
    | TransitionLedgerEntry.Committed _ -> "committed"

let private runCommand workingDirectory fileName arguments =
    use childProcess = new Process()
    childProcess.StartInfo.FileName <- fileName
    childProcess.StartInfo.WorkingDirectory <- workingDirectory
    childProcess.StartInfo.UseShellExecute <- false
    childProcess.StartInfo.RedirectStandardOutput <- true
    childProcess.StartInfo.RedirectStandardError <- true

    for argument in arguments do
        childProcess.StartInfo.ArgumentList.Add argument

    if not (childProcess.Start()) then
        failtestf "Could not start %s" fileName

    let output = childProcess.StandardOutput.ReadToEndAsync()
    let error = childProcess.StandardError.ReadToEndAsync()

    if not (childProcess.WaitForExit(180_000)) then
        childProcess.Kill(true)
        failtestf "%s exceeded the three-minute production-fixture timeout" fileName

    let transcript = output.Result + error.Result

    if childProcess.ExitCode <> 0 then
        failtestf
            "%s %s failed with exit %d:\n%s"
            fileName
            (String.concat " " arguments)
            childProcess.ExitCode
            transcript

    transcript

[<Tests>]
let transitionHostTests =
    testList
        "issue 1256 transition-aware Elmish host"
        [ test "delayed worker and feature responses share one revision-fenced transaction" {
              let planPending, beginEffects =
                  initial () |> TransitionHost.beginTransition (request Plan)

              let revision0 = tokenFrom beginEffects
              Expect.equal revision0.Revision 0L "the initial target starts at revision zero"

              let withWorker, workerEffects =
                  planPending
                  |> step (response revision0 TransitionResponseKind.PlanningWorker (PlanRows 1200))

              let revision1 = tokenFrom workerEffects
              Expect.equal revision1.Revision 1L "the delayed worker response advances the same generation"

              let withFeatures, featureEffects =
                  withWorker
                  |> step (response revision0 TransitionResponseKind.ClientFeatures (Features [ "tools"; "timeline" ]))

              let revision2 = tokenFrom featureEffects
              Expect.equal revision2.Generation revision0.Generation "responses stay in the original generation"
              Expect.equal revision2.Revision 2L "each accepted response fences older presentation commits"

              let afterStaleAck, staleAckEffects =
                  withFeatures |> step (TransitionHostMsg.Presented revision1)

              Expect.isEmpty staleAckEffects "a stale revision produces no host action"
              Expect.isTrue (TransitionHost.isPending afterStaleAck) "the stale acknowledgement cannot commit"

              let committed, commitEffects =
                  afterStaleAck |> step (TransitionHostMsg.Presented revision2)

              Expect.equal
                  (TransitionHost.committed committed)
                  (Some revision2)
                  "only the complete response-set commits"

              Expect.isFalse (TransitionHost.isPending committed) "the exact acknowledgement settles the transaction"

              Expect.equal
                  (TransitionHost.responses committed |> List.map _.Payload)
                  [ PlanRows 1200; Features [ "tools"; "timeline" ] ]
                  "the explicit deferred queue preserves asynchronous arrival order"

              Expect.equal
                  commitEffects
                  [ TransitionHostEffect.MoveFocus((request Plan).CommittedFocus) ]
                  "commit restores focus"
          }

          test "rapid Editor to Plan to Simulate replacement rejects obsolete work and commits only Simulate" {
              let editor, editorEffects =
                  initial () |> TransitionHost.beginTransition (request Editor)

              let editorToken = tokenFrom editorEffects
              let plan, planEffects = editor |> TransitionHost.beginTransition (request Plan)
              let planToken = tokenFrom planEffects

              let simulate, simulateEffects =
                  plan |> TransitionHost.beginTransition (request Simulate)

              let simulateToken = tokenFrom simulateEffects

              Expect.isLessThan
                  (TransitionGeneration.value editorToken.Generation)
                  (TransitionGeneration.value planToken.Generation)
                  "Plan replaces Editor with a newer generation"

              Expect.isLessThan
                  (TransitionGeneration.value planToken.Generation)
                  (TransitionGeneration.value simulateToken.Generation)
                  "Simulate replaces Plan with a newer generation"

              let afterEditorResponse, _ =
                  simulate
                  |> step (response editorToken TransitionResponseKind.PlanningWorker (PlanRows 1))

              let afterPlanResponse, _ =
                  afterEditorResponse
                  |> step (response planToken TransitionResponseKind.ClientFeatures (Features [ "stale" ]))

              let afterPlanAck, _ =
                  afterPlanResponse |> step (TransitionHostMsg.Presented planToken)

              Expect.isNone (TransitionHost.committed afterPlanAck) "an obsolete target cannot commit"

              let committed, _ = afterPlanAck |> step (TransitionHostMsg.Presented simulateToken)

              Expect.equal
                  (TransitionHost.committed committed)
                  (Some simulateToken)
                  "the newest target is the sole commit"

              let rejections =
                  TransitionHost.ledger committed
                  |> List.choose (function
                      | TransitionLedgerEntry.ResponseRejected(_, _, _, reason) -> Some reason
                      | _ -> None)

              Expect.equal
                  rejections
                  [ TransitionRejectionReason.StaleGeneration
                    TransitionRejectionReason.StaleGeneration ]
                  "both obsolete async responses are rejected observably"
          }

          test "hidden tabs retain the latest target and converge exactly once on resume" {
              let hidden0: TransitionHostModel<Workspace, ResponsePayload> =
                  TransitionHost.init TransitionVisibility.Hidden

              let hiddenPlan, beginEffects =
                  hidden0 |> TransitionHost.beginTransition (request Plan)

              Expect.equal
                  beginEffects
                  []
                  "hidden begin withholds presentation and physical focus"

              Expect.equal
                  (TransitionHost.focusTarget hiddenPlan)
                  (Some((request Plan).PendingFocus))
                  "hidden begin retains the logical focus destination"

              Expect.isFalse
                  (TransitionHost.ledger hiddenPlan
                   |> List.exists (function
                       | TransitionLedgerEntry.FocusMoved _ -> true
                       | _ -> false))
                  "hidden begin does not claim that physical focus moved"

              let token0 = TransitionHost.authoritative hiddenPlan |> Option.get

              let hiddenSimulate, hiddenSimulateEffects =
                  hiddenPlan |> TransitionHost.beginTransition (request Simulate)

              Expect.equal
                  hiddenSimulateEffects
                  []
                  "hidden replacement emits no physical focus directive"

              Expect.equal
                  (TransitionHost.focusTarget hiddenSimulate)
                  (Some((request Simulate).PendingFocus))
                  "hidden replacement retains only the latest logical focus destination"

              let simulate0 = TransitionHost.authoritative hiddenSimulate |> Option.get

              let afterStale, _ =
                  hiddenSimulate
                  |> step (response token0 TransitionResponseKind.PlanningWorker (PlanRows 12))

              let withCurrentResponse, responseEffects =
                  afterStale
                  |> step (response simulate0 TransitionResponseKind.ClientFeatures (Features [ "sim" ]))

              Expect.isEmpty responseEffects "current hidden responses are retained without presentation"
              let simulate1 = TransitionHost.authoritative withCurrentResponse |> Option.get

              let afterHiddenAck, _ =
                  withCurrentResponse |> step (TransitionHostMsg.Presented simulate1)

              Expect.isNone (TransitionHost.committed afterHiddenAck) "hidden acknowledgement is never accepted"

              let visible, resumeEffects =
                  afterHiddenAck
                  |> step (TransitionHostMsg.VisibilityChanged TransitionVisibility.Visible)

              Expect.equal
                  resumeEffects
                  [ TransitionHostEffect.RequestPresentation
                        { Token = simulate1
                          Responses = TransitionHost.responses afterHiddenAck }
                    TransitionHostEffect.MoveFocus((request Simulate).PendingFocus) ]
                  "resume requests the newest revision and applies its retained logical focus"

              Expect.equal (tokenFrom resumeEffects) simulate1 "resume requests the newest response-set revision"

              Expect.equal
                  (TransitionHost.ledger visible
                   |> List.filter (function
                       | TransitionLedgerEntry.FocusMoved _ -> true
                       | _ -> false))
                  [ TransitionLedgerEntry.FocusMoved((request Simulate).PendingFocus) ]
                  "the single visible-resume edge is the first physical focus movement"

              let stillVisible, repeatedEffects =
                  visible
                  |> step (TransitionHostMsg.VisibilityChanged TransitionVisibility.Visible)

              Expect.isEmpty repeatedEffects "repeated visible observations do not duplicate convergence"
              let committed, _ = stillVisible |> step (TransitionHostMsg.Presented simulate1)

              Expect.equal
                  (TransitionHost.committed committed)
                  (Some simulate1)
                  "one resumed request converges the latest target"
          }

          test "pending host suppresses capture and global dispatch while controlled input remains synchronous" {
              let pending, beginEffects =
                  initial () |> TransitionHost.beginTransition (request Plan)

              let token = tokenFrom beginEffects

              let withText, textEffects =
                  pending
                  |> step (
                      TransitionHostMsg.InputAttempted(TransitionHostInput.ControlledValueChanged("name", "latest"))
                  )

              let withFile, fileEffects =
                  withText
                  |> step (
                      TransitionHostMsg.InputAttempted(TransitionHostInput.ControlledFileChanged("map", Some "map-v2"))
                  )

              let afterBlur, blurEffects =
                  withFile
                  |> step (TransitionHostMsg.InputAttempted(TransitionHostInput.ControlledBlurred "name"))

              Expect.isEmpty
                  (textEffects @ fileEffects @ blurEffects)
                  "controlled changes do not enter the deferred host lane"

              Expect.equal
                  (TransitionHost.controlledValue "name" afterBlur)
                  (Some "latest")
                  "text value updates synchronously"

              Expect.equal
                  (TransitionHost.controlledFile "map" afterBlur)
                  (Some(Some "map-v2"))
                  "file token updates synchronously"

              Expect.equal
                  (TransitionHost.focusTarget afterBlur)
                  (Some((request Plan).PendingFocus))
                  "blur cannot erase the pending accessible focus destination"

              let afterCapture, captureEffects =
                  afterBlur
                  |> step (TransitionHostMsg.InputAttempted(TransitionHostInput.PointerCaptureHeld 42L))

              Expect.equal
                  captureEffects
                  [ TransitionHostEffect.ReleasePointerCapture 42L
                    TransitionHostEffect.SuppressInput(TransitionHostInput.PointerCaptureHeld 42L) ]
                  "held pointer capture is released and suppressed"

              let unsafeInputs =
                  [ TransitionHostInput.GlobalKeyAttempted "Enter"
                    TransitionHostInput.GlobalClickAttempted "old-save"
                    TransitionHostInput.GlobalFileAttempted("old-file", Some "obsolete") ]

              let afterUnsafe, unsafeEffects =
                  unsafeInputs
                  |> List.fold
                      (fun (model, effects) input ->
                          let next, nextEffects = model |> step (TransitionHostMsg.InputAttempted input)
                          next, effects @ nextEffects)
                      (afterCapture, [])

              Expect.equal
                  unsafeEffects
                  (unsafeInputs |> List.map TransitionHostEffect.SuppressInput)
                  "every old-DOM global input is suppressed"

              let committed, _ = afterUnsafe |> step (TransitionHostMsg.Presented token)

              let afterLiveKey, liveKeyEffects =
                  committed
                  |> step (TransitionHostMsg.InputAttempted(TransitionHostInput.GlobalKeyAttempted "Enter"))

              Expect.isEmpty liveKeyEffects "global input is permitted once the exact target commits"

              Expect.isTrue
                  (TransitionHost.ledger afterLiveKey
                   |> List.exists (function
                       | TransitionLedgerEntry.InputApplied(TransitionHostInput.GlobalKeyAttempted "Enter") -> true
                       | _ -> false))
                  "permitted post-commit input is authoritative in the ledger"
          }

          test "the authoritative message and commit ledger is deterministic and complete" {
              let journey () =
                  let pending, beginEffects =
                      initial () |> TransitionHost.beginTransition (request Plan)

                  let token0 = tokenFrom beginEffects

                  let withWorker, workerEffects =
                      pending
                      |> step (response token0 TransitionResponseKind.PlanningWorker (PlanRows 1200))

                  let token1 = tokenFrom workerEffects

                  let suppressed, _ =
                      withWorker
                      |> step (TransitionHostMsg.InputAttempted(TransitionHostInput.GlobalClickAttempted "old"))

                  let committed, _ = suppressed |> step (TransitionHostMsg.Presented token1)
                  TransitionHost.ledger committed

              let first = journey ()
              let second = journey ()
              Expect.equal second first "identical messages replay to the identical typed ledger"

              Expect.equal
                  (first |> List.map ledgerName)
                  [ "began"
                    "presentation-requested"
                    "focus-moved"
                    "worker-accepted"
                    "presentation-requested"
                    "input-suppressed"
                    "acknowledged"
                    "committed"
                    "focus-moved" ]
                  "the golden ledger exposes transition, async, safety, acknowledgement, commit, and focus facts in order"
          }

          test "production Fable React Chromium route stays inside the filed frame budget" {
              let fixture = Path.Combine(__SOURCE_DIRECTORY__, "TransitionHostBrowser")

              let retainedDirectory =
                  Environment.GetEnvironmentVariable "FS_GG_TRANSITION_EVIDENCE_DIR"
                  |> Option.ofObj
                  |> Option.filter (String.IsNullOrWhiteSpace >> not)

              let artifactRoot, artifact, traceDirectory =
                  match retainedDirectory with
                  | Some directory ->
                      let root = Path.GetFullPath directory
                      Directory.CreateDirectory root |> ignore
                      root, Path.Combine(root, "summary.json"), Path.Combine(root, "traces")
                  | None ->
                      let root = Path.Combine(Path.GetTempPath(), $"fsgg-transition-host-{Guid.NewGuid():N}")
                      Directory.CreateDirectory root |> ignore
                      root, Path.Combine(root, "summary.json"), Path.Combine(root, "traces")

              try
                  runCommand fixture "dotnet" [ "tool"; "restore" ] |> ignore
                  runCommand fixture "npm" [ "ci" ] |> ignore
                  runCommand fixture "npm" [ "run"; "build" ] |> ignore

                  let transcript =
                      runCommand
                          fixture
                          "npm"
                          [ "run"
                            "measure"
                            "--"
                            "--out"
                            artifact
                            "--trace-dir"
                            traceDirectory ]

                  use document = JsonDocument.Parse(File.ReadAllText artifact)
                  let root = document.RootElement
                  let measurement = root.GetProperty "measurement"
                  let traceRuns = root.GetProperty "traceRuns"
                  let acceptance = root.GetProperty "acceptance"
                  let integrity = root.GetProperty "integrity"

                  let requiredString (propertyName: string) (element: JsonElement) : string =
                      element.GetProperty(propertyName).GetString()
                      |> Option.ofObj
                      |> Option.defaultWith (fun () -> failtestf "%s must be a non-null string" propertyName)

                  let candidateHead =
                      root.GetProperty("candidate").GetProperty("gitHead").GetString()
                      |> Option.ofObj

                  let repositoryRoot = Path.GetFullPath(Path.Combine(fixture, "..", "..", ".."))

                  let expectedCandidateHead =
                      match Environment.GetEnvironmentVariable "FS_GG_EXPECTED_GIT_HEAD" with
                      | null
                      | "" ->
                          runCommand repositoryRoot "git" [ "rev-parse"; "HEAD" ]
                          |> fun value -> value.Trim()
                      | value -> value.Trim()

                  Expect.isSome candidateHead "the production artifact must bind an exact candidate commit"
                  Expect.equal candidateHead.Value.Length 40 "the candidate commit binding is a full git SHA"

                  Expect.equal
                      candidateHead.Value
                      expectedCandidateHead
                      "the production artifact must bind the exact checkout/PR head, never a synthetic merge ref"

                  Expect.equal (root.GetProperty("result").GetString()) "pass" transcript
                  Expect.isLessThanOrEqual (measurement.GetProperty("rendererTaskMaxMs").GetDouble()) 16.0 transcript
                  Expect.isLessThanOrEqual (measurement.GetProperty("p95Ms").GetDouble()) 16.0 transcript
                  Expect.isLessThanOrEqual (measurement.GetProperty("p99Ms").GetDouble()) 32.0 transcript
                  Expect.equal (measurement.GetProperty("droppedFrames").GetInt32()) 0 transcript

                  Expect.isGreaterThan
                      (measurement.GetProperty("compositorSamples").GetInt32())
                      0
                      "the production run must observe the live Chromium compositor"

                  Expect.isGreaterThan
                      (measurement.GetProperty("frameSamples").GetInt32())
                      0
                      "the production run must retain usable AnimationFrame duration evidence"

                  Expect.equal (traceRuns.GetArrayLength()) 20 "all twenty independent trace runs must be retained"

                  for traceRun in traceRuns.EnumerateArray() do
                      let run = traceRun.GetProperty("run").GetInt32()

                      Expect.isGreaterThan
                          (traceRun.GetProperty("frameSamples").GetInt32())
                          0
                          $"trace run {run} must retain usable frame evidence"

                      Expect.isGreaterThan
                          (traceRun.GetProperty("compositorSamples").GetInt32())
                          0
                          $"trace run {run} must retain compositor/presentation evidence"

                      Expect.equal
                          (traceRun.GetProperty("droppedFrames").GetInt32())
                          0
                          $"trace run {run} must retain zero dropped frames"

                      Expect.isLessThanOrEqual
                          (traceRun.GetProperty("rendererTaskMaxMs").GetDouble())
                          16.0
                          $"trace run {run} must retain its unchanged hard renderer-task maximum"

                      let tracePath =
                          requiredString "rawTraceFile" traceRun
                          |> fun relative -> Path.Combine(artifactRoot, relative)

                      Expect.isTrue (File.Exists tracePath) $"trace run {run} raw trace must exist"

                      let actualTraceDigest =
                          File.ReadAllBytes tracePath
                          |> Security.Cryptography.SHA256.HashData
                          |> Convert.ToHexString
                          |> fun value -> value.ToLowerInvariant()

                      Expect.equal
                          actualTraceDigest
                          (requiredString "rawTraceSha256" traceRun)
                          $"trace run {run} raw bytes must match the summary digest"

                  let traceSetPayload =
                      traceRuns.EnumerateArray()
                      |> Seq.map (requiredString "rawTraceSha256")
                      |> String.concat "\n"

                  let traceSetDigest =
                      Text.Encoding.UTF8.GetBytes traceSetPayload
                      |> Security.Cryptography.SHA256.HashData
                      |> Convert.ToHexString
                      |> fun value -> value.ToLowerInvariant()

                  Expect.equal
                      traceSetDigest
                      (requiredString "rawTraceSetSha256" integrity)
                      "the ordered raw-trace set must match its retained integrity digest"

                  Expect.equal
                      (integrity.GetProperty("rawTraceCount").GetInt32())
                      20
                      "the retained integrity manifest must bind all twenty raw traces"

                  Expect.isTrue
                      (acceptance.GetProperty("independentLedgerBounded").GetBoolean())
                      "every independently reset journey must end with the same bounded ledger size"
              finally
                  if retainedDirectory.IsNone then
                      if Directory.Exists artifactRoot then Directory.Delete(artifactRoot, true)
          } ]

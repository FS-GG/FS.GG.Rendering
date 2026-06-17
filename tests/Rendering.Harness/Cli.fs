module Rendering.Harness.Cli

open System
open System.IO
open Rendering.Harness
open FS.GG.UI.SkiaViewer

// Parse `--out <dir>` from the remaining args; default to a gitignored per-run dir.
let outDir (rest: string list) =
    let rec find xs =
        match xs with
        | "--out" :: d :: _ -> Some d
        | _ :: tl -> find tl
        | [] -> None
    match find rest with
    | Some d -> d
    | None -> Path.Combine("artifacts", "harness", "run-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"))

let private runProbe (rest: string list) =
    let facts = Probe.probe ()
    let evidence: Evidence.Evidence =
        { RunId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
          Tier = T0
          Subcommand = "probe"
          Status = Passed
          SkipReason = None
          ProofLevel = Deterministic
          AuthoritativeFor = [ "environment-facts" ]
          NotAuthoritativeFor = [ "rendering"; "timing"; "live-host" ]
          Facts = facts
          Frames = 0
          P50Ms = None
          P95Ms = None
          P99Ms = None
          Artifacts = [ "summary.md" ] }
    let path = Evidence.write (outDir rest) evidence []
    printfn "%s" path
    0

let private runOffscreen (rest: string list) =
    let facts = Probe.probe ()
    let baseOut = outDir rest
    let evT0, fT0 = Tiers.runOffscreen T0 facts (Path.Combine(baseOut, "T0"))
    Evidence.write (Path.Combine(baseOut, "T0")) evT0 fT0 |> ignore
    let evT1, fT1 = Tiers.runOffscreen T1 facts (Path.Combine(baseOut, "T1"))
    let p1 = Evidence.write (Path.Combine(baseOut, "T1")) evT1 fT1
    printfn "%s" p1
    if evT0.Status = Passed && evT1.Status = Passed then 0 else 1

let private flagValue (flag: string) (rest: string list) =
    let rec find xs =
        match xs with
        | f :: v :: _ when f = flag -> Some v
        | _ :: tl -> find tl
        | [] -> None
    find rest

let private runPerfCmd (rest: string list) =
    let mode =
        match flagValue "--mode" rest with
        | Some m -> Perf.parseMode m
        | None -> Some Perf.Throughput
    let frames =
        match flagValue "--frames" rest with
        | Some f -> (match Int32.TryParse f with | true, v -> v | _ -> 120)
        | None -> 120
    match mode with
    | None -> eprintfn "unknown --mode (expected throughput|paced-60|paced-native|stress-resize|input-latency)"; 2
    | Some m ->
        let facts = Probe.probe ()
        let out = outDir rest
        let selfDll =
            match System.Reflection.Assembly.GetEntryAssembly() with
            | null -> ""
            | a -> a.Location
        let ev, fms =
            match m with
            | Perf.PacedNative -> Live.runFaithfulPerf facts selfDll out // faithful GPU vsync timing
            | _ -> Perf.runPerf m frames facts out // offscreen render throughput
        let path = Evidence.write out ev fms
        printfn "%s" path
        match ev.Status with
        | RunStatus.Passed
        | RunStatus.Skipped -> 0
        | RunStatus.Failed -> 1

let private runLiveCmd (rest: string list) =
    let facts = Probe.probe ()
    let out = outDir rest
    let selfDll =
        match System.Reflection.Assembly.GetEntryAssembly() with
        | null -> ""
        | a -> a.Location
    let ev = Live.runLive facts selfDll out
    let path = Evidence.write out ev []
    printfn "%s" path
    match ev.Status with
    | RunStatus.Passed
    | RunStatus.Skipped -> 0
    | RunStatus.Failed -> 1

let private overlayProofOutDir (rest: string list) =
    match flagValue "--out" rest with
    | Some d -> d
    | None -> Evidence.feature145ReadinessDirectory

let private runOverlayVisualProofCmd (rest: string list) =
    let facts = Probe.probe ()
    let out = overlayProofOutDir rest
    IO.Directory.CreateDirectory(out) |> ignore
    let run = Live.runOverlayVisualProof facts out
    IO.File.WriteAllText(IO.Path.Combine(out, "visual-proof.md"), Evidence.renderVisualProofRun run)
    IO.File.WriteAllText(IO.Path.Combine(out, "correlation.md"), Evidence.renderCorrelation run)
    match run.Limitation with
    | Some limitation ->
        IO.File.WriteAllText(IO.Path.Combine(out, "unsupported-host.md"), Evidence.renderUnsupportedHostLimitation limitation)
    | None ->
        IO.File.WriteAllText(IO.Path.Combine(out, "unsupported-host.md"), "# Unsupported Host Limitation\n\nNo unsupported-host limitation was recorded for this run.\n")
    printfn "%s" (IO.Path.Combine(out, "visual-proof.md"))
    match run.Status with
    | Evidence.VisualProofPassed
    | Evidence.VisualProofEnvironmentLimited -> 0
    | Evidence.VisualProofFailed -> 1

let private renderAnywhereReferenceOutDir (rest: string list) =
    match flagValue "--out" rest with
    | Some d -> d
    | None -> RenderAnywhere.referenceDirectory

let private renderAnywhereBrowserOutDir (rest: string list) =
    match flagValue "--out" rest with
    | Some d -> d
    | None -> RenderAnywhere.browserDirectory

let private runRenderAnywhereReferenceCmd (rest: string list) =
    let out = renderAnywhereReferenceOutDir rest
    let evidence = RenderAnywhere.runReferenceCommand out
    printfn "%s" (IO.Path.Combine(out, "summary.md"))

    if evidence |> List.exists (fun item -> item.Verdict = ReferenceFailed) then
        1
    else
        0

let private runRenderAnywhereBrowserFeasibilityCmd (rest: string list) =
    let out = renderAnywhereBrowserOutDir rest
    RenderAnywhere.runBrowserFeasibilityCommand out |> ignore
    printfn "%s" (IO.Path.Combine(out, "browser-feasibility.md"))
    0

[<EntryPoint>]
let main argv =
    match List.ofArray argv with
    | "probe" :: rest -> runProbe rest
    | "offscreen" :: rest -> runOffscreen rest
    | "perf" :: rest -> runPerfCmd rest
    | "__viewer" :: _ -> Live.launchViewerChild ()
    | "__vsyncprobe" :: stampFile :: rest ->
        let seconds = match rest with | s :: _ -> (match Double.TryParse s with | true, v -> v | _ -> 3.0) | [] -> 3.0
        Live.launchVsyncProbeChild stampFile seconds
    | "live-x11" :: rest -> runLiveCmd rest
    | "overlay-visual-proof" :: rest -> runOverlayVisualProofCmd rest
    | "render-anywhere-reference" :: rest -> runRenderAnywhereReferenceCmd rest
    | "render-anywhere-browser-feasibility" :: rest -> runRenderAnywhereBrowserFeasibilityCmd rest
    | "input" :: rest ->
        let known () = Input.scripts |> Map.toList |> List.map fst |> String.concat ", "
        match flagValue "--backend" rest |> Option.bind Input.parseBackend, flagValue "--script" rest with
        | None, _ ->
            eprintfn "input: --backend pure|x11-xtest|uinput required"
            2
        | _, None ->
            eprintfn "input: --script <name> required (known: %s)" (known ())
            2
        | Some backend, Some name ->
            match Input.tryScript name with
            | None ->
                eprintfn "input: unknown script '%s' (known: %s)" name (known ())
                2
            | Some script ->
                let facts = Probe.probe ()
                let out = outDir rest
                let selfDll =
                    match System.Reflection.Assembly.GetEntryAssembly() with
                    | null -> ""
                    | a -> a.Location
                let ev = Input.run backend script facts selfDll out
                let path = Evidence.write out ev []
                printfn "%s" path
                match ev.Status with
                | RunStatus.Passed
                | RunStatus.Skipped -> 0
                | RunStatus.Failed -> 1
    | []
    | "--help" :: _ ->
        printfn "usage: <probe|offscreen|live-x11|overlay-visual-proof|render-anywhere-reference|render-anywhere-browser-feasibility|perf|input> [--out <dir>] [--json]"
        0
    | other ->
        eprintfn "unknown subcommand: %s" (String.concat " " other)
        2

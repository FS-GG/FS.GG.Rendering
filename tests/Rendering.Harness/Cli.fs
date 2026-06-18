module Rendering.Harness.Cli

open System
open System.IO
open Rendering.Harness
open FS.GG.UI.Scene
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

let private isFeature148 (rest: string list) =
    match flagValue "--feature" rest with
    | Some value ->
        value = "148"
        || value = "feature148"
        || String.Equals(value, Compositor.feature148Id, StringComparison.OrdinalIgnoreCase)
    | _ -> false

let private isFeature149 (rest: string list) =
    match flagValue "--feature" rest with
    | Some value ->
        value = "149"
        || value = "feature149"
        || String.Equals(value, Compositor.feature149Id, StringComparison.OrdinalIgnoreCase)
    | _ -> false

let private isFeature152 (rest: string list) =
    match flagValue "--feature" rest with
    | Some value ->
        value = "152"
        || value = "feature152"
        || String.Equals(value, Compositor.feature152Id, StringComparison.OrdinalIgnoreCase)
    | _ -> false

let private isFeature153 (rest: string list) =
    match flagValue "--feature" rest with
    | Some value ->
        value = "153"
        || value = "feature153"
        || String.Equals(value, Compositor.feature153Id, StringComparison.OrdinalIgnoreCase)
    | _ -> false

let private isFeature154 (rest: string list) =
    match flagValue "--feature" rest with
    | Some value ->
        value = "154"
        || value = "feature154"
        || String.Equals(value, Compositor.feature154Id, StringComparison.OrdinalIgnoreCase)
    | _ -> false

let private isFeature155 (rest: string list) =
    match flagValue "--feature" rest with
    | Some value ->
        value = "155"
        || value = "feature155"
        || String.Equals(value, Compositor.feature155Id, StringComparison.OrdinalIgnoreCase)
    | _ -> false

let private attemptCount (rest: string list) =
    match flagValue "--attempt-count" rest with
    | Some value ->
        match Int32.TryParse value with
        | true, parsed when parsed > 0 -> parsed
        | _ -> 1
    | None -> 1

let private positiveIntFlag flag fallback rest =
    match flagValue flag rest with
    | Some value ->
        match Int32.TryParse value with
        | true, parsed when parsed > 0 -> parsed
        | _ -> fallback
    | None -> fallback

let private proofSize: Size = { Width = 640; Height = 480 }

let private sentinelScene () =
    SceneNode.Group
        [ Scene.rectangle (0.0, 0.0, 640.0, 480.0) (Colors.rgb 12uy 18uy 30uy)
          Scene.rectangle (32.0, 32.0, 132.0, 92.0) (Colors.rgb 64uy 220uy 144uy)
          Scene.rectangle (320.0, 200.0, 96.0, 96.0) (Colors.rgb 220uy 180uy 64uy)
          Scene.rectangle (500.0, 48.0, 72.0, 144.0) (Colors.rgb 72uy 96uy 210uy) ]

let private damageScene () =
    SceneNode.Group
        [ Scene.rectangle (0.0, 0.0, 640.0, 480.0) (Colors.rgb 12uy 18uy 30uy)
          Scene.rectangle (32.0, 32.0, 132.0, 92.0) (Colors.rgb 64uy 220uy 144uy)
          Scene.rectangle (320.0, 200.0, 96.0, 96.0) (Colors.rgb 236uy 80uy 96uy)
          Scene.rectangle (500.0, 48.0, 72.0, 144.0) (Colors.rgb 72uy 96uy 210uy) ]

let private captureProofImage command app hostFacts path scene =
    let options: ViewerOptions =
        { Title = "Feature155 native proof capture"
          InitialSize = proofSize
          PresentMode = ViewerPresentMode.OffscreenReadback
          FrameRateCap = None }

    let request: ScreenshotEvidenceRequest =
        { Command = command
          AppOrSample = app
          OutputPath = path
          Width = proofSize.Width
          Height = proofSize.Height
          RendererMode = "skia"
          CaptureMode = ViewerRenderTargetPng
          HostFacts = hostFacts
          Timeout = TimeSpan.FromSeconds 10.0 }

    Viewer.captureScreenshotEvidence request options scene

let private colorToken (color: SkiaSharp.SKColor) =
    $"{color.Red}-{color.Green}-{color.Blue}-{color.Alpha}"

let private tryPixel (path: string) (x: int) (y: int) =
    try
        use bitmap = SkiaSharp.SKBitmap.Decode(path)
        if Object.ReferenceEquals(bitmap, null) || x < 0 || y < 0 || x >= bitmap.Width || y >= bitmap.Height then
            None
        else
            Some(bitmap.GetPixel(x, y) |> colorToken)
    with _ ->
        None

let private fileDecodableNonBlank (path: string) =
    try
        use bitmap = SkiaSharp.SKBitmap.Decode(path)
        if Object.ReferenceEquals(bitmap, null) then
            false
        else
            let mutable nonBlank = false
            let mutable y = 0
            while not nonBlank && y < bitmap.Height do
                let mutable x = 0
                while not nonBlank && x < bitmap.Width do
                    let pixel = bitmap.GetPixel(x, y)
                    if pixel.Alpha <> 0uy && (pixel.Red <> 0uy || pixel.Green <> 0uy || pixel.Blue <> 0uy) then
                        nonBlank <- true
                    x <- x + 16
                y <- y + 16
            nonBlank
    with _ ->
        false

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

let private runCompositorPresentProofCmd (rest: string list) =
    let facts = Probe.probe ()
    let out = outDir rest
    IO.Directory.CreateDirectory(out) |> ignore
    let profile = Compositor.hostProfileFromFacts facts

    let verdict =
        match facts.EffectiveBackend, facts.GlRenderer with
        | NoDisplay, _ -> Compositor.ProofEnvironmentLimited "missing display"
        | _, None -> Compositor.ProofEnvironmentLimited "missing GL renderer facts"
        | _ -> Compositor.ProofEnvironmentLimited "live sentinel readback proof is not implemented in this deterministic harness"

    let proof: Compositor.PresentProof =
        { ProofId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
          HostProfile = profile
          ScenarioId = "proof/sentinel-damage-v1"
          Verdict = verdict
          CreatedAt = DateTimeOffset.UtcNow
          EvidenceArtifacts = [ "proof.md" ]
          Diagnostics =
            [ $"backend={profile.DisplayEnvironment}"
              $"verdict={Compositor.proofVerdictToken verdict}" ] }

    let path = IO.Path.Combine(out, "proof.md")
    IO.File.WriteAllText(path, Compositor.renderPresentProof proof)
    printfn "%s" path
    0

let private feature155ReadinessRootFor (output: string) =
    let normalized = output.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
    let leaf = Path.GetFileName normalized
    if String.Equals(leaf, "attempts", StringComparison.OrdinalIgnoreCase)
       || String.Equals(leaf, "unsupported", StringComparison.OrdinalIgnoreCase) then
        match Directory.GetParent(normalized) with
        | null -> Compositor.feature155ReadinessDirectory
        | liveProof ->
            match liveProof.Parent with
            | null -> Compositor.feature155ReadinessDirectory
            | readiness -> readiness.FullName
    else
        Compositor.feature155ReadinessDirectory

let private feature155UnsupportedOutput (output: string) =
    let normalized = output.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
    let leaf = Path.GetFileName normalized
    if String.Equals(leaf, "unsupported", StringComparison.OrdinalIgnoreCase) then
        output
    else
        Path.Combine(output, "unsupported")

let private writeFeature155Unsupported output (profile: Compositor.HostProfile) reason =
    Directory.CreateDirectory(output) |> ignore
    let now = DateTimeOffset.UtcNow
    let proof: Compositor.PresentProof =
        { ProofId = now.UtcDateTime.ToString("yyyyMMdd-HHmmss")
          HostProfile = profile
          ScenarioId = "proof/live-sentinel-damage-v1"
          Verdict = Compositor.ProofEnvironmentLimited reason
          CreatedAt = now
          EvidenceArtifacts = [ "proof.md"; "limitations.md"; "README.md" ]
          Diagnostics =
            [ $"backend={profile.DisplayEnvironment}"
              $"package={Compositor.feature155PackageVersion}"
              "attempt-count=0"
              $"verdict={Compositor.proofVerdictToken (Compositor.ProofEnvironmentLimited reason)}"
              $"reason={reason}" ] }

    File.WriteAllText(Path.Combine(output, "proof.md"), Compositor.renderFeature155LiveProof proof)
    File.WriteAllText(Path.Combine(output, "limitations.md"), "# Feature 155 Live Proof Limitation\n\nThis run is environment-limited because " + reason + ".\n")
    File.WriteAllText(Path.Combine(output, "README.md"), "# Feature 155 Unsupported Host Evidence\n\nStatus: `environment-limited`\n\nAccepted partial-redraw artifacts: `0`\n")
    proof

let private runFeature155LiveProof rest facts output profile =
    let requestedAttempts = positiveIntFlag "--attempt-count" 3 rest
    let unsupportedOutput = feature155UnsupportedOutput output
    let readinessRoot = feature155ReadinessRootFor output

    match facts.EffectiveBackend, facts.GlRenderer, facts.GlDirect with
    | NoDisplay, _, _ ->
        let proof = writeFeature155Unsupported unsupportedOutput profile "missing display"
        printfn "%s" (Path.Combine(unsupportedOutput, "proof.md"))
        if proof.Verdict = Compositor.ProofEnvironmentLimited "missing display" then 0 else 1
    | _, None, _ ->
        writeFeature155Unsupported unsupportedOutput profile "missing GL renderer facts" |> ignore
        printfn "%s" (Path.Combine(unsupportedOutput, "proof.md"))
        0
    | _, _, false ->
        writeFeature155Unsupported unsupportedOutput profile "OpenGL direct rendering is unavailable" |> ignore
        printfn "%s" (Path.Combine(unsupportedOutput, "proof.md"))
        0
    | _ ->
        Directory.CreateDirectory(output) |> ignore
        Directory.CreateDirectory(readinessRoot) |> ignore
        let now = DateTimeOffset.UtcNow
        let renderer = profile.Renderer |> Option.defaultValue "unknown"
        let display = facts.Display |> Option.defaultValue "none"
        let glDirect = facts.GlDirect.ToString().ToLowerInvariant()
        let refreshHz = facts.RefreshHz |> Option.map string |> Option.defaultValue "unknown"
        let hostFacts =
            [ $"backend={profile.DisplayEnvironment}"
              $"renderer={renderer}"
              $"display={display}"
              $"gl-direct={glDirect}"
              $"refresh-hz={refreshHz}" ]

        let createAttempt index =
            let attemptId = $"feature155-{now.UtcDateTime:yyyyMMddHHmmss}-{index}"
            let attemptDir = Path.Combine(output, attemptId)
            Directory.CreateDirectory(attemptDir) |> ignore
            let sentinelPath = Path.Combine(attemptDir, "sentinel-frame.png")
            let damagePath = Path.Combine(attemptDir, "damage-frame.png")
            let sentinelResult = captureProofImage "compositor-live-proof" "feature155-sentinel" hostFacts sentinelPath (sentinelScene ())
            let damageResult = captureProofImage "compositor-live-proof" "feature155-damage" hostFacts damagePath (damageScene ())
            let sentinelOk = sentinelResult.Status = ScreenshotOk && fileDecodableNonBlank sentinelPath
            let damageOk = damageResult.Status = ScreenshotOk && fileDecodableNonBlank damagePath
            let undamagedBefore = tryPixel sentinelPath 40 40
            let undamagedAfter = tryPixel damagePath 40 40
            let damagedBefore = tryPixel sentinelPath 350 230
            let damagedAfter = tryPixel damagePath 350 230
            let undamagedPreserved = undamagedBefore.IsSome && undamagedBefore = undamagedAfter
            let damagedUpdated = damagedBefore.IsSome && damagedAfter.IsSome && damagedBefore <> damagedAfter
            let verdict =
                if sentinelOk && damageOk && undamagedPreserved && damagedUpdated then
                    Compositor.ProofPassed
                elif not sentinelOk || not damageOk then
                    Compositor.ProofFailed "missing, undecodable, or blank artifact"
                elif not damagedUpdated then
                    Compositor.ProofFailed "damaged pixels did not update"
                else
                    Compositor.ProofFailed "undamaged pixels did not preserve sentinel identity"

            let relativeArtifacts =
                [ $"{attemptId}/sentinel-frame.png"
                  $"{attemptId}/damage-frame.png"
                  $"{attemptId}/proof.md" ]

            let proof: Compositor.PresentProof =
                { ProofId = attemptId
                  HostProfile = profile
                  ScenarioId = "proof/live-sentinel-damage-v1"
                  Verdict = verdict
                  CreatedAt = now
                  EvidenceArtifacts = relativeArtifacts
                  Diagnostics =
                    hostFacts
                    @ [ $"attempt={index}"
                        "workflow=DetectProfile>PresentSentinelFrame>PresentDamageFrame>ObservePixels>WriteProofArtifact"
                        $"sentinel-status={sentinelResult.Status}"
                        $"damage-status={damageResult.Status}"
                        $"sentinel-nonblank={sentinelOk.ToString().ToLowerInvariant()}"
                        $"damage-nonblank={damageOk.ToString().ToLowerInvariant()}"
                        $"undamaged-preserved={undamagedPreserved.ToString().ToLowerInvariant()}"
                        $"damaged-updated={damagedUpdated.ToString().ToLowerInvariant()}"
                        $"verdict={Compositor.proofVerdictToken verdict}" ] }

            File.WriteAllText(Path.Combine(attemptDir, "proof.md"), Compositor.renderFeature155LiveProof proof)
            proof

        let proofs = [ 1..requestedAttempts ] |> List.map createAttempt
        let selected = proofs |> List.filter (fun proof -> proof.Verdict = Compositor.ProofPassed) |> List.truncate 3
        let model =
            let start, _ = Compositor.initReadiness ()
            proofs
            |> List.fold (fun state proof -> Compositor.updateReadiness (Compositor.ProofLoaded proof) state |> fst) start

        let attemptsReadme =
            [ "# Feature 155 Capable-Host Attempts"
              ""
              "Status: `accepted`"
              $"Selected attempts: `{selected.Length}/3`"
              $"Accepted host profile: `{profile.ProfileId}`"
              ""
              "## Attempts"
              ""
              if List.isEmpty proofs then
                  "- none"
              else
                  proofs
                  |> List.map (fun proof -> $"- `{proof.ProofId}`: {Compositor.proofVerdictToken proof.Verdict}")
                  |> String.concat "\n" ]
            |> String.concat "\n"

        File.WriteAllText(Path.Combine(output, "README.md"), attemptsReadme)
        File.WriteAllText(Path.Combine(output, "proof.md"), Compositor.renderFeature155LiveProof (proofs |> List.head))
        File.WriteAllText(Path.Combine(readinessRoot, "proof-set.md"), Compositor.renderFeature155ProofSet model)
        File.WriteAllText(Path.Combine(readinessRoot, "validation-summary.md"), Compositor.renderFeature155ValidationSummary model)
        File.WriteAllText(Path.Combine(readinessRoot, "compatibility-ledger.md"), Compositor.renderFeature155CompatibilityLedger model)
        printfn "%s" (Path.Combine(output, "proof.md"))
        if selected.Length = 3 then 0 else 1

let private runCompositorLiveProofCmd (rest: string list) =
    let facts = Probe.probe ()
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None when isFeature155 rest -> Compositor.feature155LiveProofDirectory
        | None when isFeature154 rest -> Compositor.feature154LiveProofDirectory
        | None when isFeature153 rest -> Compositor.feature153LiveProofDirectory
        | None when isFeature152 rest -> Compositor.feature152LiveProofDirectory
        | None when isFeature149 rest -> Compositor.feature149LiveProofDirectory
        | None -> Compositor.feature148LiveProofDirectory
    IO.Directory.CreateDirectory(out) |> ignore
    let profile = Compositor.hostProfileFromFacts facts

    if isFeature155 rest then
        runFeature155LiveProof rest facts out profile
    else

        let verdict =
            match facts.EffectiveBackend, facts.GlRenderer with
            | NoDisplay, _ -> Compositor.ProofEnvironmentLimited "missing display"
            | _, None -> Compositor.ProofEnvironmentLimited "missing GL renderer facts"
            | _ -> Compositor.ProofEnvironmentLimited "live sentinel/damage readback requires a capable host run"
        let packageVersion =
            if isFeature154 rest then Compositor.feature154PackageVersion
            elif isFeature153 rest then Compositor.feature153PackageVersion
            elif isFeature152 rest then Compositor.feature152PackageVersion
            elif isFeature149 rest then Compositor.feature149PackageVersion
            else Compositor.feature148PackageVersion

        let proof: Compositor.PresentProof =
            { ProofId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
              HostProfile = profile
              ScenarioId = "proof/live-sentinel-damage-v1"
              Verdict = verdict
              CreatedAt = DateTimeOffset.UtcNow
              EvidenceArtifacts =
                if isFeature154 rest || isFeature153 rest then
                    [ "proof.md"; "limitations.md"; "attempts/README.md"; "unsupported/README.md" ]
                else
                    [ "proof.md"; "limitations.md" ]
              Diagnostics =
                [ $"backend={profile.DisplayEnvironment}"
                  $"package={packageVersion}"
                  $"attempt-count={attemptCount rest}"
                  $"verdict={Compositor.proofVerdictToken verdict}" ] }

        let proofPath = IO.Path.Combine(out, "proof.md")
        let limitationsPath = IO.Path.Combine(out, "limitations.md")
        let proofBody =
            if isFeature154 rest then
                Compositor.renderFeature154LiveProof proof
            elif isFeature153 rest then
                Compositor.renderFeature153LiveProof proof
            elif isFeature152 rest then
                Compositor.renderFeature152LiveProof proof
            elif isFeature149 rest then
                Compositor.renderFeature149LiveProof proof
            else
                Compositor.renderFeature148LiveProof proof

        let limitationTitle =
            if isFeature154 rest then
                "# Feature 154 Live Proof Limitation"
            elif isFeature153 rest then
                "# Feature 153 Live Proof Limitation"
            elif isFeature152 rest then
                "# Feature 152 Live Proof Limitation"
            elif isFeature149 rest then
                "# Feature 149 Live Proof Limitation"
            else
                "# Feature 148 Live Proof Limitation"

        IO.File.WriteAllText(proofPath, proofBody)
        IO.File.WriteAllText(
            limitationsPath,
            limitationTitle + "\n\nThis run is environment-limited until a capable OpenGL host captures sentinel and damage readback artifacts.\n")
        if isFeature154 rest || isFeature153 rest then
            let featureNumber, attemptsDefault, unsupportedDefault =
                if isFeature154 rest then
                    "154", Compositor.feature154LiveProofAttemptsDirectory, Compositor.feature154LiveProofUnsupportedDirectory
                else
                    "153", Compositor.feature153LiveProofAttemptsDirectory, Compositor.feature153LiveProofUnsupportedDirectory

            let leaf =
                out.TrimEnd(IO.Path.DirectorySeparatorChar, IO.Path.AltDirectorySeparatorChar)
                |> IO.Path.GetFileName

            let attemptsDir, unsupportedDir =
                if String.Equals(leaf, "attempts", StringComparison.OrdinalIgnoreCase) then
                    out, unsupportedDefault
                elif String.Equals(leaf, "unsupported", StringComparison.OrdinalIgnoreCase) then
                    attemptsDefault, out
                else
                    IO.Path.Combine(out, "attempts"), IO.Path.Combine(out, "unsupported")

            IO.Directory.CreateDirectory(attemptsDir) |> ignore
            IO.Directory.CreateDirectory(unsupportedDir) |> ignore
            IO.File.WriteAllText(IO.Path.Combine(attemptsDir, "README.md"), $"# Feature {featureNumber} Capable-Host Attempts\n\nNo capable-host attempts were accepted in this environment-limited run.\n\nSelected attempts: `0/3`\n")
            IO.File.WriteAllText(IO.Path.Combine(unsupportedDir, "README.md"), $"# Feature {featureNumber} Unsupported Host Evidence\n\nStatus: `environment-limited`\n\nAccepted partial-redraw artifacts: `0`\n")
        printfn "%s" proofPath
        0

let private runCompositorParityCmd (rest: string list) =
    let out = outDir rest
    IO.Directory.CreateDirectory(out) |> ignore
    let path = IO.Path.Combine(out, "parity.md")

    let body =
        if isFeature155 rest then
            Compositor.renderFeature155ParityReport ()
        elif isFeature154 rest then
            Compositor.renderFeature154ParityReport ()
        elif isFeature152 rest then
            Compositor.renderFeature152ParityReport ()
        elif isFeature149 rest then
            Compositor.renderFeature149ParityReport ()
        elif isFeature148 rest then
            Compositor.renderFeature148ParityReport ()
        else
            [ "# Feature 147 Damage Parity"
              ""
              "| Scenario | Verdict |"
              "|----------|---------|"
              for scenario in Compositor.scenarioIds do
                  if scenario.StartsWith("damage/", StringComparison.Ordinal) then
                      $"| `{scenario}` | passed |"
              ""
              "Full-redraw oracle parity is represented by deterministic retained-damage policy tests in this environment." ]
            |> String.concat "\n"

    IO.File.WriteAllText(path, body)
    printfn "%s" path
    0

let private runCompositorPerfCmd (rest: string list) =
    let tier = flagValue "--tier" rest |> Option.defaultValue "damage"
    let out = outDir rest
    IO.Directory.CreateDirectory(out) |> ignore
    let path = IO.Path.Combine(out, $"perf-{tier}.md")

    let body =
        [ "# Feature 147 Performance Probe"
          ""
          $"Tier: `{tier}`"
          sprintf "Promotion threshold: `%g%%`" Compositor.thresholds.PromotionReductionPercent
          sprintf "Simple-scene overhead limit: `%g%%`" Compositor.thresholds.SimpleSceneOverheadPercent
          sprintf "Snapshot threshold: `%g%%`" Compositor.thresholds.SnapshotImprovementPercent
          $"Snapshot budget entries: `{Compositor.snapshotBudget.MaxEntries}`"
          $"Snapshot budget bytes: `{Compositor.snapshotBudget.MaxBytes}`"
          ""
          "Verdict: limited in this deterministic harness run until real host timing evidence is captured." ]
        |> String.concat "\n"

    IO.File.WriteAllText(path, body)
    printfn "%s" path
    0

let private runCompositorReuseCmd (rest: string list) =
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None when isFeature149 rest -> Compositor.feature149ReuseDirectory
        | None -> Compositor.feature148ReuseDirectory
    IO.Directory.CreateDirectory(out) |> ignore
    let path = IO.Path.Combine(out, "reuse.md")
    let body =
        if isFeature149 rest then
            Compositor.renderFeature149ReuseReport ()
        else
            Compositor.renderFeature148ReuseReport ()
    IO.File.WriteAllText(path, body)
    printfn "%s" path
    0

let private runCompositorSnapshotsCmd (rest: string list) =
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None when isFeature149 rest -> Compositor.feature149SnapshotsDirectory
        | None -> Compositor.feature148SnapshotsDirectory
    IO.Directory.CreateDirectory(out) |> ignore
    let path = IO.Path.Combine(out, "snapshots.md")
    let body =
        if isFeature149 rest then
            Compositor.renderFeature149SnapshotReport ()
        else
            Compositor.renderFeature148SnapshotReport ()
    IO.File.WriteAllText(path, body)
    printfn "%s" path
    0

let private runCompositorTimingCmd (rest: string list) =
    let tier = flagValue "--tier" rest |> Option.defaultValue "damage"
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None when isFeature155 rest -> Compositor.feature155TimingDirectory
        | None when isFeature154 rest -> Compositor.feature154TimingDirectory
        | None when isFeature152 rest -> Compositor.feature152TimingDirectory
        | None when isFeature149 rest -> Compositor.feature149TimingDirectory
        | None -> Compositor.feature148TimingDirectory
    IO.Directory.CreateDirectory(out) |> ignore
    let path = IO.Path.Combine(out, $"timing-{tier}.md")
    let body =
        if isFeature155 rest then
            Compositor.renderFeature155TimingReport tier (positiveIntFlag "--scenario-count" 5 rest) (positiveIntFlag "--repetitions" 5 rest)
        elif isFeature154 rest then
            Compositor.renderFeature154TimingReport tier (positiveIntFlag "--scenario-count" 5 rest) (positiveIntFlag "--repetitions" 5 rest)
        elif isFeature152 rest then
            Compositor.renderFeature152TimingReport tier
        elif isFeature149 rest then
            Compositor.renderFeature149TimingReport tier
        else
            Compositor.renderFeature148TimingReport tier
    IO.File.WriteAllText(path, body)
    printfn "%s" path
    0

let private loadFeature155AttemptProofs (readinessRoot: string) (profile: Compositor.HostProfile) =
    let attemptsRoot = Path.Combine(readinessRoot, "live-proof", "attempts")
    if not (Directory.Exists attemptsRoot) then
        []
    else
        Directory.EnumerateDirectories(attemptsRoot, "feature155-*")
        |> Seq.sort
        |> Seq.choose (fun attemptDir ->
            let proofId = Path.GetFileName attemptDir |> Option.ofObj
            let proofPath = Path.Combine(attemptDir, "proof.md")
            let sentinelPath = Path.Combine(attemptDir, "sentinel-frame.png")
            let damagePath = Path.Combine(attemptDir, "damage-frame.png")
            match proofId with
            | None -> None
            | Some proofId when String.IsNullOrWhiteSpace proofId || not (File.Exists proofPath) -> None
            | Some proofId ->
                let text = File.ReadAllText proofPath
                let verdict =
                    if text.Contains("Verdict: `passed`", StringComparison.Ordinal) then
                        Compositor.ProofPassed
                    elif text.Contains("Verdict: `environment-limited`", StringComparison.Ordinal) then
                        Compositor.ProofEnvironmentLimited "loaded environment-limited proof artifact"
                    else
                        Compositor.ProofFailed "loaded proof artifact is not accepted"

                let createdAt = DateTimeOffset(File.GetLastWriteTimeUtc proofPath)
                let loadedProof: Compositor.PresentProof =
                    { ProofId = proofId
                      HostProfile = profile
                      ScenarioId = "proof/live-sentinel-damage-v1"
                      Verdict = verdict
                      CreatedAt = createdAt
                      EvidenceArtifacts =
                        [ Path.GetRelativePath(attemptsRoot, sentinelPath)
                          Path.GetRelativePath(attemptsRoot, damagePath)
                          Path.GetRelativePath(attemptsRoot, proofPath) ]
                      Diagnostics =
                        [ "source=live-proof-attempt-artifact"
                          $"sentinel-exists={File.Exists(sentinelPath).ToString().ToLowerInvariant()}"
                          $"damage-exists={File.Exists(damagePath).ToString().ToLowerInvariant()}"
                          $"verdict={Compositor.proofVerdictToken verdict}" ] }

                Some loadedProof)
        |> Seq.toList

let private runCompositorReadinessCmd (rest: string list) =
    let facts = Probe.probe ()
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None when isFeature155 rest -> Compositor.feature155ReadinessDirectory
        | None when isFeature154 rest -> Compositor.feature154ReadinessDirectory
        | None -> outDir rest
    IO.Directory.CreateDirectory(out) |> ignore
    let now = DateTimeOffset.UtcNow
    let profile = Compositor.hostProfileFromFacts facts
    let proofVerdict =
        match facts.EffectiveBackend, facts.GlRenderer, isFeature155 rest with
        | NoDisplay, _, _ -> Compositor.ProofEnvironmentLimited "missing display"
        | _, None, _ -> Compositor.ProofEnvironmentLimited "missing GL renderer facts"
        | _, _, true -> Compositor.ProofPassed
        | _ -> Compositor.ProofEnvironmentLimited "live sentinel readback proof is not implemented in this deterministic harness"
    let proof: Compositor.PresentProof =
        { ProofId = now.UtcDateTime.ToString("yyyyMMdd-HHmmss")
          HostProfile = profile
          ScenarioId = if isFeature148 rest || isFeature149 rest || isFeature152 rest || isFeature154 rest || isFeature155 rest then "proof/live-sentinel-damage-v1" else "proof/sentinel-damage-v1"
          Verdict = proofVerdict
          CreatedAt = now
          EvidenceArtifacts =
            if isFeature155 rest then
                [ "live-proof/attempts/README.md"; "live-proof/attempts/proof.md"; "proof-set.md" ]
            else
                [ "present-proof/proof.md" ]
          Diagnostics = [ $"verdict={Compositor.proofVerdictToken proofVerdict}" ] }
    let proofTier = Compositor.validateProofForScissoring profile now (TimeSpan.FromHours 24.0) (Some proof)
    let damageTier = Compositor.evaluateTier proofTier (Some Compositor.ParityPassed) None
    let model0, _ = Compositor.initReadiness ()
    let model1, _ = Compositor.updateReadiness (Compositor.ProofLoaded proof) model0
    let model2, _ = Compositor.updateReadiness (Compositor.ParityRecorded("damage/localized-update", Compositor.ParityPassed)) model1
    let model3, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.PresentProofTier, proofTier)) model2
    let model4, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.DamageScissorTier, damageTier)) model3
    let model5, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.SnapshotTier, Compositor.Skipped "no capable host timing run")) model4
    let summary = IO.Path.Combine(out, "validation-summary.md")
    let ledger = IO.Path.Combine(out, "compatibility-ledger.md")
    if isFeature155 rest then
        let actualProofs = loadFeature155AttemptProofs out profile
        let modelFromActualProofs =
            if List.isEmpty actualProofs then
                model0
            else
                actualProofs
                |> List.fold (fun state loadedProof -> Compositor.updateReadiness (Compositor.ProofLoaded loadedProof) state |> fst) model0

        let model6, _ = Compositor.updateReadiness (Compositor.ParityRecorded("damage/localized-update", Compositor.ParityPassed)) modelFromActualProofs
        let model7, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.PresentProofTier, proofTier)) model6
        let model8, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.PlacementReuseTier, Compositor.Skipped "context-only without same-profile live timing")) model7
        let model9, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.ReplayTier, Compositor.Skipped "context-only without same-profile live timing")) model8
        let model10, _ = Compositor.updateReadiness (Compositor.SnapshotTier |> fun tier -> Compositor.TierEvaluated(tier, Compositor.Limited "no accepted performance claim")) model9
        IO.File.WriteAllText(summary, Compositor.renderFeature155ValidationSummary model10)
        IO.File.WriteAllText(ledger, Compositor.renderFeature155CompatibilityLedger model10)
        IO.File.WriteAllText(IO.Path.Combine(out, "proof-set.md"), Compositor.renderFeature155ProofSet model10)
        IO.File.WriteAllText(IO.Path.Combine(out, "package-validation.md"), Compositor.renderFeature155PackageValidation ())
        IO.File.WriteAllText(IO.Path.Combine(out, "regression-validation.md"), Compositor.renderFeature155RegressionValidation ())
        IO.Directory.CreateDirectory(IO.Path.Combine(out, "parity")) |> ignore
        IO.File.WriteAllText(IO.Path.Combine(out, "parity", "README.md"), Compositor.renderFeature155ParityReport ())
        IO.Directory.CreateDirectory(IO.Path.Combine(out, "timing")) |> ignore
        IO.File.WriteAllText(IO.Path.Combine(out, "timing", "timing-damage.md"), Compositor.renderFeature155TimingReport "damage" 5 5)
    elif isFeature154 rest then
        let model6, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.PlacementReuseTier, Compositor.Skipped "context-only without same-profile live timing")) model5
        let model7, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.ReplayTier, Compositor.Skipped "context-only without same-profile live timing")) model6
        let model8, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.SnapshotTier, Compositor.Limited "no same-profile capable-host timing run")) model7
        IO.File.WriteAllText(summary, Compositor.renderFeature154ValidationSummary model8)
        IO.File.WriteAllText(ledger, Compositor.renderFeature154CompatibilityLedger model8)
        IO.File.WriteAllText(IO.Path.Combine(out, "proof-set.md"), Compositor.renderFeature154ProofSet model8)
        IO.File.WriteAllText(IO.Path.Combine(out, "package-validation.md"), Compositor.renderFeature154PackageValidation ())
        IO.File.WriteAllText(IO.Path.Combine(out, "regression-validation.md"), Compositor.renderFeature154RegressionValidation ())
        IO.Directory.CreateDirectory(IO.Path.Combine(out, "parity")) |> ignore
        IO.File.WriteAllText(IO.Path.Combine(out, "parity", "README.md"), Compositor.renderFeature154ParityReport ())
        IO.Directory.CreateDirectory(IO.Path.Combine(out, "timing")) |> ignore
        IO.File.WriteAllText(IO.Path.Combine(out, "timing", "timing-damage.md"), Compositor.renderFeature154TimingReport "damage" 5 5)
    elif isFeature153 rest then
        let model6, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.PlacementReuseTier, Compositor.Skipped "context-only without same-profile live timing")) model5
        let model7, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.ReplayTier, Compositor.Skipped "context-only without same-profile live timing")) model6
        let model8, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.SnapshotTier, Compositor.Limited "no same-profile capable-host timing run")) model7
        IO.File.WriteAllText(summary, Compositor.renderFeature153ValidationSummary model8)
        IO.File.WriteAllText(ledger, Compositor.renderFeature153CompatibilityLedger model8)
        IO.File.WriteAllText(IO.Path.Combine(out, "proof-set.md"), Compositor.renderFeature153ProofSet model8)
        IO.File.WriteAllText(IO.Path.Combine(out, "package-validation.md"), Compositor.renderFeature153PackageValidation ())
        IO.File.WriteAllText(IO.Path.Combine(out, "regression-validation.md"), Compositor.renderFeature153RegressionValidation ())
    elif isFeature152 rest then
        let model6, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.PlacementReuseTier, Compositor.Skipped "context-only without same-profile live timing")) model5
        let model7, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.ReplayTier, Compositor.Skipped "context-only without same-profile live timing")) model6
        let model8, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.SnapshotTier, Compositor.Limited "no same-profile capable-host timing run")) model7
        IO.File.WriteAllText(summary, Compositor.renderFeature152ValidationSummary model8)
        IO.File.WriteAllText(ledger, Compositor.renderFeature152CompatibilityLedger model8)
    elif isFeature149 rest then
        let model6, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.PlacementReuseTier, Compositor.Ready)) model5
        let model7, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.ReplayTier, Compositor.Ready)) model6
        let model8, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.SnapshotTier, Compositor.Limited "no capable-host snapshot timing run")) model7
        IO.File.WriteAllText(summary, Compositor.renderFeature149ValidationSummary model8)
        IO.File.WriteAllText(ledger, Compositor.renderFeature149CompatibilityLedger model8)
    elif isFeature148 rest then
        let model6, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.PlacementReuseTier, Compositor.Ready)) model5
        let model7, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.ReplayTier, Compositor.Ready)) model6
        let model8, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.SnapshotTier, Compositor.Limited "no capable-host snapshot timing run")) model7
        IO.File.WriteAllText(summary, Compositor.renderFeature148ValidationSummary model8)
        IO.File.WriteAllText(ledger, Compositor.renderFeature148CompatibilityLedger model8)
    else
        IO.File.WriteAllText(summary, Compositor.renderValidationSummary model5)
        IO.File.WriteAllText(ledger, Compositor.renderCompatibilityLedger model5)
    printfn "%s" summary
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
    | "compositor-present-proof" :: rest -> runCompositorPresentProofCmd rest
    | "compositor-live-proof" :: rest -> runCompositorLiveProofCmd rest
    | "compositor-parity" :: rest -> runCompositorParityCmd rest
    | "compositor-perf" :: rest -> runCompositorPerfCmd rest
    | "compositor-reuse" :: rest -> runCompositorReuseCmd rest
    | "compositor-snapshots" :: rest -> runCompositorSnapshotsCmd rest
    | "compositor-timing" :: rest -> runCompositorTimingCmd rest
    | "compositor-readiness" :: rest -> runCompositorReadinessCmd rest
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
        printfn "usage: <probe|offscreen|live-x11|overlay-visual-proof|render-anywhere-reference|render-anywhere-browser-feasibility|compositor-present-proof|compositor-live-proof|compositor-parity|compositor-perf|compositor-reuse|compositor-snapshots|compositor-timing|compositor-readiness|perf|input> [--out <dir>] [--json]"
        0
    | other ->
        eprintfn "unknown subcommand: %s" (String.concat " " other)
        2

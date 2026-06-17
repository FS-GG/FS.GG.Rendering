namespace Rendering.Harness

open System
open System.IO
open System.Globalization
open SkiaSharp

module Evidence =

    type Evidence =
        { RunId: string
          Tier: Tier
          Subcommand: string
          Status: RunStatus
          SkipReason: string option
          ProofLevel: ProofLevel
          AuthoritativeFor: string list
          NotAuthoritativeFor: string list
          Facts: ProbeFacts
          Frames: int
          P50Ms: float option
          P95Ms: float option
          P99Ms: float option
          Artifacts: string list }

    type OverlayEvidence =
        { ReplayLog: string list
          ProductMessages: string list
          HitOrder: string list
          Diagnostics: string list }

    type HostCapabilityStatus =
        | HostCapable
        | HostUnsupported
        | HostFailed

    type HostCaptureAvailability =
        | CaptureAvailable
        | CaptureUnavailable of reason: string

    type VisualProofStatus =
        | VisualProofPassed
        | VisualProofFailed
        | VisualProofEnvironmentLimited

    type VisualArtifactState =
        | OpenOverlay
        | ClosedOverlay

    type VisualPixelContentValidation =
        | VisualPixelNonBlank
        | VisualPixelBlank
        | VisualPixelUnreadable of reason: string
        | VisualPixelInvalid of reason: string

    type VisualCaptureSource =
        | VisualLiveViewerWindow
        | VisualOffscreenHost
        | VisualSynthetic
        | VisualNoCapture

    type ExpectedOverlayState =
        | ExpectedOpen
        | ExpectedClosed

    type VisualProofFailureCategory =
        | NoFailure
        | Environment
        | Capture
        | OverlayBehavior
        | EvidenceBookkeeping

    type ReadinessDecision =
        | CaveatClosed
        | CaveatEnvironmentGated
        | CaveatFailed

    type OverlayVisualProofScenario =
        { ScenarioId: string
          InputSequence: string list
          OpenStateStep: string
          ClosedStateStep: string
          ExpectedTopmostHitTarget: string
          ExpectedFocusState: string
          ExpectedDispatchSummary: string }

    type HostCapabilityResult =
        { EffectiveBackend: string
          Display: string option
          GlRenderer: string option
          CaptureAvailability: HostCaptureAvailability
          Status: HostCapabilityStatus
          Owner: string
          Cause: string
          NextProofPath: string
          HostFacts: string list }

    type VisualArtifact =
        { ArtifactId: string
          Path: string
          State: VisualArtifactState
          Width: int
          Height: int
          PixelContentValidation: VisualPixelContentValidation
          CaptureSource: VisualCaptureSource
          RunId: string
          ScenarioId: string
          CreatedAt: DateTimeOffset
          OverlayAboveContent: bool option
          TopmostHitTarget: string option
          NoStaleOverlayPixel: bool option }

    type OverlayVisualCorrelation =
        { ScenarioId: string
          InputStep: string
          ExpectedOverlayState: ExpectedOverlayState
          TopmostHitTarget: string option
          FocusState: string
          ProductDispatchSummary: string
          ReplayLogReference: string
          BehavioralEvidenceReference: string
          ArtifactPath: string
          OverlayAboveContent: bool option
          NoStaleOverlayPixel: bool option }

    type UnsupportedHostLimitation =
        { Owner: string
          Cause: string
          HostFacts: string list
          NextProofPath: string
          TrustRationale: string
          NotAuthoritativeFor: string list }

    type ReadinessCaveatDecision =
        { Caveat: string
          Decision: ReadinessDecision
          ArtifactPaths: string list
          LimitationDetails: UnsupportedHostLimitation option
          FailureCategory: VisualProofFailureCategory
          NextWorkstreamGuidance: string
          ReviewedAt: DateTimeOffset }

    type VisualProofRun =
        { RunId: string
          ScenarioId: string
          HostCapability: HostCapabilityResult
          Status: VisualProofStatus
          OpenArtifact: VisualArtifact option
          ClosedArtifact: VisualArtifact option
          Correlations: OverlayVisualCorrelation list
          FailureCategory: VisualProofFailureCategory
          Limitation: UnsupportedHostLimitation option
          ReadinessDecision: ReadinessCaveatDecision option }

    type VisualProofValidationResult =
        { Accepted: bool
          FailureCategory: VisualProofFailureCategory
          Diagnostics: string list }

    let tierToken tier =
        match tier with
        | T0 -> "T0"
        | T1 -> "T1"
        | T2 -> "T2"
        | T3 -> "T3"
        | TUinput -> "T-uinput"

    let proofToken proof =
        match proof with
        | Deterministic -> "deterministic"
        | OffscreenPixels -> "offscreen-pixels"
        | LiveHost -> "live-host"
        | Timing -> "timing"
        | KernelInput -> "kernel-input"

    let statusToken status =
        match status with
        | Passed -> "passed"
        | Failed -> "failed"
        | Skipped -> "skipped"

    let backendToken backend =
        match backend with
        | X11 -> "x11"
        | Wayland -> "wayland"
        | NoDisplay -> "none"

    let hostCapabilityStatusToken status =
        match status with
        | HostCapable -> "capable"
        | HostUnsupported -> "unsupported"
        | HostFailed -> "failed"

    let captureAvailabilityToken availability =
        match availability with
        | CaptureAvailable -> "available"
        | CaptureUnavailable reason -> sprintf "unavailable:%s" reason

    let visualProofStatusToken status =
        match status with
        | VisualProofPassed -> "passed"
        | VisualProofFailed -> "failed"
        | VisualProofEnvironmentLimited -> "environment-limited"

    let artifactStateToken state =
        match state with
        | OpenOverlay -> "open"
        | ClosedOverlay -> "closed"

    let pixelContentToken validation =
        match validation with
        | VisualPixelNonBlank -> "non-blank"
        | VisualPixelBlank -> "blank"
        | VisualPixelUnreadable reason -> sprintf "unreadable:%s" reason
        | VisualPixelInvalid reason -> sprintf "invalid:%s" reason

    let captureSourceToken source =
        match source with
        | VisualLiveViewerWindow -> "live-viewer-window"
        | VisualOffscreenHost -> "offscreen-host"
        | VisualSynthetic -> "synthetic"
        | VisualNoCapture -> "none"

    let expectedOverlayStateToken state =
        match state with
        | ExpectedOpen -> "open"
        | ExpectedClosed -> "closed"

    let failureCategoryToken category =
        match category with
        | NoFailure -> "none"
        | Environment -> "environment"
        | Capture -> "capture"
        | OverlayBehavior -> "overlay-behavior"
        | EvidenceBookkeeping -> "evidence-bookkeeping"

    let readinessDecisionToken decision =
        match decision with
        | CaveatClosed -> "closed"
        | CaveatEnvironmentGated -> "environment-gated"
        | CaveatFailed -> "failed"

    let feature145ReadinessDirectory =
        Path.Combine("specs", "145-overlay-visual-proof", "readiness")

    let feature145ArtifactsDirectory =
        Path.Combine(feature145ReadinessDirectory, "artifacts")

    let feature144DatePickerProofScenario =
        { ScenarioId = "feature144-antshowcase-date-picker-reference"
          InputSequence =
            [ "navigate:text-numeric-input"
              "open:date-picker-calendar"
              "focus:calendar"
              "select:2026-06-17"
              "close:date-picker-calendar"
              "focus:trigger" ]
          OpenStateStep = "open:date-picker-calendar"
          ClosedStateStep = "close:date-picker-calendar"
          ExpectedTopmostHitTarget = "date-picker-calendar"
          ExpectedFocusState = "date-picker-trigger"
          ExpectedDispatchSummary = "DatePickerOpenChanged:true; DatePickerChanged:2026-06-17; DatePickerOpenChanged:false" }

    let safeSegment (value: string) =
        value.ToCharArray()
        |> Array.map (fun ch -> if Char.IsLetterOrDigit ch || ch = '-' || ch = '_' then ch else '-')
        |> fun chars -> String(chars)

    let visualArtifactRelativePath runId state =
        (Path.Combine("artifacts", safeSegment runId, sprintf "%s.png" (artifactStateToken state)))
            .Replace('\\', '/')

    // minimal JSON string escape for the controlled values used here
    let esc (s: string) =
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n")

    let q (s: string) = "\"" + esc s + "\""
    let strList (xs: string list) = "[" + String.Join(", ", xs |> List.map q) + "]"
    let optStr (o: string option) = match o with Some s -> q s | None -> "null"
    let optNum (o: float option) = match o with Some v -> v.ToString("0.###", CultureInfo.InvariantCulture) | None -> "null"
    let optInt (o: int option) = match o with Some v -> string v | None -> "null"
    let boolStr (b: bool) = if b then "true" else "false"

    let percentiles (frameMs: float list) =
        match frameMs with
        | [] -> (None, None, None)
        | _ ->
            let sorted = frameMs |> List.sort |> List.toArray
            let pick p =
                // nearest-rank percentile
                let idx = int (ceil (p * float sorted.Length)) - 1
                let i = max 0 (min (sorted.Length - 1) idx)
                Some sorted.[i]
            (pick 0.50, pick 0.95, pick 0.99)

    let toJson (evidence: Evidence) =
        let e = evidence
        let f = e.Facts
        let sb = Text.StringBuilder()
        let line (s: string) = sb.AppendLine(s) |> ignore
        line "{"
        line (sprintf "  \"runId\": %s," (q e.RunId))
        line (sprintf "  \"tier\": %s," (q (tierToken e.Tier)))
        line (sprintf "  \"subcommand\": %s," (q e.Subcommand))
        line (sprintf "  \"status\": %s," (q (statusToken e.Status)))
        line (sprintf "  \"skipReason\": %s," (optStr e.SkipReason))
        line (sprintf "  \"proofLevel\": %s," (q (proofToken e.ProofLevel)))
        line (sprintf "  \"authoritativeFor\": %s," (strList e.AuthoritativeFor))
        line (sprintf "  \"notAuthoritativeFor\": %s," (strList e.NotAuthoritativeFor))
        line "  \"env\": {"
        line (sprintf "    \"effectiveBackend\": %s," (q (backendToken f.EffectiveBackend)))
        line (sprintf "    \"display\": %s," (optStr f.Display))
        line (sprintf "    \"gl\": { \"renderer\": %s, \"version\": %s, \"direct\": %s }," (optStr f.GlRenderer) (optStr f.GlVersion) (boolStr f.GlDirect))
        line (sprintf "    \"refreshHz\": %s," (optNum f.RefreshHz))
        line (sprintf "    \"extensions\": %s" (strList f.Extensions))
        line "  },"
        line (sprintf "  \"present\": { \"swapControl\": %s, \"vblankSource\": %s }," (optInt f.SwapControl) (optStr f.VblankSource))
        line (sprintf "  \"metrics\": { \"frames\": %d, \"p50Ms\": %s, \"p95Ms\": %s, \"p99Ms\": %s }," e.Frames (optNum e.P50Ms) (optNum e.P95Ms) (optNum e.P99Ms))
        line (sprintf "  \"artifacts\": %s" (strList e.Artifacts))
        line "}"
        sb.ToString()

    let metricsCsv (frameMs: float list) =
        let sb = Text.StringBuilder()
        sb.AppendLine("frame,ms") |> ignore
        frameMs |> List.iteri (fun i ms -> sb.AppendLine(sprintf "%d,%s" i (ms.ToString("0.###", CultureInfo.InvariantCulture))) |> ignore)
        sb.ToString()

    let toSummary (evidence: Evidence) =
        let e = evidence
        let sb = Text.StringBuilder()
        let line (s: string) = sb.AppendLine(s) |> ignore
        line (sprintf "# Harness run %s — tier %s (%s)" e.RunId (tierToken e.Tier) (statusToken e.Status))
        line ""
        line (sprintf "- proof level: **%s**" (proofToken e.ProofLevel))
        line (sprintf "- authoritative for: %s" (String.Join(", ", e.AuthoritativeFor)))
        line (sprintf "- **NOT** authoritative for: %s" (String.Join(", ", e.NotAuthoritativeFor)))
        line (sprintf "- effective backend: %s" (backendToken e.Facts.EffectiveBackend))
        match e.SkipReason with
        | Some r -> line (sprintf "- skipped: %s" r)
        | None -> ()
        sb.ToString()

    let overlaySummary (evidence: OverlayEvidence) =
        sprintf
            "replay=%d product=%d hit=%d diagnostics=%d"
            evidence.ReplayLog.Length
            evidence.ProductMessages.Length
            evidence.HitOrder.Length
            evidence.Diagnostics.Length

    let pathInside (root: string) (candidate: string) =
        try
            let fullRoot =
                Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + string Path.DirectorySeparatorChar
            let fullCandidate = Path.GetFullPath(candidate)
            fullCandidate.StartsWith(fullRoot, StringComparison.Ordinal)
        with _ ->
            false

    let pngDimensionsAndContent (path: string) =
        try
            use bitmap = SKBitmap.Decode path
            if isNull bitmap then
                None, VisualPixelUnreadable "png decode returned no bitmap"
            elif bitmap.Width <= 0 || bitmap.Height <= 0 then
                Some(bitmap.Width, bitmap.Height), VisualPixelInvalid "png dimensions must be positive"
            else
                let first = bitmap.GetPixel(0, 0)
                let stepX = max 1 (bitmap.Width / 24)
                let stepY = max 1 (bitmap.Height / 24)
                let mutable diff = false
                let mutable x = 0
                while x < bitmap.Width && not diff do
                    let mutable y = 0
                    while y < bitmap.Height && not diff do
                        if bitmap.GetPixel(x, y) <> first then
                            diff <- true
                        y <- y + stepY
                    x <- x + stepX
                let content = if diff then VisualPixelNonBlank else VisualPixelBlank
                Some(bitmap.Width, bitmap.Height), content
        with ex ->
            None, VisualPixelUnreadable ex.Message

    let validationResult (diagnostics: string list) (category: VisualProofFailureCategory) =
        { Accepted = List.isEmpty diagnostics
          FailureCategory = if List.isEmpty diagnostics then NoFailure else category
          Diagnostics = diagnostics }

    let validateVisualArtifact (readinessDir: string) (runId: string) (scenario: OverlayVisualProofScenario) (artifact: VisualArtifact) =
        let diagnostics = ResizeArray<string>()
        let mutable category = Capture

        let mark newCategory message =
            if category = Capture || newCategory = EvidenceBookkeeping then
                category <- newCategory
            diagnostics.Add message

        if String.IsNullOrWhiteSpace artifact.ArtifactId then
            mark EvidenceBookkeeping "artifact id is required"
        if artifact.RunId <> runId then
            mark EvidenceBookkeeping "artifact run id does not match the current run"
        if artifact.ScenarioId <> scenario.ScenarioId then
            mark EvidenceBookkeeping "artifact scenario id does not match the selected scenario"
        if artifact.Width <= 0 || artifact.Height <= 0 then
            mark Capture "artifact record dimensions must be positive"

        match artifact.PixelContentValidation with
        | VisualPixelNonBlank -> ()
        | other -> mark Capture (sprintf "artifact record is not non-blank: %s" (pixelContentToken other))

        match artifact.CaptureSource with
        | VisualLiveViewerWindow
        | VisualOffscreenHost -> ()
        | VisualSynthetic -> mark Capture "synthetic artifacts cannot satisfy real visual proof"
        | VisualNoCapture -> mark Capture "artifact has no capture source"

        let normalizedRelative = artifact.Path.Replace('\\', '/')
        if Path.IsPathRooted artifact.Path
           || normalizedRelative.StartsWith("../", StringComparison.Ordinal)
           || normalizedRelative.Contains("/../", StringComparison.Ordinal) then
            mark EvidenceBookkeeping "artifact path must be readiness-relative and cannot escape the readiness tree"
        if not (normalizedRelative.StartsWith("artifacts/", StringComparison.Ordinal)) then
            mark EvidenceBookkeeping "artifact path must live under readiness/artifacts"
        if not (normalizedRelative.Contains("/" + safeSegment runId + "/", StringComparison.Ordinal)) then
            mark EvidenceBookkeeping "artifact path is not scoped to the current run id"

        let fullPath = Path.Combine(readinessDir, artifact.Path)
        let artifactsRoot = Path.Combine(readinessDir, "artifacts")
        if not (pathInside artifactsRoot fullPath) then
            mark EvidenceBookkeeping "artifact path resolves outside readiness/artifacts"
        elif not (File.Exists fullPath) then
            mark Capture "artifact file is missing"
        else
            let dimensions, content = pngDimensionsAndContent fullPath
            match dimensions with
            | Some(width, height) ->
                if width <> artifact.Width || height <> artifact.Height then
                    mark Capture "artifact record dimensions do not match decoded PNG dimensions"
            | None -> ()
            match content with
            | VisualPixelNonBlank -> ()
            | other -> mark Capture (sprintf "artifact PNG is not accepted as non-blank: %s" (pixelContentToken other))

        match artifact.State with
        | OpenOverlay ->
            if artifact.OverlayAboveContent <> Some true then
                mark OverlayBehavior "open artifact must explicitly prove overlay-above-content"
            if artifact.TopmostHitTarget <> Some scenario.ExpectedTopmostHitTarget then
                mark OverlayBehavior "open artifact topmost hit target does not match the selected scenario"
        | ClosedOverlay ->
            if artifact.NoStaleOverlayPixel <> Some true then
                mark OverlayBehavior "closed artifact must explicitly prove no stale overlay pixels"
            if artifact.TopmostHitTarget.IsSome then
                mark OverlayBehavior "closed artifact must not retain a stale overlay hit target"

        validationResult (diagnostics |> Seq.toList) category

    let validateOverlayVisualCorrelation
        (scenario: OverlayVisualProofScenario)
        (artifact: VisualArtifact)
        (correlation: OverlayVisualCorrelation)
        =
        let diagnostics = ResizeArray<string>()
        let mutable category = OverlayBehavior

        let mark newCategory message =
            if newCategory = EvidenceBookkeeping then
                category <- newCategory
            diagnostics.Add message

        if correlation.ScenarioId <> scenario.ScenarioId then
            mark EvidenceBookkeeping "correlation scenario id does not match the selected scenario"
        if correlation.ScenarioId <> artifact.ScenarioId then
            mark EvidenceBookkeeping "correlation scenario id does not match the artifact"
        if correlation.ArtifactPath <> artifact.Path then
            mark EvidenceBookkeeping "correlation artifact path does not match the artifact"
        if String.IsNullOrWhiteSpace correlation.ReplayLogReference then
            mark EvidenceBookkeeping "correlation must include a replay log reference"
        if String.IsNullOrWhiteSpace correlation.BehavioralEvidenceReference then
            mark EvidenceBookkeeping "correlation must include a behavioral evidence reference"
        if String.IsNullOrWhiteSpace correlation.FocusState then
            mark OverlayBehavior "correlation must include focus state"
        if String.IsNullOrWhiteSpace correlation.ProductDispatchSummary then
            mark OverlayBehavior "correlation must include product dispatch summary"

        match artifact.State, correlation.ExpectedOverlayState with
        | OpenOverlay, ExpectedOpen ->
            if correlation.InputStep <> scenario.OpenStateStep then
                mark OverlayBehavior "open correlation input step does not match scenario open step"
            if correlation.TopmostHitTarget <> Some scenario.ExpectedTopmostHitTarget then
                mark OverlayBehavior "open correlation topmost hit target disagrees with expected target"
            if correlation.OverlayAboveContent <> Some true then
                mark OverlayBehavior "open correlation must prove overlay-above-content"
        | ClosedOverlay, ExpectedClosed ->
            if correlation.InputStep <> scenario.ClosedStateStep then
                mark OverlayBehavior "closed correlation input step does not match scenario closed step"
            if correlation.TopmostHitTarget.IsSome then
                mark OverlayBehavior "closed correlation must not retain a topmost overlay hit target"
            if correlation.NoStaleOverlayPixel <> Some true then
                mark OverlayBehavior "closed correlation must prove no stale overlay pixels"
        | _ ->
            mark OverlayBehavior "correlation expected state disagrees with artifact state"

        validationResult (diagnostics |> Seq.toList) category

    let unsupportedHostLimitation (host: HostCapabilityResult) =
        { Owner = host.Owner
          Cause = host.Cause
          HostFacts = host.HostFacts
          NextProofPath = host.NextProofPath
          TrustRationale =
            "Deterministic Feature 144 overlay behavior remains useful, but it is not visual proof. The caveat stays open until a capable host produces accepted current-run artifacts."
          NotAuthoritativeFor =
            [ "Feature144 overlay visual-proof caveat closure"
              "real overlay pixel order"
              "final closed-state pixel cleanup" ] }

    let evaluateReadinessCaveat (run: VisualProofRun) =
        let artifactPaths =
            [ run.OpenArtifact |> Option.map _.Path
              run.ClosedArtifact |> Option.map _.Path ]
            |> List.choose id

        let decision, next =
            match run.Status with
            | VisualProofPassed when run.FailureCategory = NoFailure && artifactPaths.Length >= 2 ->
                CaveatClosed,
                "P5 overlay visual proof is closed for the selected Feature 144 scenario; later workstreams may proceed with the recorded artifacts as readiness evidence."
            | VisualProofEnvironmentLimited ->
                CaveatEnvironmentGated,
                "Keep the Feature 144 visual-proof caveat open and rerun overlay-visual-proof on a display/GL-capable host."
            | _ ->
                CaveatFailed,
                "Do not treat the Feature 144 visual-proof caveat as closed; resolve the classified failure and rerun the proof."

        { Caveat = "Feature 144 overlay visual proof"
          Decision = decision
          ArtifactPaths = artifactPaths
          LimitationDetails = run.Limitation
          FailureCategory = run.FailureCategory
          NextWorkstreamGuidance = next
          ReviewedAt = DateTimeOffset.UtcNow }

    let renderUnsupportedHostLimitation (limitation: UnsupportedHostLimitation) =
        let sb = Text.StringBuilder()
        let line (s: string) = sb.AppendLine(s) |> ignore
        line "# Unsupported Host Limitation"
        line ""
        line (sprintf "- owner: %s" limitation.Owner)
        line (sprintf "- cause: %s" limitation.Cause)
        line (sprintf "- next proof path: %s" limitation.NextProofPath)
        line (sprintf "- trust rationale: %s" limitation.TrustRationale)
        line (sprintf "- not authoritative for: %s" (String.Join(", ", limitation.NotAuthoritativeFor)))
        line "- host facts:"
        limitation.HostFacts |> List.iter (fun fact -> line (sprintf "  - %s" fact))
        sb.ToString()

    let renderCorrelation (run: VisualProofRun) =
        let sb = Text.StringBuilder()
        let line (s: string) = sb.AppendLine(s) |> ignore
        line "# Overlay Visual Correlation"
        line ""
        line (sprintf "- run id: %s" run.RunId)
        line (sprintf "- scenario id: %s" run.ScenarioId)
        if run.Correlations.IsEmpty then
            line "- correlations: none accepted"
        else
            run.Correlations
            |> List.iter (fun correlation ->
                line (sprintf "## %s" correlation.ArtifactPath)
                line (sprintf "- input step: %s" correlation.InputStep)
                line (sprintf "- expected overlay state: %s" (expectedOverlayStateToken correlation.ExpectedOverlayState))
                line (sprintf "- topmost hit target: %s" (correlation.TopmostHitTarget |> Option.defaultValue "none"))
                line (sprintf "- focus state: %s" correlation.FocusState)
                line (sprintf "- product dispatch: %s" correlation.ProductDispatchSummary)
                line (sprintf "- replay log: %s" correlation.ReplayLogReference)
                line (sprintf "- behavioral evidence: %s" correlation.BehavioralEvidenceReference)
                line "")
        sb.ToString()

    let renderReadinessDecision (decision: ReadinessCaveatDecision) =
        let sb = Text.StringBuilder()
        let line (s: string) = sb.AppendLine(s) |> ignore
        line "# Readiness Caveat Decision"
        line ""
        line (sprintf "- caveat: %s" decision.Caveat)
        line (sprintf "- decision: %s" (readinessDecisionToken decision.Decision))
        line (sprintf "- failure category: %s" (failureCategoryToken decision.FailureCategory))
        line (sprintf "- reviewed at: %s" (decision.ReviewedAt.ToString("O", CultureInfo.InvariantCulture)))
        line (sprintf "- next workstream guidance: %s" decision.NextWorkstreamGuidance)
        if decision.ArtifactPaths.IsEmpty then
            line "- artifact paths: none"
        else
            line "- artifact paths:"
            decision.ArtifactPaths |> List.iter (fun path -> line (sprintf "  - %s" path))
        match decision.LimitationDetails with
        | Some limitation ->
            line ""
            line "## Limitation"
            line (sprintf "- owner: %s" limitation.Owner)
            line (sprintf "- cause: %s" limitation.Cause)
            line (sprintf "- next proof path: %s" limitation.NextProofPath)
        | None -> ()
        sb.ToString()

    let renderVisualProofRun (run: VisualProofRun) =
        let decision =
            match run.ReadinessDecision with
            | Some d -> d
            | None -> evaluateReadinessCaveat run

        let sb = Text.StringBuilder()
        let line (s: string) = sb.AppendLine(s) |> ignore
        line "# Overlay Visual Proof"
        line ""
        line (sprintf "- run id: %s" run.RunId)
        line (sprintf "- scenario id: %s" run.ScenarioId)
        line (sprintf "- status: %s" (visualProofStatusToken run.Status))
        line (sprintf "- host capability: %s" (hostCapabilityStatusToken run.HostCapability.Status))
        line (sprintf "- failure category: %s" (failureCategoryToken run.FailureCategory))
        line (sprintf "- readiness decision: %s" (readinessDecisionToken decision.Decision))
        line ""
        line "## Host"
        line (sprintf "- effective backend: %s" run.HostCapability.EffectiveBackend)
        line (sprintf "- display: %s" (run.HostCapability.Display |> Option.defaultValue "none"))
        line (sprintf "- GL renderer: %s" (run.HostCapability.GlRenderer |> Option.defaultValue "none"))
        line (sprintf "- capture availability: %s" (captureAvailabilityToken run.HostCapability.CaptureAvailability))
        line (sprintf "- cause: %s" run.HostCapability.Cause)
        line ""
        line "## Artifacts"
        let artifacts =
            [ run.OpenArtifact
              run.ClosedArtifact ]
            |> List.choose id
        if artifacts.IsEmpty then
            line "- none accepted"
        else
            artifacts
            |> List.iter (fun artifact ->
                line (sprintf "- %s: %s (%dx%d, %s)" (artifactStateToken artifact.State) artifact.Path artifact.Width artifact.Height (pixelContentToken artifact.PixelContentValidation)))
        line ""
        line (renderReadinessDecision decision)
        sb.ToString()

    let write (dir: string) (evidence: Evidence) (frameMs: float list) =
        let e = evidence
        Directory.CreateDirectory(dir) |> ignore
        let runJson = Path.Combine(dir, "run.json")
        File.WriteAllText(runJson, toJson e)
        File.WriteAllText(Path.Combine(dir, "metrics.csv"), metricsCsv frameMs)
        File.WriteAllText(Path.Combine(dir, "summary.md"), toSummary e)
        runJson

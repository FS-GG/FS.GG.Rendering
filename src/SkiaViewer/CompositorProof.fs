namespace FS.GG.UI.SkiaViewer

open System
open FS.GG.UI.Scene

module CompositorProof =

    let proofAlgorithmVersion = "sentinel-damage-v1"

    [<RequireQualifiedAccess>]
    type HostDisplayEnvironment =
        | X11
        | Wayland
        | Headless
        | MissingDisplay
        | Unknown

    type HostProfile =
        { ProfileId: string
          Backend: string
          Renderer: string option
          PresentMode: ViewerPresentMode
          FramebufferSize: Size
          Scale: float option
          DisplayEnvironment: HostDisplayEnvironment
          ProofAlgorithmVersion: string }

    [<RequireQualifiedAccess>]
    type ObservedRegionKind =
        | Untouched
        | Damaged

    type PresentProofObservation =
        { RegionId: string
          Kind: ObservedRegionKind
          ExpectedIdentity: string
          ActualIdentity: string
          Matched: bool }

    [<RequireQualifiedAccess>]
    type PresentProofFailureCause =
        | StalePixels
        | ClearedPixels
        | UnsupportedObservation
        | MissingDisplay
        | Timeout
        | HostError of string
        | HostMismatch
        | SyntheticEvidence

    type PresentProofVerdict =
        | PresentProofPassed
        | PresentProofFailed of PresentProofFailureCause
        | PresentProofEnvironmentLimited of reason: string

    type PresentProof =
        { ProofId: string
          HostProfile: HostProfile
          ScenarioId: string
          Verdict: PresentProofVerdict
          ObservedUntouchedRegions: PresentProofObservation list
          ObservedDamagedRegion: PresentProofObservation option
          EvidenceArtifacts: string list
          CreatedAt: DateTimeOffset
          Diagnostics: string list }

    type ProofArtifactQuality =
        { Present: bool
          Decodable: bool
          NonBlank: bool
          Fresh: bool
          Synthetic: bool }

    type LiveProofAttempt =
        { AttemptId: string
          Proof: PresentProof
          ProofMethod: string
          ArtifactQuality: ProofArtifactQuality }

    type AcceptedProofSet =
        { ProofSetId: string
          HostProfile: HostProfile
          ProofMethod: string
          Attempts: LiveProofAttempt list
          AcceptedAt: DateTimeOffset
          Diagnostics: string list }

    [<RequireQualifiedAccess>]
    type ProofSetReadiness =
        | Accepted of AcceptedProofSet
        | FallbackGated of reason: string
        | Failed of reason: string
        | EnvironmentLimited of reason: string

    [<RequireQualifiedAccess>]
    type ProofReadiness =
        | Ready
        | Missing
        | Stale
        | HostMismatch
        | Failed of reason: string
        | EnvironmentLimited of reason: string

    [<RequireQualifiedAccess>]
    type ProofPhase =
        | NotStarted
        | DetectingProfile
        | PresentingSentinel
        | PresentingDamage
        | Observing
        | Completed

    type Model =
        { ActiveProfile: HostProfile option
          Phase: ProofPhase
          Proof: PresentProof option
          Diagnostics: string list }

    type Msg =
        | ProfileDetected of HostProfile
        | SentinelPresented
        | DamagePresented
        | ObservationCompleted of PresentProofObservation list
        | ProofFailed of PresentProofFailureCause
        | ArtifactWritten of path: string

    type Effect =
        | DetectProfile
        | PresentSentinelFrame of Rect
        | PresentDamageFrame of Rect
        | ObservePixels
        | WriteProofArtifact of path: string * proof: PresentProof

    let sentinelDamageRect =
        { X = 16.0
          Y = 16.0
          Width = 64.0
          Height = 64.0 }

    let private envToken env =
        match env with
        | HostDisplayEnvironment.X11 -> "x11"
        | HostDisplayEnvironment.Wayland -> "wayland"
        | HostDisplayEnvironment.Headless -> "headless"
        | HostDisplayEnvironment.MissingDisplay -> "missing-display"
        | HostDisplayEnvironment.Unknown -> "unknown"

    let private presentModeToken mode =
        match mode with
        | ViewerPresentMode.DirectToSwapchain -> "direct"
        | ViewerPresentMode.OffscreenReadback -> "offscreen-readback"

    let profileId (profile: HostProfile) =
        if not (String.IsNullOrWhiteSpace profile.ProfileId) then
            profile.ProfileId
        else
            [ profile.Backend
              profile.Renderer |> Option.defaultValue "unknown-renderer"
              presentModeToken profile.PresentMode
              $"{profile.FramebufferSize.Width}x{profile.FramebufferSize.Height}"
              profile.Scale |> Option.map string |> Option.defaultValue "unknown-scale"
              envToken profile.DisplayEnvironment
              profile.ProofAlgorithmVersion ]
            |> String.concat "|"
            |> fun value -> value.GetHashCode(StringComparison.Ordinal).ToString("x")
            |> sprintf "host-%s"

    let failureCauseText cause =
        match cause with
        | PresentProofFailureCause.StalePixels -> "stale pixels"
        | PresentProofFailureCause.ClearedPixels -> "cleared pixels"
        | PresentProofFailureCause.UnsupportedObservation -> "unsupported observation"
        | PresentProofFailureCause.MissingDisplay -> "missing display"
        | PresentProofFailureCause.Timeout -> "timeout"
        | PresentProofFailureCause.HostError message -> $"host error: {message}"
        | PresentProofFailureCause.HostMismatch -> "host mismatch"
        | PresentProofFailureCause.SyntheticEvidence -> "synthetic evidence"

    let verdictToken verdict =
        match verdict with
        | PresentProofPassed -> "passed"
        | PresentProofFailed _ -> "failed"
        | PresentProofEnvironmentLimited _ -> "environment-limited"

    let readinessToken readiness =
        match readiness with
        | ProofReadiness.Ready -> "ready"
        | ProofReadiness.Missing -> "missing"
        | ProofReadiness.Stale -> "stale"
        | ProofReadiness.HostMismatch -> "host-mismatch"
        | ProofReadiness.Failed _ -> "failed"
        | ProofReadiness.EnvironmentLimited _ -> "environment-limited"

    let proofSetReadinessToken readiness =
        match readiness with
        | ProofSetReadiness.Accepted _ -> "accepted"
        | ProofSetReadiness.FallbackGated _ -> "fallback-gated"
        | ProofSetReadiness.Failed _ -> "failed"
        | ProofSetReadiness.EnvironmentLimited _ -> "environment-limited"

    let artifactQualityFailure quality =
        if not quality.Present then Some "missing artifact"
        elif not quality.Decodable then Some "undecodable artifact"
        elif not quality.NonBlank then Some "blank artifact"
        elif not quality.Fresh then Some "stale artifact"
        elif quality.Synthetic then Some "synthetic artifact"
        else None

    let artifactQualityAccepted quality =
        artifactQualityFailure quality |> Option.isNone

    let proofMatchesHost (active: HostProfile) (proof: PresentProof) =
        proof.HostProfile.ProfileId = active.ProfileId
        && proof.HostProfile.Backend = active.Backend
        && proof.HostProfile.PresentMode = active.PresentMode
        && proof.HostProfile.FramebufferSize = active.FramebufferSize
        && proof.HostProfile.Scale = active.Scale
        && proof.HostProfile.DisplayEnvironment = active.DisplayEnvironment
        && proof.HostProfile.ProofAlgorithmVersion = active.ProofAlgorithmVersion

    let proofIsFresh (now: DateTimeOffset) (maxAge: TimeSpan) (proof: PresentProof) =
        proof.CreatedAt <= now && now - proof.CreatedAt <= maxAge

    let readiness (active: HostProfile) (now: DateTimeOffset) (maxAge: TimeSpan) (proof: PresentProof option) =
        match proof with
        | None -> ProofReadiness.Missing
        | Some proof when not (proofMatchesHost active proof) -> ProofReadiness.HostMismatch
        | Some proof when not (proofIsFresh now maxAge proof) -> ProofReadiness.Stale
        | Some { Verdict = PresentProofPassed } -> ProofReadiness.Ready
        | Some { Verdict = PresentProofFailed cause } -> ProofReadiness.Failed(failureCauseText cause)
        | Some { Verdict = PresentProofEnvironmentLimited reason } -> ProofReadiness.EnvironmentLimited reason

    let private proofSetId (active: HostProfile) (attempts: LiveProofAttempt list) =
        attempts
        |> List.map _.AttemptId
        |> String.concat "+"
        |> sprintf "proof-set-%s-%s" active.ProfileId

    let private attemptFailure (active: HostProfile) (now: DateTimeOffset) (maxAge: TimeSpan) (attempt: LiveProofAttempt) =
        if not (proofMatchesHost active attempt.Proof) then
            Some(Choice1Of3 "host-mismatched proof")
        elif attempt.ProofMethod <> active.ProofAlgorithmVersion then
            Some(Choice1Of3 "proof-method-mismatched proof")
        elif not (proofIsFresh now maxAge attempt.Proof) then
            Some(Choice1Of3 "stale proof")
        elif List.isEmpty attempt.Proof.EvidenceArtifacts then
            Some(Choice1Of3 "missing artifact")
        else
            match artifactQualityFailure attempt.ArtifactQuality, attempt.Proof.Verdict with
            | Some reason, _ -> Some(Choice1Of3 reason)
            | None, PresentProofPassed -> None
            | None, PresentProofFailed cause -> Some(Choice2Of3(failureCauseText cause))
            | None, PresentProofEnvironmentLimited reason -> Some(Choice3Of3 reason)

    let evaluateProofSet (active: HostProfile) (now: DateTimeOffset) (maxAge: TimeSpan) (attempts: LiveProofAttempt list) =
        match attempts with
        | [] -> ProofSetReadiness.FallbackGated "missing live proof attempts"
        | _ when attempts.Length < 3 -> ProofSetReadiness.FallbackGated "requires three fresh matching capable-host attempts"
        | _ ->
            let failures = attempts |> List.choose (attemptFailure active now maxAge)

            match failures with
            | [] ->
                let proofSet: AcceptedProofSet =
                    { ProofSetId = proofSetId active attempts
                      HostProfile = active
                      ProofMethod = active.ProofAlgorithmVersion
                      Attempts = attempts
                      AcceptedAt = now
                      Diagnostics = [ $"attempt-count={attempts.Length}"; "verdict=accepted" ] }

                ProofSetReadiness.Accepted proofSet
            | Choice3Of3 reason :: _ -> ProofSetReadiness.EnvironmentLimited reason
            | Choice2Of3 reason :: _ -> ProofSetReadiness.Failed reason
            | Choice1Of3 reason :: _ -> ProofSetReadiness.FallbackGated reason

    let classifyObservations observations =
        let untouched =
            observations
            |> List.filter (fun observation -> observation.Kind = ObservedRegionKind.Untouched)

        let damaged =
            observations
            |> List.filter (fun observation -> observation.Kind = ObservedRegionKind.Damaged)

        if List.isEmpty untouched || List.isEmpty damaged then
            PresentProofEnvironmentLimited "missing untouched or damaged observation regions"
        elif untouched |> List.exists (fun observation -> not observation.Matched) then
            PresentProofFailed PresentProofFailureCause.ClearedPixels
        elif damaged |> List.exists (fun observation -> not observation.Matched) then
            PresentProofFailed PresentProofFailureCause.StalePixels
        else
            PresentProofPassed

    let init () =
        { ActiveProfile = None
          Phase = ProofPhase.DetectingProfile
          Proof = None
          Diagnostics = [] },
        [ DetectProfile ]

    let private scenarioId = "proof/sentinel-damage-v1"

    let private proofId (profile: HostProfile) (now: DateTimeOffset) =
        $"proof-{profile.ProfileId}-{now.UtcDateTime:yyyyMMddHHmmss}"

    let private proofFromObservations (now: DateTimeOffset) (profile: HostProfile) (observations: PresentProofObservation list) (diagnostics: string list) : PresentProof =
        let verdict = classifyObservations observations

        { ProofId = proofId profile now
          HostProfile = profile
          ScenarioId = scenarioId
          Verdict = verdict
          ObservedUntouchedRegions = observations |> List.filter (fun observation -> observation.Kind = ObservedRegionKind.Untouched)
          ObservedDamagedRegion = observations |> List.tryFind (fun observation -> observation.Kind = ObservedRegionKind.Damaged)
          EvidenceArtifacts = []
          CreatedAt = now
          Diagnostics = diagnostics @ [ $"verdict={verdictToken verdict}" ] }

    let private failedProof (now: DateTimeOffset) (profile: HostProfile) (cause: PresentProofFailureCause) (diagnostics: string list) : PresentProof =
        { ProofId = proofId profile now
          HostProfile = profile
          ScenarioId = scenarioId
          Verdict = PresentProofFailed cause
          ObservedUntouchedRegions = []
          ObservedDamagedRegion = None
          EvidenceArtifacts = []
          CreatedAt = now
          Diagnostics = diagnostics @ [ $"failure={failureCauseText cause}" ] }

    let update (now: DateTimeOffset) (outputPath: string) (msg: Msg) (model: Model) =
        match msg with
        | ProfileDetected profile ->
            { model with
                ActiveProfile = Some { profile with ProfileId = profileId profile }
                Phase = ProofPhase.PresentingSentinel },
            [ PresentSentinelFrame sentinelDamageRect ]

        | SentinelPresented ->
            { model with Phase = ProofPhase.PresentingDamage },
            [ PresentDamageFrame sentinelDamageRect ]

        | DamagePresented ->
            { model with Phase = ProofPhase.Observing },
            [ ObservePixels ]

        | ObservationCompleted observations ->
            match model.ActiveProfile with
            | None ->
                { model with
                    Phase = ProofPhase.Completed
                    Diagnostics = model.Diagnostics @ [ "observation completed before host profile detection" ] },
                []
            | Some profile ->
                let proof = proofFromObservations now profile observations model.Diagnostics

                { model with
                    Phase = ProofPhase.Completed
                    Proof = Some proof },
                [ WriteProofArtifact(outputPath, proof) ]

        | ProofFailed cause ->
            match model.ActiveProfile with
            | None ->
                { model with
                    Phase = ProofPhase.Completed
                    Diagnostics = model.Diagnostics @ [ failureCauseText cause ] },
                []
            | Some profile ->
                let proof = failedProof now profile cause model.Diagnostics

                { model with
                    Phase = ProofPhase.Completed
                    Proof = Some proof },
                [ WriteProofArtifact(outputPath, proof) ]

        | ArtifactWritten path ->
            { model with Diagnostics = model.Diagnostics @ [ $"artifact-written={path}" ] }, []

    let renderProof (proof: PresentProof) =
        let renderer = proof.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = proof.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"

        let untouched =
            match proof.ObservedUntouchedRegions with
            | [] -> "- none"
            | xs -> xs |> List.map (fun x -> $"- `{x.RegionId}` matched={x.Matched}") |> String.concat "\n"

        let damaged =
            match proof.ObservedDamagedRegion with
            | None -> "- none"
            | Some x -> $"- `{x.RegionId}` matched={x.Matched}"

        let diagnostics =
            match proof.Diagnostics with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Present Path Proof"
              ""
              $"Proof: `{proof.ProofId}`"
              $"Scenario: `{proof.ScenarioId}`"
              $"Verdict: `{verdictToken proof.Verdict}`"
              $"Created: `{proof.CreatedAt:O}`"
              ""
              "## Host Profile"
              ""
              $"- Profile: `{proof.HostProfile.ProfileId}`"
              $"- Backend: `{proof.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{presentModeToken proof.HostProfile.PresentMode}`"
              $"- Framebuffer: `{proof.HostProfile.FramebufferSize.Width}x{proof.HostProfile.FramebufferSize.Height}`"
              $"- Scale: `{scale}`"
              $"- Environment: `{envToken proof.HostProfile.DisplayEnvironment}`"
              $"- Algorithm: `{proof.HostProfile.ProofAlgorithmVersion}`"
              ""
              "## Untouched Regions"
              ""
              untouched
              ""
              "## Damaged Region"
              ""
              damaged
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderProofSet readiness =
        match readiness with
        | ProofSetReadiness.Accepted proofSet ->
            let attempts =
                proofSet.Attempts
                |> List.map (fun attempt -> $"- `{attempt.AttemptId}` / `{attempt.Proof.ProofId}`")
                |> String.concat "\n"

            String.concat
                "\n"
                [ "# Accepted Live Proof Set"
                  ""
                  $"Status: `{proofSetReadinessToken readiness}`"
                  $"Proof set: `{proofSet.ProofSetId}`"
                  $"Host profile: `{proofSet.HostProfile.ProfileId}`"
                  $"Proof method: `{proofSet.ProofMethod}`"
                  $"Accepted at: `{proofSet.AcceptedAt:O}`"
                  ""
                  "## Attempts"
                  ""
                  attempts
                  ""
                  "## Diagnostics"
                  ""
                  proofSet.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"
                  "" ]
        | ProofSetReadiness.FallbackGated reason ->
            $"# Accepted Live Proof Set\n\nStatus: `fallback-gated`\nReason: {reason}\n"
        | ProofSetReadiness.Failed reason ->
            $"# Accepted Live Proof Set\n\nStatus: `failed`\nReason: {reason}\n"
        | ProofSetReadiness.EnvironmentLimited reason ->
            $"# Accepted Live Proof Set\n\nStatus: `environment-limited`\nReason: {reason}\n"

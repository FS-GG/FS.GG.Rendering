namespace FS.GG.UI.SkiaViewer

open System
open FS.GG.UI.Scene

/// Feature 147 present-path proof contracts for proof-gated compositor redraw.
module CompositorProof =

    val proofAlgorithmVersion: string

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

    val sentinelDamageRect: Rect
    val profileId: profile: HostProfile -> string
    val verdictToken: verdict: PresentProofVerdict -> string
    val readinessToken: readiness: ProofReadiness -> string
    val failureCauseText: cause: PresentProofFailureCause -> string
    val proofMatchesHost: active: HostProfile -> proof: PresentProof -> bool
    val proofIsFresh: now: DateTimeOffset -> maxAge: TimeSpan -> proof: PresentProof -> bool
    val readiness: active: HostProfile -> now: DateTimeOffset -> maxAge: TimeSpan -> proof: PresentProof option -> ProofReadiness
    val classifyObservations: observations: PresentProofObservation list -> PresentProofVerdict
    val init: unit -> Model * Effect list
    val update: now: DateTimeOffset -> outputPath: string -> msg: Msg -> model: Model -> Model * Effect list
    val renderProof: proof: PresentProof -> string

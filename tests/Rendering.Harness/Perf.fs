namespace Rendering.Harness

open System
open System.IO
open System.Diagnostics
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

module Perf =

    type PerfMode =
        | Throughput
        | Paced60
        | PacedNative
        | StressResize
        | InputLatency

    type PerfKind =
        | DeterministicKind
        | LiveHostKind
        | TimingKind

    type TimingPath =
        | FullRedraw
        | DamageScoped

    type TimingSample =
        { ScenarioId: string
          Path: TimingPath
          RunId: string
          HostProfileId: string
          DurationMs: float
          ArtifactPath: string }

    type SampleDistribution =
        { Count: int
          P50Ms: float
          P95Ms: float
          P99Ms: float
          MinMs: float
          MaxMs: float
          RawSamplePath: string }

    type TimingVerdict =
        | Positive
        | Noisy
        | NonBeneficial
        | Incomplete
        | Rejected
        | EnvironmentLimited
        | Limited

    type ScenarioTimingDecision =
        { NoiseBandMs: float
          Verdict: TimingVerdict
          ConfidenceDecision: string
          Reasons: string list }

    type MeasurementPolicy =
        | ReadbackFree
        | ReadbackOutsideMeasurement
        | ProbeReadbackIncluded
        | Unverified
        | Missing

    type InclusionStatus =
        | Included
        | Excluded
        | Probe

    type ExclusionReason =
        | TimedOut
        | Canceled
        | PartialEvidence
        | ProbeRunExcluded
        | ProofReadbackInMeasuredInterval
        | MissingMeasurementPolicy
        | MissingMetadata
        | UnverifiableMeasurementPolicy
        | CrossProfileEvidence
        | StaleEvidence
        | MixedPolicy
        | ScenarioDefinitionMismatch
        | PackageVersionMismatch
        | RunIdentityMismatch
        | MissingDisplay
        | IndirectRendering
        | SoftwareRaster
        | UnknownRenderer
        | VirtualizedPresentation
        | AmbiguousGpu
        | RefreshRateUnavailable
        | LoadNonRepresentative
        | HostFactsMissing
        | HostFactsContradictory
        | CrossLaneEvidence
        | NoisyTiming
        | PriorGateBlocked
        | UnsupportedHost
        | EnvironmentLimitedReason
        | ScenarioCoverageMissing
        | SamplePolicyMismatch
        | ArtifactUnreadable
        | ReadbackContaminated
        | FailedProofReadback

    type ClassifiedTimingSample =
        { ScenarioId: string
          ScenarioDefinitionId: string
          Path: TimingPath
          RunId: string
          HostProfileId: string
          PackageVersion: string
          DurationMs: float
          MeasurementPolicy: MeasurementPolicy
          InclusionStatus: InclusionStatus
          ExclusionReason: ExclusionReason option
          ArtifactPath: string }

    let parseMode token =
        match token with
        | "throughput" -> Some Throughput
        | "paced-60" -> Some Paced60
        | "paced-native" -> Some PacedNative
        | "stress-resize" -> Some StressResize
        | "input-latency" -> Some InputLatency
        | _ -> None

    let kindOf mode =
        match mode with
        | Throughput -> TimingKind
        | Paced60 -> TimingKind
        | PacedNative -> TimingKind
        | StressResize -> LiveHostKind
        | InputLatency -> TimingKind

    let timingPathToken path =
        match path with
        | FullRedraw -> "full-redraw"
        | DamageScoped -> "damage-scoped"

    let timingVerdictToken verdict =
        match verdict with
        | Positive -> "positive"
        | Noisy -> "noisy"
        | NonBeneficial -> "non-beneficial"
        | Incomplete -> "incomplete"
        | Rejected -> "rejected"
        | EnvironmentLimited -> "environment-limited"
        | Limited -> "limited"

    let measurementPolicyToken policy =
        match policy with
        | ReadbackFree -> "readback-free"
        | ReadbackOutsideMeasurement -> "readback-outside-measurement"
        | ProbeReadbackIncluded -> "probe-readback-included"
        | Unverified -> "unverified"
        | Missing -> "missing"

    let parseMeasurementPolicy token =
        match token with
        | "readback-free" -> Some ReadbackFree
        | "readback-outside-measurement" -> Some ReadbackOutsideMeasurement
        | "probe-readback-included" -> Some ProbeReadbackIncluded
        | "unverified" -> Some Unverified
        | "missing" -> Some Missing
        | _ -> None

    let inclusionStatusToken status =
        match status with
        | Included -> "included"
        | Excluded -> "excluded"
        | Probe -> "probe"

    let exclusionReasonToken reason =
        match reason with
        | TimedOut -> "timed-out"
        | Canceled -> "canceled"
        | PartialEvidence -> "partial-evidence"
        | ProbeRunExcluded -> "probe-run-excluded"
        | ProofReadbackInMeasuredInterval -> "proof-readback-in-measured-interval"
        | MissingMeasurementPolicy -> "missing-measurement-policy"
        | MissingMetadata -> "missing-metadata"
        | UnverifiableMeasurementPolicy -> "unverifiable-measurement-policy"
        | CrossProfileEvidence -> "cross-profile-evidence"
        | StaleEvidence -> "stale-evidence"
        | MixedPolicy -> "mixed-policy"
        | ScenarioDefinitionMismatch -> "scenario-definition-mismatch"
        | PackageVersionMismatch -> "package-version-mismatch"
        | RunIdentityMismatch -> "run-identity-mismatch"
        | MissingDisplay -> "missing-display"
        | IndirectRendering -> "indirect-rendering"
        | SoftwareRaster -> "software-raster"
        | UnknownRenderer -> "unknown-renderer"
        | VirtualizedPresentation -> "virtualized-presentation"
        | AmbiguousGpu -> "ambiguous-gpu"
        | RefreshRateUnavailable -> "refresh-rate-unavailable"
        | LoadNonRepresentative -> "load-non-representative"
        | HostFactsMissing -> "host-facts-missing"
        | HostFactsContradictory -> "host-facts-contradictory"
        | CrossLaneEvidence -> "cross-lane-evidence"
        | NoisyTiming -> "noisy-timing"
        | PriorGateBlocked -> "prior-gate-blocked"
        | UnsupportedHost -> "unsupported-host"
        | EnvironmentLimitedReason -> "environment-limited"
        | ScenarioCoverageMissing -> "scenario-coverage-missing"
        | SamplePolicyMismatch -> "sample-policy-mismatch"
        | ArtifactUnreadable -> "artifact-unreadable"
        | ReadbackContaminated -> "readback-contaminated"
        | FailedProofReadback -> "failed-proof-readback"

    let classifyMeasurementPolicy policy isExplicitProbe readbackInMeasuredInterval =
        if isExplicitProbe || policy = ProbeReadbackIncluded then
            Probe, Some ProbeRunExcluded
        elif readbackInMeasuredInterval then
            Excluded, Some ProofReadbackInMeasuredInterval
        else
            match policy with
            | ReadbackFree
            | ReadbackOutsideMeasurement -> Included, None
            | Missing -> Excluded, Some MissingMeasurementPolicy
            | Unverified -> Excluded, Some UnverifiableMeasurementPolicy
            | ProbeReadbackIncluded -> Probe, Some ProbeRunExcluded

    let private invalidDuration value =
        Double.IsNaN value || Double.IsInfinity value || value < 0.0

    let classifyTimingSample expectedRunId expectedHostProfileId expectedPackageVersion expectedScenarioDefinitions sample =
        let status, reason =
            if sample.RunId <> expectedRunId then
                Excluded, Some RunIdentityMismatch
            elif sample.HostProfileId <> expectedHostProfileId then
                Excluded, Some CrossProfileEvidence
            elif sample.PackageVersion <> expectedPackageVersion then
                Excluded, Some PackageVersionMismatch
            else
                match expectedScenarioDefinitions |> Map.tryFind sample.ScenarioId with
                | None -> Excluded, Some ScenarioDefinitionMismatch
                | Some expected when expected <> sample.ScenarioDefinitionId -> Excluded, Some ScenarioDefinitionMismatch
                | Some _ when invalidDuration sample.DurationMs -> Excluded, Some UnverifiableMeasurementPolicy
                | Some _ -> classifyMeasurementPolicy sample.MeasurementPolicy false false

        { sample with
            InclusionStatus = status
            ExclusionReason = reason }

    let percentile percentile samples =
        let finite =
            samples
            |> List.filter (fun value -> not (Double.IsNaN value) && not (Double.IsInfinity value) && value >= 0.0)
            |> List.sort

        match finite with
        | [] -> None
        | [ value ] -> Some value
        | values ->
            let clamped = Math.Clamp(percentile, 0.0, 100.0)
            let index =
                Math.Ceiling((clamped / 100.0) * float values.Length)
                |> int
                |> fun value -> Math.Clamp(value - 1, 0, values.Length - 1)

            Some values.[index]

    let summarizeSamples rawSamplePath samples =
        let finite =
            samples
            |> List.filter (fun value -> not (Double.IsNaN value) && not (Double.IsInfinity value) && value >= 0.0)

        if finite.Length <> samples.Length || List.isEmpty finite then
            None
        else
            match percentile 50.0 finite, percentile 95.0 finite, percentile 99.0 finite with
            | Some p50, Some p95, Some p99 ->
                Some
                    { Count = finite.Length
                      P50Ms = p50
                      P95Ms = p95
                      P99Ms = p99
                      MinMs = List.min finite
                      MaxMs = List.max finite
                      RawSamplePath = rawSamplePath }
            | _ -> None

    let noiseBandMs fullRedrawP50Ms =
        max 0.25 (fullRedrawP50Ms * 0.05)

    let evaluateScenario measuredRepetitions fullRedraw damageScoped =
        match fullRedraw, damageScoped with
        | None, _
        | _, None ->
            { NoiseBandMs = 0.0
              Verdict = Incomplete
              ConfidenceDecision = "incomplete"
              Reasons = [ "missing or invalid timing distribution" ] }
        | Some full, Some damage when full.Count < measuredRepetitions || damage.Count < measuredRepetitions || measuredRepetitions < 5 ->
            { NoiseBandMs = noiseBandMs full.P50Ms
              Verdict = Incomplete
              ConfidenceDecision = "incomplete"
              Reasons = [ "fewer than five measured repetitions per path" ] }
        | Some full, Some damage ->
            let noise = noiseBandMs full.P50Ms
            let p50Gain = full.P50Ms - damage.P50Ms
            let p95Gain = full.P95Ms - damage.P95Ms
            let p99Guard = damage.P99Ms <= full.P99Ms + noise

            if p50Gain >= noise && p95Gain >= noise && p99Guard then
                { NoiseBandMs = noise
                  Verdict = Positive
                  ConfidenceDecision = "positive-outside-noise-band"
                  Reasons = [] }
            elif Math.Abs(p50Gain) < noise || Math.Abs(p95Gain) < noise then
                { NoiseBandMs = noise
                  Verdict = Noisy
                  ConfidenceDecision = "inside-noise-band"
                  Reasons = [ "p50 or p95 difference is inside the declared noise band" ] }
            else
                { NoiseBandMs = noise
                  Verdict = NonBeneficial
                  ConfidenceDecision = "non-beneficial"
                  Reasons = [ "damage-scoped path is slower, equivalent, or has an unacceptable p99 tail" ] }

    let scene: SceneNode = Rectangle((20.0, 20.0, 160.0, 120.0), Colors.white)

    // One offscreen render, timed (ms). Uses the proven headless capture path.
    let renderOnce (path: string) : float =
        let request: ScreenshotEvidenceRequest =
            { Command = "perf"
              AppOrSample = "harness"
              OutputPath = path
              Width = 200
              Height = 160
              RendererMode = "viewer-render-target"
              CaptureMode = ViewerRenderTargetPng
              HostFacts = []
              Timeout = TimeSpan.FromSeconds 10.0 }
        let options: ViewerOptions =
            { Title = "harness-perf"
              InitialSize = { Width = 200; Height = 160 }
              PresentMode = ViewerPresentMode.OffscreenReadback
              FrameRateCap = None }
        let sw = Stopwatch.StartNew()
        Viewer.captureScreenshotEvidence request options scene |> ignore
        sw.Stop()
        sw.Elapsed.TotalMilliseconds

    let runPerf (mode: PerfMode) (frames: int) (facts: ProbeFacts) (outDir: string) : Evidence.Evidence * float list =
        let p = RunPlan.plan T3 facts
        let n = max 1 frames
        Directory.CreateDirectory outDir |> ignore
        let framePath = Path.Combine(outDir, "perf-frame.png")
        // Real per-frame OFFSCREEN render timing (not live-present / vsync). Warm up once.
        renderOnce framePath |> ignore
        let frameMs = [ for _ in 1..n -> renderOnce framePath ]
        let p50, p95, p99 = Evidence.percentiles frameMs
        // Honest scope: this is offscreen render throughput. It is NOT live-present timing and NOT
        // vsync-faithful — RunPlan already withholds vsync-faithful (no present facts), and we add
        // the live-present caveats so the artifact cannot be read as faithful frame pacing.
        let evidence: Evidence.Evidence =
            { Evidence.RunId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")
              Evidence.Tier = T3
              Evidence.Subcommand = "perf"
              Evidence.Status = RunStatus.Passed
              Evidence.SkipReason = None
              Evidence.ProofLevel = p.ClaimableProof
              Evidence.AuthoritativeFor = [ "offscreen-render-throughput" ]
              Evidence.NotAuthoritativeFor =
                p.NotAuthoritativeFor @ [ "paint-compose-swap-timing"; "live-present-timing" ]
              Evidence.Facts = facts
              Evidence.Frames = n
              Evidence.P50Ms = p50
              Evidence.P95Ms = p95
              Evidence.P99Ms = p99
              Evidence.Artifacts = [ "metrics.csv"; "summary.md" ] }
        evidence, frameMs

namespace Rendering.Harness.Compositor

open Rendering.Harness

open System
open System.IO
open System.Security.Cryptography
open System.Text
open Rendering.Harness.Compositor.Types
open Rendering.Harness.Compositor.Config

module FeatureState =
    let initReadiness () =
        { Proofs = []
          Parity = Map.empty
          TierVerdicts = Map.empty
          Diagnostics = [] },
        [ WriteValidationSummary validationSummaryPath
          WriteCompatibilityLedger compatibilityLedgerPath ]

    let updateReadiness msg model =
        let model' =
            match msg with
            | ProofLoaded proof -> { model with Proofs = model.Proofs @ [ proof ] }
            | ParityRecorded(scenarioId, verdict) -> { model with Parity = Map.add scenarioId verdict model.Parity }
            | TierEvaluated(tier, verdict) -> { model with TierVerdicts = Map.add tier verdict model.TierVerdicts }
            | DiagnosticRecorded diagnostic -> { model with Diagnostics = model.Diagnostics @ [ diagnostic ] }

        model',
        [ WriteValidationSummary validationSummaryPath
          WriteCompatibilityLedger compatibilityLedgerPath ]

    let initFeature154 () =
        { ProofStatus = "environment-limited"
          ParityStatus = "fallback-gated"
          TimingStatus = "inconclusive"
          PublishedArtifacts = [] },
        [ WriteFeature154Artifact feature154ValidationSummaryPath
          WriteFeature154Artifact feature154CompatibilityLedgerPath
          WriteFeature154Artifact feature154ProofSetPath ]

    let updateFeature154 msg model =
        let model' =
            match msg with
            | ProofEvidenceRecorded status -> { model with ProofStatus = status }
            | ParityEvidenceRecorded status -> { model with ParityStatus = status }
            | TimingEvidenceRecorded status -> { model with TimingStatus = status }
            | ArtifactPublished path -> { model with PublishedArtifacts = model.PublishedArtifacts @ [ path ] }

        model',
        [ WriteFeature154Artifact feature154ValidationSummaryPath
          WriteFeature154Artifact feature154CompatibilityLedgerPath
          WriteFeature154Artifact feature154ProofSetPath ]

    let feature156VerdictToken verdict =
        match verdict with
        | Feature156Positive -> "positive"
        | Feature156Noisy -> "noisy"
        | Feature156NonBeneficial -> "non-beneficial"
        | Feature156Incomplete -> "incomplete"
        | Feature156Rejected -> "rejected"
        | Feature156EnvironmentLimited -> "environment-limited"
        | Feature156Limited -> "limited"

    let feature156OverallVerdict (reports: Feature156ScenarioReport list) =
        let requiredReports =
            feature156RequiredScenarioIds
            |> List.choose (fun scenario ->
                reports |> List.tryFind (fun report -> report.ScenarioId = scenario))

        if requiredReports.Length < feature156RequiredScenarioIds.Length then
            Feature156Incomplete
        elif requiredReports |> List.forall (fun report -> report.Verdict = Feature156Positive) then
            Feature156Positive
        elif requiredReports |> List.exists (fun report -> report.Verdict = Feature156EnvironmentLimited) then
            Feature156EnvironmentLimited
        elif requiredReports |> List.exists (fun report -> report.Verdict = Feature156Limited) then
            Feature156Limited
        elif requiredReports |> List.exists (fun report -> report.Verdict = Feature156Rejected) then
            Feature156Rejected
        elif requiredReports |> List.exists (fun report -> report.Verdict = Feature156Incomplete) then
            Feature156Incomplete
        elif requiredReports |> List.exists (fun report -> report.Verdict = Feature156Noisy) then
            Feature156Noisy
        else
            Feature156NonBeneficial

    let initFeature156 warmupCount measuredRepetitions : Feature156Model * Feature156Effect list =
        let runId = "feature156-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")

        { RunId = runId
          ExpectedProfileId = feature156AcceptedProfileId
          ActiveProfile = None
          PolicyId = None
          WarmupCount = max 0 warmupCount
          MeasuredRepetitions = max 1 measuredRepetitions
          ScenarioReports = []
          PublishedArtifacts = []
          Verdict = Feature156Incomplete
          Diagnostics = [] },
        [ Feature156DetectHostProfile
          Feature156DeclarePolicy feature156PolicyId ]

    let updateFeature156 (msg: Feature156Msg) (model: Feature156Model) : Feature156Model * Feature156Effect list =
        let model' =
            match msg with
            | Feature156HostProfileDetected profile ->
                { model with ActiveProfile = Some profile }
            | Feature156HostProfileRejected reason ->
                { model with
                    Verdict = Feature156Rejected
                    Diagnostics = model.Diagnostics @ [ reason ] }
            | Feature156PolicyDeclared policyId ->
                { model with PolicyId = Some policyId }
            | Feature156ScenarioEvaluated report ->
                let reports =
                    model.ScenarioReports
                    |> List.filter (fun existing -> existing.ScenarioId <> report.ScenarioId)
                    |> fun existing -> existing @ [ report ]

                { model with
                    ScenarioReports = reports
                    Verdict = feature156OverallVerdict reports }
            | Feature156RunEnvironmentLimited reason ->
                { model with
                    Verdict = Feature156EnvironmentLimited
                    Diagnostics = model.Diagnostics @ [ reason ] }
            | Feature156SummaryPublished path ->
                { model with PublishedArtifacts = model.PublishedArtifacts @ [ path ] }
            | Feature156DiagnosticRecorded diagnostic ->
                { model with Diagnostics = model.Diagnostics @ [ diagnostic ] }

        let measurementEffects =
            feature156RequiredScenarioIds
            |> List.collect (fun scenario ->
                [ Feature156PrepareScenario scenario
                  Feature156MeasurePath(scenario, "full-redraw")
                  Feature156MeasurePath(scenario, "damage-scoped") ])

        model',
        [ Feature156WriteArtifact feature156TimingSummaryPath
          Feature156WriteArtifact feature156ValidationSummaryPath ]
        @ measurementEffects

    let feature157StatusToken status =
        match status with
        | Feature157DamageStatus.Accepted -> "accepted"
        | Feature157DamageStatus.FallbackOnly -> "fallback-only"
        | Feature157DamageStatus.Rejected -> "rejected"
        | Feature157DamageStatus.EnvironmentLimited -> "environment-limited"

    let feature157ScenarioFileName (scenarioId: string) =
        scenarioId.Replace("/", "-") + ".md"

    let feature157OverallStatus (summary: Feature157DamageSummary) =
        let acceptedScenarioSet =
            summary.AcceptedAttempts
            |> List.map _.ScenarioId
            |> Set.ofList

        let requiredCovered =
            feature157RequiredScenarioIds
            |> List.forall acceptedScenarioSet.Contains

        if summary.UnsupportedHostReason.IsSome then
            Feature157DamageStatus.EnvironmentLimited
        elif summary.Fallbacks |> List.exists (fun fallback -> fallback.Reason = "parity-mismatch") then
            Feature157DamageStatus.Rejected
        elif summary.AcceptedAttempts.Length >= 3 && requiredCovered then
            Feature157DamageStatus.Accepted
        elif not (List.isEmpty summary.Fallbacks) then
            Feature157DamageStatus.FallbackOnly
        else
            Feature157DamageStatus.Rejected

    let initFeature157 () : Feature157Model * Feature157Effect list =
        { RunId = "feature157-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
          ActiveProfile = None
          Attempts = []
          Fallbacks = []
          PublishedArtifacts = []
          Status = Feature157DamageStatus.EnvironmentLimited
          Diagnostics = [] },
        [ Feature157DetectHostProfile
          Feature157LoadAcceptedProofGate
          for scenario in feature157RequiredScenarioIds do
              Feature157PrepareScenario scenario
              Feature157RenderDamageScopedFrame scenario
              Feature157RenderFullRedrawFrame scenario
              Feature157CompareParity scenario
          Feature157WriteArtifact feature157DamageSummaryPath
          Feature157WriteArtifact feature157ValidationSummaryPath ]

    let feature157StatusFrom (attempts: Feature157DamageAttempt list) (fallbacks: Feature157Fallback list) (diagnostics: string list) =
        if diagnostics |> List.exists (fun item -> item.Contains("environment-limited", StringComparison.OrdinalIgnoreCase)) then
            Feature157DamageStatus.EnvironmentLimited
        elif fallbacks |> List.exists (fun fallback -> fallback.Reason = "parity-mismatch") then
            Feature157DamageStatus.Rejected
        elif attempts.Length >= 3 then
            let covered = attempts |> List.map _.ScenarioId |> Set.ofList
            if feature157RequiredScenarioIds |> List.forall covered.Contains then
                Feature157DamageStatus.Accepted
            else
                Feature157DamageStatus.FallbackOnly
        elif not (List.isEmpty fallbacks) then
            Feature157DamageStatus.FallbackOnly
        else
            Feature157DamageStatus.EnvironmentLimited

    let updateFeature157 (msg: Feature157Msg) (model: Feature157Model) : Feature157Model * Feature157Effect list =
        let model' =
            match msg with
            | Feature157HostProfileDetected profile ->
                { model with ActiveProfile = Some profile }
            | Feature157AttemptRecorded attempt ->
                { model with Attempts = model.Attempts @ [ attempt ] }
            | Feature157FallbackRecorded fallback ->
                { model with Fallbacks = model.Fallbacks @ [ fallback ] }
            | Feature157UnsupportedHostRecorded reason ->
                { model with
                    Status = Feature157DamageStatus.EnvironmentLimited
                    Diagnostics = model.Diagnostics @ [ $"environment-limited: {reason}" ] }
            | Feature157ArtifactPublished path ->
                { model with PublishedArtifacts = model.PublishedArtifacts @ [ path ] }
            | Feature157DiagnosticRecorded diagnostic ->
                { model with Diagnostics = model.Diagnostics @ [ diagnostic ] }

        let status = feature157StatusFrom model'.Attempts model'.Fallbacks model'.Diagnostics

        { model' with Status = status },
        [ Feature157WriteArtifact feature157DamageSummaryPath
          Feature157WriteArtifact feature157ValidationSummaryPath
          Feature157WriteArtifact feature157CompatibilityLedgerPath
          Feature157WriteArtifact feature157PackageValidationPath
          Feature157WriteArtifact feature157RegressionValidationPath ]

    let feature158StatusToken status =
        match status with
        | Feature158ReadinessStatus.Accepted -> "accepted"
        | Feature158ReadinessStatus.FallbackOnly -> "fallback-only"
        | Feature158ReadinessStatus.Rejected -> "rejected"
        | Feature158ReadinessStatus.EnvironmentLimited -> "environment-limited"

    let feature158ScenarioFileName (scenarioId: string) =
        scenarioId.Replace("/", "-") + ".md"

    let feature158StatusFromReports (reports: Feature158ScenarioReport list) (diagnostics: string list) =
        if diagnostics |> List.exists (fun item -> item.Contains("environment-limited", StringComparison.OrdinalIgnoreCase)) then
            Feature158ReadinessStatus.EnvironmentLimited
        else
            let requiredReports =
                feature158RequiredScenarioIds
                |> List.choose (fun scenario -> reports |> List.tryFind (fun report -> report.ScenarioId = scenario))

            let allRequiredCovered = requiredReports.Length = feature158RequiredScenarioIds.Length
            let allAccepted =
                allRequiredCovered
                && requiredReports
                   |> List.forall (fun report ->
                       report.Status = Feature158ReadinessStatus.Accepted
                       && not (List.isEmpty report.IncludedSamples)
                       && report.IncludedSamples
                          |> List.forall (fun sample ->
                              sample.InclusionStatus = Perf.Included
                              && (sample.MeasurementPolicy = Perf.ReadbackFree
                                  || sample.MeasurementPolicy = Perf.ReadbackOutsideMeasurement)))

            if allAccepted then
                Feature158ReadinessStatus.Accepted
            elif reports |> List.exists (fun report -> report.Status = Feature158ReadinessStatus.EnvironmentLimited) then
                Feature158ReadinessStatus.EnvironmentLimited
            elif reports |> List.exists (fun report -> report.Status = Feature158ReadinessStatus.FallbackOnly) then
                Feature158ReadinessStatus.FallbackOnly
            elif reports |> List.exists (fun report -> not (List.isEmpty report.IncludedSamples)) then
                Feature158ReadinessStatus.Rejected
            else
                Feature158ReadinessStatus.FallbackOnly

    let initFeature158 warmupCount measuredRepetitions : Feature158Model * Feature158Effect list =
        { RunId = "feature158-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
          ExpectedProfileId = feature158AcceptedProfileId
          ActiveProfile = None
          PolicyId = None
          WarmupCount = max 0 warmupCount
          MeasuredRepetitions = max 1 measuredRepetitions
          ScenarioReports = []
          ProofProbeEvidence = []
          PublishedArtifacts = []
          Status = Feature158ReadinessStatus.FallbackOnly
          Diagnostics = [] },
        [ Feature158DetectHostProfile
          Feature158DeclarePolicy feature158PolicyId
          for scenario in feature158RequiredScenarioIds do
              Feature158PrepareScenario scenario
              Feature158MeasurePath(scenario, "full-redraw")
              Feature158MeasurePath(scenario, "damage-scoped")
          Feature158WriteArtifact feature158TimingSummaryPath
          Feature158WriteArtifact feature158ValidationSummaryPath ]

    let updateFeature158 (msg: Feature158Msg) (model: Feature158Model) : Feature158Model * Feature158Effect list =
        let model' =
            match msg with
            | Feature158HostProfileDetected profile ->
                { model with ActiveProfile = Some profile }
            | Feature158HostProfileRejected reason ->
                { model with
                    Status = Feature158ReadinessStatus.Rejected
                    Diagnostics = model.Diagnostics @ [ reason ] }
            | Feature158PolicyDeclared policyId ->
                { model with PolicyId = Some policyId }
            | Feature158ScenarioEvaluated report ->
                let reports =
                    model.ScenarioReports
                    |> List.filter (fun existing -> existing.ScenarioId <> report.ScenarioId)
                    |> fun existing -> existing @ [ report ]

                { model with ScenarioReports = reports }
            | Feature158ProbeEvidenceRecorded evidence ->
                { model with ProofProbeEvidence = model.ProofProbeEvidence @ [ evidence ] }
            | Feature158RunEnvironmentLimited reason ->
                { model with
                    Status = Feature158ReadinessStatus.EnvironmentLimited
                    Diagnostics = model.Diagnostics @ [ $"environment-limited: {reason}" ] }
            | Feature158SummaryPublished path ->
                { model with PublishedArtifacts = model.PublishedArtifacts @ [ path ] }
            | Feature158DiagnosticRecorded diagnostic ->
                { model with Diagnostics = model.Diagnostics @ [ diagnostic ] }

        let status = feature158StatusFromReports model'.ScenarioReports model'.Diagnostics

        { model' with Status = status },
        [ Feature158WriteArtifact feature158TimingSummaryPath
          Feature158WriteArtifact feature158TimingSummaryJsonPath
          Feature158WriteArtifact feature158ValidationSummaryPath
          Feature158WriteArtifact feature158CompatibilityLedgerPath
          Feature158WriteArtifact feature158PackageValidationPath
          Feature158WriteArtifact feature158RegressionValidationPath ]

    let feature159StatusToken status =
        match status with
        | Feature159ReadinessStatus.Accepted -> "accepted"
        | Feature159ReadinessStatus.NonBeneficial -> "non-beneficial"
        | Feature159ReadinessStatus.FallbackOnly -> "fallback-only"
        | Feature159ReadinessStatus.Rejected -> "rejected"
        | Feature159ReadinessStatus.EnvironmentLimited -> "environment-limited"

    let feature159ScenarioFileName (scenarioId: string) =
        scenarioId.Replace("/", "-") + ".md"

    let feature159StatusFromAttempts (attempts: Feature159Attempt list) (diagnostics: string list) =
        if diagnostics |> List.exists (fun item -> item.Contains("environment-limited", StringComparison.OrdinalIgnoreCase)) then
            Feature159ReadinessStatus.EnvironmentLimited
        else
            let requiredAttempts =
                feature159RequiredScenarioIds
                |> List.choose (fun scenario -> attempts |> List.tryFind (fun attempt -> attempt.ScenarioId = scenario))

            let allRequiredCovered = requiredAttempts.Length = feature159RequiredScenarioIds.Length
            let acceptedAttempts =
                attempts
                |> List.filter (fun attempt ->
                    attempt.PolicyId = feature159PolicyId
                    && attempt.ParityStatus = "passed"
                    && attempt.CounterNetSavedWork > 0
                    && attempt.AcceptedReuseArtifacts + attempt.AcceptedPromotionArtifacts > 0)

            if allRequiredCovered && acceptedAttempts.Length >= 3 then
                Feature159ReadinessStatus.Accepted
            elif attempts |> List.exists (fun attempt -> attempt.PrimaryReason = Some "parity-mismatch" || attempt.PrimaryReason = Some "missing-policy") then
                Feature159ReadinessStatus.Rejected
            elif allRequiredCovered && attempts |> List.exists (fun attempt -> attempt.PromotionDecision = "non-beneficial") then
                Feature159ReadinessStatus.NonBeneficial
            elif not (List.isEmpty attempts) then
                Feature159ReadinessStatus.FallbackOnly
            else
                Feature159ReadinessStatus.EnvironmentLimited

    let initFeature159 () : Feature159Model * Feature159Effect list =
        { RunId = "feature159-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
          ActiveProfile = None
          PolicyId = None
          Attempts = []
          PublishedArtifacts = []
          Status = Feature159ReadinessStatus.EnvironmentLimited
          Diagnostics = [] },
        [ Feature159DetectHostProfile
          Feature159DeclarePolicy feature159PolicyId
          for scenario in feature159RequiredScenarioIds do
              Feature159PrepareScenario scenario
              Feature159EvaluatePromotion scenario
              Feature159CompareParity scenario
          Feature159WriteArtifact feature159PromotionSummaryPath
          Feature159WriteArtifact feature159ValidationSummaryPath ]

    let updateFeature159 (msg: Feature159Msg) (model: Feature159Model) : Feature159Model * Feature159Effect list =
        let model' =
            match msg with
            | Feature159HostProfileDetected profile ->
                { model with ActiveProfile = Some profile }
            | Feature159PolicyDeclared policyId ->
                { model with PolicyId = Some policyId }
            | Feature159AttemptRecorded attempt ->
                { model with Attempts = model.Attempts @ [ attempt ] }
            | Feature159RunEnvironmentLimited reason ->
                { model with
                    Status = Feature159ReadinessStatus.EnvironmentLimited
                    Diagnostics = model.Diagnostics @ [ $"environment-limited: {reason}" ] }
            | Feature159SummaryPublished path ->
                { model with PublishedArtifacts = model.PublishedArtifacts @ [ path ] }
            | Feature159DiagnosticRecorded diagnostic ->
                { model with Diagnostics = model.Diagnostics @ [ diagnostic ] }

        let status = feature159StatusFromAttempts model'.Attempts model'.Diagnostics

        { model' with Status = status },
        [ Feature159WriteArtifact feature159PromotionSummaryPath
          Feature159WriteArtifact feature159ValidationSummaryPath
          Feature159WriteArtifact feature159CompatibilityLedgerPath
          Feature159WriteArtifact feature159PackageValidationPath
          Feature159WriteArtifact feature159RegressionValidationPath ]

    let feature160StatusToken status =
        match status with
        | Feature160ReadinessStatus.Accepted -> "accepted"
        | Feature160ReadinessStatus.Blocked -> "blocked"
        | Feature160ReadinessStatus.Rejected -> "rejected"
        | Feature160ReadinessStatus.FallbackOnly -> "fallback-only"
        | Feature160ReadinessStatus.EnvironmentLimited -> "environment-limited"

    let feature160ScenarioFileName (scenarioId: string) =
        scenarioId.Replace("/", "-") + ".md"

    let feature160IterationFileName (iterationId: string) =
        iterationId.Replace("/", "-") + ".md"

    let feature160AcceptedSamplePolicy (sample: Feature158TimingSample) =
        sample.InclusionStatus = Perf.Included
        && (sample.MeasurementPolicy = Perf.ReadbackFree
            || sample.MeasurementPolicy = Perf.ReadbackOutsideMeasurement)

    let feature160IterationAccepted (iteration: Feature160Iteration) =
        let coverage = iteration.ScenarioCoverage |> Set.ofList
        iteration.Status = Feature160ReadinessStatus.Accepted
        && iteration.ExclusionReason.IsNone
        && iteration.RestrictedScenario.IsNone
        && iteration.HostProfile.ProfileId = feature160AcceptedProfileId
        && iteration.LaneId = feature160FocusedLaneId
        && iteration.PolicyId = feature160PolicyId
        && iteration.DeclaredBoundMinutes = feature160MaxIterationMinutes
        && iteration.ActualDuration <= TimeSpan.FromMinutes(float feature160MaxIterationMinutes)
        && iteration.WarmupCount = 3
        && iteration.MeasuredRepetitions = 5
        && feature160RequiredScenarioIds |> List.forall coverage.Contains
        && not (List.isEmpty iteration.IncludedSamples)
        && iteration.IncludedSamples |> List.forall feature160AcceptedSamplePolicy
        && iteration.ArtifactPaths |> List.exists (fun path -> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))

    let feature160FullValidationStatus record =
        match record with
        | None -> "missing"
        | Some validation ->
            if validation.Command <> "dotnet test FS.GG.Rendering.slnx --no-restore" then
                "stale"
            elif validation.Status = "passed" || validation.Status = "current-passed" then
                "passed"
            elif validation.Status = "failed" then
                "failed"
            elif validation.Status = "interrupted" then
                "interrupted"
            elif validation.Status = "stale" then
                "stale"
            elif String.IsNullOrWhiteSpace validation.Status then
                "undocumented"
            else
                validation.Status

    let feature160FullValidationAccepts record =
        feature160FullValidationStatus record = "passed"

    let feature160FocusedThroughputStatus (summary: Feature160ThroughputSummary) =
        match summary.UnsupportedHostReason with
        | Some _ -> Feature160ReadinessStatus.EnvironmentLimited
        | None ->
            let accepted = summary.Iterations |> List.filter feature160IterationAccepted
            if accepted.Length >= summary.RequiredAttempts then
                Feature160ReadinessStatus.Accepted
            elif summary.Iterations |> List.exists (fun iteration -> iteration.Status = Feature160ReadinessStatus.Rejected) then
                Feature160ReadinessStatus.Rejected
            elif summary.Iterations |> List.exists (fun iteration -> iteration.Status = Feature160ReadinessStatus.EnvironmentLimited) then
                Feature160ReadinessStatus.EnvironmentLimited
            elif List.isEmpty summary.Iterations then
                Feature160ReadinessStatus.FallbackOnly
            else
                Feature160ReadinessStatus.Rejected

    let feature160OverallStatus (summary: Feature160ThroughputSummary) =
        match feature160FocusedThroughputStatus summary with
        | Feature160ReadinessStatus.Accepted when not (feature160FullValidationAccepts summary.FullValidation) ->
            Feature160ReadinessStatus.Blocked
        | status -> status

    let initFeature160 attempts maxIterationMinutes : Feature160Model * Feature160Effect list =
        let requiredAttempts = max 1 attempts
        let bound = max 1 maxIterationMinutes
        let runId = "feature160-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")

        { RunId = runId
          ExpectedProfileId = feature160AcceptedProfileId
          ActiveProfile = None
          LaneId = None
          PolicyId = None
          DeclaredBoundMinutes = None
          Iterations = []
          FullValidation = None
          PublishedArtifacts = []
          Status = Feature160ReadinessStatus.FallbackOnly
          Diagnostics = [] },
        [ Feature160DetectHostProfile
          Feature160DeclareFocusedLane feature160FocusedLaneId
          Feature160DeclarePolicy feature160PolicyId
          Feature160DeclareIterationBound bound
          for attempt in 1..requiredAttempts do
              let iterationId = sprintf "%s-%03i" runId attempt
              Feature160EnforceIterationTimeout(iterationId, bound)
          for scenario in feature160RequiredScenarioIds do
              Feature160PrepareScenario scenario
              Feature160RunTimingWarmup scenario
              Feature160MeasurePath(scenario, "full-redraw")
              Feature160MeasurePath(scenario, "damage-scoped")
          Feature160WriteArtifact feature160ThroughputSummaryPath
          Feature160WriteArtifact feature160ValidationSummaryPath ]

    let feature160StatusFromModel (model: Feature160Model) =
        let summary =
            { RunId = model.RunId
              HostProfile =
                model.ActiveProfile
                |> Option.defaultValue
                    { ProfileId = feature160AcceptedProfileId
                      Backend = "OpenGL"
                      Renderer = None
                      PresentMode = "DirectToSwapchain"
                      FramebufferSize = "640x480"
                      Scale = Some 1.0
                      DisplayEnvironment = "unknown"
                      ProofAlgorithmVersion = "sentinel-damage-v1" }
              LaneId = model.LaneId |> Option.defaultValue feature160FocusedLaneId
              PolicyId = model.PolicyId |> Option.defaultValue feature160PolicyId
              DeclaredBoundMinutes = model.DeclaredBoundMinutes |> Option.defaultValue feature160MaxIterationMinutes
              RequiredAttempts = feature160RequiredAttempts
              WarmupCount = 3
              MeasuredRepetitions = 5
              Iterations = model.Iterations
              UnsupportedHostReason =
                model.Diagnostics
                |> List.tryFind (fun item -> item.Contains("environment-limited", StringComparison.OrdinalIgnoreCase))
              FullValidation = model.FullValidation
              CompatibilityImpact = "pending"
              PackageValidationStatus = "pending"
              RegressionValidationStatus = "pending"
              Status = model.Status
              ReleaseReadyStatus = "pending"
              PerformanceClaim = "performance-not-accepted"
              Diagnostics = model.Diagnostics }

        feature160OverallStatus summary

    let updateFeature160 (msg: Feature160Msg) (model: Feature160Model) : Feature160Model * Feature160Effect list =
        let model' =
            match msg with
            | Feature160HostProfileDetected profile ->
                { model with ActiveProfile = Some profile }
            | Feature160HostProfileRejected reason ->
                { model with
                    Status = Feature160ReadinessStatus.Rejected
                    Diagnostics = model.Diagnostics @ [ reason ] }
            | Feature160LaneDeclared laneId ->
                { model with LaneId = Some laneId }
            | Feature160PolicyDeclared policyId ->
                { model with PolicyId = Some policyId }
            | Feature160BoundDeclared minutes ->
                { model with DeclaredBoundMinutes = Some(max 1 minutes) }
            | Feature160IterationStarted iterationId ->
                { model with Diagnostics = model.Diagnostics @ [ $"iteration-started={iterationId}" ] }
            | Feature160IterationCompleted iteration ->
                { model with Iterations = model.Iterations @ [ iteration ] }
            | Feature160IterationExcluded iteration ->
                { model with Iterations = model.Iterations @ [ iteration ] }
            | Feature160IterationTimedOut(iterationId, reason) ->
                { model with Diagnostics = model.Diagnostics @ [ $"timed-out:{iterationId}:{reason}" ] }
            | Feature160IterationCanceled(iterationId, reason) ->
                { model with Diagnostics = model.Diagnostics @ [ $"canceled:{iterationId}:{reason}" ] }
            | Feature160FullValidationRecorded record ->
                { model with FullValidation = Some record }
            | Feature160ArtifactPublished path ->
                { model with PublishedArtifacts = model.PublishedArtifacts @ [ path ] }
            | Feature160DiagnosticRecorded diagnostic ->
                { model with Diagnostics = model.Diagnostics @ [ diagnostic ] }

        let status = feature160StatusFromModel model'

        { model' with Status = status },
        [ Feature160WriteArtifact feature160ThroughputSummaryPath
          Feature160WriteArtifact feature160ThroughputSummaryJsonPath
          Feature160WriteArtifact feature160ValidationSummaryPath
          Feature160WriteArtifact feature160CompatibilityLedgerPath
          Feature160WriteArtifact feature160PackageValidationPath
          Feature160WriteArtifact feature160RegressionValidationPath
          Feature160WriteFullValidationRecord(Path.Combine(feature160FullValidationDirectory, "validation.md")) ]

    let feature161StatusToken status =
        match status with
        | Feature161ReadinessStatus.Accepted -> "accepted"
        | Feature161ReadinessStatus.Blocked -> "blocked"
        | Feature161ReadinessStatus.Rejected -> "rejected"
        | Feature161ReadinessStatus.FallbackOnly -> "fallback-only"
        | Feature161ReadinessStatus.EnvironmentLimited -> "environment-limited"

    let feature161HostFactsFileName (entryId: string) =
        "facts-" + entryId.Replace("/", "-") + ".md"

    let feature161LedgerEntryFileName (entryId: string) =
        "entry-" + entryId.Replace("/", "-") + ".md"

    let feature161LaneIdFromFacts (facts: Feature161HostFacts) =
        let display = facts.DisplayIdentity.Trim().ToLowerInvariant().Replace(":", "")
        let renderer = facts.RendererIdentity.Trim().ToLowerInvariant()
        let rendererToken =
            if renderer.Contains("amd", StringComparison.OrdinalIgnoreCase)
               || renderer.Contains("radeon", StringComparison.OrdinalIgnoreCase) then
                "amd-mesa"
            elif String.IsNullOrWhiteSpace renderer then
                "unknown-renderer"
            else
                renderer.Replace(" ", "-").Replace("/", "-")

        let direct =
            match facts.DirectRendering with
            | Some true -> "direct-opengl"
            | Some false -> "indirect-opengl"
            | None -> "unknown-opengl"

        let displayToken = if String.IsNullOrWhiteSpace display then "missing-display" else ":" + display
        $"{facts.DisplayServer.Trim().ToLowerInvariant()}-{displayToken}-{direct}-{rendererToken}"

    let feature161ValidateHostFacts (facts: Feature161HostFacts) =
        let blank value = String.IsNullOrWhiteSpace value
        let renderer = facts.RendererIdentity.Trim()
        let displayServer = facts.DisplayServer.Trim()
        let displayIdentity = facts.DisplayIdentity.Trim()

        if blank displayServer || blank displayIdentity || displayServer = "missing-display" then
            Some Perf.MissingDisplay
        elif blank renderer then
            Some Perf.UnknownRenderer
        elif renderer.Contains("llvmpipe", StringComparison.OrdinalIgnoreCase)
             || renderer.Contains("software", StringComparison.OrdinalIgnoreCase)
             || renderer.Contains("swiftshader", StringComparison.OrdinalIgnoreCase) then
            Some Perf.SoftwareRaster
        elif facts.DirectRendering = Some false then
            Some Perf.IndirectRendering
        elif facts.DirectRendering.IsNone then
            Some Perf.HostFactsMissing
        elif facts.RefreshRateHz.IsNone && Option.isNone facts.RefreshUnavailableReason then
            Some Perf.RefreshRateUnavailable
        elif blank facts.DriverIdentity
             || blank facts.PackageVersionSet
             || blank facts.CpuLoadNote
             || blank facts.GpuLoadNote
             || blank facts.RunIdentity
             || blank facts.ScenarioIdentity
             || blank facts.TimingPolicyIdentity
             || List.isEmpty facts.ArtifactLocations then
            Some Perf.HostFactsMissing
        elif facts.TimingPolicyIdentity <> feature161PolicyId then
            Some Perf.HostFactsContradictory
        elif facts.PackageVersionSet.Contains("stale", StringComparison.OrdinalIgnoreCase) then
            Some Perf.PackageVersionMismatch
        elif facts.CpuLoadNote.Contains("non-representative", StringComparison.OrdinalIgnoreCase)
             || facts.GpuLoadNote.Contains("non-representative", StringComparison.OrdinalIgnoreCase) then
            Some Perf.LoadNonRepresentative
        elif facts.EnvironmentLimits |> List.exists (fun item -> item.Contains("virtual", StringComparison.OrdinalIgnoreCase)) then
            Some Perf.VirtualizedPresentation
        else
            None

    let feature161LedgerEntryAccepted (entry: Feature161LedgerEntry) =
        entry.Status = Feature161ReadinessStatus.Accepted
        && entry.PrimaryExclusionReason.IsNone
        && feature161ValidateHostFacts entry.HostFacts |> Option.isNone
        && entry.LaneId = feature161HostLaneId
        && entry.HostFacts.HostProfile.ProfileId = feature161AcceptedProfileId
        && entry.HostFacts.TimingPolicyIdentity = feature161PolicyId
        && entry.AcceptedLaneScopedPerformanceArtifacts > 0
        && entry.PriorGates |> List.forall (fun gate -> gate.Status = "confirmed" || gate.Status = "accepted")

    let feature161ScopeFromEntries (entries: Feature161LedgerEntry list) =
        let accepted = entries |> List.filter feature161LedgerEntryAccepted
        let blockers =
            [ if List.isEmpty accepted then
                  "no accepted lane-scoped performance artifacts"
              if entries |> List.exists (fun entry -> entry.PrimaryExclusionReason = Some Perf.NoisyTiming) then
                  "same-profile timing remains noisy"
              if entries |> List.exists (fun entry -> entry.PriorGates |> List.exists (fun gate -> gate.Status <> "confirmed" && gate.Status <> "accepted")) then
                  "prior P7 gate blocked"
              if entries |> List.exists (fun entry -> feature161ValidateHostFacts entry.HostFacts |> Option.isSome) then
                  "host facts incomplete or unsupported" ]

        match accepted with
        | entry :: _ ->
            { AcceptedLaneId = Some entry.LaneId
              AppliesTo = "X11 `:1` with direct OpenGL on AMD Radeon/Mesa for profile `probe-08a47c01`"
              NonGeneralizedLanes = feature161NonGeneralizedLanes
              RemainingBlockers = blockers
              PerformanceClaim = "performance-not-accepted" }
        | [] ->
            { AcceptedLaneId = None
              AppliesTo = "no lane accepted"
              NonGeneralizedLanes = feature161NonGeneralizedLanes
              RemainingBlockers = blockers
              PerformanceClaim = "performance-not-accepted" }

    let feature161OverallStatus (summary: Feature161Summary) =
        if summary.UnsupportedHostReason.IsSome then
            Feature161ReadinessStatus.EnvironmentLimited
        elif summary.Entries |> List.exists (fun entry -> entry.Status = Feature161ReadinessStatus.Accepted)
             && summary.ClaimScope.AcceptedLaneId.IsSome
             && summary.FullValidationStatus = "passed" then
            Feature161ReadinessStatus.Accepted
        elif summary.Entries |> List.exists (fun entry -> entry.Status = Feature161ReadinessStatus.Accepted)
             && summary.ClaimScope.AcceptedLaneId.IsSome then
            Feature161ReadinessStatus.Blocked
        elif summary.Entries |> List.exists (fun entry -> entry.Status = Feature161ReadinessStatus.Rejected) then
            Feature161ReadinessStatus.Rejected
        elif List.isEmpty summary.Entries then
            Feature161ReadinessStatus.FallbackOnly
        else
            Feature161ReadinessStatus.EnvironmentLimited

    let initFeature161 sourceThroughput : Feature161Model * Feature161Effect list =
        let runId = "feature161-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
        let source = sourceThroughput |> Option.defaultValue feature160ThroughputDirectory

        { RunId = runId
          ExpectedProfileId = feature161AcceptedProfileId
          ActiveProfile = None
          PolicyId = None
          HostFacts = None
          Entries = []
          PriorGates = []
          PublishedArtifacts = []
          Status = Feature161ReadinessStatus.FallbackOnly
          Diagnostics = [] },
        [ Feature161DetectHostProfile
          Feature161DeclarePolicy feature161PolicyId
          Feature161CollectHostFacts
          Feature161LoadThroughputPackage source
          Feature161WriteArtifact feature161LaneLedgerSummaryPath
          Feature161WriteArtifact feature161ValidationSummaryPath ]

    let feature161StatusFromModel (model: Feature161Model) =
        if model.Diagnostics |> List.exists (fun item -> item.Contains("environment-limited", StringComparison.OrdinalIgnoreCase)) then
            Feature161ReadinessStatus.EnvironmentLimited
        elif model.Entries |> List.exists feature161LedgerEntryAccepted then
            Feature161ReadinessStatus.Blocked
        elif model.Entries |> List.exists (fun entry -> entry.Status = Feature161ReadinessStatus.Rejected) then
            Feature161ReadinessStatus.Rejected
        elif model.HostFacts.IsSome then
            Feature161ReadinessStatus.FallbackOnly
        else
            Feature161ReadinessStatus.FallbackOnly

    let updateFeature161 (msg: Feature161Msg) (model: Feature161Model) : Feature161Model * Feature161Effect list =
        let model' =
            match msg with
            | Feature161HostProfileDetected profile ->
                { model with ActiveProfile = Some profile }
            | Feature161HostProfileRejected reason ->
                { model with
                    Status = Feature161ReadinessStatus.Rejected
                    Diagnostics = model.Diagnostics @ [ reason ] }
            | Feature161PolicyDeclared policyId ->
                { model with PolicyId = Some policyId }
            | Feature161HostFactsCollected facts ->
                { model with HostFacts = Some facts }
            | Feature161HostFactsRejected(reason, diagnostic) ->
                { model with
                    Status = Feature161ReadinessStatus.Rejected
                    Diagnostics = model.Diagnostics @ [ $"{Perf.exclusionReasonToken reason}: {diagnostic}" ] }
            | Feature161PriorGateLinked gate ->
                { model with PriorGates = model.PriorGates @ [ gate ] }
            | Feature161LedgerEntryRecorded entry ->
                { model with Entries = model.Entries @ [ entry ] }
            | Feature161ArtifactPublished path ->
                { model with PublishedArtifacts = model.PublishedArtifacts @ [ path ] }
            | Feature161DiagnosticRecorded diagnostic ->
                { model with Diagnostics = model.Diagnostics @ [ diagnostic ] }

        let status = feature161StatusFromModel model'

        { model' with Status = status },
        [ Feature161WriteArtifact feature161LaneLedgerSummaryPath
          Feature161WriteArtifact feature161LaneLedgerSummaryJsonPath
          Feature161WriteArtifact feature161ValidationSummaryPath
          Feature161WriteArtifact feature161CompatibilityLedgerPath
          Feature161WriteArtifact feature161PackageValidationPath
          Feature161WriteArtifact feature161RegressionValidationPath
          Feature161WriteHostFactsArtifact(Path.Combine(feature161LaneLedgerHostFactsDirectory, "facts-current.md"))
          Feature161WriteLedgerEntryArtifact(Path.Combine(feature161LaneLedgerEntriesDirectory, "entry-current.md"))
          Feature161WriteExcludedEvidenceArtifact(Path.Combine(feature161LaneLedgerExcludedDirectory, "README.md"))
          Feature161WriteUnsupportedHostArtifact(Path.Combine(feature161LaneLedgerUnsupportedDirectory, "README.md")) ]

    let artifactPath directory name = Path.Combine(directory, name)
    let feature148ArtifactPath directory name = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d148, directory, name)
    let feature149ArtifactPath directory name = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d149, directory, name)
    let feature152ArtifactPath directory name = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d152, directory, name)
    let feature153ArtifactPath directory name = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d153, directory, name)
    let feature154ArtifactPath directory name = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d154, directory, name)
    let feature155ArtifactPath directory name = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d155, directory, name)
    let feature156ArtifactPath directory name = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d156, directory, name)
    let feature157ArtifactPath directory name = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d157, directory, name)
    let feature158ArtifactPath directory name = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d158, directory, name)
    let feature159ArtifactPath directory name = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d159, directory, name)
    let feature160ArtifactPath directory name = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d160, directory, name)
    let feature161ArtifactPath directory name = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d161, directory, name)

    let feature156ScenarioFileName (scenarioId: string) =
        scenarioId.Replace("/", "-") + ".md"

    let feature158OverallStatus (summary: Feature158TimingSummary) =
        match summary.UnsupportedHostReason with
        | Some _ -> Feature158ReadinessStatus.EnvironmentLimited
        | None ->
            feature158StatusFromReports
                summary.ScenarioReports
                (summary.Diagnostics
                 @ [ if summary.Status = Feature158ReadinessStatus.EnvironmentLimited then "environment-limited" ])

    let feature159OverallStatus (summary: Feature159Summary) =
        match summary.UnsupportedHostReason with
        | Some _ -> Feature159ReadinessStatus.EnvironmentLimited
        | None -> feature159StatusFromAttempts summary.Attempts summary.Diagnostics

    let feature156FormatMs (value: float) =
        value.ToString("0.###", Globalization.CultureInfo.InvariantCulture)

    let feature156DistributionRow (distribution: Feature156PathDistribution option) =
        match distribution with
        | None -> "`missing` | `missing` | `missing` | `0`"
        | Some distribution ->
            $"`{feature156FormatMs distribution.P50Ms}` | `{feature156FormatMs distribution.P95Ms}` | `{feature156FormatMs distribution.P99Ms}` | `{distribution.SampleCount}`"

    let feature158DistributionRow (distribution: Feature158PathDistribution option) =
        match distribution with
        | None -> "`missing` | `missing` | `missing` | `0`"
        | Some distribution ->
            $"`{feature156FormatMs distribution.P50Ms}` | `{feature156FormatMs distribution.P95Ms}` | `{feature156FormatMs distribution.P99Ms}` | `{distribution.SampleCount}`"


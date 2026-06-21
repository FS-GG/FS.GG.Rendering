namespace FS.GG.UI.Testing

open System
open System.IO
open System.Security.Cryptography
open System.Text
open FS.GG.UI.Scene
open SkiaSharp
// Testing.fs was split into per-domain files; re-open the package namespace AFTER the third-party
// opens so the Testing types win unqualified-name resolution exactly as in the original single file.
open FS.GG.UI.Testing

/// Feature 180 / US2: one parameterized readiness validator. The three Feature15x readiness modules share
/// an identical diagnostics PREFIX (feature-name, missing-required-scenarios) and a byte-identical SUFFIX
/// (compatibility / package / regression ledgers, the shipped-P7 performance claim, limitation keyword).
/// Only the middle diagnostics block, the status derivation, and the result record differ per feature, so
/// each feature becomes one ReadinessValidatorConfig entry (C-VC-2, SC-002, SC-006). 'Inter is the
/// per-feature precomputed-intermediates record (coverage / violation facts) shared by the feature lambdas.
type ReadinessValidatorConfig<'Check, 'Inter, 'Status, 'Result> =
    { ComputeIntermediates: 'Check -> 'Inter
      Feature: 'Check -> string
      FeatureNameMissingMessage: string
      MissingScenarios: 'Inter -> string list
      MissingScenarioMessage: string -> string
      MiddleDiagnostics: 'Check -> 'Inter -> string list
      CompatibilityAccepted: 'Check -> bool
      PackageAccepted: 'Check -> bool
      RegressionAccepted: 'Check -> bool
      PerformanceClaim: 'Check -> string
      PerformanceClaimMessage: string
      Limitations: 'Check -> string list
      LimitationKeyword: string
      LimitationMessage: string -> string
      DeriveStatus: 'Check -> 'Inter -> string list -> 'Status
      BuildResult: 'Inter -> 'Status -> string list -> 'Result }

module ReadinessValidator =
    let validateReadiness
        (config: ReadinessValidatorConfig<'Check, 'Inter, 'Status, 'Result>)
        (check: 'Check)
        : 'Result =
        let inter = config.ComputeIntermediates check

        let diagnostics =
            [ if String.IsNullOrWhiteSpace(config.Feature check) then
                  config.FeatureNameMissingMessage
              for scenario in config.MissingScenarios inter do
                  config.MissingScenarioMessage scenario
              yield! config.MiddleDiagnostics check inter
              if not (config.CompatibilityAccepted check) then
                  "compatibility ledger is not accepted"
              if not (config.PackageAccepted check) then
                  "package validation is not accepted"
              if not (config.RegressionAccepted check) then
                  "regression validation is not accepted"
              if config.PerformanceClaim check <> "performance-not-accepted" then
                  config.PerformanceClaimMessage
              for limitation in config.Limitations check do
                  if limitation.Contains(config.LimitationKeyword, StringComparison.OrdinalIgnoreCase) then
                      config.LimitationMessage limitation ]

        let status = config.DeriveStatus check inter diagnostics
        config.BuildResult inter status diagnostics

module Feature159Readiness =
    let statusText status =
        match status with
        | Feature159Accepted -> "accepted"
        | Feature159NonBeneficial -> "non-beneficial"
        | Feature159FallbackOnly -> "fallback-only"
        | Feature159Rejected -> "rejected"
        | Feature159EnvironmentLimited -> "environment-limited"

    let private statusBlocksAcceptance status =
        match status with
        | Feature159Accepted -> false
        | _ -> true

    type Intermediates =
        { MissingScenarios: string list
          RequiredScenarioResults: Feature159ScenarioEvidence list
          MissingArtifacts: string list
          ScenarioFailures: string list
          FallbackOnly: bool
          EnvironmentLimited: bool
          NonBeneficial: bool
          UnsupportedArtifactViolation: bool }

    let private feature159Config
        : ReadinessValidatorConfig<Feature159ReadinessCheck, Intermediates, Feature159ReadinessStatus, Feature159ReadinessValidationResult> =
        { ComputeIntermediates =
            fun check ->
                let requiredScenarioResults =
                    check.RequiredScenarioIds
                    |> List.choose (fun scenario ->
                        check.Scenarios |> List.tryFind (fun candidate -> candidate.ScenarioId = scenario))

                { MissingScenarios =
                    check.RequiredScenarioIds
                    |> List.filter (fun scenario ->
                        check.Scenarios |> List.exists (fun candidate -> candidate.ScenarioId = scenario) |> not)
                  RequiredScenarioResults = requiredScenarioResults
                  MissingArtifacts =
                    requiredScenarioResults
                    |> List.choose (fun scenario ->
                        if List.isEmpty scenario.ArtifactPaths then Some scenario.ScenarioId else None)
                  ScenarioFailures =
                    requiredScenarioResults
                    |> List.choose (fun scenario ->
                        if statusBlocksAcceptance scenario.Status then
                            Some $"{scenario.ScenarioId}:{statusText scenario.Status}"
                        else
                            None)
                  FallbackOnly =
                    requiredScenarioResults.Length = check.RequiredScenarioIds.Length
                    && requiredScenarioResults |> List.forall (fun scenario -> scenario.Status = Feature159FallbackOnly)
                  EnvironmentLimited =
                    requiredScenarioResults |> List.exists (fun scenario -> scenario.Status = Feature159EnvironmentLimited)
                  NonBeneficial =
                    requiredScenarioResults.Length = check.RequiredScenarioIds.Length
                    && requiredScenarioResults |> List.exists (fun scenario -> scenario.Status = Feature159NonBeneficial)
                    && requiredScenarioResults |> List.forall (fun scenario -> scenario.Status = Feature159Accepted || scenario.Status = Feature159NonBeneficial)
                  UnsupportedArtifactViolation =
                    check.UnsupportedHostStatus = Feature159EnvironmentLimited
                    && (check.AcceptedReuseArtifacts <> 0 || check.AcceptedPromotionArtifacts <> 0) }
          Feature = fun check -> check.Feature
          FeatureNameMissingMessage = "Feature 159 readiness check must name the feature"
          MissingScenarios = fun inter -> inter.MissingScenarios
          MissingScenarioMessage = fun scenario -> $"missing required promotion scenario: {scenario}"
          MiddleDiagnostics =
            fun check inter ->
                [ for scenario in inter.MissingArtifacts do
                      $"missing promotion artifact path: {scenario}"
                  for failure in inter.ScenarioFailures do
                      $"non-accepted promotion scenario: {failure}"
                  if check.AcceptedAttemptCount < 3 && not inter.FallbackOnly && not inter.EnvironmentLimited then
                      "accepted Feature 159 readiness requires at least three accepted attempts"
                  if inter.RequiredScenarioResults |> List.exists (fun scenario -> scenario.Status = Feature159Accepted && not scenario.ParityPassed) then
                      "accepted Feature 159 scenarios require passing parity"
                  if check.Scenarios |> List.exists (fun scenario -> scenario.Status = Feature159Accepted && scenario.CounterNetSavedWork <= 0) then
                      "accepted Feature 159 scenarios require positive net saved work"
                  if inter.UnsupportedArtifactViolation then
                      "unsupported-host evidence must contain zero accepted Feature 159 reuse or promotion artifacts" ]
          CompatibilityAccepted = fun check -> check.CompatibilityAccepted
          PackageAccepted = fun check -> check.PackageAccepted
          RegressionAccepted = fun check -> check.RegressionAccepted
          PerformanceClaim = fun check -> check.PerformanceClaim
          PerformanceClaimMessage = "Feature 159 cannot accept the shipped P7 performance claim by itself"
          Limitations = fun check -> check.Limitations
          LimitationKeyword = "blocking"
          LimitationMessage = fun limitation -> $"blocking Feature 159 readiness limitation: {limitation}"
          DeriveStatus =
            fun check inter diagnostics ->
                // Per-domain override (C-VC-3): Feature 159 treats EnvironmentLimited as blocking.
                if inter.EnvironmentLimited || inter.UnsupportedArtifactViolation then
                    Feature159EnvironmentLimited
                elif not inter.MissingScenarios.IsEmpty || not inter.MissingArtifacts.IsEmpty then
                    Feature159Rejected
                elif inter.RequiredScenarioResults |> List.exists (fun scenario -> scenario.Status = Feature159Rejected) then
                    Feature159Rejected
                elif inter.FallbackOnly then
                    Feature159FallbackOnly
                elif inter.NonBeneficial then
                    Feature159NonBeneficial
                elif diagnostics.IsEmpty
                     && check.AcceptedAttemptCount >= 3
                     && inter.RequiredScenarioResults |> List.forall (fun scenario -> scenario.Status = Feature159Accepted) then
                    Feature159Accepted
                else
                    Feature159FallbackOnly
          BuildResult =
            fun inter status diagnostics ->
                { Accepted = status = Feature159Accepted && diagnostics.IsEmpty
                  Status = status
                  MissingScenarios = inter.MissingScenarios
                  Diagnostics = diagnostics } }

    let validate (check: Feature159ReadinessCheck) : Feature159ReadinessValidationResult =
        ReadinessValidator.validateReadiness feature159Config check

module Feature160ThroughputReadiness =
    let statusText status =
        match status with
        | Feature160Accepted -> "accepted"
        | Feature160Blocked -> "blocked"
        | Feature160Rejected -> "rejected"
        | Feature160FallbackOnly -> "fallback-only"
        | Feature160EnvironmentLimited -> "environment-limited"

    let private fullValidationPassed (status: string) =
        String.Equals(status, "passed", StringComparison.OrdinalIgnoreCase)
        || String.Equals(status, "current-passed", StringComparison.OrdinalIgnoreCase)

    let private samplePolicyAccepted (policy: string) =
        String.Equals(policy, "readback-free", StringComparison.OrdinalIgnoreCase)
        || String.Equals(policy, "readback-outside-measurement", StringComparison.OrdinalIgnoreCase)

    type Intermediates =
        { RequiredIterations: int
          MissingScenarios: string list
          MissingArtifacts: string list
          InvalidWarmup: string list
          InvalidRepetitions: string list
          InvalidSamplePolicy: string list
          UnsupportedArtifactViolation: bool
          FullValidationBlocked: bool }

    let private feature160Config
        : ReadinessValidatorConfig<Feature160ThroughputReadinessCheck, Intermediates, Feature160ThroughputReadinessStatus, Feature160ThroughputReadinessValidationResult> =
        { ComputeIntermediates =
            fun check ->
                let requiredScenarioResults =
                    check.RequiredScenarioIds
                    |> List.choose (fun scenario ->
                        check.Scenarios |> List.tryFind (fun candidate -> candidate.ScenarioId = scenario))

                { RequiredIterations = max 1 check.RequiredIterationCount
                  MissingScenarios =
                    check.RequiredScenarioIds
                    |> List.filter (fun scenario ->
                        check.Scenarios
                        |> List.exists (fun candidate -> candidate.ScenarioId = scenario && candidate.Covered)
                        |> not)
                  MissingArtifacts =
                    requiredScenarioResults
                    |> List.choose (fun scenario ->
                        if List.isEmpty scenario.ArtifactPaths then Some scenario.ScenarioId else None)
                  InvalidWarmup =
                    requiredScenarioResults
                    |> List.choose (fun scenario ->
                        if scenario.WarmupCount <> 3 then Some scenario.ScenarioId else None)
                  InvalidRepetitions =
                    requiredScenarioResults
                    |> List.choose (fun scenario ->
                        if scenario.MeasuredRepetitions <> 5 then Some scenario.ScenarioId else None)
                  InvalidSamplePolicy =
                    requiredScenarioResults
                    |> List.choose (fun scenario ->
                        if samplePolicyAccepted scenario.SamplePolicy then None else Some scenario.ScenarioId)
                  UnsupportedArtifactViolation =
                    check.UnsupportedHostStatus = Feature160EnvironmentLimited
                    && check.AcceptedUnsupportedHostArtifacts <> 0
                  FullValidationBlocked = not (fullValidationPassed check.FullValidationStatus) }
          Feature = fun check -> check.Feature
          FeatureNameMissingMessage = "Feature 160 throughput readiness check must name the feature"
          MissingScenarios = fun inter -> inter.MissingScenarios
          MissingScenarioMessage = fun scenario -> $"missing required throughput scenario: {scenario}"
          MiddleDiagnostics =
            fun check inter ->
                [ for scenario in inter.MissingArtifacts do
                      $"missing throughput artifact path: {scenario}"
                  for scenario in inter.InvalidWarmup do
                      $"scenario warmup must be 3: {scenario}"
                  for scenario in inter.InvalidRepetitions do
                      $"scenario measured repetitions must be 5: {scenario}"
                  for scenario in inter.InvalidSamplePolicy do
                      $"scenario sample policy is not accepted: {scenario}"
                  if check.AcceptedIterationCount < inter.RequiredIterations
                     && check.UnsupportedHostStatus <> Feature160EnvironmentLimited then
                      $"accepted Feature 160 throughput requires at least {inter.RequiredIterations} accepted iterations"
                  if inter.UnsupportedArtifactViolation then
                      "unsupported-host evidence must contain zero accepted Feature 160 performance artifacts"
                  if inter.FullValidationBlocked && check.AcceptedIterationCount >= inter.RequiredIterations && inter.MissingScenarios.IsEmpty then
                      $"full validation blocks release-ready status: {check.FullValidationStatus}" ]
          CompatibilityAccepted = fun check -> check.CompatibilityAccepted
          PackageAccepted = fun check -> check.PackageAccepted
          RegressionAccepted = fun check -> check.RegressionAccepted
          PerformanceClaim = fun check -> check.PerformanceClaim
          PerformanceClaimMessage = "Feature 160 cannot accept the shipped P7 performance claim by itself"
          Limitations = fun check -> check.Limitations
          LimitationKeyword = "overclaim"
          LimitationMessage = fun limitation -> $"overclaimed Feature 160 throughput limitation: {limitation}"
          DeriveStatus =
            fun check inter diagnostics ->
                if check.UnsupportedHostStatus = Feature160EnvironmentLimited && check.AcceptedIterationCount = 0 then
                    Feature160EnvironmentLimited
                elif inter.UnsupportedArtifactViolation
                     || not inter.MissingScenarios.IsEmpty
                     || not inter.MissingArtifacts.IsEmpty
                     || not inter.InvalidWarmup.IsEmpty
                     || not inter.InvalidRepetitions.IsEmpty
                     || not inter.InvalidSamplePolicy.IsEmpty
                     || check.PerformanceClaim <> "performance-not-accepted" then
                    Feature160Rejected
                elif check.AcceptedIterationCount >= inter.RequiredIterations
                     && inter.FullValidationBlocked then
                    Feature160Blocked
                elif diagnostics.IsEmpty
                     && check.AcceptedIterationCount >= inter.RequiredIterations
                     && fullValidationPassed check.FullValidationStatus then
                    Feature160Accepted
                elif check.AcceptedIterationCount = 0 then
                    Feature160FallbackOnly
                else
                    Feature160Rejected
          BuildResult =
            fun inter status diagnostics ->
                { Accepted = status = Feature160Accepted && diagnostics.IsEmpty
                  Status = status
                  MissingScenarios = inter.MissingScenarios
                  Diagnostics = diagnostics } }

    let validate (check: Feature160ThroughputReadinessCheck) : Feature160ThroughputReadinessValidationResult =
        ReadinessValidator.validateReadiness feature160Config check

module Feature161HostLaneReadiness =
    let statusText status =
        match status with
        | Feature161Accepted -> "accepted"
        | Feature161Blocked -> "blocked"
        | Feature161Rejected -> "rejected"
        | Feature161FallbackOnly -> "fallback-only"
        | Feature161EnvironmentLimited -> "environment-limited"
        | Feature161MissingEvidence -> "missing-evidence"

    let fullValidationPassed (status: string) =
        String.Equals(status, "passed", StringComparison.OrdinalIgnoreCase)
        || String.Equals(status, "current-passed", StringComparison.OrdinalIgnoreCase)

    let blank (value: string) = String.IsNullOrWhiteSpace value

    let requiredFacts (facts: Feature161HostFactEvidence option) =
        match facts with
        | None ->
            [ "host-facts" ]
        | Some facts ->
            [ if blank facts.LaneId then "lane-id"
              if blank facts.DisplayServer then "display-server"
              if blank facts.DisplayIdentity then "display-identity"
              if blank facts.RendererIdentity then "renderer-identity"
              if facts.DirectRendering.IsNone then "direct-rendering"
              if blank facts.RefreshStatus then "refresh-status"
              if blank facts.DriverIdentity then "driver-identity"
              if blank facts.PackageVersionSet then "package-version-set"
              if blank facts.CpuLoadNote then "cpu-load-note"
              if blank facts.GpuLoadNote then "gpu-load-note"
              if blank facts.HostProfile then "host-profile"
              if blank facts.RunIdentity then "run-identity"
              if blank facts.ScenarioIdentity then "scenario-identity"
              if blank facts.TimingPolicyIdentity then "timing-policy-identity"
              if List.isEmpty facts.ArtifactPaths then "artifact-paths" ]

    let unsupportedFacts (facts: Feature161HostFactEvidence option) =
        match facts with
        | None -> []
        | Some facts ->
            [ if facts.DisplayServer = "missing-display" || blank facts.DisplayIdentity then "missing-display"
              if facts.DirectRendering = Some false then "indirect-rendering"
              if facts.RendererIdentity.Contains("llvmpipe", StringComparison.OrdinalIgnoreCase)
                 || facts.RendererIdentity.Contains("software", StringComparison.OrdinalIgnoreCase) then
                  "software-raster"
              if facts.RendererIdentity = "unknown" then "unknown-renderer"
              if facts.EnvironmentLimits |> List.exists (fun item -> item.Contains("virtual", StringComparison.OrdinalIgnoreCase)) then
                  "virtualized-presentation"
              if facts.PackageVersionSet.Contains("stale", StringComparison.OrdinalIgnoreCase) then
                  "stale-package" ]

    type Intermediates =
        { MissingScenarios: string list
          MissingFacts: string list
          Unsupported: string list
          PriorGateBlocked: bool
          FullValidationBlocked: bool
          UnsupportedArtifactViolation: bool }

    let private feature161Config
        : ReadinessValidatorConfig<Feature161HostLaneReadinessCheck, Intermediates, Feature161HostLaneReadinessStatus, Feature161HostLaneReadinessValidationResult> =
        { ComputeIntermediates =
            fun check ->
                { MissingScenarios =
                    check.RequiredScenarioIds
                    |> List.filter (fun scenario -> check.CoveredScenarioIds |> List.contains scenario |> not)
                  MissingFacts = requiredFacts check.HostFacts
                  Unsupported = unsupportedFacts check.HostFacts
                  PriorGateBlocked =
                    check.PriorGateStatuses
                    |> List.exists (fun status ->
                        not (String.Equals(status, "confirmed", StringComparison.OrdinalIgnoreCase))
                        && not (String.Equals(status, "accepted", StringComparison.OrdinalIgnoreCase)))
                  FullValidationBlocked = not (fullValidationPassed check.FullValidationStatus)
                  UnsupportedArtifactViolation =
                    check.UnsupportedHostStatus = Feature161EnvironmentLimited
                    && check.AcceptedLaneScopedPerformanceArtifacts <> 0 }
          Feature = fun check -> check.Feature
          FeatureNameMissingMessage = "Feature 161 host lane readiness check must name the feature"
          MissingScenarios = fun inter -> inter.MissingScenarios
          MissingScenarioMessage = fun scenario -> $"missing required host-lane scenario: {scenario}"
          MiddleDiagnostics =
            fun check inter ->
                [ for fact in inter.MissingFacts do
                      $"missing host lane fact: {fact}"
                  for item in inter.Unsupported do
                      $"unsupported host lane fact: {item}"
                  if check.AcceptedLaneScopedPerformanceArtifacts < 1
                     && check.UnsupportedHostStatus <> Feature161EnvironmentLimited then
                      "accepted Feature 161 host-lane readiness requires at least one accepted lane-scoped performance artifact"
                  if inter.UnsupportedArtifactViolation then
                      "unsupported-host evidence must contain zero accepted Feature 161 performance artifacts"
                  if check.ClaimScope.AcceptedLaneId.IsNone
                     && check.AcceptedLaneScopedPerformanceArtifacts > 0 then
                      "accepted artifacts must name an accepted lane id"
                  if List.isEmpty check.ClaimScope.NonGeneralizedLanes then
                      "claim scope must list non-generalized lanes"
                  if inter.PriorGateBlocked then
                      "prior P7 gate status blocks host-lane readiness"
                  if inter.FullValidationBlocked && check.AcceptedLaneScopedPerformanceArtifacts > 0 then
                      $"full validation blocks release-ready status: {check.FullValidationStatus}" ]
          CompatibilityAccepted = fun check -> check.CompatibilityAccepted
          PackageAccepted = fun check -> check.PackageAccepted
          RegressionAccepted = fun check -> check.RegressionAccepted
          PerformanceClaim = fun check -> check.ClaimScope.PerformanceClaim
          PerformanceClaimMessage = "Feature 161 cannot broaden or accept the shipped P7 performance claim by itself"
          Limitations = fun check -> check.Limitations
          LimitationKeyword = "overclaim"
          LimitationMessage = fun limitation -> $"overclaimed Feature 161 host-lane limitation: {limitation}"
          DeriveStatus =
            fun check inter diagnostics ->
                if check.UnsupportedHostStatus = Feature161EnvironmentLimited
                   && check.AcceptedLaneScopedPerformanceArtifacts = 0 then
                    Feature161EnvironmentLimited
                elif not inter.MissingFacts.IsEmpty then
                    Feature161MissingEvidence
                elif inter.UnsupportedArtifactViolation
                     || not inter.MissingScenarios.IsEmpty
                     || not inter.Unsupported.IsEmpty
                     || check.ClaimScope.AcceptedLaneId.IsNone
                     || check.ClaimScope.PerformanceClaim <> "performance-not-accepted" then
                    Feature161Rejected
                elif inter.PriorGateBlocked || inter.FullValidationBlocked then
                    Feature161Blocked
                elif diagnostics.IsEmpty && check.AcceptedLaneScopedPerformanceArtifacts > 0 then
                    Feature161Accepted
                elif check.AcceptedLaneScopedPerformanceArtifacts = 0 then
                    Feature161FallbackOnly
                else
                    Feature161Rejected
          BuildResult =
            fun inter status diagnostics ->
                { Accepted = status = Feature161Accepted && diagnostics.IsEmpty
                  Status = status
                  MissingFacts = inter.MissingFacts
                  MissingScenarios = inter.MissingScenarios
                  Diagnostics = diagnostics } }

    let validate (check: Feature161HostLaneReadinessCheck) : Feature161HostLaneReadinessValidationResult =
        ReadinessValidator.validateReadiness feature161Config check

// Feature 182 (US4): kept in the residual Testing.fs (not moved to TestingEvidence.fs) so the public
// contract file `Testing.fsi` still declares `module PackageInspectionAssertions` (Feature146 package-
// surface test reads the Testing.fsi file text). Self-contained; no Evidence-module dependency.
module PackageInspectionAssertions =
    let validate (check: PackageInspectionAssertionCheck) =
        let diagnostics =
            [ if check.Report.Status <> check.ExpectedStatus then
                  $"expected package inspection status {check.ExpectedStatus}, got {check.Report.Status}"

              for fragment in check.RequiredDiagnosticFragments do
                  let found =
                      check.Report.Diagnostics
                      |> List.exists (fun diagnostic -> diagnostic.Message.Contains(fragment, StringComparison.OrdinalIgnoreCase))

                  if not found then
                      $"missing package diagnostic containing '{fragment}'" ]

        { Accepted = diagnostics.IsEmpty
          Diagnostics = diagnostics }

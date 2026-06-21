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

module CompositorReadiness =
    // Migrated onto the shared FS.GG.UI.Diagnostics.ReadinessStatus vocabulary (Feature 180). The
    // domain-specific FallbackGated/CompatibilityBlocked cases keep their existing literals; the rest route
    // through the single statusToken table. Output tokens are byte-identical to the prior per-domain mapper.
    let private toShared status =
        match status with
        | CompositorReadinessAccepted -> FS.GG.UI.Diagnostics.ReadinessStatus.Accepted
        | CompositorReadinessFallbackGated -> FS.GG.UI.Diagnostics.ReadinessStatus.FallbackOnly
        | CompositorReadinessFailed -> FS.GG.UI.Diagnostics.ReadinessStatus.Failed
        | CompositorReadinessEnvironmentLimited -> FS.GG.UI.Diagnostics.ReadinessStatus.EnvironmentLimited
        | CompositorReadinessMissingEvidence -> FS.GG.UI.Diagnostics.ReadinessStatus.Missing
        | CompositorReadinessCompatibilityBlocked -> FS.GG.UI.Diagnostics.ReadinessStatus.Blocked

    let statusText status =
        match status with
        | CompositorReadinessFallbackGated -> "fallback-gated"
        | CompositorReadinessCompatibilityBlocked -> "compatibility-blocked"
        | other -> FS.GG.UI.Diagnostics.ReadinessStatus.statusToken (toShared other)

    let private blocksCorrectness status =
        match status with
        | CompositorReadinessAccepted -> false
        | _ -> true

    let validate (report: CompositorReadinessReport) : CompositorReadinessValidationResult =
        let requiredStatuses =
            [ "proof", report.ProofStatus
              "parity", report.ParityStatus
              "compatibility", report.CompatibilityStatus
              "regression", report.RegressionStatus ]

        let missingEvidence =
            report.Evidence
            |> List.choose (fun evidence ->
                if evidence.EvidenceRequired && (evidence.EvidencePath.IsNone || evidence.EvidenceStatus = CompositorReadinessMissingEvidence) then
                    Some evidence.EvidenceName
                else
                    None)

        let blockedStatus =
            requiredStatuses
            |> List.filter (snd >> blocksCorrectness)
            |> List.map (fun (name, status) -> $"{name}:{statusText status}")

        let blockingLimitations =
            report.Limitations
            |> List.filter (fun item -> item.Contains("blocking", StringComparison.OrdinalIgnoreCase))

        let diagnostics =
            [ if String.IsNullOrWhiteSpace report.Feature then
                  "compositor readiness report must name the feature"
              for missing in missingEvidence do
                  $"missing required compositor readiness evidence: {missing}"
              for status in blockedStatus do
                  $"blocking compositor readiness status: {status}"
              if report.TimingStatus <> CompositorReadinessAccepted then
                  $"timing claim status: {statusText report.TimingStatus}"
              for limitation in blockingLimitations do
                  $"blocking compositor readiness limitation: {limitation}"
              for evidence in report.Evidence do
                  yield! evidence.EvidenceDiagnostics ]

        let status =
            if not missingEvidence.IsEmpty then
                CompositorReadinessMissingEvidence
            elif report.CompatibilityStatus = CompositorReadinessCompatibilityBlocked then
                CompositorReadinessCompatibilityBlocked
            elif report.ProofStatus = CompositorReadinessEnvironmentLimited then
                CompositorReadinessEnvironmentLimited
            elif requiredStatuses |> List.exists (snd >> (=) CompositorReadinessFailed) then
                CompositorReadinessFailed
            elif not blockedStatus.IsEmpty || not blockingLimitations.IsEmpty then
                CompositorReadinessFallbackGated
            else
                CompositorReadinessAccepted

        { Accepted = status = CompositorReadinessAccepted
          Status = status
          MissingEvidence = missingEvidence
          BlockingLimitations = blockingLimitations
          Diagnostics = diagnostics }

module CompositorTimingAssertions =
    let verdictText verdict =
        match verdict with
        | CompositorTimingPositive -> "positive"
        | CompositorTimingNoisy -> "noisy"
        | CompositorTimingNonBeneficial -> "non-beneficial"
        | CompositorTimingIncomplete -> "incomplete"
        | CompositorTimingRejected -> "rejected"
        | CompositorTimingEnvironmentLimited -> "environment-limited"
        | CompositorTimingLimited -> "limited"

    let private verdictBlocksPositive verdict =
        match verdict with
        | CompositorTimingPositive -> false
        | _ -> true

    let validateSummary (check: CompositorTimingSummaryCheck) : CompositorTimingSummaryValidationResult =
        let missingScenarios =
            check.RequiredScenarioIds
            |> List.filter (fun scenario ->
                check.Scenarios |> List.exists (fun candidate -> candidate.ScenarioId = scenario) |> not)

        let requiredScenarioResults =
            check.RequiredScenarioIds
            |> List.choose (fun scenario ->
                check.Scenarios |> List.tryFind (fun candidate -> candidate.ScenarioId = scenario))

        let rejectedScenarios =
            requiredScenarioResults
            |> List.choose (fun scenario ->
                if verdictBlocksPositive scenario.Verdict then
                    Some $"{scenario.ScenarioId}:{verdictText scenario.Verdict}"
                else
                    None)

        let incompleteSamples =
            requiredScenarioResults
            |> List.choose (fun scenario ->
                if scenario.FullRedrawSampleCount < check.MeasuredRepetitions
                   || scenario.DamageScopedSampleCount < check.MeasuredRepetitions then
                    Some scenario.ScenarioId
                else
                    None)

        let missingArtifacts =
            requiredScenarioResults
            |> List.choose (fun scenario ->
                if List.isEmpty scenario.ArtifactPaths then Some scenario.ScenarioId else None)

        let diagnostics =
            [ if String.IsNullOrWhiteSpace check.Feature then
                  "timing summary must name the feature"
              if check.ExpectedProfileId <> check.ActualProfileId then
                  $"profile mismatch: expected={check.ExpectedProfileId} actual={check.ActualProfileId}"
              if check.PolicyId <> "same-profile-live-threshold-v2" then
                  $"unexpected timing policy: {check.PolicyId}"
              if check.WarmupCount < 0 then
                  "warmup count must not be negative"
              if check.MeasuredRepetitions < 5 then
                  "at least five measured repetitions are required"
              for scenario in missingScenarios do
                  $"missing required timing scenario: {scenario}"
              for scenario in rejectedScenarios do
                  $"non-positive timing scenario: {scenario}"
              for scenario in incompleteSamples do
                  $"incomplete timing samples: {scenario}"
              for scenario in missingArtifacts do
                  $"missing timing artifacts: {scenario}"
              if check.ShippedPerformanceClaim <> "performance-not-accepted" then
                  "Feature 156 cannot accept the shipped P7 performance claim by itself"
              for scenario in requiredScenarioResults do
                  yield! scenario.RejectionReasons |> List.map (fun reason -> $"{scenario.ScenarioId}: {reason}") ]

        let verdict =
            if not missingScenarios.IsEmpty || not incompleteSamples.IsEmpty || not missingArtifacts.IsEmpty then
                CompositorTimingIncomplete
            elif diagnostics |> List.exists (fun item -> item.Contains("profile mismatch", StringComparison.OrdinalIgnoreCase)) then
                CompositorTimingRejected
            elif requiredScenarioResults |> List.exists (fun scenario -> scenario.Verdict = CompositorTimingEnvironmentLimited) then
                CompositorTimingEnvironmentLimited
            elif requiredScenarioResults |> List.exists (fun scenario -> scenario.Verdict = CompositorTimingLimited) then
                CompositorTimingLimited
            elif requiredScenarioResults |> List.exists (fun scenario -> scenario.Verdict = CompositorTimingRejected) then
                CompositorTimingRejected
            elif requiredScenarioResults |> List.exists (fun scenario -> scenario.Verdict = CompositorTimingNoisy) then
                CompositorTimingNoisy
            elif requiredScenarioResults |> List.exists (fun scenario -> scenario.Verdict = CompositorTimingNonBeneficial) then
                CompositorTimingNonBeneficial
            elif requiredScenarioResults.Length = check.RequiredScenarioIds.Length then
                CompositorTimingPositive
            else
                CompositorTimingIncomplete

        { Accepted = verdict = CompositorTimingPositive && diagnostics.IsEmpty
          Verdict = verdict
          MissingScenarios = missingScenarios
          RejectedScenarios = rejectedScenarios
          Diagnostics = diagnostics }

module CompositorDamageReadiness =
    // Migrated onto the shared FS.GG.UI.Diagnostics.ReadinessStatus vocabulary (Feature 180): every case
    // maps cleanly, so statusText is the single shared statusToken table applied to toShared. Output tokens
    // are byte-identical to the prior per-domain mapper.
    let private toShared status =
        match status with
        | CompositorDamageAccepted -> FS.GG.UI.Diagnostics.ReadinessStatus.Accepted
        | CompositorDamageFallbackOnly -> FS.GG.UI.Diagnostics.ReadinessStatus.FallbackOnly
        | CompositorDamageRejected -> FS.GG.UI.Diagnostics.ReadinessStatus.Rejected
        | CompositorDamageEnvironmentLimited -> FS.GG.UI.Diagnostics.ReadinessStatus.EnvironmentLimited

    let statusText status =
        FS.GG.UI.Diagnostics.ReadinessStatus.statusToken (toShared status)

    // Per-domain override (C-RS-6): unlike the canonical default, CompositorDamage treats EnvironmentLimited
    // as blocking, so this rule stays domain-local rather than routing to ReadinessStatus.blocksAcceptance.
    let private statusBlocksAcceptance status =
        match status with
        | CompositorDamageAccepted -> false
        | _ -> true

    let validate (check: CompositorDamageReadinessCheck) : CompositorDamageReadinessValidationResult =
        let missingScenarios =
            check.RequiredScenarioIds
            |> List.filter (fun scenario ->
                check.Scenarios |> List.exists (fun candidate -> candidate.ScenarioId = scenario) |> not)

        let requiredScenarioResults =
            check.RequiredScenarioIds
            |> List.choose (fun scenario ->
                check.Scenarios |> List.tryFind (fun candidate -> candidate.ScenarioId = scenario))

        let missingArtifacts =
            requiredScenarioResults
            |> List.choose (fun scenario ->
                if List.isEmpty scenario.ArtifactPaths then Some scenario.ScenarioId else None)

        let scenarioFailures =
            requiredScenarioResults
            |> List.choose (fun scenario ->
                if statusBlocksAcceptance scenario.Status then
                    Some $"{scenario.ScenarioId}:{statusText scenario.Status}"
                else
                    None)

        let fallbackOnly =
            requiredScenarioResults.Length = check.RequiredScenarioIds.Length
            && requiredScenarioResults |> List.forall (fun scenario -> scenario.Status = CompositorDamageFallbackOnly)

        let environmentLimited =
            requiredScenarioResults |> List.exists (fun scenario -> scenario.Status = CompositorDamageEnvironmentLimited)

        let unsupportedArtifactViolation =
            check.AcceptedPartialRedrawArtifacts <> 0
            && check.UnsupportedHostStatus = CompositorDamageEnvironmentLimited

        let diagnostics =
            [ if String.IsNullOrWhiteSpace check.Feature then
                  "damage readiness check must name the feature"
              for scenario in missingScenarios do
                  $"missing required damage scenario: {scenario}"
              for scenario in missingArtifacts do
                  $"missing damage artifact path: {scenario}"
              for failure in scenarioFailures do
                  $"non-accepted damage scenario: {failure}"
              if check.AcceptedAttemptCount < 3 && not fallbackOnly && not environmentLimited then
                  "accepted damage readiness requires at least three accepted attempts"
              if unsupportedArtifactViolation then
                  "unsupported-host evidence must contain zero accepted partial-redraw artifacts"
              if not check.CompatibilityAccepted then
                  "compatibility ledger is not accepted"
              if not check.PackageAccepted then
                  "package validation is not accepted"
              if not check.RegressionAccepted then
                  "regression validation is not accepted"
              if check.PerformanceClaim <> "performance-not-accepted" then
                  "Feature 157 cannot accept the shipped P7 performance claim by itself"
              for limitation in check.Limitations do
                  if limitation.Contains("blocking", StringComparison.OrdinalIgnoreCase) then
                      $"blocking damage readiness limitation: {limitation}" ]

        let status =
            if environmentLimited || unsupportedArtifactViolation then
                CompositorDamageEnvironmentLimited
            elif not missingScenarios.IsEmpty || not missingArtifacts.IsEmpty then
                CompositorDamageRejected
            elif requiredScenarioResults |> List.exists (fun scenario -> scenario.Status = CompositorDamageRejected) then
                CompositorDamageRejected
            elif fallbackOnly then
                CompositorDamageFallbackOnly
            elif diagnostics.IsEmpty
                 && check.AcceptedAttemptCount >= 3
                 && requiredScenarioResults |> List.forall (fun scenario -> scenario.Status = CompositorDamageAccepted) then
                CompositorDamageAccepted
            else
                CompositorDamageFallbackOnly

        { Accepted = status = CompositorDamageAccepted && diagnostics.IsEmpty
          Status = status
          MissingScenarios = missingScenarios
          Diagnostics = diagnostics }


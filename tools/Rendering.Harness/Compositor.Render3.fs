namespace Rendering.Harness.Compositor

open Rendering.Harness

open System
open System.IO
open System.Security.Cryptography
open System.Text
open Rendering.Harness.Compositor.Types
open Rendering.Harness.Compositor.Config
open Rendering.Harness.Compositor.FeatureState
open Rendering.Harness.Compositor.Render
open Rendering.Harness.Compositor.Render2

module Render3 =
    let emitFeature156ScenarioReport (report: Feature156ScenarioReport) =
        let full = feature156DistributionRow report.FullRedraw
        let damage = feature156DistributionRow report.DamageScoped
        let artifacts = renderArtifacts report.ArtifactPaths
        let overhead =
            if report.ProofOverheadIncluded then
                "proof-readback-or-validation-overhead-included"
            else
                "measurement-path-isolated-from-proof-readback"

        String.concat
            "\n"
            [ $"# Feature 156 Scenario: {report.ScenarioId}"
              ""
              $"Scenario id: `{report.ScenarioId}`"
              $"Verdict: `{feature156VerdictToken report.Verdict}`"
              $"Confidence decision: `{report.ConfidenceDecision}`"
              $"Warmup count: `{report.WarmupCount}`"
              $"Measured repetitions: `{report.MeasuredRepetitions}`"
              $"Noise band ms: `{feature156FormatMs report.NoiseBandMs}`"
              $"Overhead disclosure: `{overhead}`"
              ""
              "| Path | p50 ms | p95 ms | p99 ms | Samples |"
              "|------|--------|--------|--------|---------|"
              $"| full-redraw | {full} |"
              $"| damage-scoped | {damage} |"
              ""
              "## Artifacts"
              ""
              artifacts
              ""
              "## Rejection Reasons"
              ""
              feature156Reasons report
              "" ]

    let emitFeature156TimingSummary (summary: Feature156TimingSummary) =
        let renderer = summary.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = summary.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"
        let rows =
            if List.isEmpty summary.ScenarioReports then
                "| none | missing | missing | missing | missing | missing | missing | missing | missing | missing |"
            else
                summary.ScenarioReports
                |> List.map (fun report ->
                    let fullP50 = report.FullRedraw |> Option.map (fun d -> feature156FormatMs d.P50Ms) |> Option.defaultValue "missing"
                    let fullP95 = report.FullRedraw |> Option.map (fun d -> feature156FormatMs d.P95Ms) |> Option.defaultValue "missing"
                    let fullP99 = report.FullRedraw |> Option.map (fun d -> feature156FormatMs d.P99Ms) |> Option.defaultValue "missing"
                    let damageP50 = report.DamageScoped |> Option.map (fun d -> feature156FormatMs d.P50Ms) |> Option.defaultValue "missing"
                    let damageP95 = report.DamageScoped |> Option.map (fun d -> feature156FormatMs d.P95Ms) |> Option.defaultValue "missing"
                    let damageP99 = report.DamageScoped |> Option.map (fun d -> feature156FormatMs d.P99Ms) |> Option.defaultValue "missing"
                    let artifact =
                        report.ArtifactPaths
                        |> List.tryFind (fun path -> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                        |> Option.defaultValue (Path.Combine("scenarios", feature156ScenarioFileName report.ScenarioId))

                    $"| `{report.ScenarioId}` | `{fullP50}` | `{fullP95}` | `{fullP99}` | `{damageP50}` | `{damageP95}` | `{damageP99}` | `{feature156FormatMs report.NoiseBandMs}` | `{feature156VerdictToken report.Verdict}` | `{report.ConfidenceDecision}` | `{artifact}` |")
                |> String.concat "\n"

        let rejectionReasons =
            summary.ScenarioReports
            |> List.collect (fun report -> report.RejectionReasons |> List.map (fun reason -> $"{report.ScenarioId}: {reason}"))

        String.concat
            "\n"
            [ "# Feature 156 Same-Profile Timing Summary"
              ""
              $"Run identity: `{summary.RunId}`"
              $"Feature 156 timing verdict: `{feature156VerdictToken summary.OverallVerdict}`"
              $"Shipped P7 performance claim: `{summary.ShippedPerformanceClaim}`"
              $"Policy id: `{summary.PolicyId}`"
              "Noise-band formula: `max(0.25 ms, 5% of full-redraw p50)`"
              $"Warmup count: `{summary.WarmupCount}`"
              $"Measured repetitions per path: `{summary.MeasuredRepetitions}`"
              ""
              "## Host Profile"
              ""
              $"- Accepted profile id: `{feature156AcceptedProfileId}`"
              $"- Measured profile id: `{summary.HostProfile.ProfileId}`"
              $"- Backend: `{summary.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{summary.HostProfile.PresentMode}`"
              $"- Framebuffer: `{summary.HostProfile.FramebufferSize}`"
              $"- Scale: `{scale}`"
              $"- Display environment: `{summary.HostProfile.DisplayEnvironment}`"
              $"- Package version: `{feature156PackageVersion}`"
              ""
              "## Feature 155 Baseline"
              ""
              "- Proof/parity baseline: `../155-native-proof-capture/readiness/validation-summary.md`"
              "- Correctness status: `accepted` for accepted host profile `probe-08a47c01`."
              "- Fallback status: `partial-redraw-accepted` for correctness; performance remains separate."
              ""
              "## Scenario Table"
              ""
              "| Scenario | Full p50 | Full p95 | Full p99 | Damage p50 | Damage p95 | Damage p99 | Noise band | Verdict | Confidence | Artifact |"
              "|----------|----------|----------|----------|------------|------------|------------|------------|---------|------------|----------|"
              rows
              ""
              "## Rejection Reasons"
              ""
              if List.isEmpty rejectionReasons then "- none" else rejectionReasons |> List.map (sprintf "- %s") |> String.concat "\n"
              ""
              "## Overhead Disclosure"
              ""
              "- Scenario reports state whether proof readback or validation overhead is included."
              "- Readback-dominated or unseparated overhead is `limited` and cannot support a shipped claim."
              ""
              "## Remaining Gates"
              ""
              "- Feature 157 damage-scissored no-clear renderer: `remaining`"
              "- Feature 158 readback separation: `remaining`"
              "- Feature 159 net-positive reuse/promotion counters: `remaining`"
              "- Feature 160 validation throughput follow-up: `remaining`, not a shipped performance-acceptance gate"
              "- Feature 161 host performance lane ledger: `remaining`"
              ""
              "## Diagnostics"
              ""
              if List.isEmpty summary.Diagnostics then "- none" else summary.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"
              "" ]

    let emitFeature156CompatibilityLedger () =
        String.concat
            "\n"
            [ "# Feature 156 Compatibility Ledger"
              ""
              "Status: `accepted`"
              ""
              "## Public API and Diagnostics"
              ""
              "- `FS.GG.UI.Testing.CompositorTimingAssertions` adds package-visible timing summary validation for Feature 156."
              "- `FS.GG.UI.SkiaViewer.CompositorProof` adds timing path and proof-overhead disclosure helpers used by viewer-facing tests."
              "- `Rendering.Harness` adds `compositor-performance --feature 156` and `compositor-readiness --feature 156` evidence routes."
              ""
              "## Compatibility Impact"
              ""
              "- Existing Feature 155 proof, parity, fallback, and correctness vocabulary remains authoritative."
              "- `performance-not-accepted` remains the shipped P7 performance claim until later gates pass."
              "- New timing helpers are additive and do not change package identities."
              ""
              "## Migration Guidance"
              ""
              "- Consumers should treat `noisy`, `non-beneficial`, `incomplete`, `rejected`, `limited`, and `environment-limited` as non-accepting timing states."
              "- Positive Feature 156 timing is scoped to `probe-08a47c01` and is not a universal host performance claim."
              "" ]

    let emitFeature156ValidationSummary (summary: Feature156TimingSummary) =
        String.concat
            "\n"
            [ "# Feature 156 Readiness Summary"
              ""
              $"Status: `{feature156VerdictToken summary.OverallVerdict}`"
              "Proof/parity baseline: `accepted`"
              $"Timing status: `{feature156VerdictToken summary.OverallVerdict}`"
              "Correctness status: `accepted-via-feature-155`"
              "Fallback status: `partial-redraw-accepted`"
              $"Performance claim: `{summary.ShippedPerformanceClaim}`"
              $"Accepted host profile: `{feature156AcceptedProfileId}`"
              ""
              "## Evidence Links"
              ""
              "- Timing summary: `timing/summary.md`"
              "- Scenario reports: `timing/scenarios/`"
              "- Raw samples: `timing/raw/`"
              "- Unsupported host: `timing/unsupported/README.md`"
              "- Compatibility ledger: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI timing authoring: `fsi/compositor-performance-authoring.fsx`"
              "- FSI readiness authoring: `fsi/compositor-readiness-authoring.fsx`"
              ""
              "## Reviewer Determination"
              ""
              "- A reviewer can determine scenario verdicts, distributions, host profile, policy, artifact paths, limitations, and final claim status from `timing/summary.md`."
              "- Under-5-minute determination check: recorded in this package after local validation."
              ""
              "## Decision"
              ""
              "- Timing evidence is fail-closed and scoped to the accepted Feature 155 profile."
              "- `performance-not-accepted` remains the shipped P7 performance claim until Features 157, 158, 159, and 161 pass."
              "- Feature 160 remains a validation-throughput follow-up, not a performance-acceptance gate."
              "" ]

    let emitFeature156UnsupportedHostReport (reason: string) =
        String.concat
            "\n"
            [ "# Feature 156 Unsupported Host Timing"
              ""
              "Status: `environment-limited`"
              "Accepted performance artifacts: `0`"
              $"Reason: `{reason}`"
              ""
              "Unsupported or unavailable presentation environments cannot contribute to positive timing evidence."
              "" ]

    let emitFeature157Artifacts artifacts =
        match artifacts with
        | [] -> "- none"
        | xs -> xs |> List.map (sprintf "- `%s`") |> String.concat "\n"

    let emitFeature157AttemptReport (attempt: Feature157DamageAttempt) =
        let fallback =
            attempt.FallbackReason |> Option.defaultValue "none"
        let diagnostics =
            if List.isEmpty attempt.Diagnostics then "- none" else attempt.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 157 Damage Attempt"
              ""
              $"Attempt: `{attempt.AttemptId}`"
              $"Run identity: `{attempt.RunId}`"
              $"Scenario: `{attempt.ScenarioId}`"
              $"Host profile: `{attempt.HostProfile.ProfileId}`"
              $"Proof gate: `{attempt.ProofGate}`"
              $"Retained backing: `{attempt.RetainedBacking}`"
              $"Damage validation: `{attempt.DamageValidationStatus}`"
              $"Render decision: `{attempt.RenderDecision}`"
              $"Fallback reason: `{fallback}`"
              $"Preserved-pixel evidence: `{attempt.PreservedPixelEvidence}`"
              $"Damaged-pixel evidence: `{attempt.DamagedPixelEvidence}`"
              $"Parity status: `{attempt.ParityStatus}`"
              ""
              "## Artifacts"
              ""
              emitFeature157Artifacts attempt.ArtifactPaths
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let emitFeature157FallbackReport (fallback: Feature157Fallback) =
        let diagnostics =
            if List.isEmpty fallback.Diagnostics then "- none" else fallback.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 157 Fallback"
              ""
              $"Scenario: `{fallback.ScenarioId}`"
              $"Primary reason: `{fallback.Reason}`"
              $"Damage validation: `{fallback.DamageValidationStatus}`"
              $"Accepted partial-redraw artifacts: `{fallback.AcceptedPartialRedrawArtifacts}`"
              ""
              "## Artifacts"
              ""
              emitFeature157Artifacts fallback.ArtifactPaths
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let emitFeature157ParityReport (attempt: Feature157DamageAttempt) =
        String.concat
            "\n"
            [ "# Feature 157 Parity"
              ""
              $"Scenario: `{attempt.ScenarioId}`"
              $"Attempt: `{attempt.AttemptId}`"
              $"Parity status: `{attempt.ParityStatus}`"
              $"Preserved-pixel evidence: `{attempt.PreservedPixelEvidence}`"
              $"Damaged-pixel evidence: `{attempt.DamagedPixelEvidence}`"
              ""
              "Accepted parity requires zero unexplained drift outside damage and expected updates inside damage."
              "" ]

    let feature157ScenarioRows summary =
        let acceptedRows =
            summary.AcceptedAttempts
            |> List.map (fun attempt ->
                let artifacts = String.concat ", " attempt.ArtifactPaths
                $"| `{attempt.ScenarioId}` | accepted | `{attempt.AttemptId}` | `{attempt.RenderDecision}` | `{attempt.ParityStatus}` | `{artifacts}` |")

        let fallbackRows =
            summary.Fallbacks
            |> List.map (fun fallback ->
                let artifacts = String.concat ", " fallback.ArtifactPaths
                $"| `{fallback.ScenarioId}` | fallback | `none` | `{fallback.Reason}` | `not-accepted` | `{artifacts}` |")

        match acceptedRows @ fallbackRows with
        | [] -> "| none | environment-limited | none | missing evidence | not-accepted | none |"
        | rows -> rows |> String.concat "\n"

    let emitFeature157DamageSummary (summary: Feature157DamageSummary) =
        let status = feature157OverallStatus summary
        let renderer = summary.HostProfile.Renderer |> Option.defaultValue "unknown"
        let unsupported =
            summary.UnsupportedHostReason |> Option.defaultValue "none"
        let diagnostics =
            if List.isEmpty summary.Diagnostics then "- none" else summary.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 157 Damage Summary"
              ""
              $"Run identity: `{summary.RunId}`"
              $"Damage readiness status: `{feature157StatusToken status}`"
              $"Accepted host profile: `{feature157AcceptedProfileId}`"
              $"Measured host profile: `{summary.HostProfile.ProfileId}`"
              $"Unsupported-host reason: `{unsupported}`"
              $"Shipped P7 performance claim: `{summary.PerformanceClaim}`"
              ""
              "## Host Profile"
              ""
              $"- Backend: `{summary.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{summary.HostProfile.PresentMode}`"
              $"- Framebuffer: `{summary.HostProfile.FramebufferSize}`"
              $"- Display environment: `{summary.HostProfile.DisplayEnvironment}`"
              $"- Proof algorithm: `{summary.HostProfile.ProofAlgorithmVersion}`"
              ""
              "## Scenario Coverage"
              ""
              "| Scenario | Status | Attempt | Decision | Parity | Artifacts |"
              "|----------|--------|---------|----------|--------|-----------|"
              feature157ScenarioRows summary
              ""
              "## Required Scenarios"
              ""
              (feature157RequiredScenarioIds |> List.map (sprintf "- `%s`") |> String.concat "\n")
              ""
              "## Fallback Scenarios"
              ""
              (feature157FallbackScenarioIds |> List.map (sprintf "- `%s`") |> String.concat "\n")
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let escapeJson (value: string) =
        value.Replace("\\", "\\\\").Replace("\"", "\\\"")

    let emitFeature157DamageSummaryJson (summary: Feature157DamageSummary) =
        let status = feature157OverallStatus summary
        let unsupportedReason = summary.UnsupportedHostReason |> Option.defaultValue ""
        let scenarios =
            summary.ScenarioCoverage
            |> List.map (fun scenario -> $"    \"{escapeJson scenario}\"")
            |> String.concat ",\n"

        String.concat
            "\n"
            [ "{"
              $"  \"runId\": \"{escapeJson summary.RunId}\","
              $"  \"status\": \"{feature157StatusToken status}\","
              $"  \"hostProfile\": \"{escapeJson summary.HostProfile.ProfileId}\","
              $"  \"acceptedAttemptCount\": {summary.AcceptedAttempts.Length},"
              $"  \"fallbackCount\": {summary.Fallbacks.Length},"
              $"  \"unsupportedHostReason\": \"{escapeJson unsupportedReason}\","
              $"  \"performanceClaim\": \"{escapeJson summary.PerformanceClaim}\","
              "  \"scenarioCoverage\": ["
              scenarios
              "  ]"
              "}" ]

    let emitFeature157CompatibilityLedger () =
        String.concat
            "\n"
            [ "# Feature 157 Compatibility Ledger"
              ""
              "Status: `accepted-with-recorded-limitations`"
              ""
              "## Public API and Diagnostics"
              ""
              "- `FS.GG.UI.SkiaViewer.Host.GlHost` adds Feature 157 damage validation and no-clear render-decision helpers."
              "- `FS.GG.UI.SkiaViewer.Viewer.damageDecisionToken` exposes stable readiness tokens."
              "- `FS.GG.UI.Testing.CompositorDamageReadiness` validates accepted, fallback-only, rejected, and environment-limited damage packages."
              "- `Rendering.Harness` adds `compositor-damage --feature 157` and extends `compositor-readiness --feature 157`."
              ""
              "## Compatibility Impact"
              ""
              "- Existing Feature 155 proof-set and Feature 156 timing contracts remain source-compatible."
              "- Full redraw remains the default fallback unless all Feature 157 gates pass."
              "- The shipped P7 performance claim remains `performance-not-accepted`."
              "" ]

    let emitFeature157ValidationSummary (summary: Feature157DamageSummary) =
        let status = feature157OverallStatus summary
        String.concat
            "\n"
            [ "# Feature 157 Readiness Summary"
              ""
              $"Status: `{feature157StatusToken status}`"
              $"Accepted host profile: `{feature157AcceptedProfileId}`"
              $"Measured host profile: `{summary.HostProfile.ProfileId}`"
              $"Accepted attempts: `{summary.AcceptedAttempts.Length}`"
              $"Fallback attempts: `{summary.Fallbacks.Length}`"
              $"Performance claim: `{summary.PerformanceClaim}`"
              ""
              "## Evidence Links"
              ""
              "- Damage summary: `damage/summary.md`"
              "- Damage summary JSON: `damage/summary.json`"
              "- Accepted attempts: `damage/attempts/`"
              "- Fallbacks: `damage/fallbacks/`"
              "- Parity: `damage/parity/`"
              "- Unsupported host: `damage/unsupported/README.md`"
              "- Compatibility ledger: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI damage authoring: `fsi/compositor-damage-authoring.fsx`"
              "- FSI readiness authoring: `fsi/compositor-readiness-authoring.fsx`"
              ""
              "## Decision"
              ""
              "- Damage-scoped no-clear repaint is selected only when proof, profile, retained backing, damage, resources, and parity all pass."
              "- Missing or unverifiable gates use full redraw and record a primary fallback reason."
              "- `performance-not-accepted` remains the shipped P7 performance claim until later gates pass."
              "" ]

    let emitFeature157UnsupportedHostReport (reason: string) =
        String.concat
            "\n"
            [ "# Feature 157 Unsupported Host"
              ""
              "Status: `environment-limited`"
              "Accepted partial-redraw artifacts: `0`"
              $"Reason: `{reason}`"
              ""
              "Unsupported or unavailable presentation environments cannot accept damage-scoped no-clear artifacts."
              "" ]

    let feature158SampleRows (samples: Feature158TimingSample list) =
        match samples with
        | [] -> "| none | none | none | none | none | none | none | none |"
        | xs ->
            xs
            |> List.map (fun sample ->
                let reason = sample.ExclusionReason |> Option.map Perf.exclusionReasonToken |> Option.defaultValue "none"
                $"| `{sample.SampleId}` | `{sample.ScenarioId}` | `{Perf.timingPathToken sample.Path}` | `{feature156FormatMs sample.DurationMs}` | `{Perf.measurementPolicyToken sample.MeasurementPolicy}` | `{Perf.inclusionStatusToken sample.InclusionStatus}` | `{reason}` | `{sample.ArtifactPath}` |")
            |> String.concat "\n"

    let feature158Distribution (distribution: Feature158PathDistribution option) =
        feature158DistributionRow distribution

    let emitFeature158ScenarioReport (report: Feature158ScenarioReport) =
        let diagnostics =
            if List.isEmpty report.Diagnostics then "- none" else report.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ $"# Feature 158 Scenario: {report.ScenarioId}"
              ""
              $"Scenario id: `{report.ScenarioId}`"
              $"Scenario definition: `{report.ScenarioDefinitionId}`"
              $"Measurement-separation status: `{feature158StatusToken report.Status}`"
              $"Warmup count: `{report.WarmupCount}`"
              $"Measured repetitions: `{report.MeasuredRepetitions}`"
              $"Included timing samples: `{report.IncludedSamples.Length}`"
              $"Excluded timing samples: `{report.ExcludedSamples.Length}`"
              ""
              "## Distributions"
              ""
              "| Path | p50 ms | p95 ms | p99 ms | Samples |"
              "|------|--------|--------|--------|---------|"
              $"| full-redraw | {feature158Distribution report.FullRedraw} |"
              $"| damage-scoped | {feature158Distribution report.DamageScoped} |"
              ""
              "## Included Samples"
              ""
              "| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |"
              "|--------|----------|------|-------------|--------|--------|--------|----------|"
              feature158SampleRows report.IncludedSamples
              ""
              "## Excluded Samples"
              ""
              "| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |"
              "|--------|----------|------|-------------|--------|--------|--------|----------|"
              feature158SampleRows report.ExcludedSamples
              ""
              "## Proof/Probe Artifacts"
              ""
              renderArtifacts report.ProofProbeArtifacts
              ""
              "## Artifacts"
              ""
              renderArtifacts report.ArtifactPaths
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let emitFeature158ExcludedSamplesReport (reason: Perf.ExclusionReason) (samples: Feature158TimingSample list) =
        String.concat
            "\n"
            [ $"# Feature 158 Excluded Samples: {Perf.exclusionReasonToken reason}"
              ""
              $"Primary reason: `{Perf.exclusionReasonToken reason}`"
              $"Excluded sample count: `{samples.Length}`"
              ""
              "| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |"
              "|--------|----------|------|-------------|--------|--------|--------|----------|"
              feature158SampleRows samples
              "" ]

    let emitFeature158ProofProbeReport (evidence: Feature158ProofProbeEvidence list) =
        let rows =
            match evidence with
            | [] -> "| none | none | none | none | none |"
            | xs ->
                xs
                |> List.map (fun item ->
                    let scenarios = String.concat ", " item.ScenarioIds
                    let artifacts = String.concat ", " item.ReadbackArtifacts
                    let samples = String.concat ", " item.ProbeSampleIds
                    $"| `{item.ProbeId}` | `{item.HostProfile.ProfileId}` | `{scenarios}` | `{Perf.exclusionReasonToken item.ExclusionReason}` | `{samples}` | `{artifacts}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 158 Proof/Probe Evidence"
              ""
              "Proof/readback remains available as explicit probe evidence and is excluded from performance acceptance."
              ""
              "| Probe | Host profile | Scenarios | Exclusion reason | Probe samples | Readback artifacts |"
              "|-------|--------------|-----------|------------------|---------------|--------------------|"
              rows
              "" ]

    let feature158ScenarioRows (summary: Feature158TimingSummary) =
        match summary.ScenarioReports with
        | [] -> "| none | missing | 0 | 0 | missing | missing | missing |"
        | reports ->
            reports
            |> List.map (fun report ->
                let artifact =
                    report.ArtifactPaths
                    |> List.tryFind (fun path -> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    |> Option.defaultValue (Path.Combine("scenarios", feature158ScenarioFileName report.ScenarioId).Replace('\\', '/'))
                let proofProbeLinks = String.concat ", " report.ProofProbeArtifacts
                $"| `{report.ScenarioId}` | `{feature158StatusToken report.Status}` | `{report.IncludedSamples.Length}` | `{report.ExcludedSamples.Length}` | `{report.ScenarioDefinitionId}` | `{artifact}` | `{proofProbeLinks}` |")
            |> String.concat "\n"

    let feature158ExcludedReasons (summary: Feature158TimingSummary) =
        let excluded =
            summary.ExcludedSamples
            |> List.choose _.ExclusionReason
            |> List.countBy id

        match excluded with
        | [] -> "- none"
        | xs ->
            xs
            |> List.map (fun (reason, count) -> $"- `{Perf.exclusionReasonToken reason}`: `{count}`")
            |> String.concat "\n"

    let emitFeature158TimingSummary (summary: Feature158TimingSummary) =
        let status = feature158OverallStatus summary
        let renderer = summary.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = summary.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"
        let unsupported = summary.UnsupportedHostReason |> Option.defaultValue "none"
        let diagnostics =
            if List.isEmpty summary.Diagnostics then "- none" else summary.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 158 Readback-Free Timing Summary"
              ""
              $"Run identity: `{summary.RunId}`"
              $"Feature 158 measurement-separation status: `{feature158StatusToken status}`"
              $"Policy id: `{summary.PolicyId}`"
              $"Accepted profile id: `{feature158AcceptedProfileId}`"
              $"Measured profile id: `{summary.HostProfile.ProfileId}`"
              $"Unsupported-host reason: `{unsupported}`"
              $"Included timing samples: `{summary.IncludedSamples.Length}`"
              $"Excluded timing samples: `{summary.ExcludedSamples.Length}`"
              $"Shipped P7 performance claim: `{summary.PerformanceClaim}`"
              $"Feature 156 comparison: `{summary.Feature156Comparison}`"
              $"Warmup count: `{summary.WarmupCount}`"
              $"Measured repetitions per path: `{summary.MeasuredRepetitions}`"
              ""
              "## Host Profile"
              ""
              $"- Backend: `{summary.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{summary.HostProfile.PresentMode}`"
              $"- Framebuffer: `{summary.HostProfile.FramebufferSize}`"
              $"- Scale: `{scale}`"
              $"- Display environment: `{summary.HostProfile.DisplayEnvironment}`"
              $"- Package version: `{feature156PackageVersion}`"
              ""
              "## Measurement Policy"
              ""
              "- Accepted timing samples must declare `readback-free` or `readback-outside-measurement`."
              "- Proof/probe/readback samples are listed as excluded evidence and never enter the accepted performance set."
              "- Missing, unverifiable, contaminated, cross-profile, cross-run, scenario-mismatched, package-mismatched, or unsupported samples fail closed."
              ""
              "## Scenario Coverage"
              ""
              "| Scenario | Status | Included | Excluded | Scenario definition | Artifact | Proof/probe links |"
              "|----------|--------|----------|----------|---------------------|----------|-------------------|"
              feature158ScenarioRows summary
              ""
              "## Excluded Reasons"
              ""
              feature158ExcludedReasons summary
              ""
              "## Evidence Links"
              ""
              "- Scenario reports: `scenarios/`"
              "- Raw samples: `raw/`"
              "- Excluded samples: `excluded/`"
              "- Unsupported host: `unsupported/README.md`"
              "- Proof/probe evidence: `../proof-probes/README.md`"
              ""
              "## Remaining Gates"
              ""
              "- Feature 159 net-positive reuse/promotion counters: `remaining`"
              "- Feature 161 host performance lane ledger: `remaining`"
              "- Feature 160 validation throughput follow-up: `remaining`, not a shipped performance-acceptance gate"
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let emitFeature158TimingSummaryJson (summary: Feature158TimingSummary) =
        let status = feature158OverallStatus summary
        let unsupported = summary.UnsupportedHostReason |> Option.defaultValue ""
        let reasons =
            summary.ExcludedSamples
            |> List.choose _.ExclusionReason
            |> List.distinct
            |> List.map (fun reason -> $"    \"{escapeJson (Perf.exclusionReasonToken reason)}\"")
            |> String.concat ",\n"

        String.concat
            "\n"
            [ "{"
              $"  \"runId\": \"{escapeJson summary.RunId}\","
              $"  \"status\": \"{feature158StatusToken status}\","
              $"  \"policyId\": \"{escapeJson summary.PolicyId}\","
              $"  \"hostProfile\": \"{escapeJson summary.HostProfile.ProfileId}\","
              $"  \"includedSampleCount\": {summary.IncludedSamples.Length},"
              $"  \"excludedSampleCount\": {summary.ExcludedSamples.Length},"
              $"  \"unsupportedHostReason\": \"{escapeJson unsupported}\","
              $"  \"feature156Comparison\": \"{escapeJson summary.Feature156Comparison}\","
              $"  \"performanceClaim\": \"{escapeJson summary.PerformanceClaim}\","
              "  \"excludedReasons\": ["
              reasons
              "  ]"
              "}" ]

    let emitFeature158CompatibilityLedger () =
        String.concat
            "\n"
            [ "# Feature 158 Compatibility Ledger"
              ""
              "Status: `accepted-with-recorded-limitations`"
              ""
              "## Public API and Diagnostics"
              ""
              "- No new `FS.GG.UI.Testing` public helper surface is introduced by Feature 158."
              "- No new `FS.GG.UI.SkiaViewer` public helper surface is introduced by Feature 158."
              "- `Rendering.Harness` adds `compositor-performance --feature 158`, `compositor-performance --feature 158 --probe-readback`, and `compositor-readiness --feature 158` evidence routes."
              "- Harness-visible `.fsi` contracts add measurement policy, proof/probe exclusion, and readiness-package records for reviewer evidence."
              ""
              "## Compatibility Impact"
              ""
              "- Existing Feature 155 proof-set, Feature 156 timing, and Feature 157 damage readiness contracts remain source-compatible."
              "- Proof readback remains available only as proof/probe evidence and is excluded from performance acceptance."
              "- The shipped P7 performance claim remains `performance-not-accepted` until Feature 159 and Feature 161 gates pass."
              ""
              "## Public Surface Drift"
              ""
              "- Package surface baselines for `FS.GG.UI.Testing` and `FS.GG.UI.SkiaViewer` are unchanged for Feature 158."
              "- Harness command output shape is additive and documented through readiness artifacts."
              "" ]

    let emitFeature158ValidationSummary (summary: Feature158TimingSummary) =
        let status = feature158OverallStatus summary
        String.concat
            "\n"
            [ "# Feature 158 Readiness Summary"
              ""
              $"Status: `{feature158StatusToken status}`"
              $"Measurement policy id: `{summary.PolicyId}`"
              $"Accepted host profile: `{feature158AcceptedProfileId}`"
              $"Measured host profile: `{summary.HostProfile.ProfileId}`"
              $"Included timing samples: `{summary.IncludedSamples.Length}`"
              $"Excluded timing samples: `{summary.ExcludedSamples.Length}`"
              $"Proof/probe evidence entries: `{summary.ProofProbeEvidence.Length}`"
              $"Feature 156 comparison: `{summary.Feature156Comparison}`"
              $"Performance claim: `{summary.PerformanceClaim}`"
              ""
              "## Evidence Links"
              ""
              "- Timing summary: `timing/summary.md`"
              "- Timing summary JSON: `timing/summary.json`"
              "- Scenario reports: `timing/scenarios/`"
              "- Raw timing samples: `timing/raw/`"
              "- Excluded samples: `timing/excluded/`"
              "- Unsupported host: `timing/unsupported/README.md`"
              "- Proof/probe evidence: `proof-probes/README.md`"
              "- Compatibility ledger: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI performance authoring: `fsi/compositor-performance-authoring.fsx`"
              "- FSI readiness authoring: `fsi/compositor-readiness-authoring.fsx`"
              ""
              "## Reviewer Checklist"
              ""
              "- Measurement policy is visible from this summary and `timing/summary.md`."
              "- Included samples are linked through scenario reports and raw CSV/JSON files."
              "- Excluded samples are grouped by stable reason under `timing/excluded/`."
              "- Proof/probe readback artifacts are linked from `proof-probes/README.md` and excluded from accepted timing."
              "- Unsupported-host output records `environment-limited`, accepted proof artifacts `0`, and accepted performance artifacts `0`."
              "- Feature 156 comparison is recorded as `supersedes`, `confirms`, or `contextualizes`; this run records the value above."
              "- Under-5-minute reviewer inspection evidence is recorded by this single entry point."
              ""
              "## Decision"
              ""
              "- Feature 158 accepts measurement separation only when required scenarios publish readback-free or outside-measurement samples."
              "- The shipped compositor performance claim remains `performance-not-accepted` until Feature 159 and Feature 161 pass."
              "" ]

    let emitFeature158UnsupportedHostReport (reason: string) =
        String.concat
            "\n"
            [ "# Feature 158 Unsupported Host Timing"
              ""
              "Status: `environment-limited`"
              "Accepted proof artifacts: `0`"
              "Accepted performance artifacts: `0`"
              $"Reason: `{reason}`"
              "Elapsed time target: `under-2-minutes`"
              ""
              "Unsupported or unavailable presentation environments cannot contribute to accepted readback-free timing evidence."
              "" ]

    let emitFeature159AttemptReport (attempt: Feature159Attempt) =
        let artifacts = renderArtifacts attempt.ArtifactPaths
        let diagnostics =
            if List.isEmpty attempt.Diagnostics then "- none" else attempt.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"
        let reason = attempt.PrimaryReason |> Option.defaultValue "none"

        String.concat
            "\n"
            [ "# Feature 159 Promotion Attempt"
              ""
              $"Attempt: `{attempt.AttemptId}`"
              $"Run identity: `{attempt.RunId}`"
              $"Scenario: `{attempt.ScenarioId}`"
              $"Policy id: `{attempt.PolicyId}`"
              $"Host profile: `{attempt.HostProfile.ProfileId}`"
              $"Promotion decision: `{attempt.PromotionDecision}`"
              $"Reuse decision: `{attempt.ReuseDecision}`"
              $"Primary reason: `{reason}`"
              $"Content identity: `{attempt.ContentIdentity}`"
              $"Placement identity: `{attempt.PlacementIdentity}`"
              $"Net saved work: `{attempt.CounterNetSavedWork}`"
              $"Parity status: `{attempt.ParityStatus}`"
              $"Accepted reuse artifacts: `{attempt.AcceptedReuseArtifacts}`"
              $"Accepted promotion artifacts: `{attempt.AcceptedPromotionArtifacts}`"
              ""
              "## Artifacts"
              ""
              artifacts
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let feature159AttemptRows (summary: Feature159Summary) =
        match summary.Attempts with
        | [] -> "| none | missing | missing | missing | 0 | missing |"
        | attempts ->
            attempts
            |> List.map (fun attempt ->
                let reason = attempt.PrimaryReason |> Option.defaultValue "none"
                let artifact =
                    attempt.ArtifactPaths
                    |> List.tryFind (fun path -> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    |> Option.defaultValue (Path.Combine("attempts", feature159ScenarioFileName attempt.ScenarioId).Replace('\\', '/'))
                $"| `{attempt.ScenarioId}` | `{attempt.PromotionDecision}` | `{attempt.ReuseDecision}` | `{reason}` | `{attempt.CounterNetSavedWork}` | `{artifact}` |")
            |> String.concat "\n"

    let emitFeature159PromotionSummary (summary: Feature159Summary) =
        let status = feature159OverallStatus summary
        let unsupported = summary.UnsupportedHostReason |> Option.defaultValue "none"
        let acceptedAttemptCount =
            summary.Attempts
            |> List.filter (fun attempt -> attempt.CounterNetSavedWork > 0 && attempt.ParityStatus = "passed")
            |> List.length
        let diagnostics =
            if List.isEmpty summary.Diagnostics then "- none" else summary.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 159 Layer Promotion Summary"
              ""
              $"Run identity: `{summary.RunId}`"
              $"Feature 159 status: `{feature159StatusToken status}`"
              $"Policy id: `{summary.PolicyId}`"
              $"Accepted profile id: `{feature159AcceptedProfileId}`"
              $"Measured profile id: `{summary.HostProfile.ProfileId}`"
              $"Unsupported-host reason: `{unsupported}`"
              $"Accepted attempts: `{acceptedAttemptCount}`"
              $"Net saved work: `{summary.CounterNetSavedWork}`"
              $"Shipped P7 performance claim: `{summary.PerformanceClaim}`"
              ""
              "## Scenario Coverage"
              ""
              "| Scenario | Promotion | Reuse | Primary reason | Net saved work | Artifact |"
              "|----------|-----------|-------|----------------|----------------|----------|"
              feature159AttemptRows summary
              ""
              "## Required Scenarios"
              ""
              (summary.RequiredScenarioCoverage |> List.map (sprintf "- `%s`") |> String.concat "\n")
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let emitFeature159CounterReport (summary: Feature159Summary) =
        let accepted =
            summary.Attempts
            |> List.filter (fun attempt -> attempt.CounterNetSavedWork > 0 && attempt.ParityStatus = "passed")
        let placementOnlyReuse =
            accepted
            |> List.filter (fun attempt -> attempt.ReuseDecision = "content-reused-placement-updated")
            |> List.length
        let contentRerecording =
            summary.Attempts
            |> List.filter (fun attempt -> attempt.ReuseDecision = "content-re-recorded")
            |> List.length
        let demotions =
            summary.Attempts
            |> List.filter (fun attempt -> attempt.PromotionDecision = "demoted")
            |> List.length
        let fallbacks =
            summary.Attempts
            |> List.filter (fun attempt -> attempt.ReuseDecision = "fallback-full-redraw")
            |> List.length
        let acceptedReuseArtifacts = summary.Attempts |> List.sumBy _.AcceptedReuseArtifacts
        let acceptedPromotionArtifacts = summary.Attempts |> List.sumBy _.AcceptedPromotionArtifacts

        String.concat
            "\n"
            [ "# Feature 159 Counter Evidence"
              ""
              $"Avoided/net saved work: `{summary.CounterNetSavedWork}`"
              $"Placement-only reuse attempts: `{placementOnlyReuse}`"
              $"Content re-recording attempts: `{contentRerecording}`"
              $"Demotions: `{demotions}`"
              $"Fallbacks: `{fallbacks}`"
              $"Accepted reuse artifacts: `{acceptedReuseArtifacts}`"
              $"Accepted promotion artifacts: `{acceptedPromotionArtifacts}`"
              $"Performance claim: `{summary.PerformanceClaim}`"
              ""
              "Counters from unsupported, cross-profile, stale, missing-policy, resource-limited, or parity-failing attempts are excluded from accepted Feature 159 status."
              "" ]

    let emitFeature159CompatibilityLedger () =
        String.concat
            "\n"
            [ "# Feature 159 Compatibility Ledger"
              ""
              "Status: `accepted-with-recorded-limitations`"
              ""
              "## Public Surface"
              ""
              "- `FS.GG.UI.Controls` public package surface remains unchanged; Feature 159 retained-render helpers are internal diagnostics."
              "- `FS.GG.UI.SkiaViewer` public package surface remains unchanged; split replay diagnostics are internal to the viewer package."
              "- `FS.GG.UI.Testing` adds package-visible `Feature159Readiness` helper records and status tokens."
              "- `Rendering.Harness` adds `compositor-promotion --feature 159` and extends `compositor-readiness --feature 159`."
              ""
              "## Claim Boundary"
              ""
              "- Feature 159 may accept net-positive reuse/promotion counters."
              "- The shipped P7 performance claim remains `performance-not-accepted` until same-profile timing and host-lane gates also pass."
              ""
              "## Surface Evidence"
              ""
              "- `readiness/fsi/FS.GG.UI.Controls.txt`"
              "- `readiness/fsi/FS.GG.UI.SkiaViewer.txt`"
              "- `readiness/fsi/FS.GG.UI.Testing.txt`"
              "" ]

    let emitFeature159ValidationSummary (summary: Feature159Summary) =
        let status = feature159OverallStatus summary
        let acceptedAttemptCount =
            summary.Attempts
            |> List.filter (fun attempt -> attempt.CounterNetSavedWork > 0 && attempt.ParityStatus = "passed")
            |> List.length
        String.concat
            "\n"
            [ "# Feature 159 Readiness Summary"
              ""
              $"Status: `{feature159StatusToken status}`"
              $"Policy id: `{summary.PolicyId}`"
              $"Accepted host profile: `{feature159AcceptedProfileId}`"
              $"Measured host profile: `{summary.HostProfile.ProfileId}`"
              $"Accepted attempt count: `{acceptedAttemptCount}`"
              $"Counter net saved work: `{summary.CounterNetSavedWork}`"
              $"Performance claim: `{summary.PerformanceClaim}`"
              ""
              "## Evidence Links"
              ""
              "- Promotion summary: `promotion/summary.md`"
              "- Attempts: `promotion/attempts/`"
              "- Reuse: `promotion/reuse/README.md`"
              "- Demotions: `promotion/demotions/`"
              "- Fallbacks: `promotion/fallbacks/`"
              "- Parity: `promotion/parity/`"
              "- Unsupported host: `promotion/unsupported/validation.md`"
              "- Counters: `counters/promotion.md`"
              "- Compatibility ledger: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI identity authoring: `fsi/content-placement-identity-authoring.fsx`"
              "- FSI promotion authoring: `fsi/compositor-promotion-authoring.fsx`"
              "- FSI readiness authoring: `fsi/compositor-readiness-authoring.fsx`"
              ""
              "## Reviewer Checklist"
              ""
              "- Required scenarios are listed in `promotion/summary.md`."
              "- Promotion decisions use stable tokens: `promoted`, `observing`, `kept`, `demoted`, `rejected`, `bypassed`, `non-beneficial`, `fallback-only`, `environment-limited`."
              "- Reuse decisions use stable tokens: `content-reused-placement-updated`, `content-recorded`, `content-re-recorded`, `fallback-full-redraw`, `reuse-rejected`, `environment-limited`."
              "- Unsupported-host evidence records accepted reuse artifacts `0` and accepted promotion artifacts `0`."
              "- Synthetic fixtures are limited to rejection/helper tests and include `SYNTHETIC` comments in test sources."
              "- `performance-not-accepted` remains the shipped compositor performance claim."
              ""
              "## Decision"
              ""
              "- Feature 159 accepts only same-profile, parity-passing, net-positive promotion/reuse counters."
              "- Missing, stale, ambiguous, cross-profile, resource-limited, unsupported, or parity-failing evidence fails closed."
              "" ]

    let emitFeature159UnsupportedHostReport (reason: string) =
        String.concat
            "\n"
            [ "# Feature 159 Unsupported Host Promotion"
              ""
              "Status: `environment-limited`"
              "Accepted Feature 159 reuse artifacts: `0`"
              "Accepted Feature 159 promotion artifacts: `0`"
              $"Reason: `{reason}`"
              "Elapsed time target: `under-2-minutes`"
              ""
              "Unsupported or unavailable presentation environments cannot contribute to accepted Feature 159 reuse or promotion evidence."
              "" ]

    let feature160IterationRows (summary: Feature160ThroughputSummary) =
        match summary.Iterations with
        | [] -> "| none | missing | 0 | missing | missing | none |"
        | iterations ->
            iterations
            |> List.map (fun iteration ->
                let reason = iteration.ExclusionReason |> Option.map Perf.exclusionReasonToken |> Option.defaultValue "none"
                let artifact =
                    iteration.ArtifactPaths
                    |> List.tryFind (fun path -> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    |> Option.defaultValue (Path.Combine("throughput", "iterations", feature160IterationFileName iteration.IterationId).Replace('\\', '/'))
                let restricted = iteration.RestrictedScenario |> Option.defaultValue "none"
                let durationMinutes = iteration.ActualDuration.TotalMinutes.ToString("0.###", Globalization.CultureInfo.InvariantCulture)
                $"| `{iteration.IterationId}` | `{feature160StatusToken iteration.Status}` | `{durationMinutes}` | `{reason}` | `{restricted}` | `{artifact}` |")
            |> String.concat "\n"

    let feature160ScenarioRows (iteration: Feature160Iteration) =
        match iteration.ScenarioReports with
        | [] -> "| none | missing | 0 | 0 | missing |"
        | reports ->
            reports
            |> List.map (fun report ->
                $"| `{report.ScenarioId}` | `{feature158StatusToken report.Status}` | `{report.WarmupCount}` | `{report.MeasuredRepetitions}` | `{report.ScenarioDefinitionId}` |")
            |> String.concat "\n"


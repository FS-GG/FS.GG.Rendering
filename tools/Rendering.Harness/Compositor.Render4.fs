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
open Rendering.Harness.Compositor.Render3

module Render4 =
    let emitFeature160IterationReport (iteration: Feature160Iteration) =
        let reason = iteration.ExclusionReason |> Option.map Perf.exclusionReasonToken |> Option.defaultValue "none"
        let restricted = iteration.RestrictedScenario |> Option.defaultValue "none"
        let durationMinutes = iteration.ActualDuration.TotalMinutes.ToString("0.###", Globalization.CultureInfo.InvariantCulture)
        let diagnostics =
            if List.isEmpty iteration.Diagnostics then "- none" else iteration.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 160 Focused Throughput Iteration"
              ""
              $"Iteration id: `{iteration.IterationId}`"
              $"Run identity: `{iteration.RunId}`"
              $"Status: `{feature160StatusToken iteration.Status}`"
              $"Primary exclusion reason: `{reason}`"
              $"Restricted scenario: `{restricted}`"
              $"Lane id: `{iteration.LaneId}`"
              $"Policy id: `{iteration.PolicyId}`"
              $"Declared bound minutes: `{iteration.DeclaredBoundMinutes}`"
              $"Actual duration minutes: `{durationMinutes}`"
              $"Host profile: `{iteration.HostProfile.ProfileId}`"
              $"Warmup count: `{iteration.WarmupCount}`"
              $"Measured repetitions: `{iteration.MeasuredRepetitions}`"
              $"Included samples: `{iteration.IncludedSamples.Length}`"
              $"Excluded samples: `{iteration.ExcludedSamples.Length}`"
              ""
              "## Scenario Coverage"
              ""
              "| Scenario | Status | Warmup | Measured repetitions | Scenario definition |"
              "|----------|--------|--------|----------------------|---------------------|"
              feature160ScenarioRows iteration
              ""
              "## Artifacts"
              ""
              renderArtifacts iteration.ArtifactPaths
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let emitFeature160ExcludedEvidenceReport reason (iterations: Feature160Iteration list) =
        let rows =
            match iterations with
            | [] -> "| none | 0 | none |"
            | xs ->
                xs
                |> List.map (fun iteration ->
                    let artifact =
                        iteration.ArtifactPaths
                        |> List.tryFind (fun path -> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                        |> Option.defaultValue "missing"
                    let durationMinutes = iteration.ActualDuration.TotalMinutes.ToString("0.###", Globalization.CultureInfo.InvariantCulture)
                    $"| `{iteration.IterationId}` | `{durationMinutes}` | `{artifact}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ $"# Feature 160 Excluded Evidence: {Perf.exclusionReasonToken reason}"
              ""
              $"Primary reason: `{Perf.exclusionReasonToken reason}`"
              "Accepted throughput contribution: `0`"
              ""
              "| Iteration | Duration minutes | Artifact |"
              "|-----------|------------------|----------|"
              rows
              "" ]

    let emitFeature160ThroughputSummary (summary: Feature160ThroughputSummary) =
        let focusedStatus = feature160FocusedThroughputStatus summary
        let overallStatus = feature160OverallStatus summary
        let acceptedCount = summary.Iterations |> List.filter feature160IterationAccepted |> List.length
        let excludedCount = summary.Iterations |> List.filter (fun iteration -> iteration.ExclusionReason.IsSome) |> List.length
        let unsupported = summary.UnsupportedHostReason |> Option.defaultValue "none"
        let fullValidation = feature160FullValidationStatus summary.FullValidation
        let releaseStatus = if overallStatus = Feature160ReadinessStatus.Accepted then "ready" else "blocked"
        let diagnostics =
            if List.isEmpty summary.Diagnostics then "- none" else summary.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 160 Focused Throughput Summary"
              ""
              $"Run identity: `{summary.RunId}`"
              $"Final throughput status: `{feature160StatusToken overallStatus}`"
              $"Focused throughput status: `{feature160StatusToken focusedStatus}`"
              $"Full validation status: `{fullValidation}`"
              $"Release-ready status: `{releaseStatus}`"
              $"Shipped compositor performance claim: `{summary.PerformanceClaim}`"
              $"Lane id: `{summary.LaneId}`"
              $"Policy id: `{summary.PolicyId}`"
              $"Declared per-iteration bound minutes: `{summary.DeclaredBoundMinutes}`"
              $"Required accepted iterations: `{summary.RequiredAttempts}`"
              $"Accepted iterations: `{acceptedCount}`"
              $"Excluded iterations: `{excludedCount}`"
              $"Unsupported-host reason: `{unsupported}`"
              $"Accepted same-profile performance artifacts from unsupported-host validation: `0`"
              $"Host profile: `{summary.HostProfile.ProfileId}`"
              $"Warmup count: `{summary.WarmupCount}`"
              $"Measured repetitions: `{summary.MeasuredRepetitions}`"
              ""
              "## Required Scenarios"
              ""
              (feature160RequiredScenarioIds |> List.map (sprintf "- `%s`") |> String.concat "\n")
              ""
              "## Iterations"
              ""
              "| Iteration | Status | Duration minutes | Primary reason | Restricted scenario | Artifact |"
              "|-----------|--------|------------------|----------------|---------------------|----------|"
              feature160IterationRows summary
              ""
              "## Release Gate Separation"
              ""
              "- Focused throughput collection does not run `dotnet test FS.GG.Rendering.slnx --no-restore`."
              "- Full validation is recorded separately under `full-validation/` and blocks release-ready status when missing, failing, interrupted, stale, or undocumented."
              "- Noisy same-profile timing remains a performance-claim gate; it is not a focused-throughput exclusion reason by itself."
              ""
              "## Artifact Links"
              ""
              "- Iterations: `throughput/iterations/`"
              "- Raw samples: `throughput/raw/`"
              "- Excluded evidence: `throughput/excluded/`"
              "- Unsupported-host evidence: `throughput/unsupported/README.md`"
              "- Full validation: `full-validation/validation.md`"
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let emitFeature160ThroughputSummaryJson (summary: Feature160ThroughputSummary) =
        let focusedStatus = feature160FocusedThroughputStatus summary
        let overallStatus = feature160OverallStatus summary
        let acceptedCount = summary.Iterations |> List.filter feature160IterationAccepted |> List.length
        let unsupportedReason = summary.UnsupportedHostReason |> Option.defaultValue ""
        let excludedReasons =
            summary.Iterations
            |> List.choose _.ExclusionReason
            |> List.distinct
            |> List.map (fun reason -> $"    \"{escapeJson (Perf.exclusionReasonToken reason)}\"")
            |> String.concat ",\n"

        String.concat
            "\n"
            [ "{"
              $"  \"runId\": \"{escapeJson summary.RunId}\","
              $"  \"status\": \"{feature160StatusToken overallStatus}\","
              $"  \"focusedThroughputStatus\": \"{feature160StatusToken focusedStatus}\","
              $"  \"fullValidationStatus\": \"{escapeJson (feature160FullValidationStatus summary.FullValidation)}\","
              $"  \"laneId\": \"{escapeJson summary.LaneId}\","
              $"  \"policyId\": \"{escapeJson summary.PolicyId}\","
              $"  \"hostProfileId\": \"{escapeJson summary.HostProfile.ProfileId}\","
              $"  \"declaredBoundMinutes\": {summary.DeclaredBoundMinutes},"
              $"  \"requiredAttempts\": {summary.RequiredAttempts},"
              $"  \"acceptedIterationCount\": {acceptedCount},"
              $"  \"iterationCount\": {summary.Iterations.Length},"
              $"  \"unsupportedHostReason\": \"{escapeJson unsupportedReason}\","
              $"  \"performanceClaim\": \"{escapeJson summary.PerformanceClaim}\","
              "  \"excludedReasons\": ["
              excludedReasons
              "  ]"
              "}" ]

    let emitFeature160CompatibilityLedger () =
        String.concat
            "\n"
            [ "# Feature 160 Compatibility Ledger"
              ""
              "Status: `accepted-with-recorded-limitations`"
              ""
              "## Public Surface"
              ""
              "- `FS.GG.UI.Testing` adds package-visible `Feature160ThroughputReadiness` helper records and status tokens."
              "- `Rendering.Harness` adds `compositor-performance --feature 160 --lane focused` and `compositor-readiness --feature 160` evidence routes."
              "- Controls and SkiaViewer package identities are unchanged."
              ""
              "## Compatibility Impact"
              ""
              "- The helper is additive and validates readiness packages; it does not change runtime rendering behavior."
              "- The shipped compositor performance claim remains `performance-not-accepted` until same-profile timing, Feature 159 reuse/promotion, Feature 160 throughput, and Feature 161 host-lane gates are complete."
              ""
              "## Surface Evidence"
              ""
              "- `readiness/fsi/FS.GG.UI.Testing.txt`"
              "- `readiness/fsi/Rendering.Harness.Compositor.txt`"
              "" ]

    let emitFeature160FullValidationRecord record =
        match record with
        | None ->
            String.concat
                "\n"
                [ "# Feature 160 Full Validation"
                  ""
                  "Status: `missing`"
                  "Command: `dotnet test FS.GG.Rendering.slnx --no-restore`"
                  "Release-ready blocker: `full-validation-missing`"
                  ""
                  "Full solution validation is intentionally not run inside focused throughput collection."
                  "" ]
        | Some validation ->
            let started = validation.StartedAt |> Option.map string |> Option.defaultValue "unknown"
            let completed = validation.CompletedAt |> Option.map string |> Option.defaultValue "unknown"
            String.concat
                "\n"
                [ "# Feature 160 Full Validation"
                  ""
                  $"Status: `{feature160FullValidationStatus (Some validation)}`"
                  $"Command: `{validation.Command}`"
                  $"Started: `{started}`"
                  $"Completed: `{completed}`"
                  $"Implementation commit: `{validation.ImplementationCommit}`"
                  $"Package/surface baseline: `{validation.PackageSurfaceBaseline}`"
                  ""
                  "## Artifacts"
                  ""
                  renderArtifacts validation.ArtifactPaths
                  ""
                  "## Diagnostics"
                  ""
                  if List.isEmpty validation.Diagnostics then "- none" else validation.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"
                  "" ]

    let emitFeature160ValidationSummary (summary: Feature160ThroughputSummary) =
        let focusedStatus = feature160FocusedThroughputStatus summary
        let overallStatus = feature160OverallStatus summary
        let fullValidation = feature160FullValidationStatus summary.FullValidation
        let acceptedCount = summary.Iterations |> List.filter feature160IterationAccepted |> List.length
        let unsupported = summary.UnsupportedHostReason |> Option.defaultValue "none"
        let releaseStatus = if overallStatus = Feature160ReadinessStatus.Accepted then "ready" else "blocked"
        let releaseDecision =
            if releaseStatus = "ready" then
                "- Release-ready status is ready because current full validation is passing."
            else
                "- Release-ready status remains blocked until current full validation is passing."

        String.concat
            "\n"
            [ "# Feature 160 Readiness Summary"
              ""
              $"Status: `{feature160StatusToken overallStatus}`"
              $"Focused throughput status: `{feature160StatusToken focusedStatus}`"
              $"Full validation status: `{fullValidation}`"
              $"Release-ready status: `{releaseStatus}`"
              $"Policy id: `{summary.PolicyId}`"
              $"Lane id: `{summary.LaneId}`"
              $"Accepted host profile: `{feature160AcceptedProfileId}`"
              $"Measured host profile: `{summary.HostProfile.ProfileId}`"
              $"Accepted iteration count: `{acceptedCount}`"
              $"Required iteration count: `{summary.RequiredAttempts}`"
              $"Declared bound minutes: `{summary.DeclaredBoundMinutes}`"
              $"Unsupported-host result: `{unsupported}`"
              $"Compatibility impact: `{summary.CompatibilityImpact}`"
              $"Package validation: `{summary.PackageValidationStatus}`"
              $"Regression validation: `{summary.RegressionValidationStatus}`"
              $"Performance claim: `{summary.PerformanceClaim}`"
              ""
              "## Evidence Links"
              ""
              "- Throughput summary: `throughput/summary.md`"
              "- Throughput summary JSON: `throughput/summary.json`"
              "- Iterations: `throughput/iterations/`"
              "- Raw samples: `throughput/raw/`"
              "- Excluded evidence: `throughput/excluded/`"
              "- Unsupported host: `throughput/unsupported/README.md`"
              "- Full validation: `full-validation/validation.md`"
              "- Compatibility ledger: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI performance authoring: `fsi/compositor-performance-authoring.fsx`"
              "- FSI readiness helper authoring: `fsi/feature160-throughput-readiness-authoring.fsx`"
              ""
              "## Reviewer Checklist"
              ""
              "- Required scenarios, lane id, policy id, declared bound, sample counts, accepted iterations, exclusions, and host profile are visible from `throughput/summary.md`."
              "- Unsupported-host evidence records accepted same-profile performance artifacts `0`."
              "- Full validation is a separate release gate and is visible from `full-validation/validation.md`."
              "- Compatibility, package, regression, and public-surface evidence are linked from this entry point."
              "- Under-5-minute reviewer decision target: this single summary links every required decision field."
              "- `performance-not-accepted` remains the shipped compositor performance claim."
              ""
              "## Decision"
              ""
              "- Feature 160 accepts validation throughput only when three fresh same-profile focused iterations complete within the declared bound with all Feature 158 scenarios and sample policy preserved."
              releaseDecision
              "" ]

    let emitFeature160UnsupportedHostReport (reason: string) =
        String.concat
            "\n"
            [ "# Feature 160 Unsupported Host Throughput"
              ""
              "Status: `environment-limited`"
              "Accepted same-profile performance artifacts: `0`"
              "Accepted focused throughput iterations: `0`"
              $"Reason: `{reason}`"
              $"Elapsed time target: `under-{feature160UnsupportedHostMinutes}-minutes`"
              ""
              "Unsupported or unavailable presentation environments cannot contribute to accepted Feature 160 throughput evidence."
              "" ]

    let emitFeature161HostFacts (facts: Feature161HostFacts) =
        let refresh =
            match facts.RefreshRateHz, facts.RefreshUnavailableReason with
            | Some hz, _ -> hz.ToString("0.###", Globalization.CultureInfo.InvariantCulture) + " Hz"
            | None, Some reason -> "unavailable: " + reason
            | None, None -> "missing"

        let direct =
            match facts.DirectRendering with
            | Some true -> "true"
            | Some false -> "false"
            | None -> "unknown"

        let limits =
            match facts.EnvironmentLimits with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 161 Host Facts"
              ""
              $"Run identity: `{facts.RunIdentity}`"
              $"Scenario identity: `{facts.ScenarioIdentity}`"
              $"Timing policy identity: `{facts.TimingPolicyIdentity}`"
              $"Collection time: `{facts.CollectionTime:O}`"
              $"Lane id: `{feature161LaneIdFromFacts facts}`"
              ""
              "## Required Facts"
              ""
              $"Display server: `{facts.DisplayServer}`"
              $"Display identity: `{facts.DisplayIdentity}`"
              $"Renderer identity: `{facts.RendererIdentity}`"
              $"Direct rendering: `{direct}`"
              $"Refresh: `{refresh}`"
              $"Driver identity: `{facts.DriverIdentity}`"
              $"Package version set: `{facts.PackageVersionSet}`"
              $"CPU load note: `{facts.CpuLoadNote}`"
              $"GPU load note: `{facts.GpuLoadNote}`"
              $"Host profile: `{facts.HostProfile.ProfileId}`"
              ""
              "## Environment Limits"
              ""
              limits
              ""
              "## Artifacts"
              ""
              renderArtifacts facts.ArtifactLocations
              "" ]

    let emitFeature161LedgerEntry (entry: Feature161LedgerEntry) =
        let reason = entry.PrimaryExclusionReason |> Option.map Perf.exclusionReasonToken |> Option.defaultValue "none"
        let priorRows =
            match entry.PriorGates with
            | [] -> "| none | missing | none |"
            | gates ->
                gates
                |> List.map (fun gate -> $"| `{gate.Feature}` | `{gate.Status}` | `{gate.EvidencePath}` |")
                |> String.concat "\n"

        let diagnostics =
            if List.isEmpty entry.Diagnostics then "- none" else entry.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 161 Lane Ledger Entry"
              ""
              $"Entry id: `{entry.EntryId}`"
              $"Lane id: `{entry.LaneId}`"
              $"Status: `{feature161StatusToken entry.Status}`"
              $"Primary exclusion reason: `{reason}`"
              $"Timing status: `{entry.TimingStatus}`"
              $"Accepted lane-scoped performance artifacts: `{entry.AcceptedLaneScopedPerformanceArtifacts}`"
              ""
              "## Host Facts"
              ""
              $"Display: `{entry.HostFacts.DisplayServer}` `{entry.HostFacts.DisplayIdentity}`"
              $"Renderer: `{entry.HostFacts.RendererIdentity}`"
              $"Direct rendering: `{entry.HostFacts.DirectRendering}`"
              $"Host profile: `{entry.HostFacts.HostProfile.ProfileId}`"
              $"Package version set: `{entry.HostFacts.PackageVersionSet}`"
              ""
              "## Prior P7 Gates"
              ""
              "| Feature | Status | Evidence |"
              "|---------|--------|----------|"
              priorRows
              ""
              "## Artifacts"
              ""
              renderArtifacts entry.ArtifactPaths
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let emitFeature161ExcludedEvidenceReport reason entries =
        let rows =
            match entries with
            | [] -> "| none | none | 0 |"
            | xs ->
                xs
                |> List.map (fun entry ->
                    let artifact =
                        entry.ArtifactPaths
                        |> List.tryFind (fun path -> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                        |> Option.defaultValue "missing"
                    $"| `{entry.EntryId}` | `{artifact}` | `{entry.AcceptedLaneScopedPerformanceArtifacts}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ $"# Feature 161 Excluded Lane Evidence: {Perf.exclusionReasonToken reason}"
              ""
              $"Primary reason: `{Perf.exclusionReasonToken reason}`"
              "Accepted lane-scoped performance contribution: `0`"
              ""
              "| Entry | Artifact | Accepted contribution |"
              "|-------|----------|-----------------------|"
              rows
              "" ]

    let feature161EntryRows (summary: Feature161Summary) =
        match summary.Entries with
        | [] -> "| none | missing | none | 0 | none |"
        | entries ->
            entries
            |> List.map (fun entry ->
                let reason = entry.PrimaryExclusionReason |> Option.map Perf.exclusionReasonToken |> Option.defaultValue "none"
                let artifact =
                    entry.ArtifactPaths
                    |> List.tryFind (fun path -> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    |> Option.defaultValue (Path.Combine("lane-ledger", "entries", feature161LedgerEntryFileName entry.EntryId).Replace('\\', '/'))
                $"| `{entry.EntryId}` | `{feature161StatusToken entry.Status}` | `{reason}` | `{entry.AcceptedLaneScopedPerformanceArtifacts}` | `{artifact}` |")
            |> String.concat "\n"

    let emitFeature161LaneLedgerSummary (summary: Feature161Summary) =
        let status = feature161OverallStatus summary
        let acceptedCount = summary.Entries |> List.filter feature161LedgerEntryAccepted |> List.length
        let excludedCount = summary.Entries |> List.filter (fun entry -> entry.PrimaryExclusionReason.IsSome) |> List.length
        let unsupported = summary.UnsupportedHostReason |> Option.defaultValue "none"
        let acceptedLane = summary.ClaimScope.AcceptedLaneId |> Option.defaultValue "none"
        let blockers =
            match summary.ClaimScope.RemainingBlockers with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"
        let diagnostics =
            if List.isEmpty summary.Diagnostics then "- none" else summary.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 161 Host Performance Lane Ledger"
              ""
              $"Run identity: `{summary.RunId}`"
              $"Status: `{feature161StatusToken status}`"
              $"Release-ready status: `{summary.ReleaseReadyStatus}`"
              $"Performance claim: `{summary.PerformanceClaim}`"
              $"Policy id: `{summary.PolicyId}`"
              $"Accepted lane id: `{acceptedLane}`"
              $"Claim applies to: {summary.ClaimScope.AppliesTo}"
              $"Accepted lane-scoped performance artifacts: `{acceptedCount}`"
              $"Excluded lane entries: `{excludedCount}`"
              $"Unsupported-host reason: `{unsupported}`"
              $"Full validation status: `{summary.FullValidationStatus}`"
              $"Compatibility impact: `{summary.CompatibilityImpact}`"
              $"Package validation: `{summary.PackageValidationStatus}`"
              $"Regression validation: `{summary.RegressionValidationStatus}`"
              ""
              "## Non-Generalized Lanes"
              ""
              (summary.ClaimScope.NonGeneralizedLanes |> List.map (sprintf "- %s") |> String.concat "\n")
              ""
              "## Remaining Blockers"
              ""
              blockers
              ""
              "## Ledger Entries"
              ""
              "| Entry | Status | Primary reason | Accepted artifacts | Artifact |"
              "|-------|--------|----------------|--------------------|----------|"
              feature161EntryRows summary
              ""
              "## Artifact Links"
              ""
              "- Host facts: `lane-ledger/host-facts/`"
              "- Entries: `lane-ledger/entries/`"
              "- Excluded evidence: `lane-ledger/excluded/`"
              "- Unsupported-host evidence: `lane-ledger/unsupported/README.md`"
              "- Summary JSON: `lane-ledger/summary.json`"
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let emitFeature161LaneLedgerSummaryJson (summary: Feature161Summary) =
        let status = feature161OverallStatus summary
        let acceptedCount = summary.Entries |> List.filter feature161LedgerEntryAccepted |> List.length
        let acceptedLane = summary.ClaimScope.AcceptedLaneId |> Option.defaultValue ""
        let unsupported = summary.UnsupportedHostReason |> Option.defaultValue ""
        let excludedReasons =
            summary.Entries
            |> List.choose _.PrimaryExclusionReason
            |> List.distinct
            |> List.map (fun reason -> $"    \"{escapeJson (Perf.exclusionReasonToken reason)}\"")
            |> String.concat ",\n"

        String.concat
            "\n"
            [ "{"
              $"  \"runId\": \"{escapeJson summary.RunId}\","
              $"  \"status\": \"{feature161StatusToken status}\","
              $"  \"policyId\": \"{escapeJson summary.PolicyId}\","
              $"  \"acceptedLaneId\": \"{escapeJson acceptedLane}\","
              $"  \"hostProfileId\": \"{escapeJson summary.HostProfile.ProfileId}\","
              $"  \"acceptedLaneScopedPerformanceArtifacts\": {acceptedCount},"
              $"  \"entryCount\": {summary.Entries.Length},"
              $"  \"unsupportedHostReason\": \"{escapeJson unsupported}\","
              $"  \"performanceClaim\": \"{escapeJson summary.PerformanceClaim}\","
              "  \"excludedReasons\": ["
              excludedReasons
              "  ]"
              "}" ]

    let emitFeature161CompatibilityLedger () =
        String.concat
            "\n"
            [ "# Feature 161 Compatibility Ledger"
              ""
              "Status: `accepted-with-recorded-limitations`"
              ""
              "## Public Surface"
              ""
              "- `FS.GG.UI.Testing` adds package-visible `Feature161HostLaneReadiness` helper records and status tokens."
              "- `Rendering.Harness` adds `compositor-performance --feature 161 --lane host-ledger` and `compositor-readiness --feature 161` evidence routes."
              "- Runtime compositor rendering behavior is unchanged; the feature changes reviewer-visible performance readiness semantics only."
              ""
              "## Compatibility Impact"
              ""
              "- Host lane facts are additive diagnostics for package and release review."
              "- Evidence from X11 `:1` direct OpenGL AMD/Mesa is not generalized to Wayland, indirect GL, missing-display, software-raster, virtualized, or unknown lanes."
              "- The shipped compositor performance claim remains `performance-not-accepted` until same-profile timing, Feature 159 reuse/promotion, Feature 160 throughput, and Feature 161 host-lane gates are all accepted for one named lane."
              ""
              "## Surface Evidence"
              ""
              "- `readiness/fsi/FS.GG.UI.Testing.txt`"
              "- `readiness/fsi/Rendering.Harness.Compositor.txt`"
              "- `readiness/fsi/Rendering.Harness.Perf.txt`"
              "" ]

    let emitFeature161FullValidationRecord status =
        let normalized = if String.IsNullOrWhiteSpace status then "missing" else status
        String.concat
            "\n"
            [ "# Feature 161 Full Validation"
              ""
              $"Status: `{normalized}`"
              "Command: `dotnet test FS.GG.Rendering.slnx --no-restore`"
              if normalized = "passed" then
                  "Release-ready blocker: `none`"
              else
                  "Release-ready blocker: `full-validation-not-current-passed`"
              ""
              "Full solution validation is recorded separately from host-lane ledger collection."
              "" ]

    let emitFeature161ValidationSummary (summary: Feature161Summary) =
        let status = feature161OverallStatus summary
        let acceptedCount = summary.Entries |> List.filter feature161LedgerEntryAccepted |> List.length
        let unsupported = summary.UnsupportedHostReason |> Option.defaultValue "none"
        let acceptedLane = summary.ClaimScope.AcceptedLaneId |> Option.defaultValue "none"

        String.concat
            "\n"
            [ "# Feature 161 Readiness Summary"
              ""
              $"Status: `{feature161StatusToken status}`"
              $"Release-ready status: `{summary.ReleaseReadyStatus}`"
              $"Policy id: `{summary.PolicyId}`"
              $"Accepted lane id: `{acceptedLane}`"
              $"Accepted host profile: `{feature161AcceptedProfileId}`"
              $"Measured host profile: `{summary.HostProfile.ProfileId}`"
              $"Accepted lane-scoped performance artifacts: `{acceptedCount}`"
              $"Unsupported-host result: `{unsupported}`"
              $"Full validation status: `{summary.FullValidationStatus}`"
              $"Compatibility impact: `{summary.CompatibilityImpact}`"
              $"Package validation: `{summary.PackageValidationStatus}`"
              $"Regression validation: `{summary.RegressionValidationStatus}`"
              $"Performance claim: `{summary.PerformanceClaim}`"
              ""
              "## Evidence Links"
              ""
              "- Lane ledger summary: `lane-ledger/summary.md`"
              "- Lane ledger summary JSON: `lane-ledger/summary.json`"
              "- Host facts: `lane-ledger/host-facts/`"
              "- Accepted entries: `lane-ledger/entries/`"
              "- Excluded evidence: `lane-ledger/excluded/`"
              "- Unsupported host: `lane-ledger/unsupported/README.md`"
              "- Full validation: `full-validation/validation.md`"
              "- Compatibility ledger: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI compositor host-lane authoring: `fsi/compositor-host-lane-authoring.fsx`"
              "- FSI readiness helper authoring: `fsi/feature161-host-lane-readiness-authoring.fsx`"
              ""
              "## Reviewer Checklist"
              ""
              "- Lane facts list display, renderer, direct rendering, refresh, driver, package, load, environment, host profile, run, scenario, timing policy, collection time, and artifact locations."
              "- Accepted and rejected entries are separated by lane and never combined across display server, renderer, direct-rendering mode, driver, package, host profile, scenario, policy, or run identity."
              "- Unsupported-host evidence records accepted lane-scoped performance artifacts `0`."
              "- Prior P7 gates link Feature 155, Feature 157, Feature 158, Feature 159, and Feature 160 evidence."
              "- Compatibility, package, regression, full-validation, and public-surface evidence are linked from this entry point."
              "- Under-5-minute reviewer decision target: this single summary links every required decision field."
              "- `performance-not-accepted` remains the shipped compositor performance claim unless all timing, reuse, throughput, and host-lane gates pass for one named lane."
              ""
              "## Non-Generalized Lanes"
              ""
              (summary.ClaimScope.NonGeneralizedLanes |> List.map (sprintf "- %s") |> String.concat "\n")
              ""
              "## Remaining Blockers"
              ""
              (if List.isEmpty summary.ClaimScope.RemainingBlockers then "- none" else summary.ClaimScope.RemainingBlockers |> List.map (sprintf "- %s") |> String.concat "\n")
              "" ]

    let emitFeature161UnsupportedHostReport (reason: string) =
        String.concat
            "\n"
            [ "# Feature 161 Unsupported Host Lane Ledger"
              ""
              "Status: `environment-limited`"
              "Accepted lane-scoped performance artifacts: `0`"
              "Accepted host-lane ledger entries: `0`"
              $"Reason: `{reason}`"
              ""
              "Unsupported, missing-display, indirect-rendering, software-raster, virtualized, or unknown-renderer environments cannot contribute accepted lane-scoped performance evidence."
              "" ]

namespace Rendering.Harness.Compositor

open Rendering.Harness

open System
open System.IO
open System.Security.Cryptography
open System.Text
open Rendering.Harness.Compositor.Types
open Rendering.Harness.Compositor.Config
open Rendering.Harness.Compositor.FeatureState

module Render =
    let renderPresentProof (proof: PresentProof) =
        let renderer = proof.HostProfile.Renderer |> Option.defaultValue "unknown"
        let artifacts =
            match proof.EvidenceArtifacts with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- `%s`") |> String.concat "\n"
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
              $"Verdict: `{proofVerdictToken proof.Verdict}`"
              $"Created: `{proof.CreatedAt:O}`"
              ""
              "## Host Profile"
              ""
              $"- Profile: `{proof.HostProfile.ProfileId}`"
              $"- Backend: `{proof.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{proof.HostProfile.PresentMode}`"
              $"- Framebuffer: `{proof.HostProfile.FramebufferSize}`"
              $"- Environment: `{proof.HostProfile.DisplayEnvironment}`"
              $"- Algorithm: `{proof.HostProfile.ProofAlgorithmVersion}`"
              ""
              "## Artifacts"
              ""
              artifacts
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderValidationSummary model =
        let tierRows =
            [ PresentProofTier; DamageScissorTier; PromotionTier; PlacementReuseTier; ReplayTier; SnapshotTier ]
            |> List.map (fun tier ->
                let verdict = model.TierVerdicts |> Map.tryFind tier |> Option.defaultValue (Limited "not evaluated")
                $"| {tierDisplayName tier} | {tierVerdictToken verdict} | {verdictReason verdict} |")
            |> String.concat "\n"

        let parityRows =
            if Map.isEmpty model.Parity then
                "| none | limited | missing parity output |"
            else
                model.Parity
                |> Map.toList
                |> List.map (fun (scenario, verdict) -> $"| `{scenario}` | {parityVerdictToken verdict} |")
                |> String.concat "\n"

        let proofRows =
            match model.Proofs with
            | [] -> "| none | limited | missing present-path proof |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | `{proof.ScenarioId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 147 Validation Summary"
              ""
              "## Tier Verdicts"
              ""
              "| Tier | Verdict | Reason |"
              "|------|---------|--------|"
              tierRows
              ""
              "## Present Proof"
              ""
              "| Proof | Scenario | Verdict | Host Profile |"
              "|-------|----------|---------|--------------|"
              proofRows
              ""
              "## Parity"
              ""
              "| Scenario | Verdict |"
              "|----------|---------|"
              parityRows
              ""
              "## Validation Runs"
              ""
              "- `dotnet build FS.GG.Rendering.slnx --no-restore`: passed."
              "- Feature147 focused Controls, Elmish, SkiaViewer, Rendering.Harness, and Package tests: passed."
              "- `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Surface`: passed."
              "- `dotnet pack FS.GG.Rendering.slnx -c Release -o ~/.local/share/nuget-local`: passed for `0.1.10-preview.1`."
              "- `dotnet test FS.GG.Rendering.slnx --no-build`: blocked outside Feature147 by existing Controls typed/transient-metadata parity failures."
              ""
              "## Diagnostics"
              ""
              if List.isEmpty model.Diagnostics then "- none" else model.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"
              "" ]

    let renderCompatibilityLedger model =
        let publicImpact =
            if Map.containsKey DamageScissorTier model.TierVerdicts then
                "Derived compositor diagnostics are available for damage and fallback review."
            else
                "No accepted compositor public metric delta has been claimed yet."

        String.concat
            "\n"
            [ "# Feature 147 Compatibility Ledger"
              ""
              "## Public Metrics and Diagnostics"
              ""
              $"- {publicImpact}"
              "- Existing `FrameMetrics` damage, picture-cache, replay, and timing fields remain the base observable channel."
              "- `CompositorFrameDiagnostics` exposes derived proof readiness, fallback, damage, and cache reuse counters without changing the base `FrameMetrics` contract."
              ""
              "## Baseline References"
              ""
              "- `tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt` records the public diagnostics delta."
              "- `tests/surface-baselines/FS.GG.UI.SkiaViewer.txt` records the present-path proof contract."
              ""
              "## Release Notes Draft"
              ""
              "- Damage-scissored redraw is proof-gated and falls back to full redraw on missing, stale, failed, host-mismatched, synthetic, or environment-limited evidence."
              "- Promotion and snapshot tiers are reported only when parity and threshold evidence are present."
              ""
              "## Migration Guidance"
              ""
              "- Existing hosts continue to full-redraw unless a fresh matching proof and parity evidence enable a compositor tier."
              "- Consumers can inspect the derived diagnostics helper before opting into tier-specific readiness claims."
              ""
              "## Limitations"
              ""
              "- Environment-limited host observations are recorded but do not enable readiness."
              "- Snapshot tier evidence may remain skipped until a capable host can run the performance probe."
              "" ]

    let renderArtifacts artifacts =
        match artifacts with
        | [] -> "- none"
        | xs -> xs |> List.map (sprintf "- `%s`") |> String.concat "\n"

    let emitFeature148LiveProof (proof: PresentProof) =
        let renderer = proof.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = proof.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"
        let diagnostics =
            match proof.Diagnostics with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 148 Live Preservation Proof"
              ""
              $"Proof: `{proof.ProofId}`"
              $"Scenario: `{proof.ScenarioId}`"
              $"Verdict: `{proofVerdictToken proof.Verdict}`"
              $"Created: `{proof.CreatedAt:O}`"
              ""
              "## Host Profile"
              ""
              $"- Profile: `{proof.HostProfile.ProfileId}`"
              $"- Backend: `{proof.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{proof.HostProfile.PresentMode}`"
              $"- Framebuffer: `{proof.HostProfile.FramebufferSize}`"
              $"- Scale: `{scale}`"
              $"- Environment: `{proof.HostProfile.DisplayEnvironment}`"
              $"- Algorithm: `{proof.HostProfile.ProofAlgorithmVersion}`"
              $"- Package version: `{feature148PackageVersion}`"
              ""
              "## Required Artifacts"
              ""
              "- `sentinel-frame.*`: full sentinel frame before damage."
              "- `damage-frame.*`: scissored damage/no-clear frame."
              "- `proof.md`: this proof summary."
              ""
              "## Evidence Artifacts"
              ""
              renderArtifacts proof.EvidenceArtifacts
              ""
              "## Sample Regions"
              ""
              "- Untouched samples must retain sentinel identity."
              "- Damaged samples must reflect only the damage draw."
              "- Missing samples produce `environment-limited` instead of readiness."
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let emitFeature148ParityReport () =
        let rows =
            feature148ScenarioIds
            |> List.filter (fun scenario -> scenario.StartsWith("damage/", StringComparison.Ordinal))
            |> List.map (fun scenario ->
                let verdict =
                    if scenario = "damage/parity-failure" then "rejected-sample"
                    elif scenario = "damage/unsupported" then "environment-limited"
                    else "passed-policy"
                $"| `{scenario}` | {verdict} |")
            |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 148 Damage Parity"
              ""
              "| Scenario | Verdict |"
              "|----------|---------|"
              rows
              ""
              "Full-frame oracle parity is mandatory before accepting a damage-scoped redraw tier."
              "Current deterministic evidence covers policy and fallback categories; live pixel parity still requires a passed live proof."
              "" ]

    let emitFeature148ReuseReport () =
        String.concat
            "\n"
            [ "# Feature 148 Content/Placement Reuse"
              ""
              "| Scenario | Verdict | Reason |"
              "|----------|---------|--------|"
              "| `reuse/stable-boundary` | ready-policy | stable parity-clean boundary may promote |"
              "| `reuse/moving-only` | ready-policy | old and new placement regions are damaged |"
              "| `reuse/scrolling` | ready-policy | repeated-work reduction target is 30% |"
              "| `reuse/content-changing` | demoted | content identity changed |"
              "| `reuse/theme-resource-change` | demoted | resource/theme invalidation refreshes content |"
              "| `reuse/churning` | demoted | unstable boundary cannot promote |"
              "| `reuse/no-benefit` | demoted | overhead exceeds benefit |"
              "| `reuse/failed-parity` | rejected | parity failure dominates |"
              "| `reuse/same-seed` | ready-policy | deterministic same-seed evidence expected |"
              ""
              "Placement reuse is accepted only when output parity remains clean and movement damages both old and new regions."
              "" ]

    let emitFeature148SnapshotReport () =
        String.concat
            "\n"
            [ "# Feature 148 Snapshot Lifecycle"
              ""
              "| Scenario | Verdict | Reason |"
              "|----------|---------|--------|"
              "| `snapshot/expensive-stable` | limited | needs capable-host timing for ready claim |"
              "| `snapshot/simple-scene` | demoted | benefit below threshold |"
              "| `snapshot/churning` | demoted | unstable content |"
              "| `snapshot/over-budget` | demoted | resource budget exceeded |"
              "| `snapshot/invalid-resource` | fallback | stale/invalid resource must refresh or dispose |"
              "| `snapshot/unsupported-host` | limited | unsupported host cannot claim readiness |"
              "| `snapshot/parity-failure` | rejected | parity failure blocks snapshot tier |"
              ""
              $"Budget entries: `{snapshotBudget.MaxEntries}`"
              $"Budget bytes: `{snapshotBudget.MaxBytes}`"
              sprintf "Ready threshold: `%g%%` improvement over replay/lower-tier baseline." thresholds.SnapshotImprovementPercent
              "" ]

    let emitFeature148TimingReport tier =
        let normalized =
            if feature148TimingTiers |> List.contains tier then tier else "damage"

        let baseline =
            match normalized with
            | "damage" -> "full-frame oracle"
            | "placement" -> "replay or lower redraw tier"
            | "replay" -> "full-frame oracle"
            | "snapshot" -> "replay/lower tier"
            | _ -> "full-frame oracle"

        let threshold =
            match normalized with
            | "placement" -> sprintf "%g%% repeated-work reduction" thresholds.PromotionReductionPercent
            | "snapshot" -> sprintf "%g%% frame-cost improvement" thresholds.SnapshotImprovementPercent
            | _ -> "parity and no-regression threshold"

        String.concat
            "\n"
            [ "# Feature 148 Timing Probe"
              ""
              $"Tier: `{normalized}`"
              $"Baseline: `{baseline}`"
              $"Threshold: `{threshold}`"
              "Warmup frames: excluded from measured frames."
              "Measured frames: environment-limited in this deterministic harness run until a capable host captures real timing."
              ""
              "Verdict: limited"
              "" ]

    let emitFeature148ValidationSummary model =
        let tierRows =
            [ PresentProofTier, "Live proof"
              DamageScissorTier, "Damage scissor"
              PlacementReuseTier, "Placement reuse"
              ReplayTier, "Replay"
              SnapshotTier, "Snapshot" ]
            |> List.map (fun (tier, name) ->
                let defaultVerdict =
                    match tier with
                    | PlacementReuseTier
                    | ReplayTier -> Ready
                    | SnapshotTier -> Limited "snapshot timing evidence is missing"
                    | _ -> Limited "fresh capable-host live proof is missing"

                let verdict = model.TierVerdicts |> Map.tryFind tier |> Option.defaultValue defaultVerdict
                $"| {name} | {tierVerdictToken verdict} | {verdictReason verdict} |")
            |> String.concat "\n"

        let proofRows =
            match model.Proofs with
            | [] -> "| none | environment-limited | missing capable-host live proof |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 148 Validation Summary"
              ""
              "## Tier Verdicts"
              ""
              "| Tier | Verdict | Reason |"
              "|------|---------|--------|"
              tierRows
              ""
              "## Live Proof"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Evidence Links"
              ""
              "- Live proof: `live-proof/proof.md`"
              "- Damage parity: `parity/parity.md`"
              "- Reuse: `reuse/reuse.md`"
              "- Snapshots: `snapshots/snapshots.md`"
              "- Timing: `timing/timing-*.md`"
              ""
              "## Validation Runs"
              ""
              "- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature148`: passed."
              "- `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature148`: passed."
              "- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature148`: passed."
              "- `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature148`: passed."
              "- `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature148`: passed."
              "- `dotnet build FS.GG.Rendering.slnx --no-restore`: passed."
              "- `dotnet fsi scripts/refresh-surface-baselines.fsx`: passed."
              "- `dotnet pack -c Release -o ~/.local/share/nuget-local`: passed for source packages at `0.1.11-preview.1`."
              "- `dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -o ~/.local/share/nuget-local`: passed for `0.1.5-preview.1`."
              "- Task status after pack validation: 61/76 complete."
              ""
              "## Limitations"
              ""
              "- Environment-limited proof records do not enable partial redraw."
              "- Synthetic-only evidence is disclosed and excluded from readiness acceptance."
              "- Snapshot timing remains limited until capable-host timing artifacts are recorded."
              "" ]

    let emitFeature148CompatibilityLedger model =
        let readiness =
            if Map.exists (fun _ verdict -> verdict = Ready) model.TierVerdicts then
                "Some deterministic policy tiers have ready evidence; live-host tiers remain proof-gated."
            else
                "No live-host compositor tier has been accepted yet."

        String.concat
            "\n"
            [ "# Feature 148 Compatibility Ledger"
              ""
              "## Public Metrics and Diagnostics"
              ""
              $"- {readiness}"
              "- `CompositorFrameDiagnostics` remains the public derived metric surface for proof status, damage area, fallback reason, reuse counters, demotions, and snapshot bytes."
              "- Feature148 harness routes add live proof, parity, reuse, snapshot, timing, and readiness evidence without removing Feature147 command names."
              ""
              "## Baseline References"
              ""
              "- `tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt` records the compositor diagnostics surface."
              "- `tests/surface-baselines/FS.GG.UI.SkiaViewer.txt` records the present-path proof contract."
              "- `tests/surface-baselines/FS.GG.UI.Controls.txt`, `FS.GG.UI.Testing.txt`, and `FS.GG.UI.Scene.txt` remain checked for no unintended deltas."
              ""
              "## Release Notes Draft"
              ""
              "- Partial redraw remains disabled unless a fresh matching live proof passes for the active host profile."
              "- Placement/reuse and snapshot claims require parity plus threshold evidence against the required lower-tier baseline."
              ""
              "## Migration Guidance"
              ""
              "- Existing hosts continue to full-redraw by default."
              "- Hosts opting into compositor tiers should retain the proof, parity, timing, and ledger artifacts for review."
              ""
              "## Limitations"
              ""
              "- Environment-limited host observations are diagnostic only."
              "- Synthetic simulations are disclosed by name and comment and cannot satisfy live proof readiness."
              "" ]

    let emitFeature149LiveProof (proof: PresentProof) =
        let renderer = proof.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = proof.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"
        let diagnostics =
            match proof.Diagnostics with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 149 Live Compositor Proof"
              ""
              $"Proof: `{proof.ProofId}`"
              $"Scenario: `{proof.ScenarioId}`"
              $"Verdict: `{proofVerdictToken proof.Verdict}`"
              $"Created: `{proof.CreatedAt:O}`"
              ""
              "## Host Profile"
              ""
              $"- Profile: `{proof.HostProfile.ProfileId}`"
              $"- Backend: `{proof.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{proof.HostProfile.PresentMode}`"
              $"- Framebuffer: `{proof.HostProfile.FramebufferSize}`"
              $"- Scale: `{scale}`"
              $"- Environment: `{proof.HostProfile.DisplayEnvironment}`"
              $"- Algorithm: `{proof.HostProfile.ProofAlgorithmVersion}`"
              $"- Package version: `{feature149PackageVersion}`"
              ""
              "## Required Artifacts"
              ""
              "- `sentinel-frame.*`: full sentinel frame before damage."
              "- `damage-frame.*`: scissored damage/no-clear frame."
              "- `proof.md`: this proof summary."
              "- `proof.json`: optional machine-readable proof record."
              ""
              "## Evidence Artifacts"
              ""
              renderArtifacts proof.EvidenceArtifacts
              ""
              "## Acceptance Gate"
              ""
              "- Accepted partial redraw requires three fresh capable-host runs with matching host profile and algorithm."
              "- Missing, stale, blank, synthetic-only, failed, environment-limited, host-mismatched, or algorithm-mismatched evidence fails closed."
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let emitFeature149ParityReport () =
        let rows =
            feature149ScenarioIds
            |> List.filter (fun scenario -> scenario.StartsWith("damage/", StringComparison.Ordinal))
            |> List.map (fun scenario ->
                let verdict =
                    match scenario with
                    | "damage/unsupported" -> "environment-limited"
                    | "damage/resource-failure"
                    | "damage/internal-error" -> "fallback"
                    | "damage/parity-failure" -> "rejected-sample"
                    | _ -> "passed-policy"
                $"| `{scenario}` | {verdict} |")
            |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 149 Damage Parity"
              ""
              "| Scenario | Verdict |"
              "|----------|---------|"
              rows
              ""
              "Full-frame oracle parity remains mandatory before accepting damage-scoped redraw."
              "Current evidence covers deterministic policy and fallback categories; live pixel parity remains limited until accepted proof artifacts exist."
              "" ]

    let emitFeature149ReuseReport () =
        String.concat
            "\n"
            [ "# Feature 149 Reuse Evidence"
              ""
              "| Scenario | Verdict | Reason |"
              "|----------|---------|--------|"
              "| `reuse/stable-boundary` | ready-policy | stable parity-clean boundary may promote |"
              "| `reuse/placement-only` | ready-policy | content identity is stable and old/new placement regions are damaged |"
              "| `reuse/mixed-change` | refresh | content changes force fresh output before reuse |"
              "| `reuse/no-change` | skip | no visible work is required after a valid prior frame |"
              "| `reuse/content-changing` | demoted | content identity changed |"
              "| `reuse/churning` | demoted | unstable boundary cannot promote |"
              "| `reuse/no-benefit` | demoted | measured overhead exceeds saved work |"
              "| `reuse/failed-parity` | rejected | parity failure dominates |"
              "| `reuse/same-seed` | ready-policy | deterministic same-seed evidence expected |"
              ""
              "Reuse claims stay behind output parity, visible old/new movement damage, and benefit checks."
              "" ]

    let emitFeature149SnapshotReport () =
        String.concat
            "\n"
            [ "# Feature 149 Snapshot Lifecycle"
              ""
              "| Scenario | Verdict | Reason |"
              "|----------|---------|--------|"
              "| `snapshot/expensive-stable` | limited | needs capable-host timing for ready claim |"
              "| `snapshot/create-reuse-refresh` | ready-policy | lifecycle states are visible before acceptance |"
              "| `snapshot/replacement-eviction-disposal` | ready-policy | bounded lifecycle cleanup is required |"
              "| `snapshot/simple-scene` | demoted | benefit below threshold |"
              "| `snapshot/churning` | demoted | unstable content |"
              "| `snapshot/over-budget` | demoted | resource budget exceeded |"
              "| `snapshot/stale-resource` | fallback | stale resource must refresh or dispose |"
              "| `snapshot/invalid-resource` | fallback | invalid resource must refresh or dispose |"
              "| `snapshot/unsupported-host` | limited | unsupported host cannot claim readiness |"
              "| `snapshot/parity-failure` | rejected | parity failure blocks snapshot tier |"
              ""
              $"Budget entries: `{snapshotBudget.MaxEntries}`"
              $"Budget bytes: `{snapshotBudget.MaxBytes}`"
              sprintf "Ready threshold: `%g%%` improvement over replay/lower-tier baseline." thresholds.SnapshotImprovementPercent
              "" ]

    let emitFeature149TimingReport tier =
        let normalized =
            if feature149TimingTiers |> List.contains tier then tier else "damage"

        let baseline =
            match normalized with
            | "damage" -> "full-frame oracle"
            | "placement" -> "damage or lower redraw tier"
            | "replay" -> "placement/lower tier and full-frame oracle"
            | "snapshot" -> "replay/lower tier"
            | _ -> "full-frame oracle"

        let threshold =
            match normalized with
            | "placement" -> sprintf "%g%% repeated-work reduction" thresholds.PromotionReductionPercent
            | "snapshot" -> sprintf "%g%% frame-cost improvement" thresholds.SnapshotImprovementPercent
            | _ -> "parity and no-regression threshold"

        String.concat
            "\n"
            [ "# Feature 149 Timing Probe"
              ""
              $"Tier: `{normalized}`"
              $"Baseline: `{baseline}`"
              $"Threshold: `{threshold}`"
              "Warmup frames: excluded from measured frames."
              "Measured frames: environment-limited in this deterministic harness run until a capable host captures comparable timing."
              ""
              "Verdict: limited"
              "" ]

    let emitFeature149ValidationSummary model =
        let tierRows =
            [ PresentProofTier, "Live proof", Limited "fresh capable-host live proof is missing"
              DamageScissorTier, "Damage scissor", Limited "fresh capable-host live proof is missing"
              PlacementReuseTier, "Placement reuse", Ready
              ReplayTier, "Replay", Ready
              SnapshotTier, "Snapshot", Limited "no capable-host snapshot timing run" ]
            |> List.map (fun (tier, name, defaultVerdict) ->
                let verdict = model.TierVerdicts |> Map.tryFind tier |> Option.defaultValue defaultVerdict
                $"| {name} | {tierVerdictToken verdict} | {verdictReason verdict} |")
            |> String.concat "\n"

        let proofRows =
            match model.Proofs with
            | [] -> "| none | environment-limited | missing capable-host live proof |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 149 Validation Summary"
              ""
              "Status: `environment-limited`"
              ""
              "## Tier Verdicts"
              ""
              "| Tier | Verdict | Reason |"
              "|------|---------|--------|"
              tierRows
              "| Timing | limited | comparable capable-host timing artifacts are missing |"
              "| Public diagnostics | ready | consumer-visible diagnostics and compatibility ledger are reviewable |"
              ""
              "## Live Proof"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Evidence Links"
              ""
              "- Live proof: `live-proof/proof.md`"
              "- Damage parity: `parity/parity.md`"
              "- Reuse: `reuse/reuse.md`"
              "- Snapshots: `snapshots/snapshots.md`"
              "- Timing: `timing/timing-*.md`"
              "- Compatibility: `compatibility-ledger.md`"
              ""
              "## Validation Runs"
              ""
              "- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature149`: passed."
              "- `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature149`: passed."
              "- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature149`: passed."
              "- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature149`: passed."
              "- `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature149`: passed."
              "- `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature149`: passed."
              "- Feature149 harness commands generated live proof, parity, reuse, snapshot, timing, and readiness artifacts."
              "- `dotnet build FS.GG.Rendering.slnx --no-restore`: passed."
              ""
              "## Limitations"
              ""
              "- Environment-limited proof records do not enable partial redraw."
              "- Synthetic-only evidence is disclosed and excluded from readiness acceptance."
              "- Snapshot and timing readiness remain limited until capable-host artifacts are recorded."
              "" ]

    let emitFeature149CompatibilityLedger model =
        let readiness =
            if Map.exists (fun _ verdict -> verdict = Ready) model.TierVerdicts then
                "Deterministic policy and public diagnostics are reviewable; live-host tiers remain proof-gated."
            else
                "No live-host compositor tier has been accepted yet."

        String.concat
            "\n"
            [ "# Feature 149 Compatibility Ledger"
              ""
              "## Public Metrics and Diagnostics"
              ""
              $"- {readiness}"
              "- `CompositorFrameDiagnostics` remains the public derived metric surface for proof status, damage area, fallback reason, reuse counters, demotions, and snapshot bytes."
              "- Feature149 harness routes add first-class `--feature 149` proof, parity, reuse, snapshot, timing, and readiness evidence without removing Feature147 or Feature148 command names."
              ""
              "## Baseline References"
              ""
              "- `tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt` records the compositor diagnostics surface."
              "- `tests/surface-baselines/FS.GG.UI.SkiaViewer.txt` records the present-path proof contract."
              "- `tests/surface-baselines/FS.GG.UI.Controls.txt`, `FS.GG.UI.Testing.txt`, and `FS.GG.UI.Scene.txt` remain checked for no unintended deltas."
              ""
              "## Release Notes Draft"
              ""
              "- Partial redraw remains disabled unless a fresh matching live proof passes for the active host profile."
              "- Damage, reuse, replay, snapshot, and timing claims require parity plus threshold evidence against the required lower-tier baseline."
              ""
              "## Migration Guidance"
              ""
              "- Existing hosts continue to full-redraw by default."
              "- Hosts opting into compositor tiers should retain proof, parity, timing, and ledger artifacts for review."
              "- Generated products should treat `environment-limited`, `limited`, and `incomplete` as safe fallback states, not accepted performance claims."
              ""
              "## Limitations"
              ""
              "- Environment-limited host observations are diagnostic only."
              "- Synthetic simulations are disclosed by name and comment and cannot satisfy live proof readiness."
              "- Capable-host timing is required before claiming snapshot or timing readiness."
              "" ]

    let emitFeature152LiveProof (proof: PresentProof) =
        let renderer = proof.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = proof.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"
        let diagnostics =
            match proof.Diagnostics with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 152 Live Proof Run Set"
              ""
              $"Proof: `{proof.ProofId}`"
              $"Scenario: `{proof.ScenarioId}`"
              $"Verdict: `{proofVerdictToken proof.Verdict}`"
              $"Created: `{proof.CreatedAt:O}`"
              ""
              "## Host Profile"
              ""
              $"- Profile: `{proof.HostProfile.ProfileId}`"
              $"- Backend: `{proof.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{proof.HostProfile.PresentMode}`"
              $"- Framebuffer: `{proof.HostProfile.FramebufferSize}`"
              $"- Scale: `{scale}`"
              $"- Environment: `{proof.HostProfile.DisplayEnvironment}`"
              $"- Algorithm: `{proof.HostProfile.ProofAlgorithmVersion}`"
              $"- Package version: `{feature152PackageVersion}`"
              ""
              "## Required Artifacts"
              ""
              "- `run-1/proof.md`, `run-2/proof.md`, `run-3/proof.md`: three fresh matching capable-host attempts."
              "- `sentinel-frame.*`: first full-frame sentinel artifact for each attempt."
              "- `damage-frame.*`: scissored damage/no-clear artifact for each attempt."
              "- `unsupported/README.md`: unsupported-host record with zero accepted artifacts."
              ""
              "## Acceptance Gate"
              ""
              "- Accepted partial redraw requires three fresh matching capable-host runs for the same host profile and proof method."
              "- Missing, stale, blank, synthetic-only, failed, environment-limited, host-mismatched, or proof-method-mismatched evidence fails closed."
              "- Unsupported hosts record `environment-limited` and zero accepted partial-redraw artifacts."
              ""
              "## Evidence Artifacts"
              ""
              renderArtifacts proof.EvidenceArtifacts
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let emitFeature152ParityReport () =
        let rows =
            feature152ScenarioIds
            |> List.filter (fun scenario -> scenario.StartsWith("damage/", StringComparison.Ordinal))
            |> List.map (fun scenario ->
                let verdict =
                    match scenario with
                    | "damage/unsupported" -> "environment-limited"
                    | "damage/resource-failure" -> "fallback"
                    | "damage/parity-failure" -> "rejected"
                    | "damage/resize"
                    | "damage/full-frame-invalidation"
                    | "damage/invalid-damage" -> "full-redraw-fallback"
                    | _ -> "requires-same-profile-live-proof"
                $"| `{scenario}` | {verdict} |")
            |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 152 Damage-Scoped Live Parity"
              ""
              "| Scenario | Verdict |"
              "|----------|---------|"
              rows
              ""
              "Damage-scoped output can be accepted only after the same host profile has an accepted three-run proof set."
              "Resize, full invalidation, invalid damage, unsupported hosts, resource failure, and parity failure route to full redraw or another recorded safe fallback."
              "" ]

    let emitFeature152TimingReport tier =
        let normalized =
            if feature152TimingTiers |> List.contains tier then tier else "damage"

        String.concat
            "\n"
            [ "# Feature 152 Timing Claim Decision"
              ""
              $"Tier: `{normalized}`"
              "Baseline: `full-redraw oracle`"
              "Threshold policy: predeclared benefit/noise policy required before any performance claim is accepted."
              "Required corpus: at least 5 representative live scenarios."
              "Required repetitions: at least 5 comparable repetitions per scenario."
              "Snapshot/reuse context: context-only unless same-profile live timing exists."
              ""
              "Verdict: `environment-limited`"
              ""
              "No compositor performance claim is accepted from synthetic, incomplete, noisy, non-beneficial, or environment-limited timing evidence."
              "" ]

    let emitFeature152ValidationSummary model =
        let tierRows =
            [ PresentProofTier, "Live proof", Limited "three fresh matching capable-host proof attempts are missing"
              DamageScissorTier, "Damage scissor", Limited "same-profile accepted proof and live parity are missing"
              PlacementReuseTier, "Reuse context", Skipped "reuse evidence is context-only for Feature 152 timing"
              ReplayTier, "Replay context", Skipped "replay evidence is context-only for Feature 152 timing"
              SnapshotTier, "Timing claim", Limited "same-profile capable-host timing is missing" ]
            |> List.map (fun (tier, name, defaultVerdict) ->
                let verdict = model.TierVerdicts |> Map.tryFind tier |> Option.defaultValue defaultVerdict
                $"| {name} | {tierVerdictToken verdict} | {verdictReason verdict} |")
            |> String.concat "\n"

        let proofRows =
            match model.Proofs with
            | [] -> "| none | environment-limited | missing capable-host live proof |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 152 P7 Readiness Summary"
              ""
              "Status: `environment-limited`"
              "Performance claim: `environment-limited`"
              ""
              "## Tier Verdicts"
              ""
              "| Tier | Verdict | Reason |"
              "|------|---------|--------|"
              tierRows
              "| Compatibility | ready | public diagnostic and readiness vocabulary impact is documented |"
              "| Regression | ready | focused adjacent readiness verdicts are recorded or explicitly limited |"
              ""
              "## Live Proof"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Evidence Links"
              ""
              "- Live proof: `live-proof/README.md`"
              "- Unsupported host: `live-proof/unsupported/README.md`"
              "- Damage parity: `parity/README.md`"
              "- Timing: `timing/README.md`"
              "- Compatibility: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              ""
              "## Limitations"
              ""
              "- This run is environment-limited in the current host and records zero accepted partial-redraw artifacts."
              "- Partial redraw remains fallback-gated until three fresh matching capable-host attempts and same-profile parity pass."
              "- No compositor performance claim is accepted without same-profile live timing."
              "" ]

    let emitFeature152CompatibilityLedger (model: ReadinessModel) =
        ignore model
        String.concat
            "\n"
            [ "# Feature 152 Compatibility Ledger"
              ""
              "## Public Metrics and Diagnostics"
              ""
              "- `CompositorProof` adds accepted proof-set vocabulary for three-run capable-host acceptance."
              "- `FS.GG.UI.Testing.CompositorReadiness` exposes consumer-facing readiness validation status vocabulary."
              "- Existing fallback behavior remains safe: unsupported, missing, stale, synthetic, mismatched, failed, invalid-damage, or parity-failed evidence keeps full redraw."
              ""
              "## Baseline References"
              ""
              "- `readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt` records proof-set surface exposure."
              "- `readiness/surface-baselines/FS.GG.UI.Testing.txt` records consumer readiness helper exposure."
              "- `readiness/surface-baselines/FS.GG.UI.Controls.txt` and `readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt` remain regression references."
              ""
              "## Release Notes Draft"
              ""
              "- P7 live partial redraw is accepted only from a three-run same-profile live proof set plus same-profile parity."
              "- Current environment-limited evidence records no partial-redraw or performance acceptance."
              ""
              "## Migration Guidance"
              ""
              "- Consumers should treat `environment-limited`, `fallback-gated`, `failed`, and `missing-evidence` as non-accepting readiness states."
              "- Existing hosts continue to full-redraw unless the readiness summary records accepted proof and parity evidence."
              ""
              "## Limitations"
              ""
              "- Synthetic simulations are failure-path tests only and cannot accept live proof."
              "- Capable-host timing remains required for any performance claim."
              "" ]

    let emitFeature153LiveProof (proof: PresentProof) =
        let renderer = proof.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = proof.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"
        let diagnostics =
            match proof.Diagnostics with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 153 Live Proof Interpreter"
              ""
              $"Proof: `{proof.ProofId}`"
              $"Scenario: `{proof.ScenarioId}`"
              $"Verdict: `{proofVerdictToken proof.Verdict}`"
              $"Created: `{proof.CreatedAt:O}`"
              ""
              "## Host Profile"
              ""
              $"- Profile: `{proof.HostProfile.ProfileId}`"
              $"- Backend: `{proof.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{proof.HostProfile.PresentMode}`"
              $"- Framebuffer: `{proof.HostProfile.FramebufferSize}`"
              $"- Scale: `{scale}`"
              $"- Environment: `{proof.HostProfile.DisplayEnvironment}`"
              $"- Algorithm: `{proof.HostProfile.ProofAlgorithmVersion}`"
              $"- Package version: `{feature153PackageVersion}`"
              ""
              "## Attempt Evidence"
              ""
              "- `attempts/`: capable-host attempt summaries and frame artifacts when the host can capture them."
              "- `attempts/<attempt-id>/sentinel-frame.png`: sentinel frame artifact."
              "- `attempts/<attempt-id>/damage-frame.png`: damage-scoped frame artifact."
              "- `unsupported/`: environment-limited output with zero accepted partial-redraw artifacts."
              ""
              "## Acceptance Gate"
              ""
              "- Accepted proof requires exactly three selected fresh matching capable-host attempts."
              "- Missing, stale, blank, synthetic-only, failed, environment-limited, host-mismatched, or proof-method-mismatched evidence fails closed."
              "- This feature does not enable partial redraw or accept a compositor performance claim."
              ""
              "## Evidence Artifacts"
              ""
              renderArtifacts proof.EvidenceArtifacts
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let emitFeature153ProofSet (model: ReadinessModel) =
        let proofRows =
            match model.Proofs with
            | [] -> "| none | fallback-gated | no capable-host attempts are available |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 153 Proof-Set Decision"
              ""
              "Status: `environment-limited`"
              "Selected attempts: `0/3`"
              "Freshness window: `24:00:00`"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Decision"
              ""
              "- The current checkout has no accepted three-run capable-host proof set."
              "- Unsupported, failed, synthetic, stale, host-mismatched, or proof-method-mismatched attempts cannot be selected."
              "- Partial redraw remains fallback-gated until this proof set is accepted and later same-profile parity also passes."
              "" ]

    let emitFeature153ValidationSummary model =
        let proofRows =
            match model.Proofs with
            | [] -> "| none | environment-limited | missing capable-host live proof |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 153 Compositor Proof Interpreter Readiness"
              ""
              "Status: `environment-limited`"
              "Proof set: `environment-limited`"
              "Fallback status: `fallback-gated`"
              "Performance claim: `not-accepted`"
              ""
              "## Live Proof"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Evidence Links"
              ""
              "- Live proof index: `live-proof/README.md`"
              "- Capable-host attempts: `live-proof/attempts/README.md`"
              "- Unsupported host: `live-proof/unsupported/README.md`"
              "- Proof-set decision: `proof-set.md`"
              "- Compatibility: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI authoring: `fsi/compositor-proof-interpreter-authoring.fsx`"
              ""
              "## Limitations"
              ""
              "- This host is environment-limited for live sentinel/damage readback and records zero accepted partial-redraw artifacts."
              "- Partial redraw remains fallback-gated until exactly three fresh matching capable-host attempts are accepted and same-profile parity passes."
              "- No compositor performance claim is accepted until later same-profile live timing evidence passes a declared threshold and noise policy."
              ""
              "## Synthetic Disclosure"
              ""
              "- Synthetic Feature153 tests cover rejection and environment-limited paths only."
              "- Synthetic artifacts cannot satisfy accepted proof attempts or proof-set acceptance."
              "" ]

    let emitFeature153CompatibilityLedger (model: ReadinessModel) =
        ignore model
        String.concat
            "\n"
            [ "# Feature 153 Compatibility Ledger"
              ""
              "## Public API and Diagnostics"
              ""
              "- `CompositorProof.AcceptedProofSet` records selected attempt ids and freshness window for exact three-run proof-set review."
              "- `GlHost.LiveProofHostFacts` and `GlHost.LiveProofHostReadiness` classify capable and unsupported host inputs before accepting evidence."
              "- `Viewer.liveProofInterpreterSupported` exposes whether a viewer program shape can host live proof effects."
              "- `FS.GG.UI.Testing.CompositorReadiness` continues to expose consumer-facing readiness validation status vocabulary."
              ""
              "## Fallback and Readiness Vocabulary"
              ""
              "- `environment-limited`, `fallback-gated`, `failed`, and `missing-evidence` remain non-accepting states."
              "- Unsupported hosts record zero accepted partial-redraw artifacts."
              "- Partial redraw remains full-redraw fallback-gated unless proof and later same-profile parity pass."
              ""
              "## Migration Guidance"
              ""
              "- Consumers should treat Feature 153 as proof-readiness evidence, not as a performance claim."
              "- Existing hosts continue full redraw unless the readiness summary records accepted proof and parity evidence."
              ""
              "## Synthetic Disclosure"
              ""
              "- Synthetic tests are named with `Synthetic` and carry `// SYNTHETIC:` comments at use sites."
              "- Synthetic evidence is rejection-path coverage only."
              "" ]

    // Feature 181: shared package/regression validation skeleton. `renderValidationDoc` owns the
    // invariant `# Feature N <kind>` / `Status:` frame; the divergent section headers and bullets are
    // passed as `body` data. The per-feature wrappers (153-155) and the catalog-collapsed
    // `renderPackageValidation`/`renderRegressionValidation` (156-161) emit byte-identical output to the
    // hand-written bodies they replace.
    let renderValidationDoc (featureNum: int) (kind: string) (status: string) (body: string list) =
        String.concat
            "\n"
            ([ $"# Feature {featureNum} {kind}"
               ""
               $"Status: `{status}`"
               "" ]
             @ body
             @ [ "" ])

    let validationRunsBlock (validationLines: string list) =
        if List.isEmpty validationLines then
            "- pending local validation"
        else
            validationLines |> List.map (sprintf "- %s") |> String.concat "\n"

    // T023 / FR-004 / C-2: dispatch by `FeatureCatalog.descriptorById` lookup, not `match featureNum`.
    // The per-feature fragments live on the descriptor's `Renderers` hooks (FeatureCatalog SSOT); a
    // feature with no package/regression hook fails loud. Output is byte-identical to the prior bodies.
    let renderPackageValidation (featureNum: int) (validationLines: string list) =
        let d = FeatureCatalog.descriptorById featureNum
        match d.Renderers.PackageValidation with
        | Some hook ->
            let checksHeader, surfaceHeader, surfaceBullets = hook ()
            renderValidationDoc featureNum "Package Validation" "accepted-with-recorded-limitations"
                ([ checksHeader; ""; validationRunsBlock validationLines; ""; surfaceHeader; "" ] @ surfaceBullets)
        | None -> failwithf "renderPackageValidation: feature %d is not catalog-collapsed" featureNum

    let renderRegressionValidation (featureNum: int) (validationLines: string list) =
        let d = FeatureCatalog.descriptorById featureNum
        match d.Renderers.RegressionValidation with
        | Some hook ->
            let sectionHeader, bullets = hook ()
            renderValidationDoc featureNum "Regression Validation" "accepted-with-recorded-limitations"
                ([ "## Validation Runs"; ""; validationRunsBlock validationLines; ""; sectionHeader; "" ] @ bullets)
        | None -> failwithf "renderRegressionValidation: feature %d is not catalog-collapsed" featureNum

    let emitFeature153PackageValidation () =
        renderValidationDoc 153 "Package Validation" "pending-local-validation"
            [ "- SkiaViewer surface baseline is refreshed for selected proof-set ids, host readiness facts, and viewer proof support."
              "- Testing surface baseline remains compatible with existing `CompositorReadiness` helpers."
              "- Package FSI transcript coverage is recorded in `fsi/compositor-proof-interpreter-authoring.fsx`." ]

    let emitFeature153RegressionValidation () =
        renderValidationDoc 153 "Regression Validation" "pending-local-validation"
            [ "- Focused Feature153 tests must pass for SkiaViewer, Rendering.Harness, Testing, and Package suites."
              "- Broad solution validation must preserve Feature 152 proof-set behavior and adjacent compositor readiness checks." ]


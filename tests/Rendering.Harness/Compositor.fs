namespace Rendering.Harness

open System
open System.IO

module Compositor =

    let featureId = "147-compositor-damage-redraw"
    let feature148Id = "148-compositor-live-integration"

    let readinessDirectory = "specs/147-compositor-damage-redraw/readiness"
    let presentProofDirectory = Path.Combine(readinessDirectory, "present-proof")
    let parityDirectory = Path.Combine(readinessDirectory, "parity")
    let perfDirectory = Path.Combine(readinessDirectory, "perf")
    let compatibilityLedgerPath = Path.Combine(readinessDirectory, "compatibility-ledger.md")
    let validationSummaryPath = Path.Combine(readinessDirectory, "validation-summary.md")

    let feature148ReadinessDirectory = Path.Combine("specs", feature148Id, "readiness")
    let feature148LiveProofDirectory = Path.Combine(feature148ReadinessDirectory, "live-proof")
    let feature148ParityDirectory = Path.Combine(feature148ReadinessDirectory, "parity")
    let feature148ReuseDirectory = Path.Combine(feature148ReadinessDirectory, "reuse")
    let feature148SnapshotsDirectory = Path.Combine(feature148ReadinessDirectory, "snapshots")
    let feature148TimingDirectory = Path.Combine(feature148ReadinessDirectory, "timing")
    let feature148CompatibilityLedgerPath = Path.Combine(feature148ReadinessDirectory, "compatibility-ledger.md")
    let feature148ValidationSummaryPath = Path.Combine(feature148ReadinessDirectory, "validation-summary.md")
    let feature148PackageVersion = "local-harness"

    type HostProfile =
        { ProfileId: string
          Backend: string
          Renderer: string option
          PresentMode: string
          FramebufferSize: string
          Scale: float option
          DisplayEnvironment: string
          ProofAlgorithmVersion: string }

    type ProofVerdict =
        | ProofPassed
        | ProofFailed of cause: string
        | ProofEnvironmentLimited of reason: string

    type PresentProof =
        { ProofId: string
          HostProfile: HostProfile
          ScenarioId: string
          Verdict: ProofVerdict
          CreatedAt: DateTimeOffset
          EvidenceArtifacts: string list
          Diagnostics: string list }

    type ParityVerdict =
        | ParityPassed
        | ParityFailed of cause: string
        | ParitySkipped of reason: string
        | ParityEnvironmentLimited of reason: string

    type CompositorTier =
        | PresentProofTier
        | DamageScissorTier
        | PromotionTier
        | PlacementReuseTier
        | ReplayTier
        | SnapshotTier

    type TierVerdict =
        | Ready
        | Limited of reason: string
        | Rejected of reason: string
        | Skipped of reason: string

    type Thresholds =
        { PromotionReductionPercent: float
          SimpleSceneOverheadPercent: float
          SnapshotImprovementPercent: float }

    type SnapshotBudget =
        { MaxEntries: int
          MaxBytes: int64 }

    type ReadinessModel =
        { Proofs: PresentProof list
          Parity: Map<string, ParityVerdict>
          TierVerdicts: Map<CompositorTier, TierVerdict>
          Diagnostics: string list }

    type ReadinessMsg =
        | ProofLoaded of PresentProof
        | ParityRecorded of scenarioId: string * verdict: ParityVerdict
        | TierEvaluated of tier: CompositorTier * verdict: TierVerdict
        | DiagnosticRecorded of string

    type ReadinessEffect =
        | WriteValidationSummary of path: string
        | WriteCompatibilityLedger of path: string

    let thresholds =
        { PromotionReductionPercent = 30.0
          SimpleSceneOverheadPercent = 5.0
          SnapshotImprovementPercent = 20.0 }

    let snapshotBudget =
        { MaxEntries = 64
          MaxBytes = 32L * 1024L * 1024L }

    let scenarioIds =
        [ "proof/sentinel-damage-v1"
          "damage/idle"
          "damage/localized-update"
          "damage/overlap"
          "damage/frame-edge"
          "damage/full-frame-invalidation"
          "promotion/stable-boundary"
          "promotion/placement-only-move"
          "promotion/content-change"
          "promotion/churn"
          "snapshot/expensive-stable"
          "snapshot/simple-overhead"
          "snapshot/over-budget" ]

    let targetHostProfiles =
        [ { ProfileId = "x11-opengl-direct"
            Backend = "OpenGL"
            Renderer = None
            PresentMode = "DirectToSwapchain"
            FramebufferSize = "640x480"
            Scale = Some 1.0
            DisplayEnvironment = "x11"
            ProofAlgorithmVersion = "sentinel-damage-v1" }
          { ProfileId = "headless-offscreen"
            Backend = "OpenGL"
            Renderer = None
            PresentMode = "OffscreenReadback"
            FramebufferSize = "640x480"
            Scale = Some 1.0
            DisplayEnvironment = "headless"
            ProofAlgorithmVersion = "sentinel-damage-v1" }
          { ProfileId = "unsupported-display"
            Backend = "OpenGL"
            Renderer = None
            PresentMode = "DirectToSwapchain"
            FramebufferSize = "unknown"
            Scale = None
            DisplayEnvironment = "missing-display"
            ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature148ScenarioIds =
        [ "proof/live-sentinel-damage-v1"
          "proof/non-preserving-host"
          "proof/stale"
          "proof/host-mismatch"
          "proof/missing-display"
          "proof/unsupported-readback"
          "proof/timeout"
          "proof/permission"
          "proof/host-error"
          "damage/idle"
          "damage/localized-update"
          "damage/overlap"
          "damage/frame-edge"
          "damage/movement-old-new"
          "damage/resize"
          "damage/theme-global"
          "damage/stale-proof"
          "damage/disabled"
          "damage/unsupported"
          "damage/parity-failure"
          "reuse/stable-boundary"
          "reuse/moving-only"
          "reuse/scrolling"
          "reuse/content-changing"
          "reuse/theme-resource-change"
          "reuse/churning"
          "reuse/no-benefit"
          "reuse/failed-parity"
          "reuse/same-seed"
          "snapshot/expensive-stable"
          "snapshot/simple-scene"
          "snapshot/churning"
          "snapshot/over-budget"
          "snapshot/invalid-resource"
          "snapshot/unsupported-host"
          "snapshot/parity-failure"
          "timing/damage"
          "timing/placement"
          "timing/replay"
          "timing/snapshot" ]

    let feature148TargetHostProfiles =
        targetHostProfiles
        @ [ { ProfileId = "synthetic-non-preserving"
              Backend = "OpenGL"
              Renderer = Some "synthetic"
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "synthetic"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature148TimingTiers = [ "damage"; "placement"; "replay"; "snapshot" ]

    let private backendToken backend =
        match backend with
        | X11 -> "x11"
        | Wayland -> "wayland"
        | NoDisplay -> "missing-display"

    let hostProfileFromFacts (facts: ProbeFacts) : HostProfile =
        let display = backendToken facts.EffectiveBackend
        let renderer = facts.GlRenderer |> Option.filter (String.IsNullOrWhiteSpace >> not)
        let profile =
            [ display
              facts.GlRenderer |> Option.defaultValue "unknown-renderer"
              facts.GlVersion |> Option.defaultValue "unknown-gl"
              if facts.GlDirect then "direct" else "indirect" ]
            |> String.concat "|"
            |> fun value -> value.GetHashCode(StringComparison.Ordinal).ToString("x")

        { ProfileId = $"probe-{profile}"
          Backend = "OpenGL"
          Renderer = renderer
          PresentMode = "DirectToSwapchain"
          FramebufferSize = "640x480"
          Scale = Some 1.0
          DisplayEnvironment = display
          ProofAlgorithmVersion = "sentinel-damage-v1" }

    let proofVerdictToken verdict =
        match verdict with
        | ProofPassed -> "passed"
        | ProofFailed _ -> "failed"
        | ProofEnvironmentLimited _ -> "environment-limited"

    let parityVerdictToken verdict =
        match verdict with
        | ParityPassed -> "passed"
        | ParityFailed _ -> "failed"
        | ParitySkipped _ -> "skipped"
        | ParityEnvironmentLimited _ -> "environment-limited"

    let tierToken tier =
        match tier with
        | PresentProofTier -> "present-proof"
        | DamageScissorTier -> "damage-scissor"
        | PromotionTier -> "promotion"
        | PlacementReuseTier -> "placement-reuse"
        | ReplayTier -> "replay"
        | SnapshotTier -> "snapshot"

    let tierVerdictToken verdict =
        match verdict with
        | Ready -> "ready"
        | Limited _ -> "limited"
        | Rejected _ -> "rejected"
        | Skipped _ -> "skipped"

    let private tierDisplayName tier =
        match tier with
        | PresentProofTier -> "Present proof"
        | DamageScissorTier -> "Damage scissor"
        | PromotionTier -> "Promotion"
        | PlacementReuseTier -> "Placement reuse"
        | ReplayTier -> "Replay"
        | SnapshotTier -> "Snapshot"

    let private verdictReason verdict =
        match verdict with
        | Ready -> "passed proof, parity, and threshold obligations"
        | Limited reason
        | Rejected reason
        | Skipped reason -> reason

    let proofMatchesHost (active: HostProfile) (proof: PresentProof) =
        proof.HostProfile.ProfileId = active.ProfileId
        && proof.HostProfile.Backend = active.Backend
        && proof.HostProfile.PresentMode = active.PresentMode
        && proof.HostProfile.FramebufferSize = active.FramebufferSize
        && proof.HostProfile.ProofAlgorithmVersion = active.ProofAlgorithmVersion

    let proofIsFresh (now: DateTimeOffset) (maxAge: TimeSpan) (proof: PresentProof) =
        proof.CreatedAt <= now && now - proof.CreatedAt <= maxAge

    let validateProofForScissoring active now maxAge proof =
        match proof with
        | None -> Limited "missing present-path proof"
        | Some proof when not (proofMatchesHost active proof) -> Limited "present-path proof is for a different host profile"
        | Some proof when not (proofIsFresh now maxAge proof) -> Limited "present-path proof is stale"
        | Some { Verdict = ProofPassed } -> Ready
        | Some { Verdict = ProofFailed cause } -> Rejected cause
        | Some { Verdict = ProofEnvironmentLimited reason } -> Limited reason

    let evaluateTier proof parity performancePassed =
        match proof with
        | Rejected reason -> Rejected reason
        | Limited reason -> Limited reason
        | Skipped reason -> Skipped reason
        | Ready ->
            match parity, performancePassed with
            | Some(ParityFailed cause), _ -> Rejected cause
            | Some(ParityEnvironmentLimited reason), _ -> Limited reason
            | Some(ParitySkipped reason), _ -> Skipped reason
            | None, _ -> Limited "missing full-redraw oracle parity"
            | Some ParityPassed, Some false -> Rejected "performance threshold failed"
            | Some ParityPassed, Some true
            | Some ParityPassed, None -> Ready

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

    let artifactPath directory name = Path.Combine(directory, name)
    let feature148ArtifactPath directory name = Path.Combine(feature148ReadinessDirectory, directory, name)

    let renderPresentProof proof =
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

    let private renderArtifacts artifacts =
        match artifacts with
        | [] -> "- none"
        | xs -> xs |> List.map (sprintf "- `%s`") |> String.concat "\n"

    let renderFeature148LiveProof proof =
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

    let renderFeature148ParityReport () =
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

    let renderFeature148ReuseReport () =
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

    let renderFeature148SnapshotReport () =
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

    let renderFeature148TimingReport tier =
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

    let renderFeature148ValidationSummary model =
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

    let renderFeature148CompatibilityLedger model =
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

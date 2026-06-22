namespace Rendering.Harness.Compositor

open Rendering.Harness

open System
open System.IO
open System.Security.Cryptography
open System.Text
open Rendering.Harness.Compositor.Types

module Config =
    let featureId = "147-compositor-damage-redraw"
    let feature148Id = "148-compositor-live-integration"
    let feature149Id = "149-complete-compositor-p7"
    let feature152Id = "152-compositor-live-proof"
    let feature153Id = "153-compositor-proof-interpreter"
    let feature154Id = "154-compositor-proof-acceptance"
    let feature155Id = "155-native-proof-capture"
    let feature156Id = "156-same-profile-timing"
    let feature157Id = "157-no-clear-damage-scissor"
    let feature158Id = "158-separate-proof-timing"
    let feature159Id = "159-layer-promotion-keys"
    let feature160Id = "160-performance-validation-throughput"
    let feature161Id = "161-host-performance-lane-ledger"

    // Single-source-of-truth descriptor handles (feature 185 US1). Every per-feature readiness
    // directory/path is now derived from `FeatureCatalog` rather than hand-declared. Forcing
    // `descriptorById` at module load also fires the fail-loud duplicate-alias check (FR-011).
    let d148 = FeatureCatalog.descriptorById 148
    let d149 = FeatureCatalog.descriptorById 149
    let d152 = FeatureCatalog.descriptorById 152
    let d153 = FeatureCatalog.descriptorById 153
    let d154 = FeatureCatalog.descriptorById 154
    let d155 = FeatureCatalog.descriptorById 155
    let d156 = FeatureCatalog.descriptorById 156
    let d157 = FeatureCatalog.descriptorById 157
    let d158 = FeatureCatalog.descriptorById 158
    let d159 = FeatureCatalog.descriptorById 159
    let d160 = FeatureCatalog.descriptorById 160
    let d161 = FeatureCatalog.descriptorById 161

    let readinessDirectory = "specs/147-compositor-damage-redraw/readiness"
    let presentProofDirectory = Path.Combine(readinessDirectory, "present-proof")
    let parityDirectory = Path.Combine(readinessDirectory, "parity")
    let perfDirectory = Path.Combine(readinessDirectory, "perf")
    let compatibilityLedgerPath = Path.Combine(readinessDirectory, "compatibility-ledger.md")
    let validationSummaryPath = Path.Combine(readinessDirectory, "validation-summary.md")

    let feature148LiveProofDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d148, "live-proof")
    let feature148ParityDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d148, "parity")
    let feature148ReuseDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d148, "reuse")
    let feature148SnapshotsDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d148, "snapshots")
    let feature148TimingDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d148, "timing")
    let feature148CompatibilityLedgerPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d148, "compatibility-ledger.md")
    let feature148ValidationSummaryPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d148, "validation-summary.md")
    let feature148PackageVersion = "local-harness"

    let feature149LiveProofDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d149, "live-proof")
    let feature149ParityDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d149, "parity")
    let feature149ReuseDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d149, "reuse")
    let feature149SnapshotsDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d149, "snapshots")
    let feature149TimingDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d149, "timing")
    let feature149CompatibilityLedgerPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d149, "compatibility-ledger.md")
    let feature149ValidationSummaryPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d149, "validation-summary.md")
    let feature149PackageVersion = "local-harness"

    let feature152LiveProofDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d152, "live-proof")
    let feature152ParityDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d152, "parity")
    let feature152TimingDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d152, "timing")
    let feature152FsiDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d152, "fsi")
    let feature152CompatibilityLedgerPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d152, "compatibility-ledger.md")
    let feature152ValidationSummaryPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d152, "validation-summary.md")
    let feature152PackageVersion = "local-harness"

    let feature153LiveProofDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d153, "live-proof")
    let feature153LiveProofAttemptsDirectory = Path.Combine(feature153LiveProofDirectory, "attempts")
    let feature153LiveProofUnsupportedDirectory = Path.Combine(feature153LiveProofDirectory, "unsupported")
    let feature153FsiDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d153, "fsi")
    let feature153ProofSetPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d153, "proof-set.md")
    let feature153CompatibilityLedgerPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d153, "compatibility-ledger.md")
    let feature153ValidationSummaryPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d153, "validation-summary.md")
    let feature153PackageValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d153, "package-validation.md")
    let feature153RegressionValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d153, "regression-validation.md")
    let feature153PackageVersion = "local-harness"

    let feature154LiveProofDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d154, "live-proof")
    let feature154LiveProofAttemptsDirectory = Path.Combine(feature154LiveProofDirectory, "attempts")
    let feature154LiveProofUnsupportedDirectory = Path.Combine(feature154LiveProofDirectory, "unsupported")
    let feature154ParityDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d154, "parity")
    let feature154TimingDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d154, "timing")
    let feature154FsiDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d154, "fsi")
    let feature154ProofSetPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d154, "proof-set.md")
    let feature154CompatibilityLedgerPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d154, "compatibility-ledger.md")
    let feature154ValidationSummaryPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d154, "validation-summary.md")
    let feature154PackageValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d154, "package-validation.md")
    let feature154RegressionValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d154, "regression-validation.md")
    let feature154PackageVersion = "local-harness"

    let feature155LiveProofDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d155, "live-proof")
    let feature155LiveProofAttemptsDirectory = Path.Combine(feature155LiveProofDirectory, "attempts")
    let feature155LiveProofUnsupportedDirectory = Path.Combine(feature155LiveProofDirectory, "unsupported")
    let feature155ParityDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d155, "parity")
    let feature155TimingDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d155, "timing")
    let feature155FsiDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d155, "fsi")
    let feature155ProofSetPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d155, "proof-set.md")
    let feature155CompatibilityLedgerPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d155, "compatibility-ledger.md")
    let feature155ValidationSummaryPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d155, "validation-summary.md")
    let feature155PackageValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d155, "package-validation.md")
    let feature155RegressionValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d155, "regression-validation.md")
    let feature155PackageVersion = "local-harness"

    let feature156TimingDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d156, "timing")
    let feature156TimingScenariosDirectory = Path.Combine(feature156TimingDirectory, "scenarios")
    let feature156TimingRawDirectory = Path.Combine(feature156TimingDirectory, "raw")
    let feature156TimingUnsupportedDirectory = Path.Combine(feature156TimingDirectory, "unsupported")
    let feature156FsiDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d156, "fsi")
    let feature156CompatibilityLedgerPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d156, "compatibility-ledger.md")
    let feature156ValidationSummaryPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d156, "validation-summary.md")
    let feature156PackageValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d156, "package-validation.md")
    let feature156RegressionValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d156, "regression-validation.md")
    let feature156TimingSummaryPath = Path.Combine(feature156TimingDirectory, "summary.md")
    let feature156PackageVersion = "local-harness"
    // Sourced from the descriptor (feature 185 US1) so the literal lives only in FeatureCatalog;
    // 157–161 chain off this, so the whole family is now descriptor-derived. Byte-identical.
    let feature156AcceptedProfileId = d156.Config.AcceptedProfileId |> Option.defaultValue "probe-08a47c01"
    let feature156PolicyId = "same-profile-live-threshold-v2"

    let feature157DamageDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d157, "damage")
    let feature157DamageAttemptsDirectory = Path.Combine(feature157DamageDirectory, "attempts")
    let feature157DamageFallbacksDirectory = Path.Combine(feature157DamageDirectory, "fallbacks")
    let feature157DamageParityDirectory = Path.Combine(feature157DamageDirectory, "parity")
    let feature157DamageUnsupportedDirectory = Path.Combine(feature157DamageDirectory, "unsupported")
    let feature157FsiDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d157, "fsi")
    let feature157CompatibilityLedgerPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d157, "compatibility-ledger.md")
    let feature157ValidationSummaryPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d157, "validation-summary.md")
    let feature157PackageValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d157, "package-validation.md")
    let feature157RegressionValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d157, "regression-validation.md")
    let feature157DamageSummaryPath = Path.Combine(feature157DamageDirectory, "summary.md")
    let feature157DamageSummaryJsonPath = Path.Combine(feature157DamageDirectory, "summary.json")
    let feature157AcceptedProfileId = feature156AcceptedProfileId

    let feature158TimingDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d158, "timing")
    let feature158TimingScenariosDirectory = Path.Combine(feature158TimingDirectory, "scenarios")
    let feature158TimingRawDirectory = Path.Combine(feature158TimingDirectory, "raw")
    let feature158TimingExcludedDirectory = Path.Combine(feature158TimingDirectory, "excluded")
    let feature158TimingUnsupportedDirectory = Path.Combine(feature158TimingDirectory, "unsupported")
    let feature158ProofProbesDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d158, "proof-probes")
    let feature158FsiDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d158, "fsi")
    let feature158SurfaceBaselinesDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d158, "surface-baselines")
    let feature158CompatibilityLedgerPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d158, "compatibility-ledger.md")
    let feature158ValidationSummaryPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d158, "validation-summary.md")
    let feature158PackageValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d158, "package-validation.md")
    let feature158RegressionValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d158, "regression-validation.md")
    let feature158TimingSummaryPath = Path.Combine(feature158TimingDirectory, "summary.md")
    let feature158TimingSummaryJsonPath = Path.Combine(feature158TimingDirectory, "summary.json")
    let feature158AcceptedProfileId = feature156AcceptedProfileId
    let feature158PolicyId = "readback-free-timing-v1"
    let feature158PerformanceCommand = "compositor-performance --feature 158"
    let feature158ProbeCommand = "compositor-performance --feature 158 --probe-readback"
    let feature158ReadinessCommand = "compositor-readiness --feature 158"

    let feature159PromotionDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d159, "promotion")
    let feature159PromotionAttemptsDirectory = Path.Combine(feature159PromotionDirectory, "attempts")
    let feature159PromotionReuseDirectory = Path.Combine(feature159PromotionDirectory, "reuse")
    let feature159PromotionDemotionsDirectory = Path.Combine(feature159PromotionDirectory, "demotions")
    let feature159PromotionFallbacksDirectory = Path.Combine(feature159PromotionDirectory, "fallbacks")
    let feature159PromotionParityDirectory = Path.Combine(feature159PromotionDirectory, "parity")
    let feature159PromotionUnsupportedDirectory = Path.Combine(feature159PromotionDirectory, "unsupported")
    let feature159CountersDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d159, "counters")
    let feature159FsiDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d159, "fsi")
    let feature159CompatibilityLedgerPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d159, "compatibility-ledger.md")
    let feature159ValidationSummaryPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d159, "validation-summary.md")
    let feature159PackageValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d159, "package-validation.md")
    let feature159RegressionValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d159, "regression-validation.md")
    let feature159PromotionSummaryPath = Path.Combine(feature159PromotionDirectory, "summary.md")
    let feature159AcceptedProfileId = feature156AcceptedProfileId
    let feature159PolicyId = "layer-promotion-v1"
    let feature159PromotionCommand = "compositor-promotion --feature 159"
    let feature159ReadinessCommand = "compositor-readiness --feature 159"

    let feature160ThroughputDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d160, "throughput")
    let feature160ThroughputIterationsDirectory = Path.Combine(feature160ThroughputDirectory, "iterations")
    let feature160ThroughputRawDirectory = Path.Combine(feature160ThroughputDirectory, "raw")
    let feature160ThroughputExcludedDirectory = Path.Combine(feature160ThroughputDirectory, "excluded")
    let feature160ThroughputUnsupportedDirectory = Path.Combine(feature160ThroughputDirectory, "unsupported")
    let feature160FullValidationDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d160, "full-validation")
    let feature160FsiDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d160, "fsi")
    let feature160CompatibilityLedgerPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d160, "compatibility-ledger.md")
    let feature160ValidationSummaryPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d160, "validation-summary.md")
    let feature160PackageValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d160, "package-validation.md")
    let feature160RegressionValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d160, "regression-validation.md")
    let feature160ThroughputSummaryPath = Path.Combine(feature160ThroughputDirectory, "summary.md")
    let feature160ThroughputSummaryJsonPath = Path.Combine(feature160ThroughputDirectory, "summary.json")
    let feature160AcceptedProfileId = feature158AcceptedProfileId
    let feature160PolicyId = "focused-throughput-v1"
    let feature160FocusedLaneId = "focused"
    let feature160RequiredAttempts = 3
    let feature160MaxIterationMinutes = 10
    let feature160UnsupportedHostMinutes = 2
    let feature160PerformanceCommand = "compositor-performance --feature 160 --lane focused"
    let feature160ReadinessCommand = "compositor-readiness --feature 160"

    let feature161LaneLedgerDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d161, "lane-ledger")
    let feature161LaneLedgerEntriesDirectory = Path.Combine(feature161LaneLedgerDirectory, "entries")
    let feature161LaneLedgerHostFactsDirectory = Path.Combine(feature161LaneLedgerDirectory, "host-facts")
    let feature161LaneLedgerExcludedDirectory = Path.Combine(feature161LaneLedgerDirectory, "excluded")
    let feature161LaneLedgerUnsupportedDirectory = Path.Combine(feature161LaneLedgerDirectory, "unsupported")
    let feature161FullValidationDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d161, "full-validation")
    let feature161FsiDirectory = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d161, "fsi")
    let feature161CompatibilityLedgerPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d161, "compatibility-ledger.md")
    let feature161ValidationSummaryPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d161, "validation-summary.md")
    let feature161PackageValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d161, "package-validation.md")
    let feature161RegressionValidationPath = Path.Combine(FeatureCatalog.FeatureDescriptor.readinessDirectory d161, "regression-validation.md")
    let feature161LaneLedgerSummaryPath = Path.Combine(feature161LaneLedgerDirectory, "summary.md")
    let feature161LaneLedgerSummaryJsonPath = Path.Combine(feature161LaneLedgerDirectory, "summary.json")
    let feature161AcceptedProfileId = feature160AcceptedProfileId
    let feature161PolicyId = "host-lane-ledger-v1"
    let feature161HostLaneId = "x11-:1-direct-opengl-amd-mesa"
    let feature161PerformanceCommand = "compositor-performance --feature 161 --lane host-ledger"
    let feature161ReadinessCommand = "compositor-readiness --feature 161"


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

    let feature149ScenarioIds =
        [ "proof/live-sentinel-damage-v1"
          "proof/capable-host-three-run"
          "proof/non-preserving-host"
          "proof/stale"
          "proof/host-mismatch"
          "proof/algorithm-mismatch"
          "proof/missing-artifact"
          "proof/blank-artifact"
          "proof/synthetic-only"
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
          "damage/zero-damage"
          "damage/stale-proof"
          "damage/disabled"
          "damage/unsupported"
          "damage/resource-failure"
          "damage/internal-error"
          "damage/parity-failure"
          "reuse/stable-boundary"
          "reuse/placement-only"
          "reuse/mixed-change"
          "reuse/no-change"
          "reuse/content-changing"
          "reuse/churning"
          "reuse/no-benefit"
          "reuse/failed-parity"
          "reuse/same-seed"
          "snapshot/expensive-stable"
          "snapshot/create-reuse-refresh"
          "snapshot/replacement-eviction-disposal"
          "snapshot/simple-scene"
          "snapshot/churning"
          "snapshot/over-budget"
          "snapshot/stale-resource"
          "snapshot/invalid-resource"
          "snapshot/unsupported-host"
          "snapshot/parity-failure"
          "timing/damage"
          "timing/placement"
          "timing/replay"
          "timing/snapshot"
          "readiness/public-diagnostics"
          "readiness/compatibility-ledger" ]

    let feature149TargetHostProfiles =
        feature148TargetHostProfiles
        @ [ { ProfileId = "feature149-capable-host-candidate"
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature149TimingTiers = feature148TimingTiers

    let feature152ScenarioIds =
        [ "proof/live-sentinel-damage-v1"
          "proof/capable-host-three-run"
          "proof/unsupported-host-zero-accepted"
          "proof/stale"
          "proof/host-mismatch"
          "proof/proof-method-mismatch"
          "proof/missing-artifact"
          "proof/blank-artifact"
          "proof/synthetic-only"
          "damage/localized-update"
          "damage/no-change"
          "damage/movement-old-new"
          "damage/edge-clipped"
          "damage/resize"
          "damage/full-frame-invalidation"
          "damage/invalid-damage"
          "damage/unsupported"
          "damage/resource-failure"
          "damage/parity-failure"
          "timing/localized-update"
          "timing/no-change"
          "timing/movement"
          "timing/resize"
          "timing/churn"
          "readiness/final-decision"
          "readiness/compatibility-ledger"
          "readiness/package-validation" ]

    let feature152TargetHostProfiles =
        feature149TargetHostProfiles
        @ [ { ProfileId = "feature152-capable-host-candidate"
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature152TimingTiers = [ "damage" ]

    let feature153ScenarioIds =
        [ "proof/live-sentinel-damage-v1"
          "proof/capable-host-three-run"
          "proof/unsupported-host-zero-accepted"
          "proof/readback-limited"
          "proof/stale"
          "proof/host-mismatch"
          "proof/proof-method-mismatch"
          "proof/missing-artifact"
          "proof/blank-artifact"
          "proof/synthetic-only"
          "proof/selected-trio"
          "readiness/final-decision"
          "readiness/compatibility-ledger"
          "readiness/package-validation"
          "readiness/regression-validation" ]

    let feature153TargetHostProfiles =
        feature152TargetHostProfiles
        @ [ { ProfileId = "feature153-capable-host-candidate"
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature154ScenarioIds =
        [ "proof/live-sentinel-damage-v1"
          "proof/capable-host-three-run"
          "proof/unsupported-host-zero-accepted"
          "proof/stale"
          "proof/host-mismatch"
          "proof/proof-method-mismatch"
          "proof/missing-artifact"
          "proof/blank-artifact"
          "proof/undecodable-artifact"
          "proof/synthetic-only"
          "proof/incomplete"
          "proof/damaged-pixel-failure"
          "proof/undamaged-preservation-failure"
          "damage/localized-update"
          "damage/no-change"
          "damage/movement"
          "damage/overlap"
          "damage/edge-clipping"
          "damage/resize"
          "damage/full-invalidation"
          "damage/invalid-damage"
          "damage/unsupported-host"
          "damage/resource-failure"
          "timing/localized-update"
          "timing/no-change"
          "timing/movement"
          "timing/overlap"
          "timing/resize"
          "readiness/final-decision"
          "readiness/compatibility-ledger"
          "readiness/package-validation"
          "readiness/regression-validation" ]

    let feature154TargetHostProfiles =
        feature153TargetHostProfiles
        @ [ { ProfileId = "feature154-capable-host-candidate"
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature154TimingTiers = [ "damage" ]

    let feature155ScenarioIds =
        [ "proof/live-sentinel-damage-v1"
          "proof/native-capable-host-three-run"
          "proof/unsupported-host-zero-accepted"
          "proof/artifact-write-failure"
          "proof/timeout"
          "proof/missing-artifact"
          "proof/blank-artifact"
          "proof/undecodable-artifact"
          "proof/synthetic-only"
          "proof/damaged-pixel-failure"
          "proof/undamaged-preservation-failure"
          "damage/localized-update"
          "damage/no-change"
          "damage/movement"
          "damage/overlap"
          "damage/edge-clipping"
          "damage/resize"
          "damage/full-invalidation"
          "damage/invalid-damage"
          "damage/unsupported-host"
          "damage/resource-failure"
          "timing/localized-update"
          "timing/no-change"
          "timing/movement"
          "timing/overlap"
          "timing/resize"
          "readiness/final-p7-closeout"
          "readiness/compatibility-ledger"
          "readiness/package-validation"
          "readiness/regression-validation" ]

    let feature155TargetHostProfiles =
        feature154TargetHostProfiles
        @ [ { ProfileId = "feature155-current-capable-host"
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature155TimingTiers = [ "damage" ]

    let feature156RequiredScenarioIds =
        [ "timing/localized-update"
          "timing/no-change"
          "timing/movement-old-new"
          "timing/overlap"
          "timing/edge-clipping" ]

    let feature156ScenarioIds =
        feature156RequiredScenarioIds
        @ [ "timing/cross-profile-rejected"
            "timing/incomplete-samples"
            "timing/noisy"
            "timing/non-beneficial"
            "timing/readback-limited"
            "timing/unsupported-host" ]

    let feature156TargetHostProfiles =
        feature155TargetHostProfiles
        @ [ { ProfileId = feature156AcceptedProfileId
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature157RequiredScenarioIds =
        [ "damage/static-preserved"
          "damage/localized-update"
          "damage/movement-old-new"
          "damage/scroll-shifted"
          "damage/nested-retained" ]

    let feature157FallbackScenarioIds =
        [ "damage/empty-visible-change"
          "damage/out-of-bounds"
          "damage/stale"
          "damage/incomplete"
          "damage/full-frame-invalidation"
          "damage/missing-retained-backing"
          "damage/resource-failure"
          "damage/parity-mismatch"
          "damage/unsupported-host" ]

    let feature157ScenarioIds =
        feature157RequiredScenarioIds
        @ feature157FallbackScenarioIds
        @ [ "readiness/final-decision"
            "readiness/compatibility-ledger"
            "readiness/package-validation"
            "readiness/regression-validation" ]

    let feature157TargetHostProfiles =
        feature156TargetHostProfiles
        @ [ { ProfileId = feature157AcceptedProfileId
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature158RequiredScenarioIds = feature156RequiredScenarioIds

    let feature158ScenarioIds =
        feature158RequiredScenarioIds
        @ [ "timing/probe-readback"
            "timing/proof-readback-in-measured-interval"
            "timing/missing-policy"
            "timing/unverified-policy"
            "timing/cross-profile-evidence"
            "timing/package-version-mismatch"
            "timing/run-identity-mismatch"
            "timing/unsupported-host"
            "readiness/validation-summary"
            "readiness/compatibility-ledger"
            "readiness/package-validation"
            "readiness/regression-validation" ]

    let feature158TargetHostProfiles =
        feature157TargetHostProfiles
        @ [ { ProfileId = feature158AcceptedProfileId
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature159RequiredScenarioIds =
        [ "promotion/static-retained"
          "promotion/placement-only-move"
          "promotion/scroll-shifted"
          "promotion/nested-retained"
          "promotion/content-change"
          "promotion/churn-demotion"
          "promotion/fallback-safe" ]

    let feature159FallbackScenarioIds =
        [ "promotion/ambiguous-identity"
          "promotion/parity-mismatch"
          "promotion/cross-profile"
          "promotion/missing-policy"
          "promotion/unsupported-host" ]

    let feature159ScenarioIds =
        feature159RequiredScenarioIds
        @ feature159FallbackScenarioIds
        @ [ "readiness/validation-summary"
            "readiness/compatibility-ledger"
            "readiness/package-validation"
            "readiness/regression-validation" ]

    let feature159TargetHostProfiles =
        feature158TargetHostProfiles
        @ [ { ProfileId = feature159AcceptedProfileId
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature160RequiredScenarioIds = feature158RequiredScenarioIds

    let feature160ScenarioIds =
        feature160RequiredScenarioIds
        @ [ "timing/sparse-heavy-localized-update"
            "timing/restricted-debug"
            "timing/timed-out"
            "timing/canceled"
            "timing/partial-evidence"
            "timing/cross-profile-evidence"
            "timing/stale-evidence"
            "timing/mixed-policy"
            "timing/missing-metadata"
            "timing/unsupported-host"
            "timing/environment-limited"
            "timing/scenario-coverage-missing"
            "timing/sample-policy-mismatch"
            "timing/run-identity-mismatch"
            "timing/artifact-unreadable"
            "timing/readback-contaminated"
            "readiness/validation-summary"
            "readiness/full-validation"
            "readiness/compatibility-ledger"
            "readiness/package-validation"
            "readiness/regression-validation" ]

    let feature160TargetHostProfiles =
        feature159TargetHostProfiles
        @ [ { ProfileId = feature160AcceptedProfileId
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature161RequiredScenarioIds = feature160RequiredScenarioIds

    let feature161NonGeneralizedLanes =
        [ "Wayland"
          "indirect GL"
          "missing display"
          "software raster"
          "virtualized presentation"
          "unknown renderer"
          "stale package"
          "cross-profile timing" ]

    let feature161PriorGateLinks =
        [ { Feature = feature155Id; Status = "confirmed"; EvidencePath = "specs/155-native-proof-capture/readiness/validation-summary.md" }
          { Feature = feature157Id; Status = "confirmed"; EvidencePath = "specs/157-no-clear-damage-scissor/readiness/validation-summary.md" }
          { Feature = feature158Id; Status = "confirmed"; EvidencePath = "specs/158-separate-proof-timing/readiness/validation-summary.md" }
          { Feature = feature159Id; Status = "confirmed"; EvidencePath = "specs/159-layer-promotion-keys/readiness/validation-summary.md" }
          { Feature = feature160Id; Status = "confirmed"; EvidencePath = "specs/160-performance-validation-throughput/readiness/validation-summary.md" } ]

    let backendToken backend =
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
            |> fun value ->
                SHA256.HashData(Encoding.UTF8.GetBytes value)
                |> Array.take 4
                |> Array.map (fun byte -> byte.ToString("x2"))
                |> String.concat ""

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

    let tierDisplayName tier =
        match tier with
        | PresentProofTier -> "Present proof"
        | DamageScissorTier -> "Damage scissor"
        | PromotionTier -> "Promotion"
        | PlacementReuseTier -> "Placement reuse"
        | ReplayTier -> "Replay"
        | SnapshotTier -> "Snapshot"

    let verdictReason verdict =
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


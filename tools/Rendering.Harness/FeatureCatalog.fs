namespace Rendering.Harness

open System
open System.IO

module FeatureCatalog =

    type ReportVariant =
        | ValidationSummary
        | CompatibilityLedger
        | PackageValidation
        | RegressionValidation
        | UnsupportedHost
        | Timing
        | LiveProof
        | Parity
        | ProofSet
        | Reuse
        | Snapshot

    type FeatureConfig =
        { PolicyId: string option
          AcceptedProfileId: string option
          RequiredScenarioIds: string list }

    type FeatureDescriptor =
        { Id: int
          Slug: string
          CliAliases: string list
          Variants: Set<ReportVariant>
          RequiredHeaders: string list
          Config: FeatureConfig }

    let private emptyConfig =
        { PolicyId = None
          AcceptedProfileId = None
          RequiredScenarioIds = [] }

    // The shared accepted-profile id threaded across the timing/perf features (constants
    // feature156AcceptedProfileId and its aliases in Compositor.fs).
    let private sharedAcceptedProfileId = "probe-08a47c01"

    let private descriptor id slug variants config =
        { Id = id
          Slug = slug
          CliAliases = [ string id; "feature" + string id; slug ]
          Variants = Set.ofList variants
          RequiredHeaders = []
          Config = config }

    let catalog: FeatureDescriptor list =
        [ descriptor 148 "148-compositor-live-integration"
              [ ValidationSummary; CompatibilityLedger; LiveProof; Parity; Reuse; Snapshot; Timing ]
              emptyConfig
          descriptor 149 "149-complete-compositor-p7"
              [ ValidationSummary; CompatibilityLedger; LiveProof; Parity; Reuse; Snapshot; Timing ]
              emptyConfig
          descriptor 152 "152-compositor-live-proof"
              [ ValidationSummary; CompatibilityLedger; LiveProof; Parity; Timing ]
              emptyConfig
          descriptor 153 "153-compositor-proof-interpreter"
              [ ValidationSummary; CompatibilityLedger; LiveProof; PackageValidation; RegressionValidation; ProofSet ]
              emptyConfig
          descriptor 154 "154-compositor-proof-acceptance"
              [ ValidationSummary; CompatibilityLedger; LiveProof; Parity; Timing; PackageValidation; RegressionValidation; ProofSet ]
              emptyConfig
          descriptor 155 "155-native-proof-capture"
              [ ValidationSummary; CompatibilityLedger; LiveProof; Parity; Timing; PackageValidation; RegressionValidation; ProofSet ]
              emptyConfig
          descriptor 156 "156-same-profile-timing"
              [ ValidationSummary; CompatibilityLedger; PackageValidation; RegressionValidation; UnsupportedHost ]
              { emptyConfig with
                  PolicyId = Some "same-profile-live-threshold-v2"
                  AcceptedProfileId = Some sharedAcceptedProfileId }
          descriptor 157 "157-no-clear-damage-scissor"
              [ ValidationSummary; CompatibilityLedger; Parity; PackageValidation; RegressionValidation; UnsupportedHost ]
              { emptyConfig with AcceptedProfileId = Some sharedAcceptedProfileId }
          descriptor 158 "158-separate-proof-timing"
              [ ValidationSummary; CompatibilityLedger; PackageValidation; RegressionValidation; UnsupportedHost ]
              { emptyConfig with
                  PolicyId = Some "readback-free-timing-v1"
                  AcceptedProfileId = Some sharedAcceptedProfileId }
          descriptor 159 "159-layer-promotion-keys"
              [ ValidationSummary; CompatibilityLedger; PackageValidation; RegressionValidation; UnsupportedHost ]
              { emptyConfig with
                  PolicyId = Some "layer-promotion-v1"
                  AcceptedProfileId = Some sharedAcceptedProfileId }
          descriptor 160 "160-performance-validation-throughput"
              [ ValidationSummary; CompatibilityLedger; PackageValidation; RegressionValidation; UnsupportedHost ]
              { emptyConfig with
                  PolicyId = Some "focused-throughput-v1"
                  AcceptedProfileId = Some sharedAcceptedProfileId }
          descriptor 161 "161-host-performance-lane-ledger"
              [ ValidationSummary; CompatibilityLedger; PackageValidation; RegressionValidation; UnsupportedHost ]
              { emptyConfig with
                  PolicyId = Some "host-lane-ledger-v1"
                  AcceptedProfileId = Some sharedAcceptedProfileId } ]

    module FeatureDescriptor =

        let readinessDirectory (d: FeatureDescriptor) =
            Path.Combine("specs", d.Slug, "readiness")

        // The directory that a variant's artifact lives in. Directory-bearing standard variants map
        // to their sub-directory; file-based variants (validation-summary.md, compatibility-ledger.md,
        // package-validation.md, proof-set.md) live directly under the readiness directory.
        let variantDirectory (v: ReportVariant) (d: FeatureDescriptor) =
            let readiness = readinessDirectory d
            match v with
            | LiveProof -> Path.Combine(readiness, "live-proof")
            | Parity -> Path.Combine(readiness, "parity")
            | Reuse -> Path.Combine(readiness, "reuse")
            | Snapshot -> Path.Combine(readiness, "snapshots")
            | Timing -> Path.Combine(readiness, "timing")
            | ValidationSummary
            | CompatibilityLedger
            | PackageValidation
            | RegressionValidation
            | UnsupportedHost
            | ProofSet -> readiness

        let compatibilityLedgerPath (d: FeatureDescriptor) =
            Path.Combine(readinessDirectory d, "compatibility-ledger.md")

        let validationSummaryPath (d: FeatureDescriptor) =
            Path.Combine(readinessDirectory d, "validation-summary.md")

        let packageValidationPath (d: FeatureDescriptor) =
            Path.Combine(readinessDirectory d, "package-validation.md")

        let regressionValidationPath (d: FeatureDescriptor) =
            Path.Combine(readinessDirectory d, "regression-validation.md")

        let supports (v: ReportVariant) (d: FeatureDescriptor) = Set.contains v d.Variants

        let tryByAlias (value: string) =
            catalog
            |> List.tryFind (fun d ->
                d.CliAliases
                |> List.exists (fun alias -> String.Equals(alias, value, StringComparison.OrdinalIgnoreCase)))

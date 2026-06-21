namespace Rendering.Harness

/// Single source of truth for the per-feature harness data table (feature 181).
///
/// Internal-tool surface only (FR-008): consumed by `Compositor`, `Cli`, and the harness/package
/// tests. No type or value here is referenced from any shipped `src/` (`FS.GG.UI.*`) project.
module FeatureCatalog =

    /// The named kinds of per-feature report output that the generic renderer dispatches over.
    /// `ValidationSummary` and `CompatibilityLedger` are universal; the rest are subsets.
    /// Feature-unique bodies (e.g. 159 promotion, 160 throughput, 161 lane-ledger) are NOT modeled
    /// here — they stay explicit renderers (FR-007).
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

    /// Per-feature scalar configuration that feeds the generic renderers and the shared
    /// performance/readiness runner. A field is populated only where the value is a shared scalar
    /// (≥2 features) and routing it through config reduces net lines; otherwise it stays `None`/`[]`
    /// and the value remains inline in the feature's explicit body (FR-007 / SC-005).
    type FeatureConfig =
        { PolicyId: string option
          AcceptedProfileId: string option
          RequiredScenarioIds: string list }

    /// One harness feature, the unit the renderer/CLI table/tests all iterate.
    type FeatureDescriptor =
        { Id: int
          Slug: string
          CliAliases: string list
          Variants: Set<ReportVariant>
          RequiredHeaders: string list
          Config: FeatureConfig }

    /// Path/predicate helpers derived from the descriptor. The path helpers reproduce the exact
    /// byte-strings the hand-declared `feature###…` constants produced today (C-FD-2).
    module FeatureDescriptor =
        val readinessDirectory: FeatureDescriptor -> string
        val variantDirectory: ReportVariant -> FeatureDescriptor -> string
        val compatibilityLedgerPath: FeatureDescriptor -> string
        val validationSummaryPath: FeatureDescriptor -> string
        val packageValidationPath: FeatureDescriptor -> string
        val regressionValidationPath: FeatureDescriptor -> string
        val supports: ReportVariant -> FeatureDescriptor -> bool
        val tryByAlias: string -> FeatureDescriptor option

    /// The 12 features at HEAD (148,149,152,153,154,155,156,157,158,159,160,161) in
    /// `Compositor.fs` declaration order.
    val catalog: FeatureDescriptor list

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

    /// Per-descriptor override points for the genuinely divergent variant bodies (US2, FR-003).
    /// `None` = the generic template suffices; `Some f` = this feature/variant needs a bespoke body.
    /// Each hook returns only the feature-specific report fragments; the generic renderer
    /// (`Compositor.Render`) assembles them with the shared frame, so output stays byte-identical to
    /// the prior per-feature function. Functions are concrete over plain string fragments (Principle
    /// III — no SRTP, and no dependency on the `Compositor.Types` records that compile after this).
    type FeatureRenderHooks =
        { PackageValidation: (unit -> string * string * string list) option
          RegressionValidation: (unit -> string * string list) option }

    /// All-`None` hooks — the default for a descriptor whose variants all use the generic template.
    val noRenderHooks: FeatureRenderHooks

    /// One harness feature, the unit the renderer/CLI table/tests all iterate.
    type FeatureDescriptor =
        { Id: int
          Slug: string
          CliAliases: string list
          Variants: Set<ReportVariant>
          RequiredHeaders: string list
          Config: FeatureConfig
          Renderers: FeatureRenderHooks }

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

    /// Raised when the catalog violates an SSOT invariant (duplicate alias) or a lookup misses (FR-011).
    exception CatalogError of string

    /// CLI aliases shared by ≥2 descriptors (case-insensitive); empty when well-formed. Checked at
    /// module load — a duplicate fails loud before any feature runs.
    val duplicateAliases: unit -> string list

    /// Exhaustive lookup by feature id; throws `CatalogError` on a missing row (never a placeholder).
    val descriptorById: int -> FeatureDescriptor

    /// Exhaustive lookup by any CLI alias (id / `feature<N>` / slug); throws `CatalogError` if unknown.
    val descriptorByAlias: string -> FeatureDescriptor

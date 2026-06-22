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

    // Per-descriptor override points for the genuinely divergent variant bodies (US2, FR-003). A
    // field is `None` when the generic template suffices, `Some f` when this feature/variant needs a
    // bespoke body. Each hook returns only the *feature-specific* report fragments; the generic
    // renderer (`Compositor.Render`) assembles them with the shared frame, so output stays
    // byte-identical to the prior per-feature function. Functions are kept concrete over plain
    // string fragments (Principle III — no SRTP/generic-'msg, and no dependency on the
    // `Compositor.Types` report records which compile after this module).
    type FeatureRenderHooks =
        { // Package-validation: (checks-section header, surface-section header, surface bullets).
          PackageValidation: (unit -> string * string * string list) option
          // Regression-validation: (section header, bullets).
          RegressionValidation: (unit -> string * string list) option }

    let noRenderHooks =
        { PackageValidation = None
          RegressionValidation = None }

    type FeatureDescriptor =
        { Id: int
          Slug: string
          CliAliases: string list
          Variants: Set<ReportVariant>
          RequiredHeaders: string list
          Config: FeatureConfig
          Renderers: FeatureRenderHooks }

    let private emptyConfig =
        { PolicyId = None
          AcceptedProfileId = None
          RequiredScenarioIds = [] }

    // The shared accepted-profile id threaded across the timing/perf features (constants
    // feature156AcceptedProfileId and its aliases in Compositor.fs).
    let private sharedAcceptedProfileId = "probe-08a47c01"

    // Static `# Feature <N> …` report-header titles each feature's renderers emit (the byte-identical
    // CI-grepped / test-asserted literals — see readiness/rehoming-map.md). Dynamic interpolated
    // titles (`Scenario: {…}`, `Excluded …: {…}`) stay inline and are not listed here.
    let private hdr (n: int) (titles: string list) =
        titles |> List.map (fun t -> sprintf "# Feature %d %s" n t)

    let private descriptor id slug variants requiredHeaders config =
        { Id = id
          Slug = slug
          CliAliases = [ string id; "feature" + string id; slug ]
          Variants = Set.ofList variants
          RequiredHeaders = requiredHeaders
          Config = config
          Renderers = noRenderHooks }

    // Package/regression-validation render hooks for the catalog-collapsed features (156-161). The
    // string fragments are moved verbatim from the former `match featureNum` in
    // `Compositor.renderPackageValidation`/`renderRegressionValidation` (T023, FR-004) — output is
    // byte-identical; dispatch is now `descriptorById`-keyed.
    let private validationHooks pkg reg =
        { PackageValidation = Some pkg
          RegressionValidation = Some reg }

    let private renderHooksFor id =
        match id with
        | 156 ->
            validationHooks
                (fun () ->
                    "## Surface and Package Checks", "## Public Surface",
                    [ "- SkiaViewer and Testing surface baselines are refreshed when `.fsi` public timing helpers change."
                      "- Package FSI transcript coverage is recorded under `readiness/fsi/`." ])
                (fun () ->
                    "## Safety Boundary",
                    [ "- Feature 155 correctness acceptance remains the P7 safety baseline."
                      "- Unsupported-host validation remains fail-closed with zero accepted performance artifacts."
                      "- Shipped P7 performance claim remains `performance-not-accepted`." ])
        | 157 ->
            validationHooks
                (fun () ->
                    "## Validation Runs", "## Public Surface",
                    [ "- SkiaViewer, Testing, and harness signatures include the Feature 157 damage-readiness surface."
                      "- FSI authoring evidence is recorded under `fsi/`." ])
                (fun () ->
                    "## Preservation",
                    [ "- Feature 155 proof and parity acceptance remains the correctness gate."
                      "- Feature 156 timing remains context-only and `performance-not-accepted`."
                      "- Unsupported-host validation remains fail-closed with zero accepted partial-redraw artifacts." ])
        | 158 ->
            validationHooks
                (fun () ->
                    "## Validation Runs", "## Package Surface",
                    [ "- No Testing or SkiaViewer package-visible helper surface was added for Feature 158."
                      "- Feature 158 FSI evidence exercises observable harness command authoring and no-new-helper compatibility notes."
                      "- Package identity remains unchanged." ])
                (fun () ->
                    "## Preservation",
                    [ "- Feature 155 proof and parity acceptance remains the correctness gate."
                      "- Feature 156 timing remains context-only and available for comparison."
                      "- Feature 157 damage-scissored no-clear readiness remains accepted for the current stable profile."
                      "- Unsupported-host validation remains fail-closed with zero accepted proof artifacts and zero accepted performance artifacts."
                      "- Shipped P7 performance claim remains `performance-not-accepted`." ])
        | 159 ->
            validationHooks
                (fun () ->
                    "## Validation Runs", "## Package Surface",
                    [ "- Controls and SkiaViewer Feature 159 implementation details remain internal."
                      "- Testing package exposes `Feature159Readiness` for generated-product/package validation."
                      "- FSI transcripts cover content/placement identity, promotion command authoring, and readiness helper authoring." ])
                (fun () ->
                    "## Preservation",
                    [ "- Feature 155 proof capture remains the correctness gate."
                      "- Feature 157 no-clear damage readiness remains preserved."
                      "- Feature 158 readback-free timing separation remains preserved."
                      "- Unsupported-host output remains fail-closed with zero accepted Feature 159 reuse or promotion artifacts."
                      "- Shipped P7 performance claim remains `performance-not-accepted`." ])
        | 160 ->
            validationHooks
                (fun () ->
                    "## Validation Runs", "## Package Surface",
                    [ "- Rendering.Harness exposes Feature 160 focused-lane and readiness signatures."
                      "- Testing package exposes `Feature160ThroughputReadiness` for package validation."
                      "- FSI transcripts cover compositor performance authoring and throughput readiness helper authoring." ])
                (fun () ->
                    "## Preservation",
                    [ "- Feature 155 proof correctness remains preserved."
                      "- Feature 157 no-clear damage readiness remains preserved."
                      "- Feature 158 readback-free timing separation and required scenario set remain preserved."
                      "- Feature 159 reuse/promotion readiness remains a separate performance-claim gate."
                      "- Unsupported-host output remains fail-closed with zero accepted same-profile performance artifacts."
                      "- Public-surface drift is recorded in Feature 160 FSI evidence." ])
        | 161 ->
            validationHooks
                (fun () ->
                    "## Validation Runs", "## Package Surface",
                    [ "- Rendering.Harness exposes Feature 161 host-lane ledger signatures, command, and readiness rendering."
                      "- Testing package exposes `Feature161HostLaneReadiness` for package validation."
                      "- FSI transcripts cover compositor host-lane authoring and host-lane readiness helper authoring."
                      "- FSI compositor transcript: `compositor-host-lane-authoring.fsx`."
                      "- FSI helper transcript: `feature161-host-lane-readiness-authoring.fsx`." ])
                (fun () ->
                    "## Preservation",
                    [ "- Feature 155 proof correctness remains preserved."
                      "- Feature 157 no-clear damage-scissored readiness remains preserved."
                      "- Feature 158 readback-free timing separation remains preserved."
                      "- Feature 159 reuse/promotion evidence remains a separate performance-claim gate."
                      "- Feature 160 throughput evidence remains accepted only within its focused validation boundary."
                      "- Full-redraw fallback and unsupported-host fail-closed behavior remain unchanged."
                      "- Public-surface drift is recorded in Feature 161 FSI evidence." ])
        | _ -> noRenderHooks

    let catalog: FeatureDescriptor list =
        [ descriptor 148 "148-compositor-live-integration"
              [ ValidationSummary; CompatibilityLedger; LiveProof; Parity; Reuse; Snapshot; Timing ]
              (hdr 148 [ "Live Preservation Proof"; "Damage Parity"; "Content/Placement Reuse"; "Snapshot Lifecycle"; "Timing Probe"; "Validation Summary"; "Compatibility Ledger" ])
              emptyConfig
          descriptor 149 "149-complete-compositor-p7"
              [ ValidationSummary; CompatibilityLedger; LiveProof; Parity; Reuse; Snapshot; Timing ]
              (hdr 149 [ "Live Compositor Proof"; "Damage Parity"; "Reuse Evidence"; "Snapshot Lifecycle"; "Timing Probe"; "Validation Summary"; "Compatibility Ledger" ])
              emptyConfig
          descriptor 152 "152-compositor-live-proof"
              [ ValidationSummary; CompatibilityLedger; LiveProof; Parity; Timing ]
              (hdr 152 [ "Live Proof Run Set"; "Damage-Scoped Live Parity"; "Timing Claim Decision"; "P7 Readiness Summary"; "Compatibility Ledger" ])
              emptyConfig
          descriptor 153 "153-compositor-proof-interpreter"
              [ ValidationSummary; CompatibilityLedger; LiveProof; PackageValidation; RegressionValidation; ProofSet ]
              (hdr 153 [ "Live Proof Interpreter"; "Proof-Set Decision"; "Compositor Proof Interpreter Readiness"; "Compatibility Ledger" ])
              emptyConfig
          descriptor 154 "154-compositor-proof-acceptance"
              [ ValidationSummary; CompatibilityLedger; LiveProof; Parity; Timing; PackageValidation; RegressionValidation; ProofSet ]
              (hdr 154 [ "Compositor Proof Acceptance"; "Same-Profile Damage-Scoped Parity"; "Timing Decision"; "Proof-Set Acceptance"; "P7 Readiness Verdict"; "Compatibility Ledger" ])
              emptyConfig
          descriptor 155 "155-native-proof-capture"
              [ ValidationSummary; CompatibilityLedger; LiveProof; Parity; Timing; PackageValidation; RegressionValidation; ProofSet ]
              (hdr 155 [ "Native Proof Capture"; "Same-Profile Damage-Scoped Parity"; "Timing Decision"; "Native Proof Set"; "P7 Closeout Verdict"; "Compatibility Ledger" ])
              emptyConfig
          descriptor 156 "156-same-profile-timing"
              [ ValidationSummary; CompatibilityLedger; PackageValidation; RegressionValidation; UnsupportedHost ]
              (hdr 156 [ "Readiness Summary"; "Same-Profile Timing Summary"; "Unsupported Host Timing"; "Compatibility Ledger" ])
              { emptyConfig with
                  PolicyId = Some "same-profile-live-threshold-v2"
                  AcceptedProfileId = Some sharedAcceptedProfileId }
          descriptor 157 "157-no-clear-damage-scissor"
              [ ValidationSummary; CompatibilityLedger; Parity; PackageValidation; RegressionValidation; UnsupportedHost ]
              (hdr 157 [ "Readiness Summary"; "Damage Attempt"; "Damage Summary"; "Fallback"; "Parity"; "Unsupported Host"; "Compatibility Ledger" ])
              { emptyConfig with AcceptedProfileId = Some sharedAcceptedProfileId }
          descriptor 158 "158-separate-proof-timing"
              [ ValidationSummary; CompatibilityLedger; PackageValidation; RegressionValidation; UnsupportedHost ]
              (hdr 158 [ "Readiness Summary"; "Readback-Free Timing Summary"; "Proof/Probe Evidence"; "Unsupported Host Timing"; "Compatibility Ledger" ])
              { emptyConfig with
                  PolicyId = Some "readback-free-timing-v1"
                  AcceptedProfileId = Some sharedAcceptedProfileId }
          descriptor 159 "159-layer-promotion-keys"
              [ ValidationSummary; CompatibilityLedger; PackageValidation; RegressionValidation; UnsupportedHost ]
              (hdr 159 [ "Readiness Summary"; "Layer Promotion Summary"; "Promotion Attempt"; "Counter Evidence"; "Unsupported Host Promotion"; "Compatibility Ledger" ])
              { emptyConfig with
                  PolicyId = Some "layer-promotion-v1"
                  AcceptedProfileId = Some sharedAcceptedProfileId }
          descriptor 160 "160-performance-validation-throughput"
              [ ValidationSummary; CompatibilityLedger; PackageValidation; RegressionValidation; UnsupportedHost ]
              (hdr 160 [ "Readiness Summary"; "Focused Throughput Summary"; "Focused Throughput Iteration"; "Full Validation"; "Unsupported Host Throughput"; "Compatibility Ledger" ])
              { emptyConfig with
                  PolicyId = Some "focused-throughput-v1"
                  AcceptedProfileId = Some sharedAcceptedProfileId }
          descriptor 161 "161-host-performance-lane-ledger"
              [ ValidationSummary; CompatibilityLedger; PackageValidation; RegressionValidation; UnsupportedHost ]
              (hdr 161 [ "Readiness Summary"; "Host Performance Lane Ledger"; "Lane Ledger Entry"; "Host Facts"; "Full Validation"; "Unsupported Host Lane Ledger"; "Compatibility Ledger" ])
              { emptyConfig with
                  PolicyId = Some "host-lane-ledger-v1"
                  AcceptedProfileId = Some sharedAcceptedProfileId } ]
        |> List.map (fun d -> { d with Renderers = renderHooksFor d.Id })

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

    // Fail-loud SSOT integrity checks (FR-011). A duplicate CLI alias or a missing-id lookup is a
    // build/first-use failure, never a silent last-wins or empty report.

    /// Raised when the catalog violates an SSOT invariant (duplicate alias) or a lookup misses.
    exception CatalogError of string

    /// Aliases shared by ≥2 descriptors, case-insensitively. Empty when the catalog is well-formed.
    let duplicateAliases () =
        catalog
        |> List.collect (fun d -> d.CliAliases |> List.map (fun a -> a.ToLowerInvariant()))
        |> List.countBy id
        |> List.choose (fun (alias, n) -> if n > 1 then Some alias else None)

    // Forced at module load: a duplicate alias fails loud immediately, before any feature runs.
    let private assertUniqueAliases =
        match duplicateAliases () with
        | [] -> ()
        | dups -> raise (CatalogError(sprintf "duplicate CLI alias(es) in FeatureCatalog: %s" (String.concat ", " dups)))

    /// Exhaustive lookup by feature id. Throws `CatalogError` on a missing row (FR-011, edge
    /// "Feature not in the catalog") — never returns a placeholder/empty descriptor.
    let descriptorById (id: int) =
        match catalog |> List.tryFind (fun d -> d.Id = id) with
        | Some d -> d
        | None -> raise (CatalogError(sprintf "no FeatureCatalog descriptor for feature id %d" id))

    /// Exhaustive lookup by any CLI alias (id / `feature<N>` / slug). Throws on an unknown alias.
    let descriptorByAlias (value: string) =
        match FeatureDescriptor.tryByAlias value with
        | Some d -> d
        | None -> raise (CatalogError(sprintf "no FeatureCatalog descriptor for alias '%s'" value))

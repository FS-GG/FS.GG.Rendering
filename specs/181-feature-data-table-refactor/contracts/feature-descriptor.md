# Contract — `FeatureDescriptor` & catalog (internal harness surface)

**Scope**: New module `FeatureCatalog` in `tools/Rendering.Harness/` (FR-008: internal-tool surface, no
shipped `FS.GG.UI.*` package change). Declared in `FeatureCatalog.fsi`; this contract is the intent the
`.fsi` must realize. See [data-model.md](../data-model.md) for the entity shapes.

## Public surface (harness-internal)

```fsharp
module FeatureCatalog

type ReportVariant =
    | ValidationSummary | CompatibilityLedger
    | PackageValidation | RegressionValidation | UnsupportedHost
    | Timing | LiveProof | Parity | ProofSet | Reuse | Snapshot

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

module FeatureDescriptor =
    val readinessDirectory      : FeatureDescriptor -> string
    val variantDirectory        : ReportVariant -> FeatureDescriptor -> string
    val compatibilityLedgerPath : FeatureDescriptor -> string
    val validationSummaryPath   : FeatureDescriptor -> string
    val supports                : ReportVariant -> FeatureDescriptor -> bool
    val tryByAlias              : string -> FeatureDescriptor option   // matches any CliAliases entry (OrdinalIgnoreCase)

val catalog : FeatureDescriptor list   // 12 entries, declaration order preserved
```

## Behavioral contract

| # | Guarantee |
|---|---|
| C-FD-1 | `catalog` contains exactly the 12 features at HEAD (148,149,152,153,154,155,156,157,158,159,160,161), in the same order they are declared in `Compositor.fs` today. |
| C-FD-2 | For every descriptor `d` and variant `v ∈ d.Variants`, `variantDirectory v d` and the path helpers return **byte-identical** strings to the hand-declared `feature###…` constant they replace. This is the constant-drift gate (spec Edge Cases). |
| C-FD-3 | `ValidationSummary` and `CompatibilityLedger` are in every descriptor's `Variants`. |
| C-FD-4 | `tryByAlias "158"`, `tryByAlias "feature158"`, and `tryByAlias "158-compositor-performance"` all resolve to the feature-158 descriptor (replicates the existing `isFeature158` acceptance set exactly). |
| C-FD-5 | No type or value in this module is referenced from any `src/` (shipped) project — it is consumed only by `Compositor`, `Cli`, and the harness/package tests. |

## Verification

- **C-FD-2** is verified by the byte-stability diff (regenerated `readiness/**` identical to baseline) —
  if any derived path differs, a readiness artifact lands at a different location and the diff fails.
- **C-FD-1/C-FD-3/C-FD-4** are verified by direct assertions in `tests/Rendering.Harness.Tests/` (a small
  catalog-shape test: count, universal-variant presence, alias resolution per feature).
- **C-FD-5** is verified by the unchanged `FS.GG.UI.*` surface baselines (FR-008) — no shipped `.fsi` gains
  a descriptor symbol.

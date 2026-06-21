# Phase 1 Data Model — Per-Feature Data-Table Refactor

**Feature**: 181 | **Date**: 2026-06-21

This model defines the single source of truth — `FeatureDescriptor` — and the supporting `ReportVariant`
vocabulary and descriptor catalog. All shapes are **internal to `tools/Rendering.Harness/`** (FR-008: no
shipped package surface). Field names are illustrative of intent; the implementing `.fsi` is authoritative.

---

## Entity: `ReportVariant`

The named kinds of per-feature output. A discriminated union (gives compiler-exhaustiveness when the
generic renderer dispatches on variant). Universal variants apply to every feature; the rest are subsets
(see [research.md](./research.md) R2).

```fsharp
type ReportVariant =
    // universal (every feature)
    | ValidationSummary
    | CompatibilityLedger
    // broad subsets
    | PackageValidation        // 153–161
    | RegressionValidation     // 153–161
    | UnsupportedHost          // 156–161
    | Timing                   // 148,149,152,154,155,156,157,158,160
    | LiveProof                // 148,149,152,153,154,155
    | Parity                   // 148,149,152,154,155,157
    | ProofSet                 // 153,154,155
    | Reuse                    // 148,149
    | Snapshot                 // 148,149
    // feature-unique families stay explicit (FR-007) — NOT modeled as shared variants
```

**Validation rules**:
- A descriptor's `Variants` set MUST be a subset of the modeled cases. Feature-unique bodies (e.g. 159's
  counter/promotion, 161's lane-ledger) are **not** added as `ReportVariant` cases — they remain explicit
  feature-specific renderers (FR-007), referenced by the descriptor only if needed for dispatch.
- `ValidationSummary` and `CompatibilityLedger` MUST be present in every descriptor's `Variants`.

---

## Entity: `FeatureDescriptor`

The single record describing one harness feature. Source of truth for the generic renderer (US1), the CLI
command table (US2), and the data-driven compatibility tests (US3).

```fsharp
type FeatureDescriptor =
    { /// Numeric id, e.g. 158.
      Id: int
      /// Full slug / spec id, e.g. "158-compositor-performance".
      Slug: string
      /// CLI aliases that select this feature ("158", "feature158", Slug). Drives isFeature### replacement.
      CliAliases: string list
      /// The report variants this feature supports (subset of ReportVariant; see Variants rules).
      Variants: Set<ReportVariant>
      /// Required markdown headers / stable tokens asserted by the compatibility tests (US3 data).
      RequiredHeaders: string list
      /// Optional per-feature config consumed by the generic renderers (timing/parity/policy ids).
      Config: FeatureConfig }
```

### Derived (NOT stored) — directory & path constants

The 262 per-feature constants are **derived** from `Id`/`Slug`, replacing the hand-declared quintets
(`feature###ReadinessDirectory`, `…ParityDirectory`, `…TimingDirectory`, `…CompatibilityLedgerPath`,
`…ValidationSummaryPath`). One function family on the descriptor:

```fsharp
module FeatureDescriptor =
    val readinessDirectory : FeatureDescriptor -> string   // Path.Combine("specs", Slug, "readiness")
    val variantDirectory   : ReportVariant -> FeatureDescriptor -> string   // e.g. …/timing, …/parity
    val compatibilityLedgerPath : FeatureDescriptor -> string
    val validationSummaryPath   : FeatureDescriptor -> string
```

**Validation rules**:
- `Id` unique across the catalog; `Slug` unique; `Slug` begins with `string Id + "-"`.
- Derived paths MUST reproduce the **exact byte strings** the hand-declared constants produced today
  (verified by the byte-stability diff — the directory names already follow `specs/<slug>/readiness/<variant>`).
- `CliAliases` MUST include at least `string Id`, `"feature" + string Id`, and `Slug` (the three forms
  the existing `isFeature###` predicates accept), preserving CLI selection behavior exactly.

---

## Entity: `FeatureConfig`

Per-feature scalar configuration that feeds the generic renderers and the shared performance/readiness
runner — the values currently scattered as `feature###PolicyId` / `…AcceptedProfileId` / required-scenario
lists. Modeled as a record of optionals so features lacking a field carry `None` rather than forcing a shape.

```fsharp
type FeatureConfig =
    { PolicyId: string option
      AcceptedProfileId: string option
      RequiredScenarioIds: string list
      // …additional per-feature scalars surfaced only as the collapse proves them shared
    }
```

**Validation rule**: a field is added to `FeatureConfig` only when ≥2 features share it *and* routing it
through config reduces net lines; a value used by exactly one feature stays inline in that feature's
explicit body (FR-007 / SC-005).

---

## Entity: descriptor catalog

The ordered list consumed by renderer, CLI table, and tests.

```fsharp
val catalog : FeatureDescriptor list   // 12 entries: 148,149,152,153,154,155,156,157,158,159,160,161
```

**Validation rules**:
- Exactly the 12 features at HEAD; order matches the existing `Compositor.fs` declaration order (keeps
  any order-dependent output stable).
- Adding a feature = appending one entry (SC-001). The generic renderer, CLI table, and `testList` all
  iterate the catalog, so a new entry is automatically covered with no new function/handler/test file.

---

## Relationships

```
catalog : FeatureDescriptor list
   │  (each)
   ├── FeatureDescriptor ──has── Variants : Set<ReportVariant>
   │          │
   │          ├── consumed by ── Compositor generic renderer  (US1) ─► byte-identical readiness/**
   │          ├── consumed by ── Cli descriptor-keyed table    (US2) ─► byte-identical stdout/stderr/exit
   │          └── consumed by ── Package.Tests data-driven list (US3) ─► equivalent coverage
   │
   └── feature-unique bodies (NOT in Variants) ── retained explicit (FR-007), recorded with rationale
```

## State transitions

None. All entities are immutable data; the renderers and dispatch table are pure functions over them. The
only state is the existing file I/O (writing readiness artifacts), which is unchanged.

## Mapping to requirements

| Entity / rule | Requirement |
|---|---|
| `FeatureDescriptor` single record | FR-001, SC-001 |
| Derived directory/path functions | FR-002 (constant quintets → data), Constant-drift edge case |
| `Variants : Set<ReportVariant>` | FR-001, "variants not shared across features" edge case |
| Generic renderer over catalog | FR-002, SC-003 |
| `CliAliases` + catalog → command table | FR-004, SC-002 |
| `RequiredHeaders` → data-driven testList | FR-005, SC-004 |
| Append-one-entry coverage | FR-006, SC-001 |
| Feature-unique bodies excluded from collapse | FR-007, SC-005 |
| No type leaves the harness | FR-008 |

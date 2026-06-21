# Contract — generic descriptor-driven renderer (US1)

**Scope**: `tools/Rendering.Harness/Compositor.(fsi/fs)`. Replaces the structurally-repeated subset of the
97 `renderFeature…` functions with a generic path over `FeatureCatalog`; retains genuinely-divergent
bodies explicitly (FR-007). No shipped surface change (FR-008).

## Surface (harness-internal)

```fsharp
// One generic entry point per universally-collapsible variant family. Signature shape:
val renderValidationSummary   : FeatureDescriptor -> 'Payload -> string
val renderCompatibilityLedger : FeatureDescriptor -> string
// …additional renderVariant functions only where the family proves collapsible (see C-GR-2).
```

The exact payload typing is settled during implementation (some families share a payload type, some need
a small per-feature projection). The constraint is the behavioral contract below, not a specific generic
signature.

## Behavioral contract

| # | Guarantee |
|---|---|
| C-GR-1 | For **every** existing feature and **every** variant it supports, the generic renderer emits output **byte-for-byte identical** to the pre-refactor `renderFeatureNNN<Variant>` function (FR-003, SC-002). |
| C-GR-2 | A variant family is routed through the generic path **only if** (a) C-GR-1 holds AND (b) the collapse does not increase net source lines for that family (SC-005). Families failing either test stay explicit (FR-007). |
| C-GR-3 | No new `renderFeatureNNN…` function is added for a feature whose variants are all generic — appending a catalog entry yields full standard-variant coverage with zero new renderer functions (SC-001). |
| C-GR-4 | The count of surviving `renderFeature…` functions drops from ~114/97 to the count of genuinely-divergent retained bodies; the target is "the great majority collapsed" (SC-003), not a fixed number. |
| C-GR-5 | Per-feature directory/path constants are sourced from `FeatureDescriptor` helpers, not hand-declared; no constant value changes (constant-drift edge case). |

## Collapse / exclude decision (per variant family)

For each variant family, implementation MUST record one of:
- **COLLAPSED** — routed through the generic renderer; byte-identical (C-GR-1) and net-line-reducing (C-GR-2).
- **RETAINED (FR-007)** — left explicit, with a one-line reason: *divergent content*, *single-instance*
  (feature-unique), or *net-line-increase-if-collapsed*. Recorded in the plan's Implementation Outcome
  and/or alongside the body.

Expected from research (R2/R3), to be confirmed by measurement:
- Strong COLLAPSE candidates: the universal skeleton of `CompatibilityLedger`, the directory-constant
  derivation, the `ValidationSummary` section scaffolding where link/checklist/decision data is short.
- Likely RETAIN: feature-unique variants (159 counter/promotion, 160 throughput, 161 lane-ledger, 158
  proof-probe), and any `ValidationSummary`/ledger whose per-feature prose is as long as the body it
  would replace.

## Verification

- C-GR-1 / C-GR-5: byte-diff regenerated `readiness/**` vs `baseline/` (the acceptance gate).
- C-GR-3 / C-GR-4: `grep -c 'let renderFeature' Compositor.fs` before/after, plus a
  `Rendering.Harness.Tests` assertion that each catalog entry renders its declared variants without a
  dedicated function (e.g. via the generic dispatch).
- C-GR-2 / FR-007: per-family net-line delta recorded in the plan's Implementation Outcome.

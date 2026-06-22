# Phase 1 Data Model — Harness Data-Table Refactor

Entities are F# types inside `tools/Rendering.Harness/`. No persistence schema — the "data" is the
in-code descriptor table (SSOT) plus the artifacts it drives.

## FeatureDescriptor (extend — already exists in `FeatureCatalog.fs`)

The SSOT row for one harness feature; the unit the renderer, CLI table, and tests iterate.

| Field | Type | Status | Notes |
|---|---|---|---|
| `Id` | `int` | existing | Feature number (148,149,152,153,154,155,156–161). |
| `Slug` | `string` | existing | e.g. `156-same-profile-timing`; drives all directory paths. |
| `CliAliases` | `string list` | existing | `[string id; "feature"+string id; slug]`; the CLI command table keys on these. |
| `Variants` | `Set<ReportVariant>` | existing | Selects which reports the generic renderer produces. |
| `RequiredHeaders` | `string list` | **populate** | Currently `[]` for all 12 rows — fill from the header literals inline in `Compositor.fs` (US1). |
| `Config` | `FeatureConfig` | existing | `PolicyId` / `AcceptedProfileId` / `RequiredScenarioIds`. |
| `Renderers` | `FeatureRenderHooks` | **add** | Per-descriptor override points for the divergent ~20% (US2). |

**Validation / invariants** (FR-011, edge cases):
- No two descriptors may share a `CliAlias` (duplicate-alias check; build/first-use failure, not last-wins).
- Every `Id` referenced by render/CLI code must resolve to a descriptor (exhaustive lookup; fail loud, never silent-skip).
- Every `ReportVariant` in `Variants` must have a generic template **or** a matching `Renderers` hook (fail loud, not empty/garbage report).

## ReportVariant (existing — unchanged)

Enumerated report kind: `ValidationSummary | CompatibilityLedger | PackageValidation |
RegressionValidation | UnsupportedHost | Timing | LiveProof | Parity | ProofSet | Reuse | Snapshot`.
`ValidationSummary`/`CompatibilityLedger` are universal; the rest are subsets per descriptor.

## FeatureConfig (existing — unchanged)

`{ PolicyId: string option; AcceptedProfileId: string option; RequiredScenarioIds: string list }`.
A field is populated only where the value is a shared scalar (≥2 features); otherwise `None`/`[]` and
the value stays inline in the feature's explicit body.

## FeatureRenderHooks (new — US2)

Per-descriptor override points for feature/variant combinations whose output genuinely diverges from
the generic template (e.g. 159 promotion, 160 throughput, 161 lane-ledger). A record of `option`
function fields; `None` = use the generic template, `Some f` = use the override. Exact field set is
discovered while parametrizing (one optional hook per divergent variant body); keep concrete over the
harness's existing report record types (Principle III — no SRTP/generic-`'msg`).

## Compositor module re-homing (US2 — Pattern E split)

`Compositor.fs` (5,512 lines, one `module Compositor`) splits into four sibling modules. The DU/types
the tests pattern-match on (`ArtifactPublished`, `HostProfile`, `TierEvaluated`, `ParityPassed`,
`PresentProof`, `Ready`, `Rejected`, `Limited`, …) move to `Compositor.Types` and must remain
reachable (re-exported or referenced) for retargeted tests (FR-010).

| Module | Owns | Replaces |
|---|---|---|
| `Compositor.Types` | the ~60 type/DU defs | (extraction only) |
| `Compositor.Config` | descriptor-derived directory/header/profile lookups | the 110 `*ReadinessDirectory` + `feature###*Path`/`Id` constants |
| `Compositor.FeatureState` | parametric `init`/`update`/`status` over a descriptor | the 6 per-feature state machines |
| `Compositor.Render` | generic `renderFeature` + `FeatureRenderHooks` dispatch | the 85 `renderFeature*` fns; the two feature-number `match` renderers → `Id` lookups |

Each new module carries a curated `.fsi` (Constitution II). `Compositor.fsi` is rewritten — the 85
`renderFeature*` + 110 directory/`Id` vals are removed (SC-003), replaced by the parametric +
descriptor surface.

## runReadiness workflow (US3)

`runReadiness : FeatureDescriptor -> exitCode`. Pipeline: `probe → mkdirs (FeatureDescriptor.* paths)
→ build reports → write the N variant files (d.Variants) → render`. Backed by a command table
`CliAliases → runReadiness d`. Replaces the per-feature `runFeature*Cmd` handlers. Observable
contract (artifacts, paths, exit codes, unknown-alias error) preserved (FR-007).

## Lane-execution units (US4)

Carved out of `ValidationLanes.runLane`, reusing the file's existing MVU edge:
- `ProcessRunner` — spawn + stdout/stderr/exit.
- `TimeoutManager` — wall-clock + no-progress timeout → terminate (preserves `TimedOut` vs `NoProgressTimedOut`).
- `OutputBuffer` — captured-output accumulation.
- `orchestrator` — composes the three into the unchanged `LaneResult` (so `runLanes` and callers are untouched).

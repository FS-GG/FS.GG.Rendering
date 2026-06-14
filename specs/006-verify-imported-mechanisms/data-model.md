# Phase 1 Data Model: Audit Entities

The audit is documentation- and evidence-driven; its "data model" is the shape of the records that populate the inventory (`docs/audit/mechanism-inventory.md`) and the findings report (`docs/audit/mechanism-audit.md`). These are conceptual schemas realized as Markdown tables, not runtime types. Field-level schemas live in `contracts/`.

## Entity: Mechanism

One advertised behavioral/performance feature in the imported code.

| Field | Type | Notes |
|---|---|---|
| `id` | slug | stable handle, e.g. `picture-cache`, `incremental-layout` |
| `name` | text | human name |
| `category` | enum | `correctness` \| `performance` \| `timing` (a mechanism may carry claims in more than one — see Claim) |
| `source` | ref | `file.fsi:line` of the load-bearing signature |
| `wiredEntryPoint` | ref | where the live path invokes it (for present-but-dead detection, D5) |
| `oracleSeam` | text | how the audit bypasses/disables it (e.g. `MemoEnabled=false`, `PictureReplayCache.create false`) |
| `effectivenessCounter` | text | the `WorkReductionRecord`/`FrameMetrics`/`stats` field that proves work reduction |

Relationships: a Mechanism **has one or more** Claims.

## Entity: Claim

A single falsifiable assertion a Mechanism makes.

| Field | Type | Notes |
|---|---|---|
| `id` | slug | e.g. `picture-cache.parity`, `picture-cache.effectiveness` |
| `mechanismId` | ref | owning Mechanism |
| `kind` | enum | `correctness` \| `effectiveness` \| `determinism` \| `key-completeness` \| `liveness` \| `timing` |
| `statement` | text | restated as a verifiable sentence ("cache-on output equals cache-off output") |
| `advertisedSource` | enum | `documented` \| `inferred` — flag when the claim was inferred because no explicit claim existed (spec Assumption) |
| `verificationMethod` | enum | `discriminating-correctness` \| `counter-effectiveness` \| `adversarial` \| `harness-timing` |
| `status` | enum | `unverified` → terminal one of the Verification results |

State transitions: `unverified` → (`verified` \| `refuted` \| `inconclusive` \| `deferred`) once its Verification runs. No Claim may remain `unverified` at audit close (SC-002).

Relationships: a Claim **is checked by** exactly one Verification; a Claim **rolls up into** its Mechanism's Finding.

## Entity: Verification

An executed test or measurement bound to a Claim.

| Field | Type | Notes |
|---|---|---|
| `claimId` | ref | the Claim under test |
| `method` | enum | mirrors Claim.verificationMethod |
| `evidenceRef` | text | Expecto test name (`Audit: …`) and/or harness `run.json` path |
| `scenario` | text | the input/condition (e.g. "single localized change in 1000-node tree") |
| `baseline` | text | the disabled/bypassed comparison, where applicable |
| `result` | enum | `pass` \| `fail` \| `inconclusive` \| `skipped` |
| `discriminatingProof` | bool | for correctness methods: did the assertion go red when the mechanism was bypassed? (D2) |
| `margin` | text | for effectiveness: measured reduction vs baseline (e.g. "12/1000 remeasured") |
| `tier` | enum | `local-deterministic` \| `T1` \| `T2` \| `T3` — environment it ran in |
| `skipRationale` | text | required when `result = skipped`/`deferred`; names the missing capability + required tier (FR-011, Principle VI) |
| `synthetic` | bool + note | true ⇒ disclose substitute and reason (FR-012, Principle V) |

Validation rules:
- `result = pass` for a correctness method **requires** `discriminatingProof = true`.
- `result = skipped/deferred` **requires** non-empty `skipRationale`.
- An effectiveness `pass` **requires** a recorded `margin` that beats the baseline by the stated threshold; equal-to-baseline ⇒ `fail` (no-op) or `inconclusive`.

Relationships: a Verification **produces evidence for** a Finding.

## Entity: Finding

The verdict for a Mechanism (rolled up from its Claims' Verifications).

| Field | Type | Notes |
|---|---|---|
| `mechanismId` | ref | one Finding per Mechanism |
| `verdict` | enum | `works-as-advertised` \| `benefit-overstated` \| `not-working-or-no-op` \| `unverifiable-here` |
| `severity` | enum | only if divergent: `correctness-defect` > `silent-no-op` > `overstated-benefit` > `cosmetic` |
| `evidenceRefs` | ref list | the Verifications backing the verdict |
| `recommendation` | enum + text | `fix` \| `simplify` \| `remove` \| `re-scope-claim` \| `defer-to-tier` (+ detail) |
| `reproduce` | text | exact command/filter to reproduce the verdict (SC-007) |

Verdict-derivation rules:
- all Claims `verified` ⇒ `works-as-advertised`.
- correctness `verified` but effectiveness `refuted` (no-op) ⇒ `not-working-or-no-op`, severity `silent-no-op`.
- effectiveness present but below advertised margin ⇒ `benefit-overstated`.
- any correctness `refuted` ⇒ `not-working-or-no-op`, severity `correctness-defect`.
- any Claim `deferred` (and none refuted) ⇒ `unverifiable-here` for that aspect, with required tier.

## Entity: Audit Report

The consolidated collection plus a coverage summary.

| Field | Type | Notes |
|---|---|---|
| `findings` | Finding list | one per Mechanism, all Mechanisms present (SC-001) |
| `coverage` | summary | counts: verified / overstated / no-op / unverifiable, and #correctness-defects, #silent-no-ops, #overstated (SC-008) |
| `generatedFrom` | text | the audit test run + harness runs the report was built from |

Realized as `docs/audit/mechanism-audit.md`; the per-claim detail lives in `docs/audit/mechanism-inventory.md`.

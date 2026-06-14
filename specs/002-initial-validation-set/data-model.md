# Phase 1 Data Model: Define the Initial Validation Set (Stage R3)

The "data" is the structure of the decision artifacts under `docs/validation/`. These are
documentation entities, not runtime types. Field formats are in [`contracts/`](./contracts/).

## Entity: Justification Record (`docs/validation/justification-records.md`)

One per candidate test project / check (per research Decision 1).

### Sub-entity: Record fields

| Field | Description | Rule |
|---|---|---|
| Candidate | Source test project or check name | Required; unique |
| Product contract | User-visible or package/template behavior it protects | Required |
| Failure mode | The concrete regression it catches | Required |
| Owner | Who maintains it when it fails/goes stale | Required |
| Frequency | `local` \| `ci` \| `release-only` \| `manual-advisory` | Required |
| Cost | Runtime / setup / flake risk / fixture size / maintenance | Required |
| Decision | `import-now` \| `defer` \| `archive` \| `rewrite-smaller` | Required |
| Note | Smaller-form description (if `rewrite-smaller`) or rationale | Required iff `rewrite-smaller` or open question |

- **Validation** (FR-001/002/007/008, SC-001): every reviewed candidate has all six core
  fields + a decision; decision ∈ the four values; coverage includes runtime unit tests,
  API surface-drift, package/consumer, template pack/install/instantiate, docs build,
  historical readiness reports, and generated fixtures; zero undecided.

## Entity: Initial Validation Set (`docs/validation/validation-set.md`)

The `import-now` records, partitioned by frequency.

- **Composed of**: frequency groups — Local inner loop, CI, Release-only, Manual/advisory.
- **Validation** (FR-003/004, SC-002/003):
  - Each member carries a frequency label.
  - The Local inner-loop group is explicitly enumerated and small enough for routine work.
  - Release-only checks are a separate group; no item appears in both local and release-only.

## Entity: Deferral/Archive Ledger (`docs/validation/deferral-ledger.md`)

Non-imported candidates.

### Sub-entity: Ledger entry

| Field | Description | Rule |
|---|---|---|
| Candidate | Source test/check name | Required |
| Status | `deferred` \| `archived` \| `rewrite-pending` | Required |
| Reason | Why not imported now | Required |
| Binding? | Always "no — not an active obligation" | Required marker |

- **Validation** (FR-005/010, SC-004): every non-imported candidate present with a reason
  and the non-binding marker; unresolved candidates appear here as `deferred` with options,
  never omitted.
- **Decision → Status mapping**: a justification-record Decision of `defer` / `archive` /
  `rewrite-smaller` maps to a ledger Status of `deferred` / `archived` / `rewrite-pending`
  respectively.

## Entity: Harness Justification Record (`docs/validation/harness.md`)

The rendering test harness as deliberate infrastructure.

| Field | Description | Rule |
|---|---|---|
| Item | Rendering test harness | Required |
| Classification | "deliberate infrastructure" (not an imported legacy test) | Required |
| Decision | Build at Stage R5; display-agnostic parts MAY scaffold earlier | Required |
| Tiers (reference) | T0/T1/T2/T3/T-uinput (see plan R5) | Optional |

- **Validation** (FR-006, SC-005): present, classified as infrastructure, clearly distinct
  from imported legacy tests.

## Cross-cutting invariants

- **No code/test/harness import** (FR-009): no entity includes copied source, imported
  tests, built harness code, or reintroduced governance machinery.
- **Open questions are explicit** (FR-010): an unresolvable candidate is a `deferred` ledger
  entry with options, never omitted.
- **Builds on R2**: candidate scope is bounded by `docs/product/module-map.md` (excluded
  modules → their tests are archive/exclude candidates).

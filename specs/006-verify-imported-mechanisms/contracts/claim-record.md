# Contract: Claim Record

The schema for one row of the claims inventory (`docs/audit/mechanism-inventory.md`). Every advertised mechanism contributes one or more Claim rows. This is the contract the inventory MUST conform to (FR-001, FR-002, SC-001).

## Fields

| Field | Required | Domain | Meaning |
|---|---|---|---|
| Mechanism | yes | mechanism `id` (slug) | owning mechanism |
| Claim ID | yes | `<mechanism>.<aspect>` slug | unique handle |
| Kind | yes | `correctness` \| `effectiveness` \| `determinism` \| `key-completeness` \| `liveness` \| `timing` | which dimension this asserts |
| Statement | yes | falsifiable sentence | the claim restated so a test could refute it |
| Source | yes | `path.fsi:line` | where the claim lives / is implied |
| Advertised | yes | `documented` \| `inferred` | `inferred` ⇒ no explicit claim existed; the audit inferred intent (spec Assumption) |
| Verification Method | yes | `discriminating-correctness` \| `counter-effectiveness` \| `adversarial` \| `harness-timing` | how it will be checked |
| Status | yes | `unverified` \| `verified` \| `refuted` \| `inconclusive` \| `deferred` | starts `unverified`; terminal at audit close (SC-002 forbids `unverified` at close) |

## Rules

- Every mechanism in the plan's mechanism table appears in at least one Claim row (SC-001).
- A claim whose statement is not falsifiable as written MUST be flagged (`Statement` annotated "needs sharpening") rather than recorded as a vague pass (spec US1 AS2).
- `timing` and `liveness` claims that require GL/X11 carry a `Verification Method` of `harness-timing` and are expected to resolve to `deferred` on headless runs.

## Example row

| Mechanism | Claim ID | Kind | Statement | Source | Advertised | Verification Method | Status |
|---|---|---|---|---|---|---|---|
| picture-cache | picture-cache.parity | correctness | Output with `PictureCacheEnabled=true` is byte-identical to `false` on the same scene | `RetainedRender.fsi:176` | documented | discriminating-correctness | unverified |
| picture-cache | picture-cache.effectiveness | effectiveness | On a repeated unchanged render, `PictureCacheHits` reaches steady-state ≫ 0 while `PictureCacheMisses`→0 | `RetainedRender.fsi:244` | documented | counter-effectiveness | unverified |

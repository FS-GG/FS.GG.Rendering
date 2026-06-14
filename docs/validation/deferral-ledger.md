# Deferral / archive ledger

> Migration Stage R3 deliverable. Every candidate from
> [`justification-records.md`](./justification-records.md) **not** imported now, captured so
> deferred coverage is discoverable. **Entries here are NOT active obligations and do not
> block routine product work.** (This "ledger" is a deferred-work list; it is unrelated to
> the removed synthetic-evidence ledger that FR-009 forbids reintroducing.)
>
> Status maps 1:1 from the justification Decision: `defer` → `deferred`, `archive` →
> `archived`, `rewrite-smaller` → `rewrite-pending`.

| Candidate | Status | Reason | Binding? |
|---|---|---|---|
| `Parity.Tests` | rewrite-pending | Heavy gallery visual-parity suite with image fixtures and flake risk; rewrite as Stage R5 harness T1 offscreen-readback checks rather than import as-is. | no — not an active obligation |
| `ControlsPreview.Harness` | deferred | A preview/inspection harness, not a legacy test; prior art to fold into the Stage R5 rendering harness. | no — not an active obligation |
| `Governance.Tests` | archived | Governance machinery removed by the constitution; not a product concern. | no — not an active obligation |
| `SkillSupport.Tests` | archived | `SkillSupport` module excluded in R2 (`docs/product/module-map.md`); not owned here. | no — not an active obligation |
| historical readiness reports (`readiness/`, `docs/testSpecs`) | archived | Superseded historical state; left in the source archive, not carried as active obligations. | no — not an active obligation |
| `Parity` golden-image fixtures | archived | Stale visual baselines that may not represent current output; regenerate under harness T1 when needed rather than import stale fixtures. | no — not an active obligation |

## Notes

- Every candidate whose justification Decision is `defer`, `archive`, or `rewrite-smaller`
  has a row above — nothing is silently dropped (FR-005, FR-010).
- No candidate required an open-question `deferred`-with-options entry; all were settleable.
  Should one arise later, it is added here as `deferred` with the options in its Reason.

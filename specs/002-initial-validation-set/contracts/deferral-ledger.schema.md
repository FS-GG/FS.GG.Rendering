# Contract: Deferral/Archive Ledger (`docs/validation/deferral-ledger.md`)

Captures every non-imported candidate so deferred coverage is discoverable but never an
active obligation.

## Required structure

1. A one-line statement: entries here are NOT active obligations and do not block routine
   product work. (This "ledger" is a deferred-work list; it is unrelated to the removed
   synthetic-evidence ledger that FR-009 forbids reintroducing.)
2. A table:

   | Candidate | Status | Reason | Binding? |
   |---|---|---|---|

## Field rules

- **Status**: one of `deferred`, `archived`, `rewrite-pending`. These map 1:1 from the
  justification-record **Decision**: `defer` → `deferred`, `archive` → `archived`,
  `rewrite-smaller` → `rewrite-pending`.
- **Reason**: required, non-empty.
- **Binding?**: always `no — not an active obligation`.
- Every candidate whose justification-record Decision is `defer`, `archive`, or
  `rewrite-smaller` MUST have a corresponding ledger entry.
- An unresolvable candidate appears here as `deferred` with options in the Reason (FR-010).

## Acceptance (maps to spec)

- [ ] Every non-`import-now` candidate appears with a reason. *(SC-004)*
- [ ] Every entry marked non-binding. *(FR-005, SC-004)*
- [ ] No candidate is silently dropped — defer-with-options instead. *(FR-010)*

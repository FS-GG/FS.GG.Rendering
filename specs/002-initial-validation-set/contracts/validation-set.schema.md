# Contract: Initial Validation Set (`docs/validation/validation-set.md`)

The bounded active set — the `import-now` candidates, partitioned by frequency. This is what
Stage R4 actually imports and what contributors run.

## Required structure

1. A one-paragraph statement that the set is deliberately small and that the local
   inner-loop tier is the default.
2. Four frequency groups (omit a group only if empty), each a list of members:
   - **Local inner loop** — fast, deterministic, default; run on every change.
   - **CI** — runs on push/PR.
   - **Release-only** — packaging / template / perf; separate from local.
   - **Manual / advisory** — on-demand.
3. Each member references its row in `justification-records.md`.

## Field rules

- Every member appears in exactly one frequency group (no item in two groups).
- The Local inner-loop group MUST be explicitly enumerated (not "everything else").
- Release-only checks MUST NOT also appear in the Local group.

## Acceptance (maps to spec)

- [ ] Every active-set member carries a frequency label. *(SC-003)*
- [ ] Local inner-loop group is enumerated and small enough for routine work. *(SC-002)*
- [ ] Release-only group is separate from Local with zero overlap. *(SC-003)*
- [ ] Every member traces to an `import-now` justification record. *(FR-003)*

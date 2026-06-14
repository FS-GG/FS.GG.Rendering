# Contract: Verdict Record

The schema for one row of the findings report (`docs/audit/mechanism-audit.md`). One Verdict per Mechanism. This is the audit's decision surface (FR-013, FR-014, SC-002, SC-006, SC-007, SC-008).

## Fields

| Field | Required | Domain | Meaning |
|---|---|---|---|
| Mechanism | yes | mechanism `id` | the mechanism judged |
| Verdict | yes | `works-as-advertised` \| `benefit-overstated` \| `not-working-or-no-op` \| `unverifiable-here` | the call |
| Severity | when divergent | `correctness-defect` \| `silent-no-op` \| `overstated-benefit` \| `cosmetic` | ordered worst→least |
| Evidence | yes | Verification refs | the checks backing the verdict |
| Recommendation | yes | `fix` \| `simplify` \| `remove` \| `re-scope-claim` \| `defer-to-tier` (+ detail) | the action |
| Reproduce | yes | command/filter | how to reproduce the verdict (SC-007) |

## Verdict-derivation rules

- All of a mechanism's Claims `verified` ⇒ `works-as-advertised`; no Severity.
- Correctness `verified` but effectiveness `refuted` (counter does not move) ⇒ `not-working-or-no-op`, Severity `silent-no-op`.
- Effectiveness present but below advertised margin ⇒ `benefit-overstated`, Severity `overstated-benefit`.
- Any correctness `refuted` ⇒ `not-working-or-no-op`, Severity `correctness-defect`.
- Any Claim `deferred` with none refuted ⇒ `unverifiable-here`, Recommendation `defer-to-tier` (name the tier).

## Coverage summary (required footer of the report — SC-008)

The report MUST end with counts so the maintainer sees the audit looked for each failure mode:

```text
Mechanisms audited:        N
  works-as-advertised:     n1
  benefit-overstated:      n2   (overstated-benefit)
  not-working-or-no-op:    n3   (of which correctness-defects: c, silent-no-ops: s)
  unverifiable-here:       n4   (deferred to capability tiers)
Discriminating-power confirmed for all correctness passes: yes/no
```

Any of `c`, `s`, `n2`, `n3` may legitimately be zero — but the count MUST be stated (a zero proves the audit checked, not that it skipped the check).

## Example row

| Mechanism | Verdict | Severity | Evidence | Recommendation | Reproduce |
|---|---|---|---|---|---|
| picture-cache | works-as-advertised | — | `picture-cache.parity`, `picture-cache.effectiveness` | (none) | `dotnet test tests/Controls.Tests --filter "Audit: picture cache"` |
| frame-rate-cap | unverifiable-here | — | `frame-rate-cap.timing` (deferred) | defer-to-tier (T3) | `dotnet run --project tests/Rendering.Harness -- perf --mode paced-60` |

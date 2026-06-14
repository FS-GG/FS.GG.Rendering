# Contract: Verification Record

The schema for the evidence bound to each Claim. One Verification per Claim. Captured in the inventory (status/evidence columns) and cited by the report (FR-003…FR-012, FR-014, SC-003…SC-005, SC-007).

## Fields

| Field | Required | Domain | Meaning |
|---|---|---|---|
| Claim ID | yes | claim slug | the claim under test |
| Method | yes | `discriminating-correctness` \| `counter-effectiveness` \| `adversarial` \| `harness-timing` | technique used |
| Evidence Ref | yes | Expecto test name (`Audit: …`) and/or harness `run.json` path | how to find the proof |
| Scenario | yes | text | the input/condition exercised |
| Baseline | when applicable | text | the disabled/bypassed comparison |
| Result | yes | `pass` \| `fail` \| `inconclusive` \| `skipped` | outcome |
| Discriminating Proof | for correctness | `true` \| `false` | did the assertion go red when the mechanism was bypassed? |
| Margin | for effectiveness | text | measured reduction vs baseline (e.g. `12/1000 remeasured`, `hit-rate 0.98`) |
| Tier | yes | `local-deterministic` \| `T1` \| `T2` \| `T3` | environment it ran in |
| Skip Rationale | when skipped/deferred | text | missing capability + required tier (FR-011) |
| Synthetic | yes | `false` \| `true: <reason>` | substitute disclosure (FR-012) |

## Validation rules (enforced by review of the inventory/report)

1. **Discriminating power gate (SC-003):** a `correctness` Verification with `Result=pass` MUST have `Discriminating Proof=true`. A pass without a demonstrated red-when-bypassed is itself a finding, not a pass.
2. **No-op gate (SC-004):** an `effectiveness` Verification passes only with a recorded `Margin` beating the baseline by the claim's stated threshold; margin equal to baseline ⇒ `fail` (no-op) or `inconclusive`, never `pass`.
3. **No overclaiming (SC-005, Principle VI):** `Result=skipped` requires a non-empty `Skip Rationale` naming the required tier; a capability-absent check is NEVER recorded as `pass`.
4. **Synthetic disclosure (Principle V):** any `Synthetic=true` MUST also be disclosed at the test use-site comment and listed in the report.
5. **Reproducibility (SC-007):** `Evidence Ref` MUST be sufficient to re-run the check (test filter or harness command).

## Example rows

| Claim ID | Method | Evidence Ref | Scenario | Baseline | Result | Discriminating Proof | Margin | Tier | Skip Rationale | Synthetic |
|---|---|---|---|---|---|---|---|---|---|---|
| picture-cache.parity | discriminating-correctness | `Audit: picture cache on==off, red when off` | 200-node themed scene | `PictureCacheEnabled=false` | pass | true | — | local-deterministic | — | false |
| picture-cache.effectiveness | counter-effectiveness | `Audit: picture cache hits steady-state` | 30 identical re-renders | enabled vs disabled | pass | — | hits 0.98 steady | local-deterministic | — | false |
| frame-rate-cap.timing | harness-timing | (pending) | `perf --mode paced-60` | uncapped | skipped | — | — | T3 | needs display+GL runner; required tier T3 | false |

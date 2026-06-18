# Feature 155 P7 Closeout Verdict

Status: `accepted`
Proof set: `accepted`
Parity status: `accepted`
Timing status: `inconclusive`
Fallback status: `partial-redraw-accepted`
Performance claim: `not-accepted`
Selected attempts: `3/3`
Accepted host profile: `probe-08a47c01`

## Live Proof

| Proof | Verdict | Host Profile |
|-------|---------|--------------|
| `feature155-20260618114112-1` | passed | `probe-08a47c01` |
| `feature155-20260618114112-2` | passed | `probe-08a47c01` |
| `feature155-20260618114112-3` | passed | `probe-08a47c01` |

## Evidence Links

- Proof set: `proof-set.md`
- Capable-host attempts: `live-proof/attempts/README.md`
- Unsupported host: `live-proof/unsupported/README.md`
- Parity corpus: `parity/parity.md`
- Timing decision: `timing/timing-damage.md`
- Compatibility ledger: `compatibility-ledger.md`
- Package validation: `package-validation.md`
- Regression validation: `regression-validation.md`
- FSI proof authoring: `fsi/native-proof-capture-authoring.fsx`

## Decision

- P7 live partial-redraw correctness is accepted for the current capable host profile because proof and same-profile parity evidence are accepted.
- Timing is inconclusive and records no accepted performance claim.
- Unsupported-host validation remains fail-closed with zero accepted partial-redraw artifacts.

## Synthetic Disclosure

- Synthetic Feature155 tests cover rejection and environment-limited paths only.
- Synthetic artifacts cannot satisfy proof, parity, timing, or final readiness acceptance.

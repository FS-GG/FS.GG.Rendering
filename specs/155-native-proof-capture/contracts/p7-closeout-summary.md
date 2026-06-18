# Contract: P7 Closeout Summary

## Scope

This contract defines the final P7 closeout package for Feature 155. The summary must distinguish
accepted partial-redraw correctness for the current host profile from unsupported-host limitations
and performance claims.

## Required Files

The readiness package under `specs/155-native-proof-capture/readiness/` contains:

- `validation-summary.md`: final P7 proof, parity, timing, fallback, host, compatibility, package,
  regression, and limitation status.
- `proof-set.md`: selected attempts, host profile, proof method, and proof-set verdict.
- `compatibility-ledger.md`: consumer-visible changes and migration notes when needed.
- `package-validation.md`: package and public-surface validation.
- `regression-validation.md`: focused and broad regression evidence.
- `live-proof/attempts/`: capable-host attempts and artifacts.
- `live-proof/unsupported/`: unsupported-host evidence and zero accepted artifacts.
- `parity/`: same-profile parity verdicts.
- `timing/`: timing decision and claim status.

## Status Rules

- `accepted`: proof-set acceptance and same-profile parity acceptance are both current for the
  current host profile.
- `fallback-gated`: required proof or parity evidence is missing, stale, mismatched, incomplete,
  synthetic-only, or otherwise non-accepting.
- `environment-limited`: the environment cannot produce honest capable-host proof evidence.
- `failed`: completed native evidence found a correctness or artifact defect.

Timing is separate: it may accept, reject, or decline a performance claim without changing the
correctness readiness status when proof and parity are accepted.

## Acceptance Tests

- Accepted closeout links selected attempts, sentinel/damage artifacts, proof set, parity corpus,
  timing decision, package validation, compatibility ledger, and regression validation.
- Unsupported-host output records zero accepted artifacts and cannot overwrite accepted capable-host
  evidence.
- Failed proof or failed parity keeps partial redraw fallback-gated.
- Missing or non-beneficial timing records no accepted performance claim.

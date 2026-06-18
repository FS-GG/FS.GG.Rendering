# Feature 156 Readiness Summary

Status: `noisy`
Proof/parity baseline: `accepted`
Timing status: `noisy`
Correctness status: `accepted-via-feature-155`
Fallback status: `partial-redraw-accepted`
Performance claim: `performance-not-accepted`
Accepted host profile: `probe-08a47c01`

## Evidence Links

- Timing summary: `timing/summary.md`
- Scenario reports: `timing/scenarios/`
- Raw samples: `timing/raw/`
- Unsupported host: `timing/unsupported/README.md`
- Compatibility ledger: `compatibility-ledger.md`
- Package validation: `package-validation.md`
- Regression validation: `regression-validation.md`
- FSI timing authoring: `fsi/compositor-performance-authoring.fsx`
- FSI readiness authoring: `fsi/compositor-readiness-authoring.fsx`

## Reviewer Determination

- A reviewer can determine scenario verdicts, distributions, host profile, policy, artifact paths, limitations, and final claim status from `timing/summary.md`.
- Under-5-minute determination check: `timing/summary.md` contains the host profile, policy id, scenario table, rejection reasons, overhead disclosure, remaining gates, and final claim status from one entry point.

## Decision

- Timing evidence is fail-closed and scoped to the accepted Feature 155 profile.
- `performance-not-accepted` remains the shipped P7 performance claim until Features 157, 158, 159, and 161 pass.
- Feature 160 remains a validation-throughput follow-up, not a performance-acceptance gate.
- Local validation passed; package status is `accepted-with-recorded-limitations`.

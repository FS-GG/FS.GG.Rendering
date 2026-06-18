# Feature 158 Readiness Summary

Status: `accepted`
Measurement policy id: `readback-free-timing-v1`
Accepted host profile: `probe-08a47c01`
Measured host profile: `probe-08a47c01`
Included timing samples: `50`
Excluded timing samples: `0`
Proof/probe evidence entries: `1`
Feature 156 comparison: `contextualizes`
Performance claim: `performance-not-accepted`

## Evidence Links

- Timing summary: `timing/summary.md`
- Timing summary JSON: `timing/summary.json`
- Scenario reports: `timing/scenarios/`
- Raw timing samples: `timing/raw/`
- Excluded samples: `timing/excluded/`
- Unsupported host: `timing/unsupported/README.md`
- Proof/probe evidence: `proof-probes/README.md`
- Compatibility ledger: `compatibility-ledger.md`
- Package validation: `package-validation.md`
- Regression validation: `regression-validation.md`
- FSI performance authoring: `fsi/compositor-performance-authoring.fsx`
- FSI readiness authoring: `fsi/compositor-readiness-authoring.fsx`

## Reviewer Checklist

- Measurement policy is visible from this summary and `timing/summary.md`.
- Included samples are linked through scenario reports and raw CSV/JSON files.
- Excluded samples are grouped by stable reason under `timing/excluded/`.
- Proof/probe readback artifacts are linked from `proof-probes/README.md` and excluded from accepted timing.
- Unsupported-host output records `environment-limited`, accepted proof artifacts `0`, and accepted performance artifacts `0`.
- Feature 156 comparison is recorded as `supersedes`, `confirms`, or `contextualizes`; this run records the value above.
- Under-5-minute reviewer inspection evidence is recorded by this single entry point.

## Decision

- Feature 158 accepts measurement separation only when required scenarios publish readback-free or outside-measurement samples.
- The shipped compositor performance claim remains `performance-not-accepted` until Feature 159 and Feature 161 pass.

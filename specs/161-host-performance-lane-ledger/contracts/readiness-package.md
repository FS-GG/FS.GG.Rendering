# Contract: Readiness Package

## Entry Point

`specs/161-host-performance-lane-ledger/readiness/validation-summary.md` is the reviewer entry
point. It must link every accepted ledger entry, excluded ledger entry, host fact record,
unsupported-host result, prior P7 gate record, full-validation record, compatibility note, package
validation record, and regression record needed to determine Feature 161 status in under 5 minutes.

## Required Files

```text
readiness/
|-- lane-ledger/
|   |-- summary.md
|   |-- entries/
|   |   `-- entry-*.md
|   |-- host-facts/
|   |   `-- facts-*.md
|   |-- excluded/
|   |   `-- *.md
|   `-- unsupported/
|       `-- README.md
|-- full-validation/
|-- fsi/
|-- compatibility-ledger.md
|-- package-validation.md
|-- regression-validation.md
`-- validation-summary.md
```

## Validation Summary Fields

- Final lane status: `accepted`, `rejected`, `fallback-only`, `environment-limited`, or `blocked`.
- Release-ready status.
- Host profile and run identity.
- Lane id.
- Policy id.
- Display server and display identity.
- Renderer identity and direct rendering status.
- Refresh rate or reason unavailable.
- Driver identity.
- Package version set.
- CPU/GPU load notes.
- Known environment limits.
- Accepted entries and count.
- Excluded entries and primary reasons.
- Unsupported-host result.
- Prior P7 gate status.
- Full validation status.
- Compatibility impact.
- Package validation result.
- Regression validation result.
- Artifact locations.
- Shipped performance claim status.
- Non-generalized lanes.
- Remaining performance gates.
- Reviewer checklist result showing that the summary exposes the required decision fields within
  the 5 minute review target.

## Acceptance Rules

- `accepted` lane status requires complete host facts for the scoped lane and zero cross-lane
  aggregation.
- `blocked` is used when lane facts are complete but another P7 gate or full validation blocks
  claim/release readiness.
- `rejected` is used when lane evidence exists but cannot satisfy host-lane acceptance.
- `fallback-only` is used when validated paths safely fall back without accepted performance
  evidence.
- `environment-limited` is used when the host cannot produce comparable evidence and accepted
  lane-scoped performance artifacts are zero.
- Unsupported-host validation must report zero accepted lane-scoped performance artifacts.
- The shipped performance claim remains `performance-not-accepted` unless same-profile timing is
  not noisy, Feature 159 reuse/promotion counters are net-positive, Feature 160 throughput is
  accepted, and Feature 161 host-lane scoping is accepted for the claimed lane.

## Compatibility Ledger

The compatibility ledger must state one of:

- No public API surface changed.
- Public surface changed for host-lane diagnostics, lane status, exclusion reasons, claim scope,
  readiness validation, or artifact inspection; include affected `.fsi`, surface-baseline files,
  semantic tests, FSI transcripts, and migration notes.

## Package Validation

Package validation records:

- Rendering.Harness Feature 161 lane-ledger tests.
- Testing helper checks if public Testing helpers are added.
- Package.Tests compatibility checks.
- Surface-baseline command outcome when `.fsi` changes.
- Pack or package-readiness command outcome when package output changes.

## Regression Validation

Regression validation records focused preservation checks for Feature 155 proof correctness,
Feature 157 damage-scissored readiness, Feature 158 readback-free timing separation, Feature 159
reuse/promotion readiness, Feature 160 throughput readiness, unsupported-host fail-closed behavior,
package validation, and public-surface drift.

## Full Validation

Full validation records broad release-gate output separately from lane-ledger evidence. Missing,
failing, interrupted, stale, or undocumented full validation blocks release-ready status even when
lane scoping is accepted.

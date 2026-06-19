# Contract: Validation Summary

## Entry Point

`specs/163-package-feed-validation-lanes/readiness/validation-summary.md` is the reviewer entry
point. It must link the machine-checkable summary JSON and every package proof, lane log/result,
diagnostic, compatibility note, package validation record, and regression record needed to
understand readiness.

## Required Files

```text
readiness/
|-- package-proof/
|   |-- package-versions.md
|   |-- package-pins.md
|   |-- source-proof.md
|   `-- source-proof.json
|-- lanes/
|   |-- summary.md
|   |-- summary.json
|   |-- package-proof/
|   |-- antshowcase-sample/
|   |-- controls/
|   |-- rendering-harness/
|   `-- aggregate-solution/
|-- diagnostics/
|-- compatibility-ledger.md
|-- package-validation.md
|-- regression-validation.md
`-- validation-summary.md
```

## Required Summary Fields

- Overall readiness: `ready`, `blocked`, `incomplete`, or `environment-limited`.
- Current `FS.GG.UI.*` package ids and versions.
- Selected samples.
- Local feed path.
- Package cache path.
- Package source rules.
- Package pin status.
- Package source proof status.
- Required lane list.
- Lane status table.
- Aggregate solution lane status.
- Failed/timed-out/hung/skipped/canceled/not-run/environment-limited evidence.
- Accepted exceptions.
- Diagnostic artifact locations.
- Caveats and environment limitations.
- Reviewer checklist result for the under-2-minute review target.

## Overall Readiness Rules

- `ready` requires current package pins, passing source proof, passing required lanes, and no
  required incomplete evidence.
- `blocked` applies when required package proof or required lanes fail, time out, hang, are
  canceled, or have source violations.
- `incomplete` applies when required proof or lanes are skipped or not run.
- `environment-limited` applies only when the host cannot produce required proof and the limitation
  is explicitly recorded. It is not a green state.
- Focused lane success and aggregate full-solution validation must be displayed as separate rows.
- A skipped, canceled, timed-out, hung, or not-run aggregate lane cannot be represented as a
  completed full-solution validation.

## Compatibility Ledger

The compatibility ledger must state one of:

- No public UI framework API changed; repository validation harness/script contracts changed.
- Public or package-visible surface changed; include affected `.fsi`, surface baselines, semantic
  tests, FSI transcripts, and migration notes.

## Package Validation Record

Package validation records:

- Package-feed command tests.
- Source-proof tests.
- Lane-runner tests.
- Package.Tests drift/evidence assertions.
- Surface-baseline command outcome if `.fsi` changes.
- Pack/local-feed command outcome.
- AntShowcase selected-sample proof.

## Regression Validation Record

Regression validation records preservation checks for:

- AntShowcase package-only restore behavior.
- Existing sample/package validation evidence.
- Existing Rendering.Harness compositor/readiness commands.
- Focused Feature 160 and 161 lane/readiness tests touched by this feature.
- Output isolation for concurrent dotnet lanes.

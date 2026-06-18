# Feature 161 Readiness Directory

This directory is the durable evidence root for Feature 161 host performance lane scoping.

## Layout

- `lane-ledger/`: host facts, accepted entries, excluded entries, unsupported-host records, `summary.md`, and optional `summary.json`.
- `full-validation/`: release-gate record for `dotnet test FS.GG.Rendering.slnx --no-restore`.
- `fsi/`: compositor, Perf, Testing helper, and public-surface evidence.
- `compatibility-ledger.md`: additive public surface and runtime compatibility notes.
- `package-validation.md`: focused Rendering.Harness, Testing.Tests, Package.Tests, FSI, and surface-drift outcomes.
- `regression-validation.md`: preservation evidence for Features 155, 157, 158, 159, 160, full-redraw fallback, unsupported hosts, package validation, and public-surface drift.
- `validation-summary.md`: reviewer entry point with lane status, release-ready status, artifact links, non-generalized lanes, remaining blockers, and final claim status.

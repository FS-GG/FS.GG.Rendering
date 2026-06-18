# Feature 160 Readiness Package

Reviewer entry point: `validation-summary.md`.

This package separates focused throughput evidence from broad release validation:

- `throughput/summary.md` records the focused lane, accepted iterations, raw sample links, exclusions, and unsupported-host evidence.
- `full-validation/validation.md` records the full solution release gate separately from focused throughput collection.
- `compatibility-ledger.md`, `package-validation.md`, and `regression-validation.md` document public surface, package, and preservation checks.
- `fsi/` contains authoring transcripts for the exposed command and Testing helper surface.

The shipped compositor performance claim remains `performance-not-accepted` until the remaining performance gates are accepted.

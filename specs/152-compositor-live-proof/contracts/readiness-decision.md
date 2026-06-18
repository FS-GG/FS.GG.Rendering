# Contract: Readiness Decision

## Scope

This contract defines the single Feature 152 readiness entry point. Reviewers must be able to
determine whether P7 live partial redraw is accepted, environment-limited, failed, or
fallback-gated without reading private context.

## Required Readiness Files

The readiness package lives under `specs/152-compositor-live-proof/readiness/` and contains:

- `validation-summary.md`: final P7 status, tier verdicts, limitations, and evidence links.
- `compatibility-ledger.md`: public API, diagnostics, fallback, timing, readiness, docs, and
  package-impact record.
- `live-proof/`: proof attempts, accepted proof set, artifacts, and environment-limited records.
- `parity/`: full-redraw oracle and damage-scoped live parity records.
- `timing/`: same-profile timing evidence and performance claim decision.

Machine-readable records may be added beside Markdown summaries when the implementation chooses a
stable format.

## Status Rules

- `accepted`: accepted proof set exists, live parity passed for accepted scenarios, compatibility
  validation passed, and accepted partial-redraw claims link to supporting artifacts.
- `environment-limited`: the environment cannot produce honest capable-host proof, parity, or
  timing evidence; no partial-redraw or performance acceptance is recorded.
- `failed`: proof, parity, artifact quality, or compatibility validation failed.
- `fallback-gated`: evidence is missing, incomplete, stale, host-mismatched, method-mismatched,
  synthetic-only, or otherwise non-accepting; full redraw remains the safe path.

The performance claim decision is reported separately as accepted, rejected, inconclusive, or
environment-limited.

## Compatibility Rules

The compatibility ledger is required when the feature changes:

- Public API or `.fsi` surfaces.
- Public diagnostics or metrics.
- Surface baselines.
- Observable fallback behavior.
- Readiness or timing claim vocabulary.
- Release notes or migration guidance.

Accepted readiness is blocked by undocumented public drift.

## Regression Rules

Feature 152 must preserve:

- Feature 149 deterministic proof, fallback, parity, timing, and diagnostic guarantees.
- Feature 151 P8 layout acceptance and compositor-nonclaim regression.
- Existing render-anywhere, overlay, text-shaping, full-redraw, package-readiness, and surface
  baseline guarantees unless a compatibility note explicitly documents a change.

## MVU Boundary

Readiness assembly must be testable through:

- `Model`: proof attempts, proof set, parity records, timing records, compatibility ledger,
  fallback decisions, limitations, and final status.
- `Msg`: evidence loaded, proof set evaluated, parity evaluated, timing decided, compatibility
  checked, limitation classified, summary rendered.
- `Effect`: read artifacts, validate paths, run commands where appropriate, write summary, write
  ledger.
- `update`: pure transition from `Msg` and `Model` to next `Model` plus effects.
- Interpreter: executes filesystem, process, timing, and artifact I/O at the harness edge.

## Acceptance Tests

- Complete accepted proof set plus same-profile passed parity produces accepted P7 correctness
  readiness.
- Environment-limited proof produces environment-limited readiness and zero accepted artifacts.
- Failed proof, failed parity, invalid artifacts, or compatibility drift produces failed
  readiness.
- Missing, stale, synthetic-only, host-mismatched, or method-mismatched evidence produces
  fallback-gated readiness.
- Noisy, incomplete, or non-beneficial timing records no accepted performance claim.
- The summary links proof, parity, timing, fallback, compatibility, and regression evidence from
  one file.

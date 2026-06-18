# Contract: Public Diagnostics and Readiness

## Scope

This contract defines the consumer-visible compositor diagnostics and final readiness package for
P7. Consumers and maintainers must be able to tell whether the compositor is accepted,
environment-limited, failed, incomplete, or safely falling back without relying on private
repository internals.

## Public Diagnostic Surface

The package-facing surface must expose:

- Proof status: accepted, failed, environment-limited, missing, stale, host-mismatched, or
  synthetic-only rejected.
- Damage parity status: passed, failed, skipped, environment-limited, or not-run.
- Reuse status: ready, demoted, rejected, skipped, limited, or not-run.
- Snapshot status: ready, demoted, rejected, skipped, unsupported, limited, or not-run.
- Timing status: passed, failed, inconclusive, environment-limited, or not-run.
- Fallback status: none, full-redraw, lower-tier, disabled, demoted, or blocked.
- Readiness verdict: accepted, environment-limited, failed, or incomplete.
- Limitations and artifact paths that explain the verdict.

Any public type, value, helper, or formatter must be declared in `.fsi` before implementation and
covered by semantic/FSI tests.

## Required Readiness Files

The readiness package lives under `specs/149-complete-compositor-p7/readiness/` and contains:

- `validation-summary.md`: P7 verdict, tier verdicts, limitations, and artifact links.
- `compatibility-ledger.md`: public metrics/diagnostics/API changes, migration guidance, release
  notes, and surface baseline references.
- `live-proof/`: live proof records and artifacts.
- `parity/`: oracle comparison records by scenario/frame.
- `reuse/`: content/placement promotion, reuse, and demotion summaries.
- `snapshots/`: resource budget, lifecycle, support, and composition evidence.
- `timing/`: timing probe reports by tier and corpus.

Machine-readable records may be added beside Markdown summaries when the implementation chooses a
stable format.

## Tier Verdicts

Each tier records one verdict:

- `ready`: proof, parity, fallback, resource, timing, and compatibility obligations pass.
- `limited`: environment or host limitation prevents a readiness claim, but behavior fails safely.
- `rejected`: parity, performance, resource, or correctness evidence failed.
- `skipped`: tier was intentionally not attempted with rationale.
- `incomplete`: required evidence has not yet been produced.

Ready tiers must link to supporting artifacts. Limited, rejected, skipped, and incomplete tiers
must explain why they cannot count as shipped benefits.

## Compatibility Rules

The compatibility ledger is required when the feature changes:

- Public API or `.fsi` surfaces.
- Public diagnostics or metrics.
- Surface baselines.
- Observable fallback behavior.
- Release notes or migration guidance.

Ready verdicts are blocked when required compatibility impact is missing.

## MVU Boundary

Readiness assembly must be testable through:

- `Model`: collected proof, parity, reuse, snapshot, timing, compatibility, and limitation records.
- `Msg`: evidence loaded, tier evaluated, compatibility updated, summary rendered, failure
  classified.
- `Effect`: read evidence, validate artifact paths, run probe where appropriate, write summary,
  write ledger.
- `update`: pure transition from `Msg` and `Model` to next `Model` plus effects.
- Interpreter: executes filesystem, process, and timing effects in the harness/package edge.

## Acceptance Tests

- Complete passed evidence produces `accepted` P7 readiness or `ready` for the corresponding tier.
- Failed parity produces `rejected` even when timing is positive.
- Environment-limited proof or timing produces `limited` and cannot enable readiness.
- Missing compatibility ledger blocks readiness when public metrics, diagnostics, API, baselines,
  or behavior changed.
- Formatter output includes host profiles, tier verdicts, fallbacks, limitations, and artifact
  links that reviewers can inspect from one summary.
- Package validation passes with only documented compositor public-surface changes.

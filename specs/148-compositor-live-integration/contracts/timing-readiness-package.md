# Contract: Timing and Readiness Package

## Scope

This contract defines the timing probes and durable readiness package used to decide whether any P7
compositor tier can be reported as a shipped performance benefit.

## Required Readiness Files

The readiness package lives under `specs/148-compositor-live-integration/readiness/` and contains:

- `validation-summary.md`: tier verdicts, limitations, and artifact links.
- `compatibility-ledger.md`: public metrics/diagnostics/API changes, migration guidance, release
  notes, and surface baseline references.
- `live-proof/`: live proof records and artifacts.
- `parity/`: oracle comparison records by scenario/frame.
- `reuse/`: content/placement promotion, reuse, and demotion summaries.
- `snapshots/`: resource budget, lifecycle, support, and composition evidence.
- `timing/`: timing probe reports by tier and corpus.

Machine-readable records may be added beside Markdown summaries when the implementation chooses a
stable format.

## Timing Probe Rules

- Damage tier compares against full-frame redraw.
- Placement/replay tiers compare against the lower redraw tier and full-frame baseline where
  relevant.
- Snapshot tier compares against the lower reuse tier and full-frame baseline.
- Probes record host profile, warmup frames, measured frames, corpus, baseline tier, target tier,
  thresholds, environment facts, and verdict.
- Beneficial corpora and non-beneficial corpora are both required.
- Environment-limited timing is disclosure only and cannot mark a tier ready.

## Tier Verdicts

Each tier records one verdict:

- `ready`: proof, parity, fallback, resource, timing, and compatibility obligations pass.
- `limited`: environment or host limitation prevents a readiness claim, but behavior fails safely.
- `rejected`: parity, performance, resource, or correctness evidence failed.
- `skipped`: tier was intentionally not attempted with rationale.

Ready tiers must link to supporting artifacts. Limited, rejected, and skipped tiers must explain
why they cannot count as shipped benefits.

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

- Complete passed evidence produces `ready` for the corresponding tier.
- Failed parity produces `rejected` even when timing is positive.
- Environment-limited proof or timing produces `limited` and cannot enable readiness.
- Missing compatibility ledger blocks readiness when public metrics, diagnostics, API, baselines, or
  behavior changed.
- Formatter output includes host profiles, tier verdicts, fallbacks, limitations, and artifact
  links that reviewers can inspect within 10 minutes.

# Contract: Compositor Readiness Package

## Scope

This contract defines the durable evidence package reviewers use to decide which compositor tiers
are ready, limited, or rejected. It ties together proof, parity, promotion, snapshot resources,
performance, diagnostics, limitations, and compatibility impact.

## Required Files

The readiness package lives under `specs/147-compositor-damage-redraw/readiness/` and contains:

- `validation-summary.md`: human-readable tier verdicts and limitations.
- `compatibility-ledger.md`: public metrics/diagnostics/API changes, migration guidance, release
  notes, and surface baseline references.
- `present-proof/`: proof records and artifacts.
- `parity/`: oracle comparison records by scenario.
- `perf/`: probe reports by tier and corpus.

Machine-readable records may be added beside the Markdown summaries when implementation chooses a
stable format.

## Tier Verdicts

Each tier records one verdict:

- `ready`: passed proof, parity, performance, resource, and compatibility obligations.
- `limited`: environment or host limitation prevents a readiness claim, but behavior fails safely.
- `rejected`: parity, performance, resource, or correctness evidence failed.
- `skipped`: tier was intentionally not attempted with rationale.

Ready tiers must link to their supporting artifacts. Limited, rejected, and skipped tiers must name
why they cannot count as shipped benefits.

## Evidence Summary

The package summarizes:

- Host profiles and present-path proof verdicts.
- Damage/scissor parity results by corpus and frame.
- Promotion and demotion decision summaries.
- Snapshot resource budget, lifecycle, and support status.
- Performance probe thresholds, baselines, and deltas.
- Fallback reasons and unsafe/unsupported cases.
- Synthetic or environment-limited disclosure.
- Compatibility impact and migration guidance.

## Acceptance Rules

- A tier cannot be `ready` without matching proof, parity, and performance evidence.
- Missing, stale, synthetic, environment-limited, or host-mismatched evidence cannot be accepted as
  readiness proof.
- Failed tiers cannot be hidden by aggregate success.
- Compatibility ledger is required whenever public metrics, diagnostics, baselines, or observable
  behavior change.
- Reviewers must be able to identify ready, limited, rejected, and skipped tiers within 10 minutes.

## MVU Boundary

Readiness assembly must be testable as:

- `Model`: collected proof, parity, performance, resource, compatibility, and limitation records.
- `Msg`: evidence loaded, tier evaluated, compatibility updated, summary rendered, failure
  classified.
- `Effect`: read evidence, write summary, write compatibility ledger, validate artifact paths.
- `update`: pure transition from message and model to next model plus effects.
- Interpreter: executes filesystem effects in the harness or package-readiness edge.

## Acceptance Tests

- Complete passed evidence produces `ready` for the corresponding tier.
- A failed parity record produces `rejected`, even when performance is positive.
- Environment-limited proof produces `limited` and cannot enable scissoring readiness.
- Missing compatibility ledger blocks readiness when public metrics or baselines changed.
- Formatter output includes tier verdicts, host profiles, limitations, and artifact links.

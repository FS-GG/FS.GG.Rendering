# Contract: Promotion and Snapshot Reuse

## Scope

This contract defines how stable compositor boundaries become eligible for reuse, how
placement-only movement reuses content safely, how churning boundaries are demoted, and when the
higher-cost snapshot tier may be used.

## Boundary Identity

Each candidate boundary has:

- Stable boundary identity from the retained tree.
- Content identity over render-affecting inputs.
- Placement identity over position, size, transform, and clip placement.
- Observation window and stability evidence.
- Candidate size/work estimate.
- Last parity and performance verdicts.

Content identity and placement identity are separate. Content changes force fresh output; placement
changes may reuse content while damaging both old and new covered regions.

## Promotion Rules

A boundary may promote only when:

- It is stable for the configured observation window.
- It is large or expensive enough to save meaningful work.
- Its reuse path passes full-redraw oracle parity.
- Its bookkeeping overhead stays within the accepted threshold.
- It does not exceed tier resource budgets.

Promotion decisions must record the reason, observed frames, expected saved work, measured overhead,
target tier, and diagnostics.

## Demotion Rules

A boundary must demote or remain unpromoted when:

- Content churns repeatedly.
- Parity fails.
- It produces no measured benefit.
- Simple/churning corpus overhead exceeds the accepted threshold.
- Snapshot or replay resources exceed budget.
- Host capability is missing or environment-limited.

Demotion decisions must be visible in readiness evidence.

## Snapshot Tier Rules

The snapshot tier is optional and host-owned by SkiaViewer. It may be used only when:

- Host capability is supported and recorded.
- The boundary is stable, expensive, and already parity-clean.
- Probe evidence shows net benefit against the lower tier.
- Resource count/byte budget is explicit and enforced.
- Invalid, stale, or over-budget resources are refreshed, evicted, demoted, or released before use.

Snapshots are not public Scene authoring constructs in this feature.

## Performance Rules

- Stable/moving corpus must meet the repeated-work reduction target before promotion benefit is
  claimed.
- Simple/churning corpus must remain within overhead threshold or the responsible tier demotes.
- Snapshot corpus must meet its improvement threshold against the lower tier before snapshot
  readiness is claimed.
- Timing evidence must name baseline tier, target tier, corpus, threshold, and environment.

## Acceptance Tests

- Stable boundary promotes after observation and reduces repeated work while preserving parity.
- Placement-only movement reuses content at the new placement and damages old/new covered regions.
- Content change rejects stale content and produces fresh output.
- Churning boundary demotes or remains unpromoted with a recorded reason.
- Snapshot resources stay within budget and are evicted/refreshed deterministically.
- Unsupported snapshot host falls back to lower tiers or full redraw without accepted snapshot
  evidence.

# Contract: Reuse, Snapshot, and Timing Evidence

## Scope

This contract defines the remaining P7 evidence required for content/placement reuse, snapshot
composition, bounded resource lifecycle, and timing readiness. These tiers are optimizations and
cannot override proof or parity failures.

## Reuse Rules

Each reusable boundary records:

- Stable retained boundary identity.
- Content identity over render-affecting inputs.
- Placement identity over bounds, transform, clip, layer, and scale.
- Previous placement identity when movement occurs.
- Observation window and stability evidence.
- Candidate size/work estimate.
- Current tier and last parity verdict.

Content identity and placement identity are separate. Content changes require fresh output;
placement-only changes may reuse content only when old and new covered regions are damaged.

## Promotion and Demotion Rules

A boundary may promote or reuse only when:

- Content identity is stable for the configured observation window.
- The boundary is expensive or large enough to save meaningful work.
- Reuse passes full-frame oracle parity.
- Bookkeeping overhead stays within accepted threshold.
- The target tier is supported by active proof, host, and resource state.

A boundary must demote or remain unpromoted when:

- Content churns repeatedly.
- Parity fails.
- It produces no measured benefit.
- Simple/churning corpus overhead exceeds threshold.
- Snapshot or replay resource budgets are exceeded.
- Proof or host capability is missing.

Promotion, reuse, refresh, demotion, and rejection decisions must be visible in diagnostics and
readiness evidence.

## Snapshot Resource Rules

Snapshot resources are SkiaViewer-owned host artifacts. Each resource records:

- Resource id and source boundary id.
- Content identity used for freshness.
- Host profile where the resource is valid.
- Byte estimate and budget id.
- State: created, reused, refreshed, replaced, evicted, disposed, invalid, unsupported, bypassed,
  or failed.
- Last-used frame.
- Diagnostics and artifact references where available.

Budget checks happen before accepted reuse. Over-budget, invalid, stale, or host-mismatched
resources trigger refresh, eviction, disposal, demotion, bypass, or full redraw before use.

## Snapshot Composition Rules

- Snapshot composition requires accepted proof where preservation matters and full-redraw oracle
  parity before readiness claims.
- Unsupported hosts record a limitation and cannot report snapshot readiness.
- Snapshot parity failure rejects or demotes the snapshot tier.
- Snapshot resources are never exposed as new public Scene authoring constructs in this feature.

## Timing Probe Rules

- Full redraw, damage-scoped redraw, and snapshot-assisted redraw must have comparable repeated
  measurements.
- Damage tier compares against full redraw.
- Placement/replay tiers compare against the lower redraw tier and full redraw where relevant.
- Snapshot tier compares against the lower reuse tier and full redraw.
- Probes record host profile, warmup frames, measured frames, corpus, baseline tier, target tier,
  thresholds, environment facts, and verdict.
- Beneficial corpora and non-beneficial corpora are both required.
- Environment-limited, incomplete, noisy, or inconclusive timing is disclosure only and cannot
  mark a tier ready.

## Acceptance Tests

- Stable content with unchanged placement promotes only after observation and parity evidence.
- Placement-only movement reuses content at the new placement and damages old/new regions.
- Content change with similar bounds rejects stale content and produces fresh output.
- Churning or non-beneficial boundaries demote or remain unpromoted with recorded reason.
- Expensive stable content creates and composes a snapshot after eligibility, parity, and benefit
  evidence.
- Over-budget resources evict or bypass deterministically.
- Invalid or stale resources refresh, demote, or fall back without stale output.
- Timing reports enough comparable measurements to support or reject a performance claim.

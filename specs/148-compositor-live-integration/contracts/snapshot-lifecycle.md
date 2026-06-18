# Contract: Snapshot Lifecycle

## Scope

This contract defines the bounded SkiaViewer-owned snapshot tier for expensive stable content. The
snapshot tier is an optimization above replay/placement reuse and is not a new public Scene
authoring construct for this feature.

## Eligibility

A boundary may use a snapshot only when:

- The active host supports the resource path and records that support.
- The boundary is stable, expensive, and parity-clean.
- Content identity is fresh.
- Timing evidence shows net benefit against the lower tier.
- Resource budget has capacity or deterministic eviction can create capacity safely.

## Resource Record

Each snapshot resource records:

- Resource id and source boundary id.
- Content identity used for freshness.
- Host profile where the resource is valid.
- Byte estimate and budget id.
- State: available, missing, invalid, refreshed, evicted, disposed, unsupported, or bypassed.
- Last-used frame.
- Diagnostics and artifact references where available.

## Budget Rules

- Entry count and byte estimate are explicit.
- Budget checks happen before accepting reuse.
- Over-budget state triggers refresh, eviction, demotion, bypass, or full redraw before use.
- Eviction and disposal are deterministic and recorded.
- Resource lifecycle evidence links to readiness summaries.

## Freshness and Fallback Rules

- Content mismatch requires refresh or demotion before use.
- Host-profile mismatch invalidates host-specific snapshots.
- Invalid or lost resources fall back to lower tiers or full redraw without accepted stale output.
- Unsupported hosts record a limitation and cannot report snapshot readiness.
- Snapshot parity failure rejects or demotes the snapshot tier.

## Acceptance Tests

- Expensive stable content creates and composes a snapshot after eligibility and parity evidence.
- Simple or churning content rejects or demotes the snapshot tier before sustained overhead.
- Over-budget resources evict or bypass deterministically.
- Invalid or stale resources refresh, demote, or fall back without stale output.
- Unsupported host profiles continue through lower tiers/full redraw and disclose the limitation.

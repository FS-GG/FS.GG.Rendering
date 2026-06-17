# Snapshot Tier Evidence

Feature147 snapshot readiness is bounded by explicit resource and performance thresholds.

## Resource Budget

- Maximum retained entries: 64.
- Maximum deterministic byte estimate: 32 MiB.
- Over-budget, invalid, stale, unsupported, or losing snapshots must refresh, evict, demote, or fall back before reuse.

## Performance Expectations

- Snapshot reuse needs at least 20% frame-cost improvement on the expensive stable corpus before it can be reported ready.
- Simple and churning scenes must stay within 5% overhead of the lower tier or the responsible tier must demote.

## Current Status

The deterministic budget and policy contracts are implemented and tested. Live snapshot composition and host timing evidence are not claimed ready in this slice.

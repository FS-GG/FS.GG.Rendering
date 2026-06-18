# Readiness Validation Contract

## Purpose

Define the evidence required before Feature150 can be treated as an accepted Tier 1 layout change.

## Required Evidence

- Public surface and compatibility: `.fsi` additions, surface baseline refresh, semantic FSI tests,
  compatibility ledger, and migration notes.
- Intrinsic protocol: deterministic query tests, unsupported-query diagnostics, and cache dependency
  evidence.
- ScrollViewer: viewport/extent/range validation across at least 10 representative cases.
- Full/incremental parity: cold full, warm incremental, and changed-input incremental results match
  the agreed corpus for bounds, placements, scroll extents, and diagnostics.
- Cache/invalidation: at least 5 input-change categories prove no accepted stale measured or
  intrinsic result.
- Regression: existing retained rendering, overlay, render-anywhere, text-shaping, compositor
  readiness, disabled-cache, and default layout compatibility evidence remains valid outside
  documented layout deltas.

## Artifact Shape

Readiness output lives under `specs/150-intrinsic-layout-protocol/readiness/`:

- `validation-summary.md`
- `compatibility-ledger.md`
- `scrollviewer-validation.md`
- `intrinsic-cache-validation.md`
- `full-incremental-parity.md`

Each artifact records the commands or tests used, verdict, limitations, and links to supporting
paths when generated.

## Acceptance Rules

- Accepted readiness requires every required evidence category to pass or have an explicit,
  bounded, non-blocking limitation.
- Synthetic or fixture-only evidence may cover failure paths but cannot replace accepted parity,
  compatibility, or ScrollViewer evidence.
- Failed, skipped, incomplete, or environment-limited results are visible and do not count as
  accepted behavior.
- A maintainer can review the final status from `validation-summary.md` in under 10 minutes.

# Feature 148 Reuse Artifacts

## Schema

- `reuse.md`: corpus decisions for stable, moving-only, scrolling, content-changing, churning, failed-parity, and same-seed scenarios.
- `demotions.md`: demotion reasons for content, theme/resource, provider, host-profile, parity, churn, and no-benefit changes.
- `work-reduction.md`: repeated-work reduction and measured overhead summary.

## Acceptance

Moving-only and scrolling scenarios require deterministic old/new movement damage, parity-clean output, and at least 30% repeated-work reduction before reuse is ready.

## Current Status

The retained-render policy separates stable content decisions from placement movement damage and demotes parity-failed, churning, or low-benefit boundaries.

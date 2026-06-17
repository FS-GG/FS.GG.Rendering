# Promotion and Placement Reuse Evidence

Feature147 promotion readiness is based on deterministic policy records in this slice.

## Expected Evidence

- Stable boundaries are eligible only after the configured observation window and expected work reduction threshold.
- Placement-only movement damages both old and new placement rectangles while preserving content identity.
- Content identity changes reject stale reuse.
- Churning or no-benefit boundaries are demoted or left unpromoted.

## Current Status

The deterministic retained-render policy and derived frame diagnostics are covered by focused tests. Live renderer integration and measured repeated-work reduction remain limited until a capable host run records parity and timing evidence.

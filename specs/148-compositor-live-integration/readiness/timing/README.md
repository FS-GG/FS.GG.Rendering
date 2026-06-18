# Feature 148 Timing Artifacts

## Schema

- `timing-damage.md`: damage-scoped redraw timing against the full-frame oracle.
- `timing-placement.md`: placement/reuse timing against replay or the lower redraw tier.
- `timing-replay.md`: replay baseline timing.
- `timing-snapshot.md`: snapshot timing against replay or the lower tier.

## Probe Rules

- Warmup frames are excluded from measured frames.
- Beneficial and non-beneficial corpus runs are both required.
- Environment-limited runs disclose why timing cannot be authoritative.
- Threshold failures demote or reject the tier instead of claiming readiness.

## Current Status

The deterministic harness can format tier reports and readiness summaries. Real timing claims remain limited until a capable host run produces measured frame data.

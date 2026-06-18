# Feature 156 Same-Profile Timing Summary

Run identity: `feature156-20260618130411`
Feature 156 timing verdict: `environment-limited`
Shipped P7 performance claim: `performance-not-accepted`
Policy id: `same-profile-live-threshold-v2`
Noise-band formula: `max(0.25 ms, 5% of full-redraw p50)`
Warmup count: `1`
Measured repetitions per path: `1`

## Host Profile

- Accepted profile id: `probe-08a47c01`
- Measured profile id: `probe-eb6566e6`
- Backend: `OpenGL`
- Renderer: `unknown`
- Present mode: `DirectToSwapchain`
- Framebuffer: `640x480`
- Scale: `1`
- Display environment: `missing-display`
- Package version: `local-harness`

## Feature 155 Baseline

- Proof/parity baseline: `../155-native-proof-capture/readiness/validation-summary.md`
- Correctness status: `accepted` for accepted host profile `probe-08a47c01`.
- Fallback status: `partial-redraw-accepted` for correctness; performance remains separate.

## Scenario Table

| Scenario | Full p50 | Full p95 | Full p99 | Damage p50 | Damage p95 | Damage p99 | Noise band | Verdict | Confidence | Artifact |
|----------|----------|----------|----------|------------|------------|------------|------------|---------|------------|----------|
| `timing/localized-update` | `missing` | `missing` | `missing` | `missing` | `missing` | `missing` | `0` | `environment-limited` | `environment-limited` | `scenarios/timing-localized-update.md` |
| `timing/no-change` | `missing` | `missing` | `missing` | `missing` | `missing` | `missing` | `0` | `environment-limited` | `environment-limited` | `scenarios/timing-no-change.md` |
| `timing/movement-old-new` | `missing` | `missing` | `missing` | `missing` | `missing` | `missing` | `0` | `environment-limited` | `environment-limited` | `scenarios/timing-movement-old-new.md` |
| `timing/overlap` | `missing` | `missing` | `missing` | `missing` | `missing` | `missing` | `0` | `environment-limited` | `environment-limited` | `scenarios/timing-overlap.md` |
| `timing/edge-clipping` | `missing` | `missing` | `missing` | `missing` | `missing` | `missing` | `0` | `environment-limited` | `environment-limited` | `scenarios/timing-edge-clipping.md` |

## Rejection Reasons

- timing/localized-update: missing display
- timing/no-change: missing display
- timing/movement-old-new: missing display
- timing/overlap: missing display
- timing/edge-clipping: missing display

## Overhead Disclosure

- Scenario reports state whether proof readback or validation overhead is included.
- Readback-dominated or unseparated overhead is `limited` and cannot support a shipped claim.

## Remaining Gates

- Feature 157 damage-scissored no-clear renderer: `remaining`
- Feature 158 readback separation: `remaining`
- Feature 159 net-positive reuse/promotion counters: `remaining`
- Feature 160 validation throughput follow-up: `remaining`, not a shipped performance-acceptance gate
- Feature 161 host performance lane ledger: `remaining`

## Diagnostics

- missing display
- accepted performance artifacts=0

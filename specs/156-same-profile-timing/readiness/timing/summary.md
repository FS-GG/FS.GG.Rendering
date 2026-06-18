# Feature 156 Same-Profile Timing Summary

Run identity: `feature156-20260618130404`
Feature 156 timing verdict: `noisy`
Shipped P7 performance claim: `performance-not-accepted`
Policy id: `same-profile-live-threshold-v2`
Noise-band formula: `max(0.25 ms, 5% of full-redraw p50)`
Warmup count: `3`
Measured repetitions per path: `5`

## Host Profile

- Accepted profile id: `probe-08a47c01`
- Measured profile id: `probe-08a47c01`
- Backend: `OpenGL`
- Renderer: `AMD Radeon Graphics (radeonsi, renoir, ACO, DRM 3.64, 7.0.11-arch1-1)`
- Present mode: `DirectToSwapchain`
- Framebuffer: `640x480`
- Scale: `1`
- Display environment: `x11`
- Package version: `local-harness`

## Feature 155 Baseline

- Proof/parity baseline: `../155-native-proof-capture/readiness/validation-summary.md`
- Correctness status: `accepted` for accepted host profile `probe-08a47c01`.
- Fallback status: `partial-redraw-accepted` for correctness; performance remains separate.

## Scenario Table

| Scenario | Full p50 | Full p95 | Full p99 | Damage p50 | Damage p95 | Damage p99 | Noise band | Verdict | Confidence | Artifact |
|----------|----------|----------|----------|------------|------------|------------|------------|---------|------------|----------|
| `timing/localized-update` | `13.605` | `14.054` | `14.054` | `13.278` | `14.296` | `14.296` | `0.68` | `noisy` | `inside-noise-band` | `scenarios/timing-localized-update.md` |
| `timing/no-change` | `13.645` | `14.785` | `14.785` | `13.179` | `13.363` | `13.363` | `0.682` | `noisy` | `inside-noise-band` | `scenarios/timing-no-change.md` |
| `timing/movement-old-new` | `13.317` | `13.465` | `13.465` | `12.908` | `13.117` | `13.117` | `0.666` | `noisy` | `inside-noise-band` | `scenarios/timing-movement-old-new.md` |
| `timing/overlap` | `13.465` | `13.885` | `13.885` | `12.914` | `13.255` | `13.255` | `0.673` | `noisy` | `inside-noise-band` | `scenarios/timing-overlap.md` |
| `timing/edge-clipping` | `12.734` | `14` | `14` | `13.14` | `14.223` | `14.223` | `0.637` | `noisy` | `inside-noise-band` | `scenarios/timing-edge-clipping.md` |

## Rejection Reasons

- timing/localized-update: p50 or p95 difference is inside the declared noise band
- timing/no-change: p50 or p95 difference is inside the declared noise band
- timing/movement-old-new: p50 or p95 difference is inside the declared noise band
- timing/overlap: p50 or p95 difference is inside the declared noise band
- timing/edge-clipping: p50 or p95 difference is inside the declared noise band

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

- same-profile timing measurement completed
- shipped performance claim remains performance-not-accepted

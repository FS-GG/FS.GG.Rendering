# Feature 158 Readback-Free Timing Summary

Run identity: `feature158-20260618152718`
Feature 158 measurement-separation status: `accepted`
Policy id: `readback-free-timing-v1`
Accepted profile id: `probe-08a47c01`
Measured profile id: `probe-08a47c01`
Unsupported-host reason: `none`
Included timing samples: `50`
Excluded timing samples: `0`
Shipped P7 performance claim: `performance-not-accepted`
Feature 156 comparison: `contextualizes`
Warmup count: `3`
Measured repetitions per path: `5`

## Host Profile

- Backend: `OpenGL`
- Renderer: `AMD Radeon Graphics (radeonsi, renoir, ACO, DRM 3.64, 7.0.11-arch1-1)`
- Present mode: `DirectToSwapchain`
- Framebuffer: `640x480`
- Scale: `1`
- Display environment: `x11`
- Package version: `local-harness`

## Measurement Policy

- Accepted timing samples must declare `readback-free` or `readback-outside-measurement`.
- Proof/probe/readback samples are listed as excluded evidence and never enter the accepted performance set.
- Missing, unverifiable, contaminated, cross-profile, cross-run, scenario-mismatched, package-mismatched, or unsupported samples fail closed.

## Scenario Coverage

| Scenario | Status | Included | Excluded | Scenario definition | Artifact | Proof/probe links |
|----------|--------|----------|----------|---------------------|----------|-------------------|
| `timing/localized-update` | `accepted` | `10` | `0` | `feature156-required-v1:timing-localized-update` | `scenarios/timing-localized-update.md` | `` |
| `timing/no-change` | `accepted` | `10` | `0` | `feature156-required-v1:timing-no-change` | `scenarios/timing-no-change.md` | `` |
| `timing/movement-old-new` | `accepted` | `10` | `0` | `feature156-required-v1:timing-movement-old-new` | `scenarios/timing-movement-old-new.md` | `` |
| `timing/overlap` | `accepted` | `10` | `0` | `feature156-required-v1:timing-overlap` | `scenarios/timing-overlap.md` | `` |
| `timing/edge-clipping` | `accepted` | `10` | `0` | `feature156-required-v1:timing-edge-clipping` | `scenarios/timing-edge-clipping.md` | `` |

## Excluded Reasons

- none

## Evidence Links

- Scenario reports: `scenarios/`
- Raw samples: `raw/`
- Excluded samples: `excluded/`
- Unsupported host: `unsupported/README.md`
- Proof/probe evidence: `../proof-probes/README.md`

## Remaining Gates

- Feature 159 net-positive reuse/promotion counters: `remaining`
- Feature 161 host performance lane ledger: `remaining`
- Feature 160 validation throughput follow-up: `remaining`, not a shipped performance-acceptance gate

## Diagnostics

- readback-free timing measurement completed
- shipped performance claim remains performance-not-accepted

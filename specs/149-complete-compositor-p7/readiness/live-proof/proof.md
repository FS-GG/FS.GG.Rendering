# Feature 149 Live Compositor Proof

Proof: `20260618-050050`
Scenario: `proof/live-sentinel-damage-v1`
Verdict: `environment-limited`
Created: `2026-06-18T05:00:50.2959282+00:00`

## Host Profile

- Profile: `probe-1046a800`
- Backend: `OpenGL`
- Renderer: `AMD Radeon Graphics (radeonsi, renoir, ACO, DRM 3.64, 7.0.11-arch1-1)`
- Present mode: `DirectToSwapchain`
- Framebuffer: `640x480`
- Scale: `1`
- Environment: `x11`
- Algorithm: `sentinel-damage-v1`
- Package version: `local-harness`

## Required Artifacts

- `sentinel-frame.*`: full sentinel frame before damage.
- `damage-frame.*`: scissored damage/no-clear frame.
- `proof.md`: this proof summary.
- `proof.json`: optional machine-readable proof record.

## Evidence Artifacts

- `proof.md`
- `limitations.md`

## Acceptance Gate

- Accepted partial redraw requires three fresh capable-host runs with matching host profile and algorithm.
- Missing, stale, blank, synthetic-only, failed, environment-limited, host-mismatched, or algorithm-mismatched evidence fails closed.

## Diagnostics

- backend=x11
- package=local-harness
- verdict=environment-limited

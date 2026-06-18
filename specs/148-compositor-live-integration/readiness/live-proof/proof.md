# Feature 148 Live Preservation Proof

Proof: `20260618-040426`
Scenario: `proof/live-sentinel-damage-v1`
Verdict: `environment-limited`
Created: `2026-06-18T04:04:26.3504074+00:00`

## Host Profile

- Profile: `probe-cfdfc555`
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

## Evidence Artifacts

- `proof.md`
- `limitations.md`

## Sample Regions

- Untouched samples must retain sentinel identity.
- Damaged samples must reflect only the damage draw.
- Missing samples produce `environment-limited` instead of readiness.

## Diagnostics

- backend=x11
- package=local-harness
- verdict=environment-limited

# Feature 152 Live Proof Run Set

Proof: `20260618-083408`
Scenario: `proof/live-sentinel-damage-v1`
Verdict: `environment-limited`
Created: `2026-06-18T08:34:08.4862086+00:00`

## Host Profile

- Profile: `probe-3d6ca238`
- Backend: `OpenGL`
- Renderer: `AMD Radeon Graphics (radeonsi, renoir, ACO, DRM 3.64, 7.0.11-arch1-1)`
- Present mode: `DirectToSwapchain`
- Framebuffer: `640x480`
- Scale: `1`
- Environment: `x11`
- Algorithm: `sentinel-damage-v1`
- Package version: `local-harness`

## Required Artifacts

- `run-1/proof.md`, `run-2/proof.md`, `run-3/proof.md`: three fresh matching capable-host attempts.
- `sentinel-frame.*`: first full-frame sentinel artifact for each attempt.
- `damage-frame.*`: scissored damage/no-clear artifact for each attempt.
- `unsupported/README.md`: unsupported-host record with zero accepted artifacts.

## Acceptance Gate

- Accepted partial redraw requires three fresh matching capable-host runs for the same host profile and proof method.
- Missing, stale, blank, synthetic-only, failed, environment-limited, host-mismatched, or proof-method-mismatched evidence fails closed.
- Unsupported hosts record `environment-limited` and zero accepted partial-redraw artifacts.

## Evidence Artifacts

- `proof.md`
- `limitations.md`

## Diagnostics

- backend=x11
- package=local-harness
- verdict=environment-limited

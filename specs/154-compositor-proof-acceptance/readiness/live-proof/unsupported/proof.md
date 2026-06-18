# Feature 154 Compositor Proof Acceptance

Proof: `20260618-104702`
Scenario: `proof/live-sentinel-damage-v1`
Verdict: `environment-limited`
Created: `2026-06-18T10:47:02.1643496+00:00`

## Host Profile

- Profile: `probe-e453c0ef`
- Backend: `OpenGL`
- Renderer: `unknown`
- Present mode: `DirectToSwapchain`
- Framebuffer: `640x480`
- Scale: `1`
- Environment: `missing-display`
- Algorithm: `sentinel-damage-v1`
- Package version: `local-harness`

## Acceptance Gate

- Accepted proof requires exactly three selected fresh matching capable-host attempts from one host profile and one proof method.
- Each accepted attempt must include fresh, decodable, non-blank, non-synthetic sentinel and damage artifacts.
- Damaged pixels must update and undamaged pixels must preserve the sentinel identity.
- Unsupported, stale, missing, blank, undecodable, synthetic-only, incomplete, failed-pixel, host-mismatched, or proof-method-mismatched evidence fails closed.
- Unsupported-host output records zero accepted partial-redraw artifacts.

## Evidence Artifacts

- `proof.md`
- `limitations.md`
- `attempts/README.md`
- `unsupported/README.md`

## Diagnostics

- backend=missing-display
- package=local-harness
- attempt-count=1
- verdict=environment-limited

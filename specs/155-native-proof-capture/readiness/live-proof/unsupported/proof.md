# Feature 155 Native Proof Capture

Proof: `20260618-114119`
Scenario: `proof/live-sentinel-damage-v1`
Verdict: `environment-limited`
Created: `2026-06-18T11:41:19.3626876+00:00`

## Host Profile

- Profile: `probe-eb6566e6`
- Backend: `OpenGL`
- Renderer: `unknown`
- Present mode: `DirectToSwapchain`
- Framebuffer: `640x480`
- Scale: `1`
- Environment: `missing-display`
- Algorithm: `sentinel-damage-v1`
- Package version: `local-harness`

## Native Capture Gate

- Capable-host proof capture must produce exactly three selected fresh matching attempts.
- Each selected attempt must include current-run sentinel and damage artifacts.
- Each selected attempt must be fresh, decodable, non-blank, and non-synthetic.
- Damaged pixels must update and undamaged pixels must preserve the sentinel identity.
- Unsupported-host output records zero accepted partial-redraw artifacts.

## Evidence Artifacts

- `proof.md`
- `limitations.md`
- `README.md`

## Diagnostics

- backend=missing-display
- package=local-harness
- attempt-count=0
- verdict=environment-limited
- reason=missing display

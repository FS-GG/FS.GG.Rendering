# Feature 155 Native Proof Capture

Proof: `feature155-20260618114112-2`
Scenario: `proof/live-sentinel-damage-v1`
Verdict: `passed`
Created: `2026-06-18T11:41:12.4030714+00:00`

## Host Profile

- Profile: `probe-08a47c01`
- Backend: `OpenGL`
- Renderer: `AMD Radeon Graphics (radeonsi, renoir, ACO, DRM 3.64, 7.0.11-arch1-1)`
- Present mode: `DirectToSwapchain`
- Framebuffer: `640x480`
- Scale: `1`
- Environment: `x11`
- Algorithm: `sentinel-damage-v1`
- Package version: `local-harness`

## Native Capture Gate

- Capable-host proof capture must produce exactly three selected fresh matching attempts.
- Each selected attempt must include current-run sentinel and damage artifacts.
- Each selected attempt must be fresh, decodable, non-blank, and non-synthetic.
- Damaged pixels must update and undamaged pixels must preserve the sentinel identity.
- Unsupported-host output records zero accepted partial-redraw artifacts.

## Evidence Artifacts

- `feature155-20260618114112-2/sentinel-frame.png`
- `feature155-20260618114112-2/damage-frame.png`
- `feature155-20260618114112-2/proof.md`

## Diagnostics

- backend=x11
- renderer=AMD Radeon Graphics (radeonsi, renoir, ACO, DRM 3.64, 7.0.11-arch1-1)
- display=:1
- gl-direct=true
- refresh-hz=119.93
- attempt=2
- workflow=DetectProfile>PresentSentinelFrame>PresentDamageFrame>ObservePixels>WriteProofArtifact
- sentinel-status=ScreenshotOk
- damage-status=ScreenshotOk
- sentinel-nonblank=true
- damage-nonblank=true
- undamaged-preserved=true
- damaged-updated=true
- verdict=passed

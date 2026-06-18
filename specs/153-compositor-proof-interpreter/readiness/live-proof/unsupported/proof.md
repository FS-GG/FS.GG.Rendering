# Feature 153 Live Proof Interpreter

Proof: `20260618-095153`
Scenario: `proof/live-sentinel-damage-v1`
Verdict: `environment-limited`
Created: `2026-06-18T09:51:53.6309957+00:00`

## Host Profile

- Profile: `probe-8ad4501d`
- Backend: `OpenGL`
- Renderer: `unknown`
- Present mode: `DirectToSwapchain`
- Framebuffer: `640x480`
- Scale: `1`
- Environment: `missing-display`
- Algorithm: `sentinel-damage-v1`
- Package version: `local-harness`

## Attempt Evidence

- `attempts/`: capable-host attempt summaries and frame artifacts when the host can capture them.
- `attempts/<attempt-id>/sentinel-frame.png`: sentinel frame artifact.
- `attempts/<attempt-id>/damage-frame.png`: damage-scoped frame artifact.
- `unsupported/`: environment-limited output with zero accepted partial-redraw artifacts.

## Acceptance Gate

- Accepted proof requires exactly three selected fresh matching capable-host attempts.
- Missing, stale, blank, synthetic-only, failed, environment-limited, host-mismatched, or proof-method-mismatched evidence fails closed.
- This feature does not enable partial redraw or accept a compositor performance claim.

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

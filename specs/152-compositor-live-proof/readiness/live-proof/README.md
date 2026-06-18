# Feature 152 Live Proof Evidence Index

Status: `environment-limited`

Feature 152 requires three fresh matching capable-host live proof attempts before P7 live partial redraw can be accepted. The current local run records the gate and rejection rules, but it does not claim acceptance because this environment did not produce live sentinel/damage OpenGL readback artifacts.

## Required Capable-Host Runs

| Run | Required Artifacts | Current Status |
|-----|--------------------|----------------|
| `run-1` | `sentinel-frame.*`, `damage-frame.*`, `proof.md` | missing capable-host artifact |
| `run-2` | `sentinel-frame.*`, `damage-frame.*`, `proof.md` | missing capable-host artifact |
| `run-3` | `sentinel-frame.*`, `damage-frame.*`, `proof.md` | missing capable-host artifact |

Acceptance requires all three runs to share host profile, proof method, framebuffer, scale, package identity, freshness window, non-blank artifacts, and non-synthetic artifact quality.

## Rejection Rules

Missing, stale, blank, synthetic-only, failed, environment-limited, host-mismatched, proof-method-mismatched, or missing-artifact evidence remains non-accepting and keeps full redraw as the safe path.

## Unsupported Host

See `unsupported/README.md`. Unsupported-host evidence is valid only as an `environment-limited` record and must report zero accepted partial-redraw artifacts.


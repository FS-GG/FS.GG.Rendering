# Contract: Damage-Scoped Live Parity

## Scope

This contract defines the live parity gate after an accepted proof set exists. Damage-scoped output
must match the full-redraw oracle for representative frame transitions on the same accepted host
profile.

## Preconditions

Damage-scoped parity may run only when:

- A fresh accepted proof set exists for the active host profile and proof method.
- The compositor mode is enabled.
- The frame has valid damage or a valid no-change preservation decision after a prior frame.
- The scenario does not require full-frame invalidation.
- The host supports the scissor/no-clear path used by the proof.
- A full-redraw oracle artifact can be produced for comparison.

If any precondition fails, the frame uses full redraw or another safe fallback and records a reason.

## Required Corpus Coverage

The live parity corpus must include:

- Localized update.
- No-change frame after a valid prior frame.
- Placement-only movement with old and new covered regions.
- Overlapping and edge-clipped damage.
- Resize or framebuffer-size change.
- Frame-wide invalidation.
- Invalid damage information.
- Unsupported or unavailable host path.
- Resource or internal failure path.

Additional Feature 149 deterministic parity cases may remain in regression coverage but cannot
replace the live capable-host parity gate.

## Parity Rules

- The same host profile and proof method must be used for proof and accepted scoped parity.
- Damage regions are clipped to the frame and overlaps count once.
- Full-redraw output is the oracle.
- Damage-scoped output must match the oracle for accepted scenarios.
- No-change preservation is accepted only after a valid prior frame and accepted proof set.
- Resize, frame-wide invalidation, host-profile drift, invalid damage, resource failure, and
  parity failure require full redraw or a recorded safe fallback.

## Evidence Records

Each parity result records:

- Host profile id and proof set id.
- Scenario id and frame id.
- Damage regions, union area, and full-frame invalidation flag.
- Applied scissor/no-clear state when scoped redraw runs.
- Full-redraw oracle artifact.
- Damage-scoped output artifact, or fallback artifact.
- Parity verdict and fallback reason.
- Diagnostics sufficient to distinguish environment limits from defects.

## Acceptance Tests

- Localized live updates repaint damage-scoped output and match the full-redraw oracle.
- No-change frames preserve valid output or fall back with a reason.
- Movement damages old and new regions and matches the oracle.
- Resize, full invalidation, invalid damage, host-profile change, and resource failure choose full
  redraw or another safe fallback.
- Unsupported hosts record `environment-limited` and do not count as accepted parity.
- Parity failure rejects scoped readiness even when proof and timing evidence exist.

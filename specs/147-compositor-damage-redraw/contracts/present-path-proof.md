# Contract: Present Path Proof

## Scope

This contract defines the proof required before damage-scissored redraw can preserve untouched
pixels between presents. It covers the active SkiaViewer/OpenGL host profile, proof evidence, safe
failure, and readiness gating.

## Public or Observable Surface

The implementation must expose a proof surface in `src/SkiaViewer` with a companion `.fsi` if the
proof result is public, package-visible, or consumed by the harness outside internals.

Required capabilities:

- Describe the active host profile.
- Run a controlled sentinel/damage present proof.
- Return `passed`, `failed`, or `environment-limited`.
- Persist or return proof artifacts and diagnostics.
- Reject stale, missing, synthetic, or host-mismatched proof evidence for readiness.

## Proof Sequence

The proof sequence must:

- Draw a full-frame sentinel pattern through the real host profile under test.
- Present the sentinel frame.
- Redraw only a known damaged region using the scissor path and no full-frame clear.
- Present the second frame.
- Observe enough pixels to verify that untouched sentinel regions survived and the damaged region
  changed.
- Record environment limitations honestly when observation cannot be completed.

## Host Profile Identity

The host profile record contains:

- Backend and present mode.
- Renderer/vendor/version facts when available.
- Framebuffer size and scale.
- Display environment.
- Proof algorithm version.
- Package or binary version when available.

Proof evidence is valid only when the active profile matches the recorded profile.

## Verdict Rules

- `passed`: every checked untouched region matches the sentinel and every checked damaged region
  matches the second-frame draw.
- `failed`: the host clears, corrupts, or cannot preserve untouched regions despite a valid
  observation path.
- `environment-limited`: display, GL context, readback, timing, permissions, or other environment
  limitations prevent an honest verdict.

Only `passed` can enable damage-scissored redraw readiness.

## MVU Boundary

The proof workflow must be testable as:

- `Model`: host profile, proof phase, observed pixels/artifacts, verdict, diagnostics.
- `Msg`: profile detected, sentinel presented, damage presented, observation completed, failure
  classified, artifact written.
- `Effect`: detect profile, draw/present frame, observe/read pixels, write artifact, classify host
  limitation.
- `update`: pure transition from message and model to next model plus effects.
- Interpreter: executes GL, filesystem, and timing effects at the SkiaViewer/harness edge.

## Acceptance Tests

- Capable profile produces `passed` proof with recorded host profile and artifact identity.
- Fresh-clearing or simulated non-preserving profile produces `failed` and disables scissoring.
- Missing display or unsupported observation produces `environment-limited` and disables
  scissoring.
- Host-profile mismatch, stale proof, missing proof, and changed proof algorithm reject readiness.
- Same-seed proof scenarios produce stable scenario ids and verdict fields.

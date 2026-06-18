# Contract: Live Preservation Proof

## Scope

This contract extends Feature 147's present-path proof from deterministic/environment-limited
readiness into a live SkiaViewer/OpenGL preservation proof. The proof decides whether the active
host profile can safely run damage-scoped redraw without repainting untouched regions.

## Public or Observable Surface

If any proof type, helper, harness command, or testing assertion is package-visible, it must be
declared in the corresponding `.fsi` before implementation. The observable surface must support:

- Describing the active host profile.
- Running the live sentinel/damage proof.
- Returning `passed`, `failed`, or `environment-limited`.
- Recording proof artifacts and diagnostics.
- Rejecting missing, stale, synthetic-only, failed, environment-limited, host-mismatched, or
  algorithm-mismatched evidence.

## Proof Sequence

The live proof must:

- Draw and present a full-frame sentinel pattern through the active host profile.
- Draw and present a second frame that changes only a known damage rectangle.
- Use the same scissor/no-clear path later used by damage-scoped redraw.
- Observe enough pixels after the second present to verify untouched sentinel regions and the
  damaged region.
- Record host profile facts, algorithm version, artifacts, timestamp, and diagnostics.
- Return `environment-limited` when a display, GL context, readback, timing, permission, or
  observation limitation prevents an honest verdict.

## Verdict Rules

- `passed`: all checked untouched regions match the sentinel and all checked damaged samples match
  the second-frame draw.
- `failed`: the host clears, corrupts, presents stale damaged pixels, or otherwise cannot preserve
  untouched regions despite valid observation.
- `environment-limited`: the environment cannot produce an honest proof.

Only `passed` for the active matching host profile may unlock damage-scoped readiness.

## Freshness and Matching Rules

Proof evidence is accepted only when:

- The proof verdict is `passed`.
- The active host profile matches the proof host profile.
- The proof algorithm version matches the current implementation.
- Required artifacts exist and are readable.
- The freshness policy accepts the proof for the current run.

Any rejection must produce a fallback reason consumable by damage-scoped redraw diagnostics.

## MVU Boundary

The proof workflow must be testable through:

- `Model`: host profile, proof phase, artifacts, observed samples, verdict, diagnostics.
- `Msg`: profile detected, sentinel presented, damage presented, samples observed, failure
  classified, artifact written.
- `Effect`: detect profile, draw/present, set scissor, read pixels, write artifact, classify
  limitation.
- `update`: pure transition from `Msg` and `Model` to next `Model` plus effects.
- Interpreter: executes GL, filesystem, timing, and environment effects at the SkiaViewer/harness
  edge.

## Acceptance Tests

- Capable host profile produces `passed` proof with matching untouched and damaged samples.
- Fresh-clearing or simulated non-preserving host produces `failed` and disables partial redraw.
- Missing display, unsupported observation, timeout, or permission limitation produces
  `environment-limited` and disables partial redraw.
- Host mismatch, stale proof, missing artifact, and algorithm mismatch reject readiness.
- Same-seed proof runs produce stable scenario ids, verdict categories, and artifact references.

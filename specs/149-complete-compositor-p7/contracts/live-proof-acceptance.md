# Contract: Live Proof Acceptance

## Scope

This contract defines the final P7 live proof gate. It turns the existing proof model into a real
SkiaViewer/OpenGL preservation test and decides whether partial redraw may be accepted for the
active host profile.

## Public or Observable Surface

If proof records, readiness helpers, harness commands, or testing assertions are package-visible,
they must be declared in the corresponding `.fsi` before implementation. The observable surface
must support:

- Describing the active host profile.
- Running the live sentinel/damage proof.
- Returning `accepted`, `failed`, or `environment-limited`.
- Recording frame artifacts, pixel observations, and diagnostics.
- Rejecting missing, stale, blank, synthetic-only, failed, environment-limited,
  host-mismatched, or algorithm-mismatched evidence.

## Proof Sequence

The live proof must:

- Detect the active SkiaViewer/OpenGL host profile.
- Draw and present a full-frame sentinel pattern through the active presenter.
- Draw and present a second frame that changes only a known damage rectangle.
- Use the same scissor/no-clear path later used by damage-scoped redraw.
- Observe enough pixels after the second present to verify untouched sentinel regions and the
  damaged region.
- Persist artifacts that show the sentinel frame, the damage frame, sample regions, and verdict.
- Return `environment-limited` when display, GL context, readback, timing, permission, or
  observation limitations prevent an honest verdict.

## Acceptance Rules

- `accepted`: all checked untouched regions match the sentinel and all checked damaged samples
  match the second-frame draw.
- `failed`: the host clears, corrupts, presents stale damaged pixels, produces blank/stale
  artifacts, or otherwise cannot prove preservation.
- `environment-limited`: the environment cannot produce an honest proof.

Only `accepted` for a fresh, matching active host profile may unlock damage-scoped rendering.

## Freshness and Matching Rules

Proof evidence is accepted only when:

- The proof verdict is `accepted`.
- The active host profile matches the proof host profile.
- The proof algorithm version matches the current implementation.
- Required artifacts exist, are readable, and are not blank/stale.
- The freshness policy accepts the proof for the current run.

Any rejection must produce a fallback reason consumable by damage-scoped redraw diagnostics.

## MVU Boundary

The proof workflow must be testable through:

- `Model`: host profile, proof phase, artifacts, observed samples, verdict, and diagnostics.
- `Msg`: profile detected, sentinel presented, damage presented, samples observed, failure
  classified, artifact written.
- `Effect`: detect profile, draw/present, set scissor, read pixels, write artifact, classify
  limitation.
- `update`: pure transition from `Msg` and `Model` to next `Model` plus effects.
- Interpreter: executes GL, filesystem, timing, and environment effects at the SkiaViewer/harness
  edge.

## Acceptance Tests

- A capable host produces accepted proof with matching untouched and damaged samples.
- Three consecutive capable-host proof runs produce accepted artifacts with no stale, blank, or
  missing-artifact failures.
- A fresh-clearing, stale, blank, or simulated non-preserving host produces `failed` and disables
  partial redraw.
- Missing display, unsupported readback, timeout, permission, or unavailable GL produces
  `environment-limited` and disables partial redraw.
- Host mismatch, stale proof, missing artifact, synthetic-only proof, and algorithm mismatch reject
  readiness.

# Contract: Live Proof Run-Set Acceptance

## Scope

This contract defines the Feature 152 proof gate that can accept live partial redraw for a capable
host. It extends the existing Feature 149 proof vocabulary with run-set aggregation and stricter
artifact quality rules.

## Public or Observable Surface

Any package-visible type, helper, formatter, readiness field, or harness command argument must be
declared in the corresponding `.fsi` before implementation and covered by semantic tests. The
observable surface must support:

- Recording the active host profile and proof method.
- Running or loading individual live proof attempts.
- Aggregating attempts into an accepted proof set.
- Returning `accepted`, `failed`, or `environment-limited` with reviewer-visible reasons.
- Rejecting missing, stale, blank, synthetic-only, failed, environment-limited, host-mismatched,
  or proof-method-mismatched evidence.
- Reporting zero accepted artifacts on unsupported or unavailable hosts.

## Required Run Set

An accepted proof set requires:

- At least three proof attempts.
- Every attempt has verdict `accepted`.
- Every attempt uses the same host profile and proof method.
- Every attempt is fresh for the current validation run.
- Required artifacts exist, decode, are non-blank, are not stale, and are not synthetic.
- Damaged samples show the second-frame damage draw.
- Undamaged samples retain the sentinel identity from the prior frame.

If any run fails these rules, the proof set remains unaccepted and records the blocking reason.

## Verdict Rules

- `accepted`: three fresh matching capable-host attempts satisfy all sample and artifact-quality
  rules.
- `failed`: live readback completed but the host cleared, corrupted, or failed to update pixels, or
  artifacts are stale, blank, missing, mismatched, or otherwise invalid.
- `environment-limited`: display, GL context, readback, permission, timeout, renderer, or host
  setup prevents an honest proof.

Only `accepted` can unlock live damage-scoped readiness for the matching host profile.

## Freshness and Matching Rules

Proof evidence is valid only when:

- The active host profile matches the proof host profile.
- The proof method and algorithm version match.
- The framebuffer size and scale match.
- The package or harness identity is recorded.
- Attempt timestamps are fresh enough for the current readiness run.
- Artifact identities match the run under review.

Any drift produces a fallback reason consumable by damage-scoped rendering and readiness output.

## MVU Boundary

The proof workflow must be testable through:

- `Model`: active host profile, proof method, attempt list, artifact quality, sample observations,
  run-set state, verdict, and diagnostics.
- `Msg`: profile detected, attempt started, sentinel presented, damage presented, samples
  observed, artifact validated, attempt classified, run set evaluated.
- `Effect`: detect profile, draw/present, set scissor, read pixels, validate artifacts, write
  summaries, classify environment limits.
- `update`: pure transition from `Msg` and `Model` to next `Model` plus effects.
- Interpreter: executes GL, filesystem, timing, and environment effects at the SkiaViewer or
  harness edge.

## Acceptance Tests

- Three fresh matching capable-host attempts produce an accepted proof set.
- A single accepted attempt does not accept partial redraw.
- Host mismatch, proof-method mismatch, stale artifacts, blank artifacts, missing artifacts, and
  synthetic-only artifacts fail closed.
- Non-preserving hosts classify as `failed` and record the pixel condition.
- Missing display, unsupported readback, timeout, permission, or unavailable GL classifies as
  `environment-limited`, completes under 2 minutes, and records zero accepted artifacts.

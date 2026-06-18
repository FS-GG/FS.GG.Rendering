# Contract: Live Proof Attempt

## Scope

This contract defines the observable behavior of one real host-backed compositor proof attempt.
The attempt must exercise the presentation host, capture sentinel and damage-frame evidence, and
classify the result with Feature 152 vocabulary.

## Public or Observable Surface

Any package-visible type, helper, formatter, readiness field, or harness command argument must be
declared in the corresponding `.fsi` before implementation and covered by semantic tests. The
observable surface must support:

- Recording the active host profile and proof method.
- Running or loading one live proof attempt.
- Capturing sentinel-frame and damage-frame artifacts.
- Recording artifact quality and sample observations.
- Returning `accepted`, `failed`, or `environment-limited` with reviewer-visible reasons.
- Reporting zero accepted partial-redraw artifacts for unsupported or unavailable hosts.

## Attempt Inputs

An attempt receives or discovers:

- Active host profile.
- Proof method and algorithm version.
- Output readiness directory.
- Damage region and sample plan.
- Current run identity.
- Freshness window.

## Attempt Output

Each attempt records:

- Attempt id.
- Host profile and proof method.
- Sentinel artifact path.
- Damage artifact path.
- Damaged-region sample observations.
- Undamaged-region sample observations.
- Artifact quality decision.
- Final classification and reason.
- Diagnostics sufficient to distinguish host limits from implementation defects.

## Acceptance Rules

An attempt is `accepted` only when:

- The host profile is capable and fully recorded.
- Required sentinel and damage artifacts exist.
- Required artifacts are decodable, non-blank, non-stale, current-run, and non-synthetic.
- Damaged samples show the expected damaged-frame update.
- Undamaged samples preserve the sentinel-frame identity.
- The proof method matches the active method.

An attempt is `failed` when live evidence is available but shows incorrect pixels, invalid
quality, stale artifacts, blank artifacts, undecodable artifacts, or another implementation
failure.

An attempt is `environment-limited` when display, GL context, readback, permission, timeout,
renderer, or host setup prevents honest proof evidence.

## MVU Boundary

The attempt workflow must be testable through:

- `Model`: active profile, proof method, phase, artifacts, observations, quality, verdict, and
  diagnostics.
- `Msg`: profile detected, attempt started, sentinel presented, damage presented, artifact
  written, samples observed, artifact validated, attempt classified.
- `Effect`: detect profile, create host, present sentinel, present damage, read pixels, validate
  artifacts, write summaries, classify environment limits.
- `update`: pure transition from `Msg` and `Model` to next `Model` plus effects.
- Interpreter: executes GL, X11/Xvfb, filesystem, and environment effects at the SkiaViewer or
  harness edge.

## Acceptance Tests

- A capable-host attempt records host profile, proof method, sentinel artifact, damage artifact,
  artifact quality, freshness, and classification.
- A preserving host accepts the attempt.
- Missing, stale, blank, synthetic-only, undecodable, or mismatched artifacts fail closed.
- Damaged pixels not updating fails closed.
- Undamaged pixels not preserving sentinel identity fails closed.
- Missing display, unsupported readback, timeout, permission, or unavailable GL produces
  `environment-limited`, completes under 2 minutes, and records zero accepted artifacts.

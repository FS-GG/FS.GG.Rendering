# Data Model: Native Proof Capture

## Host Capability Record

**Purpose**: Facts used to decide whether the current host can run accepted live proof capture.

**Fields**:

- Display environment and display token.
- Renderer identity.
- Readback availability.
- Permission status.
- Timeout status.
- Framebuffer size and scale.
- Proof method.

**Validation rules**:

- Missing display, missing renderer, unavailable readback, denied permission, or timeout prevents
  accepted proof capture.
- A capable record must produce a stable host profile used by proof, parity, and timing evidence.

## Proof Workflow State

**Purpose**: Observable state for the native proof-capture workflow.

**States**:

```text
initialized -> profile-detected -> sentinel-presented -> damage-presented
            -> pixels-observed -> artifact-written
            -> accepted | failed | environment-limited
```

**Validation rules**:

- State transitions occur only through messages.
- Native I/O is requested as effects and interpreted at the edge.
- Any failure state records a specific reviewer-visible reason.

## Proof Workflow Effect

**Purpose**: Native work requested by the pure proof workflow.

**Effect types**:

- Detect host profile.
- Present sentinel frame.
- Present damage frame.
- Observe damaged and undamaged pixels.
- Evaluate artifact quality.
- Write proof attempt artifacts.
- Record failure or timeout.

**Validation rules**:

- Effects must be interpretable on a capable host.
- Failed effects must return messages that fail closed.

## Native Proof Run

**Purpose**: A current closeout execution that collects accepted proof attempts.

**Fields**:

- Run identity.
- Output directory.
- Host capability record.
- Host profile.
- Attempt count.
- Freshness window.
- Proof method.
- Diagnostics.

**Validation rules**:

- A closeout run needs three selected accepted attempts to accept the proof set.
- Attempt identities must be current-run identities and must not silently reuse older artifacts.

## Proof Attempt Artifact

**Purpose**: Durable reviewer-visible output for one native capture attempt.

**Fields**:

- Attempt identity.
- Host profile.
- Proof method.
- Sentinel artifact path and quality.
- Damage artifact path and quality.
- Damaged sample observations.
- Undamaged sample observations.
- Verdict and reason.
- Diagnostics.

**Validation rules**:

- Accepted attempts require fresh, decodable, non-blank, non-synthetic sentinel and damage
  artifacts.
- Accepted attempts require damaged pixels to update and undamaged pixels to preserve sentinel
  identity.
- Missing, stale, blank, undecodable, synthetic-only, failed-pixel, incomplete, mismatched, or
  timed-out attempts fail closed.

## Accepted Proof Set

**Purpose**: The exact three selected accepted attempts that unlock the proof gate.

**Fields**:

- Proof set identity.
- Host profile.
- Proof method.
- Selected attempt identities.
- Attempts.
- Freshness policy.
- Accepted timestamp.
- Reasons and diagnostics.

**Validation rules**:

- Accepted proof sets require exactly three selected accepted attempts.
- Selected attempts must share host profile and proof method.
- Extra attempts may be context but cannot hide contradictory evidence.

## P7 Closeout Summary

**Purpose**: Single review entry point for final P7 status.

**Fields**:

- Proof-set status.
- Selected attempts and artifact links.
- Host profile.
- Parity status.
- Timing status.
- Fallback status.
- Unsupported-host status.
- Compatibility impact.
- Package validation.
- Regression validation.
- Remaining limitations.

**Validation rules**:

- P7 partial-redraw correctness is accepted only when proof set and same-profile parity are both
  accepted.
- Performance claim status is reported separately.
- Unsupported-host evidence cannot be counted as accepted proof.

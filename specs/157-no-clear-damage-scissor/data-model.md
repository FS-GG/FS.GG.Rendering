# Data Model: No-Clear Damage-Scissored Render Path

## Host Proof Gate

**Purpose**: The accepted proof that allows the runtime path to consider damage-scoped repaint.

**Fields**:

- Proof set id.
- Selected attempt ids.
- Host profile.
- Proof method and algorithm version.
- Accepted timestamp and freshness window.
- Artifact quality status.
- Diagnostics.

**Validation rules**:

- Must be accepted through Feature 155-compatible proof-set evaluation.
- Must match the active host profile, renderer identity, display environment, present mode,
  framebuffer size, package version, and proof algorithm.
- Synthetic-only, stale, cross-profile, rejected, or environment-limited proof cannot unlock the
  damage-scoped path.

## Host Profile

**Purpose**: Stable identity used to decide whether proof, attempts, parity, and readiness are
comparable.

**Fields**:

- Profile id.
- Backend and renderer identity.
- Present mode.
- Framebuffer size and scale.
- Display environment.
- Package and harness version.
- Proof algorithm version.

**Validation rules**:

- Accepted Feature 157 attempts must use stable profile `probe-08a47c01` unless a later accepted
  proof explicitly replaces it.
- Evidence from different profiles cannot be combined in one accepted result.

## Retained Frame State

**Purpose**: Trusted previous frame content used to preserve pixels outside damage.

**Fields**:

- Retained frame id.
- Previous scene identity or fingerprint.
- Run identity.
- Host profile.
- Framebuffer size.
- Backing kind: current-buffer-preserved or retained-frame-restored.
- Backing artifact path when captured.
- Validity status and diagnostics.

**Validation rules**:

- Must belong to the current run, current host profile, and previous frame.
- Must match the current framebuffer size and scale.
- Lost, stale, cross-run, cross-profile, resized, disposed, or resource-failed backing forces full
  redraw.

## Damage Region

**Purpose**: Declared visible area that must be repainted for the current frame.

**Fields**:

- Damage id.
- Frame id.
- Rectangles in framebuffer coordinates.
- Union bounds and union area.
- Source scenario.
- Damage definition version.
- Validation status.

**Validation rules**:

- Rectangles are clamped to the framebuffer and empty clipped rectangles are discarded.
- Empty damage is valid only for a no-visible-change scenario.
- Out-of-bounds, stale, duplicate-only, incomplete movement, ambiguous, or disconnected damage
  forces full redraw with a damage-validation reason.
- Resize and full-frame invalidation use full redraw.

## Damage Validation Result

**Purpose**: Reviewer-visible classification for damage input.

**Values**:

- `valid`
- `empty-no-change`
- `empty-visible-change`
- `out-of-bounds`
- `stale`
- `duplicated`
- `incomplete`
- `ambiguous`
- `full-frame-invalidation`

**Validation rules**:

- Only `valid` and `empty-no-change` can avoid rejection; `empty-no-change` normally skips repaint.
- Any invalid status must include a fallback reason in the attempt record.

## Damage-Scoped Frame Attempt

**Purpose**: One frame repaint decision and result.

**Fields**:

- Attempt id and run id.
- Frame id and previous frame id.
- Host profile.
- Proof gate reference.
- Retained frame state.
- Damage region and validation result.
- Render decision.
- Fallback reason when applicable.
- Preserved-pixel evidence.
- Damaged-pixel evidence.
- Parity result.
- Artifact paths.
- Diagnostics.

**Validation rules**:

- Accepted attempts require all eligibility gates to pass.
- Attempts with any fallback reason record zero accepted partial-redraw artifacts.
- Accepted attempts must include preserved-pixel, damaged-pixel, and parity evidence.

## Render Decision

**Purpose**: The host-level decision for a frame.

**Values**:

- `damage-scoped-accepted`
- `full-redraw`
- `skip-no-change`
- `rejected`
- `environment-limited`

**Validation rules**:

- `damage-scoped-accepted` is allowed only after proof, retained backing, damage, resource, and
  parity gates pass.
- `full-redraw` is the default when any gate is missing or unverifiable.
- `environment-limited` is never accepted partial redraw.

## Full-Redraw Fallback

**Purpose**: Safe rendering behavior used whenever damage-scoped repaint is not eligible.

**Fields**:

- Fallback reason.
- Original requested path.
- Frame id.
- Host profile.
- Diagnostics.
- Evidence artifacts.

**Reason categories**:

- `proof-rejection`
- `host-limitation`
- `missing-retained-content`
- `invalid-damage`
- `resource-failure`
- `parity-mismatch`
- `environment-limitation`
- `full-frame-invalidation`

**Validation rules**:

- Every fallback must choose exactly one primary reason and may include additional diagnostics.
- Fallback output must be produced through the existing full-redraw path.

## Parity Result

**Purpose**: Comparison between damage-scoped output and equivalent full redraw.

**Fields**:

- Scenario id.
- Full-redraw artifact.
- Damage-scoped artifact.
- Allowed damage region.
- Preserved-region comparison.
- Damaged-region comparison.
- Outside-damage drift count.
- Verdict.
- Diagnostics.

**Validation rules**:

- Accepted parity requires zero unexplained drift outside damage and expected changes inside damage.
- Parity mismatch rejects the attempt and gates future damage-scoped frames until fresh proof and
  retained backing are available.

## Readiness Summary

**Purpose**: Single reviewer-facing entry point for Feature 157.

**Fields**:

- Final status: accepted, fallback-only, rejected, or environment-limited.
- Host profile and run identity.
- Accepted attempts.
- Rejected and fallback attempts.
- Scenario coverage.
- Damage validation statuses.
- Preserved-pixel evidence.
- Damaged-pixel evidence.
- Parity status.
- Fallback reasons.
- Artifact paths.
- Compatibility impact.
- Performance claim status.
- Remaining gates.

**Validation rules**:

- Accepted status requires at least three fresh current-host attempts and at least five
  representative scenarios.
- Unsupported-host packages must state zero accepted partial-redraw artifacts.
- Performance claim remains `performance-not-accepted` unless later report-defined gates are also
  satisfied.

## Damage Workflow State

**Purpose**: State for collecting and publishing damage-scoped evidence.

**States**:

```text
initialized -> profile-bound -> proof-gated -> retained-backing-ready
            -> damage-validated -> damage-rendered -> parity-checked
            -> summary-published -> accepted | fallback-only | rejected | environment-limited
```

**Validation rules**:

- State transitions occur only through workflow messages.
- Native I/O is represented as effects and interpreted at the edge.
- Any failure state records a reviewer-visible reason.

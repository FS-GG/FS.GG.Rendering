# Contract: Proof Set Decision

## Scope

This contract defines the aggregate decision over the required three live proof attempts. It uses
the existing Feature 152 readiness vocabulary and does not redefine broader P7 parity or timing
policy.

## Required Proof Set

An accepted proof set requires:

- Exactly three selected attempts.
- Every selected attempt has classification `accepted`.
- Every selected attempt uses the same host profile.
- Every selected attempt uses the same proof method.
- Every selected attempt is fresh for the current validation run.
- Every selected attempt has accepted artifact quality.
- Every selected attempt proves both damaged-pixel update and undamaged-pixel preservation.

If more than three attempts are present, the proof-set record must name the exact three selected
attempts. Extra attempts may be linked as supporting context but cannot hide failed, stale, or
mismatched evidence.

## Verdict Rules

- `accepted`: exactly three fresh matching capable-host attempts satisfy all acceptance rules.
- `fallback-gated`: attempts are missing, fewer than three, stale, host-mismatched,
  proof-method-mismatched, synthetic-only, or otherwise non-accepting without a live host defect.
- `failed`: live evidence completed but the host cleared, corrupted, failed to update pixels, or
  produced invalid proof artifacts.
- `environment-limited`: display, GL context, readback, permission, timeout, renderer, or host
  setup prevents honest proof attempts.

Only `accepted` can support a later same-host parity gate. It does not by itself enable partial
redraw or accept a performance claim.

## Freshness and Matching Rules

Proof evidence is valid only when:

- The active host profile matches each attempt host profile.
- The proof method and algorithm version match.
- The framebuffer size and scale match.
- The package or harness identity is recorded.
- Attempt timestamps are fresh enough for the current readiness run.
- Artifact identities match the attempts under review.

Any drift produces a fallback reason consumable by readiness output.

## Required Output

The proof-set decision records:

- Proof-set id.
- Status token.
- Selected attempt ids.
- Host profile id.
- Proof method.
- Freshness window.
- Acceptance or blocking reasons.
- Links to each attempt summary and artifact directory.
- Statement that parity and timing remain separate gates.

## Acceptance Tests

- Exactly three fresh matching capable-host accepted attempts produce an accepted proof set.
- One or two accepted attempts remain fallback-gated.
- Four attempts require an explicit selected trio and cannot silently ignore a failed or mismatched
  attempt in the same evidence set.
- Host mismatch, proof-method mismatch, stale artifacts, blank artifacts, missing artifacts, and
  synthetic-only artifacts fail closed.
- Non-preserving hosts classify as `failed` and record the pixel condition.
- Unsupported hosts classify as `environment-limited` and record zero accepted partial-redraw
  artifacts.

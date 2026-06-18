# Contract: Capable Host Proof Set

## Scope

This contract defines the observable Feature 154 proof-set acceptance behavior. It reuses Feature
153's `CompositorProof` proof-attempt and proof-set vocabulary and adds the Feature 154 readiness
package around real capable-host attempts.

## Public or Observable Surface

Any package-visible type, helper, formatter, readiness field, command argument, or output token
must be declared in the corresponding `.fsi` before implementation and covered by semantic tests.
The observable surface must support:

- Loading or running live proof attempts for Feature 154.
- Recording host profile, proof method, selected attempt ids, freshness window, artifact quality,
  classification, and reasons.
- Evaluating a proof set with the existing exact-three fail-closed rules.
- Writing Feature 154 proof artifacts under
  `specs/154-compositor-proof-acceptance/readiness/live-proof/`.
- Recording unsupported-host runs with zero accepted partial-redraw artifacts.

## Attempt Inputs

Each attempt receives or discovers:

- Active host profile.
- Proof method and algorithm version.
- Current run identity.
- Output readiness directory.
- Sentinel and damage frame roles.
- Damage region and undamaged sample plan.
- Freshness window.

## Required Proof Set

An accepted proof set requires:

- Exactly three selected attempts.
- Every selected attempt has classification `accepted`.
- Every selected attempt uses the same host profile.
- Every selected attempt uses the same proof method.
- Every selected attempt is fresh for the current readiness run.
- Every selected attempt has accepted artifact quality.
- Every selected attempt proves both damaged-pixel update and undamaged-pixel preservation.

Extra attempts may be linked as context but cannot hide failed, stale, mismatched, or incomplete
evidence. The proof set must name the exact selected attempt ids.

## Verdict Rules

- `accepted`: exactly three fresh matching capable-host accepted attempts satisfy all acceptance
  rules.
- `fallback-gated`: attempts are missing, fewer than three, stale, host-mismatched,
  proof-method-mismatched, synthetic-only, incomplete, or otherwise non-accepting without proving a
  live host defect.
- `failed`: live evidence completed but the host cleared, corrupted, failed to update pixels,
  failed to preserve undamaged pixels, or produced invalid artifacts.
- `environment-limited`: display, GL context, readback, permission, timeout, renderer, or host
  setup prevents honest proof attempts.

Only `accepted` can support the same-profile parity gate. It does not by itself accept partial
redraw or a performance claim.

## Required Output

The proof-set package records:

- `live-proof/attempts/`: selected attempt summaries and artifacts.
- `live-proof/unsupported/`: unsupported-host evidence and zero-accepted-artifact statement.
- `proof-set.md`: proof-set id, status token, selected attempt ids, host profile id, proof method,
  freshness window, acceptance or blocking reasons, and attempt links.

Machine-readable records may be added beside Markdown summaries when the implementation chooses a
stable format.

## Acceptance Tests

- Exactly three fresh matching capable-host accepted attempts produce an accepted proof set.
- One or two accepted attempts remain fallback-gated.
- Four attempts require an explicit selected trio and visible treatment of excluded context.
- Host mismatch, proof-method mismatch, stale artifacts, blank artifacts, missing artifacts,
  synthetic-only artifacts, damaged-pixel failures, and undamaged-preservation failures fail
  closed.
- Unsupported hosts classify as `environment-limited`, complete under 2 minutes, and record zero
  accepted partial-redraw artifacts.

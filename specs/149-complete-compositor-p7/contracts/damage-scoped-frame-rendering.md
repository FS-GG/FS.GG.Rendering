# Contract: Damage-Scoped Frame Rendering

## Scope

This contract defines the observable behavior of rendering only damaged frame regions after a
matching live proof is accepted. Full redraw remains both the oracle and the safe fallback.

## Preconditions

Damage-scoped redraw may run only when:

- The active host profile has a fresh accepted live proof.
- The compositor mode is enabled.
- The frame has valid damage or a zero-damage preserve decision after a valid prior frame.
- The frame has no full-frame invalidation.
- Host scissor/no-clear support is available for the current presenter.
- Validation scenarios can compare the produced frame to a full-redraw oracle.

If any precondition fails, the frame uses full redraw or a lower safe tier and records a fallback
reason.

## Damage Rules

- Damage originates from retained-render decisions, movement regions, layout invalidation, and
  render-affecting input changes.
- Damage rectangles are clipped to the frame and deduplicated.
- Overlaps count once in union area.
- Placement-only movement includes both old covered regions and new covered regions.
- Resize, scale, theme/global visual change, resource/provider change, host-profile change, failed
  proof, and unsafe preservation are full-frame invalidations.
- Empty damage is valid only for idle/no-op frames after a valid prior frame; otherwise full redraw
  is selected.

## Host Scissor Rules

The host implementation must:

- Apply scissor rectangles that cover the damage union.
- Avoid full-frame clear during damage-scoped frames.
- Preserve untouched regions only after accepted proof.
- Reset scissor and clear behavior before full redraw, readback, or any non-scoped frame.
- Prevent frame-local scoping state from leaking into the next frame.
- Record final scissor rectangles, scissor area, no-clear mode, and reset diagnostics.

## Fallback Rules

Full redraw or lower-tier fallback is required for:

- Missing, stale, failed, environment-limited, host-mismatched, or synthetic-only proof.
- Invalid damage bounds or unsafe zero-damage preservation.
- Full-frame invalidation.
- Unsupported host capability.
- Disabled compositor mode.
- Snapshot/resource failure.
- Internal compositor error.
- Oracle parity failure.

Every fallback records a user-readable reason and must match the full-redraw visible output.

## Frame Evidence

Each validation frame records:

- Host profile id and proof id.
- Scenario id and frame id.
- Damage rectangles, union area, and full-frame invalidation flag.
- Applied scissor rectangles and no-clear state.
- Fallback reason when not damage-scoped.
- Full-redraw oracle identity.
- Damage-scoped output identity.
- Parity verdict and diagnostics.

## Acceptance Tests

- Localized update repaints only the damage union and matches the oracle.
- Overlapping damage reports union area rather than summed overlap.
- Edge damage is clipped to the frame.
- Movement damages old and new covered regions.
- Resize, theme, provider, host-profile, or resource changes force full redraw or full-frame
  damage.
- Missing, stale, failed, host-mismatched, synthetic-only, or environment-limited proof falls back
  to full redraw.
- Zero-damage frames preserve the prior image only after accepted proof and a valid prior frame.
- Scissor/no-clear state does not leak into later full redraw or readback paths.

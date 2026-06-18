# Contract: Damage-Scoped Redraw Integration

## Scope

This contract defines the observable behavior of repainting only damaged frame regions on a live
host after a matching preservation proof passes. Full-frame redraw remains both the oracle and the
safe fallback.

## Preconditions

Damage-scoped redraw may run only when:

- The active host profile has a fresh accepted live preservation proof.
- The compositor mode is enabled.
- The frame has a non-empty damage plan and no full-frame invalidation.
- The full-frame oracle is available for validation scenarios.
- Host scissor/no-clear support is available for the current presenter.

If any precondition fails, the frame uses full-frame redraw or a lower safe tier and records a
fallback reason.

## Damage Rules

- Damage originates from retained-render decisions, movement regions, layout invalidation, and
  render-affecting input changes.
- Damage rectangles are clipped to the frame and deduplicated.
- Overlaps count once in union area.
- Placement-only movement includes both old and new covered regions.
- Resize, scale, theme/global visual change, resource/provider change, host-profile change, failed
  proof, and unsafe preservation are full-frame invalidations.
- Empty damage is valid only for idle/no-op frames.

## Host Scissor Rules

The host implementation must:

- Apply scissor rectangles that cover the damage union.
- Avoid full-frame clear during damage-scoped frames.
- Preserve untouched regions only after accepted proof.
- Reset scissor and clear behavior before full redraw, readback, or any non-scoped frame.
- Prevent frame-local scoping state from leaking into the next frame.
- Record final scissor rectangles, scissor area, no-clear mode, and reset diagnostics.

## Frame Evidence

Each validation frame records:

- Host profile id and proof id.
- Scenario id and frame id.
- Damage rectangles, union area, and full-frame invalidation flag.
- Applied scissor rectangles and no-clear state.
- Fallback reason when not damage-scoped.
- Full-frame oracle identity.
- Damage-scoped output identity.
- Parity verdict and diagnostics.

## Acceptance Rules

- Every accepted damage-scoped frame must match the full-frame oracle.
- Disabled, unsupported, rejected, or unsafe damage-scoped redraw must produce the same visible
  output through full redraw or a lower safe tier.
- A parity failure marks the damage tier rejected until fixed.
- Performance evidence cannot override missing or failed proof.
- Environment-limited evidence is recorded but cannot count as ready.

## Acceptance Tests

- Localized update repaints only the damage union and matches the oracle.
- Overlapping damage reports union area rather than summed overlap.
- Edge damage is clipped to the frame.
- Resize/theme/provider changes force full-frame redraw or full-frame damage.
- Missing, stale, failed, host-mismatched, or environment-limited proof falls back to full redraw.
- Scissor/no-clear state does not leak into later full redraw or readback paths.

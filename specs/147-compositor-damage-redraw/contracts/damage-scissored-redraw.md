# Contract: Damage-Scissored Redraw

## Scope

This contract defines the observable behavior of repainting only damaged frame regions after a
present-path proof passes. The full-redraw oracle remains the acceptance baseline.

## Preconditions

Damage-scissored redraw may run only when:

- The current host profile has a passed, fresh present-path proof.
- The frame does not require full-frame invalidation.
- Damage rectangles are known, clipped to the frame, and unioned.
- The full-redraw oracle is available for the validation corpus.

If any precondition is missing, the frame falls back to full redraw and records a fallback reason.

## Damage Rules

- Damage comes from retained-render repaint decisions and layout/paint invalidation records.
- Overlapping damage regions are counted once in union area.
- Old and new placement regions are damaged for placement-only movement.
- Resize, scale, theme, provider/resource, unsupported host, and unsafe preservation changes are
  full-frame invalidations.
- Empty damage is valid only for idle or true no-op frames.

## Host Scissor Rules

The host scissor implementation must:

- Apply scissor rectangles that cover the damage union.
- Never assume pixels outside the frame are valid.
- Avoid clearing untouched regions on scissored frames.
- Reset scissor state before full redraw, readback, or any non-scissored path.
- Record the final scissor rects and area in diagnostics.

## Frame Evidence

Each scissored frame evidence record contains:

- Host profile id and proof id.
- Scenario id and frame id.
- Damage rectangles, union area, and full-frame invalidation flag.
- Applied scissor rectangles.
- Fallback reason when not scissored.
- Full-redraw oracle identity.
- Scissored output identity.
- Parity verdict and diagnostics.

## Acceptance Rules

- Every accepted scissored frame matches the full-redraw oracle.
- Disabled or unsupported scissoring must produce the same visible output through full redraw.
- A parity failure marks damage-scissored redraw not ready.
- A missing or failed proof cannot be overridden by performance evidence.
- Environment-limited evidence is recorded but cannot count as readiness.

## Acceptance Tests

- Localized update repaints only the damage union and matches the oracle.
- Overlapping rectangles report union area, not summed overlapping area.
- Resize/theme/provider changes force full-frame invalidation.
- Missing, stale, failed, or host-mismatched proof falls back to full redraw.
- Scissor state does not leak into subsequent full redraw or readback frames.

# ScrollViewer Extent Contract

## Purpose

ScrollViewer proves the intrinsic layout protocol by deriving scrollable content extent from
official layout/intrinsic results rather than inspecting rendered descendant bounds.

## Viewport Contract

- The ScrollViewer viewport is the laid-out bounds of the ScrollViewer control.
- Viewport width and height remain fixed by parent constraints and authored layout inputs.
- Clipping behavior remains the existing container-clip behavior unless a compatibility ledger entry
  documents an intentional change.

## Content Extent Contract

- Content extent is computed from the child participant's intrinsic/natural size under the relevant
  viewport cross-axis constraints.
- Empty or smaller-than-viewport content reports extent equal to the viewport and zero unnecessary
  overflow.
- Exact-fit content reports extent equal to the viewport and zero unnecessary overflow.
- Overflowing content reports the full natural content extent while keeping the viewport fixed.
- Nested scrollable content records which participant supplied the extent and does not require a
  descendant-bounds walk.

## Scroll Range Contract

- `MaxOffsetX = max 0 (ContentWidth - ViewportWidth)`.
- `MaxOffsetY = max 0 (ContentHeight - ViewportHeight)`.
- Current offsets are clamped to the accepted range.
- Changed intrinsic content updates the range on reevaluation.
- Unrelated surrounding layout remains equivalent to full layout after range changes.

## Diagnostics Contract

ScrollViewer emits diagnostics when:

- content lacks required intrinsic capability;
- intrinsic result is rejected or unavailable;
- fallback measured extent is used;
- content extent is contradictory or non-finite;
- cache dependency evidence is insufficient.

## Acceptance

The contract is accepted when validation covers at least 10 representative cases, including empty,
smaller-than-viewport, exact-fit, barely overflowing, substantially overflowing, nested scroll,
clipped/layered parent, text/content-driven natural size, dynamic content change, and invalid
intrinsic fallback.

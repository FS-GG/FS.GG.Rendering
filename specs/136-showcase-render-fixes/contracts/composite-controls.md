# Contract — Composite-Control Structure (`FS.GG.UI.Controls`)

Covers FR-006, FR-007, FR-008. All rules theme-invariant (antLight ≡ antDark structure).

## data-grid (FR-006)

- `data-grid-row` and `data-grid-header` lay out as `Row` (not `Column`) in `directionOf` (`Control.fs:1901`).
- Columns render side-by-side; header and body share a column-width track so cells align vertically.
- **Oracle**: a grid with named columns + rows renders as a table; header cell N is horizontally aligned with
  body cell N.

## menu / context-menu / combo rows (FR-005 structural part)

- `rowsGeom`: `rowH = max(minRowHeight, box.Height / n)`; overflow grows/scrolls/clips, never compresses items
  onto a shared baseline.
- **Oracle**: each item occupies a distinct y-band; none overprint.

## descriptions (FR-007)

- Item spacing scaled or truncated-with-affordance to fit `box.Height`; never paints past the box into the
  next control.
- **Oracle**: label/value pairs aligned and readable; bottom item within `box.Y + box.Height`.

## qr-code (FR-007)

- Enforce a minimum module-grid size; clip to box; a non-empty payload yields a populated module grid.
- **Oracle**: QR renders a visible non-empty grid (not blank) even when its box is small.

## charts / graphs (FR-008)

- All chart geometry wrapped in `Scene.clipped (RectClip box)`; degenerate data guarded (`n=0` → no
  divide-by-zero; NaN/Inf coordinates rejected).
- **Oracle**: chart body stays within its box; does not overrun the adjacent control's title; empty data
  renders an empty (not crashing/overrunning) chart.

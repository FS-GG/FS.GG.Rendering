# Contract — Overlay / Transient-Surface Render Pass (`FS.GG.UI.Controls`)

Covers FR-005. Builds on the existing `Overlay` container (`Control.fsi:506`).

## Render order

- `Control.renderTree` paints in two ordered groups: **in-flow** scene first, then the **overlay** scene
  (controls marked overlay/transient), so overlays paint last (z-top).
- Final scene = `inFlow @ overlay`; overlay nodes draw at their true coordinates, above siblings.

## Transient surfaces routed through the overlay layer

`menu`/`context-menu` open lists, `combo-box`, `auto-complete`, and `date-picker`/`time-picker` calendars,
when shown, emit their surface into the overlay layer rather than in-flow.

## Item layout within a surface

- Items lay out with `rowH = max(minRowHeight, box.Height/n)`; no two items share a baseline (no overprint).

## Hit-testing

- `nearestAuthored` consults the overlay group before in-flow, so the topmost overlay wins pointer hits.

## Invariants

- An open transient surface's drawn area is never overprinted by an in-flow sibling.
- A page with no open transient surface renders identically to a pure in-flow pass (overlay group empty).

## Surface impact (Tier 1)

- New/clarified public entry on `Control` for the overlay layer is declared in `Control.fsi`; the surface-area
  baseline for `Control` is updated and disclosed in the rebaseline ledger.

## Test oracle

- An open combo/auto-complete/date-picker dropdown's items are distinct and paint above the controls beneath.
- Menu/context-menu items never overprint each other.

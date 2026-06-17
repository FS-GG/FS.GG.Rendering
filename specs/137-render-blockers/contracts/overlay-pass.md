# Contract — Overlay / Transient-Surface Render Pass (`FS.GG.UI.Controls`)

Covers FR-004, FR-005, FR-006, FR-007. Builds on the existing `Overlay` container and extends feature 136 R4.

## Render order

- `Control.renderTree` paints **in-flow** first, then the **overlay** group last (z-top); final scene =
  `inFlow @ overlay`. Overlay nodes draw at true coordinates and are NOT wrapped by ancestor container clips.
- The retained emit (`assemble`/`SubtreeScene`) reproduces the identical split → full ≡ retained.

## Transient surfaces routed through the overlay layer

- `menu`/`context-menu` open lists, `combo-box`, `auto-complete`, `date-picker`/`time-picker` calendars, when
  shown, emit their surface into the overlay group.

## Item layout

- Items within a surface occupy distinct y-bands (no shared baseline / no overprint).

## Hit-testing

- `nearestAuthored`/`hitTest` consult the overlay group before in-flow; the topmost overlay wins.

## Invariants

- An open transient surface's drawn area is never overprinted by an in-flow sibling.
- An overlay nested inside a clipped container is NOT clipped by that container.
- A page with no open transient surface renders byte-identically to a pure in-flow pass (empty overlay group).

## Surface impact (Tier 1)

- New/clarified public overlay-pass entry on `Control` is declared in `Control.fsi`; the `Controls`
  surface-area baseline is updated and disclosed in the rebaseline ledger.

## Test oracle

- An open dropdown over an in-flow sibling paints above it; items distinct; hit at the overlap returns the
  overlay; empty-overlay page is byte-identical to the pre-overlay render; full ≡ retained holds.

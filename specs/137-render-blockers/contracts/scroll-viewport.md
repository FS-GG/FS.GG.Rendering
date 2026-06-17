# Contract — ScrollViewer Viewport (`FS.GG.UI.Controls` / `FS.GG.UI.Layout`)

Covers FR-008. Builds on the container-clip model (container-clipping.md).

## Viewport contract

- A `ScrollViewer` clips its content to its box, exposes a scroll offset, and renders a scroll affordance.
- Content taller than the viewport is clipped (scrollable), never spilled outside the box.
- Any viewport metric read back by consumers is surfaced through the module `.fsi` (Tier 1 surface update if so).

## Invariants

- Nothing inside a `ScrollViewer` paints outside its box.
- A page taller than its content region paints nothing outside the region; status/feedback render within the window.

## Test oracle

- Content taller than the box → content clipped to box + affordance present; a control beyond the fold is
  clipped (scrollable) not spilled; bounded-page test (nothing outside the content region).

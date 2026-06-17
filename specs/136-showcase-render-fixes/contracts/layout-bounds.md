# Contract — Layout Bounds, Clipping & Scroll (`FS.GG.UI.Layout` / `Controls` + sample `Shell`)

Covers FR-003, FR-004, FR-009, FR-010.

## Region non-overlap (FR-003)

- The flex main-axis split honours explicit basis/weight per child instead of dividing space uniformly
  (`Layout.fs:~272`).
- The showcase `Shell.fs` declares explicit sizes: app bar (top, fixed height), nav rail (left, fixed width),
  content (flex-grow, scrolling), feedback (fixed height), status (bottom, fixed height).
- **Oracle**: app bar / nav / content / feedback / status drawn rects are mutually disjoint on every page.

## Container clipping (FR-004 / FR-009)

- `paintNode` clips each container's children to the container bounds (`Scene.clipped`).
- **Oracle**: no child paints past its parent's right/bottom edge; nav-rail labels stay within the rail width;
  no sibling control overlaps an adjacent control or section label.

## ScrollViewer viewport (FR-009 / FR-010)

- `ScrollViewer` clips content to its box and exposes a scroll offset + affordance; content taller than the
  viewport is clipped (scrollable), not spilled past the window.
- **Oracle**: a page taller than the content region shows a scroll affordance and paints nothing outside the
  content region; the status strip and feedback text render fully within the window.

## Layer split (FR-011)

- Framework: flex distribution, container clipping, ScrollViewer viewport.
- Sample: the specific chrome-region sizes, nav-rail width, content scroll wiring in `Shell.fs`.

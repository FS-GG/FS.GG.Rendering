# Contract: Scroll Interaction

Governs the content-region `scroll-viewer` control. Tier 1 (shared control behavior).

## Surface touched

- `src/Controls/Widgets/Containers.fsi` — `ScrollViewerProps`/`ScrollViewer` (offset state seam;
  `OnChanged` retained as optional report).
- `src/Controls/Control.fsi` / `Control.fs` — `scrollAffordance`, `scrollViewerGeom`, content
  translate/clip by offset.
- `src/Controls/Pointer.fsi` — `Scroll` interaction already present (`Pointer.fs:242`); thumb-drag
  and scroll-key paths added/confirmed.
- Layout hit-test seam — offset-aware hit-testing for scroll descendants.

Each `.fsi` delta updates the matching surface baseline in the same change.

## Behavior

1. **Drag** (FR-001): dragging the thumb scrolls the content; thumb position reflects the new
   offset. `Offset` clamps to `[0, max(0, ContentHeight - ViewportHeight)]`.
2. **Wheel** (FR-001): wheel over the region scrolls by a consistent increment and stops cleanly
   at top and bottom (no overscroll). Delivered via `Scroll(...)` from the viewer's
   `PointerScrolled`/`Wheel` path.
3. **Keyboard** (FR-001): the supported scroll keys scroll the content when focus is in the region —
   `ArrowUp`/`ArrowDown` (line step), `PageUp`/`PageDown` (viewport-height step), `Home`/`End` (jump
   to top/bottom), and `Space`/`Shift+Space` (page down/up). Each reduces to `applyScrollDelta` and
   clamps at the bounds.
4. **No-overflow** (FR-002): when content fits, no draggable thumb is presented; a one-pixel
   overflow is treated as non-scrollable to avoid flicker.
5. **Offset-aware hit-testing** (FR-009): hover/focus/activation inside the region map to the
   control under the pointer after the offset is subtracted.

All three input kinds reduce to one `applyScrollDelta` transition. Repaint stays damage-local —
scrolling MUST NOT reintroduce full-tree frame preparation.

## Acceptance evidence

- Failing-first Expecto tests: offset clamp at both bounds, thumb height from viewport/content
  ratio, thumb position from offset, no-thumb when `ContentHeight <= ViewportHeight`, and correct
  control resolved after a scroll (offset-aware hit-test).
- Live desktop run: a page taller than the region scrolls by drag, wheel, and keyboard; previously
  hidden bottom content becomes reachable; thumb tracks offset (SC-001).
- Headless lane: `environment-limited` with disclosed substitute where no real presentation exists.

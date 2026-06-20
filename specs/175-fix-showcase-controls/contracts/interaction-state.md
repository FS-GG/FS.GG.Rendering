# Contract: Interaction State (Hover / Focus / Active / Overlays)

Governs live hover, focus, active, and combined states for interactive controls, and overlay
open/dismiss/focus-return. Tier 1 (shared control behavior).

## Surface touched

- `src/Controls.Elmish/ControlsElmish.fs(i)` — `HoverChanged`/`FocusChanged` routing
  (`:210-215`), retained repaint trigger on hover/focus, `interpretOverlayEffect` (`:227`).
- `src/Controls/ControlRuntime.fs(i)` — `applyRuntimeVisualState` coverage for every interactive
  kind used by the showcase, including the `ghost` nav buttons.
- `src/Controls/Control.fs` — per-kind `*Geom` paint of `Hovered`/`Focused`/combined via Ant
  palette roles.

Each `.fsi` delta updates the matching surface baseline in the same change.

## Behavior

1. **Hover** (FR-003): pointer-over shows the Ant hover state; pointer-leave clears it.
2. **Focus** (FR-004): keyboard focus shows a focus affordance distinct from hover; the affordance
   moves with focus.
3. **Combined** (FR-005): hover+focus shows the combined Ant state with neither affordance
   suppressing the other; focus persists when the pointer leaves while focus remains.
4. **Display-only** (FR-008): display-only controls never present an interactive hover/focus
   affordance and stay static under input.
5. **Overlays** (FR-013): drawer/popover/popconfirm/tooltip/dialog/tour/context-menu open and
   dismiss under real input; on close, focus returns to the control that opened the overlay (the
   trigger), or — if that control no longer exists or is unfocusable — to the nearest focusable
   ancestor still present.
6. **Appearance** (FR-011): all affordances use resolved Ant palette roles, correct in antLight and
   antDark, with no new spacing/alignment/clipping/contrast regression.

Hover/focus repaints are model-unchanged and damage-local; they MUST NOT rebuild the view tree per
pointer move.

## Acceptance evidence

- Failing-first tests: `applyRuntimeVisualState` stamps the correct `VisualState` per kind
  (including `ghost`); combined hover+focus keeps both; display-only kinds stay `Normal`; overlay
  open/dismiss emits focus-return.
- Live desktop run: hover and focus appear/clear across every interactive control on every page,
  visible within the live-responsiveness target (SC-002).
- Visual review set in both appearances confirms correct palette roles (SC-006).

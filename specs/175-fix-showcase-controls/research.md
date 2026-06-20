# Phase 0 Research: Fix Non-Functional Controls in the Second Ant Showcase

This phase resolves the planning unknowns and fixes the tier classification. Each item is
recorded as Decision / Rationale / Alternatives considered. Code references are to the current
tree and were confirmed during planning.

## R1 — Tier classification (Tier 1 vs Tier 2)

- **Decision**: The feature is **Tier 1**. The spec's downgrade-to-Tier-2 condition ("confined to
  sample wiring with no control-surface change") does not hold.
- **Rationale**: The three reported defects resolve in shared `FS.GG.UI.*` behavior, not sample
  wiring: scroll-offset ownership and thumb tracking live in the `scroll-viewer` control
  (`src/Controls/Widgets/Containers.fs:144`, painted by `Control.scrollAffordance`
  `src/Controls/Control.fs:1501` and `scrollViewerGeom:1489`); hover/focus repaint and visual-state
  stamping live in `ControlsElmish.fs` (`HoverChanged`/`FocusChanged` at `:210-215`) and
  `ControlRuntime.applyRuntimeVisualState`; offset-aware hit-testing lives in the layout hit-test
  seam used by `routeInteractivePointer`. Changing these alters observable behavior of shared
  controls, which is Tier 1 by the constitution's Change Classification.
- **Alternatives considered**: Treating it as sample-local Tier 2 — rejected because the content
  region uses the shared `scroll-viewer` control and the missing scroll-offset/repaint behavior
  would have to be re-implemented in the sample, forking control behavior (forbidden: one semantic
  control set). Per-fix tier is still recorded: genuinely sample-local fixes (e.g. binding an
  unbound `OnChanged`) are marked Tier 2 in the finding log.

## R2 — Scroll offset ownership and update path (FR-001, FR-002)

- **Decision**: Own the content region's scroll offset as control-runtime state keyed by the
  `scroll-viewer` `ControlId`. The already-emitted `Scroll(control, dx, dy, x, y)` interaction
  (`src/Controls/Pointer.fs:242`, delivered from the viewer's `PointerScrolled`/`Wheel` path at
  `src/SkiaViewer/SkiaViewer.fs:2325` and re-encoded in `ControlsElmish.fs:1612`), thumb-drag, and
  standard scroll keys all reduce to a single "apply scroll delta" transition that updates the
  offset, clamped to `[0, max(0, contentHeight - viewportHeight)]`. The content is translated by
  `-offset` and clipped to the viewport; `scrollAffordance` derives thumb height from the
  viewport/content ratio and thumb position from `offset / (contentHeight - viewportHeight)`.
- **Rationale**: The pipeline already produces `Scroll` interactions and the affordance already
  computes thumb height from a ratio — the missing piece is offset state and consuming the delta
  into content translation + thumb position. Keeping the offset in control-runtime state (not the
  product `Model`) preserves the pure-Core/host split and lets every showcase/product inherit it.
- **Alternatives considered**: (a) Pushing offset into the product `Model` via `OnChanged` — keeps
  Core pure but forces every consumer to wire scroll plumbing and re-render on every wheel tick,
  which conflicts with damage-local repaint; the `OnChanged` seam is retained as an optional report,
  not the mechanism. (b) A viewer-only scroll transform with no thumb tracking — rejected; it fails
  FR-001's "thumb position reflects offset".

## R3 — Clamp and no-overflow affordance (FR-002, Edge cases)

- **Decision**: Clamp scrolling at both bounds with no overscroll; when `contentHeight <=
  viewportHeight`, present no draggable thumb (no active scroll affordance), matching the existing
  `ratio = 1.0` branch in `scrollAffordance`. Treat a one-pixel overflow as non-draggable to avoid
  flicker (a small dead-zone threshold).
- **Rationale**: Directly satisfies acceptance scenario 4 and the "exactly fits / one-pixel
  overflow" edge case without a flickering thumb.
- **Alternatives considered**: Always showing the thumb — rejected as misleading per FR-002.

## R4 — Offset-aware hit-testing inside the scroll region (FR-009)

- **Decision**: Subtract the region's current scroll offset before hit-testing controls inside the
  scrollable content, so hover/focus/activation map to the control actually under the pointer after
  scrolling. Apply this at the hit-test seam used by `routeInteractivePointer`
  (`Layout.hitTestComputed`), scoped to descendants of a `scroll-viewer` by its offset.
- **Rationale**: Without this, post-scroll pointer-to-control mapping is wrong (FR-009 and the
  "hit-testing after scroll" edge case). Doing it at the hit-test seam keeps it shared and correct
  for nested content.
- **Alternatives considered**: Re-laying-out the subtree at the offset each frame — rejected as more
  expensive and redundant with content translation.

## R5 — Hover/focus live-feedback break (FR-003, FR-004, FR-005)

- **Decision**: Phase 1/implementation confirms the precise break, but the infrastructure to use is
  fixed: `HoverChanged`/`FocusChanged` → `DispatchControlRuntimeMessage(HoverControl/FocusControl)`
  (`ControlsElmish.fs:210-215`) → `ControlRuntime.applyRuntimeVisualState` stamps `VisualState`
  (`Hovered`/`Focused`/combined) → each `*Geom` in `Control.fs` paints the state via a model-unchanged
  damage-local repaint. The likely gaps to verify and close: (a) the hover repaint is actually
  triggered on `HoverEnter/HoverLeave` (not only on model change); (b) `applyRuntimeVisualState`
  covers every interactive kind used by the showcase, including the `ghost` nav buttons
  (`Shell.fs:65`); (c) combined hover+focus does not let one state suppress the other.
- **Rationale**: The pieces exist but the live experience is dead, so the defect is in the wiring
  between host hover/focus messages and the retained repaint, and/or per-kind coverage — not a
  missing subsystem. Confirming the exact link avoids rebuilding working infrastructure.
- **Alternatives considered**: Dispatching hover/focus into the product `Model` — rejected; it would
  rebuild the view tree per pointer move and defeat damage-local repaint.

## R6 — Per-control root-cause map and overlays (FR-006, FR-007, FR-013)

- **Decision**: Produce a per-control root-cause map across the 13 interaction families
  (`InteractionContracts.fs`), recording for each failing control the cause and the fix tier.
  Overlay-bearing controls (drawer, popover, popconfirm, tooltip, dialog, tour, context menu) must
  open and dismiss under real input with focus returning to a sensible location, driven through the
  existing overlay-effect interpreter (`interpretOverlayEffect`, `ControlsElmish.fs:227`).
- **Rationale**: FR-006/FR-007 require live behavior to match scripted coverage for every contract;
  the map is the instrument that guarantees none is missed and that each fix is tier-correct.
- **Alternatives considered**: Fixing only the three named symptoms — rejected; the spec requires a
  systematic pass over every control with zero unresolved defects (SC-005, SC-007).

## R7 — Display-only confirmation (FR-008)

- **Decision**: Confirm the ~30 display-only controls (recorded in `InteractionContracts.fs:60-111`,
  e.g. `scroll-viewer`'s container chrome aside, layout/display/feedback/chart/graph kinds) remain
  static and present no interactive hover/focus affordance, consistent with their recorded reasons.
- **Rationale**: FR-008/SC-004 require display-only controls to stay clearly non-interactive even as
  hover/focus is added to interactive kinds.
- **Alternatives considered**: None — this is a confirmation step, not a design choice.

## R8 — Appearance fidelity (FR-011)

- **Decision**: Hover/focus/active/scroll affordances use the resolved Ant palette roles
  (`theme.Accent`, `theme.Muted`, focus border role) and are reviewed in both antLight and antDark
  via the existing visual-readiness path, with no new spacing/alignment/clipping/contrast
  regression.
- **Rationale**: FR-011/SC-006 make appearance fidelity a release bar; the single semantic control
  set resolves per appearance, so no per-theme fork is introduced.
- **Alternatives considered**: Hardcoded colors — rejected; violates the palette-role policy.

## R9 — Evidence path and headless substitute (Principle V, FR-010, FR-012)

- **Decision**: Reuse the existing live-responsiveness runner and visual-readiness paths in
  `SecondAntShowcase.App` for accepted evidence. Deterministic Expecto tests cover scroll-offset
  transitions, clamp, thumb tracking, offset-aware hit-testing, hover/focus stamping, and
  live-vs-scripted parity. Where a real interactive path is unavailable in the headless lane, report
  `environment-limited` and disclose any synthetic substitute at the use site (`// SYNTHETIC: ...`),
  in the test name (`Synthetic` token), and in the PR.
- **Rationale**: Matches the constitution's evidence rules and the spec's assumption that existing
  evidence paths are reused; keeps "passes scripted coverage but dead under real input" from
  recurring by tying parity to a real-input verification path.
- **Alternatives considered**: A new bespoke live runner — rejected as unjustified duplication.

## Resolved unknowns summary

All Technical Context items are resolved; no `NEEDS CLARIFICATION` remains. The feature is Tier 1;
the shared-surface scope is the `scroll-viewer` offset/thumb/affordance, the hover/scroll pointer
state machine, retained pointer/focus routing, offset-aware hit-testing, and paint-time visual
state. Sample-local fixes are recorded per-finding as Tier 2.

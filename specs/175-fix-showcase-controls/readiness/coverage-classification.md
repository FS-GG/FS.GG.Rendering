# Coverage Classification — 175-fix-showcase-controls (T004)

Every catalog control resolves to **exactly one** classification — `Interactive` (has an
InteractionContract) or `DisplayOnly` (has a recorded reason) — with zero unclassified
(FR-012, SC-007). Source of truth: `SecondAntShowcase.Core.InteractionContracts.all` (contracts)
and `InteractionContracts.displayOnlyReasons`, validated against the live catalog by
`InteractionContracts.coverage` and `CoverageMap.check`.

## Catalog totals (live, from `SecondAntShowcase coverage`)

`96/96 controls mapped, 19 pages (13 catalog + 6 template), 0 unreferenced, 0 duplicated`

| Classification | Count |
|----------------|------:|
| Interactive (contracted) | 45 |
| DisplayOnly (recorded reason) | 51 |
| **Total** | **96** |
| Unclassified | 0 |

> Note: plan.md estimated "~96 interactive / ~30 display-only"; those were approximate. The
> verified split is **45 interactive / 51 display-only** (96 total = the full catalog). This doc
> records the real figures.

## Interactive controls (45) by contract family (13 families + graph/custom)

| ContractId | Controls | Input | Primary action → visible evidence |
|------------|----------|-------|-----------------------------------|
| button-click | button, icon-button, split-button, float-button | pointer-discrete | activate → command counter changes |
| toggle-switch | toggle-button, switch | pointer-discrete | toggle → checked visual changes |
| text-entry | text-box, text-area, auto-complete | key-down | type → entered text renders |
| numeric-entry | numeric-input | key-down | set value → numeric display changes |
| date-time | date-picker, time-picker | pointer-discrete | choose → selected value visible |
| slider-rating | slider, rate, progress-bar | pointer-move | adjust → position/stars change |
| selection-single | radio-group, combo-box, list-box, segmented, cascader, color-picker | pointer-discrete | select → selected label/swatch changes |
| selection-multi | check-box, multi-select-list, tree-view | pointer-discrete | select → checked rows change |
| navigation | tabs, menu, breadcrumb, steps, pagination, anchor, affix | pointer-discrete | navigate → active item changes |
| disclosure | collapse, drawer, popover, popconfirm, tooltip, dialog, overlay, tour | pointer-discrete | open/close → surface visibility changes |
| upload | upload | pointer-discrete | select artifact → file name changes |
| data-collection | list-view, data-grid | pointer-discrete | select/page → row/page state changes |
| form-validation | validation-message | pointer-discrete | submit invalid → message appears |
| graph-custom | graph-view, custom-control | pointer-discrete | script action → status changes |

## DisplayOnly controls (51) — recorded reasons

Static typography/media/identity: text-block, rich-text, label, icon, separator, badge, tag,
avatar, image, card, descriptions, statistic, qr-code, watermark, calendar, carousel, timeline.
Seeded charts (15): line-chart, bar-chart, pie-chart, scatter-plot, area-chart, column-chart,
histogram, box-plot, heatmap, radar-chart, rose-chart, waterfall-chart, funnel-chart, gauge-chart,
treemap. Seeded graph/diagram: sunburst, sankey-diagram, chord-diagram. Layout primitives (8):
stack, grid, dock, wrap, border, panel, scroll-viewer, split-view. Other display-only: toolbar
(composes button-contract commands), toast, spinner, empty, skeleton, alert, result, context-menu.

### Classification tensions to resolve in US3 (recorded, not yet changed)

- **context-menu** — **RESOLVED (F-006)**: kept `DisplayOnly` in the sample. FR-013's overlay
  open/dismiss + focus-return is a **framework** contract owned by `OverlayState` and verified by
  `Feature143*`/`Feature144*` (incl. `recoveryFocus` → trigger on close); the sample presents the
  context-menu as a menu *pattern* and adds no new overlay behaviour to verify at the sample level.
  Reason clarified to: "menu-pattern sample; context-menu overlay open/dismiss + focus-return is
  covered by the framework `OverlayState` tests, not a sample Core-state transition."
- **scroll-viewer** — `DisplayOnly` as a *Core state transition* ("host scrolling behavior, not a
  pure Core state transition"). This stays correct: US1 fixes the *host/shared-control* scroll
  behavior (Tier 1), not a Core `Model` transition, so the sample's classification is unchanged.
  The content region binding (`content-scroll`) is the sample-local wiring site.

## Display-only confirmation (FR-008, SC-004) — US3 T035

The 51 display-only controls must remain static with no interactive hover/focus affordance after
US2 adds hover/focus to interactive kinds. Confirmation result recorded here on US3 completion:
_pending US3 T035_.

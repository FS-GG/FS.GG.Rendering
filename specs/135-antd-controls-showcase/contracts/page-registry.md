# Contract: Page Registry (96 controls → family pages + 6 template pages)

`PageRegistry.all : Page list` = **13 Catalog (family) pages** covering all 96 controls (the coverage
bijection) **++ 6 Template (enterprise) pages** (exempt from the bijection, R2/R4). The id→page assignment
below is finalized against `src/Controls/catalog.yml` at implementation time; the `coverage` check
(contracts/cli.md) is the authoritative gate and fails on any drift.

## Catalog pages (the bijection — every id appears on exactly one)

| # | Page id | Title | Controls (catalog ids) |
|---|---|---|---|
| 1 | `display-typography` | Display & Typography | text-block, rich-text, label, icon, separator, badge, tag, avatar |
| 2 | `cards-stats-media` | Cards, Stats & Media | image, card, descriptions, statistic, qr-code, watermark, calendar, collapse, carousel, timeline |
| 3 | `buttons` | Buttons & Commands | button, icon-button, toggle-button, split-button |
| 4 | `text-numeric-input` | Text & Numeric Input | text-box, text-area, numeric-input, slider, date-picker, time-picker, rate, auto-complete, upload |
| 5 | `selection-toggles` | Selection & Toggles | check-box, radio-group, switch, list-box, multi-select-list, combo-box, color-picker, cascader |
| 6 | `layout-containers` | Layout & Containers | stack, grid, dock, wrap, border, panel, scroll-viewer, split-view |
| 7 | `navigation-menus` | Navigation & Menus | tabs, menu, context-menu, toolbar, float-button, breadcrumb, steps, pagination, segmented, anchor, affix |
| 8 | `overlays` | Overlays | tooltip, dialog, overlay |
| 9 | `feedback-status` | Feedback & Status | toast, progress-bar, spinner, validation-message, empty, skeleton, alert, result, drawer, popover, popconfirm, tour |
| 10 | `data-collections` | Data Collections | list-view, tree-view, data-grid |
| 11 | `charts-statistical` | Charts I — Statistical | line-chart, bar-chart, pie-chart, scatter-plot, area-chart, column-chart, histogram, box-plot |
| 12 | `charts-advanced` | Charts II — Advanced | heatmap, radar-chart, rose-chart, waterfall-chart, funnel-chart, gauge-chart, treemap, sunburst |
| 13 | `graphs-custom` | Graphs & Custom | graph-view, sankey-diagram, chord-diagram, custom-control |

**Total: 96 controls** (8+10+4+9+8+8+11+3+12+3+8+8+4). Bijection invariant: zero unreferenced, zero
duplicated against `Catalog.supportedControls`.

## Template pages (Kind = Template; `ControlIds = []`, exempt)

| # | Page id | Title | Recipe source | Composed catalog controls (asserted by TemplateTests) |
|---|---|---|---|---|
| T1 | `tpl-workbench` | Workbench | `templates/workbench.md` | toolbar, data-grid, panel, card, statistic |
| T2 | `tpl-list` | List Page | `templates/list.md` | toolbar, data-grid (or list-view), pagination, tag |
| T3 | `tpl-detail` | Detail Page | `templates/detail.md` | descriptions, card, panel, tabs, timeline |
| T4 | `tpl-form` | Form Page | `templates/form.md` + `patterns/input.md` | text-box, text-area, select(combo-box), switch, validation-message, button, result |
| T5 | `tpl-result` | Result Page | `templates/result.md` | result, button, statistic |
| T6 | `tpl-exception` | Exception (403/404/500) | `templates/exception.md` | result, button, icon |

Template pages are validated by **composition** (every node is a known `Catalog` id — SC-002), not by the
bijection. The exact control set per template follows each recipe's machine-checked `refs` block; the lists
above are the planning baseline.

## Rules

- Every `Catalog` page id and every `Template` page id is unique across `PageRegistry.all`.
- Nav rail lists all 19 pages (family pages first, then a "Templates" group). Any page reachable in ≤2
  actions (SC-008).
- Adding/removing a catalog control reddens the `coverage` check until a Catalog page is updated (drift
  honesty).

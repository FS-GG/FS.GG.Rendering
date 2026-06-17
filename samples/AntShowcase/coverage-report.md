# Ant Design Controls Showcase — Coverage Report

Committed control→page map (FR-003 / SC-001). The authoritative gate is the live
`coverage` subcommand (`dotnet run --project AntShowcase.App -- coverage`), which reads
`FS.GG.UI.Controls.Catalog.supportedControls` and reddens on any drift. As of the feed
refreshed for feature 132, that is **96/96 controls mapped, 19 pages (13 catalog + 6
template), 0 unreferenced, 0 duplicated**.

## Catalog pages (the bijection — every id appears on exactly one)

| # | Page id | Controls (catalog ids) |
|---|---------|------------------------|
| 1 | `display-typography` | text-block, rich-text, label, icon, separator, badge, tag, avatar |
| 2 | `cards-stats-media` | image, card, descriptions, statistic, qr-code, watermark, calendar, collapse, carousel, timeline |
| 3 | `buttons` | button, icon-button, toggle-button, split-button |
| 4 | `text-numeric-input` | text-box, text-area, numeric-input, slider, date-picker, time-picker, rate, auto-complete, upload |
| 5 | `selection-toggles` | check-box, radio-group, switch, list-box, multi-select-list, combo-box, color-picker, cascader |
| 6 | `layout-containers` | stack, grid, dock, wrap, border, panel, scroll-viewer, split-view |
| 7 | `navigation-menus` | tabs, menu, context-menu, toolbar, float-button, breadcrumb, steps, pagination, segmented, anchor, affix |
| 8 | `overlays` | tooltip, dialog, overlay |
| 9 | `feedback-status` | toast, progress-bar, spinner, validation-message, empty, skeleton, alert, result, drawer, popover, popconfirm, tour |
| 10 | `data-collections` | list-view, tree-view, data-grid |
| 11 | `charts-statistical` | line-chart, bar-chart, pie-chart, scatter-plot, area-chart, column-chart, histogram, box-plot |
| 12 | `charts-advanced` | heatmap, radar-chart, rose-chart, waterfall-chart, funnel-chart, gauge-chart, treemap, sunburst |
| 13 | `graphs-custom` | graph-view, sankey-diagram, chord-diagram, custom-control |

**Total: 96 controls** (8 + 10 + 4 + 9 + 8 + 8 + 11 + 3 + 12 + 3 + 8 + 8 + 4).

## Template pages (Kind = Template; `ControlIds = []`, exempt from the bijection)

Validated by **composition** (every node maps to a known catalog id — SC-002), not by the
bijection (R2/R4). See `AntShowcase.Tests/TemplateTests.fs`.

| # | Page id | Title | Composed catalog controls |
|---|---------|-------|---------------------------|
| T1 | `tpl-workbench` | Workbench | toolbar, data-grid, panel, card, statistic |
| T2 | `tpl-list` | List Page | toolbar, text-box, button, tag, data-grid, pagination |
| T3 | `tpl-detail` | Detail Page | descriptions, tabs, panel, card, timeline |
| T4 | `tpl-form` | Form Page | label, text-box, text-area, combo-box, switch, validation-message, button, result |
| T5 | `tpl-result` | Result Page | result, statistic, button |
| T6 | `tpl-exception` | Exception (403/404/500) | icon, result, button |

Adding or removing a catalog control reddens the `coverage` check until a catalog page is
updated (drift honesty). Template pages stay exempt by the `Kind` filter.

# Contract: Control Coverage

## Coverage Domain

The authoritative control domain is:

```fsharp
FS.GG.UI.Controls.Catalog.supportedControls
```

The current planning snapshot from the existing AntShowcase coverage report is 96 controls. The implementation must not depend on that count staying fixed.

## Page Assignment

Catalog controls are assigned only through `Kind = catalog` pages.

Planned catalog page groups:

| Page id | Coverage role |
|---|---|
| `display-typography` | display, text, labels, icons, status badges |
| `cards-stats-media` | card-like display, descriptions, media, calendar, timeline |
| `buttons` | button and action controls |
| `text-numeric-input` | text, numeric, date/time, sliders, rating, upload |
| `selection-toggles` | checkbox, radio, switch, lists, combo, color/cascader |
| `layout-containers` | stack, grid, dock, wrap, border, panel, scroll, split |
| `navigation-menus` | tabs, menu, context menu, toolbar, breadcrumb, steps, pagination, segmented, anchor, affix |
| `overlays` | tooltip, dialog, overlay |
| `feedback-status` | toast, progress, spinner, validation, empty, skeleton, alert, result, drawer, popover, popconfirm, tour |
| `data-collections` | list, tree, data grid |
| `charts-statistical` | statistical chart controls |
| `charts-advanced` | advanced chart controls |
| `graphs-custom` | graph and custom surfaces |

Template pages are compositions and must have `ControlIds = []` for coverage purposes.

## Coverage Result

Coverage output must include:

- catalog count
- assigned count
- catalog page count
- template page count
- missing control ids
- duplicated control ids
- unknown assigned control ids
- status
- human-readable summary line

Clean coverage requirements:

- `MissingControlIds = []`
- `DuplicatedControlIds = []`
- `UnknownControlIds = []`
- every assigned id appears in the live catalog
- every live catalog id appears exactly once across catalog pages

## Drift Behavior

When the catalog grows:

- new ids appear in `MissingControlIds`
- CLI exits non-zero
- tests fail until the new controls are assigned exactly once

When the catalog shrinks or an id changes:

- stale assignments appear in `UnknownControlIds`
- CLI exits non-zero
- tests fail until the page assignment is updated

When a control is assigned more than once:

- the id appears in `DuplicatedControlIds`
- CLI exits non-zero
- tests fail until one assignment is removed

## Template Composition

Template pages must be validated separately:

- every composed control id maps to a known catalog id
- template composition does not create coverage duplicates
- each template page records its composed controls for reviewer traceability

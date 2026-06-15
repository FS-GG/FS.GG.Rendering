# Contract: Page Registry & Coverage Map

The pure shapes `ControlsGallery.Core` exposes to the `App` edge and the test project.
(Sample-internal; not a packed public surface.)

## The 10 pages (the registry)

`Core.Pages.all : GalleryPage list` — exactly 10 entries, in nav order. The
control→page assignment is authoritative for the coverage check.

| Index | Page Id | Title / Family | Control ids |
|-------|---------|----------------|-------------|
| 1 | `display-typography` | Display & Typography | text-block, rich-text, label, image, icon, separator, badge |
| 2 | `buttons` | Buttons | button, icon-button, toggle-button, split-button |
| 3 | `text-numeric-input` | Text & Numeric Input | text-box, text-area, numeric-input, slider, date-picker, time-picker |
| 4 | `selection-toggles` | Selection & Toggles | check-box, radio-group, switch, list-box, multi-select-list, combo-box, color-picker |
| 5 | `data-collections` | Data & Collections | list-view, tree-view, data-grid |
| 6 | `layout-containers` | Layout & Containers | stack, grid, dock, wrap, border, panel, scroll-viewer, split-view |
| 7 | `navigation-menus` | Navigation & Menus | tabs, menu, context-menu, toolbar |
| 8 | `overlays-feedback` | Overlays & Feedback | tooltip, dialog, overlay, toast, progress-bar, spinner, validation-message |
| 9 | `charts` | Charts | line-chart, bar-chart, pie-chart, scatter-plot |
| 10 | `pointer-custom` | Pointer Playground / Custom | graph-view, custom-control |

**Totals**: 7 + 4 + 6 + 7 + 3 + 8 + 4 + 7 + 4 + 2 = **52 controls across 10 pages**,
each control on exactly one page.

## Coverage check

```
Core.CoverageMap.check : unit -> CoverageResult
// CoverageResult = { Unreferenced: string list; Duplicated: string list }
```

**Contract**:

- Domain = `Catalog.supportedControls |> List.map (fun d -> d.Id)` (52 ids), obtained
  from the **public** `FS.GG.UI.Controls.Catalog` package surface.
- `Unreferenced` = catalog ids appearing on **zero** pages.
- `Duplicated` = control ids appearing on **more than one** page.
- **Pass** ⇔ both lists empty ⇔ bijection between the 52 catalog ids and the union of
  page `ControlIds`.
- The check MUST fail if the catalog grows/shrinks and the registry is not updated
  (intentional drift detection, spec Edge Cases / FR-003).

## Reachability (SC-006)

Every control is reachable in ≤ 2 navigation actions: (1) select its page in the rail,
(2) scroll it into view. The registry guarantees a control belongs to exactly one page,
so the page selection that surfaces it is unique.

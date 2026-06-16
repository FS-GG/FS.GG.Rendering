# Phase 1 Data Model: Ant Design Charts adoption (D2C.1)

Entities are expressed against the **existing public types** (`Catalog.ControlDefinition`, the
`DesignSystem` `Theme`/`Color` types, the `Charts` data shapes `ChartSeries`/`ChartPoint`). New named
entities are the chart-coverage-matrix row and the net-new chart-control catalog entry. No new public
type is introduced for the palette (it is a pure helper over existing `Theme` roles).

## 1. Net-new chart control (catalog entry)

Each net-new chart control is a `Catalog` row + a `Charts2` `.fsi`/`.fs` module, identical in shape to
the existing chart controls.

| Attribute | Constraint |
|---|---|
| `id` | kebab-case, unique in `catalog.yml` (e.g. `area-chart`, `column-chart`, `histogram`, `box-plot`, `heatmap`, `radar-chart`, `rose-chart`, `waterfall-chart`, `funnel-chart`, `gauge-chart`, `sankey-diagram`, `chord-diagram`, `treemap`, `sunburst`) |
| `category` | `chart` (relational/hierarchical layouts that are graph-like may use `graph`) |
| `module` | `Charts2` (or the PascalCase chart name) |
| `requiredAttributes` | minimal (data is optional; geometry falls back to a sample/empty state) |
| `commonAttributes` | the standard set (`enabled, visible, width, height, padding, style, theme, accessibility`) |
| `visualStates` | the standard 8 (`normal, disabled, hover, pressed, focused, selected, validation, loading`) |
| `accessibility` | role (e.g. `Image`/`Group`) + nameSource + stateMetadata + keyboard, same schema as existing rows |
| `events` | typically none (charts are presentational; data is parent-owned) |
| `supportStatus` | `supported` |

Invariants: renders coherently under **both** themes from a theme-role-derived palette; **no** branch
on theme identity; passes the chart-control test families (catalog/semantic/accessibility/rendering).
Data is parent-owned via attributes (`series`/`values`/`items`); no internal mutable state.

## 2. Chart-coverage-matrix row

The matrix is a doc table (`docs/product/ant-design/coverage/ant-chart-coverage.md`); each row:

| Column | Meaning | Validated by honesty check |
|---|---|---|
| `antChart` | exact Ant Charts overview name | must be present for every overview entry (no gaps) |
| `antCategory` | Ant's grouping (Statistical / Relational / Hierarchical / Geo-Flow / General) | informational |
| `disposition` | `existing` \| `net-new` \| `composition` \| `not-applicable` | must be one of the four; never blank |
| `repoControls` | chart-control id(s) involved (for existing/net-new/composition) | each id must exist in `Catalog` |
| `tokenEntries` | ≥1 `DesignTokensExt`/`DesignTokens` entry the styling uses (for covered rows) | each must exist in the `DesignSystem` public surface |
| `rationale` | one line; required for `composition` and `not-applicable` | non-empty |

Matrix header records: the Ant source (the central reference hub) and the hub's snapshot retrieval date
(`2026-06-16`) — the hub is the single owner of that date; no fabricated upstream version label.

## 3. Ant chart palette mapping (helper, not a public type)

A pure helper `chartPalette: Theme -> Color list` (internal to the chart geometry) deriving a
categorical series palette from existing `Theme` roles — e.g. `[Accent; Danger; Success; Warning;
Muted; Foreground]` — for the **net-new** charts. The existing five charts keep their current literal
palette on the Default path (SC-004 byte-identical) and diverge under Ant via the `Accent`-driven
primary series + theme-role axis/grid/legend.

| Source | Use |
|---|---|
| `Theme.Accent` | primary series / brand mark (Ant brand-blue under `AntTheme`) |
| `Theme.Danger` / `Success` / `Warning` | categorical series + status marks (gauge/funnel/box outliers) |
| `Theme.Muted` | axis lines, gridlines, inactive marks, treemap/heatmap low end |
| `Theme.Foreground` / `Background` | labels / canvas; on-mark text |

Validation: no inline hex/size at chart use sites for net-new charts; every colour traces to a `Theme`
role (token-sourced). The helper is theme-identity-blind (a function of role *values*, not `Theme.Name`).

## 4. Provenance / decision record

A `docs/product/decisions/0007-antdesign-charts-adoption.md` entry recording: the design-language-only
posture (no AntV/React/JS dependency), the net-new public chart controls (surface delta), the chosen Ant
Charts snapshot (via the hub), and the "no token-value change / opt-in / no fork / no charting
dependency" guarantees. The hub gains an Ant Charts overview snapshot section.

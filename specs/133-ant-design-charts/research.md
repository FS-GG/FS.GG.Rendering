# Phase 0 Research: Ant Design Charts adoption (D2C.1)

## R1 — The Ant-styling seam for charts (how charts diverge under the Ant theme)

- **Decision**: Route chart mark/axis/grid/legend colours through **theme roles** read in the chart
  geometry (`Control.fs`'s `lineGeom`/`barGeom`/`pieGeom`/`scatterGeom`/`graphGeom` and the net-new
  chart geometry). The existing five charts already colour their **primary series** from `theme.Accent`
  (via the `palette`/`colorAt` helpers, index 0), so under the Ant theme (Ant brand-blue Accent) they
  already diverge; axis/grid/legend/background already read `theme.Foreground`/`Muted`/`Background`.
- **Rationale**: This is the same resolver/token seam D2.1 used — no chart-control fork, no theme-
  identity branch. The Ant theme (feature 132) supplies Ant token values; the chart geometry is a pure
  function of `theme`.
- **Alternatives considered**: (a) a per-theme literal chart palette — rejected: it needs a theme-
  identity branch (breaks FR-007) or changes Default output (breaks SC-004). (b) An AntV/G2 runtime —
  rejected outright (FR-008: design language only, no charting engine).

## R2 — Default-theme byte-identical strategy (SC-004)

- **Decision**: Leave the **existing five charts' Default render path untouched** (their literal
  categorical palette for series ≥2 stays exactly as today). Their Ant divergence rides the already
  `Accent`-driven primary series + theme-role axis/grid/legend — which is sufficient for the parity
  test's "≥1 divergent visual property" assertion. **Net-new charts** have no pre-feature baseline, so
  they are **fully theme-role-derived** (a categorical palette built from `Accent`/`Danger`/`Success`/
  `Warning`/`Muted`/`Foreground`), diverging cleanly under Ant.
- **Rationale**: Keeps the existing charts' Default output byte-for-byte identical (no geometry change
  to their Default path), while net-new charts — having no baseline — can be token/role-driven without
  any byte-identical constraint. No existing design-token value changes (FR-009/SC-005); any new chart
  palette helper is additive and theme-role-derived (not a new token store).
- **Alternatives considered**: Re-deriving the existing charts' full palette from theme roles — rejected:
  it would change their Default multi-series output (Default `Danger`/`Success` ≠ the current literals),
  breaking SC-004.

## R3 — Net-new vs composition vs not-applicable split

- **Decision**: Add net-new generic chart controls for the high-value Ant Charts types with no repo
  analog and a clean primitive: **`area-chart`, `column-chart`, `histogram`, `box-plot`, `heatmap`,
  `radar-chart`, `rose-chart`, `waterfall-chart`, `funnel-chart`, `gauge-chart`, `sankey-diagram`,
  `chord-diagram`, `treemap`, `sunburst`** (~14). Disposition the rest:
  - `existing` — line/bar/pie/scatter/graph (the five repo charts) and the Ant types they cover
    (Line, Bar/Column-as-bar where applicable, Pie, Scatter, Network-Graph → `graph-view`).
  - `composition` — combo / dual-axis / stacked-and-grouped variants (a composition of existing charts);
    bullet (gauge + reference line) where a primitive is overkill.
  - `not-applicable` — geo/map/flow charts (choropleth, dot-map, heat-map-geo, flow-map) — they need a
    geospatial/map-tile dependency this feature forbids (FR-008); recorded with rationale.
- **Rationale**: Mirrors D2.1's "add a primitive only when a composition can't express it cleanly" rule;
  keeps the net-new count tractable while the matrix records the honest disposition of every entry.
- **Alternatives considered**: Implementing geo charts via a map dependency — rejected (FR-008). A
  larger net-new set including niche statistical variants — deferred to `composition` where a primitive
  adds little value.

## R4 — Chart-control authoring shape & render wiring

- **Decision**: Net-new charts are kind-string controls authored exactly like the existing `LineChart`
  (`Control.create "<kind>" attrs` + data attribute helpers), grouped in a new `Charts2` module pair.
  Each new kind is added to `richFamilies` and gets a geometry case in `Control.fs`'s `faithfulContent`,
  built from existing `Scene` primitives (`rectangle`/`path`/`circle`/`line`/`textRun`/`arc`).
- **Rationale**: Identical to the D2.1 net-new control mechanism (which shipped 30 controls green); the
  catalog `GENERATED` blocks are hand-maintained (no generator in-repo — the source-repo generator is
  governance material excluded at import).
- **Alternatives considered**: A typed `Props`/`Widget` front door per chart — deferred; the core
  control module shape is sufficient and matches the existing chart controls.

## R5 — Coverage-matrix & honesty-check pattern

- **Decision**: One Markdown matrix table (`docs/product/ant-design/coverage/ant-chart-coverage.md`),
  one row per Ant Charts overview entry, columns `antChart | antCategory | disposition | repoControls |
  tokenEntries | rationale`. The Expecto honesty check parses it and fails on: missing row vs a pinned
  snapshot list, blank/invalid disposition, covered row naming a chart-control id absent from `Catalog`,
  covered row naming a token entry absent from the `DesignSystem` public surface, missing rationale for
  `composition`/`not-applicable`. Reflection over `DesignTokensExt`/`DesignTokens` builds the live token
  name set (same technique as `Feature131`/`Feature132`).
- **Rationale**: Reuses the proven D2.1 mechanism verbatim, scoped to charts.

## R6 — Snapshot source (Ant Design Charts overview)

- **Decision**: Ant Design Charts is a distinct AntV-based product from the Ant Design components the
  current hub covers. Add an **Ant Charts overview snapshot** section to the central reference hub
  (`docs/product/ant-design/reference/ant-llms-sources.md`); the hub owns the `2026-06-16` retrieval
  date. The matrix's pinned snapshot list lives in the honesty-check test (the canonical Ant Charts
  overview families: statistical / relational / hierarchical / geo-flow / general).
- **Rationale**: Keeps a single owner for the retrieval date (FR-011), no fabricated version label, and
  no scattering of raw upstream URLs — consistent with the F6 hub discipline.

## Resolved unknowns

No `NEEDS CLARIFICATION` remained in the spec. All Technical-Context choices above are resolved; no
open clarifications block planning.

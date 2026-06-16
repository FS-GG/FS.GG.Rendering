# Contract: Ant Charts coverage matrix + honesty check

**Artifact**: `docs/product/ant-design/coverage/ant-chart-coverage.md`
**Guard**: `tests/Controls.Tests/Feature133ChartCoverageMatrixTests.fs` (Expecto)

## Matrix format

Header records: the Ant source (the central reference hub) and the hub's snapshot retrieval date
(`2026-06-16`) — the hub is the single owner of that date; no fabricated upstream version label. Then
one table row per Ant Design Charts overview entry:

| antChart | antCategory | disposition | repoControls | tokenEntries | rationale |
|---|---|---|---|---|---|
| Line | Statistical | existing | `line-chart` | `Seed.colorPrimary`, `Alias.Light.borderDefault` | — |
| Area | Statistical | net-new | `area-chart` | `Seed.colorPrimary`, `Alias.Light.surfaceContainer` | new generic area chart |
| Sankey | Relational | net-new | `sankey-diagram` | `Seed.colorPrimary`, `Alias.Light.borderDefault` | new generic flow diagram |
| Dual Axes | Statistical | composition | `line-chart`,`bar-chart` | `Seed.colorPrimary` | composed from two existing charts; no primitive needed |
| Choropleth Map | Geo-Flow | not-applicable | — | — | needs a geospatial/map-tile dependency this feature forbids |

## Honesty-check rules (test MUST fail if any holds)

- **H1 (no gaps)**: an Ant Charts overview entry from the pinned snapshot list has no matrix row.
- **H2 (dispositioned)**: a row's `disposition` is blank or not one of `existing` / `net-new` /
  `composition` / `not-applicable`.
- **H3 (no dangling control)**: an `existing`/`net-new`/`composition` row names a `repoControls` id
  absent from `Catalog`.
- **H4 (no dangling token)**: a covered row's `tokenEntries` names an entry absent from the
  `FS.GG.UI.DesignSystem` public surface.
- **H5 (rationale present)**: a `composition` or `not-applicable` row has an empty `rationale`.
- **H6 (no bare-deferred)**: count of entries with no disposition is zero (SC-001).

## Coverage targets (Success Criteria mapping)

- SC-001 → H1 + H6 (100% dispositioned).
- SC-002 → H3 + H4 (100% covered rows reference live chart-control + token surface).
- The summary line at the matrix foot reports counts per disposition; the test asserts the totals
  reconcile with the snapshot-list size.

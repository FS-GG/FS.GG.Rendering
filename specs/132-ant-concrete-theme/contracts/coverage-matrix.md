# Contract: Ant component coverage matrix + honesty check

**Artifact**: `docs/product/ant-design/coverage/ant-component-coverage.md`
**Guard**: `tests/Controls.Tests/Feature132CoverageMatrixTests.fs` (Expecto)

## Matrix format

Header records: Ant source (the repo Ant reference hub) and the hub's snapshot retrieval date (`2026-06-16`) — the hub is the single owner of that date; there is no upstream Ant version label to record. Then one table row per Ant component-overview entry:

| antComponent | antCategory | disposition | repoControls | tokenEntries | rationale |
|---|---|---|---|---|---|
| Button | General | existing | `button` | `Component.Button.*`, `Seed.colorPrimary` | — |
| Tag | Data Display | net-new | `tag` | `Alias.*`, `Component.*` | new generic chip control |
| Card | Data Display | composition | `panel`,`stack`,`text-block` | `Alias.*`, `Space.*` | composed; no primitive needed |
| ConfigProvider | Other | not-applicable | — | — | React/DOM provider; theme/policy selection exists non-as-a-control |

## Honesty-check rules (test MUST fail if any holds)

- **H1 (no gaps)**: an Ant overview component from the pinned snapshot list has no matrix row.
- **H2 (dispositioned)**: a row's `disposition` is blank or not one of `existing` / `net-new` / `composition` / `not-applicable`.
- **H3 (no dangling control)**: an `existing`/`net-new`/`composition` row names a `repoControls` id absent from `Catalog`.
- **H4 (no dangling token)**: a covered row's `tokenEntries` names an entry absent from the `FS.GG.UI.DesignSystem` public surface.
- **H5 (rationale present)**: a `composition` or `not-applicable` row has an empty `rationale`.
- **H6 (no bare-deferred)**: count of entries with no disposition is zero (SC-002).

## Coverage targets (Success Criteria mapping)

- SC-002 → H1 + H6 (100% dispositioned).
- SC-003 → H3 + H4 (100% covered rows reference live surface).
- The summary line at the matrix foot reports counts per disposition; the test asserts the totals reconcile with the snapshot list size.

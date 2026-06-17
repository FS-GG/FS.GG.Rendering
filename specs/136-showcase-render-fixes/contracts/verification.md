# Contract — Verification (19-page re-capture + defect-absence)

Covers FR-013, SC-001..SC-007. Reuses the feature-135 evidence harness (spec Assumption).

## Re-capture command

```bash
cd samples/AntShowcase
dotnet run --project AntShowcase.App -c Release -- evidence --seed 1
# → artifacts/ant-showcase/1/<page-id>/{frame.png,state.txt,run.json,summary.md} for all 19 pages
```
Pages (nav order): display-typography, cards-stats-media, buttons, text-numeric-input, selection-toggles,
layout-containers, navigation-menus, overlays, feedback-status, data-collections, charts-statistical,
charts-advanced, graphs-custom, tpl-workbench, tpl-list, tpl-detail, tpl-form, tpl-result, tpl-exception.

## Acceptance checks (mapped to success criteria)

| Check | Criterion |
|---|---|
| Every authored character renders as authored across all 19 pages, both themes; `@`/`—`/`#`/`▸`/`·` verified | SC-001 |
| Zero mid-word truncation; `Stable`/`Upload`/`Refresh`/numeric labels in full | SC-002 |
| Zero region overlap and zero sibling-control overlap; nothing painted outside its content region | SC-003 |
| Composite controls render canonical structure (data-grid table, menu/combo distinct, descriptions aligned, QR populated, charts in-box) | SC-004 |
| Re-captured 19-page evidence shows none of the seven defect classes; before/after reviewer confirms | SC-005 |
| Framework-caused defects fixed at framework level; sample fixes limited to composition; split recorded | SC-006 |
| G1/G2 byte-identical or re-baselined-and-disclosed | SC-007 |

## Test tiers

- **Framework semantic tests** (`tests/`): glyph correctness (incl. the five audited chars), measure/advance
  agreement, overlay z-order/no-overprint, data-grid table structure, region non-overlap, container clipping,
  clipped scroll. Each fails on today's renderer, passes after.
- **Sample evidence tests** (`samples/AntShowcase/AntShowcase.Tests`): re-run the 19-page suite; theme
  invariance (antLight ≡ antDark) for every fix.

## Evidence discipline (Principle V/VI)

- Real GL screenshots where a GL/display surface exists; no-GL hosts record a disclosed degrade
  (`ProvesScreenshot=false` + reason), never a fabricated pass.
- Fallback/tofu disclosures (FR-001) surfaced in each page's `run.json`/`summary.md`.

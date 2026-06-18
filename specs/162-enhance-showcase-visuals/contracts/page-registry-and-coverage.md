# Contract: Page Registry and Coverage

## Page Registry

The authoritative page registry is `AntShowcase.Core.PageRegistry.all`.

Required baseline:

- 13 catalog pages.
- 6 enterprise template pages.
- 19 pages total.

If the live page set changes before acceptance, evidence and summaries must update to the live page
set and must not rely on stale counts.

## Catalog Coverage

Catalog coverage is a bijection from live `Catalog.supportedControls` ids to catalog-page
`ControlIds`.

Acceptance rules:

- Every live catalog control appears on exactly one catalog page.
- No assigned catalog id is absent from the live catalog.
- No catalog id is duplicated.
- Template pages have empty `ControlIds` and are exempt from the bijection.
- Coverage output records catalog count, page count, unreferenced ids, and duplicated ids.

## Evidence Matrix

Preferred visual readiness requires:

- Every page in `PageRegistry.all`.
- Both Ant themes.
- Size `1600x1000`.
- Screenshot count equal to `page-count * 2`.

Representative minimum-size evidence requires:

- Size `1280x800`.
- Both Ant themes.
- Dense, large-control, feedback/status, form, result, or exception representatives as specified by
  quickstart or readiness summary.

## Drift Rules

- If catalog count changes, `coverage` must fail until page assignments are updated.
- If page count changes, the visual-readiness command must use the new registry and summaries must
  record the new count.
- A readiness summary with stale page, theme, or catalog counts is invalid.

## Test Expectations

Focused tests must assert:

- `CoverageMap.check ()` reports zero unreferenced and zero duplicated ids.
- The catalog count matches the live package surface.
- Page ids are unique and directly selectable.
- Visual-readiness expected screenshot count derives from the live page registry, not a hardcoded
  stale count.

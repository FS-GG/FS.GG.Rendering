# Responsiveness summary resp-20260619-225632-944a3b

- scope: second-antshowcase/all-interactive/antLight
- overall readiness: environment-limited
- baseline profile: 2026-06-19
- optimized profile: resp-20260619-225632-944a3b
- preparation reduction: n/a
- first-frame preparation reduction: n/a
- parity: environment-limited
- records: records.jsonl
- first failed budget: environment-boundary
- caveat: SYNTHETIC deterministic headless substitute; no accepted live input-to-present readiness claimed.
- required interactive families: 14
- accepted interactive families: 0
- rejected interactive families: 0
- blocked interactive families: 14
- display-only exclusions: 51
- missing interactive families: 0
- artifact write status: complete

Links: `summary.json`, `records.jsonl`, `environment.md`

| Page | Input | Control | Count | p50 | p95 | max | long frames | readiness |
|------|-------|---------|-------|-----|-----|-----|-------------|-----------|
| buttons | pointer-discrete | button-click | 1 | n/a | n/a | n/a | 1 | environment-limited |
| buttons | pointer-discrete | toggle-switch | 1 | n/a | n/a | n/a | 1 | environment-limited |
| text-numeric-input | key-down | text-entry | 1 | n/a | n/a | n/a | 1 | environment-limited |
| text-numeric-input | key-down | numeric-entry | 1 | n/a | n/a | n/a | 1 | environment-limited |
| text-numeric-input | pointer-discrete | date-time | 1 | n/a | n/a | n/a | 1 | environment-limited |
| text-numeric-input | pointer-move | slider-rating | 1 | n/a | n/a | n/a | 1 | environment-limited |
| selection-toggles | pointer-discrete | selection-single | 1 | n/a | n/a | n/a | 1 | environment-limited |
| selection-toggles | pointer-discrete | selection-multi | 1 | n/a | n/a | n/a | 1 | environment-limited |
| navigation-menus | pointer-discrete | navigation | 1 | n/a | n/a | n/a | 1 | environment-limited |
| cards-stats-media | pointer-discrete | disclosure | 1 | n/a | n/a | n/a | 1 | environment-limited |
| text-numeric-input | pointer-discrete | upload | 1 | n/a | n/a | n/a | 1 | environment-limited |
| data-collections | pointer-discrete | data-collection | 1 | n/a | n/a | n/a | 1 | environment-limited |
| feedback-status | pointer-discrete | form-validation | 1 | n/a | n/a | n/a | 1 | environment-limited |
| graphs-custom | pointer-discrete | graph-custom | 1 | n/a | n/a | n/a | 1 | environment-limited |

## Missing Interactive Families

- none

## Environment Limitations

- headless-substitute:no-live-presentation-boundary
- test-override:forced-substitute
- presentation:missing-boundary
- require-live:visible-surface-unavailable

## Drag Continuity

- `slider-rating`: missing-boundary

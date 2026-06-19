# Responsiveness summary resp-20260619-225707-d7532f

- scope: second-antshowcase/all-interactive/antDark
- overall readiness: rejected
- baseline profile: 2026-06-19
- optimized profile: resp-20260619-225707-d7532f
- preparation reduction: pending render-lag probe correlation
- first-frame preparation reduction: pending render-lag probe correlation
- parity: not-accepted
- records: records.jsonl
- first failed budget: input-to-visible-p95
- evidence path: live GL viewer presentation boundary
- required interactive families: 14
- accepted interactive families: 0
- rejected interactive families: 14
- blocked interactive families: 0
- display-only exclusions: 51
- missing interactive families: 0
- artifact write status: complete

Links: `summary.json`, `records.jsonl`, `environment.md`

| Page | Input | Control | Count | p50 | p95 | max | long frames | readiness |
|------|-------|---------|-------|-----|-----|-----|-------------|-----------|
| buttons | pointer-discrete | button-click | 1 | 253.865 | 253.865 | 253.865 | 1 | rejected |
| buttons | pointer-discrete | toggle-switch | 1 | 11.321 | 11.321 | 11.321 | 0 | accepted |
| text-numeric-input | key-down | text-entry | 1 | 9.781 | 9.781 | 9.781 | 0 | accepted |
| text-numeric-input | key-down | numeric-entry | 1 | 5.31 | 5.31 | 5.31 | 0 | accepted |
| text-numeric-input | pointer-discrete | date-time | 1 | 5.24 | 5.24 | 5.24 | 0 | accepted |
| text-numeric-input | pointer-move | slider-rating | 1 | 5.213 | 5.213 | 5.213 | 0 | accepted |
| selection-toggles | pointer-discrete | selection-single | 1 | 6.37 | 6.37 | 6.37 | 0 | accepted |
| selection-toggles | pointer-discrete | selection-multi | 1 | 5.207 | 5.207 | 5.207 | 0 | accepted |
| navigation-menus | pointer-discrete | navigation | 1 | 5.228 | 5.228 | 5.228 | 0 | accepted |
| cards-stats-media | pointer-discrete | disclosure | 1 | 5.24 | 5.24 | 5.24 | 0 | accepted |
| text-numeric-input | pointer-discrete | upload | 1 | 5.244 | 5.244 | 5.244 | 0 | accepted |
| data-collections | pointer-discrete | data-collection | 1 | 5.22 | 5.22 | 5.22 | 0 | accepted |
| feedback-status | pointer-discrete | form-validation | 1 | 5.256 | 5.256 | 5.256 | 0 | accepted |
| graphs-custom | pointer-discrete | graph-custom | 1 | 5.209 | 5.209 | 5.209 | 0 | accepted |

## Missing Interactive Families

- none

## Drag Continuity

- `slider-rating`: continuous

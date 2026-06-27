# T016 — Snapshot tag namespace swap (US2, SC-003) — verified 2026-06-27

New `fs-gg-ui/v*` tags created at the SAME commits the legacy tags pointed to (FR-004), carrying the
same snapshot subjects; legacy `fs-skia-ui/v*` deleted local+remote (FR-005).

| tag | commit | subject carried |
|---|---|---|
| fs-gg-ui/v0.1.50-preview.1 | 57be86c | "coherent FS.GG.UI.* snapshot for fs-gg-ui template pin" |
| fs-gg-ui/v0.1.51-preview.1 | d9f4c81 | "Coherent FS.GG.UI snapshot 0.1.51-preview.1 — 16 members + the new FS.GG.UI BOM/metapackage (feature 207)…" |

Verification:
- `git tag -l 'fs-gg-ui/v*'` → exactly the two ✓
- `git tag -l 'fs-skia-ui/v*'` → empty ✓ (clean break, FR-005)
- `git rev-list -n1 fs-gg-ui/v0.1.50-preview.1` → 57be86c… ✓
- `git rev-list -n1 fs-gg-ui/v0.1.51-preview.1` → d9f4c81… ✓
- remote (`git ls-remote --tags origin`): fs-gg-ui/v* present, fs-skia-ui/v* gone ✓
- unrelated `fs-gg-ui-template/v0.1.50-preview.1` left untouched ✓

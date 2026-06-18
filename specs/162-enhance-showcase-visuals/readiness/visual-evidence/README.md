# Preferred Visual Evidence

Preferred evidence is captured at `1600x1000` for all live AntShowcase pages in both canonical themes.

Expected files:

- `summary.md`
- `summary.json`
- `contact-sheet-light.png`
- `contact-sheet-dark.png`
- `reviewer-defects.md`
- `light/<page-id>.png`
- `dark/<page-id>.png`
- `completeness/summary.md`
- `completeness/missing.md`
- `completeness/degraded.md`
- `completeness/dimensions.md`

The summary must resolve CLI aliases `light,dark` to canonical `antLight,antDark` and must not mark readiness accepted without reviewer classification.

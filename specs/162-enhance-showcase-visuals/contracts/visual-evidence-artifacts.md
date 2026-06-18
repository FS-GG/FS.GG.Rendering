# Contract: Visual Evidence Artifacts

## Preferred Evidence Tree

```text
readiness/visual-evidence/
|-- summary.md
|-- summary.json
|-- contact-sheet-light.png
|-- contact-sheet-dark.png
|-- reviewer-defects.md
|-- light/
|   |-- display-typography.png
|   `-- ...
|-- dark/
|   |-- display-typography.png
|   `-- ...
`-- completeness/
    |-- summary.md
    |-- missing.md
    |-- degraded.md
    `-- dimensions.md
```

## Summary Fields

- Command and arguments.
- Run id.
- Seed.
- Requested size.
- Accepted-size role: preferred or minimum.
- Page count.
- Page ids.
- Theme count.
- Theme ids.
- Required screenshot count.
- Present screenshot count.
- Contact sheet paths.
- Completeness status.
- Capture availability.
- Reviewer defect classification status.
- Visual-readiness status.
- Limitations and lower-level follow-ups.

## Screenshot Rules

- File names are stable and derived from page id and theme.
- Every screenshot path is inside the requested output tree.
- Image dimensions equal the requested size.
- Images are decodable and non-empty.
- Stale screenshots from prior runs are not counted.
- A screenshot can prove visual readiness only when `captureSource` is real screenshot capture, not
  deterministic scene fallback.

## Contact Sheet Rules

- Contact sheets group screenshots by theme.
- Contact sheets are generated only from screenshots that passed completeness checks.
- Contact sheets label or order pages using stable page ids so reviewers can map defects back to
  pages.
- Missing or degraded theme images prevent the affected contact sheet from being marked complete.

## Completeness Rules

Preferred evidence passes completeness only when:

- Required page count is 19.
- Required theme count is 2.
- Required screenshot count is 38.
- Present valid screenshot count is 38.
- No screenshot is missing, degraded, stale, undecodable, or wrong-size.

Completeness is necessary but not sufficient for accepted visual readiness.

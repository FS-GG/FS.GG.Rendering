# Contract: Visual Review

## Source Guidance

Visual review must use local repo guidance:

- `docs/product/ant-design/reference/ant-llms-sources.md`
- `docs/product/ant-design/README.md`
- `docs/product/ant-design/patterns/display.md`
- `docs/product/ant-design/patterns/input.md`
- `docs/product/ant-design/patterns/selection.md`
- `docs/product/ant-design/patterns/layout.md`
- `docs/product/ant-design/patterns/navigation.md`
- `docs/product/ant-design/patterns/overlay.md`
- `docs/product/ant-design/patterns/feedback.md`
- `docs/product/ant-design/patterns/data.md`
- `docs/product/ant-design/patterns/chart.md`
- `docs/product/ant-design/patterns/graph.md`
- `docs/product/ant-design/patterns/custom.md`

Raw upstream Ant URLs should not be cited in sample docs or review artifacts; link the local hub.

## Accepted Sizes and Themes

Accepted sizes:

- preferred: `1600x1000`
- minimum: `1280x800`

Accepted themes:

- `antLight`
- `antDark`

Required target matrix:

```text
all pages x antLight,antDark x preferred,minimum
```

With 19 planned pages, the required count is 76 targets.

## Review Categories

The reviewer classification must cover:

- palette roles
- spacing rhythm
- typography hierarchy
- contrast
- clipping
- overlap
- alignment
- normal, hover, active, focus, disabled, selected, checked, error, validation, loading, and overlay states where supported
- Ant Design conformance against local pattern docs
- stale or drifted state after theme switch

## Finding Lifecycle

Finding statuses:

- `open`: issue detected and not fixed
- `fixed`: code/content changed but affected target not re-reviewed
- `reviewed`: affected target re-reviewed
- `closed`: reviewer accepted the fix

Final acceptance requirements:

- zero `open` findings
- zero `fixed` findings
- zero `reviewed` but unclosed blocking findings
- every closed finding has a re-reviewed target record
- every required target has reviewer classification
- every required target has live accepted capture evidence

## Environment-Limited Evidence

When live visual capture or live window review is unavailable:

- commands must complete without hanging
- output must state the host limitation
- target status must be `environment-limited` or `degraded`
- the run must not claim accepted visual fidelity
- final visual acceptance remains blocked or review-required until live review is supplied

## Reviewer Artifacts

Required artifacts:

```text
readiness/
|-- visual-review-summary.md
|-- visual-review-summary.json
|-- visual-findings.md
|-- limitations.md
|-- preferred/
|   |-- light/
|   |-- dark/
|   `-- contact-sheet.png
`-- minimum/
    |-- light/
    |-- dark/
    `-- contact-sheet.png
```

`visual-review-summary.json` must be sufficient to compute:

- required target count
- complete target count
- degraded/environment-limited count
- reviewer-classified target count
- unresolved finding count
- overall status

Markdown summaries must not hide limitations or unresolved findings.

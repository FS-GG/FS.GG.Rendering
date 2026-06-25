# Feedback — feature 196 implementation phase

## Process friction

- **tasks.md T005 was subtly wrong (severity: medium).** It instructed wiring a no-op `labelNode` stub
  that "returns `Scene.group []`" into `drawSymbol`/`drawBadge`/`drawRing` and claimed this keeps label-free
  output byte-identical. It does **not**: `Scene.empty`/`Scene.group []` each emit a node (`Empty`/`Group`),
  so appending one to a grammar's child list adds an element and **drifts** the pinned goldens. The correct
  zero-drift form is **conditional append** — the helper returns `Scene option` and the grammar appends the
  node only on `Some`. The green `token`/`gallery`/`filmstrip` goldens confirm the conditional form.
  *How to apply:* when a "no-op channel must be byte-identical", never emit a placeholder node — omit it.

- **FR-013 vs. baseline granularity (severity: low).** The spec/plan/tasks assumed the surface baseline
  "gains the `Label` field." The actual `refresh-surface-baselines.fsx` records **type names only**, not
  record fields, so a new field produces **zero baseline diff**. The authoritative field-level Tier-1
  contract is the curated `Symbology.fsi`. Future Tier-1 specs that add only a record field should expect
  zero baseline drift and treat the `.fsi` diff as the surface evidence.

## Generalizable-code candidates

- A public `Scene` glyph-run / text-run extractor (a `Scene -> GlyphRunData list` fold) would let tests
  assert drawn-label width without a hand-rolled `SceneNode` walk. Today the test traverses the public
  `Scene`/`SceneNode` IR directly — fine, but a shared helper would remove the duplication if more
  text-bearing features land.

## What worked

- The `measureTextResolved`/`glyphRunProof` seam made the pure-vs-render split clean: pure library bakes a
  deterministic proof node; the render edge (`Fonts.resolveText` → `report.TofuCount = 0`) supplies real
  tofu-free coverage evidence. Verifying tofu-free via the real bundled-font registry (not a synthetic
  assertion on the pure `Missing` flag, which is always `false`) is the honest path.

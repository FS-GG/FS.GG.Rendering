# Contract — Font Registry & Text Rendering (`FS.GG.UI.SkiaViewer`)

Covers FR-001, FR-002, FR-010 and the determinism constraint (SC-005).

## Bundled standard set (always available)

`FS.GG.UI.SkiaViewer` embeds and exposes these families, loaded via `SKTypeface.FromStream` from manifest
resources (never the host's `SKTypeface.Default`):

- `Noto Sans` (Regular, Bold), `Noto Sans Mono` (Regular)
- `Inter` (Regular, Bold), `JetBrains Mono` (Regular)
- `DejaVu Sans` (Regular, Bold), `DejaVu Sans Mono` (Regular)

Default family `Noto Sans`; default mono `Noto Sans Mono`. Licenses (OFL/free) recorded in PROVENANCE.

## Resolution contract

- Given a text-render request `{ Family; Weight; Size }`, the registry returns a real cached `SKFont` for the
  best matching bundled face.
- **Per-character fallback chain**: requested family → next bundled family in fixed order → deliberate ASCII
  substitute → disclosed tofu box. A character is only ever drawn as: the authored glyph, a deliberate
  substitute, or a tofu box. **It is never drawn as a different plausible glyph** (the `@`→`7` defect).
- Required correct glyphs: `@`, `#`, digits, ASCII letters (mixed case preserved — no force-uppercase).
- Permitted deliberate substitutes for uncovered decoratives: `—`→`–`/`-`, `▸`→`>`, `·`→`•`.

## Measurement contract (FR-002)

- The advance used to **size** a text box equals the advance used to **draw** it.
- `Scene.measureText` (pure) is calibrated to the default family; the real-font measurer
  (`SKFont.MeasureText`) is injected into the sizing path when rendering through Skia.
- Acceptance: `Stable`, `Upload`, `Refresh`, and numeric-input labels render in full with no clip.

## Disclosure contract (FR-001)

- Every `Substituted`/`Tofu` outcome emits a structured diagnostic at the use site and is aggregated into the
  per-page evidence record (counts + affected code points).
- A missing bundled asset fails loudly (Principle VI), not silently.

## Determinism contract (SC-005)

- Identical input + seed ⇒ byte-identical text rendering on any host, because text never depends on host
  fonts. Verified by two same-seed headless runs.

## Test oracle

- `@` in `ada@example.com` renders as `@` (not `7`); `—`/`#`/`▸`/`·` render as authored-or-deliberate, never
  the wildcard `7`-shape.
- Mixed case preserved (`Stable`, not `STABLE`).
- Two same-seed runs are byte-identical.

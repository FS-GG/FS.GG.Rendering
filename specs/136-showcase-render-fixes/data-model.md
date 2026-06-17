# Phase 1 Data Model — Showcase Rendering Defect Fixes

This feature is a rendering/correctness fix; the "entities" are the new framework structures plus the
verification ledgers. Field shapes are advisory — the binding declaration is each module's `.fsi`.

## 1. Bundled font set (embedded assets)

The standard set always available in `FS.GG.UI.SkiaViewer`, declared as `<EmbeddedResource>`:

| Family name (logical) | Faces (weight) | Asset | Kind |
|---|---|---|---|
| `Noto Sans` | Regular, Bold | `NotoSans-Regular.ttf`, `NotoSans-Bold.ttf` | sans |
| `Noto Sans Mono` | Regular | `NotoSansMono-Regular.ttf` | mono |
| `Inter` | Regular, Bold | `Inter-Regular.ttf`, `Inter-Bold.ttf` | sans |
| `JetBrains Mono` | Regular | `JetBrainsMono-Regular.ttf` | mono |
| `DejaVu Sans` | Regular, Bold | `DejaVuSans.ttf`, `DejaVuSans-Bold.ttf` | sans |
| `DejaVu Sans Mono` | Regular | `DejaVuSansMono.ttf` | mono |

**Default family**: `Noto Sans` (used when `Theme.FontFamily = None`). **Default mono**: `Noto Sans Mono`.
Each face carries its license (OFL/free) — recorded in `PROVENANCE.md`.

## 2. FontRegistry (new — `SkiaViewer/Fonts.fs[i]`)

The resolver from a logical request to a real, cached typeface.

- **Request**: `{ Family: string option; Weight: int option; Size: float }` (derived from `FontSpec` /
  `Theme`).
- **Resolution**: requested family → next bundled family in a fixed fallback order → … . Returns the resolved
  `SKTypeface` + a `Resolved` record (which family/face actually served).
- **Cache**: keyed by `(familyName, weight, size)` → `SKFont`; loaded once per process (`mutable`/`Dictionary`,
  disclosed at use site as a render cache). I/O (asset load) happens once at the SkiaViewer edge.
- **Fixed fallback order**: requested family first, then `Noto Sans → Inter → DejaVu Sans` (sans) and
  `Noto Sans Mono → JetBrains Mono → DejaVu Sans Mono` (mono), so a missing glyph in one family is sought in
  the next before substitution/tofu.

## 3. FallbackResolution (per-character outcome — disclosure)

Records what actually rendered for a character, for FR-001 disclosure.

- `Authored` — drawn from a bundled face that covers the character (the normal case; `@`, `#`, letters).
- `Substituted of original:char * substitute:char` — deliberate legible swap (`—`→`–`, `▸`→`>`, `·`→`•`).
- `Tofu of original:char` — no coverage and no deliberate substitute → visible missing-glyph box.

Aggregated per rendered page into the evidence record: counts of substituted/tofu characters and the set of
affected code points. **Invariant**: no code path may produce a wrong *but plausible* glyph (the `@`→`7`
class); only `Authored`, `Substituted`, or `Tofu` are permitted outcomes.

## 4. TextMeasurement seam (`Scene.measureText` + injected real-metrics measurer)

- `Scene.measureText : string -> FontSpec -> TextMetrics` stays pure; its heuristic is **calibrated** so its
  advance matches the bundled-font renderer for the default family at common sizes.
- A real-metrics measurer (`SkiaViewer`, using `SKFont.MeasureText`) is injected into the sizing path so box
  sizing uses true advances. **Invariant**: the advance used to *size* a control's text box equals the
  advance used to *draw* it → no mid-word clip (FR-002).
- `TextMetrics` shape unchanged: `{ Width; Height; Baseline }`.

## 5. OverlayLayer node (render order)

- A control may be marked as an **overlay/transient** surface (built on the existing `Overlay` container).
- `Control.renderTree` produces two ordered groups: **in-flow** scene then **overlay** scene; the final scene
  is `inFlow @ overlay` so overlays paint last (z-top).
- Hit-testing (`nearestAuthored`) consults the overlay group first so the topmost overlay wins.
- **Invariant**: an open transient surface's drawn area is never overprinted by an in-flow sibling.

## 6. Composite-control structural rules

| Control | Rule |
|---|---|
| `data-grid` | `data-grid-row`/`-header` → `Row`; shared column-width track; header/body cells aligned. |
| `menu`/`context-menu`/combo rows | `rowH = max(minRowHeight, box.Height/n)`; items never share a baseline. |
| `descriptions` | spacing scaled or truncated to `box.Height`; never paints past the box. |
| `qr-code` | minimum module-grid size; clipped to box; non-empty payload → populated grid. |
| charts | geometry clipped to `RectClip box`; degenerate data (`n=0`/NaN/Inf) guarded. |

## 7. Layout/region rules

- **Region bounds**: app bar, nav rail, content, feedback, status occupy mutually non-overlapping rects
  (flex honours explicit basis; Shell declares sizes).
- **Container clipping**: every container clips its children to its bounds in `paintNode`.
- **ScrollViewer viewport**: clips content to its box; exposes a scroll offset + affordance; taller content is
  clipped, not spilled.

## 8. Defect ↔ remediation-layer matrix (FR-011 / SC-006)

The authoritative split recorded for SC-006 (count of framework vs sample fixes).

| Defect class | Remediation layer | Where |
|---|---|---|
| wrong-glyph | framework (renderer) | R1/R3 — `Fonts`, `SceneRenderer` |
| truncated-text | framework (renderer+scene) | R2 — measurement seam |
| composite-structure | framework (control) | R5 — `Control.fs` geometry |
| overlay-overprint | framework (renderer+control) | R4 — overlay pass |
| control-overlap | framework (layout) | R6 — `paintNode` clipping |
| region-overlap | framework (layout) + **sample** | R6 — flex + **Shell sizing** |
| unbounded-content (scroll/spill) | framework (layout/control) + **sample** | R6 — viewport + **Shell width/scroll** |

**SC-006 split**: framework = 5 classes fully + 2 classes partly; sample = chrome-region sizing + nav width +
content scroll only. Recorded in the rebaseline ledger.

## 9. Rebaseline ledger (verification artifact)

See `contracts/rebaseline-ledger.md`. One row per re-established baseline: baseline id/path, the defect/fix
that changed it, before/after note, and theme(s) affected.

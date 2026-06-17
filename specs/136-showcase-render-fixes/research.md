# Phase 0 Research — Showcase Rendering Defect Fixes

Resolved decisions (R1–R8). Each: **Decision / Rationale / Alternatives considered**. Findings are anchored
to concrete code locations confirmed during investigation.

## Root-cause map (confirmed)

| Defect class | Root cause (file:line) | Owning layer |
|---|---|---|
| Wrong glyph (`@`→`7`, `—`/`#`/`▸`/`·`) | `drawTextWithFallback` whole-string `ContainsGlyphs` check drops to `vectorGlyphPattern`; unmapped chars hit the `_` wildcard (`SceneRenderer.fs:219`) that visually reads as `7` (cf. `'7'` at `:197`). Decoratives `—`/`#`/`▸`/`·` unmapped. | Framework (renderer) |
| All-caps text | `vectorGlyphPattern` calls `Char.ToUpperInvariant` (`SceneRenderer.fs:163`). | Framework (renderer) |
| Truncated text (`Stable`→`STABL`) | Vector advance `cell*6 ≈ 0.857·size` (`SceneRenderer.fs:222-223`) vs measurer `0.58·size` (`Scene.fs:534`); boxes sized too small → clip rect cuts text (`Control.fs:~1815-1824`). | Framework (renderer + scene) |
| data-grid stacked vertically | `directionOf` defaults `data-grid-row`/`-header` to `Column` (`Control.fs:1901-1911`). | Framework (control) |
| menu/combo item overprint | `rowsGeom` `rowH = box.Height/n` collapses when box short (`Control.fs:898-914`). | Framework (control) |
| dropdowns overprint neighbours | No overlay/z-layer; dropdowns drawn in-flow at fixed offsets (`Control.fs:1099,1643`); single in-flow pass in `renderTree`. | Framework (renderer + control) |
| descriptions overlap | Fixed `16 + i*22` offsets exceed box (`Control.fs:1420-1426`). | Framework (control) |
| charts overrun | No clip + degenerate-data (`n=0`/NaN) edge cases (`Control.fs:513-645`). | Framework (control) |
| qr-code blank | `side = min(w,h)-8`; collapses to ~0 when box compressed (`Control.fs:1457-1464`). | Framework (control) |
| region/control overlap, spill | Flex splits main-axis size uniformly when no basis (`Layout.fs:~272`); `paintNode` does not clip container children (`Control.fs:~2035-2040`). | Framework (layout) + sample (Shell sizing) |
| no scroll | `ScrollViewer` is visual chrome only; `scrollViewerGeom` draws a bar but never clips a viewport (`Control.fs:1292-1297`). | Framework (control/layout) |

## R1 — Text rendering: bundle a standard font set (DECIDED)

**Decision**: Ship a **standard set of fonts** as embedded assets in `FS.GG.UI.SkiaViewer` and route all text
through them deterministically. Bundle **all three families** (per user direction): **Noto Sans**
(Regular + Bold) + **Noto Sans Mono**, **Inter** (Regular + Bold) + **JetBrains Mono**, **DejaVu Sans** +
**DejaVu Sans Mono**. A new `Fonts` module (`SkiaViewer/Fonts.fs[i]`) loads each via
`SKTypeface.FromStream` from `GetManifestResourceStream`, caches the `SKTypeface`/`SKFont` per (family,
weight, size), and resolves a `Theme.FontFamily` name to a real typeface. `drawTextWithFallback` is rewritten
to use the registry; the 5×7 `vectorGlyphPattern` is demoted to the final disclosed-tofu renderer only.

**Rationale**:
- **Determinism** — the byte-identical same-seed evidence gate (feature-135 SC-004; this feature SC-005)
  cannot hold while text depends on the host's `SKTypeface.Default`. Bundled assets are fixed across the
  sandbox, CI, and the dev box.
- **Fidelity + coverage in one move** — real typefaces give correct `@`/`#`/`—`/`·` *and* mixed-case
  credible text, which the 5×7 path destroys (uppercases, ~no symbol coverage). The showcase's purpose is to
  look production-credible.
- **Benefits every consumer** (FR-011) — fixed once in the renderer, not per sample.
- **Standard set, not one face** — bundling the set makes regular/bold/mono always available so controls
  never fake weight, and gives the design-system room to map `Theme.FontFamily` names onto real faces.

**Alternatives considered**:
- *Expand the 5×7 vector path only* — rejected: keeps blocky all-caps text (fails "render as authored" and
  credibility) even though it is deterministic; still needs the measurement reconciliation anyway.
- *Use `SKTypeface.Default` + per-char fallback, no bundling* — rejected: host-dependent output breaks
  determinism; coverage is whatever the host happens to have.
- *Bundle a single face* — rejected by user direction in favour of the full standard set (regular/bold/mono
  across families).
- *Add a NuGet font package* — rejected: embedded `.ttf` assets avoid a new runtime dependency
  (constitution: minimize dependencies); licenses (OFL/free) are recorded in PROVENANCE.

**Open verification (P-A probe, per repo "probe-driven render debugging" practice)**: confirm empirically
whether `SKTypeface.Default` has glyphs in the headless sandbox today (explains how often the vector path
fires now) and that embedded `SKTypeface.FromStream` loading succeeds in the GL screenshot path.

**P-A probe RESULTS (T004, standalone SkiaSharp 4.147.0-preview.3.1 console, run 2026-06-17, headless):**

- **(a) `SKTypeface.Default` covers nothing here.** `FamilyName = ""` and `ContainsGlyph` is `false` for
  every probe character — including plain `A`, `0`, `S`. So today the whole-string `ContainsGlyphs` check
  in `drawTextWithFallback` fails on *all* text and the 5×7 `vectorGlyphPattern` path fires for **everything**
  — which is exactly why the showcase reads as blocky ALL-CAPS with `@`→`7`. This is the root-cause
  confirmation: it is not "occasional" fallback; the native path never runs in this sandbox.
- **(b) Every bundled face loads via `SKTypeface.FromStream` and covers the critical glyphs.** All nine
  faces load (non-null typeface, correct `FamilyName`) and cover `@ # S digit ·`. The only gap: `▸`
  (U+25B8) is **absent from Noto Sans and Inter** but **present in DejaVu Sans, DejaVu Sans Mono, Noto Sans
  Mono, JetBrains Mono**. ⇒ the fixed sans fallback chain (Noto Sans → Inter → **DejaVu Sans**) resolves
  `▸` from DejaVu before any ASCII substitute is needed; the `▸`→`>` deliberate substitute (R3) is only a
  last-resort if no bundled face covers it.
- **Advance**: real `SKFont.MeasureText "Stable"` at 16px = **47.0px**, vs the pure heuristic
  `0.58·6·16 = 55.7px` and the vector advance `0.857·6·16 = 82.3px`. The truncation class is the
  measure-vs-draw gap; sizing boxes from the real measurer (R2 seam) makes the clip rect fit the drawn
  text.

**Implication for implementation**: (1) the registry MUST route through `FromStream` and never touch
`SKTypeface.Default`; (2) the per-character fallback chain across the bundled families (not just the
requested family) is load-bearing for `▸`; (3) the measurement seam must use the same `SKFont` the renderer
draws with.

## R2 — Measurement reconciled to rendering (DECIDED)

**Decision**: Introduce a **measurement seam** so `Scene.measureText` agrees with the bundled font's real
advances. `Scene` stays pure (no SkiaSharp), so the real-metrics measurer is provided by `SkiaViewer` (which
owns `SKFont.MeasureText`) and injected; `Scene.measureText` keeps a calibrated heuristic as the default for
pure callers, but the layout/box-sizing path uses the real-font measurer when rendering through Skia. Both
sides MUST yield the same advance the renderer uses, so clip rects fit the drawn text.

**Rationale**: The truncation class is purely a measure/render disagreement (`0.58` vs `0.857` today). The
divergence exists because measurement lives in the pure `Scene` layer while glyph advances live in the Skia
renderer. A seam lets the real metrics flow to sizing without polluting `Scene` with a SkiaSharp dependency
(Principle III/IV — I/O at the edge).

**Alternatives considered**:
- *Hardcode a single shared constant in both places* — rejected: real fonts have per-glyph advances; one
  constant re-introduces drift for proportional text and still mis-sizes mono vs sans.
- *Move `measureText` into SkiaViewer* — rejected: would make `Scene` depend on SkiaSharp, breaking the pure
  geometry layer and many pure callers/tests.

## R3 — Fallback & substitution policy with disclosure (DECIDED)

**Decision**: A per-character fallback chain: **requested family → other bundled families → deliberate ASCII
substitute → disclosed "tofu" box**. `@` and `#` MUST render correctly (covered by the bundled fonts).
Decorative punctuation with no coverage may be substituted with a deliberate, legible equivalent (`—`→`–`/`-`,
`▸`→`>`, `·`→`•`); a truly-uncovered character renders as a visible tofu box (never a wildcard that looks
like another character). Every substitution and tofu is **disclosed**: a structured diagnostic at the use
site and a field in the per-page evidence record (FR-001).

**Rationale**: FR-001 demands authored characters render correctly *and* that any fallback be deliberate and
disclosed — the current silent `@`→`7` is the exact anti-pattern. Spec Assumption #3 already permits ASCII
substitution for decoratives while requiring `@` correct.

**Alternatives considered**:
- *Bundle a symbol font for `▸` et al.* — deferred/rejected: adds assets for purely decorative glyphs the
  spec already allows substituting; revisit only if a real content glyph needs coverage.
- *Silent substitution* — rejected: violates FR-001 disclosure.

## R4 — Real overlay pass in `renderTree` (DECIDED)

**Decision**: Add a **deferred overlay pass** to `Control.renderTree`: in-flow nodes paint first, then nodes
marked as overlay/transient (built on the existing `Overlay` container, `Control.fsi:506`) paint last at
their true coordinates, above siblings. Hit-testing (`nearestAuthored`) respects the z-order so the topmost
overlay wins. Transient control surfaces (combo-box, auto-complete, date-picker dropdowns, menus) emit their
open surface into this layer rather than in-flow.

**Rationale**: The renderer is a single in-flow pass (painter's algorithm by document order), so an open
dropdown is overprinted by whatever follows it. FR-005 requires transient surfaces to render distinctly above
neighbours; a z-top pass is the correct, reusable fix (user-selected over the minimal sample workaround). The
`Overlay` container already exists, limiting greenfield surface.

**Alternatives considered**:
- *Reserve in-flow space + clip in the sample* — rejected by user: per-sample workaround, does not fix the
  framework for other consumers, cannot represent true floating surfaces.

**Surface impact**: new/clarified public entry on `Control` for the overlay layer; `.fsi` + surface baseline
updated (Tier 1).

## R5 — Composite-control structure fixes (DECIDED)

**Decision (per control)**:
- **data-grid** — map `data-grid-row` and `data-grid-header` to `Row` in `directionOf` (`Control.fs:1901`);
  share a column-width track so header and body cells align (table structure, FR-006).
- **menu / context-menu / combo rows** — `rowsGeom` uses `rowH = max(minRowHeight, box.Height/n)` and grows
  or scrolls/clips when items exceed the box; no two items share a baseline (FR-005).
- **descriptions** — respect `box.Height`: scale spacing or truncate-with-affordance instead of fixed
  `16+i*22`; never paint past the box (FR-007).
- **qr-code** — enforce a minimum module-grid size and clip to box so a non-empty payload always shows a
  populated grid (FR-007).
- **charts** — wrap chart geometry in `Scene.clipped (RectClip box)` and guard degenerate data
  (`n=0`/NaN/Inf) so bodies stay inside the box (FR-008).

**Rationale**: Each is a localized control-geometry defect with a localized, framework-level fix; all are
theme-invariant.

**Alternatives considered**: deferring data-grid/QR to sample composition — rejected: the defects reproduce
from the control geometry itself (FR-011 → framework).

## R6 — Layout bounds, clipping, and scroll (DECIDED)

**Decision**:
- **Container clipping** — `paintNode` clips container children to the container bounds (`Scene.clipped`),
  so children no longer paint past their parent (right-edge spill, nav-label bleed) (FR-004/FR-009).
- **Flex distribution** — the main-axis split honours explicit basis/weight instead of dividing space
  uniformly across children (`Layout.fs:~272`), so chrome regions (app bar/nav/content/feedback/status) take
  their declared sizes (FR-003).
- **Scroll** — `ScrollViewer` becomes a real clipping viewport: it clips content to its box and exposes a
  scroll offset + affordance; content taller than the viewport is clipped, not spilled (FR-009/FR-010).
- **Sample Shell** — `Shell.fs` assigns explicit region sizes (app bar / feedback / status), a fixed
  nav-rail width, and content flex-grow + scroll. *This is the only sample-level remediation.*

**Rationale**: Clipping, flex correctness, and real scroll are framework deficiencies benefiting all
consumers (FR-011 → framework); the specific chrome sizes are pure composition (→ sample).

**Alternatives considered**: clip everything at the sample only — rejected: leaves the framework spill defect
for other consumers.

## R7 — Re-baseline & disclosure strategy (DECIDED)

**Decision**: Treat all renderer/control output changes as **intended correctness fixes** under FR-012/SC-007.
Re-establish: (a) G1 Controls Gallery and G2 Sample Apps golden evidence; (b) the rendered-output drift gate
baseline; (c) surface-area baselines for every public module whose `.fsi` gains surface (font registry,
overlay-pass entry, measurement seam). Commit a **rebaseline ledger** (`contracts/rebaseline-ledger.md`)
listing each changed baseline, the defect/fix that caused it, and before/after disclosure. Per
constitution memory `surface-baseline-gaps`, also close the two latent drift-gate holes if touched
(`FS.GG.UI.Color` unguarded; missing `readiness/surface-baselines/`).

**Rationale**: A Tier-1 renderer change necessarily moves golden/drift baselines; the constitution requires
the change be intended, re-baselined, and disclosed — never silently green.

**Alternatives considered**: gating fixes to avoid touching baselines — rejected: impossible for a real
rendering correctness fix and contrary to FR-012.

## R8 — Verification harness (DECIDED)

**Decision**: Reuse the feature-135 19-page screenshot-evidence harness as the verification vehicle (no new
evidence mechanism — spec Assumption). Add framework-level semantic tests in `tests/` for each defect class
(glyph correctness incl. the specific `@`/`—`/`#`/`▸`/`·` cases; measure/advance agreement; no item
overprint; data-grid table structure; region non-overlap; clipped scroll; overlay z-order). Verification runs
both themes (antLight + antDark). Re-capture all 19 pages and confirm zero instances of the seven defect
classes (FR-013/SC-001..005). GL screenshots where available; disclosed degrade on no-GL.

**Re-capture command** (feature-135 harness):
```bash
cd samples/AntShowcase
dotnet run --project AntShowcase.App -c Release -- evidence --seed 1            # all 19 pages → artifacts/ant-showcase/1/<page-id>/
```

**Rationale**: Reusing the established deterministic harness keeps verification aligned with how the defects
were found and satisfies SC-005's before/after reviewer comparison.

**Alternatives considered**: a new evidence mechanism — rejected by spec Assumption (reuse the 135 harness).

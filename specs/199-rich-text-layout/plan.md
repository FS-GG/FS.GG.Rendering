# Implementation Plan: Symbology Full Rich-Text Layout (paragraph layout + decoration/slant/tracking)

**Branch**: `199-rich-text-layout` | **Date**: 2026-06-26 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/199-rich-text-layout/spec.md`

**Source design**: the symbology **M0–M7 roadmap** is complete; this is the next **deferred M7 backlog item**, and the direct continuation of the 196→197→198 label chain. Spec 196 ([label / glyph-text channel](../196-symbology-label-text/spec.md)) shipped a single-line label; spec 197 ([multi-line / paragraph label](../197-symbology-multiline-label/spec.md)) widened it to several lines; spec 198 ([rich-text label runs](../198-symbology-rich-text-label/spec.md)) let the label carry **styled runs** with per-run **colour / weight / size** (`LabelText.Rich of LabelRun list`) — and explicitly deferred (198 FR-018) the two things this feature delivers: **full rich-text layout** (per-paragraph alignment incl. justification + explicit paragraph/line structure) and the **typographic run attributes beyond colour/weight/size** (italic/slant, underline, strike-through, letter-spacing/tracking).

## Summary

Complete the rich-text label as a **bounded completion** of the existing channel — not a new subsystem. Two layered additions to the label introduced by 196–198:

1. **Per-run typography (US1 / P1).** Each `LabelRun` gains four optional attributes on top of colour/weight/size — **`Italic`** (synthetic slant), **`Underline`**, **`Strike`**, **`Tracking`** (letter-spacing) — each `None`-defaulted so an all-default run reproduces the spec-198 run **byte-for-byte**. They ride the **existing** `LabelText.Rich` path: the run-emission already proven in 198 (`richLabelNodes`) is extended so a set attribute *adds* a sheared transform / decoration line / tracked advance, and an unset attribute *adds nothing* (structural zero-drift).

2. **Paragraph layout (US2 / P2).** A new `LabelText.Laid of LabelParagraph list` case carries **explicit paragraphs**, each with its own **alignment** — **`Leading | Center | Trailing | Justify`** (new `LabelAlign` type). Alignment operates **inside** the existing per-grammar label region using the **same** per-run fit machinery (wrap / per-run shrink-to-floor (`fitLabelW`, floor `0.62×`) / cap / ellipsis / max-height line / common baseline) proven in 197/198. **Default alignment is `Center`** — exactly the 198 flow — so a `Laid` label of one `Center` paragraph with all-default runs lays out **byte-identically** to the equivalent `Rich`/`Plain`/no-label symbol (layered zero drift). `Justify` distributes measured inter-word space to fill the region width, leaving the **last line of each paragraph** (and any single-token line) un-justified (falling back to the paragraph's base alignment).

Everything stays in the **pure scene-only layer** (FR-018): slant is a baseline-pivoted shear via the existing `Scene.withPerspective`; underline/strike are existing `Scene.line` nodes following each drawn run fragment's fitted geometry; tracking is realised by per-glyph `Scene.glyphRunProof` advances and folded into per-run **measurement** so it never pushes the block past the region. **No new scene primitive, no new font file, no GPU/compute path** (FR-018/FR-019). It is **deterministic** under a fixed measurement provider (FR-011), **tofu-free** at the render edge per run (FR-006) — slant/decoration/tracking keep real glyphs — and **opt-in / layered-additive** (FR-004): unused, every byte matches 198 → 197 → the pre-feature symbol.

Technical approach (grounded against the post-198 tree on 2026-06-26 — `src/Symbology/Symbology.fs`, `src/Scene/`):

- **Surface delta — Tier 1, anticipated by the spec (FR-017).** `Symbology.fsi` gains: four optional fields on `LabelRun` (`Italic: bool option`, `Underline: bool option`, `Strike: bool option`, `Tracking: float option`); a `LabelAlign` DU (`Leading | Center | Trailing | Justify`); a `LabelParagraph` record (`{ Runs: LabelRun list; Align: LabelAlign }`); a new `LabelText.Laid of LabelParagraph list` case; and convenience constructors (`paragraph`, `align`, `laidLabel`). The existing `Plain`/`Rich` cases and every other public type/val keep their **byte-stable** signatures. Per Constitution II every new public type/field is declared in the `.fsi`. Only the **symbology** surface baseline moves (FR-017); zero drift elsewhere.

- **Per-run attributes ride the existing `Rich` path (FR-003, US1).** `RunStyle`/`resolveStyle` (`Symbology.fs:444`) gain the four resolved attributes (defaults: upright, no decoration, `Tracking = 0.0`). In `richLabelNodes`' per-segment emission (`Symbology.fs:539`):
  - **Slant**: a set `Italic` wraps the segment's glyph node in `Scene.withPerspective` with a baseline-pivoted horizontal shear (`M11=1; M12=slant; M13=-slant*baselineY; M22=1; M33=1`, else identity), so the baseline stays fixed and glyphs lean — synthetic italic with **real glyphs** (tofu-free), reusing a primitive the renderer already supports (FR-018/FR-019). Unset ⇒ no wrapper node (zero drift).
  - **Underline / strike**: a set attribute appends a `Scene.line` (stroked in the run's colour, thickness derived from the run size) spanning the drawn segment's fitted width — underline below the baseline, strike at mid-x-height — **per drawn line fragment** of a wrapped run (FR-008), clamped to the segment's fitted extent so it never runs past the region or a clipped glyph. Unset ⇒ no line node.
  - **Tracking**: folded into a tracking-aware width used by break/justify/fit (`trackedWidth = baseWidth + tracking*(glyphs-1)`), and drawn by emitting one `Scene.glyphRunProof` per character advanced by `charWidth + tracking` (per-glyph **positioning** for letter-spacing — a layout mechanic, not per-glyph *styling*, which stays out of scope FR-019). `Tracking = 0`/unset ⇒ the existing single-node emission (zero drift).
  An all-default run hits none of these branches and emits the **exact** 198 node (FR-004).

- **Paragraph layout is a new case reusing the proven fit (FR-001/FR-002/FR-007, US2).** `LabelText.Laid paras` runs the 197/198 pipeline **per paragraph** — atomise → greedy break (tracking-aware) → per-line shrink-to-floor + ellipsis fit (`fitLabelW`, floor `0.62×`) → cap to the per-grammar budget shared across paragraphs → per-line max-height/common-baseline — then **places** each line by the paragraph's alignment instead of the hard-coded centre:
  - region span from `centerX ± regionWidth/2`; `Leading` → left edge, `Center` → `centerX - total/2` (**the 198 placement verbatim**), `Trailing` → right edge - total.
  - `Justify` on a line that is **not** its paragraph's last line **and** has ≥2 inter-word gaps: distribute `(regionWidth - total)` evenly across the gaps (advance `x` by `segW + spaceW + extraPerGap`); the **last line of each paragraph** and any **single-token line** fall back to the paragraph's base alignment (FR-008) — never a stretched final line, never a stretched glyph, never a clip.
  - paragraphs stack downward by the running line height; the **first line of the first paragraph** is anchored at the spec-197/198 first-line baseline (the zero-drift anchor).

- **Structural, layered zero-drift dispatch (FR-004/SC-003).** `labelDispatch` (`Symbology.fs:588`) extends to:
  | Label | Path | Guarantee |
  |---|---|---|
  | `None` | `[]` | byte-identical to the pre-feature symbol |
  | `Some (Plain s)` | unchanged 197 `wrapLabel`/`labelNodes` | byte-identical to spec 197 |
  | `Some (Rich runs)`, all runs default | join → `Plain` path | byte-identical to plain |
  | `Some (Rich runs)`, any styled run (incl. new attrs) | `richLabelNodes` | 198 + new per-run typography |
  | `Some (Laid [{ Runs; Align=Center }])`, single para, all-default runs | reduce to the `Rich`/`Plain` path | **byte-identical to 198** (default = 198 flow) |
  | `Some (Laid paras)`, any non-default alignment / >1 paragraph / styled run | new `laidLabelNodes` | the new paragraph layout |
  `isDefaultRun` (`Symbology.fs:434`) is widened to also require `Italic`/`Underline`/`Strike` unset-or-false and `Tracking` unset-or-`0.0`, so the all-default join-to-`Plain` and the single-`Center`-paragraph reduction stay byte-clean.

- **Tofu-free is a render-edge property, per styled run, unchanged (FR-006/FR-012).** Every drawn glyph is still a `Scene.glyphRunProof` carrying per-glyph `Missing`/`FallbackMode`; slant wraps it (glyphs unchanged), decoration is a non-text `line`, tracking splits it into per-char proofs (each still carrying `Missing`). The pure library **never installs and never requires a measurer** and never throws without one. A render-bridge test rasterises a fully laid-out / decorated label through `Symbology.Render` under the real measurer and asserts every run is non-tofu.

- **Boards + linter unchanged (FR-013/FR-014).** `render`/`gallery`/`galleryIn`/`filmstrip`/`filmstripIn`/`animate`/`animateIn` keep their signatures (they thread the whole `Token`), so a laid-out roster renders per grammar by construction. The legibility linter does **not** read `Token.Label` (verified post-198: no `Label` reference in `Legibility.fs`), so its verdict stays grammar-independent and unchanged by however the label is laid out or decorated — a test asserts layout/decoration does not alter a roster's `Report`.

- **Author-supplied, guidance-governed (FR-015).** Alignment, decoration, and run colours come from the existing scene/label vocabulary and are **not** re-mapped or rejected at runtime; the pre-attentive channels remain the faction/state palettes, the label stays inspection-detail, and "don't impersonate the pre-attentive encodings or crowd the region / over-justify" is a **skill caveat**, not a runtime rule.

- **Loop docs (FR-020).** The `fs-gg-symbology` skill documents full rich-text layout — alignment / justification / explicit breaks / decoration, the run attributes (italic/underline/strike/tracking on top of colour/weight/size), that they require the real measurer for tofu-free output, keep paragraphs short + a restrained alignment/decoration set, don't impersonate faction/state encodings or crowd the region, how surplus degrades, complements (never replaces) the sigil — authored canonically in `src/Symbology/skill/SKILL.md`, mirrored, passing `scripts/check-agent-skill-parity.fsx`.

> **Standing assumption — behaviour is unverified until exercised.**
> This is a *greenfield-additive* completion, not a defect fix, so there is no root-cause map. The layout
> is pure scene logic with **no GL/raster/IO**, fully exercisable headlessly; its *tofu-free rendering per
> run* (incl. sheared/tracked/decorated runs) is a render-edge property verified by a **real**
> render-bridge test (rasterise a laid-out, decorated, justified token through `Symbology.Render` under the
> installed measurer and assert every run is non-tofu), not assumed from this plan. The analogue of the
> live-smoke mandate is an **early FSI/test smoke** (Foundational phase): once the new `LabelRun` fields,
> `LabelAlign`/`LabelParagraph`/`LabelText.Laid`, the extended `richLabelNodes`, and `laidLabelNodes`
> exist, load the public surface in FSI and confirm an all-default `Rich` run is byte-identical to 198, a
> single `Center` all-default paragraph is byte-identical to the equivalent `Rich`, a run that sets
> italic/underline/strike/tracking differs from the same characters without it, `Leading`/`Trailing`
> place lines off-centre, `Justify` fills wrapped lines while leaving the last line un-justified, an
> over-budget laid-out label caps+ellipsises, and a degenerate laid-out token returns the placeholder
> without throwing — before building out US1/US2/US3. Treat that smoke — and the render-bridge tofu
> test — not this plan's narrative, as the confirmation the channel works.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution Engineering Constraints).

**Primary Dependencies** (existing, consumed via public types only): the `Token`/`Grammar`/`LabelText`/`LabelRun` types of `FS.GG.UI.Symbology` (`src/Symbology/Symbology.fsi`); transitively `FS.GG.UI.Scene` for `Color` (`Types.fsi:9`), `FontSpec` (`{ Family; Size; Weight: int option }`, `Types.fsi:168` — **no italic/slant or tracking field**, confirming both are synthesised in the pure layer), `TextMetrics` (`Types.fsi:181`), `PerspectiveTransform` (3×3 affine, `Types.fsi:144`) consumed via `Scene.withPerspective` (`Scene.fsi:148`) for synthetic slant, `Scene.line` (`Scene.fsi:90`) + `Paint.stroke` (`Scene.fsi:22`) for underline/strike, `Scene.glyphRunProof`/`Scene.measureTextResolved` (`Scene.fsi:123/138`) already used by the grammars. The headless render bridge `FS.GG.UI.Symbology.Render` (`Render.toPng`) and the real measurer `SkiaViewer.Fonts` installs are reused **as-is** for the tofu-free render test. **No new third-party dependency, no new font files** (FR-018/FR-019).

**Storage**: None. The label performs no IO; its entire output is part of the returned `Scene` value (FR-011/FR-018).

**Testing**: Expecto + FsCheck, matching `tests/Symbology.Tests/`. Existing channel-presence / determinism / placeholder / gallery / linter / label / multi-line / rich-text batteries stay green (the new `LabelRun` fields are additive — fixtures built via `Symbology.run` + `with`-copy are unaffected; any raw `LabelRun` record literal gains the four new `None` fields, value-preserving). New batteries: a **per-run typography** battery (italic/underline/strike/tracking presence + all-default ≡ 198 byte-identity + tracking-in-measurement); a **paragraph-layout** battery (each alignment places lines correctly; justify fills wrapped lines + last line un-justified + single-token fallback; explicit paragraphs/breaks; default `Center`/single-para ≡ 198; cap+ellipsis under every alignment; decoration follows wrapped geometry); a **render-bridge tofu test** (rasterise a laid-out/decorated/justified label, every run non-tofu); a **linter-invariance test** (layout/decoration does not change a roster's `Report`); and a new pinned laid-out cross-process golden.

**Target Platform**: Linux/CI headless. Layout is pure CPU (no GL); the tofu-free render test runs through the existing `Symbology.Render` bridge (real measurer installed at the edge). Fully reproducible across processes under a fixed measurement provider (SC-004).

**Project Type**: Multi-project F# solution (`FS.GG.Rendering.slnx`). The change is **internal to the existing `src/Symbology/` library** plus a curated **`.fsi` surface addition** (new `LabelRun` fields, `LabelAlign`/`LabelParagraph` types, the `Laid` case, ctors); extended tests in existing test projects. No new project, no new sample (the existing `gallery`/`galleryIn` boards already render a laid-out roster).

**Performance Goals**: Design-time/review tool, not a render hot path. A laid-out label adds `O(words)` measurement during break/justify + ≤ (budget × segments-per-line) nodes (plus per-decoration `line` nodes and, for tracked runs, per-char nodes) per unit; a board is `O(N)`. No fps/throughput guarantee is asserted (parity with the existing grammars).

**Constraints**: **Layered zero-drift is the hard constraint** (FR-004/SC-003) — a no-label token, a `Plain` label, an all-default `Rich` run, **and a single `Center` all-default `Laid` paragraph** are byte-identical to the spec-198/197/196/pre-feature goldens; **default alignment = `Center` reproduces the 198 flow byte-for-byte**. **Provider-relative determinism** (FR-011/SC-004) — identical laid-out `Token` under a fixed measurement provider ⇒ byte-identical scene; break/justify/alignment/decoration geometry are deterministic functions of the resolved per-run measurement; no wall-clock, randomness, or IO. **Tofu-free at the render edge, never required by the pure library, per run** (FR-006/FR-012). **Fitted, capped, no clip/overflow, mixed-size lines sized to tallest run, decoration confined to drawn geometry, last paragraph line un-justified** (FR-007/FR-008/SC-005). **Pure scene-only, no new primitive/font/GPU path** (FR-018/FR-019). **Non-overlapping siting** (FR-005). **Surface baseline moves only for the symbology package** (FR-017); **grammar-independent linter unchanged** (FR-014/SC-006); **author colours/alignment/decoration, guidance-only governance** (FR-015).

**Scale/Scope**: M7 backlog full-rich-text-layout thread only. `Symbology.fsi` gains four `LabelRun` fields, `LabelAlign`, `LabelParagraph`, the `Laid` case, and ctors; `Symbology.fs` gains the resolved decoration/slant/tracking in `RunStyle`/`resolveStyle`, the slant-wrap / decoration-line / tracked-advance emission inside `richLabelNodes`, a tracking-aware measure, an `alignPlace` helper (leading/centre/trailing/justify), and `laidLabelNodes` (per-paragraph layout), plus the dispatch extension (~1 edited source `.fs` + its `.fsi`). Tests: additive batteries + a new render-bridge tofu case + a linter-invariance assertion + a new laid-out cross-process golden + the mirrored skill full-rich-text doc. The symbology surface baseline is regenerated (FR-017). Deferred (FR-019): inline images, hyperlinks, lists, per-glyph styling, per-run font family, auto-label-from-stats, label-bound motion, advanced bidi, new GPU/compute path, new font files.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence in this plan |
|---|---|---|
| **I. Spec → FSI → Semantic Tests → Implementation** | PASS | The `.fsi` is edited **first** (new `LabelRun` fields, `LabelAlign`/`LabelParagraph`, the `Laid` case, ctors), then semantic Expecto tests over the public `token`/`badge`/`ring`/`render` surface (not internals) fail-before/pass-after, then the `.fs` body. The surface delta is intentional and curated (FR-017). |
| **II. Visibility in `.fsi`, not `.fs`** | PASS | Every new public type/field/ctor is declared in `Symbology.fsi`; the internal `laidLabelNodes`/`alignPlace`/tracking-aware measure/slant-wrap keep the existing `let private` style and are omitted from the `.fsi`. The symbology surface baseline is regenerated (FR-017); no other baseline moves. |
| **III. Idiomatic Simplicity** | PASS | Four optional fields on an existing record, a 4-case alignment DU, a 2-field paragraph record, and one new case + layout function composing the **existing** `measureTextResolved`/`fitLabel`/`glyphRunProof`/`line`/`withPerspective` primitives. Optional attributes with `Option.defaultValue` defaults; justify/align are pure folds over measured widths (no `mutable`, mirroring the existing `breakLines`). No SRTP, reflection, type providers, custom operators, or non-trivial CE. The new `Laid` case is justified (Principle III) by the need to keep the `Plain`/`Rich` zero-drift paths **structurally** byte-stable rather than retyping `Rich`. |
| **IV. Elmish/MVU boundary** | PASS (N/A) | The label is **pure, stateless, IO-free** (`Token -> Scene`); no multi-step state, IO, retries, or background work, so the MVU obligation does not attach. Provider-relative purity is itself the contract the tests assert (SC-004). The render-edge measurer seam already exists and is reused, not introduced. |
| **V. Test Evidence Mandatory** | PASS | Expecto semantic tests over the public surface, fail-before/pass-after; all **real** (pure scene logic). Tofu-free claim verified by a **real** render-bridge raster test through `Symbology.Render` under the real measurer (laid-out, decorated, justified label; every run non-tofu). Determinism via `SceneCodec.export(...).CanonicalBytes`; layered zero-drift via the pinned 198/197/196/pre-feature goldens (kept green — the new fields are additive `None` defaults; default `Center`/single-para reduces to the 198 path). **No existing assertion weakened or deleted**; new behaviour gets added assertions, each at least as strong (every drawn segment ≤ region, decoration ≤ segment extent, last paragraph line un-justified, every run non-tofu). |
| **VI. Observability & Safe Failure** | PASS | Safe failure *as a visible placeholder*: a degenerate (`R <= 0`) laid-out/decorated `Token` renders the placeholder and never throws (FR-010/SC-005); empty/whitespace/empty-run labels degrade to no-label without throwing (FR-009) regardless of alignment/decoration. Tofu, if the edge ever lacked coverage, is **disclosed** per run via the glyph run's `Missing` flag, never silently drawn as a plausible-wrong glyph. |
| **Change Classification** | **Tier 1** | **Alters observable behaviour covered by existing specs (the label channel) AND adds public surface.** Tier 1 by both clauses. The `Symbology.fsi` surface baseline is regenerated (FR-017) with zero drift elsewhere; the discipline (spec, plan, FSI-first, semantic tests, docs/skill, zero-drift goldens for no-label / plain / all-default-run / default-`Center`-paragraph input) mirrors spec 198. |
| **Engineering Constraints** | PASS | `net10.0`; F#-only; change internal to the existing **pure** package plus its curated `.fsi`, referencing only its own `Token`/`LabelText` types + already-referenced `FS.GG.UI.Scene` `Color`/`FontSpec`/`PerspectiveTransform`/text+line vocabulary; **no new dependency, no new font files, no new scene primitive, no GPU/compute path** (FR-018/FR-019); baselines maintained (only the symbology surface moves, recorded); `FS.GG.UI.*` identity untouched; SkiaSharp/GL backend untouched (slant/decoration ride the existing transform/line/text seams). **No control fork**: symbology vocabulary, not a per-theme control copy. |

**Gate result: PASS** — no violations; Complexity Tracking left empty. Tier 1 by **both** the behavioural and surface clauses; the `Symbology.fsi` baseline intentionally moves and is regenerated (FR-017), mirroring spec 198's honest surface-delta note.

## Project Structure

### Documentation (this feature)

```text
specs/199-rich-text-layout/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — field-shape decisions (LabelRun typography fields; LabelAlign +
│                        #   LabelParagraph + LabelText.Laid case), slant/decoration/tracking realisation
│                        #   in the pure scene layer, alignment + justification algorithm, zero-drift
│                        #   dispatch extension, render-edge tofu path, test-battery shape
├── data-model.md        # Phase 1 — the new/extended types; the extended dispatch table; per-run
│                        #   resolution incl. decoration/slant/tracking; alignment placement; per-grammar
│                        #   budgets (reused); contract vs design-loop split
├── quickstart.md        # Phase 1 — build + FSI smoke + run tests + per-SC validation + surface baseline regen
├── contracts/           # Phase 1 — the full-rich-text-layout contract (the .fsi delta + behaviour table)
│   └── symbology-rich-text-layout-api.md
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/Symbology/                       # EXISTING pure package FS.GG.UI.Symbology (Scene-only)
├── Symbology.fsi                    #   EDIT — ADD 4 optional fields on `LabelRun`
│                                    #     (`Italic`/`Underline`/`Strike`/`Tracking`, all `… option`);
│                                    #     ADD `type LabelAlign = Leading | Center | Trailing | Justify`;
│                                    #     ADD `type LabelParagraph = { Runs: LabelRun list; Align: LabelAlign }`;
│                                    #     ADD `LabelText.Laid of LabelParagraph list` case;
│                                    #     ADD ctors `paragraph`/`align`/`laidLabel`. Plain/Rich byte-stable.
├── Symbology.fs                     #   EDIT — extend `RunStyle`/`resolveStyle` with the 4 attrs (defaults
│                                    #     upright / no-decoration / tracking 0); tracking-aware width;
│                                    #     in `richLabelNodes` per-segment emit: slant via `withPerspective`
│                                    #     baseline shear, underline/strike via `Scene.line` per drawn fragment,
│                                    #     tracking via per-char `glyphRunProof` advance (all gated on the attr,
│                                    #     zero drift when unset); `alignPlace` (leading/centre/trailing/justify,
│                                    #     last-paragraph-line + single-token fallback); `laidLabelNodes`
│                                    #     (per-paragraph break→cap→max-height→aligned place); extend
│                                    #     `isDefaultRun` (also require new attrs unset/false/0) and
│                                    #     `labelDispatch`: None→[]; Plain→197; Rich(all-default)→Plain;
│                                    #     Rich(styled)→richLabelNodes; Laid(single Center all-default)→Rich/Plain;
│                                    #     Laid(else)→laidLabelNodes. Budgets reused (Token≤3, Badge≤2, Ring≤2)
├── Legibility.fsi / Legibility.fs   #   UNCHANGED (does NOT read Token.Label; label stays inspection-detail)
└── skill/SKILL.md                   #   EDIT — full-rich-text section: alignment/justify/breaks/decoration,
                                     #     attrs italic/underline/strike/tracking, keep paragraphs short +
                                     #     restrained set, don't impersonate faction/state or crowd/over-justify,
                                     #     requires real measurer for tofu-free, complements the sigil

tests/
├── Symbology.Tests/                 # EXISTING — additive (new LabelRun fields default None; default
│   │                                #   Center/single-para reduces to 198 path; existing goldens stay green)
│   ├── ChannelPresenceTests.fs      #   EDIT — ADD: same chars w/ vs w/o a new attr (italic/tracking) ⇒ differing
│   │                                #     bytes; same runs under different alignment ⇒ differing bytes
│   ├── DeterminismTests.fs          #   EDIT — ADD laid-out render-twice byte-equal + NEW pinned laid-out
│   │                                #     cross-process golden; existing 198/197/196 goldens UNCHANGED
│   ├── PlaceholderTests.fs          #   EDIT — ADD degenerate token WITH a laid-out/decorated label → placeholder
│   ├── GalleryTests.fs              #   EDIT — ADD laid-out roster reproducible per grammar (FR-013)
│   ├── LegibilityTests.fs           #   EDIT — ADD layout/decoration presence does NOT change a roster's Report
│   ├── RichLabelTests.fs            #   EDIT — ADD per-run typography battery: italic/underline/strike/tracking
│   │                                #     presence; all-default (incl. new attrs) ≡ 198 byte-identity; tracking
│   │                                #     folded into measurement (widens wrap/fit); decoration follows wrapped
│   │                                #     run geometry per line fragment
│   └── (new) LaidLabelTests.fs      #   NEW — paragraph-layout battery: each alignment places lines (centre
│                                    #     centred, trailing right, leading left); justify fills wrapped lines +
│                                    #     last paragraph line un-justified + single-token fallback; explicit
│                                    #     paragraphs/breaks; default Center single-para ≡ equivalent Rich;
│                                    #     cap+ellipsis under every alignment; each segment ≤ region; register in .fsproj
└── Symbology.Render.Tests/          # EXISTING — extend RenderLabelTests.fs: rasterise a LAID-OUT (justified,
                                     #     multi-paragraph) + DECORATED (italic/underline/strike/tracking) labelled
                                     #     token through Render.toPng under the real measurer; assert EVERY run is
                                     #     non-tofu (TofuCount = 0) and the board is non-blank (FR-006)

readiness/surface-baselines/
└── FS.GG.UI.Symbology.*            # REGEN — the symbology surface baseline moves (new LabelRun fields,
                                     #   LabelAlign/LabelParagraph, Laid case, ctors); EVERY OTHER package
                                     #   baseline UNCHANGED (FR-017, recorded)

.claude/skills/fs-gg-symbology/      # pointer wrapper -> canonical (inherits the full-rich-text-doc edit)
.agents/skills/fs-gg-symbology/      # pointer wrapper -> canonical (inherits the full-rich-text-doc edit)
template/product-skills/fs-gg-symbology/SKILL.md   # EDIT — mirror the full-rich-text-doc update (adapted copy)

FS.GG.Rendering.slnx                 # no new project (change lands in existing Symbology.fsproj)
```

**Structure Decision**: Full rich-text layout is a **layered enrichment of the single existing label channel** inside `src/Symbology/` — **not** a new channel, project, or markup grammar. Two additions, each chosen to keep the prior zero-drift paths **structurally** byte-stable:

1. **Per-run typography rides the existing `Rich` path** by adding four `None`-defaulted fields to `LabelRun` (rather than a parallel "decorated run" type), so the 198 run-emission is extended in place and an all-default run is byte-identical to 198. Slant/decoration/tracking are synthesised from primitives the renderer **already** supports (`withPerspective` shear, `line`, per-glyph `glyphRunProof`) — honouring FR-018's "no new scene primitive / font / GPU path."

2. **Paragraph layout is a new `LabelText.Laid` case** (a `LabelParagraph list`, each paragraph carrying a `LabelAlign`) **rather than retyping `Rich of LabelRun list`**. Retyping `Rich` would break every 198 `Rich [...]` call site and risk golden drift; a new case keeps `Plain`/`Rich` verbatim and makes the **default-`Center`-single-paragraph ≡ 198** path a structural reduction, exactly as 198 made `Plain`/all-default-`Rich` structural. Alignment is **per paragraph** because the spec scopes it there (FR-001/FR-002); `Center` is the default because that is literally the 198 flow.

The board/motion functions need **no signature change** (FR-013) — they thread the whole `Token`. The cost — a curated `.fsi` surface delta + additive (not value-changing) fixture touch-ups — is anticipated by the spec (FR-017) and confined to the symbology package (the only `Token.Label` consumer). Tier 1 by both clauses; the symbology surface baseline is regenerated and recorded (FR-017).

## Complexity Tracking

> No constitution violations. Section intentionally empty.

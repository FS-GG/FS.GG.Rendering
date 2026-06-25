# Phase 0 Research: Symbology Full Rich-Text Layout

**Feature**: 199-rich-text-layout | **Date**: 2026-06-26

Resolves every "design-loop detail" the spec deferred to planning (spec Assumptions). Each item is a
**Decision / Rationale / Alternatives** triple, grounded against the post-198 tree on 2026-06-26
(`src/Symbology/Symbology.fs`, `src/Scene/Types.fsi`, `src/Scene/Scene.fsi`).

---

## R1 — How are the per-run typographic attributes expressed?

**Decision**: Add **four optional fields** to the existing `LabelRun` record, each `None`-defaulted to "no change":
```fsharp
type LabelRun =
    { Text: string
      Color: Color option
      Weight: int option
      Scale: float option
      Italic: bool option       // None ⇒ upright
      Underline: bool option    // None ⇒ no underline
      Strike: bool option       // None ⇒ no strike-through
      Tracking: float option }   // None ⇒ 0.0 letter-spacing (em-fraction of the run's resolved size)
```
Authors set them by record-copy on a `run`, e.g. `{ Symbology.run "quoted" with Italic = Some true }`.

**Rationale**: The attributes are **per-run**, exactly like colour/weight/size, so they belong on `LabelRun`. Optional + `None`-defaulted means an all-default run resolves byte-identically to the 198 run (FR-004 structural zero-drift) and existing `Symbology.run`-based fixtures keep compiling unchanged. `Tracking` is a float (em-fraction of the resolved size) so it scales with the run and folds cleanly into measurement (R6). `Italic`/`Underline`/`Strike` are `bool option` (not an enum) because each is an independent on/off — the simplest faithful encoding (Constitution III).

**Alternatives considered**:
- *A parallel `DecoratedRun` type or a `Decoration` sub-record* — rejected: a second run shape fractures the one-run model and the 198 emission; flat optional fields keep one path.
- *A `Slant: float option` (arbitrary angle) instead of `Italic: bool`* — rejected: the spec asks for italic (on/off), not a slant ramp; a fixed synthetic slant constant (R3) is simpler and matches "italic". (A future angle is a non-breaking widening if ever needed.)
- *Tracking as absolute px* — rejected: couples to a grammar's pixel size and breaks grammar-independence; an em-fraction multiplier on the resolved size keeps one mapping driving all three grammars.

---

## R2 — How is alignment + explicit paragraph/line structure expressed?

**Decision**: Add a new **`Laid` case** to `LabelText`, carrying a list of **paragraphs**, each with its own alignment — keeping `Plain`/`Rich` byte-stable:
```fsharp
type LabelAlign =
    | Leading
    | Center      // the default — reproduces the spec-198 flow exactly
    | Trailing
    | Justify

type LabelParagraph =
    { Runs: LabelRun list
      Align: LabelAlign }

[<RequireQualifiedAccess>]
type LabelText =
    | Plain of string
    | Rich of LabelRun list
    | Laid of LabelParagraph list   // NEW
```
Convenience ctors: `paragraph : LabelRun list -> LabelParagraph` (`Align = Center`), `align : LabelAlign -> LabelRun list -> LabelParagraph`, `laidLabel : LabelParagraph list -> LabelText` (`= LabelText.Laid`). **Explicit line breaks within a paragraph** reuse the existing `\n`/`\r\n` hard-break handling in `atomsOf` (`Symbology.fs:462`); **paragraph breaks** are the list boundaries (each paragraph starts a new line and may carry its own alignment — FR-002).

**Rationale**: Alignment is **per-paragraph** (FR-001/FR-002), so a paragraph list with a per-paragraph `Align` is the faithful shape. A **new case** (not a retype of `Rich of LabelRun list`) keeps the 198 `Plain`/`Rich` paths and their pinned goldens **verbatim** and makes "default `Center`, single paragraph, all-default runs ≡ 198" a **structural reduction** — the same discipline 198 used to keep `Plain`/all-default-`Rich` byte-clean. `Center` is the default `LabelAlign` because the 198/197 flow centres each line (`Symbology.fs:423`, `:572`); making the default reproduce that flow byte-for-byte is FR-004's hard requirement.

**Alternatives considered**:
- *Retype `Rich of LabelRun list` → `Rich of RichLabel` with paragraphs+alignment inside* — rejected: breaks every `LabelText.Rich [...]` call site, forces a re-migration of 198 fixtures, and risks a 1-byte golden drift in the proven path. A new case is cheaper to prove (Constitution V).
- *An `Align` field on the `Rich` case (no paragraphs)* — rejected: cannot express multiple paragraphs with **different** alignments (FR-002 "each paragraph MAY carry its own alignment"); the paragraph list is the minimal structure that does.
- *A markup string with alignment/break tags* — rejected (same reasons as 198 R1: opaque, needs a parser, not idiomatic, breaks determinism story).
- *A separate vertical-alignment control* — out of scope (spec Assumptions keep the 197/198 vertical siting); horizontal alignment / justification is the deliverable.

---

## R3 — Synthetic slant (italic) in the pure scene layer

**Decision**: `FontSpec` has **no italic/slant field** (`{ Family; Size; Weight: int option }`, `Types.fsi:168`), so italic is **synthetic**: wrap the run segment's `glyphRunProof` node in `Scene.withPerspective` (`Scene.fsi:148`) with a **baseline-pivoted horizontal shear** `PerspectiveTransform`:
```
M11 = 1.0 ; M12 = slant ; M13 = -slant * baselineY
M21 = 0.0 ; M22 = 1.0  ; M23 = 0.0
M31 = 0.0 ; M32 = 0.0  ; M33 = 1.0          (pure affine — no perspective)
```
with a fixed `slant` constant (≈ `0.21`, ~12°). The `-slant * baselineY` term fixes the baseline (`y = baselineY` maps to itself) so the glyphs lean without sliding off the baseline. `Italic = None`/`Some false` ⇒ **no wrapper node** (the exact 198 emission — zero drift).

**Rationale**: Real glyphs sheared by a transform are **tofu-free** (the glyph run is unchanged; only the node's coordinate frame leans), satisfying "synthetic slant where the renderer already supports it — no new font files" (FR-019). `withPerspective` is an **existing** primitive the codec/render already round-trip (`Scene.fs`/`SceneCodec.fs`/`SceneWire.fs`), so no new scene vocabulary (FR-018). A fixed slant constant keeps it deterministic and matches "italic" (on/off, R1).

**Alternatives considered**:
- *Italic via `FontSpec.Family` (selecting an italic face)* — rejected: that is per-run font-family selection, explicitly out of scope (FR-019: no new font files; family choice deferred); and it would depend on the installed faces, breaking determinism across providers.
- *A dedicated skew primitive* — rejected: would be a new scene primitive (FR-018 forbids); the affine `PerspectiveTransform` already expresses shear.

---

## R4 — Underline / strike-through geometry

**Decision**: A set `Underline`/`Strike` appends a `Scene.line` (`Scene.fsi:90`) stroked with the run's resolved colour (`Paint.stroke colour thickness`, `Scene.fsi:22`) **per drawn line fragment** of the run, spanning that fragment's **fitted** width:
- **underline**: `y = baselineY + descentOffset` (a small fraction of the resolved size below the baseline);
- **strike**: `y = baselineY - xHeightMid` (≈ mid-x-height above the baseline);
- thickness ≈ `max 1.0 (resolvedSize * 0.06)`; `x` runs from the segment's drawn start to start + fitted width.

A run that **wraps** is decorated on **each** of its drawn line fragments (FR-008); the line is clamped to the fitted segment extent so it **never** extends past the region or a clipped glyph. Unset ⇒ no line node (zero drift).

**Rationale**: Underline/strike are non-text rules, so a stroked `line` (an existing primitive) is the natural, FR-018-compliant encoding — and a `line` carries no glyphs so it cannot be tofu. Driving the line from the **already-fitted** segment geometry (after `fitLabelW`) guarantees it follows the drawn run and stays within the region (FR-008). Per-fragment decoration falls out of emitting decoration inside the per-line, per-segment loop.

**Alternatives considered**:
- *Decoration as a glyph (combining underline char)* — rejected: shaping-dependent, not deterministic, and could tofu.
- *A filled `rectangle` rule* — viable but `line` + stroke width is the simpler, thickness-controlled primitive already used elsewhere; chosen for simplicity.

---

## R5 — Alignment + justification placement

**Decision**: Replace the hard-coded centre (`startX = centerX - total/2.0`, `Symbology.fs:572`) with an `alignPlace` that, given the region span (`left = centerX - regionWidth/2`, `right = centerX + regionWidth/2`), the line's total measured width `total`, its segments, and whether the line is its paragraph's **last** line, returns each segment's `x`:
- **Leading** → `startX = left`, segments packed left-to-right with the base inter-word space.
- **Center** → `startX = centerX - total/2.0` (**the 198 placement verbatim**).
- **Trailing** → `startX = right - total`.
- **Justify**, when the line is **not** the paragraph's last line **and** has ≥2 inter-word gaps: `extraPerGap = (regionWidth - total) / gaps`; advance `x` by `segW + spaceW + extraPerGap` per gap (no glyph stretch, no clip — FR-008). A justified line that **is** the paragraph's last line, or a **single-token** line with no distributable gap, **falls back to the paragraph's base alignment** for that line (FR-008) — here `Leading` (so it reads as a normal left-aligned tail).

**Rationale**: Alignment is a pure function of measured widths and the region span — deterministic and provider-relative (FR-011). Keeping `Center` exactly the 198 computation is what makes the default-alignment path byte-identical (FR-004). Skipping the paragraph's last line and single-token lines is the standard, faithful justification rule the spec mandates (FR-007/FR-008: "the last line of each paragraph left un-justified"; "a line with no distributable inter-word space MUST fall back to the paragraph's base alignment").

**Alternatives considered**:
- *Justify by stretching glyphs / scaling the font* — rejected (FR-008: "MUST NOT stretch glyphs or clip").
- *Justify the last line too* — rejected (FR-007/Edge cases: stretched final line is the classic justification defect).

---

## R6 — Tracking (letter-spacing) in measurement and drawing

**Decision**: `Tracking` (an em-fraction of the resolved size) is folded into **both** measurement and drawing:
- **Measurement**: a tracking-aware width `trackedWidth weight text size tracking = baseWidth + (size * tracking) * float (max 0 (glyphCount - 1))`, used everywhere break/justify/fit measures a tracked segment, so tracking enters wrapping and the region fit (FR-007: "letter-spacing is included in per-run measurement so tracking never pushes the block past the region").
- **Drawing**: a tracked segment is emitted as **one `Scene.glyphRunProof` per character**, each advanced by `charWidth + size*tracking`. `Tracking = 0`/unset ⇒ the existing **single-node** emission (zero drift), so only a tracked run pays the per-char cost.

**Rationale**: `glyphRunProof`/`measureTextResolved` take **no** tracking parameter, so letter-spacing must be realised by per-character **positioning** in the pure layer — a layout mechanic, not per-glyph *styling* (which stays out of scope, FR-019). Each character is still a `glyphRunProof` carrying its own `Missing` flag, so a tracked run is verified **tofu-free** exactly like an untracked one. `glyphCount` is the character count of the (already whitespace-trimmed) segment; the approximation `+ size*tracking*(n-1)` is exact for the inter-glyph gaps that tracking inserts and is deterministic.

**Alternatives considered**:
- *A `Tracking` field on `FontSpec` consumed at the render edge* — rejected: changes a shared Scene type for one symbology feature, and pushes a measurable layout quantity to the edge (breaking pure-layer wrap/justify). Keeping it in the symbology layer is FR-018-clean.
- *Per-cluster (grapheme) advance instead of per-char* — deferred: opaque-short-string treatment (FR-019: no new shaping/bidi responsibility); per-`char` is deterministic and sufficient for the short Latin-ish codes the channel targets, with non-Latin following whatever the measurer supports.

---

## R7 — Extended dispatch & structural layered zero-drift

**Decision**: `labelDispatch` (`Symbology.fs:588`) extends, and `isDefaultRun` (`:434`) widens:
| Label | Path | Guarantee |
|---|---|---|
| `None` | `[]` | byte-identical to the pre-feature symbol (FR-004) |
| `Some (Plain s)` | unchanged 197 `wrapLabel`/`labelNodes` | byte-identical to spec 197 |
| `Some (Rich runs)`, all runs default | join → `Plain` path | byte-identical to plain |
| `Some (Rich runs)`, any styled run | `richLabelNodes` (now decoration/slant/tracking-aware) | 198 + new per-run typography |
| `Some (Laid [{ Runs; Align = Center }])`, single para, all-default runs | reduce to the `Rich`/`Plain` path | **byte-identical to 198** (default = 198 flow) |
| `Some (Laid paras)`, any non-`Center` alignment / >1 paragraph / styled run | `laidLabelNodes` | new paragraph layout |

`isDefaultRun r` is widened to also require `r.Italic ∈ {None; Some false} && r.Underline ∈ {None; Some false} && r.Strike ∈ {None; Some false} && (r.Tracking = None || r.Tracking = Some 0.0)`, so the all-default-`Rich`→`Plain` join and the single-`Center`-paragraph reduction stay byte-clean.

**Rationale**: Delegating the unstyled / all-default / default-`Center`-single-paragraph cases to the **verbatim** 198/197 code makes SC-003 **structural** rather than incidental — there is no second code path that could drift the pinned goldens. The new attributes only add nodes (slant wrapper / decoration line / per-char tracking) when set, so the 198 emission is a strict subset.

**Alternatives considered**: One unified laid-out path handling the plain/centre cases too — rejected: any refactor of the proven 197/198 emit risks a golden drift; structural delegation is the safe choice the spec's hard zero-drift constraint demands.

---

## R8 — Tofu-free verification at the render edge (per run, incl. slant/decoration/tracking)

**Decision**: Every drawn glyph remains a `Scene.glyphRunProof` carrying per-glyph `Missing`/`FallbackMode`: slant **wraps** the unchanged glyph run, decoration is a non-text `line` (no glyphs), tracking **splits** the run into per-char glyph proofs (each carrying `Missing`). The pure library **never installs and never requires a measurer** and never throws without one (FR-012). A render-bridge test (`tests/Symbology.Render.Tests/RenderLabelTests.fs`) rasterises a **laid-out (justified, multi-paragraph) + decorated (italic/underline/strike/tracking)** labelled token through `Render.toPng` under the real measurer (`SkiaViewer.Fonts`) and asserts **every** run's `TofuCount = 0` and the board is non-blank (FR-006).

**Rationale**: Tofu-free is a property of the installed real measurer, asserted with **real** evidence (Constitution V), not assumed. The slant transform and decoration line do not introduce glyphs that could tofu; tracking preserves the glyph proofs.

**Alternatives considered**: Asserting tofu-free in the pure library — impossible/incorrect: the pure fallback measurer can report `Missing`; tofu-free is the render edge's contract.

---

## R9 — Determinism under a fixed measurement provider

**Decision**: Identical laid-out `Token` ⇒ identical `Scene` ⇒ identical `SceneCodec.export(...).CanonicalBytes`, in-process and cross-process, under a fixed measurement provider. The break, justify, alignment, slant matrix, decoration geometry, and tracked advances are pure functions of the resolved per-run measurement + fixed constants; no wall-clock, randomness, or IO (FR-011/SC-004). A new laid-out cross-process golden is pinned (alongside the unchanged 198/197/196/pre-feature goldens).

**Rationale**: Mirrors the existing determinism contract; the only new inputs (alignment, the four run attributes, the slant constant) are pure data/constants folded into measurement, paint, and transform.

**Alternatives considered**: Cross-provider byte-identity — explicitly **not** promised (provider-relative, FR-011).

---

## R10 — Fixture touch-ups & test-battery shape

**Decision**: Adding four `None`-defaulted fields to `LabelRun` is **additive**: fixtures built via `Symbology.run` + `with`-copy are unaffected; any **raw** `LabelRun` record literal gains the four `None` fields (value-preserving). No existing assertion or pinned golden changes. New coverage:
- extend `RichLabelTests.fs`: per-run italic/underline/strike/tracking presence; all-default (incl. new attrs) ≡ 198 byte-identity; tracking folded into measurement (widens wrap/fit); decoration follows wrapped run geometry per fragment.
- new `LaidLabelTests.fs`: each alignment places lines (centre centred, trailing right-sited, leading left); justify fills wrapped lines + last paragraph line un-justified + single-token fallback; explicit paragraphs/breaks; default `Center` single-para ≡ equivalent `Rich`; cap+ellipsis under every alignment; each drawn segment ≤ region.
- extend `ChannelPresenceTests`/`DeterminismTests`/`PlaceholderTests`/`GalleryTests`/`LegibilityTests` + `RenderLabelTests` as in the plan's structure tree.

**Rationale**: Confines the blast radius (the only `Token.Label` consumer is Symbology), keeps the zero-drift goldens green by construction, and isolates the paragraph-layout behaviour in a new file (Constitution V — existing evidence preserved, new evidence added).

**Alternatives considered**: A migration that rebuilds every fixture through new ctors — unnecessary; the additive `None` fields require no value change.

---

## R11 — Skill documentation (loop guidance)

**Decision**: Extend `src/Symbology/skill/SKILL.md` (and mirror to `template/product-skills/fs-gg-symbology/SKILL.md`) with a full-rich-text section: alignment (leading/centre/trailing/justify) per paragraph, explicit paragraphs/breaks, the run attributes (italic/underline/strike/tracking on top of colour/weight/size); keep paragraphs **short** and the alignment/decoration set **restrained**; **do not** justify or decorate so as to impersonate the faction/state pre-attentive encodings or crowd the region; requires the real measurer for tofu-free output; surplus degrades via wrap→cap→ellipsis under every alignment; complements (never replaces) the vector sigil. Passes `scripts/check-agent-skill-parity.fsx` (`critical=0 high=0`).

**Rationale**: FR-020 + the governance caveat (FR-015) are loop guidance, not runtime rules; the skill is where they live, mirrored for parity (SC-007).

**Alternatives considered**: Enforcing the caveat at runtime — rejected (FR-015: author-supplied alignment/decoration/colours are not re-mapped/rejected; the linter's governance is unchanged).

---

**All NEEDS CLARIFICATION resolved.** No open unknowns remain for Phase 1.

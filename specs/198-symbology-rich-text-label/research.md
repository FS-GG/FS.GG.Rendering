# Phase 0 Research: Symbology Rich-Text Label Runs

**Feature**: 198-symbology-rich-text-label | **Date**: 2026-06-25

Resolves every "design-loop detail" the spec deferred to planning (spec Assumptions). Each item is a
**Decision / Rationale / Alternatives** triple, grounded against the tree on 2026-06-25.

---

## R1 — How is per-run styling expressed in the single label channel?

**Decision**: Retype the existing channel `Token.Label : string option` → **`Token.Label : LabelText option`**, where
```fsharp
type LabelRun =
    { Text: string
      Color: Color option     // None ⇒ default labelInk
      Weight: int option      // None ⇒ default weight (maps onto FontSpec.Weight : int option)
      Scale: float option }    // None ⇒ 1.0 (× grammar base size)
[<RequireQualifiedAccess>]
type LabelText =
    | Plain of string          // unstyled, single- or multi-line via \n — the spec-197 channel verbatim
    | Rich of LabelRun list    // ordered styled runs
```
Convenience constructors in the `Symbology` module: `plainLabel : string -> LabelText` (= `LabelText.Plain`), `run : string -> LabelRun` (default-styled run), `richLabel : LabelRun list -> LabelText`.

**Rationale**: Per-run colour/weight/size cannot live in a bare `string` without inventing markup. The single field is **retyped, not supplemented** — a second `RichLabel` field would be the "second label channel" FR-001 forbids. A 2-case DU keeps the **zero-drift `Plain` path structural** (it re-enters the spec-197 code unchanged) while `Rich` carries the styled runs; `[<RequireQualifiedAccess>]` matches the existing `Grammar` convention (`Grammar.Token`) and avoids `Plain`/`Rich` name collisions. The `LabelRun` attributes are exactly the deferred set (FR-003), each optional with an `Option.defaultValue` default so an all-default run reproduces the uniform style.

**Alternatives considered**:
- *Markup-in-string* (e.g. `"<b>BRAVO</b> ac-12"`) — rejected: opaque, needs a parser, error-prone, not idiomatic F#, and breaks the byte-clean determinism story.
- *Parallel `RichLabel : LabelRun list option` field alongside `Label`* — rejected: a second channel (FR-001), plus ambiguity when both are set.
- *`Token.Label : LabelRun list option` (no DU)* — viable, but loses the explicit, structural `Plain` zero-drift path; would need an "all runs default" collapse heuristic instead of a named case. The DU is clearer and cheaper to prove (Constitution V).
- *`Scale` as an absolute size* — rejected: would couple runs to a grammar's pixel size and break grammar-independence; a multiplier on the per-grammar base size keeps one mapping driving all three grammars (FR-001).

---

## R2 — Inline run line-breaking algorithm

**Decision**: A pure greedy inline break over an **atom stream**:
1. **Atomise**: for each run in order, split `Text` on `\r\n`/`\n` into segments (segment boundaries are **hard breaks**); split each segment on whitespace (`' '`, `'\t'`) into words; drop empty/whitespace-only runs and words (FR-007). Each word carries its run's **resolved style** (R4).
2. **Break**: pack words onto the current line while the running measured width (each word measured in its **own** resolved font via `Scene.measureTextResolved`, plus inter-word spaces) ≤ region width; on overflow start a new line; a hard break forces a new line. Never break inside a word (a too-wide single word becomes its own over-wide line, fitted in R3).
3. **Cap**: truncate to the grammar's per-region **budget** (Token ≤ 3, Badge ≤ 2, Ring ≤ 2 — reused from 197); when lines are dropped, the **last kept line** gains a trailing ellipsis (FR-006/SC-005), re-fitted ≤ region.

Implemented as a fold (no `mutable`), mirroring the existing `wrapSegment` (`Symbology.fs:321`).

**Rationale**: Greedy is the proven, deterministic approach already used for the uniform multi-line wrap (197/R2); extending it to per-word styles only changes the per-word measurement font. Word-atomic breaking guarantees "never clip mid-glyph" at the break level; the per-segment `fitLabel` (R3) guarantees it at the draw level.

**Alternatives considered**: Knuth–Plass / minimum-raggedness line breaking — rejected: overkill for a few short inspection-detail runs, non-trivial, and harder to prove deterministic. Per-glyph breaking — rejected (out of scope, FR-018; and clips mid-word).

---

## R3 — Per-segment fit (no clip / no overflow)

**Decision**: After breaking, each **contiguous same-style segment** on a line passes through the **existing `fitLabel`** (`Symbology.fs:281`) at that segment's resolved size — shrink-toward-floor (`size * 0.62`), then measured ellipsis-truncate at a glyph boundary. This is reused verbatim except it is parameterised by the run's resolved `FontSpec` (weight + scaled size) instead of the default font.

**Rationale**: `fitLabel` is the proven, measurement-verified guarantee that a drawn run is ≤ region width and never cut mid-glyph (197/SC-005). Reusing it per segment extends the same guarantee to styled runs with zero new fit logic. A single word wider than the region (no wrap point) degrades through exactly this path — the latitude the spec grants ("wrap/shrink/truncate", FR-006).

**Alternatives considered**: A new bidi-aware fitter — out of scope (FR-018). Clipping via a Skia clip rect — rejected: clips mid-glyph (violates FR-006) and would be a render-edge concern, not pure-scene.

---

## R4 — Per-run style resolution (colour / weight / size)

**Decision**: A run resolves at a grammar base size `b` to `(Color, FontSpec)`:
- `colour = run.Color |> Option.defaultValue labelInk`  (the spec-196 ink `rgb 235 235 235`).
- `size   = max 1.0 (b * (run.Scale |> Option.defaultValue 1.0))`.
- `font   = { Family = None; Size = size; Weight = run.Weight }`.

The existing `labelFontOf (size)` is generalised to `labelFontWith (weight: int option) (size: float)`; `labelFontWith None size` ≡ today's `{ Family=None; Size=max 1.0 size; Weight=None }` (zero drift on the plain path). The node paint is `Paint.fill colour`.

**Rationale**: Maps the three attributes directly onto the existing `FontSpec` (weight, size) and `Paint.fill` (colour) — no new scene vocabulary (FR-016). Scale as a multiplier on `b` keeps grammar-independence (R1). Defaults recover the uniform style exactly, so an all-default run is byte-identical to plain (FR-002).

**Alternatives considered**: Named weight enum (e.g. `Regular | Bold`) — rejected: `FontSpec.Weight` is already `int option`; a parallel enum would need mapping and adds surface for no gain. Per-run `Family` (font choice) — deferred (FR-016: no new font files; family selection is out of scope this iteration).

---

## R5 — Line geometry: height, baseline, centring, stacking

**Decision**:
- **Line height** = `max` over the line's runs of `lineHeightOf (run resolved size)` (the existing measured `TextMetrics.Height` helper, `Symbology.fs:273`). Mixed sizes/weights ⇒ the tallest run sets the line height (FR-006).
- **Baseline**: runs on a line share a **common baseline**; the **first** line is anchored at spec 197's exact first-line baseline (the zero-drift anchor); subsequent lines stack **downward** by the line height. Screen-aligned (heading never rotates the block).
- **Centring**: each line is centred by its **total measured width** (sum of segment widths, measured in their own fonts) about `centerX` — matching the existing per-line centring (`Symbology.fs:387`).

**Rationale**: A common baseline with max-height spacing is the standard inline-text behaviour and the minimum needed to keep mixed-size runs from overlapping vertically (FR-006). Anchoring the first line at the 197 baseline is what makes the all-default `Rich`/`Plain` path byte-identical (FR-002).

**Alternatives considered**: Top-aligned runs — rejected: looks broken for mixed sizes and complicates the baseline. Per-run vertical offsets (super/subscript) — out of scope (FR-018).

---

## R6 — Case dispatch & structural zero-drift

**Decision**: The per-grammar label helper dispatches on the label:
| Label | Path | Guarantee |
|---|---|---|
| `None` | `[]` | byte-identical to pre-feature symbol (FR-002) |
| `Some (LabelText.Plain s)` | existing `wrapLabel`/`labelNodes` on `s` | byte-identical to spec 197 (and 196 for one line) |
| `Some (LabelText.Rich runs)`, all runs default-styled | join run texts → the `Plain` path | byte-identical to the equivalent plain label (FR-002) |
| `Some (LabelText.Rich runs)`, any styled run | new `richLabelNodes` | the new behaviour |

"Default-styled" = `Color = None && Weight = None && (Scale = None || Scale = Some 1.0)`.

**Rationale**: Delegating the unstyled and all-default cases to the **verbatim** 197 code makes SC-003 structural rather than incidental — there is no second code path that could drift the goldens. Joining all-default runs' texts (preserving inter-run spacing per their original text) reproduces the plain inline flow.

**Alternatives considered**: One unified styled path handling the plain case too — rejected: any refactor of the proven 197 emit risks a 1-byte golden drift; the spec's hard constraint (SC-003) makes structural delegation the safe choice.

---

## R7 — Tofu-free verification at the render edge (per run)

**Decision**: Each styled segment is a `Scene.glyphRunProof` node carrying per-glyph `Missing`/`FallbackMode`. The pure library **never installs and never requires a measurer** and never throws without one (FR-010). A render-bridge test (`tests/Symbology.Render.Tests/RenderLabelTests.fs`) rasterises a **multi-run styled** labelled token through `Render.toPng` under the real measurer (`SkiaViewer.Fonts`) and asserts **every** run's `TofuCount = 0` and the board is non-blank (FR-005).

**Rationale**: Tofu-free is a property of the installed real measurer, asserted with **real** evidence (Constitution V), not assumed. Identical to the 196/197 seam, now per styled run.

**Alternatives considered**: Asserting tofu-free in the pure library — impossible/incorrect: the pure fallback measurer can report `Missing`; tofu-free is the render edge's contract.

---

## R8 — Determinism under a fixed measurement provider

**Decision**: Identical styled `Token` ⇒ identical `Scene` ⇒ identical `SceneCodec.export(...).CanonicalBytes`, in-process and cross-process, under a fixed measurement provider. The inline break, per-run fit, and geometry are pure functions of the resolved measurement; no wall-clock, randomness, or IO (FR-009/SC-004). A new styled cross-process golden is pinned (alongside the unchanged `0dda10bd…`/`6710215b…`/`b41c9626…`).

**Rationale**: Mirrors the existing determinism contract; the only new inputs (run colours/weights/scales) are pure data folded into measurement and paint.

**Alternatives considered**: Cross-provider byte-identity — explicitly **not** promised (provider-relative, FR-009).

---

## R9 — Fixture migration & test-battery shape

**Decision**: The type change touches every Symbology test fixture that sets `Label = Some "X"` (~30 sites across `ChannelPresenceTests`, `DeterminismTests`, `PlaceholderTests`, `GalleryTests`, `LegibilityTests`, `LabelTests`, `MultilineLabelTests`, `RenderLabelTests`). All migrate **mechanically and value-preservingly** to `Some (LabelText.Plain "X")` — behaviour and bytes unchanged. New coverage lives in a new `RichLabelTests.fs` (per-run presence; single-default-`Rich` ≡ `Plain` byte-identity; inline wrap; cap+ellipsis; mixed-size line-height; empty-run collapse; first-line baseline; per-segment ≤ region) + a styled render-bridge tofu test + a linter-invariance assertion + the new styled golden. **No existing assertion is modified or deleted**; the migration only rewrites the *constructor*, not the *expectation*.

**Rationale**: Confines the blast radius (the only `Token.Label` consumer is Symbology), keeps the zero-drift goldens green by construction, and isolates new behaviour in a new file (Constitution V — existing evidence preserved, new evidence added).

**Alternatives considered**: A compatibility shim keeping `Some "X"` compiling (e.g. an implicit op) — F# has no user-defined implicit string conversion here; rejected as non-idiomatic. The mechanical migration is simpler and explicit.

---

## R10 — Skill documentation (loop guidance)

**Decision**: Extend `src/Symbology/skill/SKILL.md` (and mirror to `template/product-skills/fs-gg-symbology/SKILL.md`) with a rich-text section: supported attributes (colour/weight/size), keep runs **few** and the palette **restrained**, **do not** colour runs to impersonate the faction/state pre-attentive encodings, requires the real measurer for tofu-free output, surplus runs/width degrade via wrap→cap→ellipsis, complements (never replaces) the vector sigil. Passes `scripts/check-agent-skill-parity.fsx` (`critical=0 high=0`).

**Rationale**: FR-017 + the colour caveat (FR-013) are loop guidance, not runtime rules; the skill is where they live, mirrored for parity (SC-007).

**Alternatives considered**: Enforcing the colour caveat at runtime — rejected (FR-013: author-supplied colours are not re-mapped/rejected; the linter's governance is unchanged).

---

**All NEEDS CLARIFICATION resolved.** No open unknowns remain for Phase 1.

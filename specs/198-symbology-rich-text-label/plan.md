# Implementation Plan: Symbology Rich-Text Label Runs (per-run colour / weight / size)

**Branch**: `198-symbology-rich-text-label` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/198-symbology-rich-text-label/spec.md`

**Source design**: the symbology **M0–M7 roadmap** ([`docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md`](../../docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md)) is complete; this is the next **deferred M7 "(∞)" backlog item**. Spec 196 ([label / glyph-text channel](../196-symbology-label-text/spec.md)) shipped a single-line label; spec 197 ([multi-line / paragraph label](../197-symbology-multiline-label/spec.md)) widened it to several lines — both draw the whole label in **one uniform style** and explicitly deferred **rich-text styling (per-run colour / weight / size)** (197 FR-016). This feature delivers that item by letting the **same label channel** carry **styled runs**.

## Summary

Let the existing optional identity label carry **styled runs** — a short ordered sequence of text spans, each with its own optional **colour, weight, and size** — so a designer can give a unit a loud bold callsign next to a dim small code in one label. Styling rides the **same single label channel** (one field on `Token`, one `'stats -> Token` mapping, no second channel — FR-001), is **opt-in and layered-additive** (no label ≡ pre-feature symbol; a *plain* label, single- or multi-line, ≡ spec 197 byte-for-byte; a single default-styled run ≡ the plain label — FR-002/SC-003), is **fitted per-run** to each grammar's existing label region (wrap/cap/ellipsis, no clip/overflow — FR-006/SC-005), and is **tofu-free** when rendered through the render bridge's real measurer (FR-005). It stays in the **pure scene-only layer** (no raster/GL/IO, no new font files — FR-016) and is **deterministic** under a fixed measurement provider (FR-009/SC-004).

Technical approach (grounded against the tree on 2026-06-25):

- **The label channel widens to a typed run model — a real `.fsi` / surface change (FR-015, anticipated by the spec).** Unlike 197 (which rode the existing `string option` with zero surface delta), per-run styling cannot be expressed in a bare string without inventing a markup grammar (rejected — opaque, error-prone, not idiomatic F#). The single shared channel is therefore retyped from `Token.Label : string option` to **`Token.Label : LabelText option`**, where:
  ```fsharp
  type LabelRun =
      { Text: string
        Color: Color option    // None ⇒ default labelInk (the spec-196 ink)
        Weight: int option     // None ⇒ default weight; maps directly onto FontSpec.Weight (int option)
        Scale: float option }   // None ⇒ 1.0; multiplies the grammar's base label size (keeps grammar-independence)
  [<RequireQualifiedAccess>]
  type LabelText =
      | Plain of string         // unstyled — single- or multi-line via embedded \n; the spec-197 channel verbatim
      | Rich of LabelRun list   // ordered styled runs
  ```
  This keeps **one channel** (one field, one mapping — FR-001), names the two modes so the **zero-drift path is structural** (`LabelText.Plain s` literally re-enters the spec-197 code), and declares every new public type in the `.fsi` (Constitution II). Run attributes are exactly **colour / weight / size** (FR-003); each is optional and inherits the default when `None`, so an all-default run reproduces the uniform style exactly.
- **Layered zero-drift, structural not incidental (FR-002/SC-003).** The per-grammar label dispatcher routes by case:
  - `None` ⇒ `[]` (no node) — byte-identical to the pre-feature symbol.
  - `Some (LabelText.Plain s)` ⇒ the **unchanged spec-197** `wrapLabel`/`labelNodes` path on `s` — byte-identical to 197 (and to 196 for a one-line `s`).
  - `Some (LabelText.Rich runs)` where **every run is default-styled** (`Color=None && Weight=None && Scale∈{None,Some 1.0}`) ⇒ join the run texts and route through the **same** `LabelText.Plain` path — byte-identical to the equivalent plain label (FR-002's "single default run ≡ plain").
  - `Some (LabelText.Rich runs)` with **any** non-default attribute ⇒ the **new** `richLabelNodes` inline-run layout.
  Because the unstyled and all-default cases delegate to the verbatim 197 code, **every** existing spec-192/194/195/196/197 golden (`0dda10bd…` no-label, `6710215b…` `HMR-7`, `b41c9626…` multi-line, gallery/filmstrip/badge/ring) stays byte-identical once its fixture is migrated from `Some "X"` to `Some (LabelText.Plain "X")` (mechanical, value-preserving). No existing assertion is weakened.
- **`richLabelNodes` is a bounded inline-run layout reusing the proven per-segment fit (FR-006).** The styled path: (1) **atomise** the run sequence into a stream of `(word, resolvedStyle)` plus hard-break markers (split each run's `Text` on `\n`/`\r\n` → hard breaks; split each segment on whitespace → words; empty/whitespace runs drop, FR-007); (2) **greedy line-break** — pack words while the running line width (each word measured **in its own resolved font**, `Scene.measureTextResolved`) ≤ region width, else start a new line; a hard break forces a new line; (3) **cap** the line count to the grammar's existing per-region budget and **ellipsis** the last kept line (FR-006/SC-005); (4) per line, **line-height = max** of each run's `lineHeightOf` at its scale, runs share a common baseline (FR-006); (5) **centre** each line by its total measured width and emit **one `Scene.glyphRunProof` node per contiguous same-style segment** left-to-right, the first line anchored at spec 197's exact first-line baseline, subsequent lines stacked downward by the line-height. A single word wider than the region has no wrap point and degrades through the **existing `fitLabel`** (shrink-toward-floor → measured ellipsis-truncate, `Symbology.fs:281`) applied per styled segment, so no segment ever clips mid-glyph or overflows (FR-006/SC-005).
- **Per-run style resolution (FR-003).** A run resolves to `(Color, FontSpec)` at a grammar base size `b`: colour = `run.Color |> Option.defaultValue labelInk`; font = `{ Family = None; Size = max 1.0 (b * (run.Scale |> Option.defaultValue 1.0)); Weight = run.Weight }`. The existing `labelFontOf` becomes `labelFontWith (weight: int option) (size: float)` with the no-weight, scale-1.0 call reproducing today's `{ Family=None; Size; Weight=None }` exactly (zero drift on the plain path).
- **Tofu-free is a render-edge property, per styled segment (FR-005/FR-010).** Each emitted node is `Scene.glyphRunProof` carrying per-glyph `Missing`/`FallbackMode`; the pure library **never installs and never requires a measurer** and never throws without one. Tofu-free *rendering* of every run comes from the real measurer/shaper `Symbology.Render` already installs — the render-bridge test rasterises a **styled** label and asserts every run is non-tofu under the installed measurer.
- **Safe degenerate / empty (FR-007/FR-008).** A label that is `None`, `Plain ""`/whitespace, `Rich []`, or `Rich` of all-empty/whitespace runs ⇒ no node, no throw. A degenerate `Token` (`R <= 0`) with any label (plain or styled) still degrades to the existing **visible placeholder** — placeholder wins (`Symbology.fs:413`); neither path throws.
- **Boards + linter unchanged (FR-011/FR-012).** `render`/`gallery`/`galleryIn`/`filmstrip`/`filmstripIn`/`animate`/`animateIn` keep their **signatures** (they thread the whole `Token`), so a styled-label roster renders per grammar by construction. The legibility linter (`Legibility.score`/`scoreAnimated`) governs only the **pre-attentive** channels and does **not** read `Token.Label` (verified: no `Label` reference in `Legibility.fs`), so its verdict is grammar-independent and unchanged by plain-or-styled labels — a test asserts label styling does not alter a roster's `Report`.
- **Colour policy: author-supplied, guidance-governed (FR-013).** Run colours are taken from the existing `Scene.Color` vocabulary and are **not** re-mapped or rejected at runtime; the symbology pre-attentive channels remain the faction/state palettes, the label stays inspection-detail, and "don't impersonate faction/state encodings" is a **skill caveat**, not an enforced rule — the linter's governance is untouched.
- **Specification-first, Tier 1 (behavioural + surface).** This alters behaviour covered by specs 196/197 **and** adds public surface (`LabelRun`, `LabelText`, the retyped `Token.Label`, convenience constructors). It is **Tier 1**; the `Symbology.fsi` surface baseline **will move** and is regenerated (FR-015), with **zero drift** on every other package baseline. Expecto semantic tests fail-before/pass-after through the public `token`/`badge`/`ring`/`render` surface; new styled-run batteries prove the new behaviour while every pre-feature golden stays byte-identical.
- **Loop docs (FR-017).** The `fs-gg-symbology` skill documents rich-text runs — supported attributes (colour/weight/size), keep to a few short runs and a restrained palette, do not impersonate faction/state encodings, requires the real measurer for tofu-free output, complements (never replaces) the sigil — authored canonically in `src/Symbology/skill/SKILL.md` and mirrored, passing `scripts/check-agent-skill-parity.fsx`.

> **Standing assumption — behaviour is unverified until exercised.**
> This is a *greenfield-additive* widening, not a defect fix, so there is no root-cause map. The run
> layout is pure scene logic with **no GL/raster/IO**, fully exercisable headlessly; its *tofu-free
> rendering per run* is a render-edge property verified by a **real** render-bridge test (rasterise a
> styled-run-labelled token through `Symbology.Render` under the installed measurer and assert every run
> is non-tofu), not assumed from this plan. The analogue of the live-smoke mandate is an **early
> FSI/test smoke** (Foundational phase): once the `LabelText`/`LabelRun` types and `richLabelNodes`
> exist, load the public surface in FSI and confirm `LabelText.Plain "X"` is byte-identical to its
> spec-197 render, a single default-styled `Rich` run is byte-identical to that plain label, a two-run
> styled label emits ≥2 nodes differing in colour/weight/size within the region, an over-wide styled run
> wraps/shrinks, an over-budget styled label caps+ellipsises, and a degenerate styled token returns the
> placeholder without throwing — before building out US1/US2/US3. Treat that smoke — and the
> render-bridge tofu test — not this plan's narrative, as the confirmation the channel works.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution Engineering Constraints).

**Primary Dependencies** (existing, consumed via public types only): the `Token`/`Grammar` types of `FS.GG.UI.Symbology` (`src/Symbology/Symbology.fsi`); transitively `FS.GG.UI.Scene` for `Color` (`{ Red; Green; Blue; Alpha }`, `src/Scene/Types.fsi:9`), `FontSpec` (`{ Family; Size; Weight: int option }`, `Types.fsi:168`), `TextMetrics` (`{ Width; Height; Baseline }`, `Types.fsi:181`), `Scene.measureTextResolved`/`Scene.glyphRunProof`/`Scene.group` (already referenced by `Symbology.fsproj`). The headless render bridge `FS.GG.UI.Symbology.Render` (`Render.toPng`) and the real measurer `SkiaViewer.Fonts` installs are reused **as-is** for the tofu-free render test. **No new third-party dependency, no new font files** (FR-016).

**Storage**: None. The label performs no IO; its entire output is part of the returned `Scene` value (FR-009/FR-016).

**Testing**: Expecto + FsCheck, matching `tests/Symbology.Tests/`. Existing channel-presence / determinism / placeholder / gallery / linter / label / multi-line batteries are **migrated** (the `Some "X"` fixtures become `Some (LabelText.Plain "X")`, value-preserving — existing assertions stay green by construction) and **extended**; a new styled-run battery covers per-run colour/weight/size presence, single-default-run ≡ plain byte-identity, inline wrap/cap/ellipsis, mixed-size line-height, empty-run collapse, degenerate-with-styled-label; a **render-bridge tofu test** asserts every rasterised styled run is non-tofu; a **linter-invariance test** asserts styled-label presence does not change a roster's `Report`.

**Target Platform**: Linux/CI headless. Run layout is pure CPU (no GL); the tofu-free render test runs through the existing `Symbology.Render` bridge (real measurer installed at the edge). Fully reproducible across processes under a fixed measurement provider (SC-004).

**Project Type**: Multi-project F# solution (`FS.GG.Rendering.slnx`). The change is **internal to the existing `src/Symbology/` library** plus a curated **`.fsi` surface addition** (new `LabelRun`/`LabelText` types, retyped `Token.Label`, convenience constructors); extended tests in existing test projects. No new project, no new sample (the existing `gallery`/`galleryIn` boards already render a styled roster).

**Performance Goals**: Design-time/review tool, not a render hot path. Each styled label adds `O(words)` measurement during inline break + ≤ (budget × runs-per-line) glyph-run nodes per unit; a board is `O(N)`. No fps/throughput guarantee is asserted (parity with the existing grammars).

**Constraints**: **Layered zero-drift is the hard constraint** (FR-002/SC-003) — a no-label token, a `Plain` label (single- or multi-line), and an all-default `Rich` run are byte-identical to the spec-192/196/197 goldens. **Provider-relative determinism** (FR-009/SC-004) — identical styled `Token` under a fixed measurement provider ⇒ byte-identical scene; inline break is a deterministic function of the resolved per-run measurement; no wall-clock, randomness, or IO. **Tofu-free at the render edge, never required by the pure library, per run** (FR-005/FR-010). **Fitted, capped, no clip/overflow, mixed-size lines sized to tallest run** (FR-006/SC-005). **Non-overlapping siting** (FR-004). **Surface baseline moves only for the symbology package** (FR-015) — every other baseline unchanged; **grammar-independent linter unchanged** (FR-012/SC-006); **author colours, guidance-only governance** (FR-013).

**Scale/Scope**: M7 backlog rich-text thread only. `Symbology.fsi` gains `LabelRun`, `LabelText`, the retyped `Token.Label`, and convenience constructors; `Symbology.fs` gains `richLabelNodes` (inline-run layout), a per-run style resolver (`labelFontWith` + colour default), and the case-dispatch in the per-grammar label helpers (~1 edited source `.fs` + its `.fsi`). Tests: ~30 fixture migrations (`Some "X"` → `Some (LabelText.Plain "X")`) + a new `RichLabelTests.fs` battery + 1 styled render-bridge tofu test + 1 linter-invariance assertion + 1 new styled cross-process golden + the mirrored skill rich-text doc. The symbology surface baseline is regenerated (FR-015). Deferred (FR-018): full rich-text layout, attributes beyond colour/weight/size (italic/underline/strike/spacing/per-glyph), auto-label-from-stats, label-bound motion, advanced bidi, new GPU/compute path, new font files.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence in this plan |
|---|---|---|
| **I. Spec → FSI → Semantic Tests → Implementation** | PASS | The `.fsi` is edited **first** (new `LabelRun`/`LabelText`, retyped `Token.Label`, ctors), then semantic Expecto tests over the public `token`/`badge`/`ring`/`render` surface (not internals) fail-before/pass-after, then the `.fs` body. The surface delta is intentional and curated (FR-015). |
| **II. Visibility in `.fsi`, not `.fs`** | PASS | Every new public type/ctor is declared in `Symbology.fsi`; the internal `richLabelNodes`/`labelFontWith`/style resolver keep the existing `let private` style (`Symbology.fs:260+`) and are omitted from the `.fsi`. The symbology surface baseline is regenerated to capture the addition (FR-015); no other baseline moves. |
| **III. Idiomatic Simplicity** | PASS | A small record (`LabelRun`) + a 2-case DU (`LabelText`) + greedy inline break composing the **existing** `measureTextResolved`/`fitLabel`/`glyphRunProof` primitives. Optional run attributes with `Option.defaultValue` defaults; no SRTP, reflection, type providers, custom operators, or non-trivial CE. No new `mutable` (the line-break is a pure fold, mirroring the existing `wrapSegment`). The DU is justified (Principle III) by the need to name the zero-drift `Plain` path structurally. |
| **IV. Elmish/MVU boundary** | PASS (N/A) | The label is **pure, stateless, IO-free** (`Token -> Scene`); no multi-step state, IO, retries, or background work, so the MVU obligation does not attach. Provider-relative purity is itself the contract the tests assert (SC-004). The render-edge measurer seam already exists and is reused, not introduced. |
| **V. Test Evidence Mandatory** | PASS | Expecto semantic tests over the public surface, fail-before/pass-after; all **real** (pure scene logic). Tofu-free claim verified by a **real** render-bridge raster test through `Symbology.Render` under the real measurer. Determinism via `SceneCodec.export(...).CanonicalBytes`; layered zero-drift via the pinned `0dda10bd…`/`6710215b…`/`b41c9626…`/gallery/filmstrip/badge/ring goldens (kept green after value-preserving fixture migration). **No existing assertion weakened or deleted**; new behaviour is covered by added assertions, each at least as strong (every styled segment ≤ region, every run non-tofu). |
| **VI. Observability & Safe Failure** | PASS | Safe failure *as a visible placeholder*: a degenerate (`R <= 0`) styled-labelled `Token` renders the placeholder and never throws (FR-008/SC-005); empty/whitespace/empty-run labels degrade to no-label without throwing (FR-007). Tofu, if the edge ever lacked coverage, is **disclosed** per run via the glyph run's `Missing` flag (the existing fail-loud text contract), never silently drawn as a plausible-wrong glyph. |
| **Change Classification** | **Tier 1** | **Alters observable behaviour covered by existing specs (196/197) AND adds public surface.** Tier 1 by both clauses. The `Symbology.fsi` surface baseline is regenerated (FR-015) with zero drift elsewhere; the discipline (spec, plan, FSI-first, semantic tests, docs/skill, zero-drift goldens for no-label / plain / all-default-run input) mirrors spec 195 (which likewise added surface + behaviour). |
| **Engineering Constraints** | PASS | `net10.0`; F#-only; change internal to the existing **pure** package plus its curated `.fsi`, referencing only its own `Token` types + already-referenced `FS.GG.UI.Scene` `Color`/`FontSpec`/text vocabulary; **no new dependency, no new font files** (FR-016); baselines maintained (only the symbology surface moves, recorded); `FS.GG.UI.*` identity untouched; SkiaSharp/GL backend untouched (the label rides the existing text/measurer seam). **No control fork**: symbology vocabulary, not a per-theme control copy. |

**Gate result: PASS** — no violations; Complexity Tracking left empty. Tier 1 by **both** the behavioural and surface clauses; the `Symbology.fsi` baseline intentionally moves and is regenerated (FR-015), mirroring spec 195's honest surface-delta note.

## Project Structure

### Documentation (this feature)

```text
specs/198-symbology-rich-text-label/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — field-shape decision (LabelText DU + LabelRun record), inline-run
│                        #   break algorithm, zero-drift delegation strategy, per-run style resolution
│                        #   (colour/weight/scale), line-height = max-run, centring, render-edge tofu path,
│                        #   fixture-migration plan, test-battery shape
├── data-model.md        # Phase 1 — LabelRun/LabelText types; the retyped Token; the case-dispatch table;
│                        #   per-run resolution rules; per-grammar budgets (reused); contract vs design-loop split
├── quickstart.md        # Phase 1 — build + FSI smoke + run tests + per-SC validation + surface baseline regen
├── contracts/           # Phase 1 — the rich-text label contract (the .fsi delta + behaviour table)
│   └── symbology-rich-text-label-api.md
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/Symbology/                       # EXISTING pure package FS.GG.UI.Symbology (Scene-only)
├── Symbology.fsi                    #   EDIT — ADD `type LabelRun = { Text; Color; Weight; Scale }`,
│                                    #     `[<RequireQualifiedAccess>] type LabelText = Plain of string | Rich of LabelRun list`,
│                                    #     RETYPE `Token.Label : LabelText option`, ADD convenience ctors
│                                    #     (e.g. `Symbology.plainLabel`, `Symbology.run`, `Symbology.richLabel`)
├── Symbology.fs                     #   EDIT — `labelFontWith (weight) (size)` (generalises `labelFontOf`);
│                                    #     per-run style resolver (colour default labelInk, font from weight+scale);
│                                    #     `richLabelNodes` (atomise → greedy inline break → cap+ellipsis →
│                                    #     per-line max-height + common baseline → centred per-segment nodes);
│                                    #     case-dispatch in tokenLabelNodes/badgeLabelNodes/ringLabelNodes:
│                                    #       None→[]; Plain→(existing wrapLabel/labelNodes); Rich(all-default)→Plain;
│                                    #       Rich(styled)→richLabelNodes. Budgets reused (Token≤3, Badge≤2, Ring≤2)
├── Legibility.fsi / Legibility.fs   #   UNCHANGED (does NOT read Token.Label; label stays inspection-detail)
└── skill/SKILL.md                   #   EDIT — rich-text section: opt-in inspection-detail, attrs colour/weight/size,
                                     #     keep runs few + palette restrained, don't impersonate faction/state,
                                     #     requires real measurer for tofu-free, complements the sigil

tests/
├── Symbology.Tests/                 # EXISTING — MIGRATE `Some "X"` → `Some (LabelText.Plain "X")` (value-preserving),
│   │                                #   then extend; all current assertions stay green by construction
│   ├── ChannelPresenceTests.fs      #   EDIT — migrate fixtures; ADD: same chars / different run styling ⇒ differing bytes
│   ├── DeterminismTests.fs          #   EDIT — migrate `HMR-7`/`ALPHA\nBRAVO` to Plain (goldens UNCHANGED);
│   │                                #     ADD styled render-twice byte-equal + NEW pinned styled cross-process golden
│   ├── PlaceholderTests.fs          #   EDIT — migrate; ADD degenerate token WITH a styled label → placeholder, no throw
│   ├── GalleryTests.fs              #   EDIT — migrate; styled roster reproducible per grammar (FR-011)
│   ├── LegibilityTests.fs           #   EDIT — migrate; ADD styled-label presence does NOT change a roster's Report (FR-012)
│   ├── LabelTests.fs                #   EDIT — migrate single-line cases (UNCHANGED behaviour)
│   ├── MultilineLabelTests.fs       #   EDIT — migrate `\n` cases to Plain (UNCHANGED behaviour)
│   └── (new) RichLabelTests.fs      #   NEW — per-run colour/weight/size presence; single default Rich run ≡ Plain
│                                    #     byte-identity; inline wrap-to-width; line-cap + ellipsis; mixed-size line-height;
│                                    #     empty/whitespace-run collapse; first-line baseline preserved; each segment ≤
│                                    #     region; register in .fsproj
└── Symbology.Render.Tests/          # EXISTING — extend RenderLabelTests.fs: rasterise a STYLED (multi-run) labelled token
                                     #     through Render.toPng under the real measurer; assert EVERY run is non-tofu
                                     #     (TofuCount = 0) and the board is non-blank (FR-005)

readiness/surface-baselines/
└── FS.GG.UI.Symbology.*            # REGEN — the symbology surface baseline moves (new LabelRun/LabelText, retyped
                                     #   Token.Label, ctors); EVERY OTHER package baseline UNCHANGED (FR-015, recorded)

.claude/skills/fs-gg-symbology/      # pointer wrapper -> canonical (inherits the rich-text-doc edit)
.agents/skills/fs-gg-symbology/      # pointer wrapper -> canonical (inherits the rich-text-doc edit)
template/product-skills/fs-gg-symbology/SKILL.md   # EDIT — mirror the rich-text-doc update (adapted copy)

FS.GG.Rendering.slnx                 # no new project (change lands in existing Symbology.fsproj)
```

**Structure Decision**: Rich-text styling is a **typed widening of the single existing label channel** — `Token.Label : string option` becomes `Token.Label : LabelText option` (a `Plain | Rich` DU over a `LabelRun` record) inside `src/Symbology/`, **not** a new channel, parallel field, project, or markup grammar. Rationale (FR-001/FR-002/FR-003/FR-015): the "one channel vocabulary" principle means the same `'stats -> Token` mapping must carry the label unchanged, so the single field is retyped rather than supplemented (a second `RichLabel` field would be the forbidden "second label channel"); a 2-case DU makes the **zero-drift `Plain` path structural** (it literally re-enters the spec-197 code) and the run record carries exactly the deferred attributes (colour/weight/size), each optional with a default. Emitting styled runs as **per-segment sibling nodes appended to the child list** (rather than a wrapping `group`) preserves the byte-identity of the no-label (`[]`), plain, and all-default-run cases against the pinned goldens. The board/motion functions need **no signature change** — they thread the whole `Token`. The cost — a curated `.fsi` surface delta + ~30 mechanical fixture migrations — is anticipated by the spec (FR-015) and confined to the symbology package (the only `Token.Label` consumer). Tier 1 by both clauses; the symbology surface baseline is regenerated and recorded (FR-015).

## Complexity Tracking

> No constitution violations. Section intentionally empty.

## Implementation Status (2026-06-25) — ✅ COMPLETE

All 32 tasks in [tasks.md](./tasks.md) are done and verified. Delivered exactly as planned — a typed
widening of the single label channel, layered zero-drift, no second channel, no new dependency/font.

- **Surface (T004, FR-015)**: `Symbology.fsi` adds `LabelRun`, `LabelText` (`Plain | Rich`), the
  convenience ctors `plainLabel`/`run`/`richLabel`, and retypes `Token.Label : LabelText option`. Surface
  baseline regenerated — **only** `FS.GG.UI.Symbology.txt` moved; zero drift on every other baseline.
- **Implementation (T005–T008)**: `Symbology.fs` generalises `labelFontOf`→`labelFontWith` /
  `fitLabel`→`fitLabelW` / `lineHeightOf`→`lineHeightOfW` (the `None`-weight path is byte-identical),
  adds the per-run style resolver and `richLabelNodes` (atomise → greedy inline break → cap+ellipsis →
  per-line max-height common baseline → centred per-segment `glyphRunProof`), and routes via
  `labelDispatch`: `None`→`[]`, `Plain`/all-default-`Rich`→the verbatim spec-197 `labelNodes`, styled
  `Rich`→`richLabelNodes`.
- **Zero-drift proof (T009, FR-002/SC-003)**: ~30 fixtures migrated `Some "X"`→`Some (LabelText.Plain "X")`
  value-preservingly; the pinned goldens `0dda10bd…` / `6710215b…` / `b41c9626…` stayed byte-identical.
- **Evidence**: early FSI smoke 10/10; `Symbology.Tests` **287 passed** (+69 styled cases incl. a new
  pinned styled cross-process golden `2fd5ea98…`); `Symbology.Render.Tests` **11 passed** (+3 styled
  tofu-free cases); skill-parity `critical=0 high=0`. Full per-SC ledger in `readiness/ledger.md`.
- **Docs (T026/T027, FR-017)**: rich-text section authored in `src/Symbology/skill/SKILL.md` and mirrored
  to `template/product-skills/fs-gg-symbology/SKILL.md` (parity green).
- **Deviations**: none. The "verify/tune" tasks (T015/T021/T022/T025) required no further tuning — the
  layout was correct against its tests on first implementation, and the board entry points kept their
  signatures (FR-011) unchanged.

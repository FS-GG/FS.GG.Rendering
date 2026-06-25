# Implementation Plan: Symbology Multi-line / Paragraph Label Channel

**Branch**: `197-symbology-multiline-label` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/197-symbology-multiline-label/spec.md`

**Source design**: the symbology **M0–M7 roadmap** ([`docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md`](../../docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md)) is complete; this is the next **deferred backlog item**. Spec 196 ([symbology label / glyph-text channel](../196-symbology-label-text/spec.md)) shipped a **single-line** identity label and explicitly deferred **multi-line / paragraph text** (its FR-016). This feature delivers that item by **widening the existing label channel** to multiple lines.

## Implementation Status — ✅ COMPLETE (2026-06-25)

All 21 tasks (T001–T021) are done and marked `[X]` in [tasks.md](./tasks.md). The feature shipped exactly
as planned: multi-line rides the existing `Label : string option` (no `.fsi` edit, no baseline moves).

- **Source (1 file):** `src/Symbology/Symbology.fs` — added internal `wrapSegment` (greedy whitespace
  word-wrap), `wrapLabel` (split `\n` → trim → drop-blank → wrap → cap-to-budget → ellipsis-last-line),
  `lineHeightOf`, a list-returning `labelNodes` (downward stacking from the spec-196 baseline via the
  existing `fitLabel` per line), and a `Scene list`-taking `withLabel` (bare-sibling append). Per-grammar
  budgets: Token ≤ 3, Badge ≤ 2, Ring ≤ 2.
- **Tests:** `Symbology.Tests` 177 → **218** green (+41); `Symbology.Render.Tests` 6 → **8** green (+2).
  New `MultilineLabelTests.fs` (stacking, wrap, cap+ellipsis, blank-collapse, single-word degrade,
  pure-path FR-009) + extensions to Determinism/ChannelPresence/Placeholder/Gallery/Legibility/RenderLabel.
  No existing assertion modified or deleted — layered zero-drift held by construction.
- **Goldens:** `0dda10bd…` (no-label) and `6710215b…` (one-line `"HMR-7"`) unchanged; new multi-line
  cross-process golden pinned `b41c9626…`.
- **Docs:** multi-line section authored in `src/Symbology/skill/SKILL.md` and mirrored to
  `template/product-skills/fs-gg-symbology/SKILL.md`; skill parity `critical=0 high=0`.
- **Evidence:** [readiness/baseline.md](./readiness/baseline.md) (T002/T003/T005/T020),
  [readiness/sc-validation.md](./readiness/sc-validation.md) (SC-001…SC-007 all PASS),
  [feedback/implement-2026-06-25.md](./feedback/implement-2026-06-25.md).
- **Surface/regression:** `Symbology.fsi` diff empty; `readiness/surface-baselines/` clean (FR-013);
  no-regression re-run shows the **same 2 pre-existing reds** (`Package.Tests`, `ControlsGallery.Tests`)
  with identical fail counts — nothing regressed.

## Summary

Let the existing optional identity label (`Token.Label : string option`, spec 196) carry **more than one line** — a short paragraph — **stacked and fitted** inside each grammar's label region, drawn **tofu-free** through the render bridge's real measurer. Multi-line content is expressed in the **same field** via embedded line breaks (`\n`) plus **whitespace soft-wrap** of a long line to the region width; **no new public surface, no second channel, no new mapping**. The change is **layered-additive**: a `Token` with **no** label is byte-identical to the pre-feature symbol (spec-192 goldens), and a label that **fits on one line** is byte-identical to spec 196's single-line label (spec-196 goldens) — multi-line is engaged only when the label contains whitespace or an explicit break and is too wide for one line.

Technical approach (grounded against the tree on 2026-06-25):

- **No `.fsi` / surface change — multi-line rides the existing `Label : string option`.** The label is already a `string option` on the shared `Token` (`src/Symbology/Symbology.fsi:62`). Multi-line is an **internal behavioural widening** of how that string is laid out, expressed as embedded line breaks plus soft-wrap. No new field, no new val, no per-grammar mapping (FR-001). This is the plainest opt-in shape (Idiomatic Simplicity) and keeps **every** symbology surface baseline unchanged (FR-013) — the alternative (`string list` / a new `LabelLines` field) would add surface *and* break the "existing callers pass `Some "X"`" byte-identity story.
- **Layered zero-drift, proven by the actual 196 fixtures (FR-002/SC-003).** The render path emits the label as **sibling glyph-run nodes appended to the child list**, exactly as spec 196 appends its single node (`Symbology.fs:330` `withLabel`). For a label that fits on **one line at base size**, the emit is the **same single `Scene.glyphRunProof` node at the same baseline** ⇒ byte-identical canonical bytes. Confirmed against the pinned goldens: the labelled cross-process golden pins `Label = Some "HMR-7"` (`DeterminismTests.fs:130`) — a short label that fits one line, so it stays `6710215b…`; the overlong-fit fixture is `"THIS-CALLSIGN-IS-FAR-TOO-LONG-TO-FIT-1234567890"` (`LabelTests.fs:67`) — a **single unbroken token with no whitespace**, so under **whitespace-only** wrap it cannot wrap and falls back to spec 196's per-line shrink/ellipsis ⇒ one node ≤ region, the existing test passes **unmodified**; and all `Label = None` goldens (`0dda10bd…`, gallery/filmstrip, badge/ring) carry no label node. **Every existing spec-192/194/195/196 golden and test stays green with zero modification.**
- **Multi-line fit reuses the proven single-line fit per line (FR-005).** A new internal `wrapLabel` normalises the raw string — split on `\n`/`\r\n` into paragraph segments, trim, drop blank/whitespace segments (deterministic collapse, FR-006) — then **greedy whitespace word-wraps** each segment to the region width using the same real measurement (`Scene.measureTextResolved`), caps the total to the grammar's per-region **line budget**, and ellipsis-marks the last drawn line when content remains. **Each emitted line then passes through the existing `fitLabel`** (shrink-toward-floor, then measured ellipsis-truncate — `Symbology.fs:273`) so every line is guaranteed ≤ region width, never clipped mid-glyph (FR-005/SC-005). A single unbroken word wider than the region has no wrap point and degrades via that same per-line fit (the edge-case "wrap/shrink/truncate" latitude the spec grants).
- **Stacked, screen-aligned, first line anchored at the 196 baseline (FR-003).** Lines stack **downward** from the existing per-grammar baseline by a measured line-height (`TextMetrics.Height`, `src/Scene/Types.fsi:181`), screen-aligned (heading never rotates the block). Anchoring the **first** line at spec 196's exact baseline is what makes the one-line case byte-identical. The per-grammar **line budget + line-height** (provisional: Token ≤ 3, Badge ≤ 2, Ring ≤ 2 inside the inner disc) is the one new geometry knob; exact values are a design-loop detail (research.md / data-model.md), the contract is FR-003 (sited, observable, non-overlapping) + FR-005 (capped, no overflow).
- **Tofu-free is a render-edge property, per line (FR-004/FR-009).** Each line is a `Scene.glyphRunProof` node whose `GlyphRunData` carries per-glyph `Missing`/`FallbackMode`; the pure library **never installs and never requires a measurer** and never throws without one. Tofu-free *rendering* of every line comes from the real measurer/shaper `Symbology.Render` already installs — the render-bridge test asserts **every** line run is non-tofu under the installed measurer.
- **Safe degenerate / empty (FR-006/FR-007).** Empty / whitespace / blank-lines-only ⇒ no nodes, no throw. A degenerate `Token` (`R <= 0`) with any label still degrades to the existing **visible placeholder** — placeholder wins over the label (`Symbology.fs:336`); neither path throws.
- **Boards + linter unchanged (FR-010/FR-011/FR-012).** `gallery`/`galleryIn`/`filmstrip`/`filmstripIn`/`animate`/`animateIn` need **no signature change** — they thread the whole `Token`, so a multi-line roster renders per grammar by construction. The legibility linter (`Legibility.score`/`scoreAnimated`) governs the **pre-attentive** channels; the label (single- or multi-line) is **inspection-detail** and is *not* added to the §4 capacity table, so the verdict is unchanged and grammar-independent — a test asserts label presence does not alter a roster's `Report`.
- **Specification-first, Tier 1 (behavioural).** This alters observable behaviour covered by spec 196 (a label containing whitespace/`\n` and too wide for one line now wraps/stacks instead of one-line shrink), so it is **Tier 1**, but it adds **no public surface** — `Symbology.fsi` is unchanged and every baseline stays put (recorded per FR-013). Expecto semantic tests fail-before/pass-after through the public `token`/`badge`/`ring`/`render` surface; the new multi-line behaviour is proven by new batteries while every pre-feature golden stays byte-identical.
- **Loop docs (FR-015).** The `fs-gg-symbology` skill documents multi-line as **opt-in inspection-detail**: keep to a few short lines, how surplus width/lines degrade (wrap → cap → ellipsis), requires the real measurer for tofu-free output, complements (never replaces) the sigil — authored canonically in `src/Symbology/skill/SKILL.md` and mirrored, passing `scripts/check-agent-skill-parity.fsx`.

> **Standing assumption — behaviour is unverified until exercised.**
> This is a *greenfield-additive* widening, not a defect fix, so there is no root-cause map. The line
> layout is pure scene logic with **no GL/raster/IO**, fully exercisable headlessly; its *tofu-free
> rendering per line* is a render-edge property verified by a **real** render-bridge test (rasterise a
> multi-line-labelled token through `Symbology.Render` under the installed measurer and assert every line
> run is non-tofu), not assumed from this plan. The analogue of the live-smoke mandate is an **early
> FSI/test smoke** (Foundational phase): once `wrapLabel`/`labelNodes` exist, load the public surface in
> FSI and confirm a one-line label is byte-identical to its spec-196 render, a `\n`-bearing label stacks
> N nodes within the region, a too-wide whitespace label wraps, an over-budget label caps+ellipsises, and
> a degenerate labelled token returns the placeholder without throwing — before building out US1/US2/US3.
> Treat that smoke — and the render-bridge tofu test — not this plan's narrative, as the confirmation the
> channel works.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution Engineering Constraints).

**Primary Dependencies** (existing, consumed via public types only): the `Token`/`Grammar` types of `FS.GG.UI.Symbology` (`src/Symbology/Symbology.fsi`); transitively `FS.GG.UI.Scene` for `FontSpec`/`TextMetrics` (`{ Width; Height; Baseline }`, `src/Scene/Types.fsi:181`)/`Scene.measureTextResolved`/`Scene.glyphRunProof`/`Scene.group` (already referenced by `Symbology.fsproj`). The headless render bridge `FS.GG.UI.Symbology.Render` (`Render.toPng`) and the real measurer `SkiaViewer.Fonts` installs are reused **as-is** for the tofu-free render test. **No new third-party dependency, no new font files** (FR-014/FR-016).

**Storage**: None. The label performs no IO; its entire output is part of the returned `Scene` value (FR-008/FR-014).

**Testing**: Expecto + FsCheck, matching `tests/Symbology.Tests/`. Existing channel-presence / determinism / placeholder / gallery / linter / label batteries are **extended** (existing assertions untouched — they stay green by construction); a new multi-line battery covers wrap/cap/ellipsis/blank-collapse/stacking + one-line byte-identity; a **render-bridge tofu test** (`tests/Symbology.Render.Tests/`) asserts every rasterised line is non-tofu; a **linter-invariance test** asserts multi-line label presence does not change a roster's `Report`.

**Target Platform**: Linux/CI headless. Line layout is pure CPU (no GL); the tofu-free render test runs through the existing `Symbology.Render` bridge (real measurer installed at the edge). Fully reproducible across processes under a fixed measurement provider (SC-004).

**Project Type**: Multi-project F# solution (`FS.GG.Rendering.slnx`). The change is **internal to the existing `src/Symbology/` library** (no `.fsi` edit); extended tests in existing test projects. No new project, no new sample (the existing `gallery`/`galleryIn` boards already render a multi-line roster).

**Performance Goals**: Design-time/review tool, not a render hot path. Each label adds `O(words)` measurement during wrap + ≤ budget glyph-run nodes per unit; a board is `O(N)`. No fps/throughput guarantee is asserted (parity with the existing grammars).

**Constraints**: **Layered zero-drift is the hard constraint** (FR-002/SC-003) — a no-label token and a one-line-fitting label are byte-identical to the spec-192/196 goldens; the `token`/`gallery`/`filmstrip`/badge/ring/`HMR-7` goldens must stay green. **Provider-relative determinism** (FR-008/SC-004) — identical multi-line `Token` under a fixed measurement provider ⇒ byte-identical scene; wrapping is a deterministic function of the resolved measurement; no wall-clock, randomness, or IO. **Tofu-free at the render edge, never required by the pure library, per line** (FR-004/FR-009). **Fitted, capped, no clip/overflow** (FR-005/SC-005) — long lines wrap/shrink/truncate, line count is capped, empty/whitespace ⇒ no label, `R <= 0` ⇒ placeholder. **Non-overlapping siting** (FR-003). **Zero surface drift** on every baseline (FR-013 — no surface added); **grammar-independent linter unchanged** (FR-011/SC-006).

**Scale/Scope**: M7 backlog multi-line thread only. `Symbology.fs` gains an internal `wrapLabel` + a list-returning `labelNodes`/`withLabel` + per-grammar line budgets (~1 edited source file; **no `.fsi` edit**); extended `Symbology.Tests` batteries + 1 multi-line render-bridge tofu test + 1 linter-invariance assertion + the mirrored skill multi-line doc. Deferred (FR-016): rich-text per-run styling, auto-label-from-stats, label-bound motion, justified/bidi advanced typography, new GPU/compute path, new font files.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence in this plan |
|---|---|---|
| **I. Spec → FSI → Semantic Tests → Implementation** | PASS | `Symbology.fsi` is **unchanged** (no new surface — multi-line rides the existing `Label : string option`), so the FSI step is "confirm no surface delta". Expecto semantic tests call the public `token`/`badge`/`ring`/`render` surface (not internals) and fail-before/pass-after; the `.fs` body is written against the unchanged signature. |
| **II. Visibility in `.fsi`, not `.fs`** | PASS | No public surface is added. The internal `wrapLabel`/`labelNodes`/per-grammar budgets carry **no** `private`/`internal`/`public` modifiers beyond the existing `let private` style (`Symbology.fs:260+`) and are omitted from the `.fsi`. No baseline moves (FR-013). |
| **III. Idiomatic Simplicity** | PASS | Reuses the existing `string option` field + composes existing `Scene` text primitives in the same helper style; greedy whitespace wrap + per-line reuse of the proven `fitLabel`; `Scene list` append instead of `Scene option` append. No SRTP, reflection, type providers, custom operators, or non-trivial CE. No new `mutable`. |
| **IV. Elmish/MVU boundary** | PASS (N/A) | The label is **pure, stateless, IO-free** (`Token -> Scene`); no multi-step state, IO, retries, or background work, so the MVU obligation does not attach. Provider-relative purity is itself the contract the tests assert (SC-004). The render-edge measurer seam already exists and is reused, not introduced. |
| **V. Test Evidence Mandatory** | PASS | Expecto semantic tests over the public surface, fail-before/pass-after; all **real** (pure scene logic). Tofu-free claim verified by a **real** render-bridge raster test through `Symbology.Render` under the real measurer. Determinism via `SceneCodec.export(...).CanonicalBytes`; layered zero-drift via the pinned `0dda10bd…`/`6710215b…`/gallery/filmstrip/badge/ring goldens (kept green). **No existing test weakened or deleted** — existing label assertions stay green unmodified (verified against the `HMR-7`/no-whitespace-overlong fixtures); new behaviour is covered by added assertions, each at least as strong (every wrapped line ≤ region). |
| **VI. Observability & Safe Failure** | PASS | Safe failure *as a visible placeholder*: a degenerate (`R <= 0`) labelled `Token` renders the placeholder and never throws (FR-007/SC-005); empty/whitespace/blank-lines label degrades to no-label without throwing (FR-006). Tofu, if the edge ever lacked coverage, is **disclosed** per line via the glyph run's `Missing` flag (the existing fail-loud text contract), never silently drawn as a plausible-wrong glyph. |
| **Change Classification** | **Tier 1** | **Alters observable behaviour covered by an existing spec** (spec 196: a label with internal whitespace/`\n` too wide for one line now wraps/stacks instead of one-line shrink) — Tier 1 by the behavioural clause, even though it adds **no public surface**. `.fsi` and baselines are therefore unchanged and that is recorded (FR-013). The discipline (spec, plan, tests, docs/skill, zero-drift goldens for no-label and one-line-fitting input) mirrors specs 194/195/196. |
| **Engineering Constraints** | PASS | `net10.0`; F#-only; change internal to the existing **pure** package referencing only its own `Token` types + already-referenced `FS.GG.UI.Scene` text vocabulary; **no new dependency, no new font files** (FR-016); existing `.fsi` already curated; baselines maintained (unchanged); `FS.GG.UI.*` identity untouched; SkiaSharp/GL backend untouched (the label rides the existing text/measurer seam). **No control fork**: symbology vocabulary, not a per-theme control copy. |

**Gate result: PASS** — no violations; Complexity Tracking left empty. Tier 1 by the **behavioural** clause (not the surface clause); `.fsi`/baselines intentionally unchanged because no public surface is added — recorded per FR-013, mirroring spec 196's honest baseline note.

## Project Structure

### Documentation (this feature)

```text
specs/197-symbology-multiline-label/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — multi-line representation (embedded \n + whitespace soft-wrap),
│                        #   wrap/cap/ellipsis policy, per-grammar line budgets, line-height source,
│                        #   first-line-baseline zero-drift anchor, render-edge tofu path, test-battery shape
├── data-model.md        # Phase 1 — the (unchanged) Token; the wrap/normalise pipeline; per-grammar
│                        #   line-budget table; the contract vs design-loop split; fit/empty/degenerate rules
├── quickstart.md        # Phase 1 — build + FSI smoke + run tests + per-SC validation + baseline check
├── contracts/           # Phase 1 — the multi-line behaviour contract (no .fsi delta + behaviour table)
│   └── symbology-multiline-label-api.md
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/Symbology/                       # EXISTING pure package FS.GG.UI.Symbology (Scene-only)
├── Symbology.fsi                    #   UNCHANGED — `Label : string option` already present; no new surface
├── Symbology.fs                     #   EDIT — internal `wrapLabel` (split \n → trim/drop-blank → greedy
│                                    #     whitespace wrap → cap to per-grammar budget → ellipsis last line);
│                                    #     `labelNodes : ... -> Scene list` (per line via existing `fitLabel`,
│                                    #     stacked from the 196 baseline); `withLabel` takes `Scene list`
│                                    #     (append; [] ≡ no-label, [one] ≡ 196 single-line — byte-identical);
│                                    #     per-grammar line budgets in tokenLabelNode/badgeLabelNode/ringLabelNode
├── Legibility.fsi / Legibility.fs   #   UNCHANGED (label is inspection-detail; NOT added to the capacity table)
└── skill/SKILL.md                   #   EDIT — multi-line section: opt-in inspection-detail, keep lines short,
                                     #     wrap→cap→ellipsis degrade, requires real measurer for tofu-free

tests/
├── Symbology.Tests/                 # EXISTING — extend; all current assertions stay green UNMODIFIED
│   ├── ChannelPresenceTests.fs      #   EDIT — a single-line vs same-text-with-`\n` label produce differing bytes
│   ├── DeterminismTests.fs          #   EDIT — multi-line render-twice byte-equal + NEW pinned multi-line golden;
│   │                                #     existing `0dda10bd…`/`6710215b…`/gallery/filmstrip/badge/ring UNCHANGED
│   ├── PlaceholderTests.fs          #   EDIT — degenerate token WITH a multi-line label → placeholder, no throw
│   ├── GalleryTests.fs              #   EDIT — multi-line roster reproducible per grammar (FR-010)
│   ├── LegibilityTests.fs           #   EDIT — multi-line label presence does NOT change a roster's Report (FR-011)
│   ├── LabelTests.fs                #   EDIT — existing single-line cases UNCHANGED; ADD a list-returning helper
│   └── (new) MultilineLabelTests.fs #   NEW — \n stacking; whitespace wrap-to-width; line-cap + ellipsis; blank
│                                    #     collapse; one-line byte-identity to 196; first-line baseline preserved;
│                                    #     each drawn line ≤ region; register in .fsproj
└── Symbology.Render.Tests/          # EXISTING — add (RenderLabelTests.fs): rasterise a MULTI-LINE labelled token
                                     #     through Render.toPng under the real measurer; assert EVERY line run is
                                     #     non-tofu (TofuCount = 0) and the board is non-blank (FR-004)

readiness/surface-baselines/
└── (no change)                      # NO baseline moves — no public surface added (FR-013, recorded)

.claude/skills/fs-gg-symbology/      # pointer wrapper -> canonical (inherits the multi-line-doc edit)
.agents/skills/fs-gg-symbology/      # pointer wrapper -> canonical (inherits the multi-line-doc edit)
template/product-skills/fs-gg-symbology/SKILL.md   # EDIT — mirror the multi-line-doc update (adapted copy)

FS.GG.Rendering.slnx                 # no new project (change lands in existing Symbology.fsproj)
```

**Structure Decision**: Multi-line is an **internal behavioural widening of the existing `Label : string option`** inside `src/Symbology/Symbology.fs`, not a new field, channel, project, or `.fsi` change. Rationale (FR-001/FR-002/FR-013/FR-014): the "one channel vocabulary" principle means the same `'stats -> Token` mapping must carry the label unchanged, so widening the existing field (embedded `\n` + soft-wrap) keeps the vocabulary singular, adds **no public surface** (every baseline stays put), and reuses the proven single-line `fitLabel` per line. Emitting lines as **sibling nodes appended to the child list** (rather than a wrapping `group`) is what preserves byte-identity for the no-label (`[]`) and one-line-fitting (`[one node]`) cases against the pinned spec-192/196 goldens. The board/motion functions need **no signature change** — they thread the whole `Token`. `.fsproj` compile order is unchanged. Tier 1 by the behavioural clause; `.fsi`/baselines intentionally unchanged and recorded (FR-013).

## Complexity Tracking

> No constitution violations. Section intentionally empty.

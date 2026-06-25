# Implementation Plan: Symbology Label / Glyph Text Channel (M7 — label/glyph-text thread)

**Branch**: `196-symbology-label-text` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/196-symbology-label-text/spec.md`

**Source design**: [`docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md`](../../docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md) — roadmap milestone **M7 (governance & breadth)**, the **label / glyph text** thread (§9 risk 1, §10.8). The two sibling M7 threads have shipped — the legibility linter ([spec 194](../194-symbology-legibility-linter/spec.md)) and the Badge/Ring grammars ([spec 195](../195-symbology-badge-ring-grammars/spec.md), which explicitly deferred label text as backlog in FR-015). This feature delivers the **third and final** M7 thread.

## Summary

Add **one optional identity-label channel** — a short text string (name / callsign / code) — to the existing fixed `Token` channel vocabulary, sited per grammar (Token / Badge / Ring) and drawn **tofu-free** through the headless render bridge's real measurer/shaper. The channel is purely additive and opt-in: a `Token` with **no** label renders **byte-identically** to today's symbol, so every pre-feature golden stays green. A single `'stats -> Token` mapping carries the label and drives all three grammars unchanged — no new per-grammar mapping. When present, the label is **fitted** to a per-grammar label region using **real text measurement** (`Scene.measureTextResolved`) so it never overflows the footprint, clips mid-glyph, or overlaps another channel.

Technical approach (grounded against the tree on 2026-06-25):

- **One new optional field on the shared `Token`.** Add `Label: string option` to the `Token` record in `src/Symbology/Symbology.fsi`/`.fs`, defaulting to `None` in `defaultToken`. This is the natural extension of the "one channel vocabulary" principle (FR-001): the same mapping drives all grammars. `None` (and empty/whitespace `Some`) emit **no text node**, so the scene — and therefore its `SceneCodec` canonical bytes — is **byte-identical** to the current symbol (FR-002/SC-003). The Token record gains a field but the *emitted Scene* for a label-free token does not change, so the `token`/`gallery`/`filmstrip` golden SHAs in `DeterminismTests` stay green.
- **No new project, no new dependency.** The label is drawn by new pure helpers **inside the existing `src/Symbology/` package** (namespace `FS.GG.UI.Symbology`, `module Symbology`), beside `token`/`badge`/`ring`. They depend only on the **already-referenced** `FS.GG.UI.Scene` text/measurement vocabulary — `FontSpec`, `Scene.measureTextResolved`, `Scene.glyphRunProof`/`buildGlyphRun` (which carry per-glyph `Missing` tofu evidence) and `Scene.group` — adding **no** rendering, raster, GL, or IO reach to the pure library (FR-014). They are *not* placed in `src/Symbology.Render/`.
- **Per-grammar label region siting (FR-003).** Each grammar gets a designated label region that does not overlap the sigil, health, or other channels: the contract is FR-003 (sited, observable, non-overlapping), the exact geometry is a design-loop detail (see Assumptions / research.md). Provisional siting: **Token** = a short caption strip below the body belly (below the health arc, screen-aligned); **Badge** = a caption band along the bottom inner edge of the frame (below the speed-pip row / health bar); **Ring** = a centred caption beneath the sigil inside the ring's inner disc. All three draw the label **screen-aligned** (heading never rotates the text).
- **Real-measurement fit (FR-005).** A pure `fitLabel` helper measures the trimmed string with `Scene.measureTextResolved text font`, then **shrinks** the font toward a floor size to fit the region width, and if still over at the floor **truncates with an ellipsis** at a measured glyph boundary (the shrink-then-truncate policy resolved in research.md R3), guaranteeing the drawn label stays within its region, never clips mid-glyph, and never overflows into an adjacent channel. Because `measureTextResolved` with no measurer is byte-identical to the pure `measureText` heuristic and the rendering edge installs a measurer whose advances match the drawn advances, the fit is a **deterministic function of the resolved measurement** (FR-008): identical labelled `Token` under a fixed provider ⇒ identical scene ⇒ identical canonical bytes.
- **Tofu-free is a render-edge property (FR-004/FR-009).** The pure helper emits the label as a `glyphRunProof` node (whose `GlyphRunData` carries the `Missing` per-glyph flags and `FallbackMode`); it **never installs and never requires a measurer** and never throws when none is present (FR-009). Tofu-free *rendering* comes from the real measurer/shaper that `Symbology.Render` (via `SkiaViewer.Fonts.installMeasurementSeam`) already installs — the same seam Controls text already rides. The render-bridge test asserts the labelled glyph run is non-tofu (`Missing = false`) under the installed measurer.
- **Safe degenerate / empty (FR-006/FR-007).** Empty or whitespace-only label ⇒ treated as no label (no node, no throw). A degenerate `Token` (`R <= 0`) carrying a label still degrades to the existing **visible placeholder** — the placeholder rule wins over the label; neither path throws.
- **Boards + linter unchanged (FR-010/FR-011/FR-012).** `gallery`/`galleryIn`/`filmstrip`/`filmstripIn`/`animate`/`animateIn` need **no signature change** — they already thread the whole `Token`, so a labelled roster renders on a review board reproducibly per grammar by construction. The legibility linter (`Legibility.score`/`scoreAnimated`) takes `Token list` and governs the **pre-attentive** channels; the label is **inspection-detail** and is *not* added to the §4 capacity table, so the linter's verdict is unchanged and grammar-independent (FR-011/SC-006). A test asserts label presence does not alter a roster's `Report`.
- **Specification-first, Tier 1.** `Symbology.fsi` gains the `Label` field (and any label-fit helper that must be public — expected: none beyond the field; fit is internal), authored and FSI-exercised before the `.fs` body; Expecto semantic tests fail-before/pass-after through the public surface; the symbology surface baseline `readiness/surface-baselines/FS.GG.UI.Symbology.txt` is regenerated to capture the new `Label` field, with **zero drift** on every other baseline and **zero change** to label-free rendering (FR-013/SC-007).
- **Loop docs (FR-015).** The `fs-gg-symbology` design-loop skill documents the label as an **opt-in inspection-detail identity channel** (when to use it; requires the real measurer for tofu-free output; keep strings short; complements, never replaces, the vector sigil), authored canonically in `src/Symbology/skill/SKILL.md` and mirrored to every skill tree, passing `scripts/check-agent-skill-parity.fsx`.

> **Standing assumption — root-cause hypotheses are unverified until exercised.**
> This is a *greenfield additive* channel, not a defect fix, so there is no root-cause map. The label's
> *scene construction* is pure scene logic with **no GL/raster/IO**, fully exercisable headlessly; its
> *tofu-free rendering* is a property of the render edge and is verified by a **real** render-bridge test
> (rasterise a labelled token through `Symbology.Render` under the installed measurer and assert the label's
> glyph run is non-tofu), not assumed from this plan. The analogue of the live-smoke mandate is an **early
> FSI/test smoke** (Foundational phase): once `Symbology.fsi` carries `Label` and a first `.fs` stub exists,
> load the public surface in FSI and confirm a hand-built labelled `Token` renders a non-empty `Scene` in each
> grammar, an unlabelled one is byte-identical to the pre-feature golden, and a degenerate labelled one returns
> the placeholder without throwing — before building out US1/US2/US3. Treat that smoke — and the render-bridge
> tofu test — not this plan's narrative, as the confirmation the channel works.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution Engineering Constraints).

**Primary Dependencies** (existing, consumed via public types only): the channel/`Token` types of `FS.GG.UI.Symbology` declared in `src/Symbology/Symbology.fsi`; transitively `FS.GG.UI.Scene` for `FontSpec`/`TextMetrics`/`Color`/`Paint`/`Scene.measureTextResolved`/`Scene.glyphRunProof`/`Scene.buildGlyphRun`/`Scene.group` (already referenced by `Symbology.fsproj`). The headless render bridge `FS.GG.UI.Symbology.Render` (`Render.toPng`) and `SkiaViewer.Fonts.installMeasurementSeam` are reused **as-is** for the tofu-free render test. **No new third-party dependency**; no new font files (FR-016).

**Storage**: None. The label channel performs no IO; its entire output is part of the returned `Scene` value (FR-008/FR-014).

**Testing**: Expecto + FsCheck, matching `tests/Symbology.Tests/`. Existing channel-presence / determinism / placeholder / gallery / linter batteries are extended to cover the label channel across all three grammars; a **render-bridge tofu test** (`tests/Symbology.Render.Tests/`) asserts the rasterised label is non-tofu under the installed measurer; a **linter-invariance test** asserts label presence does not change a roster's `Report`.

**Target Platform**: Linux/CI headless. Scene construction is pure CPU (no GL); the tofu-free render test runs through the existing `Symbology.Render` bridge (real measurer installed at the edge). Fully reproducible across processes under a fixed measurement provider (SC-004).

**Project Type**: Multi-project F# solution (`FS.GG.Rendering.slnx`). New public surface (one record field) added to the **existing `src/Symbology/` library**; extended tests in existing test projects. No new project, no new sample required (the existing `gallery`/`galleryIn` boards already render a labelled roster).

**Performance Goals**: Design-time/review tool, not a render hot path. Each label adds `O(1)` measurement + one glyph-run node per unit; a board is `O(N)`. No fps/throughput guarantee is asserted (parity with the existing grammars).

**Constraints**: **Opt-in zero-drift is the hard constraint** (FR-002/SC-003) — a label-free token's emitted scene and canonical bytes are byte-identical to the pre-feature symbol; the `token`/`gallery`/`filmstrip` goldens must stay green. **Provider-relative determinism** (FR-008/SC-004) — identical labelled `Token` under a fixed measurement provider ⇒ byte-identical scene; no wall-clock, randomness, or IO in the library. **Tofu-free at the render edge, never required by the pure library** (FR-004/FR-009) — the library emits a deterministic text node and never throws without a measurer. **Fitted, no clip/overflow** (FR-005/SC-005) — overlong labels truncate/shrink within the region; empty/whitespace ⇒ no label; `R <= 0` ⇒ placeholder. **Non-overlapping siting** (FR-003) — the label never collides with the sigil, health, or other channels. **Zero surface drift** on every baseline except `FS.GG.UI.Symbology.txt` (FR-013); **grammar-independent linter unchanged** (FR-011/SC-006).

**Scale/Scope**: M7 label/glyph-text thread only. `Symbology.fsi`/`Symbology.fs` gain the `Label` field + per-grammar label siting + an internal `fitLabel` helper (~2 edited source files); extended `Symbology.Tests` batteries + 1 render-bridge tofu test + 1 linter-invariance test + the regenerated symbology baseline + the mirrored skill label doc. Deferred (FR-016): multi-line/paragraph text, rich-text styling, auto-label-from-stats, label-bound motion, new GPU/compute path, shipping new font files.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence in this plan |
|---|---|---|
| **I. Spec → FSI → Semantic Tests → Implementation** | PASS | `Symbology.fsi` gains the `Label` field and is FSI-exercised before any `.fs` body; Expecto semantic tests call the public `token`/`badge`/`ring`/`render` surface (not internals) and fail-before/pass-after; the `.fs` bodies are written against the now-stable signature. |
| **II. Visibility in `.fsi`, not `.fs`** | PASS | The new `Label` field is declared **only** in `Symbology.fsi`; the internal `fitLabel`/per-grammar label helpers are simply omitted from the `.fsi` and carry **no** `private`/`internal`/`public` modifiers in `.fs`. The symbology surface baseline is regenerated to capture the new field (Tier 1). |
| **III. Idiomatic Simplicity** | PASS | One optional record field + pure helpers composing existing `Scene` text primitives, in the same `clamp01`/`factionColor`/`sigilScene` helper style the grammars already use; no SRTP, reflection, type providers, custom operators, or non-trivial CE. `string option` is the plainest opt-in shape; `None` is the default. Any local `mutable` (none anticipated) would be disclosed at the use site. |
| **IV. Elmish/MVU boundary** | PASS (N/A) | The label channel is **pure, stateless, IO-free** (`Token -> Scene`); no multi-step state, IO, retries, or background work, so the MVU-boundary obligation does not attach. Provider-relative purity is itself the contract the tests assert (SC-004). The render-edge measurer seam already exists and is reused, not introduced here. |
| **V. Test Evidence Mandatory** | PASS | Expecto semantic tests over the public surface, fail-before/pass-after. Scene-construction tests are all **real** (pure scene logic). The tofu-free claim is verified by a **real** render-bridge raster test through `Symbology.Render` under the real measurer — no synthetic substitute. Determinism asserted via `SceneCodec.export(...).CanonicalBytes`; zero-drift via the pinned `token`/`gallery`/`filmstrip` golden SHAs. |
| **VI. Observability & Safe Failure** | PASS | Safe failure *as a visible placeholder*: a degenerate (`R <= 0`) labelled `Token` renders the placeholder and never throws (FR-007/SC-005); empty/whitespace label degrades to no-label without throwing (FR-006). Tofu, if the edge ever lacked coverage, is **disclosed** via the glyph run's `Missing` flag (the existing fail-loud text contract), never silently drawn as a plausible-wrong glyph. |
| **Change Classification** | **Tier 1** | Adds public API surface (one `Token` field) to the existing `FS.GG.UI.Symbology` package. Requires the full chain: spec, plan, `.fsi`, **surface-baseline update**, test evidence, docs/skill update. Existing Token-rendering / linter / core surfaces show **zero drift** for label-free input (FR-012/FR-013). |
| **Engineering Constraints** | PASS | `net10.0`; F#-only; new surface in the existing **pure** package referencing only its own `Token` types + already-referenced `FS.GG.UI.Scene` text vocabulary; **no new dependency**, **no new font files** (FR-016); `.fsi` provided; surface baseline maintained; `FS.GG.UI.*` identity untouched; SkiaSharp/GL backend untouched (the label touches no new raster path — it rides the existing text/measurer seam). **No control fork**: this is symbology vocabulary, not a per-theme control copy. |

**Gate result: PASS** — no violations; Complexity Tracking left empty. Tier 1 because it adds a curated public surface (one field) to an existing package; the discipline (FSI-first, `.fsi` curation, baseline regeneration with zero drift elsewhere, zero-drift goldens for label-free output) mirrors specs 192/194/195.

## Project Structure

### Documentation (this feature)

```text
specs/196-symbology-label-text/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — field shape (string option), per-grammar label-region siting,
│                        #   fit strategy (shrink vs ellipsis-truncate), tofu-free verification path,
│                        #   zero-drift strategy, linter-invariance rationale, test-battery shape
├── data-model.md        # Phase 1 — extended Token (Label field); per-grammar label-region tables;
│                        #   the contract vs design-loop split; fit/empty/degenerate rules; check semantics
├── quickstart.md        # Phase 1 — build + FSI smoke + run tests + per-SC validation + baseline refresh
├── contracts/           # Phase 1 — the label public-surface contract (the .fsi sketch + behaviour table)
│   └── symbology-label-api.md
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/Symbology/                       # EXISTING pure package FS.GG.UI.Symbology (Scene-only)
├── Symbology.fsi                    #   EDIT — add `Label: string option` to the `Token` record; existing
│                                    #     vals/signatures UNCHANGED (boards already thread the whole Token)
├── Symbology.fs                     #   EDIT — Label=None in defaultToken; per-grammar label-region siting in
│                                    #     drawSymbol/drawBadge/drawRing; internal `fitLabel` (measure→shrink/
│                                    #     truncate→glyphRunProof); empty/whitespace→no node; placeholder wins
├── Legibility.fsi / Legibility.fs   #   UNCHANGED (label is inspection-detail; NOT added to the capacity table)
└── skill/SKILL.md                   #   EDIT — label section: opt-in inspection-detail identity channel

tests/
├── Symbology.Tests/                 # EXISTING — extend batteries to cover the label channel
│   ├── ChannelPresenceTests.fs      #   EDIT — two tokens differing only in Label produce differing bytes (US1/SC)
│   ├── DeterminismTests.fs          #   EDIT — labelled token byte-stable render-twice; Label=None goldens UNCHANGED
│   ├── PlaceholderTests.fs          #   EDIT — degenerate token WITH label → placeholder, no throw (FR-007)
│   ├── GalleryTests.fs              #   EDIT — labelled roster reproducible per grammar (FR-010)
│   ├── LegibilityTests.fs           #   EDIT — label presence does NOT change a roster's Report (FR-011/SC-006)
│   └── (new) LabelTests.fs          #   NEW file — empty/whitespace→no label; fit-within-region (overlong measured
│                                    #     ≤ region); per-grammar siting observable; register in .fsproj
└── Symbology.Render.Tests/          # EXISTING — add: rasterise a labelled token through Render.toPng under the
                                     #     installed real measurer; assert label glyph run is non-tofu (FR-004)

readiness/surface-baselines/
└── FS.GG.UI.Symbology.txt           # REGENERATE — gains the `Label` field on `Token`; ALL OTHER baselines unchanged

.claude/skills/fs-gg-symbology/      # pointer wrapper -> canonical (inherits the label-doc edit)
.agents/skills/fs-gg-symbology/      # pointer wrapper -> canonical (inherits the label-doc edit)
template/product-skills/fs-gg-symbology/SKILL.md   # EDIT — mirror the label-doc update (adapted copy)

FS.GG.Rendering.slnx                 # no new project to register (surface lands in existing Symbology.fsproj)
```

**Structure Decision**: The label is **one optional field on the shared `Token`** plus pure label-siting helpers **inside the existing `src/Symbology/` package**, not a new project and not part of `src/Symbology.Render/`. Rationale (FR-001/FR-002/FR-014): the "one channel vocabulary" principle means the label must be a field every grammar reads from the same `Token`, so a single `'stats -> Token` mapping carries it unchanged; the helpers must stay in the pure scene-only layer and consume only the already-referenced `FS.GG.UI.Scene` text/measurement vocabulary, so placing them beside `token`/`badge`/`ring` keeps the vocabulary singular and adds no project, dependency, raster reach, or font file. The board/motion functions (`gallery`/`galleryIn`/`filmstrip`/`filmstripIn`/`animate`/`animateIn`) need **no signature change** — they already thread the whole `Token`, so a labelled roster flows through them by construction. `.fsproj` compile order is unchanged. Tier 1: the symbology surface baseline is regenerated to capture the new field, with zero drift on the `Scene`/`SkiaViewer`/`Controls`/`Canvas`/`Legibility`/`Symbology.Render` baselines and zero change to label-free rendering (FR-013/SC-007).

## Complexity Tracking

> No constitution violations. Section intentionally empty.

## Implementation Status — COMPLETE (2026-06-25)

All 32 tasks in [tasks.md](./tasks.md) are done and marked `[X]`. This was the **third and final M7 thread**;
M7 (and the M0–M7 symbology roadmap) is now complete.

**Evidence:**

- **Build:** `dotnet build FS.GG.Rendering.slnx -c Debug` — succeeded, 0 warnings/0 errors.
- **Tests:** `Symbology.Tests` **177 passed** (+24 new label tests across US1/US2/US3); `Symbology.Render.Tests`
  **6 passed** (+3 tofu-free render tests); `SymbologyBoard.Tests` **11 passed** (consumer, no regression).
- **Zero drift (FR-002/SC-003):** the `token defaultToken` / `gallery` / `filmstrip` goldens are byte-unchanged
  (`token defaultToken` SHA still `0dda10bd…`). Achieved by **conditionally appending** the label node
  (`Scene option`) so a `None`/empty/whitespace label adds no element — *not* the T005 `Scene.group []` stub,
  which would have drifted the goldens.
- **Determinism (SC-004):** labelled render-twice byte-equal; labelled cross-process golden pinned `6710215b…`.
- **Tofu-free (FR-004/SC-002):** verified at the render edge — `Fonts.resolveText` → `TofuCount = 0` for Latin
  callsigns + a non-blank rasterised board through `Render.toPng`.
- **Linter invariance (FR-011/SC-006):** `Legibility.score`/`scoreAnimated` `Report` identical with/without labels.
- **Skill (FR-015):** label section authored in `src/Symbology/skill/SKILL.md`, mirrored to the template;
  `check-agent-skill-parity.fsx` → `critical=0 high=0`.

**Honest deviations / notes (see [feedback/implementation.md](./feedback/implementation.md)):**

- **FR-013 baseline:** `refresh-surface-baselines.fsx` records public **type names only**, not record fields,
  so adding `Token.Label` yields **zero baseline diff anywhere**. The field-level Tier-1 contract is the
  curated `Symbology.fsi` (which changed). Baselines were regenerated and are consistent.
- **FR-014:** `git diff src/Symbology/Symbology.fsproj` is empty — no new dependency, reference, or font asset.

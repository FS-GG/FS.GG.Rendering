# Implementation Plan: Badge & Ring Alternative Symbology Grammars (M7 — grammar-breadth thread)

**Branch**: `195-symbology-badge-ring-grammars` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/195-symbology-badge-ring-grammars/spec.md`

**Source design**: [`docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md`](../../docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md) — roadmap milestone **M7 (governance & breadth)**, the **Badge/Ring alternative grammars** thread. M1–M5 (spec 192, the Directional Token) and M6 (spec 193, the live board) are complete; the linter thread shipped as spec 194. The §4 grammar table names **three** form factors — Badge, Token, Ring — of which only the **Directional Token** was built. This feature delivers the two remaining form factors as sibling grammars behind the **same fixed channel vocabulary**. The third M7 thread (**label / glyph text**) and any grammar auto-selection stay backlog and out of scope (FR-015).

## Summary

Ship two **new pure symbol grammars** — **Badge** (a compact, screen-aligned framed emblem) and **Ring** (a centred radial gauge) — as sibling elements to the existing Directional Token. Each is a deterministic function from the existing `Token` value to a `Scene`, consuming the **same fixed channel vocabulary** with **no new per-game mapping**: one `'stats -> Token` mapping drives all three grammars unchanged. A designer can render the same roster in a **selected grammar** on a review board (gallery, and motion filmstrip where the overlay is grammar-agnostic) to A/B which form factor reads best, and the existing legibility linter (spec 194) keeps applying with a **grammar-independent** verdict — because the linter scores the `Token` channel values, which never depend on which grammar draws them.

Technical approach (grounded against the tree on 2026-06-25):

- **No new project, no new dependency.** Both grammars are new pure functions **inside the existing `src/Symbology/` package** (namespace `FS.GG.UI.Symbology`, `module Symbology`), beside the `token`/`animate`/`gallery`/`filmstrip` they sit next to. They depend only on the channel/`Token` types already declared in `Symbology.fsi` and, transitively, `FS.GG.UI.Scene` for `Color`/`Paint`/`Path`/`RadialGradient`/`arc` — **no rendering, raster, GL, or IO** added (FR-012). They are *not* placed in `src/Symbology.Render/`, the only component allowed to pull raster/host machinery.
- **Grammar selection as a value.** A new `[<RequireQualifiedAccess>] type Grammar = Token | Badge | Ring` makes the form-factor choice a first-class value (the unit of A/B comparison, FR-001/SC-001). A `render : Grammar -> Token -> Scene` dispatcher and grammar-parameterized board functions (`galleryIn`, `filmstripIn`, `animateIn`) let a review board draw a roster in a *selected* grammar (FR-008). The existing `token`/`animate`/`gallery`/`filmstrip` keep their exact signatures and **byte-identical** behaviour (FR-010/SC-006) — they are the `Grammar.Token` path.
- **Full channel siting per grammar (FR-003).** Each grammar renders *every* `Token` channel so varying any one observably alters output (SC-002): **Badge** = frame hue=faction, frame stroke width=threat, solid/dashed frame=state, interior gradient=charge, bottom health bar, speed-pip row, corner shield mount, centre sigil, class-driven outline, edge heading pip; **Ring** = outer-ring hue=faction, ring thickness=threat, solid/dashed ring=state, radial interior gradient=charge, health **arc sweep** (monotone in health, FR-007), rim speed beads, ring shield mount, centre sigil, class-driven inner glyph, heading needle. Both are **screen-aligned**: heading is a discrete indicator, not whole-body rotation (FR-006). Exact geometry is a design-loop detail (see Assumptions); the **contract** is FR-003 (every channel observable) + FR-004 (determinism).
- **Degenerate input (FR-005).** Each grammar reuses the same `R <= 0` → visible placeholder rule the Token grammar already enforces; neither raises on degenerate or otherwise valid input (SC-004).
- **Grammar-agnostic motion only (FR-014).** `animateIn` draws the selected grammar's base symbol, then applies only **centre/radius** motion overlays (the Pulse/Blink/Damage family) so the overlay is identical across grammars and deterministic. Heading-/tail-bound motions that cannot be expressed identically across grammars are deferred (Assumptions); the existing Token `animate` is untouched.
- **Linter is grammar-independent by construction (FR-009/SC-005).** `Legibility.score`/`scoreAnimated` already take `Token list` / `(Motion * Token) list` — they never see a `Grammar`. A test renders a fixed roster in each grammar (proving the *drawings* differ) yet asserts the linter returns the **identical** report. No linter change.
- **Specification-first, Tier 1.** `Symbology.fsi` gains the `Grammar` type and the new `val`s, authored and FSI-exercised before any `.fs` body; Expecto semantic tests fail-before/pass-after through the public surface; the symbology surface baseline `readiness/surface-baselines/FS.GG.UI.Symbology.txt` is regenerated to capture `Grammar` (+`Grammar+Tags`), with **zero drift** on every other baseline and **zero change** to the existing Token rendering behaviour (FR-011/SC-006).
- **Loop docs (FR-013).** The `fs-gg-symbology` design-loop skill's grammar section documents Badge and Ring as **selectable** grammars behind the same channel set (when to prefer each; that the `ChannelMap` is unchanged), authored in the canonical `src/Symbology/skill/SKILL.md` and mirrored to every skill tree, passing `scripts/check-agent-skill-parity.fsx` (SC-007).

> **Standing assumption — root-cause hypotheses are unverified until exercised.**
> This is a *greenfield additive* pair of pure functions, not a defect fix, so there is no root-cause map. The
> grammars are pure scene logic with **no GL/raster/IO**, so they are fully exercisable headlessly. The analogue
> of the live-smoke mandate is an **early FSI/test smoke** (Foundational phase): once `Symbology.fsi` carries the
> `Grammar` type + new `val`s and a first `.fs` stub exists, load the public surface in FSI (or run a single
> Expecto test) and confirm `badge`, `ring`, and `render Grammar.Badge` run end-to-end on a hand-built `Token`
> and return a non-empty `Scene` — before building out US1/US2. Treat that smoke — not this plan's narrative — as
> the confirmation the surface is usable. The grammar-independence assertion (FR-009/SC-005) is verified by a real
> test that scores one roster across all three grammars, not assumed from this plan.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution Engineering Constraints).

**Primary Dependencies** (existing, consumed via public types only): the channel/`Token` types of `FS.GG.UI.Symbology` (`Token`, `Faction`/`Klass`/`Sigil`/`TokenState`/`Motion`) declared in `src/Symbology/Symbology.fsi`; transitively `FS.GG.UI.Scene` for `Color`/`Paint`/`Path`/`PathSpec`/`RadialGradient`/`Scene.arc`/`ellipse`/`circle`/`line`/`group` (already referenced by `Symbology.fsproj`). **No new third-party dependency**; the grammars add nothing to the package's dependency closure beyond what `Symbology` already references.

**Storage**: None. The grammars perform no IO; their entire output is the returned `Scene` value (FR-004/FR-012).

**Testing**: Expecto + FsCheck, matching the existing `tests/Symbology.Tests/`. Existing channel-presence / determinism / placeholder batteries are extended to run over **all three grammars** (a `grammars` list), plus grammar-board reproducibility and a linter-grammar-independence test. A test asserts the existing **Token** golden/determinism output is **byte-unchanged** (FR-010/SC-006).

**Target Platform**: Linux/CI headless (pure CPU scene logic; no GL, no window system). Fully reproducible across processes (SC-003).

**Project Type**: Multi-project F# solution (`FS.GG.Rendering.slnx`). New public surface added to the **existing `src/Symbology/` library**; new/extended tests in existing test projects. No new project, no new sample required (an optional lightweight `samples/SymbologyBoard` grammar-compare demo supports US3 but is not a contract).

**Performance Goals**: Design-time/review tool, not a render hot path. Each grammar emits `O(channels)` scene primitives per unit; a board is `O(N · channels)`. No fps/throughput guarantee is asserted (parity with the existing Token grammar).

**Constraints**: **Purity is the hard constraint** (FR-004/FR-012) — `badge`/`ring`/`render`/`animateIn` are deterministic functions of their input with no wall-clock, randomness, or IO; rendering the same `Token` twice (in-process or across processes) yields byte-identical scene data (SC-003). **Every channel observable** (FR-003/SC-002) — no silently-dropped channel in either grammar; health on Ring is **monotone** in the value (FR-007). **Screen-aligned** (FR-006) — Badge/Ring do not rigidly rotate with heading; heading is a discrete indicator. **Safe degenerate** (FR-005/SC-004) — `R <= 0` → visible placeholder, never blank, never an exception. **Zero behavioural drift** on the existing Token grammar and its motion/gallery/filmstrip (FR-010/SC-006); **zero surface drift** on every baseline except `FS.GG.UI.Symbology.txt` (FR-011). **Grammar-independent linter** (FR-009/SC-005).

**Scale/Scope**: M7 grammar-breadth thread only. `Symbology.fsi`/`Symbology.fs` gain the `Grammar` type + `badge`/`ring`/`render`/`galleryIn`/`filmstripIn`/`animateIn` (~2 edited source files) + extended tests across the existing `Symbology.Tests` battery + 1 linter-independence test + the regenerated symbology baseline + the mirrored skill grammar doc. Deferred (FR-015): label/glyph text, grammar auto-selection, any new GPU/compute path, and bespoke per-grammar (heading-bound) motion.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence in this plan |
|---|---|---|
| **I. Spec → FSI → Semantic Tests → Implementation** | PASS | `Symbology.fsi` gains the `Grammar` type + new `val`s and is FSI-exercised before any `.fs` body; Expecto semantic tests call the public `badge`/`ring`/`render`/`animateIn`/`galleryIn`/`filmstripIn` surface (not internals) and fail-before/pass-after; the `.fs` bodies are written against the now-stable signatures. |
| **II. Visibility in `.fsi`, not `.fs`** | PASS | The new surface is declared **only** in `Symbology.fsi`; the `.fs` carries **no** `private`/`internal`/`public` modifiers on top-level bindings (per-grammar helpers are simply omitted from the `.fsi`). The symbology surface baseline is regenerated to capture the new `Grammar` type (Tier 1). |
| **III. Idiomatic Simplicity** | PASS | Pure functions over records/DUs/lists composing existing `Scene` primitives; the same `clamp01`/`lerpColor`/`factionColor`/`rotate` helper style the Token grammar already uses; no SRTP, reflection, type providers, custom operators, or non-trivial CE. Any local `mutable` (none anticipated) would be disclosed at the use site. |
| **IV. Elmish/MVU boundary** | PASS (N/A) | The grammars are **pure, stateless, IO-free functions** (`Token -> Scene`); no multi-step state, IO, retries, or background work, so the MVU-boundary obligation does not attach. Purity is itself the contract the tests assert (SC-003). |
| **V. Test Evidence Mandatory** | PASS | Expecto semantic tests over the public surface, fail-before/pass-after, all **real** (pure scene logic is fully exercisable headlessly — no GL/raster, so no synthetic substitute is needed or used). Determinism asserted by rendering the same `Token` twice and comparing canonical bytes; channel presence by varying one channel at a time. |
| **VI. Observability & Safe Failure** | PASS | Safe failure *as a visible placeholder*: a degenerate (`R <= 0`) `Token` renders the placeholder in every grammar and never throws (FR-005/SC-004). No startup/GL/IO paths exist to instrument; the placeholder is the actionable, visible signal. |
| **Change Classification** | **Tier 1** | Adds public API surface (the `Grammar` type + new `val`s) to the existing `FS.GG.UI.Symbology` package. Requires the full chain: spec, plan, `.fsi`, **surface-baseline update**, test evidence, docs/skill update. Existing Token/linter/core surfaces show **zero drift** (FR-010/FR-011). |
| **Engineering Constraints** | PASS | `net10.0`; F#-only; new surface in the existing **pure** package referencing only its own `Token` types + already-referenced `FS.GG.UI.Scene`; **no new dependency**; `.fsi` provided; surface baseline maintained; `FS.GG.UI.*` identity untouched; SkiaSharp/GL backend untouched (the grammars touch no raster path). **No control fork**: this is symbology vocabulary, not a per-theme control copy. |

**Gate result: PASS** — no violations; Complexity Tracking left empty. Tier 1 because it adds a curated public surface to an existing package; the discipline (FSI-first, `.fsi` curation, baseline regeneration with zero drift elsewhere) mirrors specs 192/194.

## Project Structure

### Documentation (this feature)

```text
specs/195-symbology-badge-ring-grammars/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — grammar-selection surface shape, per-channel siting per grammar,
│                        #   grammar-agnostic motion subset, Token zero-drift strategy, test-battery shape
├── data-model.md        # Phase 1 — Grammar type; Badge & Ring per-channel siting tables; the contract vs
│                        #   design-loop split; the new/extended public surface; check semantics
├── quickstart.md        # Phase 1 — build + FSI smoke + run tests + per-SC validation + baseline refresh
├── contracts/           # Phase 1 — the grammar public-surface contract (the .fsi sketch)
│   └── symbology-grammars-api.md
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/Symbology/                       # EXISTING pure package FS.GG.UI.Symbology (Scene-only)
├── Symbology.fsi                    #   EDIT — add `type Grammar` + `badge`/`ring`/`render`/`galleryIn`/
│                                    #     `filmstripIn`/`animateIn` vals; existing vals UNCHANGED
├── Symbology.fs                     #   EDIT — add Badge & Ring grammars + Grammar dispatch + grammar
│                                    #     boards; existing token/animate/gallery/filmstrip behaviour UNCHANGED
├── Legibility.fsi / Legibility.fs   #   UNCHANGED (linter is grammar-independent by construction)
└── skill/SKILL.md                   #   EDIT — grammar section: Badge & Ring as selectable grammars

tests/
├── Symbology.Tests/                 # EXISTING — extend batteries to run over all three grammars
│   ├── ChannelPresenceTests.fs      #   EDIT — every channel observable in Badge & Ring (SC-002)
│   ├── DeterminismTests.fs          #   EDIT — byte-identical Badge & Ring; Token output BYTE-UNCHANGED (SC-006)
│   ├── PlaceholderTests.fs          #   EDIT — degenerate Token → placeholder in Badge & Ring (SC-004)
│   ├── GalleryTests.fs              #   EDIT — galleryIn reproducible per grammar; empty/single roster
│   ├── MotionTests.fs / FilmstripTests.fs # EDIT — animateIn/filmstripIn grammar-agnostic & deterministic (FR-014)
│   └── (new) GrammarTests.fs        #   NEW file — render-dispatch + Ring health-arc monotonicity (FR-007, mandatory); register in Symbology.Tests.fsproj
└── SymbologyBoard.Tests/            # EXISTING — add: one roster lints to the SAME report across grammars (SC-005)

readiness/surface-baselines/
└── FS.GG.UI.Symbology.txt           # REGENERATE — gains `Grammar` + `Grammar+Tags`; ALL OTHER baselines unchanged

samples/SymbologyBoard/              # OPTIONAL (US3/P3) — lightweight grammar-compare board; not a contract

.claude/skills/fs-gg-symbology/      # pointer wrapper -> canonical (inherits the grammar-doc edit)
.agents/skills/fs-gg-symbology/      # pointer wrapper -> canonical (inherits the grammar-doc edit)
template/product-skills/fs-gg-symbology/SKILL.md   # EDIT — mirror the grammar-doc update (adapted copy)

FS.GG.Rendering.slnx                 # no new project to register (surface lands in existing Symbology.fsproj)
```

**Structure Decision**: The two grammars are **new pure functions inside the existing `src/Symbology/` package**, not a new project and not part of `src/Symbology.Render/`. Rationale (FR-001/FR-002/FR-012): they consume only the channel/`Token` types already declared in `Symbology.fsi`, must stay in the pure scene-only layer, and exist precisely to share **one** channel vocabulary with the Token grammar — placing them beside `token` keeps that vocabulary singular and adds no project, dependency, or raster reach. Grammar selection is a value (`type Grammar = Token | Badge | Ring`, `[<RequireQualifiedAccess>]` so `Grammar.Token` never collides with the `Token` record); `render`/`galleryIn`/`filmstripIn`/`animateIn` dispatch on it, while the existing `token`/`gallery`/`filmstrip`/`animate` remain byte-identical as the `Grammar.Token` path (FR-010/SC-006). `.fsproj` compile order is unchanged (`Symbology.fsi → Symbology.fs → Legibility.fsi → Legibility.fs`). Tier 1: the symbology surface baseline is regenerated to capture `Grammar`, with zero drift on the `Scene`/`SkiaViewer`/`Controls`/`Canvas`/`Legibility` baselines and zero change to the existing rendering behaviour (FR-011/SC-006).

## Complexity Tracking

> No constitution violations. Section intentionally empty.

## Implementation Status — COMPLETE (2026-06-25)

All 29 tasks (T001–T029) are implemented and `[X]` in [tasks.md](./tasks.md). The feature shipped exactly
as planned: two new pure grammars behind one fixed channel vocabulary, byte-stable Token path, and a
grammar-independent linter.

**Delivered surface** (`src/Symbology/Symbology.fsi`/`.fs`, additive only):
`type Grammar = Token | Badge | Ring` + `badge` / `ring` / `render` / `galleryIn` / `filmstripIn` /
`animateIn`. Both grammars reuse the Token grammar's channel helpers
(`clamp01`/`factionColor`/`lerpColor`/`strokePaint`/`chargeFill`/`sigilScene`/`shieldMount`/`placeholder`);
the existing `token`/`animate`/`gallery`/`filmstrip` bodies are **unedited**.

**Design notes worth keeping:**
- **Ring health monotonicity (FR-007)** is encoded as a fixed-start (top, screen-aligned) arc built from
  discrete lit segments — `lit = floor(24 · clamp01 Health)`. The lit count (sweep extent) is monotone
  non-decreasing in Health *and* observable through the public surface via `Scene.describe` element count,
  since no public API exposes an arc's sweep angle numerically. This is the proxy the FR-007 test asserts.
- **Screen-alignment (FR-006)** is achieved by drawing the centre sigil through `sigilScene { t with
  Heading = 0.0 }` and siting heading as a discrete edge pip (Badge) / centre needle (Ring); the
  frame/ring never rotate with heading.
- **Grammar-agnostic motion (FR-014)**: a private `agnosticOverlay` reproduces the Token `animate`
  Pulse/Blink/Damage overlay geometry on top of the selected grammar's base; Idle/Spin/Moving → static
  base. `Grammar.Token` paths delegate to the existing functions for guaranteed byte-identity.

**Evidence (T029, see `readiness/quickstart-validation.md` — local, gitignored by `specs/*/readiness/`):**
- `tests/Symbology.Tests` 🟢 131/131 · `tests/SymbologyBoard.Tests` 🟢 11/11 · `Symbology.Render.Tests` 🟢 3/3.
- Surface baseline diff is **exactly** `+ FS.GG.UI.Symbology.Grammar` + `+ …Grammar+Tags`, zero drift elsewhere (T027).
- Skill-parity check passed: critical=0, high=0 (T028).
- Token zero-drift goldens (`token`/`gallery`/`filmstrip` canonical-byte SHAs) stay green — Token grammar byte-unchanged.
- Pre-existing reds carried from baseline and untouched by this feature: `tests/Package.Tests` (8),
  `samples/ControlsGallery/ControlsGallery.Tests` (2).

**Docs (FR-013):** `src/Symbology/skill/SKILL.md` (canonical) + `template/product-skills/fs-gg-symbology/SKILL.md`
(mirror) document Badge/Ring as selectable grammars sharing one ChannelMap; the `.claude/` and `.agents/`
skill trees inherit the canonical via their pointer wrappers.

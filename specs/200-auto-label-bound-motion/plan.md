# Implementation Plan: Symbology Auto-Label & Label-Bound Motion (channel-projected labels + motion-timeline label animation)

**Branch**: `200-auto-label-bound-motion` | **Date**: 2026-06-26 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/200-auto-label-bound-motion/spec.md`

**Source design**: the symbology **M0–M7 roadmap** is complete; this lifts the **two items spec 199 explicitly deferred** (199 FR-019) — the next deferred M7 backlog thread and the direct continuation of the 196→197→198→199 label chain. Spec 196 ([label / glyph-text channel](../196-symbology-label-text/spec.md)) shipped a single-line label; 197 ([multi-line / paragraph](../197-symbology-multiline-label/spec.md)) widened it; 198 ([rich-text runs](../198-symbology-rich-text-label/spec.md)) added per-run colour/weight/size (`LabelText.Rich`); 199 ([full rich-text layout](../199-rich-text-layout/spec.md)) added per-paragraph alignment/justify, explicit paragraphs (`LabelText.Laid`), and per-run italic/underline/strike/tracking — then deferred **auto label** (derive a label from a unit's encoded state without hand-authoring each) and **label-bound motion** (animate the label's runs over the symbology motion timeline). This feature delivers both.

## Summary

Complete the label channel as a **bounded completion** — not a new subsystem, not a new channel, not a new clock. Two opt-in, layered-additive capabilities on the existing `Token.Label` channel proven in 196–199:

1. **Auto label (US1 / P1).** `Token` gains an opt-in `AutoLabel: AutoLabelSpec option`. When set (and no explicit `Label` is present), the library **projects a styled label from that `Token`'s own encoded channels** — a game-agnostic compact readout (faction code, class, state, a health/threat tier, speed, shield) — expressed in the existing 196–199 rich-text vocabulary. The projection is a **pure deterministic function of the `Token`'s channels only** (never a game's raw stats — FR-002), **overridable** (explicit `Label` always wins — FR-003), and yields **no label** when it projects to no drawable glyphs (FR-004). The resolved label flows through the **same single `labelDispatch`** the grammars already use — one resolved label, no second label channel, no per-grammar mapping. A `Token` with `AutoLabel = None` is byte-identical to spec 199.

2. **Label-bound motion (US2 / P2).** `Token` gains an opt-in `LabelMotion: LabelMotion option` binding the **resolved** label to a restrained motion vocabulary — **`TypeOn | Fade | Pulse | Scroll`** — **sampled as a pure function of the motion phase the board already supplies** (`filmstripIn` / `animateIn`). No new clock, no wall-clock, **no signature change** to any board/motion entry point (FR-005). The label-bearing symbol drawing gains an **internal** `labelPhase` parameter that **defaults to the rest phase (`0.0`)**: the static entry points (`token`/`badge`/`ring`/`render`/`gallery`/`galleryIn`) pass rest, so a motion-bound label at rest is **byte-identical to the static spec-199 label** (FR-007); only the motion entry points (`animate`/`animateIn`/`filmstrip`/`filmstripIn`) thread the **same sampled `ph`** the existing overlay already reads, so the label animates deterministically alongside the existing motion overlay. A `Token` with `LabelMotion = None` is byte-identical to spec 199 across the whole timeline.

Everything stays in the **pure scene-only layer** (FR-019): the projection composes the existing label-run / `LabelText` vocabulary; the motion is a per-phase transform of the **already-resolved** label realised with primitives the renderer already supports (`glyphRunProof` prefix reveal for type-on, paint alpha for fade, `withPerspective`/size scale for pulse, an X-offset within the fitted region for scroll). **No new scene primitive, no new font file, no GPU/compute path.** Both capabilities are **deterministic** under a fixed measurement provider + fixed phase (FR-015), **tofu-free** per run at the render edge (FR-010), **fitted at every phase** by the same wrap/cap/truncate/placeholder machinery as a hand-authored 199 label (FR-011), and **opt-in / layered-additive** (FR-008): unused, every byte matches 199 → 198 → 197 → the pre-feature symbol.

Technical approach (grounded against the post-199 tree on 2026-06-26 — `src/Symbology/Symbology.fs`/`.fsi`, `src/Symbology.Render/`):

- **Surface delta — Tier 1, anticipated by the spec (FR-020).** `Symbology.fsi` gains: an `AutoField` DU (the channel selectors the projection may read — `FactionCode | KlassCode | StateCode | HealthTier | ThreatTier | SpeedPips | ShieldFlag`); an `AutoLabelSpec` record (`{ Fields: AutoField list; Separator: string }`); a `LabelMotion` DU (`TypeOn | Fade | Pulse | Scroll`); two `None`-defaulted optional fields on `Token` (`AutoLabel: AutoLabelSpec option`, `LabelMotion: LabelMotion option`); and convenience constructors (`autoLabel`, `autoLabelSep`, `labelMotion`). `defaultToken` gains the two `None` fields. The existing `Token`/`LabelText`/`LabelRun`/`Grammar`/`Motion` types and **every board/motion entry-point signature** keep their **byte-stable** shape (FR-005). Per Constitution II every new public type/field is declared in the `.fsi`. Only the **symbology** surface baseline moves (FR-020); zero drift elsewhere.

- **Auto-label projects only from `Token` channels, resolves before dispatch (FR-001/FR-002/FR-004, US1).** A new pure `projectAutoLabel : Token -> AutoLabelSpec -> LabelText option` reads **only** the `Token`'s already-encoded channels (`Faction`/`Klass`/`State`/`Health`/`Threat`/`Speed`/`Shield`) — never a game's raw stats — and formats the requested `Fields` into a compact readout joined by `Separator`, emitted as the existing `LabelText` (a `Rich`/`Plain` value). It is a deterministic fold over the selected channels: no wall-clock, randomness, IO, or global state. An empty `Fields`, or a projection whose joined text is empty/all-whitespace, returns `None` (FR-004/FR-012 — treated as no label, exactly like an empty hand-authored label). A new `resolveLabel : Token -> LabelText option` performs the **resolution order**: `t.Label |> Option.orElseWith (fun () -> t.AutoLabel |> Option.bind (projectAutoLabel t))` — **explicit wins**, exactly one resolved label or none (FR-003). The three per-grammar label helpers feed `resolveLabel t` (not `t.Label`) into the unchanged `labelDispatch`, so the projected label rides the **same** fit/wrap/cap/decoration path as a hand-authored label and renders in every grammar with **zero** new per-grammar mapping (FR-001/FR-009/FR-011).

- **Label-bound motion is a per-phase transform of the resolved label, threaded internally (FR-005/FR-006/FR-007, US2).** The label-bearing internal drawers (`tokenLabelNodes`/`badgeLabelNodes`/`ringLabelNodes` and their `drawSymbol`/`drawBadge`/`drawRing` callers) gain a `labelPhase: float` parameter **defaulting to `restPhase = 0.0`**. A new `motionLabelNodes` wraps `labelDispatch`: it resolves the static label nodes once, then applies the bound `LabelMotion` as a **pure function of the normalised phase** (`ph = phase - floor phase`, the existing convention):
  - **`TypeOn`** — reveal a measured **prefix** of the resolved text proportional to `ph` (whole-glyph steps so it never clips mid-glyph); at `ph = 0.0` (rest) the full label is shown ⇒ **identity** ⇒ byte-identical to static (the rest frame reveals everything; non-rest frames reveal a growing prefix — see research for the rest-is-full vs rest-is-empty decision and its zero-drift consequence).
  - **`Fade`** — ramp the run paint **alpha** by `ph`; rest = full alpha = the static paint (identity).
  - **`Pulse`** — oscillate a **size/alpha** factor by `sin(ph·2π)`; the scale factor is `1.0` at rest (identity) and is **capped so the scaled label still fits** the region (FR-011).
  - **`Scroll`** (overflow ticker) — translate an **overlong** line by an X-offset within the fitted region; rest offset = `0.0` (identity); the line scrolls **within** the region across phases, clipped to the region by the **same** fit extent so it never overflows into adjacent channels and the drawn line count stays capped (FR-011).
  Each branch's **rest value is the identity transform**, so `motionLabelNodes` at `restPhase` emits the **exact** `labelDispatch` node list (FR-007). When `LabelMotion = None`, the drawers call `labelDispatch` directly (no transform) ⇒ zero drift at every phase (FR-008). The static entry points pass `restPhase`; `animate`/`animateIn`/`filmstrip`/`filmstripIn` pass the **same `ph`** they already compute for the overlay into the base-symbol label draw, so the label animates with the symbol — **no public signature change** (FR-005).

- **Composition: project first, then animate (FR-013).** A `Token` MAY set **both** `AutoLabel` and `LabelMotion`. `resolveLabel` runs first (pure function of channels, explicit-or-projected), then `motionLabelNodes` animates the **resolved** label per phase — composition stays deterministic, fitted, and tofu-free.

- **Structural, layered zero-drift (FR-008/SC-003).** The resolution + phase threading is arranged so the unused paths are **structurally** the spec-199 paths:
  | Token | Path | Guarantee |
  |---|---|---|
  | `AutoLabel = None`, `LabelMotion = None`, any `Label` | `resolveLabel = t.Label`; `labelPhase = restPhase`; `LabelMotion = None` ⇒ `labelDispatch` directly | **byte-identical to spec 199** (and through 199's chain to 198/197/pre-feature) |
  | `AutoLabel = Some _`, `Label = None` | `resolveLabel = projectAutoLabel` ⇒ a `LabelText` ⇒ `labelDispatch` | projected label on the existing fit path |
  | `AutoLabel = Some _`, `Label = Some _` | `resolveLabel = t.Label` (explicit wins) | byte-identical to the same `Token` without `AutoLabel` |
  | `LabelMotion = Some _`, sampled at rest (`ph = 0.0`) | `motionLabelNodes` rest = identity | byte-identical to the static spec-199 label |
  | `LabelMotion = Some _`, non-rest phase | `motionLabelNodes` per-phase transform | the animated label |
  A `Token` opting into neither reaches `labelDispatch` with `resolveLabel t = t.Label` and `labelPhase = restPhase`, hitting the **exact** spec-199 dispatch (FR-008).

- **Degenerate token + visible placeholder unchanged (FR-014).** The `R <= 0` guard in `drawSymbol`/`drawBadge`/`drawRing` still emits the **visible placeholder** and suppresses the label before any resolution/animation runs — placeholder wins over auto/motion, never a blank scene, never an exception.

- **Tofu-free is a render-edge property, per resolved run, at every phase (FR-010/FR-016).** Every drawn glyph is still a `Scene.glyphRunProof` carrying per-glyph `Missing`; type-on draws a prefix of proofs, fade/pulse change paint, scroll offsets position — glyphs are unchanged, so tofu-freeness is preserved across phases. The pure library **never installs and never requires a measurer** and never throws without one; on the pure-fallback path it still emits the resolved label's styled text nodes with the recorded phase. A render-bridge test rasterises an **auto-derived + motion-bound** label through `Symbology.Render` under the real measurer and asserts every run is non-tofu at sampled phases.

- **Boards + linter unchanged (FR-017/FR-018).** `render`/`gallery`/`galleryIn`/`filmstrip`/`filmstripIn`/`animate`/`animateIn` keep their signatures (they thread the whole `Token`), so an auto-labelled / motion-bound roster renders per grammar and across the timeline by construction. The legibility linter does **not** read `Token.Label`/`AutoLabel`/`LabelMotion` (verified post-199: no `Label` reference in `Legibility.fs`), so its verdict stays grammar-independent and unchanged by however the label is derived or animated — a test asserts auto/motion does not alter a roster's `Report`.

- **Author-supplied, guidance-governed (FR-018).** The projection reads channels and emits author-default label ink/style; nothing is re-mapped or rejected at runtime. The pre-attentive channels remain the faction/state palettes, the label stays inspection-detail, and "keep auto-projections compact, motion restrained, don't impersonate the faction/state encodings or crowd the region" is a **skill caveat**, not a runtime rule (FR-018).

- **Loop docs (FR-022).** The `fs-gg-symbology` skill documents auto-label (when to auto-derive vs hand-author, channel-only + overridable, compact) and label-bound motion (the four kinds, deterministic phase sampling, rest = static, restrained), that both require the real measurer for tofu-free output, how surplus/overflow degrades, and that they complement (never replace) the vector sigil — authored canonically in `src/Symbology/skill/SKILL.md`, mirrored, passing `scripts/check-agent-skill-parity.fsx`.

> **Standing assumption — behaviour is unverified until exercised.**
> This is a *greenfield-additive* completion, not a defect fix, so there is no root-cause map. The
> projection and motion are pure scene logic with **no GL/raster/IO**, fully exercisable headlessly; their
> *tofu-free rendering per run at every phase* is a render-edge property verified by a **real**
> render-bridge test (rasterise an auto-derived + motion-bound token through `Symbology.Render` under the
> installed measurer at sampled phases and assert every run is non-tofu), not assumed from this plan. The
> analogue of the live-smoke mandate is an **early FSI/test smoke** (Foundational phase): once
> `AutoField`/`AutoLabelSpec`/`LabelMotion`, the two `Token` fields, `projectAutoLabel`, `resolveLabel`,
> the `labelPhase`-threaded drawers, and `motionLabelNodes` exist, load the public surface in FSI and
> confirm a `Token` opting into neither is byte-identical to 199, an `AutoLabel` `Token` with no explicit
> label renders a channel-projected label, the same `Token` with an explicit label renders the explicit
> one (explicit wins), two `Token`s differing in one projected channel produce different auto-labels while
> identical channels produce identical bytes, a motion-bound label at `phase = 0.0` equals the static
> label, a non-rest phase differs, a `Scroll` over an overlong line stays within the region, and a
> degenerate (`R <= 0`) auto/motion token returns the placeholder without throwing — before building out
> US1/US2/US3. Treat that smoke — and the render-bridge tofu test — not this plan's narrative, as the
> confirmation the capabilities work.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution Engineering Constraints).

**Primary Dependencies** (existing, consumed via public types only): the `Token`/`Grammar`/`LabelText`/`LabelRun`/`Motion` types of `FS.GG.UI.Symbology` (`src/Symbology/Symbology.fsi`); transitively `FS.GG.UI.Scene` for `Color` (incl. alpha for fade), `FontSpec` (`{ Family; Size; Weight: int option }` — no per-glyph motion field, confirming motion is synthesised in the pure layer), `Scene.glyphRunProof`/`Scene.measureTextResolved` (prefix reveal + measured fit), `Scene.withPerspective` (pulse scale, reused from 199's slant), `Paint.fill`/`Paint.stroke` (fade alpha + decoration) — all already used by the grammars. The headless render bridge `FS.GG.UI.Symbology.Render` (`Render.toPng`) and the real measurer the `Symbology.Render` edge installs are reused **as-is** for the tofu-free render test. **No new third-party dependency, no new font files** (FR-019/FR-021).

**Storage**: None. The projection and motion perform no IO; their entire output is part of the returned `Scene` value (FR-015/FR-019).

**Testing**: Expecto + FsCheck, matching `tests/Symbology.Tests/`. Existing channel-presence / determinism / placeholder / gallery / linter / label / multi-line / rich-text / laid-out batteries stay green (the new `Token` fields are additive `None` defaults — fixtures built via `Symbology.defaultToken` + `with`-copy are unaffected; any raw `Token` record literal gains the two new `None` fields, value-preserving). New batteries: an **auto-label** battery (projection reads channels; differing projected channel ⇒ differing label; identical channels ⇒ byte-identical; explicit overrides auto; degenerate projection ⇒ no label; opt-out ≡ 199 byte-identity); a **label-motion** battery (each kind animates with phase; rest phase ≡ static 199 byte-identity; scroll stays within region + capped lines; no-motion ≡ 199 across the timeline; deterministic per-phase); a **composition** test (auto + motion together); a **render-bridge tofu test** (rasterise an auto-derived + motion-bound label at sampled phases; every run non-tofu); a **linter-invariance test** (auto/motion does not change a roster's `Report`); and a new pinned auto-label / motion-frame cross-process golden.

**Target Platform**: Linux/CI headless. Projection + motion are pure CPU (no GL); the tofu-free render test runs through the existing `Symbology.Render` bridge (real measurer installed at the edge). Fully reproducible across processes under a fixed measurement provider + fixed phase (SC-004).

**Project Type**: Multi-project F# solution (`FS.GG.Rendering.slnx`). The change is **internal to the existing `src/Symbology/` library** plus a curated **`.fsi` surface addition** (the two new DUs + record, two `Token` fields, ctors); extended tests in `tests/Symbology.Tests/` and `tests/Symbology.Render.Tests/`. No new project, no new sample (the existing `galleryIn`/`filmstripIn` boards already render an auto-labelled / motion-bound roster by threading the whole `Token`).

**Performance Goals**: Design-time/review tool, not a render hot path. Auto-label adds `O(fields)` to build the projected `LabelText` (then the existing `O(words)` fit); motion adds `O(1)` per-phase arithmetic over the already-resolved nodes. A board is `O(N)`; a filmstrip is `O(N × samples)`. No fps/throughput guarantee is asserted (parity with the existing grammars/timeline).

**Constraints**: **Layered zero-drift is the hard constraint** (FR-008/SC-003) — a `Token` opting into neither capability, a `Plain`/`Rich`/`Laid` label, and a no-label token are byte-identical to the 199/198/197/pre-feature goldens; a **motion-bound label at the rest phase reproduces the static spec-199 label byte-for-byte** (FR-007). **Provider- and phase-relative determinism** (FR-015/SC-004) — identical channels under a fixed measurement provider ⇒ identical projection ⇒ identical bytes; identical `(Token, phase)` ⇒ identical frame; no wall-clock, randomness, or IO. **Channel-only projection, explicit override, exactly one resolved label** (FR-002/FR-003). **Tofu-free at the render edge, never required by the pure library, per run, at every phase** (FR-010/FR-016). **Fitted/capped, no mid-glyph clip, no overflow into adjacent channels, scroll within region — at every sampled phase** (FR-011/SC-005). **Pure scene-only, no new primitive/font/GPU path** (FR-019/FR-021). **No signature change to any board/motion entry point** (FR-005/FR-017). **Surface baseline moves only for the symbology package** (FR-020); **grammar-independent linter unchanged** (FR-018/SC-006); **per-game stat semantics stay the caller's** (FR-002/FR-021).

**Scale/Scope**: M7 backlog auto-label + label-bound-motion thread only. `Symbology.fsi` gains `AutoField`, `AutoLabelSpec`, `LabelMotion`, two `Token` fields, and ctors; `Symbology.fs` gains `projectAutoLabel`, `resolveLabel`, the `labelPhase` threading through the three drawers + their callers, `motionLabelNodes` (the four per-phase transforms), and the static-vs-motion entry-point wiring (~1 edited source `.fs` + its `.fsi`). Tests: additive batteries + a render-bridge tofu case + a linter-invariance assertion + a new auto/motion cross-process golden + the mirrored skill doc. The symbology surface baseline is regenerated (FR-020). Deferred (FR-021): per-game stat → label semantics inside the library, inline images, hyperlinks, lists, per-glyph styling, advanced bidi/complex-script beyond the installed measurer, any new GPU/compute path, new font files.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence in this plan |
|---|---|---|
| **I. Spec → FSI → Semantic Tests → Implementation** | PASS | The `.fsi` is edited **first** (the two DUs + record, two `Token` fields, ctors), then semantic Expecto tests over the public `token`/`badge`/`ring`/`render`/`animate`/`filmstripIn` surface (not internals) fail-before/pass-after, then the `.fs` body (`projectAutoLabel`/`resolveLabel`/`motionLabelNodes`/the `labelPhase` threading). The surface delta is intentional and curated (FR-020). |
| **II. Visibility in `.fsi`, not `.fs`** | PASS | Every new public type/field/ctor is declared in `Symbology.fsi`; the internal `projectAutoLabel`/`resolveLabel`/`motionLabelNodes`/`restPhase`/the per-phase transforms keep the existing `let private` style and are omitted from the `.fsi`. The symbology surface baseline is regenerated (FR-020); no other baseline moves. |
| **III. Idiomatic Simplicity** | PASS | Two small DUs, a two-field record, two `None`-defaulted `Token` fields, and a handful of pure helpers composing the **existing** `labelDispatch`/`measureTextResolved`/`glyphRunProof`/`withPerspective`/`Paint` primitives. The projection is a fold over selected channels; the motion transforms are arithmetic on the normalised phase (no `mutable`, mirroring the existing `animate`). No SRTP, reflection, type providers, custom operators, or non-trivial CE. The two new `Token` fields (rather than overloading `LabelText`) are justified by FR-003's **explicit-overrides-auto** requirement, which needs the projection request and the explicit label to coexist on the `Token`. |
| **IV. Elmish/MVU boundary** | PASS (N/A) | The projection and motion are **pure, stateless, IO-free** (`Token`/`phase -> Scene`); no multi-step state, IO, retries, or background work, so the MVU obligation does not attach. The motion phase is a caller-owned value (the board already exposes it), not owned state. Provider-/phase-relative purity is itself the contract the tests assert (SC-004). |
| **V. Test Evidence Mandatory** | PASS | Expecto semantic tests over the public surface, fail-before/pass-after; all **real** (pure scene logic). Tofu-free claim verified by a **real** render-bridge raster test through `Symbology.Render` under the real measurer (auto-derived + motion-bound label at sampled phases; every run non-tofu). Determinism via `SceneCodec.export(...).CanonicalBytes`; layered zero-drift via the pinned 199/198/197/pre-feature goldens (kept green — new fields are additive `None` defaults; rest phase reduces to the 199 path). **No existing assertion weakened or deleted**; new behaviour gets added assertions (resolved label = explicit when both set, projected label differs by channel, rest frame = static, scroll ≤ region, every run non-tofu at sampled phases). |
| **VI. Observability & Safe Failure** | PASS | Safe failure *as a visible placeholder*: a degenerate (`R <= 0`) auto/motion `Token` renders the placeholder and never throws (FR-014); a degenerate/empty projection and a motion-bound empty label degrade to no-label without throwing (FR-004/FR-012) at every phase. Tofu, if the edge ever lacked coverage, is **disclosed** per run via the glyph run's `Missing` flag, never silently drawn. |
| **Change Classification** | **Tier 1** | **Alters observable behaviour covered by existing specs (the label channel) AND adds public surface.** Tier 1 by both clauses. The `Symbology.fsi` surface baseline is regenerated (FR-020) with zero drift elsewhere; the discipline (spec, plan, FSI-first, semantic tests, docs/skill, zero-drift goldens for opt-out / explicit-override / rest-phase) mirrors specs 198–199. |
| **Engineering Constraints** | PASS | `net10.0`; F#-only; change internal to the existing **pure** package plus its curated `.fsi`, referencing only its own `Token`/`LabelText`/`Motion` types + already-referenced `FS.GG.UI.Scene` text/transform/paint vocabulary; **no new dependency, no new font files, no new scene primitive, no GPU/compute path** (FR-019/FR-021); baselines maintained (only the symbology surface moves, recorded); `FS.GG.UI.*` identity untouched; SkiaSharp/GL backend untouched (motion rides the existing transform/paint/text seams). **No control fork**: symbology vocabulary, not a per-theme control copy. |

**Gate result: PASS** — no violations; Complexity Tracking left empty. Tier 1 by **both** the behavioural and surface clauses; the `Symbology.fsi` baseline intentionally moves and is regenerated (FR-020), mirroring the spec-198/199 honest surface-delta note.

## Project Structure

### Documentation (this feature)

```text
specs/200-auto-label-bound-motion/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — field-shape decisions (AutoLabelSpec/AutoField projection
│                        #   selectors; LabelMotion kinds), the channel-only projection algorithm,
│                        #   explicit-overrides-auto resolution order, the rest-phase=identity decision
│                        #   and how labelPhase is threaded without a signature change, the four motion
│                        #   transforms + fit-at-every-phase, zero-drift dispatch, render-edge tofu path,
│                        #   test-battery shape
├── data-model.md        # Phase 1 — the new/extended types; resolveLabel order; the per-phase transforms;
│                        #   the labelPhase threading table; per-grammar budgets/regions (reused);
│                        #   contract vs design-loop split
├── quickstart.md        # Phase 1 — build + FSI smoke + run tests + per-SC validation + surface baseline regen
├── contracts/           # Phase 1 — the auto-label / label-motion contract (the .fsi delta + behaviour table)
│   └── symbology-auto-label-motion-api.md
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/Symbology/                       # EXISTING pure package FS.GG.UI.Symbology (Scene-only)
├── Symbology.fsi                    #   EDIT — ADD `type AutoField = FactionCode | KlassCode | StateCode
│                                    #     | HealthTier | ThreatTier | SpeedPips | ShieldFlag`;
│                                    #     ADD `type AutoLabelSpec = { Fields: AutoField list; Separator: string }`;
│                                    #     ADD `type LabelMotion = TypeOn | Fade | Pulse | Scroll`;
│                                    #     ADD 2 optional fields on `Token` (`AutoLabel: AutoLabelSpec option`,
│                                    #     `LabelMotion: LabelMotion option`); ADD ctors
│                                    #     `autoLabel`/`autoLabelSep`/`labelMotion`. Existing types + ALL
│                                    #     board/motion entry-point signatures byte-stable.
├── Symbology.fs                     #   EDIT — ADD `projectAutoLabel` (pure Token-channel fold → LabelText option),
│                                    #     `resolveLabel` (explicit `Label` orElse projected `AutoLabel`),
│                                    #     `restPhase = 0.0`, `motionLabelNodes` (TypeOn prefix reveal / Fade alpha /
│                                    #     Pulse sin scale-capped-to-fit / Scroll in-region offset, each rest=identity);
│                                    #     THREAD a `labelPhase` (default `restPhase`) through `tokenLabelNodes`/
│                                    #     `badgeLabelNodes`/`ringLabelNodes` + `drawSymbol`/`drawBadge`/`drawRing`;
│                                    #     feed `resolveLabel t` (not `t.Label`) into dispatch; static entry points
│                                    #     pass `restPhase`, `animate`/`animateIn`/`filmstrip`/`filmstripIn` pass the
│                                    #     sampled `ph`. `defaultToken` gains the 2 `None` fields. Placeholder guard,
│                                    #     `labelDispatch`, budgets (Token≤3, Badge≤2, Ring≤2) UNCHANGED.
├── Legibility.fsi / Legibility.fs   #   UNCHANGED (does NOT read Token.Label/AutoLabel/LabelMotion)
└── skill/SKILL.md                   #   EDIT — auto-label + label-motion section: auto-derive vs hand-author,
                                     #     channel-only + overridable + compact, the four motion kinds + deterministic
                                     #     phase sampling + rest=static + restrained, requires real measurer for
                                     #     tofu-free, don't impersonate faction/state or crowd the region, how
                                     #     surplus/overflow degrades, complements the sigil

tests/
├── Symbology.Tests/                 # EXISTING — additive (new Token fields default None; rest phase reduces
│   │                                #   to the 199 path; existing goldens stay green)
│   ├── ChannelPresenceTests.fs      #   EDIT — ADD: two Tokens differing in one projected channel ⇒ differing
│   │                                #     auto-label bytes; identical channels ⇒ byte-identical auto-label
│   ├── DeterminismTests.fs          #   EDIT — ADD auto/motion render-twice byte-equal + NEW pinned auto-label /
│   │                                #     motion-frame cross-process golden; existing 199/198/197 goldens UNCHANGED
│   ├── PlaceholderTests.fs          #   EDIT — ADD degenerate token WITH an auto / motion label → placeholder
│   ├── GalleryTests.fs              #   EDIT — ADD auto-labelled / motion-bound roster reproducible per grammar +
│   │                                #     filmstripIn frames reproducible (FR-017)
│   ├── LegibilityTests.fs           #   EDIT — ADD auto/motion presence does NOT change a roster's Report
│   ├── RichLabelTests.fs            #   EDIT — ADD opt-out ≡ 199 byte-identity with the two new fields present-as-None
│   ├── (new) AutoLabelTests.fs      #   NEW — auto-label battery: projection reads channels; differing channel ⇒
│   │                                #     differing label; identical ⇒ byte-identical; explicit overrides auto
│   │                                #     (exactly one resolved label); empty/whitespace/degenerate projection ⇒
│   │                                #     no label, no throw; opt-out ≡ 199. Register in .fsproj
│   └── (new) LabelMotionTests.fs    #   NEW — label-motion battery: each kind (TypeOn/Fade/Pulse/Scroll) advances
│                                    #     with phase; rest phase ≡ static 199 byte-identity; scroll over overlong
│                                    #     content stays within region + capped line count; no-motion ≡ 199 across
│                                    #     the timeline; deterministic per-phase (same phase ⇒ same bytes); auto +
│                                    #     motion composition. Register in .fsproj
└── Symbology.Render.Tests/          # EXISTING — extend RenderLabelTests.fs: rasterise an AUTO-DERIVED + MOTION-BOUND
                                     #     labelled token through Render.toPng under the real measurer at sampled
                                     #     phases; assert EVERY run is non-tofu (TofuCount = 0) and the board is
                                     #     non-blank (FR-010)

readiness/surface-baselines/
└── FS.GG.UI.Symbology.*            # REGEN — the symbology surface baseline moves (AutoField/AutoLabelSpec/
                                     #   LabelMotion, the 2 Token fields, ctors); EVERY OTHER package baseline
                                     #   UNCHANGED (FR-020, recorded)

.claude/skills/fs-gg-symbology/      # pointer wrapper -> canonical (inherits the auto-label/motion-doc edit)
.agents/skills/fs-gg-symbology/      # pointer wrapper -> canonical (inherits the auto-label/motion-doc edit)
template/product-skills/fs-gg-symbology/SKILL.md   # EDIT — mirror the auto-label/motion-doc update (adapted copy)

FS.GG.Rendering.slnx                 # no new project (change lands in existing Symbology.fsproj)
```

**Structure Decision**: Auto-label and label-bound motion are a **layered enrichment of the single existing label channel** inside `src/Symbology/` — **not** a new channel, project, markup grammar, or motion clock. Two decisions keep the prior zero-drift paths **structurally** byte-stable:

1. **Auto-label is two `Token` fields resolving into the existing `LabelText`** (`AutoLabel` projection request + the existing explicit `Label`), not a new `LabelText` case. FR-003 requires the projection request and an explicit label to **coexist** on the `Token` so explicit can override auto; a single `LabelText` slot cannot carry both. `resolveLabel` collapses them to **one** `LabelText option` that feeds the **unchanged** `labelDispatch`, so the projected label rides the exact 196–199 fit/wrap/cap/decoration path in every grammar — honouring "one shared label channel, one resolved label, no second channel" (FR-001) while keeping the per-game `'stats -> Token` mapping the caller's (FR-002).

2. **Label-bound motion is one `Token` field + an internal `labelPhase` thread**, not a new entry point. The board already exposes the phase via `filmstripIn`/`animateIn`; binding the resolved label to that phase needs only an **internal** `labelPhase` parameter (default `restPhase = 0.0`) on the symbol drawers — the static entry points pass rest (byte-identical to 199), the motion entry points pass the **same** sampled `ph` they already compute. **No board/motion signature changes** (FR-005); rest-phase = identity makes the static frame byte-stable (FR-007); `LabelMotion = None` skips the transform entirely (zero drift, FR-008).

The cost — a curated `.fsi` surface delta + additive (not value-changing) fixture touch-ups (every `Token` literal gains two `None` fields) — is anticipated by the spec (FR-020) and confined to the symbology package (the only `Token.Label` consumer). Tier 1 by both clauses; the symbology surface baseline is regenerated and recorded (FR-020).

## Complexity Tracking

> No constitution violations. Section intentionally empty.

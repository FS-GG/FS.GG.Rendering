# Implementation Plan: FS.GG.UI runtime ergonomics polish

**Branch**: `241-runtime-ergonomics` | **Date**: 2026-07-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/241-runtime-ergonomics/spec.md`

## Summary

Three additive, non-architectural ergonomics fixes to the FS.GG.UI runtime surface, resolving the
`Polish` child (**#74**) of the Space Invaders epic (**.github#165**):

- **§3.4 collision (doc-only)** — a consumer's own `Msg.KeyDown`/`KeyUp` collides with the shipped
  public contract `FS.GG.UI.KeyboardInput.KeyboardMsg` cases `KeyDown of KeyId` / `KeyUp of KeyId`.
  Remedy: add these to the existing `docs/product.md` collision guidance. **No `.fsi` change** —
  `[<RequireQualifiedAccess>]` on `KeyboardMsg` is rejected because it is a shipped public contract
  and would force every existing unqualified use (package, samples, consumers) to change (violates
  FR-002's regression constraint).
- **§3.5 no-op aliases (library + template + docs)** — the product `update` returns
  `model, []` (`AdapterCommand<'msg> = AdapterEffect<'msg> list`) and `subscriptions` returns `[]`
  (`AdapterSubscription<'msg> list`). Add product-facing `Cmd.none`/`Sub.none` no-ops on the
  **`FS.GG.UI.Controls.Elmish`** package (the adapter the product already references), consume them
  in the product template, and surface them in `docs/product.md` + the `fs-gg-elmish` product skill.
- **§3.6 measureText (docs/skill only)** — the pure host-independent
  `FS.GG.UI.Scene.measureText : string -> FontSpec -> TextMetrics` **already ships and is already in
  the packed api-surface** (`api-surface/Scene/Scene.fsi:489`). The gap is discoverability: surface
  it in `docs/product.md` + a product skill with a worked HUD self-positioning idiom. **No new
  measurer** (FR-005).

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> The three friction points were reproduced against the current tree (KeyboardMsg collision source,
> the `[]` returns in `template/base/src/Product/Model.fs`, the already-shipped `Scene.measureText`).
> `/speckit-tasks` MUST still schedule an **early live compile/smoke** in the Foundational phase that
> scaffolds a `game` product and reproduces the §3.4 collision + confirms the §3.5 alias compiles and
> the §3.6 idiom places HUD text, before the guidance is finalized.

## Technical Context

**Language/Version**: F# on `net10.0`. Deliverables: one small `.fs`/`.fsi` addition to
`src/Controls.Elmish` (the `Cmd.none`/`Sub.none` no-ops), Markdown (`docs/product.md`, one product
`SKILL.md`), F# template edits (`template/base/src/Product/*.fs`), and Package/surface test edits.

**Primary Dependencies**: none new. Reuses existing `AdapterCommand`/`AdapterSubscription`
(`FS.GG.UI.Controls.Elmish`), `Scene.measureText` (`FS.GG.UI.Scene`), and `KeyboardMsg`
(`FS.GG.UI.KeyboardInput`). No new package references; no version bump.

**Storage**: N/A.

**Testing**: Expecto — `SurfaceAreaTests` (the Principle II baseline gate; the new `Cmd`/`Sub`
modules add two exported names to `readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt`), a new
behavioral test proving `Cmd.none = []`/`Sub.none = []` and the template compiles, and the Package
skill/currency tests if the `fs-gg-elmish` skill body is touched. Live: scaffold a `game` product,
reproduce §3.4 pre-fix, confirm §3.5/§3.6 post-fix.

**Target Platform**: cross-platform library + template; guidance materializes into generated
products' `.agents/skills/` and `docs/`.

**Project Type**: library-surface + product-template + guidance polish within the FS.GG.UI product.

**Performance Goals**: N/A (aliases are compile-time; `measureText` is the existing pure heuristic).

**Constraints**: additive and behavior-preserving (FR-006) — no change to rendering output, input
dispatch order, or the Elmish update/subscription contract. `Cmd.none`/`Sub.none` are exactly `[]`
(law: `Cmd.none = ([] : AdapterCommand<'msg>)`). The new module names `Cmd`/`Sub` must not degrade
the collision story the feature improves: generated products use `AdapterCommand` and do **not**
`open Elmish`, so `Cmd.none` resolves unambiguously; the docs note the qualified fallback for a
product that also opens Fable.Elmish.

**Scale/Scope**: 1 `.fsi` + 1 `.fs` edit (Controls.Elmish); 1 surface baseline (+2 lines); 1 new
behavioral test; `template/base/src/Product/Model.fs` (+`EvidenceCommands.fs`) `[]` → `Cmd.none`/
`Sub.none`; `docs/product.md` (+collision line, +measureText idiom, +alias note); one product
`SKILL.md` (`fs-gg-elmish`, and a HUD-measure line in `fs-gg-scene`/`fs-gg-layout`/`fs-gg-game-core`).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

- **I. Spec → FSI → Semantic Tests → Implementation** — ✅ Honored. §3.5 adds public surface, so the
  order is: `.fsi` sketch (contracts/adapter-noop.md) → FSI-exercised law (`Cmd.none = []`) →
  semantic test (fails before, passes after) → implementation. §3.4/§3.6 are docs; their "test" is
  the collision-guidance/currency check + the live scaffold reproduction.
- **II. Visibility Lives in `.fsi`** — ✅ The `Cmd.none`/`Sub.none` no-ops are declared in
  `src/Controls.Elmish/ControlsElmish.fsi`; the surface-area baseline
  `readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt` is refreshed (via
  `scripts/refresh-surface-baselines.fsx`) and the drift gate `SurfaceAreaTests` stays green.
  §3.4/§3.6 add **no** F# surface.
- **III. Idiomatic Simplicity** — ✅ The no-ops are one-line values over the existing effect-list
  model; no justification-required feature. The §3.4 remedy is the *simpler* of the two options
  (docs, not an attribute with package-wide blast radius).
- **IV. Elmish/MVU Is the Boundary** — ✅ Reinforced, not crossed: `Cmd.none`/`Sub.none` make the MVU
  no-op explicit at the product's `update`/`subscriptions` boundary. No new stateful/I/O path.
- **V. Test Evidence Is Mandatory** — ✅ Real evidence only: the new behavioral test fails before
  (`Cmd`/`Sub` don't exist) and passes after; the surface baseline fails on undeclared drift; the
  live scaffold reproduction is a real compile, not a fixture. No synthetic evidence.
- **VI. Observability and Safe Failure** — ✅ N/A for runtime paths. `measureText` stays pure and
  conservative (never narrower than drawn); the surface-drift gate fails loudly on undeclared API.
- **Change Classification** — **Tier 1 (contracted change)** because §3.5 adds public API to the
  `FS.GG.UI.Controls.Elmish` package. Full chain required: `.fsi` + surface baseline + semantic test
  + docs. §3.4 and §3.6 are additive documentation/guidance (no contract surface changes) carried
  under the same feature.

**Result: PASS.** No violations; Complexity Tracking table omitted.

## Project Structure

### Documentation (this feature)

```text
specs/241-runtime-ergonomics/
├── plan.md              # This file
├── research.md          # Phase 0 — the three remedy decisions (doc-vs-attribute, alias placement, verify-before-add)
├── data-model.md        # Phase 1 — the no-op values, KeyboardMsg collision entry, measureText/TextMetrics idiom
├── quickstart.md        # Phase 1 — validate end-to-end (surface gate, behavioral test, live scaffold repro)
├── contracts/
│   ├── adapter-noop.md  # Phase 1 — Cmd.none / Sub.none .fsi contract + laws
│   └── guidance.md      # Phase 1 — the product.md collision line + measureText idiom + skill-surfacing contract
├── checklists/
│   └── requirements.md  # spec quality checklist (from /speckit-specify)
└── tasks.md             # Phase 2 — /speckit-tasks (NOT created here)
```

### Source Code (repository root)

```text
# §3.5 — product-facing no-op aliases (Tier 1 public surface)
src/Controls.Elmish/ControlsElmish.fsi         # + module Cmd { val none: AdapterCommand<'msg> }
                                               # + module Sub { val none: AdapterSubscription<'msg> list }
src/Controls.Elmish/ControlsElmish.fs          # implementations: none = []  (paired, no access modifiers per Principle II)
readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt   # refreshed: +FS.GG.UI.Controls.Elmish.Cmd, +.Sub

template/base/src/Product/Model.fs             # update/subscriptions: model, [] -> model, Cmd.none ; [] -> Sub.none
template/base/src/Product/EvidenceCommands.fs  # Init/adapter [] -> Cmd.none where it is the command no-op

# §3.4 — collision guidance (doc-only, no .fsi change)
template/base/docs/product.md                  # + KeyboardMsg.KeyDown/KeyUp to the collision list (beside Text/CloseRequested/Rect)

# §3.6 — surface the already-shipped pure measureText (docs/skill only; api-surface already carries it)
template/base/docs/product.md                  # + a HUD self-positioning idiom using Scene.measureText -> TextMetrics
template/product-skills/fs-gg-scene/SKILL.md   # + HUD-measure line pointing at Scene.measureText (and/or fs-gg-game-core)

# Guidance for §3.5 aliases
template/product-skills/fs-gg-elmish/SKILL.md  # show update returns model, Cmd.none and subscribe returns Sub.none

# Tests
tests/Package.Tests/SurfaceAreaTests.fs        # baseline drift gate (reads refreshed .txt; no code edit expected)
tests/Package.Tests/<new-or-existing>.fs       # NEW behavioral: Cmd.none = [] ; Sub.none = [] ; template snippet compiles
tests/Package.Tests/Feature224SkillCatalogCurrencyTests.fs  # only if a skill id/body currency check applies
```

> **Scope note.** Two of three items are pure surfacing (§3.4 doc line; §3.6 the helper already
> ships + is already packed). The only genuinely net-new API is §3.5's two no-op values — deliberately
> the smallest durable capability that gives the consumer the requested Elmish-convention readability
> without duplicating a measurer or breaking `KeyboardMsg`.

**Structure Decision**: Put the no-op aliases on `FS.GG.UI.Controls.Elmish` (home of
`AdapterCommand`/`AdapterSubscription`, already referenced by every generated product) rather than
in the product template only — so it is a real, baseline-tracked capability, not regenerated
boilerplate. Keep §3.4 and §3.6 as guidance changes with **zero** F# surface impact. The packed
api-surface needs no change for §3.6 (`Scene.measureText` already present); the Controls.Elmish
surface is governed by the baseline txt, not the packed `api-surface/` tree, so no packed-surface
edit is required for §3.5 (recorded here so FR-007 is not misread as needing one).

## Complexity Tracking

*No Constitution violations — table omitted.*

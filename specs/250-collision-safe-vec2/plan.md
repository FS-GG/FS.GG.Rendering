# Implementation Plan: Collision-Safe Vec2/Position in the Model Template

**Branch**: `250-collision-safe-vec2` | **Date**: 2026-07-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/250-collision-safe-vec2/spec.md`

## Summary

Ship a small, **product-owned, collision-safe** 2D vector type in the **game / sample-pack** model template so
a scaffolded game model never rediscovers the `X`/`Y`/`Width`/`Height`-vs-`FS.GG.UI.Scene` field-label collision
(*Hollow Depths* §2.3, FS-GG/FS.GG.Rendering#138). The type is emitted as a new adaptable helper file
`<ProductDir>/Vec2.fs` (module `AppRoot.Geometry`), following the **exact** precedent of the existing
`Collision.fs` / `Visibility.fs` / `Grids.fs` / `LineDrawing.fs` helpers: compiled before `Model.fs`,
`Exists`-guarded so a swap can delete it, reusing the shared `FS.GG.UI.Scene` / `FS.GG.UI.Canvas` vocabulary. Its
field labels (`Vx` / `Vy`) share **zero** names with `Scene.Point` (`X`,`Y`) or `Scene.Rect` (`X`,`Y`,`Width`,`Height`),
so the record-label mis-inference that currently poisons the durable `LayoutEvidence.fs` (which `open`s both
`FS.GG.UI.Scene` and `AppRoot.Model`) is structurally impossible for a model expressed with it. The starter `Model.fs`
is re-expressed in terms of `Vec2` and demonstrates the accumulator + `stepSim` (via `FixedStep.drain`) pattern wired
to the host `Tick`. A generated-product test asserts the zero-label-overlap invariant and a clean build; the durable
governance spine, evidence tokens, and a starter swap all keep passing.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> The collision root cause (bare `X`/`Y`/`Width`/`Height` model labels leaking into the durable `LayoutEvidence.fs`
> through its `open FS.GG.UI.Scene` + `open AppRoot.Model`) is corroborated by the *Hollow Depths* §2.3 report, the
> shipped Pong starter's own `CenterX`/`CenterY` workaround + "Record-label note" comment, and the confirmed opens in
> `template/base/src/Product/LayoutEvidence.fs`. It remains provisional until reproduced. `/speckit-tasks` MUST
> schedule an **early live reproduction** in the Foundational phase — scaffold a game product, add a bare
> `X`/`Y`/`Width`/`Height` model record, and observe the real `FS3566`/`FS0039` wall — **before** any fix, so the
> mechanism (not just the symptom) is confirmed on the current template.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution Engineering Constraints).

**Primary Dependencies**: Emitted product references `FS.GG.UI.Scene` (`Point`/`Rect`) and, on the game/sample-pack
gate, `FS.GG.UI.Canvas` (`FixedStep.drain`, `Rng`). No **new** framework package or public-API surface is introduced —
`Vec2.fs` is product-template content, not a library module.

**Storage**: N/A (pure value type + template content).

**Testing**: Generated-product Expecto tests (`tests/Product.Tests/BehaviorTests.fs`, replaceable) for the Vec2
surface, the zero-label-overlap invariant, and the accumulator/`stepSim` behavior; the durable
`GovernanceTests.fs` (source-scan invariants) is unchanged. Template pack→install→instantiate→build→test composition
tests exercise the game profile. FSI transcript (constitution Principle I) sketches the `Vec2` surface before the
`.fs` body.

**Target Platform**: Cross-platform .NET; the collision-safety and simulation behavior are host-independent (no GL
required — assertions are pure).

**Project Type**: F# UI framework **template** change (generated-product source), delivered through the
`FS.GG.UI.Template` package — the same template/skill/release path as sibling helper features (246–249).

**Performance Goals**: N/A beyond determinism — `Vec2` arithmetic and `FixedStep.drain` are pure, total, and
byte-identical across runs (safe inside a replayed `update`).

**Constraints**: Field labels MUST NOT overlap `Scene.Point`/`Scene.Rect`; the durable spine
(`LayoutEvidence.fs`/`EvidenceCommands.fs`/`Program.fs`/`WindowOptions.fs`) and its evidence tokens stay green across
the change and across a starter swap; non-game profiles (`app`/`governed`/`headless-scene`) stay byte-identical.

**Scale/Scope**: One new ~1-screen product-owned file, one starter `Model.fs` re-expression + the paired
`LayoutEvidence.fs`/`EvidenceCommands.fs`/`View.fs` re-points (template-authoring re-point, tokens preserved), one
`Product.fsproj` compile item, one skill note (`fs-gg-project` / model-swap guidance), a `Vec2.fs` fragment/README, a
generated-product test, and a `FS.GG.UI.Template` republish. Coordinated with the org board (#138 → epic #137).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Spec → FSI → Semantic Tests → Implementation** — PASS (planned). The `Vec2` surface (type + `vec2`/`add`/
  `scale`/`toPoint`/`toRect`) is sketched in an FSI transcript and validated by use before the `.fs` body; semantic
  tests exercise it through the generated product. Tasks order FSI-sketch → tests → implementation.
- **II. Visibility Lives in `.fsi`** — N/A for the delivered artifact. `Vec2.fs` is **product-template source**, not a
  framework public module; the generated Product tree (`Model.fs`/`View.fs`/…) ships no `.fsi` files, so no `.fsi` or
  surface-area baseline is added. This is the same posture as the sibling `Collision.fs`/`Grids.fs` helpers. Because
  no `FS.GG.UI.*` public surface changes, **no library `.fsi`/baseline update is required** (see Change Classification).
- **III. Idiomatic Simplicity Is the Default** — PASS. A plain record + a handful of pure functions; no operators,
  SRTP, reflection, or non-trivial computation expressions. Any `mutable` in the demonstrated `stepSim` (single
  unaliased accumulator) is disclosed at the use site per the principle.
- **IV. Elmish/MVU Boundary** — PASS. The starter is already an MVU `update`; the accumulator lives in `Model`, the
  `stepSim` is a pure transition, and `FixedStep.drain` is a pure primitive invoked inside `update` on `Tick` (no
  wall-clock read). No new I/O crosses `update`.
- **V. Test Evidence Is Mandatory** — PASS (planned). Tests fail before / pass after: the zero-label-overlap assertion
  and the bare-`X`/`Y` reproduction both exercise real compilation of a generated product. Prefer real build evidence
  over synthetic; any unavoidable synthetic use is disclosed with the `Synthetic` token.
- **VI. Observability and Safe Failure** — PASS. If a naming guard/lint ships (FR-007 backstop), it emits an actionable
  diagnostic naming the offending field and the `Scene` collision rather than failing silently.

**Change Classification**: **Tier 2 for the `FS.GG.UI.*` libraries** (no public API surface added or changed — the
Canvas/Scene packages are consumed, not modified) **and a template-content change** to the `FS.GG.UI.Template`
product contract (new emitted file + starter re-expression), validated by the template composition/governance tests
and shipped via a template republish. No `.fsi`/baseline churn on the framework. Rationale recorded in
[research.md](./research.md) (Decision 1).

**Gate result**: PASS — no violations; Complexity Tracking left empty.

## Project Structure

### Documentation (this feature)

```text
specs/250-collision-safe-vec2/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output — placement + label-scheme + demo decisions
├── data-model.md        # Phase 1 output — Vec2 type, laws, interop, model deltas
├── quickstart.md        # Phase 1 output — reproduce-the-trap + verify-the-fix run guide
├── contracts/
│   └── vec2-surface.md  # Phase 1 output — product-owned Vec2 module surface + assertions
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
template/base/src/Product/
├── Vec2.fs              # NEW — product-owned `AppRoot.Geometry` (collision-safe Vec2 + interop),
│                        #        game/sample-pack only, Exists-guarded, compiled before Model.fs
├── Model.fs             # EDIT (replaceable) — game branch re-expressed in Vec2; accumulator + stepSim on Tick
├── View.fs              # EDIT (replaceable) — read positions via Vec2 (toPoint) instead of CenterX/CenterY
├── LayoutEvidence.fs    # EDIT (durable, re-point) — read Ball/entity position via Vec2; tokens unchanged
├── EvidenceCommands.fs  # EDIT (durable, re-point) — same re-point; command surface + tokens unchanged
└── Product.fsproj       # EDIT — add `<Compile Include="Vec2.fs" Condition="Exists('Vec2.fs')" />` before Model.fs

template/base/tests/Product.Tests/
└── BehaviorTests.fs     # EDIT (replaceable) — assert zero-label-overlap invariant + accumulator/stepSim behavior

template/fragments/vec2/            # NEW (if fragment-delivered like siblings) — README + src mirror
└── src/Product/Vec2.fs

# Authoring guidance (surface the pitfall where authors first meet it — FR-008)
template/base/.claude/skills/fs-gg-project/…   # or the model-swap guidance note
template/base/docs/scaffold-map.md             # add Vec2.fs to the replaceable "adaptable helper you own" list
```

**Structure Decision**: Single generated-product template tree under `template/base/src/Product/` (the concrete
generated path is `src/<ProjectName>/**`, module `Product.*`/`AppRoot.*`). `Vec2.fs` is a new **replaceable, adaptable
helper you own**, gated to `game`/`sample-pack` and `Exists`-guarded, occupying the same fsproj slot family as
`Collision.fs`/`Grids.fs`. It is used by the (replaceable) starter `Model.fs`; the durable spine keeps its scanned
tokens and compile order. Whether a parallel `template/fragments/vec2/` mirror is emitted (as siblings do) is a
Phase-0 delivery decision. No `src/**` framework library file changes.

## Complexity Tracking

> No constitution violations — section intentionally empty.

---
description: "Task list for feature 250 — collision-safe Vec2/Position in the model template"
---

# Tasks: Collision-Safe Vec2/Position in the Model Template

**Input**: Design documents from `/specs/250-collision-safe-vec2/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/vec2-surface.md](./contracts/vec2-surface.md), [quickstart.md](./quickstart.md)

**Tests**: INCLUDED. The load-bearing guarantee is a compile-time property (zero label overlap → durable spine builds
clean), so real build/test evidence is mandatory (constitution V). Tests are written to FAIL before the fix.

**Organization**: by user story (spec.md priorities). US1 (P1) is the MVP — the collision-safe type + a game model that
builds clean. US2 (P2) demonstrates the accumulator + `stepSim`. US3 (P3) surfaces the pitfall in authoring guidance.

> **Plan refinement discovered during tasking (authoritative here).** `template/base/ → ./` is copied **wholesale
> (ungated)**, so the type ships as a **game/sample-pack fragment** `template/fragments/vec2/src/Product/Vec2.fs`
> (`template.json`-gated + `Exists`-guarded fsproj item), mirroring `Collision.fs`/`Grids.fs` — **not** in
> `template/base/src/Product/`. Unlike the purely-additive siblings, the base game starter **depends on** this fragment
> (FR-003 requires the starter be expressed in the safe type); the fragment is always materialized for game/sample-pack
> (same condition as the game branch of `Model.fs`), and this intentional base→fragment dependency is documented in
> `scaffold-map.md` (T023). #138 needs **no new skill / skill-manifest entry** — only authoring-guidance edits.

## Path conventions

Framework repo paths: `template/**` (template content), `tests/**` (framework test projects), `scripts/**`,
`.template.config/template.json`. The generated product tree names the dir `src/<ProjectName>/` and the module
`AppRoot.*`; template source uses `template/base/src/Product/` and `template/fragments/vec2/src/Product/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: scaffolding + a full red/green baseline so pre-existing reds are never mistaken for regressions.

- [ ] T001 Create the fragment directory `template/fragments/vec2/src/Product/` (empty placeholder; body authored later), and the readiness dir `specs/250-collision-safe-vec2/readiness/`
- [ ] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/250-collision-safe-vec2/readiness/baseline.md` (globs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: confirm the collision mechanism on the LIVE template and draft the seams before any fix. **No user-story
work begins until this phase is complete.**

- [ ] T003 Placement/registry map: confirm the plan's live facts and record in `specs/250-collision-safe-vec2/readiness/placement-map.md` — (a) the four sibling fragment sources in `.template.config/template.json` are `template/fragments/<name>/src/ → src/`, gated `(profile == "game" || profile == "sample-pack")`, no `copyOnly`; (b) `Product.fsproj` lines 9–27 hold the `Exists`-guarded gated compile items before `Model.fs`; (c) `LayoutEvidence.fs` opens BOTH `FS.GG.UI.Scene` and `AppRoot.Model` (the collision vector), and `EvidenceCommands.fs`/`View.fs` read the starter `Ball` position fields; (d) confirm #138 adds NO skill — `grep -i vec2` in `scripts/generate-skill-manifest.fsx` catalog + `template/product-skills/` is empty, so no manifest/parity gate is touched (unlike feature 249)
- [ ] T004 **Early live smoke run (STANDING — do not omit)**: (a) materialize a `game` product from the CURRENT template and build it green (pre-change baseline); (b) **reproduce the trap** (quickstart Scenario A): add `type Enemy = { X: float; Y: float; Width: float; Height: float }` + `Enemies` to the generated `src/<ProductDir>/Model.fs`, build, and OBSERVE the real `FS3566`/`FS0039` wall originating in the untouched durable `LayoutEvidence.fs` — this confirms the mechanism (not just the symptom) the fix removes. Record live evidence (or `environment-limited` with a disclosed substitute) in `specs/250-collision-safe-vec2/readiness/mechanism-repro.md`; revert the throwaway
- [ ] T005 [P] Confirm test + evidence scaffolding both stories depend on: `tests/Canvas.Tests/` already `ProjectReference`s `Canvas.Lib` + `Scene` and has Expecto+FsCheck, so the raw `Vec2.fs` fragment compiles there — add empty `tests/Canvas.Tests/Feature250Vec2Tests.fs` and `tests/Package.Tests/Feature250CollisionSafeVec2Tests.fs`, each registered in its `.fsproj`. NOTE: `tests/Product.Tests/` is the *generated* product's project, not a framework test project — Vec2 LOGIC laws live in `tests/Canvas.Tests/`; template gating/label-invariant assertions live in `tests/Package.Tests/`
- [ ] T006 Draft the seams (compile-first, constitution I FSI-first): author an FSI transcript `scripts/vec2-prelude.fsx` sketching `type Vec2 = { Vx: float; Vy: float }` + `vec2`/`zero`/`add`/`sub`/`scale`/`clamp`/`toPoint`/`toRect` and FINALIZE the field labels (recommended `Vx`/`Vy`; any change MUST keep zero overlap with `Point`/`Rect`), per [contracts/vec2-surface.md](./contracts/vec2-surface.md) §C1; then author `template/fragments/vec2/src/Product/Vec2.fs` with the module + type + the eight signatures as compiling stubs (`failwith "TODO"` bodies), mirroring the exact namespace/module header convention of the sibling `template/fragments/grids/src/Product/Grids.fs`

**Checkpoint**: collision mechanism confirmed against a live build; Vec2 seams drafted — user-story work can begin.

---

## Phase 3: User Story 1 — A fresh game model compiles without rediscovering the Rect collision (Priority: P1) 🎯 MVP

**Goal**: ship the collision-safe `Vec2` (zero label overlap) and express the starter's entity positions in it so a
game product builds clean without touching the durable spine.

**Independent Test**: scaffold a game product built on `Geometry.Vec2`, leave `LayoutEvidence.fs` untouched → `dotnet
build` exits 0 with no `FS3566`/`FS0039` from `LayoutEvidence.fs`; the zero-label-overlap assertion is green.

### Tests for User Story 1 (write first; must FAIL before impl)

- [ ] T007 [P] [US1] Fill `tests/Canvas.Tests/Feature250Vec2Tests.fs` (compiles the raw `template/fragments/vec2/src/Product/Vec2.fs` via a `<Compile Include>`; Canvas.Tests refs Canvas+Scene): FsCheck/Expecto properties for the [data-model.md](./data-model.md) laws — (a) `toPoint (vec2 x y) = { X = x; Y = y }`; (b) `add a zero = a`, commutativity, `scale 1.0 v = v`, `scale 0.0 v = zero`; (c) `toRect` centered (`X = Vx - w/2`, `Y = Vy - h/2`, `Width = w`, `Height = h`); (d) TOTALS — non-finite/negative inputs never throw / never NaN, `clamp` with `min ≤ max` returns in-range per component; (e) DETERMINISM — repeat-run byte-identity of every op on a fixed scenario. Fails against the T006 stubs
- [ ] T008 [P] [US1] Fill `tests/Package.Tests/Feature250CollisionSafeVec2Tests.fs` — the collision-safety invariant ([contracts/vec2-surface.md](./contracts/vec2-surface.md) §C2): assert every record type declared in the game/sample-pack `Vec2.fs` fragment AND in the game branch of `template/base/src/Product/Model.fs` shares ZERO field-label names with `FS.GG.UI.Scene.Point` (`X`,`Y`) and `Rect` (`X`,`Y`,`Width`,`Height`) — prefer a source/reflection scan over the fragment + a `GovernanceTests`-style source scan of the `Model.fs` game branch. Also assert the fragment source + gated compile item are present. Fails until T009–T012 land

### Implementation for User Story 1

- [ ] T009 [US1] Implement `template/fragments/vec2/src/Product/Vec2.fs`: `type Vec2 = { Vx: float; Vy: float }` and pure/total `vec2`/`zero`/`add`/`sub`/`scale`/`clamp`/`toPoint`/`toRect` (straight-line float arithmetic, non-finite guards mirroring `FixedStep.drain`'s total posture; centered `toRect`). `open FS.GG.UI.Scene` for `Point`/`Rect` ONLY — the sole place bare `{ X = … }` literals appear in the product tree. Mark the type + bodies as the editable "yours to adapt" lines. Makes T007 pass
- [ ] T010 [US1] Add the gated compile item to `template/base/src/Product/Product.fsproj` under the existing `(profile == "game" || profile == "sample-pack")` region, BEFORE `Model.fs` and before/alongside `Collision.fs`: `<Compile Include="Vec2.fs" Condition="Exists('Vec2.fs')" />` — delete-safe (Exists guard), compile-order-scan compatible (anchors on the literal `Compile Include="X.fs"`; a new file before the six scanned files is safe per scaffold-map)
- [ ] T011 [US1] Add the fragment source to `.template.config/template.json`: `{ "condition": "(profile == \"game\" || profile == \"sample-pack\")", "source": "template/fragments/vec2/src/", "target": "src/" }` (no `copyOnly` → `sourceName` substitution rewrites `Product/` → `src/<ProductDir>/`, matching the siblings and the [[fragment-target-sourcename-rename]] fix)
- [ ] T012 [US1] Re-express the game branch of `template/base/src/Product/Model.fs` positions in `Vec2` (FR-003, no accumulator yet — that is US2): `open AppRoot.Geometry`; `type Ball = { Pos: Vec2; Velocity: Vec2 }`; carry the playfield extent as `Playfield: Vec2` (NOT `PlayfieldWidth/Height`); rewrite `stepBall`/serve/bounce/score over `add`/`scale`/`clamp`. After this, the `AppRoot.Model` game branch declares NONE of `X`/`Y`/`Width`/`Height`. (Non-game branches untouched.)
- [ ] T013 [US1] Re-point the DURABLE spine field reads (keep files + ALL scanned tokens; scaffold-map "durable — must re-point"): in `template/base/src/Product/LayoutEvidence.fs` and `EvidenceCommands.fs`, read `model.Ball.Pos.Vx/Vy` + `model.Playfield.Vx/Vy` via `Geometry` (active-item bounds via `toRect`/`toPoint`); in `View.fs` draw ball/paddles via `toPoint`. Preserve `hud-region`/`gameplay-region`/`measurement-mode`/`overlap` tokens, `RendererMode = "deterministic-scene"`, and the six-file compile order. Do NOT edit `GovernanceTests.fs`
- [ ] T014 [P] [US1] Write `template/fragments/vec2/README.md`: consumer-owned, adaptable source — yours to rename (`Vx`/`Vy`), extend (`Z`), or delete AFTER you swap `Model.fs`; states WHY the labels avoid `X`/`Y`/`Width`/`Height` (the `Scene` record-label pitfall); mirror the `template/fragments/grids/README.md` tone
- [ ] T015 [US1] Verify quickstart B on a real `game` render: `Vec2.fs` lands at `src/<ProductDir>/Vec2.fs` + compiles before `Model.fs`; add an author `type Enemy = { Pos: Vec2; Velocity: Vec2 }` using the safe vocabulary, leave `LayoutEvidence.fs` untouched → `dotnet build` exits 0 with NO `FS3566`/`FS0039` from `LayoutEvidence.fs`; `dotnet test tests/Canvas.Tests tests/Package.Tests` green (T007/T008). Confirm the durable `GovernanceTests` source-scan passes unedited. Record in `specs/250-collision-safe-vec2/readiness/us1-collision-safe.md`

**Checkpoint**: US1 delivers the MVP — the collision is structurally gone; a game model built on `Vec2` compiles clean.

---

## Phase 4: User Story 2 — The starter shows the accumulator + stepSim pattern at the edit site (Priority: P2)

**Goal**: demonstrate the fixed-step simulation pattern (`FixedStep.drain` + a `Model`-carried accumulator + a pure
`stepSim`) wired to the host `Tick`, at the file authors open first.

**Independent Test**: the shipped `Model.fs` carries `SimAccumulator` + `Vec2` positions; `update`'s `Tick` runs
`stepSim` via `FixedStep.drain`; a scripted `frameTime` sequence yields byte-identical states and the ball stays in
`Playfield`.

### Tests for User Story 2 (write first; must FAIL before impl)

- [ ] T016 [P] [US2] Extend `tests/Package.Tests/Feature250CollisionSafeVec2Tests.fs` (or a paired generated-product behavior assertion) per [contracts/vec2-surface.md](./contracts/vec2-surface.md) §C5: assert the shipped game `Model` carries `SimAccumulator: float` and entity `Pos`/`Velocity` as `Geometry.Vec2`, that `update` on `Tick` drains via `FixedStep.drain`, and that a scripted `frameTime` sequence is deterministic (byte-identical) with the ball clamped inside `Playfield`. Fails until T017

### Implementation for User Story 2

- [ ] T017 [US2] Refactor the game `Model.fs` `Tick` handling: add `SimAccumulator: float` to `Model`; on `Tick`, `let struct(steps, acc') = FixedStep.drain interval frameTime model.SimAccumulator` (feature-239 primitive, already referenced by the game/sample-pack `Product.fsproj`), run the pure `stepSim` (the T012 step logic over `Vec2`) `steps` times, carry `acc'`. Keep it MINIMAL (readable Pong, not a new engine); accumulator lives in `Model` (Elmish boundary), no wall-clock in `update`; disclose any `mutable` at the use site (constitution III). Re-point `LayoutEvidence`/behavior if the `Tick` shape changed the read surface (tokens preserved). Makes T016 pass
- [ ] T018 [US2] Verify quickstart C on a real `game` render: drive a scripted `frameTime` sequence twice, confirm byte-identical model states and in-`Playfield` clamping; `toPoint`/`toRect` round-trip. Record in `specs/250-collision-safe-vec2/readiness/us2-accumulator-stepsim.md`

**Checkpoint**: US1 + US2 both hold — the starter is collision-safe AND teaches the fixed-step loop at the edit site.

---

## Phase 5: User Story 3 — The pitfall is documented where an author first meets it (Priority: P3)

**Goal**: name the `Scene`-label collision, name `Geometry.Vec2` as the default, and state the rule, at the model-editing
site and in the swap/game-core guidance.

**Independent Test**: the `Model.fs` comment + `fs-gg-model-swap` guidance + `scaffold-map.md` all name the collision,
the safe type, and the rule; `grep` finds `Vec2.fs` classified replaceable in both swap surfaces.

### Implementation for User Story 3

- [ ] T019 [US3] Upgrade the `template/base/src/Product/Model.fs` game-branch comment: replace the current "Record-label note (we renamed to avoid it)" with a pointer to `Geometry.Vec2` as the default AND the rule — "do not put `X`/`Y`/`Width`/`Height` labels on a game record while `FS.GG.UI.Scene` is open; use `Geometry.Vec2` + `toPoint`/`toRect`," naming why (record-label inference leaks into the durable `LayoutEvidence.fs`) — at the model-editing site (FR-008)
- [ ] T020 [US3] Update BOTH swap-guidance surfaces so the helper is reachable (FR-008): (a) `template/base/docs/scaffold-map.md` — add `src/<ProductDir>/Vec2.fs` to the replaceable "adaptable helper you own" list next to `Collision.fs`/`Grids.fs`, noting it compiles before `Model.fs`, is `Exists`-guarded, AND that (uniquely) the shipped starter DEPENDS on it so deleting it requires also swapping `Model.fs`; (b) `template/product-skills/fs-gg-model-swap/SKILL.md` — add `Vec2.fs` to the "Replaceable — rewrite freely" list and note the collision rule
- [ ] T021 [P] [US3] Add a short "collision-safe positions" note to `template/product-skills/fs-gg-game-core/SKILL.md` (the accumulator/stepSim + sim-primitive skill): point at `Geometry.Vec2` for positions/velocities, the `FixedStep.drain` + `stepSim` + `Tick` pattern the starter now demonstrates, and the `Scene`-label pitfall; link `[[fs-gg-scene]]`, `[[fs-gg-model-swap]]`
- [ ] T022 [US3] Verify quickstart F: `grep -c "Vec2.fs"` ≥ 1 in both swap surfaces; the `Model.fs` comment + `fs-gg-model-swap` + `fs-gg-game-core` all name the collision + the safe type + the rule. Record in `specs/250-collision-safe-vec2/readiness/us3-guidance.md`

**Checkpoint**: all three stories independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T023 [P] Non-game non-regression (FR-010/SC-006/quickstart E): scaffold `--profile app` and `--profile governed` from the branch, confirm byte-identical output vs pre-change baseline (no `Vec2.fs`, no `FS.GG.UI.Canvas` reference), governance posture unchanged. Record in `specs/250-collision-safe-vec2/readiness/non-game-unchanged.md`
- [ ] T024 [US1] Starter-swap evidence (FR-005/C3/Decision 6): perform a full swap — replace `Model.fs`/`View.fs` with a `Vec2`-based author model, re-point only the model-field reads in `LayoutEvidence.fs`/`EvidenceCommands.fs` (tokens preserved) — and confirm `GovernanceTests` + evidence commands stay green across the swap (as in the feature-220 swap evidence). Record in `specs/250-collision-safe-vec2/readiness/starter-swap.md`
- [ ] T025 Run the full `quickstart.md` A–F end-to-end on a real `game` render; confirm every SC-001…SC-006 mapping holds; consolidate evidence under `specs/250-collision-safe-vec2/readiness/`
- [ ] T026 Re-run the baseline (`dotnet fsi scripts/baseline-tests.fsx`) and diff against T002 — confirm ZERO new reds attributable to this feature (solution + Package.Tests + samples)
- [ ] T027 [P] Capture per-phase feedback under `specs/250-collision-safe-vec2/feedback/` (process friction, generalizable-code candidates, severity) if the feedback capability is active
- [ ] T028 **Release prep (coordination note STAGED — do NOT flip until release)**: this is a Tier-1 template-CONTRACT change (the emitted product gains `Vec2.fs` + a re-shaped starter). Via the `cross-repo-coordination` skill, draft the publish-before-flip updates for the `fs-gg-ui-template` contract in `FS-GG/.github` — `registry/dependencies.yml` (contract/version + consuming edge), `registry/CHANGELOG.md` (one dated newest-first entry), `docs/registry/compatibility.md` rows — and the `FS.GG.UI.Template` republish. Stage as a coordination note in `specs/250-collision-safe-vec2/release-coordination.md`; the actual republish/flip happens at release (feature merge / speckit-merge). Close board #138 and check the epic-#137 child box on completion

---

## Dependencies & Execution Order

- **Setup (P1)** → no deps.
- **Foundational (P2)** → depends on Setup; **blocks all user stories**. T004 (live repro) MUST precede any fix.
- **US1 (P3, MVP)** → depends on Foundational. T007/T008 (failing tests) before T009–T013; T009 before T010–T013; T015 last.
- **US2 (P4)** → depends on US1 (re-expressed `Model.fs`/`Vec2` in place). T016 before T017; T018 last.
- **US3 (P5)** → depends on US1 (the type/paths it documents); independent of US2. T019/T020/T021 before T022.
- **Polish (P6)** → after the targeted stories; T028 (release) is STAGED, executed at release.

### Within stories / parallel opportunities

- `[P]` tasks touch different files: T005 ∥ (T007 ∥ T008 ∥ T014) once seams exist; T023 ∥ T027; T021 ∥ T019/T020.
- T007 (Canvas.Tests laws) and T008 (Package.Tests invariant) are independent files → parallel.

## Implementation Strategy

- **MVP = Setup + Foundational + US1.** The collision is structurally removed and a game model built on `Vec2` compiles
  clean — the whole point of #138. STOP and validate (quickstart B) before US2/US3.
- **Incremental**: US1 (collision-safe) → US2 (accumulator/stepSim demo) → US3 (guidance). Each adds value without
  breaking the previous. Release-coordination (T028) is staged now, flipped at merge (publish-before-flip).

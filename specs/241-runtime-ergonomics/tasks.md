# Tasks: FS.GG.UI runtime ergonomics polish

**Feature**: `241-runtime-ergonomics` | **Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

Resolves **FS-GG/FS.GG.Rendering#74** (P1 Rendering child of epic **FS-GG/.github#165**). Three
additive items: **§3.4** KeyboardMsg collision (doc-only), **§3.6** surface the already-shipped
`Scene.measureText` (doc/skill), **§3.5** `Cmd.none`/`Sub.none` no-ops (library + template + doc).

## Format: `[ID] [P?] [Story] Description`

- **[P]** = parallelizable (different files, no incomplete dependency).
- Story labels: **[US1]** §3.4 collision (P1), **[US2]** §3.6 measureText (P2), **[US3]** §3.5 no-ops (P3).
- Tests included (Constitution V — test evidence mandatory).

---

## Phase 1: Setup

- [ ] T001 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/241-runtime-ergonomics/readiness/baseline.md` (records the full red/green set across solution + Package.Tests + samples; flag pre-existing reds here, including the known NU1403 lockfile drift, so they are not "discovered" at merge).

---

## Phase 2: Foundational (blocking prerequisites)

- [ ] T002 Confirm the root-cause map in [plan.md](./plan.md) against the tree: `KeyboardMsg.KeyDown of KeyId`/`KeyUp` at `src/KeyboardInput/KeyboardInput.fsi:78–80`; the `model, []`/`[]` returns at `template/base/src/Product/Model.fs` (188–200, 202) + `EvidenceCommands.fs` (277, 295); `Scene.measureText` present at `src/Scene/Scene.fsi:135` **and** packed at `template/base/docs/api-surface/Scene/Scene.fsi:489`. Record any drift in [research.md](./research.md).
- [ ] T003 **Early live smoke run** (before finalizing any guidance): scaffold a `game` product, and in it (a) add `type Msg = KeyDown of KeyId | ...` + a `mapKey` returning `Some (KeyDown k)` and reproduce the `type 'KeyId' does not match 'ViewerKey'` compile error PRE-fix; (b) confirm `update` today returns `model, []` and `subscriptions` returns `[]`; (c) confirm `Scene.measureText "text" font` is callable and returns a `TextMetrics`. Record evidence (or `environment-limited` + disclosed substitute) in `specs/241-runtime-ergonomics/readiness/live-smoke.md`. Build with `dotnet build` — NOT `fake.sh -t Dev` (marker only).
- [ ] T004 Draft the public-surface seam for §3.5 in [contracts/adapter-noop.md](./contracts/adapter-noop.md) form: the exact `module Cmd`/`module Sub` `.fsi` block for `src/Controls.Elmish/ControlsElmish.fsi` (do not implement yet). Confirm no existing `Cmd`/`Sub` module in `FS.GG.UI.Controls.Elmish` (`readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt`).

**Checkpoint**: hypotheses confirmed live; the only net-new surface (§3.5) is sketched. User stories may now proceed independently.

---

## Phase 3: User Story 1 — §3.4 KeyboardMsg collision guidance (Priority: P1) 🎯 MVP

**Goal**: a consumer can model its own `Msg.KeyDown`/`KeyUp` and compile, having found the remedy in docs.
**Independent test**: scaffold a `game` product, define `Msg.KeyDown of KeyId` + `mapKey`, follow the new `product.md` line → compiles with **zero** `does not match 'ViewerKey'` errors; `grep KeyboardMsg template/base/docs/product.md` hits (SC-001).

- [ ] T005 [US1] Add the collision line to `template/base/docs/product.md` (in the existing collision paragraph beside `Text`/`CloseRequested`/`Rect`/`ControlEventOrigin.Text`): name `FS.GG.UI.KeyboardInput.KeyboardMsg.KeyDown of KeyId` / `KeyUp of KeyId` as collision-prone with a product's own `Msg.KeyDown`/`KeyUp`; remedy = qualify the framework cases (`KeyboardMsg.KeyDown`) or avoid an unqualified `open FS.GG.UI.KeyboardInput` where the product defines input messages — **order-independent** (per [contracts/guidance.md](./contracts/guidance.md) G1).
- [ ] T006 [US1] Verify against the live scaffold from T003: apply the documented remedy → the product compiles (`dotnet build`); capture the before/after in `readiness/live-smoke.md`.

**Checkpoint**: §3.4 delivered and independently verified.

---

## Phase 4: User Story 2 — §3.6 surface the pure `measureText` (Priority: P2)

**Goal**: a consumer finds the pure `Scene.measureText` and places HUD text from measured metrics, no magic numbers.
**Independent test**: the documented snippet compiles in a scaffolded product and positions a HUD label with 0 literal coordinates; `grep measureText` hits `product.md` and ≥1 product skill (SC-002, SC-003).

- [ ] T007 [US2] Add the measureText HUD idiom to `template/base/docs/product.md` per [contracts/guidance.md](./contracts/guidance.md) G2: name `FS.GG.UI.Scene.measureText : string -> FontSpec -> TextMetrics` as the pure authoring-time metric (vs. render-edge shaping), note `TextMetrics.{Width,Height,Baseline}` and its conservative calibration, and a worked self-positioning snippet (e.g. right-align a score in the reserved HUD band) computing origin/box from `(measureText text font).Width`/`.Height` + the HUD region — no literal coordinate.
- [ ] T008 [P] [US2] Name the helper in `template/base/product-skills`/... — add a HUD-measure pointer line to `template/product-skills/fs-gg-scene/SKILL.md` (and cross-link from `fs-gg-layout` and/or `fs-gg-game-core`) pointing at `Scene.measureText` with the self-positioning idiom.
- [ ] T009 [US2] Verify the snippet compiles in the scaffold and positions text from metrics (`dotnet build` + eyeball/headless HUD-bounds evidence); record in `readiness/live-smoke.md`. Confirm no new measurer was added (FR-005).

**Checkpoint**: §3.6 delivered; capability discoverable without reading framework source.

---

## Phase 5: User Story 3 — §3.5 `Cmd.none` / `Sub.none` no-ops (Priority: P3)

**Goal**: a product `update` returns `model, Cmd.none` and `subscriptions` returns `Sub.none`, identical to `[]`.
**Independent test**: the law test passes (`Cmd.none = []`, `Sub.none = []`); the template compiles with the aliases; surface gate green (SC-004).

### Tests for User Story 3 ⚠️ (write first — fail before, pass after)

- [ ] T010 [US3] Add a behavioral test in `tests/Package.Tests` (new file e.g. `Feature241NoOpAliasTests.fs`, registered in the test list): assert `FS.GG.UI.Controls.Elmish.Cmd.none = ([] : AdapterCommand<_>)`, `AdapterCmd.productMessages Cmd.none = []`, and `Sub.none = ([] : AdapterSubscription<_> list)` ([contracts/adapter-noop.md](./contracts/adapter-noop.md) laws). Confirm it FAILS to compile/pass before implementation.

### Implementation for User Story 3

- [ ] T011 [US3] Add `module Cmd { val none: AdapterCommand<'msg> }` and `module Sub { val none: AdapterSubscription<'msg> list }` to `src/Controls.Elmish/ControlsElmish.fsi` (the T004 seam, with the documenting `///` comments).
- [ ] T012 [US3] Implement the paired bodies in `src/Controls.Elmish/ControlsElmish.fs` — `let none = []` for each; no access modifiers (Principle II).
- [ ] T013 [US3] Refresh the surface baseline: `dotnet fsi scripts/refresh-surface-baselines.fsx`; confirm `readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt` gains exactly `FS.GG.UI.Controls.Elmish.Cmd` and `…Sub` (`git diff` = +2 lines); `dotnet test tests/Package.Tests --filter SurfaceArea` green.
- [ ] T014 [US3] Consume in the product template: in `template/base/src/Product/Model.fs` change `model, []` → `model, Cmd.none` and the `subscriptions` `[]` → `Sub.none`; in `template/base/src/Product/EvidenceCommands.fs` change the command no-op `[]` → `Cmd.none`. Behaviour-preserving (FR-006).
- [ ] T015 [P] [US3] Surface in guidance: add the `Cmd.none`/`Sub.none` alias note to `template/base/docs/product.md` (with the qualified-fallback note, research D2) and show `update` returning `model, Cmd.none` + `subscribe` returning `Sub.none` in `template/product-skills/fs-gg-elmish/SKILL.md` ([contracts/guidance.md](./contracts/guidance.md) G3).
- [ ] T016 [US3] Confirm T010 now passes; verify the template still builds/tests (`dotnet build` + `dotnet test` on the generated product template) and the aliases resolve unambiguously in the scaffold (no `open Elmish` ambiguity). Record in `readiness/live-smoke.md`.

**Checkpoint**: §3.5 delivered as a real, baseline-tracked capability + consumed by the template.

---

## Phase 6: Polish & cross-cutting

- [ ] T017 Run the full gate set: `dotnet test tests/Package.Tests` (SurfaceArea + skill-manifest/currency green, FR-008), full solution `dotnet test`, and a generated-product template build/test — confirm no regression vs. T001 baseline (SC-005).
- [ ] T018 [P] Update `specs/241-runtime-ergonomics/readiness/` with the final evidence (surface-diff, law test, live-smoke before/after, gate results). Confirm all spec Success Criteria SC-001…SC-005 are met.
- [ ] T019 On merge: move Coordination board item **#74** and (if all epic children done) note epic **.github#165** status; close **FS-GG/FS.GG.Rendering#74** via the merge PR. (Board flip already set to *In progress* at feature start.)

---

## Dependencies & ordering

- **Setup (T001)** → **Foundational (T002–T004)** → user stories.
- **US1 (T005–T006)**, **US2 (T007–T009)**, **US3 (T010–T016)** are mutually **independent** and may be done in any order / parallel after Phase 2. Priority order for delivery: US1 (MVP) → US2 → US3.
- Within US3: T010 (test) → T011 → T012 → T013 → T014 → T016; T015 [P] after T011.
- **Polish (T017–T019)** after all targeted stories.

## Parallel opportunities

- After Phase 2: the three stories run in parallel (disjoint files — `product.md` is touched by all three but in different sections; sequence the `product.md` edits if done by one agent, or split by section).
- T008 [P] (scene skill) alongside T007; T015 [P] (elmish skill) alongside T011–T014.

## MVP scope

**US1 (§3.4)** alone — a one-line docs fix that unblocks the hard compile stop — is a shippable MVP.

## Format validation

All tasks use `- [ ] Txxx [P?] [US?] description + file path`; Setup/Foundational/Polish carry no story label; US phases are labeled. ✅

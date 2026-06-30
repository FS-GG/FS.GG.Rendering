---
description: "Task list for Replaceable Game Starter Scene"
---

# Tasks: Replaceable Game Starter Scene

**Input**: Design documents from `/specs/220-game-starter-scene/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/fs-gg-ui-template-contract.md, quickstart.md

**Tests**: This feature changes generated test files (`GovernanceTests.fs`, `BehaviorTests.fs`) as
part of its contract, so test-authoring tasks are first-class here (not optional). They are the
durable-spine and replaceable-behavior tasks called out in data-model.md §2 and research.md
Decision 4.

**Organization**: Tasks are grouped by user story (P1 → P2 → P3) to enable independent
implementation and testing. The deliverable is the `fs-gg-ui-template` template under `template/`
plus a cross-repo ADR — there is no new framework package surface.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (Setup, Foundational, Polish carry no story label)
- All paths are repo-relative

## Path Conventions

This is a **templating contract change**. Work lives under `template/` (profiles, `capabilities.yml`,
the shared `template/base` product source specialized by `//#if (profile == …)` preprocessor
conditionals, and `template/base/docs/`), plus `docs/product/decisions/` for the cross-repo ADR and
`specs/220-game-starter-scene/` for evidence.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the evidence workspace and the no-regression baseline before any change.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline task MUST run **every**
> test project and record the full red/green set so pre-existing failures are known up front and not
> mistaken for regressions at merge. Use the discovery-based runner
> (`scripts/baseline-tests.fsx`), which globs `*.Tests.fsproj` so nothing silently drops out —
> including `tests/Package.Tests` and `samples/**/*.Tests`, which the solution omits. The NU1403
> FSharp.Core lockfile workaround in auto-memory applies if a restore is blocked.

- [X] T001 Create the evidence workspace `specs/220-game-starter-scene/readiness/` (holds the profile-matrix probe output, the FR-007 byte-diff baseline, and the SC-001/SC-004 swap evidence)
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/220-game-starter-scene/readiness/baseline.md` (runs EVERY test project — solution + `tests/Package.Tests` + `samples/**` — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the plan's reachability hypotheses against the real generated output and
capture the FR-007 diff baseline BEFORE authoring any game branch. Draft the contract seam.

**⚠️ CRITICAL**: No user-story work may begin until the profile-matrix probe (T004) confirms or
replaces the Decision-2 reachability hypotheses.

> **⚠️ Early live smoke run (STANDING, do not omit).** The plan's reachability claims (which
> profiles hit which `//#else`, whether `sample-pack` launches via `Viewer.runApp`, whether the
> game `//#else` governance branch is truly unexercised) are **read from source and provisional**
> (plan.md "Standing assumption"). T004 + T005 drive the real template tooling and the real
> generated products to confirm them before anything is authored. Do not defer this evidence.

- [X] T003 Read `template/capabilities.yml`, `template/profiles/app.yml`, and the six `template/base/src/Product/*.fs(proj)` files plus both `template/base/tests/Product.Tests/*.fs` to map every existing `//#if (profile == …)` / `//#else` conditional and record which profile currently hits each branch — output to `specs/220-game-starter-scene/readiness/conditional-map.md`
- [X] T004 **Profile-matrix instantiation probe** (quickstart Scenario A): pack/install the template, instantiate `app`, `headless-scene`, `governed`, `sample-pack` into scratch dirs, run `Test` (build + `Product.Tests`) on each, and record pass/fail + the generated `Program.fs` default-launch branch and `Product.fsproj` package set per profile → `specs/220-game-starter-scene/readiness/profile-matrix-probe.md`. **Confirm**: `sample-pack` emits `Viewer.runApp viewerOptions generatedHost` in the default branch and the controls package set (validates the `game || sample-pack` launch grouping). If not, record the corrected pinning before proceeding. **Also confirm `governed` is instantiable**: there is **no** `template/profiles/governed.yml` (only `app`, `headless-scene`, `sample-pack` exist as profile files); `governed` appears only in `capabilities.yml` `profiles:` lists. Record how `governed` is selected/derived at instantiation — or correct the stale reference — before it is relied on as an FR-007 diff target in T006/T024.
- [X] T005 **Early live smoke run**: drive at least one instantiated interactive product (the `app`/controls default) in the real viewer host and capture live evidence (or `environment-limited` with a disclosed substitute per the Feature-168 evidence rules) → `specs/220-game-starter-scene/readiness/smoke-run.md`, so the launch/host behavior the game branch will reuse is observed, not assumed
- [X] T006 Capture the **FR-007 byte-diff baseline**: snapshot the generated `headless-scene` / `governed` / `sample-pack` trees from T004 into `specs/220-game-starter-scene/readiness/fr007-baseline/` (the diff target re-checked in US3)
- [X] T007 Draft the cross-repo ADR seam at `docs/product/decisions/0010-fs-gg-ui-template-default-starter.md` capturing the `app → game` default flip, the new `game` profile, and the family-agnostic entrypoint assertion (per contracts/fs-gg-ui-template-contract.md §C5; flesh out + register in Polish)

**Checkpoint**: Reachability hypotheses confirmed/replaced against real output; FR-007 baseline
captured; contract seam drafted — game-branch authoring can begin.

---

## Phase 3: User Story 1 - The default game starter is mine to replace (Priority: P1) 🎯 MVP

**Goal**: Scaffolding the game/rendering default produces a runnable minimal Pong-style starter
whose no-flag launch renders a live interactive game scene; the developer can replace it at the
default entrypoint and every generated governance test passes with no governance-test edits and no
extra launch flag.

**Independent Test**: Instantiate the `game` profile, run `Test` on the unmodified default (green),
then replace `Model.fs`/`View.fs` with a minimal Pong and re-run `Test` — green build + product
tests, **0** edits to `GovernanceTests.fs`, no `-- pong` flag (quickstart Scenarios B & C; SC-001,
SC-002, SC-004).

### Profile + capabilities (selects the game family)

- [X] T008 [P] [US1] Create `template/profiles/game.yml` — game/rendering default starter selecting the game family; capabilities = `scene, skiaviewer, elmish, keyboard-input, layout, controls, full-governance`, `validationCommands: [Dev, Test, Verify]` (mirror `app.yml`'s set per data-model.md §3)
- [X] T009 [US1] Register `game` in `template/capabilities.yml` — add `game` to the relevant capability `profiles:` lists (scene, skiaviewer, elmish, keyboard-input, layout, controls) and note the game/rendering default; also confirm whether the `full-governance` capability declares a `profiles:` list and, if so, add `game` to it (game.yml's capability set includes `full-governance` per T008)

### Game-branch content (developer-owned seam)

- [X] T010 [P] [US1] Add the game-family branch to `template/base/src/Product/Model.fs` — `//#if (profile == "game")` minimal Pong `Model` (ball center+velocity, left/right paddle positions, score, playfield size, tick count, last input), `Msg` (`Tick` | paddle-move per side | `NoOp`), pure `update` (integrate/bounce/clamp/score), and `init`; controls model stays in `//#else` (app + sample-pack). Avoid bare `X/Y/Width/Height` record labels (Scene literal collision — data-model.md §1)
- [X] T011 [P] [US1] Add the game-family `view : Model -> SceneNode` to `template/base/src/Product/View.fs` — `Group` of `Scene` primitives (playfield border `Rectangle`, ball `Rectangle`, two paddle `Rectangle`s, score HUD `Text`) in the `//#if (profile == "game")` branch; controls view unchanged in `//#else`

### Durable spine — re-point + conditional (model-agnostic plumbing)

- [X] T012 [US1] Re-point the game branch in `template/base/src/Product/EvidenceCommands.fs` — `generatedHost.View = view`, `viewerEffectsForModel`, and the deterministic `--scene-evidence`/`SceneEvidence.render` (`RendererMode = "deterministic-scene"`) point at the game `view`/`initialModel`; keep the command surface + must-survive tokens; `interactiveHost` stays `//#if (profile == "app")` only (depends on T010, T011)
- [X] T013 [US1] Re-point the game branch in `template/base/src/Product/LayoutEvidence.fs` — HUD region → score strip, gameplay region → playfield, and the active-item/movement helpers onto the ball; keep `hud-region`/`gameplay-region`/`measurement-mode`/overlap tokens (depends on T010)
- [X] T014 [P] [US1] Thread `game` into the package/compile gate in `template/base/src/Product/WindowOptions.fs` — extend the existing `(profile == "app" || profile == "sample-pack")` gate to `(… || profile == "game")` and re-confirm window-option defaults (conditional-only change)
- [X] T015 [P] [US1] Thread `game` into `template/base/src/Product/Product.fsproj` — extend every `(profile == "app" || profile == "sample-pack")` package/compile conditional to include `game` so it pulls in `SkiaViewer`/`Elmish`/`KeyboardInput`/`Layout`/`Controls`/`Controls.Elmish`/`DesignSystem`/`Themes.Default` + `WindowOptions.fs`
- [X] T016 [US1] Group the launch host in `template/base/src/Product/Program.fs` — default `| None ->` branch: `app → ControlsElmish.runInteractiveApp`; `game || sample-pack → Viewer.runApp viewerOptions generatedHost`. Verify against T004 that `sample-pack`'s emitted launch call is **unchanged** (depends on T004, T012)

### Governance + behavior tests (durable vs replaceable)

- [X] T017 [US1] Make the game `//#else` assertions in `template/base/tests/Product.Tests/GovernanceTests.fs` satisfiable by the minimal skeleton **and** a Pong swap — keep the model-agnostic evidence/structure/discoverability scans and the family-appropriate persistent-host assertion (`Viewer.runApp viewerOptions generatedHost`); ensure no assertion pins a controls-only token (SC-004 invariant, contract §C3)
- [X] T018 [US1] Rewrite the game `//#else` branch of `template/base/tests/Product.Tests/BehaviorTests.fs` to drive the skeleton's `update`/`view`/`tick`/`generatedHost` (ball motion, paddle input, score, tick advance); the `//#if (profile == "app")` pointer-click test stays for the controls family (depends on T010, T011)

### US1 validation (the headline journey)

- [X] T019 [US1] Instantiate the `game` profile and run `Test` on the **unmodified** default → green (edge case: the default is a valid, live, moving product), capturing launch `mode=interactive-window` evidence → `specs/220-game-starter-scene/readiness/game-default.md` (depends on T008–T018)
- [X] T020 [US1] **Swap-to-Pong** (quickstart Scenario B): in a fresh `game` scaffold, replace the starter by editing only `Model.fs`, `View.fs`, `BehaviorTests.fs` (plus documented `LayoutEvidence.fs`/`EvidenceCommands.fs` field re-points), run `Test` → green with **0** edits to `GovernanceTests.fs` and **no** `-- pong` flag; record evidence → `specs/220-game-starter-scene/readiness/swap-to-pong.md` (SC-001, SC-004, FR-008; depends on T019)

**Checkpoint**: The game/rendering default is a runnable minimal game, replaceable at the default
entrypoint, green across a Pong swap with no governance-test edits — MVP complete.

---

## Phase 4: User Story 2 - Swapping the starter is a small, bounded change (Priority: P2)

**Goal**: The starter swap is confined to the developer-owned seam (`Model.fs`/`View.fs`/
`BehaviorTests.fs`) plus the documented "re-point model fields" set; the scaffold map matches the
real edit set exactly, with no undocumented coupling.

**Independent Test**: Perform the documented swap and `git status`/diff the generated tree —
changed files ⊆ the scaffold-map replaceable + re-point classification, **0** undocumented files
forced to change (quickstart Scenario D; SC-003, SC-005).

- [X] T021 [US2] Align `template/base/docs/scaffold-map.md` to the real game-starter edit set — replaceable = `<ProductDir>/Model.fs`, `<ProductDir>/View.fs`, `tests/Product.Tests/BehaviorTests.fs`; re-point = `LayoutEvidence.fs`, `EvidenceCommands.fs`; durable-untouched = `WindowOptions.fs`, `Product.fsproj`, `Program.fs`, `GovernanceTests.fs` (`WindowOptions.fs`/`Product.fsproj` are conditional-only authoring changes — T014/T015 — with no model-field re-point at swap time, so they are untouched on a swap, matching quickstart Scenario B/D and T020) (contract §C4; FR-005)
- [X] T022 [US2] Verify the documented swap edit set: from a clean `game` scaffold perform the Scenario-B swap, diff the generated tree, and confirm the changed-file set ⊆ the T021 classification with **0** undocumented files; record → `specs/220-game-starter-scene/readiness/edit-set-diff.md` (SC-003, SC-005; depends on T020, T021)

**Checkpoint**: The scaffold map's replaceable/durable classification provably matches the real
swap — the few-file promise holds.

---

## Phase 5: User Story 3 - The controls showcase stays available, just not forced (Priority: P3)

**Goal**: The controls showcase remains a discoverable, explicit option (`app` profile), and the
`headless-scene` / `governed` / `sample-pack` profiles are unchanged (byte-identical output + tests).

**Independent Test**: Instantiate the explicit `app` profile → controls showcase still generates +
passes governance; diff `headless-scene`/`governed`/`sample-pack` against the FR-007 baseline →
empty (quickstart Scenario E; SC-006).

- [X] T023 [US3] Re-describe `template/profiles/app.yml` as the explicit, opt-in controls-showcase option (no longer "the" default) — copy/metadata only, no change to its generated output (FR-006)
- [X] T024 [US3] Re-instantiate `headless-scene`, `governed`, `sample-pack` post-change and diff against the FR-007 baseline (`specs/220-game-starter-scene/readiness/fr007-baseline/`) → diff MUST be empty; record → `specs/220-game-starter-scene/readiness/fr007-diff.md` (SC-006; depends on T006 and all US1 edits)
- [X] T025 [US3] Instantiate the explicit `app` profile and run `Test` → controls showcase generates and passes its governance tests; record → `specs/220-game-starter-scene/readiness/app-controls.md` (SC-006 controls half; depends on T023)

**Checkpoint**: Controls preserved as an explicit option; the three non-interactive profiles
provably unchanged — **0** regressions.

---

## Phase 6: Polish & Cross-Cutting Concerns (FR-009 + release)

**Purpose**: Coordinate the `fs-gg-ui-template` contract change with downstream consumers, bump the
template version, and run the full quickstart validation.

- [X] T026 Finalize the ADR `docs/product/decisions/0010-fs-gg-ui-template-default-starter.md` (from T007 seam) — record the default-starter change, new `game` profile, and relaxed entrypoint assertion as an accepted decision
- [X] T027 [P] Update the `fs-gg-ui-template` contract/compatibility registry entry per the `cross-repo-coordination` skill (the versioned surface delta: new profile + family-agnostic governance)
- [X] T028 File the Coordination-board issue for SDD (scaffold-provider: enumerate `game`, flip game/rendering default `app → game`) and Templates (governance expectations for the new default + relaxed assertion), sequenced alongside sibling item #32 (FR-009; depends on T026, T027)
- [ ] T029 Bump the template version metadata to a coherent preview and republish so downstream consumption is not silently broken (research.md Decision 5; depends on T028)
- [X] T030 Run the full quickstart.md validation (Scenarios A–F) end to end and record the consolidated result → `specs/220-game-starter-scene/readiness/quickstart-validation.md`, confirming SC-001 through SC-006

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — **BLOCKS all user stories** (the probe must
  confirm reachability before any game branch is authored)
- **User Story 1 (Phase 3, P1)**: Depends on Foundational — the MVP
- **User Story 2 (Phase 4, P2)**: Depends on US1 (swap evidence T020 → edit-set diff T022)
- **User Story 3 (Phase 5, P3)**: Depends on Foundational baseline (T006) + US1 edits (to diff
  against); `app.yml` re-describe (T023) is independent
- **Polish (Phase 6)**: Depends on the contract change being implemented (US1–US3)

### Within User Story 1

- Profile/capabilities (T008–T009) and content (T010–T011) before spine re-points (T012–T013)
- Launch grouping (T016) depends on the probe (T004) and the host re-point (T012)
- Governance/behavior tests (T017–T018) after content exists
- Validation (T019–T020) last, after all US1 edits

### Parallel Opportunities

- **Setup**: T001, T002 are sequential (workspace then baseline)
- **Foundational**: T003 → T004 → (T005, T006) ; T007 is independent ([P]-able alongside the probe)
- **US1 content**: T008, T010, T011 can run in parallel ([P]); T014, T015 ([P]) parallel to the
  spine re-points once content exists
- **US3**: T023 is independent of T024/T025
- **Polish**: T027 [P] parallel to T026

---

## Parallel Example: User Story 1

```bash
# Kick off the independent game-family pieces together:
Task: "Create template/profiles/game.yml (T008)"
Task: "Add game-family Model/Msg/update to template/base/src/Product/Model.fs (T010)"
Task: "Add game-family view to template/base/src/Product/View.fs (T011)"

# Then the conditional-only gates in parallel:
Task: "Thread game into template/base/src/Product/WindowOptions.fs (T014)"
Task: "Thread game into template/base/src/Product/Product.fsproj (T015)"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1: Setup (workspace + comprehensive baseline)
2. Phase 2: Foundational — including the **profile-matrix probe + early live smoke run** that
   validate the reachability hypotheses against the real generated output before any game branch is
   authored
3. Phase 3: User Story 1 → the replaceable game default
4. **STOP and VALIDATE**: instantiate `game`, run the unmodified default green, swap to Pong green
   with 0 governance-test edits (SC-001/SC-002/SC-004)

### Incremental Delivery

1. Setup + Foundational → reachability confirmed, FR-007 baseline captured
2. US1 → replaceable game default (MVP)
3. US2 → scaffold-map matches the real swap (bounded-change promise)
4. US3 → controls preserved + non-interactive profiles provably unchanged
5. Polish → cross-repo coordination, version bump, full quickstart validation

---

## Notes

- [P] = different files, no dependency on incomplete tasks
- The two conditional groupings (content vs launch) differ and each must pin `sample-pack`
  unchanged — getting them wrong silently regresses sample-pack (FR-007); the T024 diff is the gate
- `GovernanceTests.fs` is never edited to make a replacement pass (FR-002); the game `//#else`
  assertions are only made *satisfiable* (T017)
- Keep any synthetic evidence's `// SYNTHETIC:` disclosure + `Synthetic` test-name token
- Commit after each task or logical group

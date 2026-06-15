---
description: "Task list for Feature 096 — Runtime Visual-State Bridge (conformance backfill)"
---

# Tasks: Runtime Visual-State Bridge (Feature 096)

**Input**: Design documents from `/specs/096-runtime-visual-state-bridge/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/runtime-bridge.md

**Tests**: The `Feature096RuntimeBridgeTests` + `Feature096BridgePropertyTests` (in `Controls.Tests`)
and `Feature096LiveBridgeTests` (in `Elmish.Tests`) suites already ship in the imported source. This
feature is a **backfill conformance pass** (the pattern features 091, 093, and 095 established): no new
product behavior is built. Tasks **confirm** the public projection (`deriveVisualState`), the internal
host bridge (`applyRuntimeVisualState` + the feature-112 targeted variants), the `VisualState` carrier
and reader, the executable suites, the regenerated readiness evidence, and the zero
public-surface-baseline delta — they do not author new bridge code. Where a task would normally "write
a test," it instead **runs and confirms the already-shipped suite green**.

**Organization**: Tasks are grouped by user story (US1–US4 from spec.md) so each story's contract can
be confirmed independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files/suites, no dependencies)
- **[Story]**: Which user story this task confirms (US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- Single F# project: `src/Controls/`, `tests/Controls.Tests/`, `tests/Elmish.Tests/` at repository root
- Surface baseline: `tests/surface-baselines/FS.GG.UI.Controls.txt`
- Readiness evidence: `specs/096-runtime-visual-state-bridge/readiness/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Restore/build the libraries the conformance pass exercises.

- [X] T001 Restore and build the Controls library: `dotnet build src/Controls/Controls.fsproj` (net10.0, LangVersion=latest per `Directory.Build.props`); confirm a clean build with FS0078 promoted to error.
- [X] T002 [P] Build the `Controls.Tests` assembly: `dotnet build tests/Controls.Tests/Controls.Tests.fsproj` and confirm it references `tests/Controls.Tests/Feature096RuntimeBridgeTests.fs` and reaches the internal bridge (`applyRuntimeVisualState` and the feature-112 targeted variants) via `[<assembly: InternalsVisibleTo("Controls.Tests")>]` (declared in `src/Controls/Controls.fsproj`).
- [X] T003 [P] Build the `Elmish.Tests` assembly: `dotnet build tests/Elmish.Tests/Elmish.Tests.fsproj` and confirm it references `tests/Elmish.Tests/Feature096LiveBridgeTests.fs` (the live `RetainedRender`-path stories that regenerate the readiness markdown).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the runtime-state vocabulary, the public projection + internal bridge `.fsi` surface, the single carrier + reader, and the zero public-surface-baseline delta as `contracts/runtime-bridge.md` pins them — these underlie every user story.

**⚠️ CRITICAL**: All US confirmations depend on this phase establishing that the shipped surface matches the contract.

- [X] T004 [P] Confirm the consumed vocabulary: the `VisualState` DU on `src/Controls/Types.fsi` (~L256, eight cases `Normal | Disabled | Hover | Pressed | Focused | Selected | Loading | Validation of ValidationState`); the `visualState` attribute builder in `src/Controls/Attributes.fs` (~L72, the single carrier); and the `visualStateOf: Attr<'msg> list -> VisualState` reader on `src/Controls/Control.fsi` (~L100, absent ≡ `Normal`) — contract §1/§2 (the carrier 093 reads, the channel 096 writes).
- [X] T005 [P] Confirm the **public** projection is declared in `src/Controls/ControlRuntime.fsi` (the sole declaration, Principle II): `val deriveVisualState: model: ControlRuntimeModel -> controlId: ControlId -> VisualState` (~L96), and the supporting `ControlRuntimeModel` (~L41), `ControlRuntimeMsg` (~L53), `ControlRuntimeEffect` (~L28) MVU types — contract §1.
- [X] T006 [P] Confirm the **internal** host bridge in `src/Controls/ControlRuntime.fsi`: `val internal applyRuntimeVisualState: ControlRuntimeModel -> Control<'msg> -> Control<'msg>` (~L103), and the feature-112 targeted variants `type internal RuntimeStampResult<'msg>` (~L76), `val internal applyRuntimeVisualStateTargeted` (~L116), `val internal runtimeStampFor` (~L129) — all carrying the `internal` qualifier in the `.fsi` (DF-1, the sanctioned home for visibility), reachable only via `InternalsVisibleTo` — contract §2.
- [X] T007 Confirm the precedence cascade in `src/Controls/ControlRuntime.fs` (~L208-222) implements the closed runtime order `Pressed > Selected > Focused > Hover > Normal` (PressedControls → Pressed; Selection.ControlId → Selected; FocusedControl → Focused; HoveredControl → Hover; else Normal) with **no per-kind branching** and the head states (`Disabled`/`Validation`/`Loading`) never produced — contract §1 (FR-001, FR-002).
- [X] T008 Confirm zero public-surface delta: run the surface-drift check `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Surface"` and `grep -nE "ControlRuntime" tests/surface-baselines/FS.GG.UI.Controls.txt` — expect `FS.GG.UI.Controls.ControlRuntime` + `ControlRuntimeModel`/`ControlRuntimeMsg`/`ControlRuntimeEffect` present (lines ~61-89), `deriveVisualState` the lone public function entry, the bridge functions absent (internal), and no baseline diff — SC-008, contract §1 (FR-008).

**Checkpoint**: Surface, vocabulary, and precedence confirmed — user-story conformance can proceed (suites may run in parallel).

---

## Phase 3: User Story 1 - A control restyles from live interaction with zero consumer code (Priority: P1) 🎯 MVP

**Goal**: The public projection selects the highest-ranked state under the closed runtime precedence; the bridge stamps the derived state onto a control the consumer left unstyled so feature 093's resolver paints it; a non-interacted sibling is untouched; and each widened kind visibly restyles while remaining byte-identical at rest.

**Independent Test**: Build a model where one id is simultaneously pressed/selected/focused/hovered and confirm `deriveVisualState` returns `Pressed`, peeling each state to reveal the next, with an unknown id ⇒ `Normal`; stamp a hovered/pressed/selected model onto a NO-attribute control and confirm it carries the derived state; confirm the resolved paint of a widened kind differs from its `Normal` render; confirm a non-interacted sibling stays `Normal` and is returned unchanged.

### Confirmation for User Story 1

- [X] T009 [P] [US1] Run the precedence + totality + determinism tests: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~096&FullyQualifiedName~T010"`; confirm in `Feature096RuntimeBridgeTests.fs` that `deriveVisualState` returns `Pressed` for a simultaneously pressed/selected/focused/hovered id and peeling reveals `Selected → Focused → Hover → Normal` (the closed runtime precedence), an id named by no interaction state (and an unknown id) resolves to `Normal` (never throws), and identical inputs yield an identical result — SC-001, contract §1 (FR-001, FR-002).
- [X] T010 [P] [US1] Run the no-attribute restyle + sibling-unchanged tests: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~096&FullyQualifiedName~T011"`; confirm a control the consumer left **unstyled** (NO `visualState` attribute) carries the derived `Hover`/`Pressed`/`Selected` after the bridge, and a non-interacted sibling resolves `Normal` and is returned structurally unchanged (no attribute added) — SC-002, contract §2 clause 2 (US1 acceptance 3, 5).
- [X] T011 [P] [US1] Run the focus-indicator tests: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~096&FullyQualifiedName~T017"`; confirm a focused control gains the `Focused` indicator with no consumer focus attribute, and moving focus moves the indicator (the previously-focused control returns to `Normal`) — SC-002.
- [X] T012 [US1] Run the scoped-kinds restyle test: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~096&FullyQualifiedName~T015"`; confirm each widened kind (`button`/`slider`/`text-box`/`radio-group`/`switch`) restyles under a runtime state (resolved paint differs from `Normal`, the runtime state visibly drove the draw via the 093 resolver) and is byte-identical at rest — SC-006 (restyle half), contract §3 (FR-006). The unmigrated-kind no-delta half is confirmed in US2/T015.

**Checkpoint**: US1 contract (§1, §2 clause 2, §3 restyle) confirmed green and independently testable — MVP slice validated (interaction-driven styling "just works" for migrated controls with zero consumer code).

---

## Phase 4: User Story 2 - An un-interacted control is byte-identical to its un-bridged self (Priority: P1)

**Goal**: A control or tree nobody is interacting with (empty model, no consumer attribute) is returned structurally unchanged, renders a `Scene` byte-identical to the un-bridged build, and recomputes zero nodes on the live retained path; every kind's `Normal` attribute is byte-identical to its unset render, and an unmigrated kind shows no render delta.

**Independent Test**: Apply the bridge with an empty model to a multi-control tree and confirm no attribute added anywhere; render the at-rest bridged tree and confirm its `Scene` is byte-identical to the un-bridged build; feed the at-rest tree to the live `RetainedRender.step` and confirm `RecomputedNodeCount = 0`; confirm an unmigrated kind shows no render delta.

### Confirmation for User Story 2

- [X] T013 [P] [US2] Run the at-rest identity tests: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~096&FullyQualifiedName~T012"`; confirm in `Feature096RuntimeBridgeTests.fs` that a `Normal`-and-unset (empty-model) multi-control tree is returned UNCHANGED (no `visualState` attribute added anywhere), the at-rest bridged tree is `Scene`-byte-identical to the un-bridged build (a derived `Normal` emits nothing), and the live `RetainedRender.step` recomputes **0** nodes at rest — SC-003, contract §2 clause 4 (FR-005).
- [X] T014 [P] [US2] Run the unmigrated-kind no-delta test: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~096&FullyQualifiedName~T023"`; confirm an unmigrated kind (`progress-bar`/`numeric-input`) is stamped but shows **no render delta** under a runtime state, and that for every kind a `Normal` attribute is byte-identical to the unset render — SC-006 (no-delta + `Normal`≡unset halves), contract §3 (FR-006).

**Checkpoint**: US2 confirmed — the no-interaction case is provably inert (byte-identity + Scene-identity + zero-recompute), so the bridge is safe on the live render path. Co-critical with US1.

---

## Phase 5: User Story 3 - Author intent out-ranks derived interaction, through one carrier channel (Priority: P2)

**Goal**: A consumer-set non-`Normal` state is preserved 100% even while the runtime reports the same control interacted; only a consumer-`Normal`/absent slot is filled by the derived state; arbitration flows through the single `visualState` carrier (replace-or-append); and the head states are never produced by the projection.

**Independent Test**: Give a consumer-`Disabled` control to a model reporting it hovered/pressed/focused and confirm it stays `Disabled`; give a consumer-`Selected` control to a model reporting it pressed and confirm it stays `Selected`; give a consumer-`Normal` control to a model reporting it focused and confirm the derived `Focused` fills the slot; property-test (≥1000 combos) that a consumer non-`Normal` state is preserved 100%, a consumer-`Normal` takes the derived state, the result is deterministic, and the bridge never throws.

### Confirmation for User Story 3

- [X] T015 [P] [US3] Run the consumer-vs-derived arbitration tests: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~096&FullyQualifiedName~T021"`; confirm in `Feature096RuntimeBridgeTests.fs` that a consumer-`Disabled` control reported hovered/pressed/focused stays `Disabled`, a consumer-`Selected` control reported pressed stays `Selected`, and a consumer-`Normal` control reported focused becomes `Focused` (the derived state fills the slot) — SC-004, contract §2 clauses 2/3 (FR-003).
- [X] T016 [US3] Run the property suite: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~bridge properties"`; confirm `Feature096BridgePropertyTests` proves over ≥1000 `Gen096`-generated `(model, id, consumer-state)` combos that (a) `deriveVisualState` is total + deterministic, (b) the closed order holds — a consumer non-`Normal` state is preserved 100% and a consumer-`Normal` control takes `deriveVisualState model id`, and (c) `applyRuntimeVisualState` is deterministic — and confirm the `Gen096` consumer generator (`Feature096RuntimeBridgeTests.fs:254-263`) includes the head states (`Disabled`/`Loading`/`Validation`) so the "never derived / always preserved" boundary is exercised — SC-004, contract §1/§2 (FR-001, FR-003).

**Checkpoint**: US3 confirmed — author intent is authority; the runtime only fills a `Normal`/absent slot, through the single carrier, and never manufactures a head state.

---

## Phase 6: User Story 4 - The runtime look rides retained identity and repaints only what changed (Priority: P2)

**Goal**: Because the derived state is stamped pre-reconcile, the runtime look rides feature 092's stable retained identity (E2) through the keyed diff — surviving a position-shifting re-render where a position-keyed baseline loses it — and a localized interaction produces a bounded repaint (`RecomputedNodeCount < BaselineNodeCount`), not a full-tree rebuild.

**Independent Test**: Focus an editor, derive its `Focused` via the bridge, insert a banner above it (shifting siblings), and re-derive through the live retained path — confirm the control keeps its retained id and stays `Focused`, where a position-keyed baseline loses identity; render a 3-control row at rest, step the live `RetainedRender` with a model hovering exactly one control, and confirm `RecomputedNodeCount < BaselineNodeCount` with the hovered control counted as changed work.

### Confirmation for User Story 4

- [X] T017 [P] [US4] Run the bounded-repaint test: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~096&FullyQualifiedName~T024"`; confirm a single hover entering one control is a bounded (`RecomputedNodeCount < BaselineNodeCount`) repaint via the live retained path, with the hovered control counted as changed work — proven by the feature-112 targeted-stamp variants (`runtimeStampFor`/`applyRuntimeVisualStateTargeted` reporting `RuntimeStateTouchedNodeCount`) — SC-005, contract §2 (FR-007).
- [X] T018 [US4] Run the live retained-identity survival test: `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter "FullyQualifiedName~096&FullyQualifiedName~T018"`; confirm in `Feature096LiveBridgeTests.fs` that a `Focused` indicator derived via the bridge stays on the **same retained identity** across a sibling-prepend that shifts its host, proven through the **live** `RetainedRender.init`/`step` path (`hand-seeded-state-by-identity=false`), where a position-keyed baseline loses identity — SC-007, contract §2 (FR-007).
- [X] T019 [US4] Run the responds-proof test: `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter "FullyQualifiedName~096&FullyQualifiedName~T020"`; confirm a focus/hover input restyles on the live retained path while an inert (un-bridged) build paints identical frames regardless of interaction (structural `Scene` inequality, not pixels) — SC-002 (responds), contract §5 non-goal disclosure.

**Checkpoint**: US4 confirmed — the runtime look is identity-stable and incrementally cheap (rides E2, bounded repaint), cohering with retained render rather than rebuilding per frame.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Whole-feature validation, readiness regeneration, and the recorded, bounded deviations/follow-ups (kept visible per Complexity Tracking).

- [X] T020 Run the full Feature 096 conformance pass in one shot across both assemblies — `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~096"` and `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter "FullyQualifiedName~096"` — and confirm all three suites green (16 + 3 + 2 tests); this is the gate `/speckit-implement` reduces to. **GREEN (2026-06-15):** Controls.Tests `~096` = 19/19 passed (16 `Feature096RuntimeBridgeTests` + 3 `Feature096BridgePropertyTests`); Elmish.Tests `~096` = 2/2 passed (`Feature096LiveBridgeTests`); surface-drift `~Surface` = 8/8 passed (T008). 29 tests, 0 failed, 0 skipped.
- [X] T021 [P] Confirm the readiness evidence is regenerated green by `Feature096LiveBridgeTests`: `ls specs/096-runtime-visual-state-bridge/readiness/` — expect `focus-survives-reshuffle.md` (`status=pass`, `retained-id-stable-across-shift=true`, `focused-state-before-shift=Focused`, `focused-state-after-shift=Focused`, `baseline-loses-identity-on-shift=true`) and `responds-proof.md` (`focus-input-restyles=true`, `press-input-restyles=true`, `un-bridged-build-is-inert=true`) — structural `Scene` evidence as disclosed in quickstart.md §4.
- [X] T022 [P] Execute the `quickstart.md` validation end-to-end (steps 1–4): build, run the three suites, confirm zero surface delta, and read the two readiness markdown files.
- [X] T023 [P] **DF-2 (bounded follow-up, recorded):** confirm the forward-looking `Selected` branch is disclosed — `deriveVisualState` derives `Selected` from `model.Selection`, but the live host (`ControlsElmish`) populates focus/hover/press and not the text-range `Selection`, so that branch is exercised by tests (T009/T015/T016) rather than the real render path today. The branch is total/deterministic regardless; wiring a host that tracks selection lights it up without a 096 code change — scoped follow-up, not part of this contract. **CONFIRMED (2026-06-15):** disclosed verbatim at the use site — `src/Controls/ControlRuntime.fs` carries a "Feature 102 (R8): forward-looking branch … never populates `model.Selection` … Kept (not removed) so a future host … derives `Selected` here without a code change" comment on the `elif model.Selection …` arm.
- [X] T024 [P] **DF-3 (bounded follow-up, recorded):** confirm the visible-restyle scope is disclosed — 096 widened `button`/`slider`/`text-box`/`radio-group`/`switch` (T012); `progress-bar`/`numeric-input` and other kinds are stamped but render no delta (T014) and are correctly inert at `Normal`. Widening the visible restyle to further kinds (093-styling territory) is scoped follow-up, not part of this contract.
- [X] T025 Verify the Constitution Check still holds post-conformance: zero public-surface delta (only `deriveVisualState`, on the `ControlRuntime` module committed at import; the bridge + feature-112 variants internal), all three suites green, the recorded deviations (import-inverted order; DF-1 `internal`-in-`.fsi`; DF-2 forward-looking `Selected`; DF-3 scoped restyle) unchanged and still justified — confirming the backfill restores the `Spec → .fsi → semantic tests → implementation` chain without adding behavior.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup (built libraries) — BLOCKS all user-story confirmation (the surface/projection/bridge/carrier must be confirmed first).
- **User Stories (Phases 3–6)**: All depend on Foundational. Once Phase 2 is green the test filters are independent and may run in parallel.
- **Polish (Phase 7)**: Depends on all desired story confirmations being complete.

### User Story Dependencies

- **US1 (P1)**: After Foundational — independent (precedence + no-attribute restyle + focus-indicator + scoped-restyle). The MVP slice every other story builds on (the projection + stamp).
- **US2 (P1)**: After Foundational — independent (at-rest identity + unmigrated-kind no-delta). Co-critical with US1: interaction styling is only trustworthy if the no-interaction case is provably inert.
- **US3 (P2)**: After Foundational — independent (arbitration + ≥1000-case properties), but the *behavior* refines US1's stamp with the consumer-vs-derived boundary.
- **US4 (P2)**: After Foundational — independent (bounded repaint + live retained survival + responds), but the *behavior* depends on US1 producing the stamped state and on feature 092's live `RetainedRender` path (E2).

### Within Each User Story

- The suites already ship and pass; each task **runs and confirms green** rather than authoring tests.
- No "models before services" ordering applies — the projection is one pure function and the bridge one pure walk, already implemented.

### Parallel Opportunities

- T002 ‖ T003 (after T001).
- All Foundational confirmations T004 ‖ T005 ‖ T006 (different declarations), then T007, then T008.
- Once Phase 2 completes, the filters map to independent runs: US1 (T009/T010/T011 ‖, then T012), US2 (T013 ‖ T014), US3 (T015 ‖ T016), US4 (T017 ‖ T018/T019).
- Polish: T021 ‖ T022 ‖ T023 ‖ T024.

---

## Parallel Example: User-story confirmation (after Phase 2)

```bash
# Each user story's filters run independently and concurrently:
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~096&FullyQualifiedName~T010"   # US1 precedence (T009)
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~096&FullyQualifiedName~T012"   # US2 at-rest identity (T013)
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~bridge properties"            # US3 ≥1000-case properties (T016)
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj    --filter "FullyQualifiedName~096&FullyQualifiedName~T018"   # US4 live retained survival (T018)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (build all three projects).
2. Complete Phase 2: Foundational (confirm projection/bridge/carrier/precedence, zero baseline delta).
3. Complete Phase 3: US1 — confirm precedence + no-attribute restyle + focus-indicator + scoped-restyle green.
4. **STOP and VALIDATE**: the US1 filters are green; the MVP slice (interaction-driven styling with zero consumer code) is proven.

### Incremental Delivery (conformance order)

1. Setup + Foundational → projection/bridge/carrier/precedence confirmed, zero delta.
2. US1 → precedence + no-attribute restyle + focus-indicator + per-widened-kind restyle green (MVP).
3. US2 → at-rest byte-identity + Scene-identity + zero-recompute + unmigrated-kind no-delta green.
4. US3 → consumer-vs-derived arbitration + ≥1000-case properties green.
5. US4 → bounded repaint + live retained-identity survival + responds-proof green.
6. Polish → full `~096` pass green (both assemblies), readiness regenerated, quickstart validated, deviations DF-1/DF-2/DF-3 confirmed recorded.

### Backfill Note

This is **not** a build. Per plan.md, `/speckit-implement` reduces to a conformance pass: confirm the
three suites are green, the readiness evidence regenerates, and the surface delta is zero. Do not
author new bridge behavior; the recorded deviations (import-inverted order, DF-1 `internal`-in-`.fsi`,
DF-2 forward-looking `Selected`, DF-3 scoped visible restyle) are bounded follow-ups, not work for this
feature.

---

## Notes

- [P] tasks = different files/filters, no dependencies.
- [Story] label maps each confirmation to the spec user story for traceability.
- Each user story's tests are independently runnable via their `--filter` (Expecto test names carry `T0xx` markers under the "Feature 096 …" test lists).
- The three suites already pass in the imported source — a red test is a regression to investigate, not a TODO to implement.
- The runtime look is painted by feature 093's resolver (E3) and rides feature 092's retained identity (E2); 096 derives + stamps only, so there is no draw/identity code to confirm beyond the bridge stamping the single `visualState` carrier.

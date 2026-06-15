---
description: "Task list for Feature 093 — Visual-State Style Layer (conformance backfill)"
---

# Tasks: Visual-State Style Layer (Feature 093)

**Input**: Design documents from `/specs/093-visual-state-style-layer/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/style-resolver.md

**Tests**: The four semantic suites already ship in the imported source. This feature is a **backfill
conformance pass** (the pattern feature 091 established): no new product behavior is built. Tasks
**confirm** the `.fsi` surface, the four executable suites, the frozen-oracle parity evidence, and the
zero public-surface-baseline delta — they do not author new resolver code. Where a task would normally
"write a test," it instead **runs and confirms the already-shipped suite green**.

**Organization**: Tasks are grouped by user story (US1–US4 from spec.md) so each story's contract can
be confirmed independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files/suites, no dependencies)
- **[Story]**: Which user story this task confirms (US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- Single F# project: `src/Controls/`, `tests/Controls.Tests/` at repository root
- Surface baseline: `tests/surface-baselines/FS.GG.UI.Controls.txt`
- Readiness evidence: `specs/093-visual-state-style-layer/readiness/parity/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Restore/build the libraries the conformance pass exercises.

- [X] T001 Restore and build the Controls library: `dotnet build src/Controls/Controls.fsproj` (net10.0, LangVersion=latest per `Directory.Build.props`); confirm a clean build with FS0078 promoted to error.
- [X] T002 [P] Build the test assembly `dotnet build tests/Controls.Tests/Controls.Tests.fsproj` and confirm it references the four `Feature093*` suites and reaches internals via `[<assembly: InternalsVisibleTo("Controls.Tests")>]`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the styling vocabulary, the resolver `.fsi` surface, and the public attributes exist as the contract pins them — these underlie every user story.

**⚠️ CRITICAL**: All US confirmations depend on this phase establishing that the shipped surface matches `contracts/style-resolver.md`.

- [X] T003 [P] Confirm the five styling types are declared on `src/Controls/Types.fsi` and match the contract: `ValidationState` (3 cases), `VisualState` (8 cases incl. `Validation of ValidationState`), `[<RequireQualifiedAccess>] StyleVariant` (6 cases), `StyleClass` (`Variant`/`Custom`), and the flat 7-field `ResolvedStyle`. Confirm `ResolvedStyle` is declared **before** `Theme` (D7 declaration-order requirement).
- [X] T004 [P] Confirm `Style.resolve` is declared in `src/Controls/Style.fsi` with signature `theme: Theme -> baseStyle: ResolvedStyle -> classes: StyleClass list -> state: VisualState -> ResolvedStyle`, and that the `.fsi` is the sole declaration of the public surface (Principle II).
- [X] T005 [P] Confirm the two public attribute builders `Attr.styleClasses` / `Attr.visualState` exist in `src/Controls/Attributes.fs` (declared in `Attributes.fsi`) with their `AttrValue.StyleClassesValue` / `AttrValue.VisualStateValue` carriers.
- [X] T006 Confirm zero public-surface delta: run the surface-drift check `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Surface"` and `grep -E "Style|VisualState|StyleVariant|StyleClass|ResolvedStyle|ValidationState" tests/surface-baselines/FS.GG.UI.Controls.txt` — expect the styling surface present and no baseline diff.

**Checkpoint**: Surface and vocabulary confirmed — user-story conformance can proceed (suites may run in parallel).

---

## Phase 3: User Story 1 - Styled by declaring a semantic variant (Priority: P1) 🎯 MVP

**Goal**: A control resolves to concrete, token-derived paint by attaching a semantic `StyleVariant` (or free-form `Custom`) class — never by reading palette roles or writing colour literals.

**Independent Test**: Resolve a neutral base under each of the six built-in `StyleVariant`s on one theme; confirm six pairwise-distinguishable token-derived `ResolvedStyle`s, `Primary` from the accent family and `Danger` from the danger family, a `Custom "promo"` flowing through the same fold, and an unknown `Custom` resolving exactly to the base.

### Confirmation for User Story 1

- [X] T007 [P] [US1] Run the variant-distinctness / variant-identity suite: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Feature 093 style resolver"`; confirm green coverage of SC-001 — six variants pairwise-distinguishable, `Primary`←accent family, `Danger`←danger family (contract C-6).
- [X] T008 [US1] Confirm in `tests/Controls.Tests/Feature093StyleResolverTests.fs` that a free-form `Custom "promo"` flows through the same fold and an unknown `Custom` resolves to the base (identity delta, no throw, no field drop) — backed by `applyCustom` (`src/Controls/Style.fs:44`), contract C-5.

**Checkpoint**: US1 contract (C-5, C-6) confirmed green and independently testable — MVP slice validated.

---

## Phase 4: User Story 2 - Appearance reflects interaction state, predictably (Priority: P1)

**Goal**: A control's look responds to its `VisualState` under one fixed precedence (`base < classes-in-attach-order < state`, last-writer-wins per field); state wins over class on shared fields, later class wins over earlier, and `Loading` inherits `Normal`.

**Independent Test**: Resolve a base under all eight `VisualState` cases (differentiated states distinct, `Loading == Normal`); resolve a class+overlapping-state and confirm the state field wins while non-overlapping class fields remain; resolve two classes and confirm the later wins; property-test (≥1000 inputs) purity/determinism/outermost-state.

### Confirmation for User Story 2

- [X] T009 [P] [US2] Confirm the state-precedence cases in `tests/Controls.Tests/Feature093StyleResolverTests.fs` (run via `--filter "FullyQualifiedName~Feature 093 style resolver"`): eight states distinct with `Loading == Normal`, `Disabled` (state) fill wins over `Primary` (class) while non-overlapping class fields remain, later-attached class wins — SC-002, contracts C-3/C-4.
- [X] T010 [P] [US2] Run the property suite: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Feature 093 resolver properties"`; confirm ≥1000 FsCheck cases (via the `Gen093` generator) prove purity/determinism (C-2), the state layer is provably outermost (C-3), and `resolve theme base [] Normal = base` (C-4) — SC-004.

**Checkpoint**: US1 + US2 together confirm the full resolver fold replaces procedural per-kind styling (C-2, C-3, C-4 green).

---

## Phase 5: User Story 3 - State-driven look survives an unrelated re-render (Priority: P2)

**Goal**: A control's visual state rides its attributes through the keyed reconciler diff and survives an unrelated, position-shifting re-render — proven through the live retained path, not a hand-seeded state map.

**Independent Test**: Build a keyed control in a non-`Normal` state with a class, render frame 1 via `RetainedRender.init`, step to a frame 2 that prepends an unrelated sibling (shifting the keyed control), and confirm it is found under the same key with state-driven paint identical in content (geometry aside) before and after.

### Confirmation for User Story 3

- [X] T011 [US3] Run the retained-state suite: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Feature 093 retained"`; confirm in `tests/Controls.Tests/Feature093RetainedStateTests.fs` that a `Disabled`+`Primary` keyed control resolves to its `Disabled` (muted) look — not the `Primary` accent — under the same key after a prepended sibling shifts it, via the live `RetainedRender.init`/`step` path (SC-005, contract C-10, depends on feature 092's retained identity).

**Checkpoint**: US3 confirmed — state survival proven on the live retained path (C-10 green).

---

## Phase 6: User Story 4 - Additive, scoped, parity-preserving migration (Priority: P2)

**Goal**: Replacing procedural styling with the resolver does not change what migrated `Button`/`CheckBox` paint (default no-class structurally scene-equal under every theme/state), an unmigrated kind is unaffected by an attached class, and the resolver is total over every input with all colours theme-sourced.

**Independent Test**: Compare migrated `Button`/`CheckBox` no-class render to a frozen inline reproduction of the pre-refactor geometry for each (kind, theme, state) and assert structural scene equality; attach a class to an unmigrated kind and assert zero render delta; property-test totality.

### Confirmation for User Story 4

- [X] T012 [P] [US4] Run the parity suite: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Feature 093 migration parity"`; confirm in `tests/Controls.Tests/Feature093ParityTests.fs` structural scene equality for migrated `Button`/`CheckBox` default no-class paint vs. the frozen procedural baseline under light **and** dark themes (SC-003, contract C-8), and that swapping the theme re-paints while structure is unchanged (SC-006, contract C-7).
- [X] T013 [P] [US4] Confirm the unmigrated-kind no-delta case in `Feature093ParityTests.fs`: attaching a style class to an unmigrated kind yields zero render-output delta (SC-007, contract C-9).
- [X] T014 [P] [US4] Confirm the six frozen-oracle scene artifacts exist and are regenerated green by the parity suite: `ls specs/093-visual-state-style-layer/readiness/parity/` shows `button.{light,dark}.normal.scene.txt`, `check-box.{light,dark}.normal.scene.txt`, `check-box-checked.{light,dark}.normal.scene.txt`.
- [X] T015 [US4] Confirm resolver totality from `Feature093StylePropertyTests` (already run in T010): `resolve` returns a `ResolvedStyle` for every `(theme, base, classes, state)` without throwing — every `StyleVariant` and every `VisualState` matched, any `Custom` accepted (FR-002/FR-004, contract C-1).

**Checkpoint**: All four stories confirmed — the layer is safe on the live path (C-1, C-7, C-8, C-9 green).

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Whole-feature validation and the recorded, bounded deviations/follow-ups (kept visible per Complexity Tracking).

- [X] T016 Run the full Feature 093 conformance pass in one shot — `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Feature 093"` — and confirm all four suites green; this is the gate `/speckit-implement` reduces to.
- [X] T017 [P] Execute the `quickstart.md` validation end-to-end (steps 1–4): run the four suites, inspect the six parity scenes, confirm zero surface delta, and optionally exercise the FSI identity/precedence/unknown-Custom snippets.
- [X] T018 [P] **DF-1 (Tier-2 cleanup — RESOLVED in this pass):** `src/Controls/Style.fs`'s eight helpers (`isDark`, `successColor`, `warningColor`, `applyVariant`, `applyCustom`, `applyClass`, `applyValidation`, `applyState`) previously carried redundant `private` modifiers; these have been stripped so visibility is `.fsi`-driven alone (FS0078-as-error keeps them private by omission). Confirmed behavior-neutral by a clean `dotnet build src/Controls/Controls.fsproj` — Principle II is now a clean pass.
- [X] T019 [P] **DF-2 (bounded follow-up, recorded):** confirm the migration-footprint gap is disclosed — six controls call `Style.resolve` (`Button`:840, `CheckBox`:735, `RadioGroup`:625, `Slider`:662, `Switch`:698, `TextBox`:1009 in `src/Controls/Control.fs`) but only `Button`/`CheckBox` have a frozen-oracle parity scene; the other four rely on the totality/purity proofs. Adding their parity scenes is scoped follow-up, not part of this contract.
- [X] T020 Verify the Constitution Check still holds post-conformance: zero public-surface delta, four suites green, the two recorded deviations (import-inverted order; redundant `private`) unchanged and still justified — confirming the backfill restores the `Spec → .fsi → semantic tests → implementation` chain without adding behavior.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup (built libraries) — BLOCKS all user-story confirmation (the surface/vocabulary must be confirmed first).
- **User Stories (Phases 3–6)**: All depend on Foundational. Once Phase 2 is green the four suites are independent and may run in parallel.
- **Polish (Phase 7)**: Depends on all desired story confirmations being complete.

### User Story Dependencies

- **US1 (P1)**: After Foundational — independent (Feature093StyleResolverTests).
- **US2 (P1)**: After Foundational — independent (Feature093StyleResolverTests + Feature093StylePropertyTests). Co-critical with US1.
- **US3 (P2)**: After Foundational — independent suite, but the *behavior* depends on US1/US2 producing styled output and on feature 092's retained identity.
- **US4 (P2)**: After Foundational — independent (Feature093ParityTests + the property totality from US2's T010). Protects US1–US3 rather than adding a journey.

### Within Each User Story

- The suites already ship and pass; each task **runs and confirms green** rather than authoring tests.
- No "models before services" ordering applies — the resolver is one pure function already implemented.

### Parallel Opportunities

- T002 ‖ (after T001).
- All Foundational confirmations T003 ‖ T004 ‖ T005 (different files), then T006.
- Once Phase 2 completes, the four suites map to independent filters and can run concurrently: T007/T008 (US1), T009/T010 (US2), T011 (US3), T012/T013/T014 (US4).
- Polish: T017 ‖ T018 ‖ T019.

---

## Parallel Example: User-story confirmation (after Phase 2)

```bash
# Each user story's suite runs independently and concurrently:
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Feature 093 style resolver"    # US1 + US2 (T007/T008/T009)
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Feature 093 resolver properties"    # US2 (T010), US4 totality (T015)
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Feature 093 retained"    # US3 (T011)
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Feature 093 migration parity"           # US4 (T012/T013/T014)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (build).
2. Complete Phase 2: Foundational (confirm surface/vocabulary, zero baseline delta).
3. Complete Phase 3: US1 — confirm variant identity + Custom flow green.
4. **STOP and VALIDATE**: `--filter "FullyQualifiedName~Feature 093 style resolver"` is green; the MVP slice (declarative token-derived styling) is proven.

### Incremental Delivery (conformance order)

1. Setup + Foundational → surface/vocabulary confirmed, zero delta.
2. US1 → variant identity green (MVP).
3. US2 → precedence + ≥1000-case purity/determinism/outermost-state green.
4. US3 → state survives a position-shifting re-render on the live retained path.
5. US4 → frozen-oracle parity, unmigrated no-delta, totality green.
6. Polish → full `~Feature093` pass green, quickstart validated, deviations DF-1/DF-2 confirmed recorded.

### Backfill Note

This is **not** a build. Per plan.md, `/speckit-implement` reduces to a conformance pass: confirm the
four suites are green, the parity oracle matches, and the surface delta is zero. Do not author new
resolver behavior; the recorded deviations (DF-1 redundant `private`, DF-2 four unpinned migrated
kinds) are bounded follow-ups, not work for this feature.

---

## Notes

- [P] tasks = different files/suites, no dependencies.
- [Story] label maps each confirmation to the spec user story for traceability.
- Each user story's suite is independently runnable via its `--filter`.
- The four suites already pass in the imported source — a red suite is a regression to investigate, not a TODO to implement.
- Colours must trace to `Theme`/`DesignTokens` tokens (no inline literals) — confirmed by code review + both-theme parity (C-7).

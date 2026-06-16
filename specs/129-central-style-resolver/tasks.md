---
description: "Task list for Central Visual-State Style Resolver (F4)"
---

# Tasks: Central Visual-State Style Resolver (`theme → kind → intent → states → style`) — Workstream F, Phase F4

**Input**: Design documents from `/specs/129-central-style-resolver/`

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/, quickstart.md

**Tests**: INCLUDED. The spec mandates an automated parity/totality/divergence check (FR-010, SC-001/003/007) as the always-runnable evidence that F4 is behaviour-neutral, total, and seam-capable. Test tasks are therefore first-class, not optional.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

F# UI rendering framework, single solution `FS.GG.Rendering.slnx`. Source under `src/`, tests under `tests/`, both at repo root. No new project; one new source file, one new test file, edits to existing project/control/test files.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm a clean baseline and capture the parity oracle before any code changes.

- [X] T001 Confirm clean pre-migration baseline: run `dotnet build FS.GG.Rendering.slnx -c Release` (expect 0 warnings / 0 errors under `TreatWarningsAsErrors=true`) and `dotnet test FS.GG.Rendering.slnx -c Release`; record the current pass/skip counts (the G7 integrity oracle) in the PR/working notes.
- [X] T002 [P] Record the surface/token drift baseline: run `dotnet fsi scripts/refresh-surface-baselines.fsx` then `git status --porcelain tests/surface-baselines/` and confirm it is empty before any change (the G6 oracle).
- [X] T003 [P] Capture the parity oracle literals: read the current `buttonGeom` structural base records at `src/Controls/Control.fs` (the filled `"button"` base — `Fill = theme.Accent`, `StrokeWidth = 0.0` — and the outline `"icon-button"` base — `Fill = transparent`, `Stroke = theme.Accent`, `StrokeWidth = 2.0`, lines ~823–839) and copy them verbatim into the working notes for reuse as the test oracle in T013.

**Checkpoint**: Baseline counts and oracle literals captured; tree still byte-identical to pre-F4.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Scaffold the internal module, project wiring, IVT grants, and test file. Both P1 stories depend on this.

**⚠️ CRITICAL**: No user-story work can begin until this phase is complete.

- [X] T004 Create the new file `src/DesignSystem/StyleResolver.fs` with `module internal StyleResolver` (no `.fsi`), `open`ing the DesignSystem types, and declare the `type IntentPolicy = { ApplyIntent: Theme -> string -> ResolvedStyle -> ResolvedStyle }` seam record (per data-model.md and contracts/resolver-contract.md). Leave function bodies as stubs to be filled in US1.
- [X] T005 Edit `src/DesignSystem/DesignSystem.fsproj`: add `<Compile Include="StyleResolver.fs" />` in correct order (after `Style.fs`, before any consumer), and add **one new** `InternalsVisibleTo` grant — `FS.GG.UI.Controls` (so `buttonGeom` can call the resolver). NOTE: the `Controls.Tests` grant already exists in this `.fsproj` — do NOT re-add it (avoid a duplicate entry).
- [X] T006 Verify the IVT additions are invisible to the public-surface gate: re-run `dotnet fsi scripts/refresh-surface-baselines.fsx` and confirm `git status --porcelain tests/surface-baselines/` is still empty (quickstart V5 / G6 — IVT is not public surface).
- [X] T007 Create the new test file `tests/Controls.Tests/Feature129CentralStyleResolverTests.fs` with the test module skeleton and trait/category names that match the quickstart filters (`Feature129` plus `parity`, `totality`, `divergence`), and register it via `<Compile Include="..." />` in `tests/Controls.Tests/Controls.Tests.fsproj` in the correct order.

**Checkpoint**: Solution builds with the empty internal module + IVT grants + empty test file; surface gate still clean. User-story implementation can begin.

---

## Phase 3: User Story 1 - One central path resolves a control's draw style from intent + state (Priority: P1) 🎯 MVP

**Goal**: A single total, deterministic resolution path `theme + kind + intent + state(s) → ResolvedStyle` exists, composing on the 093 back-half `Style.resolve`, with intent threaded as a consumed argument.

**Independent Test**: Call the path across the full `{kind} × {intent} × {state}` cross-product (incl. an unknown kind and unknown intent string) under a fixed theme; confirm a concrete style for every combination, no exception, deterministic across runs, and that the intent argument is reachable (consumed, not dropped).

### Tests for User Story 1 ⚠️ (write FIRST, ensure they FAIL before implementation)

- [X] T008 [P] [US1] Totality + determinism test (G3) in `tests/Controls.Tests/Feature129CentralStyleResolverTests.fs`: enumerate `{button, icon-button, an unknown/"Custom" kind} × {primary, secondary, danger, ghost, an unknown intent string} × {all 8 VisualState cases incl. a representative Validation}`, assert `StyleResolver.resolve neutralPolicy theme kind intent classes state` returns a concrete `ResolvedStyle` with zero exceptions, and that two repeated runs are byte-equal. (FR-004, SC-003)
- [X] T009 [P] [US1] Precedence-composition test in `tests/Controls.Tests/Feature129CentralStyleResolverTests.fs`: with non-empty `classes` and a non-`Normal` state, assert `resolve` equals `Style.resolve theme (baseStyleFor theme kind) classes state` — proving the front half only supplies `baseStyle` and the 093 `base < classes(attach order) < state` precedence is preserved, not replaced. (FR-006, contract guarantee 5)

### Implementation for User Story 1

- [X] T010 [US1] Implement `baseStyleFor : Theme -> string -> ResolvedStyle` in `src/DesignSystem/StyleResolver.fs`: a total `match` on kind — `"button"` → filled base, `"icon-button"` → outline base, any other/unknown → the filled base (defined fallback; never empty/transparent-only/exception), using the literals captured in T003. (FR-004, R5)
- [X] T011 [US1] Implement `neutralPolicy : IntentPolicy` (`ApplyIntent = fun _ _ s -> s`), `resolve : IntentPolicy -> Theme -> string -> string -> StyleClass list -> VisualState -> ResolvedStyle` (`= Style.resolve theme (policy.ApplyIntent theme intent (baseStyleFor theme kind)) classes state`), and `resolveDefault = resolve neutralPolicy` in `src/DesignSystem/StyleResolver.fs`. (FR-001, FR-002, contracts/resolver-contract.md)
- [X] T012 [US1] Run the US1 tests (`dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "Feature129&totality"`) and confirm T008/T009 now pass; build stays warning-clean.

**Checkpoint**: The central resolution path exists, is total/deterministic, composes with 093, and threads intent. Independently testable via the totality filter.

---

## Phase 4: User Story 2 - Migrating the Button is behaviour-neutral under the default theme (Priority: P1)

**Goal**: `buttonGeom` obtains its `baseStyle` from `StyleResolver.resolveDefault` (replacing the hardcoded `primary: bool` dispatch) and `faithfulContent` extracts + forwards the intent — with byte-identical default-theme output and unchanged suite pass/skip counts.

**Independent Test**: For the default theme, render `{button, icon-button} × {primary, secondary, danger, ghost} × {8 states}` through the migrated path and assert resolved `ResolvedStyle` and emitted `Scene` byte-equal the pre-migration oracle; run the full suite and confirm unchanged pass/skip counts.

**Dependency note**: Depends on US1 (the `resolveDefault` path must exist to call).

### Tests for User Story 2 ⚠️ (write FIRST, ensure they FAIL before the migration edit)

- [X] T013 [P] [US2] Parity (style) test (G1) in `tests/Controls.Tests/Feature129CentralStyleResolverTests.fs`: for the default `light` and `dark` themes, across `{button, icon-button} × {primary, secondary, danger, ghost} × {8 VisualState cases}`, assert `StyleResolver.resolveDefault theme kind intent classes state` byte-equals the oracle `Style.resolve theme <literal structural base for kind from T003> classes state`. (FR-003, SC-001)
- [X] T014 [P] [US2] Parity (scene) test (G2) in `tests/Controls.Tests/Feature129CentralStyleResolverTests.fs`: render the representative button set through the migrated dispatch and assert the emitted `Scene` (filled rect+text / outline stroke+text) is byte-identical to the pre-migration scene. (FR-003, SC-001)

### Implementation for User Story 2

- [X] T015 [US2] Migrate `buttonGeom` in `src/Controls/Control.fs` (~line 816): replace the `primary: bool` parameter with `kind: string` + `intent: string`, remove the inline hardcoded `baseStyle` literals (now relocated to `baseStyleFor`), and obtain `baseStyle` from `StyleResolver.resolveDefault theme kind intent classes state`. Keep all other geometry/text emission unchanged.
- [X] T016 [US2] Update `faithfulContent` in `src/Controls/Control.fs` (~line 1062): extract the intent from the existing `style` attribute (`textValueOf "style" control`, defaulting to a neutral `"primary"` when absent) and forward `kind` + `intent` into the `"button"`/`"icon-button"` dispatch (~lines 1095–1096) → `buttonGeom`. (FR-002, R3 — eliminates the dead-code intent drop)
- [X] T017 [US2] Confirm `src/Controls/Widgets/Primitives.fs` is unchanged behaviourally — intent still lowers to the `style` attribute string; do NOT alter the lowering or add a new attribute key (R3 alternative rejected).
- [X] T018 [US2] Run the parity tests (`dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "Feature129&parity"`) and confirm T013/T014 pass; build stays warning-clean (`dotnet build FS.GG.Rendering.slnx -c Release`, 0 warnings).

**Checkpoint**: Button is migrated; default-theme output byte-identical; both P1 stories complete → MVP deliverable.

---

## Phase 5: User Story 3 - Intent is a theme-overridable seam (capability, not yet exercised) (Priority: P2)

**Goal**: Prove the seam admits intent divergence through a non-default `IntentPolicy` supplied directly to `StyleResolver.resolve`, with zero edits to any control render code and no new control type.

**Independent Test**: Supply a divergent policy mapping `"danger"` to `theme.Danger`; assert the resolved `Danger` style differs from `Primary` under it, while remaining equal under `neutralPolicy` — reached through the resolver alone.

**Dependency note**: Depends on US1 (`resolve`/`IntentPolicy` must exist). Independent of US2.

### Tests for User Story 3 ⚠️

- [X] T019 [P] [US3] Divergence test (G4) in `tests/Controls.Tests/Feature129CentralStyleResolverTests.fs`: construct a non-default `IntentPolicy` whose `ApplyIntent` maps `"danger"` → `{ s with Fill = theme.Danger; Stroke = theme.Danger; Foreground = theme.Background }`; assert `resolve divergentPolicy theme kind "danger" classes state ≠ resolve … "primary" …`, AND that under `neutralPolicy` the same two are EQUAL (today's drop preserved by default). (FR-002, FR-005, SC-002, SC-007)
- [X] T020 [P] [US3] No-control-edit assertion (G5) in `tests/Controls.Tests/Feature129CentralStyleResolverTests.fs`: document/assert that the divergence in T019 is produced purely by calling `StyleResolver.resolve divergentPolicy …` with no edit to any control render function, and that the control-type count is unchanged. (FR-008, SC-006, SC-007)
- [X] T021 [US3] Run the divergence tests (`dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "Feature129&divergence"`) and confirm T019/T020 pass.

**Checkpoint**: All three user stories complete; the seam D2/Ant will consume is proven reachable.

---

## Phase 6: Polish & Cross-Cutting Concerns (Gates)

**Purpose**: Prove the global invariants — zero surface/token drift, suite integrity, render-loop neutrality.

- [X] T022 [P] Surface/token-drift gate (G6, V5): run `dotnet fsi scripts/refresh-surface-baselines.fsx` and assert `git status --porcelain tests/surface-baselines/` is empty; confirm the design-token-drift gate stays green (no regenerated public rows). An unchanged surface baseline also confirms **no `Theme` record shape change** — `Theme` is public, so any field add/remove would show as a baseline row. (FR-007, FR-012, SC-004)
- [X] T023 Full-suite integrity gate (G7, V6): run `dotnet test FS.GG.Rendering.slnx -c Release` and confirm pass/skip counts match the T001 baseline exactly — no test removed, skipped, or weakened. (FR-010, SC-005)
- [X] T024 [P] Render-loop neutrality check (G8, V6): confirm no edits touched the animation/layout/memoization/virtualization/cache/fingerprint seams (097/099/103/113/114/116/117/120/121); at-rest, settled, and cached behaviour byte-identical for identical inputs. (FR-011, SC-008)
- [X] T025 Final warning-clean build (V1) + dependency check: `dotnet build FS.GG.Rendering.slnx -c Release` → 0 warnings, 0 errors; and confirm **no new `<PackageReference>`/project dependency** was introduced by F4 (`git diff -- '**/*.fsproj'` shows only the T005 `Compile`/IVT additions — no new package, no JSON/web/DOM/icon-font dependency). (FR-009)
- [X] T026 Execute the full quickstart runbook V1–V6 in `specs/129-central-style-resolver/quickstart.md` end-to-end and confirm every validation passes.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories.
- **User Story 1 (Phase 3, P1)**: Depends on Foundational. The MVP path.
- **User Story 2 (Phase 4, P1)**: Depends on Foundational AND US1 (calls `resolveDefault`).
- **User Story 3 (Phase 5, P2)**: Depends on Foundational AND US1 (uses `IntentPolicy`/`resolve`). Independent of US2.
- **Polish (Phase 6)**: Depends on all desired user stories being complete.

### Within Each User Story

- Tests are written FIRST and must FAIL before the corresponding implementation task.
- US1: tests (T008–T009) → `baseStyleFor` (T010) → policy/resolve (T011) → green (T012).
- US2: tests (T013–T014) → `buttonGeom` (T015) → `faithfulContent` (T016) → Primitives unchanged (T017) → green (T018).
- US3: tests (T019–T020) → green (T021).

### Parallel Opportunities

- Setup: T002 and T003 run in parallel (T001 first to establish a clean build).
- US1 tests T008 and T009 are [P] (same file — author together, then split if needed).
- US2 tests T013 and T014 are [P]; US3 tests T019 and T020 are [P].
- Across stories: once Foundational is done, US3 (P2) can proceed in parallel with US2 (P1) since both depend only on US1, not on each other.
- Polish: T022 and T024 are [P] (independent gates); T023/T025/T026 are sequential verification.

---

## Parallel Example: User Story 2

```bash
# Author the two parity tests together (they fail until the migration lands):
Task: "Parity (style) test G1 in tests/Controls.Tests/Feature129CentralStyleResolverTests.fs"
Task: "Parity (scene) test G2 in tests/Controls.Tests/Feature129CentralStyleResolverTests.fs"

# Then the migration edits (same file, sequential):
Task: "Migrate buttonGeom in src/Controls/Control.fs"
Task: "Update faithfulContent in src/Controls/Control.fs"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 — both P1)

1. Phase 1: Setup — clean baseline + oracle literals.
2. Phase 2: Foundational — module + IVT + test scaffold (CRITICAL, blocks stories).
3. Phase 3: US1 — the central resolution path (totality green).
4. Phase 4: US2 — migrate the Button (parity green, byte-identical).
5. **STOP and VALIDATE**: full suite green with unchanged pass/skip counts → F4's acceptance is met (behaviour-neutral migration with the path live). This is the shippable MVP.

### Incremental Delivery

1. Setup + Foundational → scaffold ready, surface gate clean.
2. US1 → central path exists, total/deterministic → totality filter green.
3. US2 → Button migrated, default-theme byte-identical → parity filter green (MVP!).
4. US3 → seam divergence proven without control edits → divergence filter green (strategic payoff).
5. Polish → all global gates green (surface/suite/render-loop neutral).

### Notes

- [P] tasks = different files or independent verifications, no dependencies.
- [Story] label maps each task to its user story for traceability.
- Verify each test FAILS before its implementation task; commit after each task or logical group.
- Hard invariants throughout: zero public-surface delta, unchanged pass/skip counts, byte-identical default-theme output, no `Theme` shape change, no new dependency, no control forked per intent/theme.

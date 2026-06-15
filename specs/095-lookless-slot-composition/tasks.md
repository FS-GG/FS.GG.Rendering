---
description: "Task list for Feature 095 — Lookless Slot Composition (conformance backfill)"
---

# Tasks: Lookless Slot Composition (Feature 095)

**Input**: Design documents from `/specs/095-lookless-slot-composition/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/slot-composition.md

**Tests**: The `Feature095SlotCompositionTests` suite already ships in the imported source. This
feature is a **backfill conformance pass** (the pattern features 091 and 093 established): no new
product behavior is built. Tasks **confirm** the internal slot seam (`.fsi`), the typed front-door
props, the executable suite, the frozen-oracle parity evidence, and the zero public-surface-baseline
delta — they do not author new slot code. Where a task would normally "write a test," it instead
**runs and confirms the already-shipped suite green**.

**Organization**: Tasks are grouped by user story (US1–US3 from spec.md) so each story's contract can
be confirmed independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files/suites, no dependencies)
- **[Story]**: Which user story this task confirms (US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Single F# project: `src/Controls/`, `tests/Controls.Tests/` at repository root
- Surface baseline: `tests/surface-baselines/FS.GG.UI.Controls.txt`
- Readiness evidence: `specs/095-lookless-slot-composition/readiness/parity/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Restore/build the libraries the conformance pass exercises.

- [X] T001 Restore and build the Controls library: `dotnet build src/Controls/Controls.fsproj` (net10.0, LangVersion=latest per `Directory.Build.props`); confirm a clean build with FS0078 promoted to error.
- [X] T002 [P] Build the test assembly `dotnet build tests/Controls.Tests/Controls.Tests.fsproj` and confirm it references `tests/Controls.Tests/Feature095SlotCompositionTests.fs` and reaches the internal slot seam via `[<assembly: InternalsVisibleTo("Controls.Tests")>]` (declared in `src/Controls/Controls.fsproj`).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the slot vocabulary, the internal seam `.fsi` surface, and the typed front-door props exist as `contracts/slot-composition.md` pins them — these underlie every user story.

**⚠️ CRITICAL**: All US confirmations depend on this phase establishing that the shipped surface matches the contract.

- [X] T003 [P] Confirm the slot carrier on `src/Controls/Types.fsi`: the `Slot` discriminant on `AttrCategory` (~L375) and `SlotFillsValue of (string * Control<'msg>) list` on the already-public `AttrValue<'msg>` (~L415) — contract §1.
- [X] T004 [P] Confirm the internal slot seam is declared in `src/Controls/Control.fsi` (the sole declaration, Principle II): `slotFill: (string * Control<'msg>) list -> Attr<'msg>`, `slotFillsOf: Attr<'msg> list -> (string * Control<'msg>) list`, `slotFor: string -> Attr<'msg> list -> Control<'msg> option`, `lowerSlots: Control<'msg> -> Control<'msg>` — contract §3. Confirm `SlotName`/`slotName`/`slotRegions` are **absent** from the `.fsi` (private by omission).
- [X] T005 [P] Confirm the typed front door: `ButtonProps.Leading`/`Trailing` in `src/Controls/Widgets/Primitives.fsi` (~L38-39) and `PanelProps.Header`/`Footer` in `src/Controls/Widgets/Containers.fsi` (~L30-31), each `Widget<'msg> option`, lowered to `ControlInternals.slotFill` in the respective `.fs` (Primitives.fs ~L88-108, Containers.fs ~L126-137) — contract §2.
- [X] T006 Confirm zero public-surface delta: run the surface-drift check `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Surface"` and `grep -n "SlotFillsValue" tests/surface-baselines/FS.GG.UI.Controls.txt` — expect `FS.GG.UI.Controls.AttrValue\`1+SlotFillsValue` present and no baseline diff (the slot seam and props are internal/typed; the lone public entry was committed at import).

**Checkpoint**: Surface and vocabulary confirmed — user-story conformance can proceed (suites may run in parallel).

---

## Phase 3: User Story 1 - A consumer fills a control's named region with their own sub-tree (Priority: P1) 🎯 MVP

**Goal**: Through the typed front door, a fill is lowered into the control's `Children` at the correct position (`[leading; intrinsic; trailing]`), the slot carrier is consumed, `slotFor` distinguishes absent from present-but-empty, lowering is pure/total/deterministic, and there is no public free-form slot escape hatch.

**Independent Test**: Fill `Button.Leading` and confirm the fill appears in the lowered `Children` with the carrier consumed; fill `Leading`+`Trailing` and confirm two distinct correctly-ordered regions; fill `Panel.Header`+`Footer` and confirm `[header; body; footer]`; confirm `slotFor` resolves present (incl. empty) vs. absent; property-test (≥1000 inputs) purity/determinism/totality and no-slot identity; confirm no public `Attr.slot` / slot-name path.

### Confirmation for User Story 1

- [X] T007 [P] [US1] Run the placement suite: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~095 US1 slot placement"`; confirm green coverage of SC-001 — `Button.Leading`/`Trailing` land in two distinct `[leading; intrinsic; trailing]`-ordered regions, `Panel` children order `[header; body; footer]`, the slot carrier is consumed (no residue), `slotFor` returns `Some` for present (incl. present-but-empty) and `None` for absent (absent ≠ empty), and — when a control carries more than one `Slot`-category attribute — only the **last** is honored (last-writer-wins, FR-002) — contract §3 clauses 2/3, §1 absent≠empty + at-most-one/last-writer-wins rule. If the shipped suite does not already cover the multi-attribute last-writer-wins case, flag it as a missing assertion (do not weaken the check to green). **FLAGGED (2026-06-15):** the `slotPlacement` list (4 tests) covers placement/ordering/carrier-consumption/`slotFor`-absent≠empty but has **no dedicated assertion** for a control carrying two `Slot`-category attributes (only-the-last-honored, FR-002). The *behavior* is nonetheless correct: `slotFillsOf` reads via `AttrKeys.tryKey` which is `List.rev |> List.tryFind` (`src/Controls/Internal/AttrKeys.fs:47`), i.e. genuinely last-writer-wins. Gap is test-coverage only, not a defect; recorded as a bounded missing-assertion follow-up (not weakened to green).
- [X] T008 [P] [US1] Run the property suite: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~095 US1 lowering properties"`; confirm ≥1000 FsCheck cases (via the `Gen095` generator, `Feature095SlotCompositionTests.fs:109`) prove purity/determinism (identical `(kind, fills)` → identical IR), totality (never throws for any `(kind, fills)`), and additivity (no slot attribute ⇒ `lowerSlots` is the identity / byte-identical) — SC-005, contract §3 clauses 1/5.
- [X] T009 [US1] Run the typed-closure suite: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~095 US1 typed closure"`; confirm in `Feature095SlotCompositionTests.fs` that there is no public free-form `Attr.slot` builder or consumer slot-name string — the typed `Props` fields are the only authoring path, and the lookless/single-control non-goals hold (no template/selector/specificity/cascade) — SC-006, contract §2/§5 (FR-001, FR-008).

**Checkpoint**: US1 contract (§2, §3 clauses 1–3 & 5, §5) confirmed green and independently testable — MVP slice validated (region-targeted composition works through the closed typed front door).

---

## Phase 4: User Story 2 - An unfilled control is byte-identical to its pre-slot self (Priority: P1)

**Goal**: A slotted-capable control with nothing filled carries no slot attribute, gains no peripheral children, and renders structurally scene-equal to the frozen pre-slot baseline under both themes; exposure is scoped so a non-opted-in kind (`CheckBox`) is entirely unaffected.

**Independent Test**: Author an unfilled `Button`, confirm no slot attribute / no extra children, and assert structural scene equality to `readiness/parity/button.{light,dark}.normal.scene.txt` under both themes; lower an unfilled `Panel` and confirm it equals the legacy no-slot panel; confirm `CheckBox` gains no slots.

### Confirmation for User Story 2

- [X] T010 [P] [US2] Run the unfilled-parity suite: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~095 US2 unfilled byte-identity"`; confirm in `Feature095SlotCompositionTests.fs` that an unfilled `Button` carries no slot attribute and no peripheral children and is structurally scene-equal to the frozen baseline under light **and** dark themes (SC-002), an unfilled `Panel` lowers identical to the legacy no-slot panel, and a non-slotted `CheckBox` (via `slotRegions _ -> [],[]`, `src/Controls/Control.fs:148`) gains no slots (SC-007) — contract §3 clauses 1/4 (FR-003, FR-007).
- [X] T011 [P] [US2] Confirm the frozen-oracle scene artifacts exist and are regenerated green by the evidence list: run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~095 evidence capture"` and `ls specs/095-lookless-slot-composition/readiness/parity/` — expect `button.light.normal.scene.txt` and `button.dark.normal.scene.txt` (structural scene equality, not pixels, as disclosed in quickstart.md §4).

**Checkpoint**: US2 confirmed — the no-composition case is provably inert (byte-identity + frozen-scene parity), so the seam is safe on the live path. **DF-2 note**: `panel`'s unfilled case is proven by `lowerSlots`-identity (T010), not a frozen scene — capturing a `panel` oracle is bounded follow-up (see Phase 5 / plan Complexity Tracking).

---

## Phase 5: User Story 3 - Slotted content inherits dispatch, style, focus, and identity for free (Priority: P2)

**Goal**: Because a fill is lowered into ordinary `Children`, slotted content participates in every prior reconciler feature by construction — flat per-`ControlId` dispatch (E1), the feature-093 visual-state resolver (E3), focus/tab routing (E4), and retained identity (E2) across a position-shifting re-render via the live retained path — with no slot-specific special-casing.

**Independent Test**: Put a dispatching binding in a slot and confirm it dispatches (E1); put a `Danger`-classed control in a slot and confirm it resolves distinctly via the E3 resolver; put a focusable control in a slot and confirm it appears in `Focus.order` (E4); render a keyed slotted control at frame 1, step to a frame 2 that inserts a sibling above its host, and confirm via the live `RetainedRender` path it keeps its `RetainedId` and the stepped scene equals a full rebuild (E2).

### Confirmation for User Story 3

- [X] T012 [P] [US3] Run the composition suite: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~095 US3 slotted content composes"`; confirm in `Feature095SlotCompositionTests.fs` that a binding inside a slot dispatches through the flat per-`ControlId` mechanism (E1), a `Danger`-classed slotted control resolves distinctly via the feature-093 `Style.resolve` (E3), and a focusable slotted control appears in `Focus.order` (E4) — SC-003, contract §3 clause 6 (FR-004).
- [X] T013 [US3] Run the retained-identity suite: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~095 US3 slotted retained identity"`; confirm a keyed slotted control keeps its `RetainedId` across a sibling-prepend that shifts its host, and the stepped scene equals a full rebuild — proven through the **live** `RetainedRender.init`/`step` path, not a hand-seeded map (SC-004, contract §3 clause 6 / FR-005, depends on feature 092's retained identity).

**Checkpoint**: US3 confirmed — slotted content is a first-class citizen (E1–E4 inherited by construction, no special-casing).

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Whole-feature validation and the recorded, bounded deviations/follow-ups (kept visible per Complexity Tracking).

- [X] T014 Run the full Feature 095 conformance pass in one shot — `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~095"` — and confirm all seven test lists green; this is the gate `/speckit-implement` reduces to.
- [X] T015 [P] Execute the `quickstart.md` validation end-to-end (steps 1–4): build, run the suite, confirm zero surface delta, and inspect the two `button` parity scenes. **Note (2026-06-15):** quickstart §2 documented `--filter "FullyQualifiedName~Feature095"`, which matches **zero** tests (the Expecto lists are named `"095 …"`, not `"Feature095 …"`); corrected to `~095` (the filter tasks.md uses), which returns all 17 green. Both `button.{light,dark}.normal.scene.txt` inspected — 22-line frozen-oracle scenes with distinct light/dark fill colours.
- [X] T016 [P] **DF-1 (Tier-2 cleanup — disclosed, optional):** `src/Controls/Control.fs` carries `private` on `slotRegions`/`SlotName` (`:148`/`:133`) which have no `.fsi` entry, so they are already private by omission under FS0078-as-error. Confirm this is harmless duplication (not a second source of truth for a public symbol); stripping it is a behavior-neutral tidy that may ride a later pass — recorded, not required for this feature.
- [X] T017 [P] **DF-2 (bounded follow-up, recorded):** confirm the slot-exposure footprint is disclosed — `slotRegions` (`src/Controls/Control.fs:148`) opts in `button` and `panel`, but only `button.{light,dark}.normal.scene.txt` are captured under `readiness/parity/`; `panel`'s unfilled case relies on the `lowerSlots`-identity proof (T010). Capturing a frozen `panel` oracle (and any future opted-in kind) is scoped follow-up, not part of this contract.
- [X] T018 Verify the Constitution Check still holds post-conformance: zero public-surface delta (only `SlotFillsValue`, committed at import), all seven test lists green, the recorded deviations (import-inverted order; DF-1; DF-2) unchanged and still justified — confirming the backfill restores the `Spec → .fsi → semantic tests → implementation` chain without adding behavior.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup (built libraries) — BLOCKS all user-story confirmation (the surface/seam/props must be confirmed first).
- **User Stories (Phases 3–5)**: All depend on Foundational. Once Phase 2 is green the test lists are independent and may run in parallel.
- **Polish (Phase 6)**: Depends on all desired story confirmations being complete.

### User Story Dependencies

- **US1 (P1)**: After Foundational — independent (placement + properties + typed-closure lists). The MVP slice every other story builds on.
- **US2 (P1)**: After Foundational — independent (unfilled-parity + evidence lists). Co-critical with US1: composition is only trustworthy if the no-composition case is provably inert.
- **US3 (P2)**: After Foundational — independent (composition + retained-identity lists), but the *behavior* depends on US1 producing lowered children and on feature 092's retained identity (E2) and feature 093's resolver (E3).

### Within Each User Story

- The suite already ships and passes; each task **runs and confirms green** rather than authoring tests.
- No "models before services" ordering applies — the lowering is one pure function already implemented.

### Parallel Opportunities

- T002 ‖ (after T001).
- All Foundational confirmations T003 ‖ T004 ‖ T005 (different files), then T006.
- Once Phase 2 completes, the test lists map to independent filters and can run concurrently: T007/T008/T009 (US1), T010/T011 (US2), T012/T013 (US3).
- Polish: T015 ‖ T016 ‖ T017.

---

## Parallel Example: User-story confirmation (after Phase 2)

```bash
# Each user story's lists run independently and concurrently:
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~095 US1"   # US1 (T007/T008/T009)
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~095 US2"   # US2 (T010), + 095 evidence (T011)
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~095 US3"   # US3 (T012/T013)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (build).
2. Complete Phase 2: Foundational (confirm seam/props/carrier, zero baseline delta).
3. Complete Phase 3: US1 — confirm placement + ordering + `slotFor` + properties + typed closure green.
4. **STOP and VALIDATE**: `--filter "FullyQualifiedName~095 US1"` is green; the MVP slice (region-targeted composition through the closed typed front door) is proven.

### Incremental Delivery (conformance order)

1. Setup + Foundational → seam/props/carrier confirmed, zero delta.
2. US1 → placement/ordering/`slotFor`/properties/typed-closure green (MVP).
3. US2 → unfilled byte-identity + frozen-oracle parity (both themes) + scoped exposure green.
4. US3 → E1/E3/E4 composition + E2 retained-identity survival on the live retained path green.
5. Polish → full `~095` pass green, quickstart validated, deviations DF-1/DF-2 confirmed recorded.

### Backfill Note

This is **not** a build. Per plan.md, `/speckit-implement` reduces to a conformance pass: confirm the
suite is green, the parity oracle matches, and the surface delta is zero. Do not author new slot
behavior; the recorded deviations (import-inverted order, DF-1 redundant `private`, DF-2 only `button`
parity-pinned) are bounded follow-ups, not work for this feature.

---

## Notes

- [P] tasks = different files/lists, no dependencies.
- [Story] label maps each confirmation to the spec user story for traceability.
- Each user story's lists are independently runnable via their `--filter`.
- The seven test lists already pass in the imported source — a red list is a regression to investigate, not a TODO to implement.
- Slotted content inherits E1–E4 by construction (it rides ordinary `Children`); there is no slot-specific dispatch/style/focus/identity code to confirm beyond the composition lists.

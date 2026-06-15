# Tasks: Layout Cache — Incremental Re-Measure (Feature 097)

**Input**: Design documents from `/specs/097-layout-cache/`

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/layout-cache.md, quickstart.md

**Tests**: Test tasks ARE included — but as a **conformance pass**, not authored-from-scratch. Per the plan, the
implementation (the public `Layout.evaluateIncremental` evaluator in `src/Layout/`; `layoutDirtySet` + the
incremental `step` wiring + the two metrics in `src/Controls/RetainedRender.fs`/`.fsi`) and all three
Expecto/FsCheck suites **already exist** in the imported source. `/speckit-tasks` and `/speckit-implement`
reduce to confirming the suites are green and the public-surface delta is zero — **not** building new behavior.
The one genuine authoring task is the **readiness evidence**: unlike 092/099, feature 097 imported with **no
`readiness/` artifacts**, so this backfill authors them from the existing suites.

**Organization**: Tasks are grouped by user story to enable independent verification of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files/assemblies, no dependencies)
- **[Story]**: Which user story this task verifies (US1–US4)
- Exact file paths included in each task

## Path Conventions

Single F# solution (`FS.GG.Rendering.slnx`) — `src/` and `tests/` at repository root. No new project is added;
097 carries the previous frame's `LayoutResult` on the existing retained render record, derives the dirty set
from the reconcile patch, calls the already-public `Layout.evaluateIncremental`, and surfaces two internal
re-measure metrics.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the build environment and the artifacts under verification are present.

- [X] T001 Build the solution clean: `dotnet build FS.GG.Rendering.slnx -c Release` — expect 0 warnings, 0 errors (quickstart §1)
- [X] T002 [P] Confirm the public evaluator exists: `src/Layout/Layout.fsi` declares `evaluate` and `evaluateIncremental`, and `src/Layout/Types.fsi` declares `LayoutResult { Bounds; Diagnostics; Invalidated; Revision }` and `ComputedBounds`
- [X] T003 [P] Confirm the internal wiring exists in `src/Controls/RetainedRender.fs`/`.fsi`: the `Layout: FS.GG.UI.Layout.LayoutResult` cache field, the `internal layoutDirtySet` function, the incremental `Layout.evaluateIncremental` call in `step`, and the `RemeasuredNodeCount` / `LayoutInvalidatedNodeCount` fields on `WorkReductionRecord`
- [X] T004 [P] Confirm the three authoritative suites exist: `tests/Layout.Tests/Feature097IncrementalTests.fs`, `tests/Layout.Tests/Audit_IncrementalLayout.fs`, `tests/Controls.Tests/Feature097WiringTests.fs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The contract gates that MUST hold before any per-story verification is meaningful — the
public/internal signatures, the zero-new-public-surface invariant, and the lock-step name-set guard 097 depends on.

**⚠️ CRITICAL**: No user-story verification is trustworthy until these gates pass.

- [X] T005 Verify the evaluator signature in `src/Layout/Layout.fsi` matches contract C1 (`contracts/layout-cache.md`): `evaluateIncremental : previous: LayoutResult -> changedNodeIds: LayoutNodeId list -> available: AvailableSpace -> root: LayoutNode -> LayoutResult` (FR-001, FR-004, FR-007, FR-009)
- [X] T006 Verify the wiring signatures match contracts C3/C4/C5: `internal layoutDirtySet : prev -> patch -> next -> Set<string>`; the `Layout` cache field on the retained record; `RemeasuredNodeCount: int` and `LayoutInvalidatedNodeCount: int` on `WorkReductionRecord` (FR-002, FR-003, FR-005, FR-006)
- [X] T007 Confirm zero NEW public-surface delta (FR-011): regenerate via `dotnet fsi scripts/refresh-surface-baselines.fsx` and confirm `tests/surface-baselines/FS.GG.UI.Layout.txt` and `FS.GG.UI.Controls.txt` are **byte-unchanged** (the evaluator was already baselined; the wiring is `internal`). Confirm "no delta" via the file diff, not a script exit code (the script has no real `--check` mode — carried note from the 099 pass)
- [X] T008 Confirm the lock-step name-set guard passes: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "101"` — `Feature101LayoutDriftGuardTests` green, so `layoutDirtySet`'s read of `ControlInternals.layoutAffectingAttrNames` stays in lock-step with the layout lowering (097 consumes this; feature 101 owns it)

**Checkpoint**: Public/internal contract, surface invariant, and the name-set lock-step confirmed — per-story verification can proceed.

---

## Phase 3: User Story 1 - A localized geometry change re-measures only its boundary subtree (Priority: P1) 🎯 MVP

**Goal**: A single control's geometry change under a fixed-size ancestor re-measures only that boundary
subtree and reuses the cached bounds for the root and unrelated siblings — a strict subset — while producing
bounds byte-identical to a full re-measure.

**Independent Test**: For `root(stack) → [ panel(fixed 200×100) → [leafA, leafB] ; sibling ]`, change only
`leafA`'s width: `evaluateIncremental` re-measures `leafA` + `panel`, not the root or `sibling`, and equals a
full `evaluate`; on the wired `step` path, `0 < RemeasuredNodeCount < BaselineNodeCount` and the rendered
`Scene` equals a full `Control.renderTree`.

- [X] T009 [US1] Run the pure-evaluator suite: `dotnet test tests/Layout.Tests/Layout.Tests.fsproj -c Release --filter "097"` and confirm the fixed-size-boundary subset test in `tests/Layout.Tests/Feature097IncrementalTests.fs` is green — `Invalidated` contains `0.0.0` + boundary `0.0`, excludes the root, bounds equal a full evaluate (SC-001), and `Revision` advances by 1 (FR-001, FR-007)
- [X] T010 [US1] Run the wired suite: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "097"` and confirm the localized-edit test in `tests/Controls.Tests/Feature097WiringTests.fs` is green — `0 < RemeasuredNodeCount < BaselineNodeCount` and the wired `Scene` equals a full `Control.renderTree` (SC-001, SC-003, SC-005; FR-005, FR-008)
- [X] T011 [P] [US1] Author `specs/097-layout-cache/readiness/us1-partial-remeasure.md` from the two suites above against SC-001 (strict-subset re-measure under a fixed-size boundary; byte-identical bounds), disclosing the `DeterministicRenderOnly` / structural-equality scope (no pixel claim)

**Checkpoint**: US1 (the MVP headline payoff — partial re-measure) verified at both the evaluator and the wired layer.

---

## Phase 4: User Story 2 - The incremental result is always byte-identical to a full re-measure (Priority: P1)

**Goal**: Across any tree shape and any cumulative edit sequence, the incremental evaluator (carrying its
result forward) produces bounds byte-identical to a full `evaluate` at every step — the load-bearing INV-1
guarantee.

**Independent Test**: Over ≥1000 FsCheck-generated `(tree, edit-sequence)` cases, apply each cumulative edit
through both evaluators and assert the bounds maps are equal at every step. On the wired path, a localized
edit, a child insert, and a content-only change each render byte-identical to a full rebuild.

- [X] T012 [US2] Confirm the equivalence invariant in `tests/Layout.Tests/Feature097IncrementalTests.fs` is green — `evaluateIncremental` (cache carried) byte-identical to full `evaluate` over ≥1000 generated cases (SC-002, FR-007)
- [X] T013 [P] [US2] Confirm the audit cross-check `tests/Layout.Tests/Audit_IncrementalLayout.fs` is green (the spec-006 mechanism-audit lineage — equivalence + honest `Invalidated`)
- [X] T014 [US2] Confirm the wired byte-identity tests in `tests/Controls.Tests/Feature097WiringTests.fs` are green for localized-edit, child-insert, and content-only frames — each `Scene` equals a full `Control.renderTree` (SC-005; FR-005, FR-008)
- [X] T015 [P] [US2] Author `specs/097-layout-cache/readiness/us2-equivalence.md` from the FsCheck invariant + the wired parity tests against SC-002/SC-005 (byte-identity over ≥1000 cases and at the wired render), disclosing scope

**Checkpoint**: US1 + US2 (the full MVP — partial re-measure that is provably equivalent) verified.

---

## Phase 5: User Story 3 - The re-measure metric is honest — never under- or over-claims (Priority: P2)

**Goal**: `RemeasuredNodeCount` reports the actual re-measured set — strict subset for a localized edit,
exactly the baseline for a whole-tree relayout, 0 at rest — and `LayoutInvalidatedNodeCount` (pre-propagation)
is always `≤ RemeasuredNodeCount`.

**Independent Test**: On the wired path: a localized edit yields `0 < RemeasuredNodeCount < BaselineNodeCount`;
a root-orientation change (content-sized chain) yields `RemeasuredNodeCount = BaselineNodeCount`; an at-rest
frame yields `RemeasuredNodeCount = 0`. Over generated trees the partial set is a strict subset containing the
requested node; `Invalidated` is a proper superset of the request.

- [X] T016 [US3] Confirm the whole-tree-relayout test in `tests/Controls.Tests/Feature097WiringTests.fs` is green — a root geometry change re-measures the baseline (`RemeasuredNodeCount = BaselineNodeCount`; never under-reports) (SC-003, FR-010)
- [X] T017 [US3] Confirm the at-rest test in `tests/Controls.Tests/Feature097WiringTests.fs` is green — an identical-tree frame re-measures nothing (`RemeasuredNodeCount = 0`) and renders byte-identical (SC-003/SC-006, FR-006/FR-008)
- [X] T018 [P] [US3] Confirm the honest-`Invalidated` + strict-subset properties in `tests/Layout.Tests/Feature097IncrementalTests.fs` — the partial re-measured set is a strict subset containing the requested node, and `Invalidated` is a proper superset of the request (SC-008, FR-001); confirm `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount` holds by construction per `data-model.md` (FR-006)
- [X] T019 [P] [US3] Author `specs/097-layout-cache/readiness/us3-metric-honesty.md` against SC-003/SC-008 (subset/baseline/zero metric + honest superset `Invalidated`)

**Checkpoint**: The work-reduction metric is auditable and honest in all three regimes.

---

## Phase 6: User Story 4 - A non-geometry change does not dirty measure (Priority: P2)

**Goal**: A content/style/state/visual-state change does not enter the dirty set, so the frame re-measures
nothing while still repainting byte-identical; only a geometry attribute change or a non-`Keep` child op
dirties measure.

**Independent Test**: On the wired path, a content-only change yields `RemeasuredNodeCount = 0` and a
byte-identical `Scene`; a child insert under the boundary yields `RemeasuredNodeCount > 0` and stays
byte-identical.

- [X] T020 [US4] Confirm the content-only test in `tests/Controls.Tests/Feature097WiringTests.fs` is green — a text-only change re-measures nothing (`RemeasuredNodeCount = 0`) yet renders byte-identical to a full rebuild (SC-004, FR-003)
- [X] T021 [US4] Confirm the child-insert test in `tests/Controls.Tests/Feature097WiringTests.fs` is green — a child op dirties its container (`RemeasuredNodeCount > 0`) and stays byte-identical (FR-003)
- [X] T022 [P] [US4] Author `specs/097-layout-cache/readiness/us4-dirty-set-precision.md` against SC-004 (non-geometry change is measure-clean; child op / geometry attr dirties)

**Checkpoint**: All four user stories independently verified.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Whole-feature conformance closeout — full suite, surface gate, evidence mapping, cross-artifact
consistency, and the scoped-out boundaries.

- [X] T023 Run the full test suite (`dotnet test FS.GG.Rendering.slnx -c Release`) and confirm 0 failures across `Layout.Tests` and `Controls.Tests` (no regression introduced by the verification pass; 18 honest skips remain, unrelated to 097)
- [X] T024 Re-confirm zero new public-surface delta after the full run: `tests/surface-baselines/FS.GG.UI.Layout.txt` and `FS.GG.UI.Controls.txt` byte-unchanged (FR-011)
- [X] T025 [P] Verify the readiness → success-criteria mapping in `specs/097-layout-cache/quickstart.md` is accurate (each newly-authored `readiness/` file maps to SC-001…SC-008) and that every file discloses its `DeterministicRenderOnly` / structural-/bounds-equality scope (no pixel/desktop claim)
- [X] T026 [P] Confirm the documented scope boundary holds: 097 owns the measure/bounds cache + incremental re-measure; the paint-side partial repaint (091), the picture cache (116), the text-measure cache (117), and the name-set guard (101) are out of scope and owned by their features — not asserted by the Feature097 suites
- [X] T027 Record the inherited Tier-2 follow-up DF-1 (redundant `internal`/`private` access modifiers in `RetainedRender.fs`) as out-of-scope for this backfill (Complexity Tracking in plan.md) — not edited here
- [X] T028 Run `/speckit-analyze` to confirm cross-artifact consistency (spec ↔ plan ↔ tasks) per quickstart "Done When"

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup — confirms the contract signatures, the zero-new-surface gate,
  and the name-set lock-step that make all per-story verification meaningful. **Blocks Phases 3–6.**
- **User Stories (Phases 3–6)**: All depend on Foundational. Once it passes, all four can be verified in
  parallel (they read different suites / different assertions).
- **Polish (Phase 7)**: Depends on all desired user-story phases being verified.

### User Story Dependencies

- **US1 (P1)**: Independent — `Feature097IncrementalTests` (Layout.Tests) + `Feature097WiringTests` (Controls.Tests).
- **US2 (P1)**: Independent — the FsCheck equivalence invariant + `Audit_IncrementalLayout` + wired parity. The
  correctness foundation US1's partial re-measure rests on.
- **US3 (P2)**: Independent — the wired metric tests + the evaluator subset/superset properties.
- **US4 (P2)**: Independent — the wired dirty-set-precision tests.

### Parallel Opportunities

- Setup T002, T003, T004 are independent inspections — run in parallel.
- The pure-evaluator verification (Layout.Tests: T009, T012, T013, T018) runs fully in parallel with the
  wired verification (Controls.Tests: T010, T014, T016, T017, T020, T021) — different assemblies, different
  `--filter` runs.
- Every readiness-evidence authoring task marked [P] (T011, T015, T019, T022) writes a distinct file — run in parallel.
- Polish T025 and T026 are independent reads — run in parallel.

---

## Parallel Example: Verification fan-out

```bash
# The two suites are in different assemblies — run them concurrently:
dotnet test tests/Layout.Tests/Layout.Tests.fsproj     -c Release --filter "097"   # US1/US2/US3 (pure evaluator)
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "097"   # US1/US2/US3/US4 (wired path)

# Readiness-evidence authoring each touches a distinct file:
Task: "Author readiness/us1-partial-remeasure.md from the boundary-subset + wired-subset tests (SC-001)"
Task: "Author readiness/us2-equivalence.md from the FsCheck invariant + wired parity (SC-002/SC-005)"
Task: "Author readiness/us3-metric-honesty.md from the metric + superset tests (SC-003/SC-008)"
Task: "Author readiness/us4-dirty-set-precision.md from the content-only + child-insert tests (SC-004)"
```

---

## Implementation Strategy

> This is a **conformance backfill** (task C2 of the 2026-06-15 missing-features plan). "Implementation" =
> confirming the existing artifacts are green, authoring the missing readiness evidence, and confirming the
> contract holds — no new product behavior is built.

### MVP First (US1 + US2)

1. Phase 1: Setup — clean build + artifact presence.
2. Phase 2: Foundational — contract signatures + zero-new-surface gate + name-set lock-step (CRITICAL; blocks story verification).
3. Phase 3 + Phase 4: verify US1 (partial re-measure) and US2 (equivalence) at both the evaluator and wired layers.
4. **STOP and VALIDATE**: the MVP (a localized change re-measures only its boundary subtree, provably
   byte-identical to a full re-measure) is proven.

### Incremental Verification

1. Setup + Foundational → contract confirmed.
2. US1 → US2 → the MVP (P1) is proven.
3. US3 (P2) → metric honesty (subset / baseline / zero, honest `Invalidated`).
4. US4 (P2) → dirty-set precision (non-geometry change is measure-clean).
5. Polish → full suite, surface gate, readiness mapping, cross-artifact analyze.

---

## Notes

- [P] tasks = different files/assemblies, no dependencies.
- [Story] label maps each verification task to its user story for traceability.
- This pass must introduce **no** source edits beyond what is needed to make the suites green; if anything is
  red, that is a finding to report, not a license to redesign. The follow-up DF-1 cleanup is explicitly NOT
  performed here (T027).
- The single genuine authoring deliverable is the **readiness evidence** (T011, T015, T019, T022) — 097
  imported with executable suites but no captured artifacts; the suites are the authoritative proof.
- The surface-drift gate (T007, T024) is the direct verification of FR-011 (which has no separate SC).
- All proofs are deterministic, judged by bounds-map equality + structural scene equality; pixel/desktop
  visibility is out of scope and disclosed in each readiness file.
</content>

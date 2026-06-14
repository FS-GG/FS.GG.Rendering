---
description: "Task list for Define the Initial Validation Set (Migration Stage R3)"
---

# Tasks: Define the Initial Validation Set (Migration Stage R3)

**Input**: Design documents from `/specs/002-initial-validation-set/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Documentation/decision feature (no behavior-changing code). The spec and
quickstart use **review-based acceptance**, so there are no automated-test tasks.
Validation tasks below execute the quickstart scenarios (V1–V5) against the Success
Criteria.

**Organization**: Tasks are grouped by user story (from spec.md) for independent delivery.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in each task.

## Path Conventions

Durable artifacts live under `docs/validation/` at the repository root. Planning artifacts
and contracts live under `specs/002-initial-validation-set/`. No `src/` or `tests/` paths
are used — this stage writes no code and copies no tests (test import is Stage R4, harness
build is Stage R5).

Source-of-record for content: archived repo at `/home/developer/projects/FS-Skia-UI`
(`tests/**`, `readiness/surface-baselines`, `template/base/tests/Product.Tests`) and the
R2 outputs `docs/product/module-map.md` and `docs/product/docs-to-import.md`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the artifact home.

- [x] T001 Create the `docs/validation/` directory at the repository root.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the authoritative candidate list that all records reference.

**⚠️ CRITICAL**: Blocks the justification records (US1) and therefore the set and ledger.

- [x] T002 Enumerate the source candidate surface from `/home/developer/projects/FS-Skia-UI`: the 16 test projects under `tests/**` (Color/Scene/Layout/Input/KeyboardInput/Elmish/SkiaViewer/Controls/Testing/Lib/Smoke/Parity/Package/Governance/SkillSupport.Tests, ControlsPreview.Harness), `readiness/surface-baselines` + `scripts/refresh-surface-baselines.fsx`, `template/base/tests/Product.Tests`, and `readiness/`/`docs/testSpecs`; cross-reference each to `docs/product/module-map.md` (owned vs excluded) and record any deltas from `research.md` back into `specs/002-initial-validation-set/research.md`.

**Checkpoint**: Candidate list confirmed — record authoring can begin.

---

## Phase 3: User Story 1 - Every candidate check has a justified decision (Priority: P1) 🎯 MVP

**Goal**: A justification record for every candidate test/check (six fields + a decision) so no check is imported or dropped without a reason.

**Independent Test**: Pick any candidate; its record has product contract, failure mode, owner, frequency, cost, and a decision ∈ {import-now, defer, archive, rewrite-smaller} — none undecided (quickstart V1).

### Implementation for User Story 1

- [x] T003 [US1] Author `docs/validation/justification-records.md` per `specs/002-initial-validation-set/contracts/justification-record.schema.md`: one table row per candidate with columns Candidate, Product contract, Failure mode, Owner, Frequency, Cost, Decision, Note — covering all seven candidate classes (runtime unit, API surface-drift, package/consumer, template pack/install/instantiate, docs build, historical readiness, generated fixtures) per FR-007.
- [x] T004 [US1] In `docs/validation/justification-records.md`, set each Decision applying the plan defaults from `research.md` Decision 2 with per-candidate deviations (FR-008): runtime unit tests + Smoke → import-now; surface-baselines → import-now (ci); template `Product.Tests` → import-now (release/ci); `Package.Tests` → import-now only if it protects current consumers else defer; `Parity.Tests` → rewrite-smaller (describe smaller form, fold into Stage R5 harness); `Governance.Tests`/`SkillSupport.Tests` → archive; historical readiness reports/stale fixtures → defer/archive; unclear-contract candidates → defer with options.
- [x] T005 [US1] Validate `docs/validation/justification-records.md` against quickstart V1: every candidate has all six fields + a decision ∈ the four values, all seven classes present, none undecided (SC-001); `rewrite-smaller` rows describe the smaller form.

**Checkpoint**: Justification records complete — the MVP decision artifact exists and is independently reviewable.

---

## Phase 4: User Story 2 - The active set is bounded and frequency-labeled (Priority: P2)

**Goal**: A bounded "import now" set partitioned by frequency so the local tier stays fast and release-only checks are separate.

**Independent Test**: Every member is frequency-labeled, the Local inner-loop subset is enumerated and small, and Release-only is a separate group with no overlap (quickstart V2).

### Implementation for User Story 2

- [x] T006 [US2] Author `docs/validation/validation-set.md` per `specs/002-initial-validation-set/contracts/validation-set.schema.md`: list the `import-now` members from the justification records, partitioned into Local inner loop / CI / Release-only / Manual-advisory groups, each member referencing its justification row; include the one-paragraph "deliberately small, local tier is default" statement.
- [x] T007 [US2] Validate `docs/validation/validation-set.md` against quickstart V2: every member frequency-labeled and in exactly one group; Local inner-loop group enumerated and small enough for routine work (SC-002); Release-only separate from Local with zero overlap (SC-003).

**Checkpoint**: Justification records AND bounded active set both stand independently.

---

## Phase 5: User Story 3 - Deferred work is captured, not lost (Priority: P3)

**Goal**: Deferred/archived candidates and the rendering harness are recorded so intent is preserved without creating active obligations.

**Independent Test**: Every non-imported candidate appears in the ledger with a reason and non-binding marker; the harness has its own infrastructure record distinct from legacy tests (quickstart V3, V4).

### Implementation for User Story 3

- [x] T008 [P] [US3] Author `docs/validation/deferral-ledger.md` per `specs/002-initial-validation-set/contracts/deferral-ledger.schema.md`: a row for every candidate whose Decision is defer/archive/rewrite-smaller, with Status, Reason, and the "no — not an active obligation" marker; unresolved candidates recorded as `deferred` with options (FR-005, FR-010).
- [x] T009 [P] [US3] Author `docs/validation/harness.md`: record the rendering test harness as deliberate infrastructure (decision: build at Stage R5; display-agnostic parts — env probe, CLI skeleton, evidence schema — MAY scaffold earlier; reference tiers T0–T3/T-uinput), explicitly distinct from imported legacy tests; note `ControlsPreview.Harness`/`Parity.Tests` as related prior art to fold in, not import wholesale (FR-006).
- [x] T010 [US3] Validate against quickstart V3 (every non-import-now candidate in the ledger with a reason + non-binding marker — SC-004) and V4 (harness classified as deliberate infrastructure, distinct from legacy tests — SC-005).

**Checkpoint**: All R3 deliverables authored.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Index, final validation, and constitution-compliance sweep.

- [x] T011 [P] Add `docs/validation/README.md` indexing the four artifacts (justification-records, validation-set, deferral-ledger, harness) as the validation-strategy entry point.
- [x] T012 Run the full quickstart V1–V5 review and confirm the Stage R3 exit criteria are satisfiable (SC-006); re-confirm all items in `specs/002-initial-validation-set/checklists/requirements.md` still pass.
- [x] T013 [P] Compliance sweep: confirm no `src/` or `tests/` files were added, no tests/source copied, no harness code written (FR-009), and no removed governance machinery reintroduced; confirm excluded-module tests (`Governance.Tests`, `SkillSupport.Tests`) are archive/exclude, not import, consistent with the R2 module map.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories.
- **User Stories (Phase 3–5)**: All depend on Foundational. US2 and US3 derive from US1's
  records, so US1 comes first; US2 and US3 are then independent of each other.
- **Polish (Phase 6)**: Depends on the desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: After Foundational. No dependency on other stories. — MVP.
- **US2 (P2)**: After US1 (the active set is the `import-now` subset of US1's records).
- **US3 (P3)**: After US1 (the ledger is the non-`import-now` subset of US1's records).
- **US2 and US3** are independent of each other and can proceed in parallel after US1.

### Within Each User Story

- US1: T003 → T004 (same file, sequential) → T005 (validate).
- US2: T006 → T007 (validate).
- US3: T008, T009 parallel (different files) → T010 (validate).

### Parallel Opportunities

- After US1 completes, US2 and US3 can be authored in parallel.
- Within US3, T008 and T009 run in parallel.
- In Polish, T011 and T013 run in parallel; T012 runs after the artifacts exist.

---

## Parallel Example: User Story 3

```bash
# Author the two independent US3 artifacts together:
Task: "Author docs/validation/deferral-ledger.md"
Task: "Author docs/validation/harness.md"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup (T001).
2. Phase 2: Foundational (T002) — confirm the candidate list.
3. Phase 3: User Story 1 (T003–T005) — the justification records.
4. **STOP and VALIDATE**: review the records (V1). This alone makes the Stage R4 import
   deliberate and is the prerequisite reference for everything else.

### Incremental Delivery

1. Setup + Foundational → candidate list ready.
2. US1 (records) → validate → the MVP decision artifact.
3. US2 (bounded set) and US3 (ledger + harness) → validate.
4. Polish → index + full V1–V5 review + compliance sweep.

---

## Notes

- [P] tasks = different files, no dependencies.
- This stage produces Markdown only; "implement" = author the document to its contract.
- No code, no test/source copy, no harness build (FR-009) — enforced by T013.
- Stop at any checkpoint to validate a story independently.

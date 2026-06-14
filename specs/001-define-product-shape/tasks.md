---
description: "Task list for Define Product Shape (Migration Stage R2)"
---

# Tasks: Define Product Shape (Migration Stage R2)

**Input**: Design documents from `/specs/001-define-product-shape/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: This is a documentation/decision feature (no behavior-changing code). The spec and
quickstart use **review-based acceptance**, so there are no automated-test tasks. Validation
tasks below execute the quickstart scenarios (V1–V5) against the Success Criteria.

**Organization**: Tasks are grouped by user story (from spec.md) for independent delivery.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in each task.

## Path Conventions

Durable artifacts live under `docs/product/` at the repository root. Planning artifacts and
contracts live under `specs/001-define-product-shape/`. No `src/` or `tests/` paths are used —
this stage writes no code (source import is Stage R4).

Source-of-record for content: archived repo at `/home/developer/projects/FS-Skia-UI`
(`src/**`, `.template.config/`, samples) and its migration docs at
`/home/developer/projects/FS-GG.github/docs/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the artifact home.

- [x] T001 Create the `docs/product/` and `docs/product/decisions/` directories at the repository root.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the authoritative source inventory that all artifacts reference.

**⚠️ CRITICAL**: Blocks the module map (US1) and the docs-to-import list (US3).

- [x] T002 Verify the source module inventory against `/home/developer/projects/FS-Skia-UI/src` (Color, Controls, Controls.Elmish, Elmish, Input, KeyboardInput, Layout, Scene, SkiaViewer, SkillSupport, Testing), the template at `/home/developer/projects/FS-Skia-UI/.template.config`, and the 13 sample galleries; record any deltas from `research.md` Decision 1 back into `specs/001-define-product-shape/research.md`.

**Checkpoint**: Source inventory confirmed — user-story authoring can begin.

---

## Phase 3: User Story 1 - Maintainer can explain what rendering owns (Priority: P1) 🎯 MVP

**Goal**: A single product/module map that lets any reader state the rendering ownership boundary and each module's role without reading source code.

**Independent Test**: Hand `docs/product/module-map.md` to someone unfamiliar with the project; they correctly list the modules, their responsibilities, and the ownership boundary using the file alone (quickstart V1).

### Implementation for User Story 1

- [x] T003 [US1] Author `docs/product/module-map.md` per `specs/001-define-product-shape/contracts/module-map.schema.md`: ownership-boundary paragraph + module table (columns Area, Source module, Layer, Responsibility, Disposition, Reason) with a row for each FR-001 area (scene, color, layout, input, viewer, Elmish integration, controls, controls Elmish integration, testing helpers, template support).
- [x] T004 [US1] In `docs/product/module-map.md`, add the **Exclusions** subsection listing the Vulkan backend and governance/`SkillSupport` as explicitly not owned, each with a reason (per FR-003 and research.md Decision 1).
- [x] T005 [US1] Validate `docs/product/module-map.md` against quickstart V1: every FR-001 area present, zero blank dispositions (SC-002), boundary derivable from the file alone (SC-001).

**Checkpoint**: Module map complete — the MVP product-shape answer exists and is independently reviewable.

---

## Phase 4: User Story 2 - Layer boundaries are unambiguous (Priority: P2)

**Goal**: A layering document defining the four UI layers and the one-control-set rule so any change can be classified to a single layer.

**Independent Test**: Using `docs/product/layering.md` only, classify a new control, a new design-system primitive, a new theme, and a new kit to single, non-overlapping layers (quickstart V2).

### Implementation for User Story 2

- [x] T006 [US2] Author `docs/product/layering.md` per `specs/001-define-product-shape/contracts/layering.schema.md`, adapted from `/home/developer/projects/FS-GG.github/docs/design-and-controls.md`: Ownership paragraph + four Layer Definitions (semantic controls, design-system primitives, themes, design-specific kits) each with Owns / Does NOT own / Examples.
- [x] T007 [US2] In `docs/product/layering.md`, add the one-control-set rule (one semantic control set, many themes; reject `AntButton`/`FluentButton` forks with justification) and the decision-rule table (change type → layer) per FR-005.
- [x] T008 [US2] Validate `docs/product/layering.md` against quickstart V2 (all five classification cases resolve as expected, SC-003) and confirm no contradiction with the constitution Engineering-Constraints layering clause.

**Checkpoint**: Module map AND layering doc both stand independently.

---

## Phase 5: User Story 3 - Migration decisions are recorded (Priority: P3)

**Goal**: The open product-shape decisions (package identity, template ownership) and the docs-to-import list are written down so Stage R4 has no unresolved ambiguity.

**Independent Test**: Each decision record states choice + rationale + revisit trigger with no "decide later" gaps; the docs-to-import list carries a disposition on every entry (quickstart V3, V4).

### Implementation for User Story 3

- [x] T009 [P] [US3] Author `docs/product/decisions/0001-package-identity.md` per `specs/001-define-product-shape/contracts/decision-record.schema.md`: keep `FS.Skia.UI.*`, rebrand deferred to Stage R8, status `deferred`, with rationale (constitution alignment) and revisit trigger.
- [x] T010 [P] [US3] Author `docs/product/decisions/0002-template-ownership.md` per the decision-record contract: rendering repo owns the `dotnet new` template for now, status `accepted`, rationale, and revisit trigger (template cadence divergence).
- [x] T011 [P] [US3] Author `docs/product/docs-to-import.md` per `data-model.md`, each entry marked `import-as-is` / `adapt` / `exclude`. Candidate `/home/developer/projects/FS-GG.github/docs/` docs to triage: `design-and-controls.md` (→ adapt, becomes `layering.md`), `rendering-project.md`, `transition-and-boundaries.md`, `research-notes.md`, `rendering-implementation-plan.md`, `index.md`, `project-split-decision.md`; governance-only docs (`governance-project.md`, `governance-implementation-plan.md`, `implementation-plan.md`) and any historical readiness logs → `exclude`.
- [x] T012 [US3] Validate the three artifacts against quickstart V3 (definite decisions, package-identity agrees with constitution — SC-004) and V4 (every docs-to-import entry has a disposition — SC-005); confirm any deliverable that could not be resolved is recorded as a `deferred` decision record with options rather than omitted (FR-010).

**Checkpoint**: All R2 deliverables authored.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Index, final validation, and constitution-compliance sweep.

- [x] T013 [P] Add `docs/product/README.md` indexing the four artifacts (module map, layering, decisions, docs-to-import) as the product-shape entry point.
- [x] T014 Run the full quickstart V1–V5 review and confirm the Stage R2 exit criteria are satisfiable (SC-006); confirm no required deliverable was silently omitted — any unresolved item appears as a `deferred` decision record (FR-010); re-confirm all items in `specs/001-define-product-shape/checklists/requirements.md` still pass.
- [x] T015 [P] Compliance sweep: confirm no `src/` or `tests/` files were added, no legacy tests/code imported, and no removed governance machinery reintroduced (FR-009); verify `FS.Skia.UI.*` package identity is preserved across all artifacts.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS US1 and US3.
- **User Stories (Phase 3–5)**: All depend on Foundational. US1/US2/US3 are otherwise independent.
- **Polish (Phase 6)**: Depends on the desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: After Foundational. No dependency on other stories. — MVP.
- **US2 (P2)**: After Foundational. Independent of US1 (different file); references the same constitution layering rule.
- **US3 (P3)**: After Foundational. Independent of US1/US2 (different files).

### Within Each User Story

- US1: T003 → T004 (same file, sequential) → T005 (validate).
- US2: T006 → T007 (same file, sequential) → T008 (validate).
- US3: T009, T010, T011 parallel (different files) → T012 (validate).

### Parallel Opportunities

- Once Foundational (T002) completes, US1, US2, and US3 can be authored in parallel by different people.
- Within US3, T009/T010/T011 run in parallel.
- In Polish, T013 and T015 run in parallel; T014 runs after the artifacts exist.

---

## Parallel Example: User Story 3

```bash
# Author the three independent US3 artifacts together:
Task: "Author docs/product/decisions/0001-package-identity.md"
Task: "Author docs/product/decisions/0002-template-ownership.md"
Task: "Author docs/product/docs-to-import.md"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup (T001).
2. Phase 2: Foundational (T002) — confirm the source inventory.
3. Phase 3: User Story 1 (T003–T005) — the module map.
4. **STOP and VALIDATE**: review the map (V1). This alone answers "what does rendering own?" and unblocks Stage R3 thinking.

### Incremental Delivery

1. Setup + Foundational → inventory ready.
2. US1 (map) → validate → the MVP product-shape answer.
3. US2 (layering) → validate.
4. US3 (decisions + docs list) → validate.
5. Polish → index + full V1–V5 review + compliance sweep.

---

## Notes

- [P] tasks = different files, no dependencies.
- This stage produces Markdown only; "implement" = author the document to its contract.
- No code, no test import, no governance machinery (FR-009) — enforced by T015.
- Stop at any checkpoint to validate a story independently.

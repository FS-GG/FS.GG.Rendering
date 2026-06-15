---
description: "Task list for Stage R7 — Bridge the Old Repository"
---

# Tasks: Bridge the Old Repository (Migration Stage R7)

**Input**: Design documents from `/specs/007-bridge-old-repository/`

**Prerequisites**: plan.md, spec.md (required); research.md, data-model.md, contracts/, quickstart.md (present)

**Tests**: This feature is documentation-only. There is no product code to unit-test; its evidence is
the four **mechanical validation checks** defined in [`quickstart.md`](./quickstart.md) (link
integrity, provenance coverage, no-product-change guard, no-overclaim/no-rebrand grep). They are
grouped in Phase 7 (Validation) and each must be shown to fail under its discriminating-power
perturbation (Constitution Principle V).

**Organization**: Tasks are grouped by user story (US1–US4 from spec.md, in priority order) so each
story is an independently deliverable handoff increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1–US4 (user-story phases only)
- Exact file paths are included in every task

## Path note

All deliverables are Markdown at the **repository root** tree: `docs/bridge/**`, root `PROVENANCE.md`,
root `README.md`. No `src/`, `tests/`, `template/`, `*.props`, or `*.slnx` files are touched (FR-010).
The hub file `docs/bridge/README.md` is edited by **both US1 and US4** (different sections) — those
edits are sequential, never parallel.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the bridge folder and the hub skeleton so story phases fill distinct sections
without create-conflicts.

- [x] T001 Create the `docs/bridge/` directory at the repo root.
- [x] T002 Create hub skeleton `docs/bridge/README.md` with the six required section headings from [`contracts/bridge-hub.md`](./contracts/bridge-hub.md) — *Canonical home*, *What moved*, *Directional policy*, *Archive note*, *Identity status*, *Links* — each left as an empty placeholder to be filled by US1/US4.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the shared "imported top-level areas" reference that US2's coverage check and
US1's *What moved* summary both rely on.

**⚠️ CRITICAL**: Complete before user-story phases begin.

- [x] T003 Confirm the `IMPORTED` top-level area set from [`contracts/provenance-coverage.md`](./contracts/provenance-coverage.md) against the working tree (`ls src tests template .template.config .template.package docs/imported tests/surface-baselines scripts` + the root build-metadata files), and note any area not yet reflected in `PROVENANCE.md` — this list drives T005 and T008.

**Checkpoint**: Bridge folder, hub skeleton, and the imported-area reference exist — stories can begin.

---

## Phase 3: User Story 1 - Bridge document declares the canonical home (Priority: P1) 🎯 MVP

**Goal**: A discoverable bridge hub naming `FS.GG.Rendering` as the canonical home plus a copy-ready
old-repo redirect, so a visitor on the archived repo/packages reaches the live product.

**Independent Test**: From `docs/bridge/README.md` and `docs/bridge/old-repo-redirect.md`, a reader
learns the canonical home + source commit, and the redirect block is paste-ready under a
"not yet applied" header; the hub is reachable in one hop from `README.md`.

### Implementation for User Story 1

- [x] T004 [US1] Fill the *Canonical home* section of `docs/bridge/README.md`: state `FS.GG.Rendering` is the canonical home, name source repo `EHotwagner/FS-Skia-UI`, and pin import commit `f759f399` (FR-001).
- [x] T005 [US1] Fill the *What moved*, *Identity status*, and *Links* sections of `docs/bridge/README.md`: one-paragraph summary of imported areas (from T003) that **links** `../../PROVENANCE.md` without restating the path map; identity-retained one-liner linking `package-identity-migration.md`; working links to `old-repo-redirect.md`, `package-identity-migration.md`, `../../PROVENANCE.md`, `../product/decisions/0001-package-identity.md`, and the org profile `https://github.com/FS-GG/.github` (FR-002, FR-012 supportive). *(Same file as T004 — run after T004.)*
- [x] T006 [P] [US1] Create `docs/bridge/old-repo-redirect.md` per [`contracts/old-repo-redirect.md`](./contracts/old-repo-redirect.md): recorded-action header (target = archived old-repo README + `FS.Skia.UI.*` package pages; **Status: NOT yet applied**; read-only/archived acknowledged), a fenced copy-ready README banner block, a fenced package-page deprecation block (no rename claim), and an owner apply checklist (FR-004, FR-011).
- [x] T007 [P] [US1] Add a one-hop discoverability link to the root `README.md` pointing to `docs/bridge/README.md` (e.g., under Status or a "Migration / bridge" line) (FR-012, SC-008).

**Checkpoint**: US1 is the MVP handoff — canonical home declared, redirect paste-ready, bridge
discoverable.

---

## Phase 4: User Story 2 - Provenance and import-path lineage are first-class (Priority: P1)

**Goal**: `PROVENANCE.md` is bridge-grade and complete, so any imported file traces to its source and
the lineage survives the old repo going cold.

**Independent Test**: For every `IMPORTED` area, `PROVENANCE.md` has a path-map row or a named-gap
entry; every deliberate adaptation/exclusion carries a rationale; the pinned commit is present.

### Implementation for User Story 2

- [x] T008 [US2] Complete/verify the `PROVENANCE.md` path map so every `IMPORTED` area from T003 is covered by a path-map row **or** listed under a *Named gaps* subsection — no silent omissions (FR-003, SC-002).
- [x] T009 [US2] Verify/complete the `PROVENANCE.md` *Adaptations* and *Exclusions* sections: each deliberate divergence has a one-line rationale; record the repo-authored `FS.GG.Rendering.slnx` as an adaptation (not a 1:1 path-map row), per the contract note. *(Same file as T008 — run after T008.)*

**Checkpoint**: Lineage is complete and authoritative; the hub's *What moved* link (T005) now resolves
to a bridge-grade record.

---

## Phase 5: User Story 3 - Package/template migration note (Priority: P2)

**Goal**: A note that fixes the retained `FS.Skia.UI.*` identity and defers any rename to R8, so
"repo moved" is never read as "packages renamed."

**Independent Test**: The note lists each retained package/template identity as unchanged by the move,
links decision `0001`, and decides no rename.

### Implementation for User Story 3

- [x] T010 [P] [US3] Create `docs/bridge/package-identity-migration.md`: a retained-identity table (`FS.Skia.UI.Scene/.Layout/.Controls/...` and the template package ID, status **retained — unchanged by the repository move**), a deferral statement linking `../product/decisions/0001-package-identity.md` and Stage R8, and a non-decision disclaimer that this note neither decides nor begins a rebrand (FR-005, FR-006, SC-003).

**Checkpoint**: Identity confusion is actively prevented; R8 has a clean baseline.

---

## Phase 6: User Story 4 - Archive note and directional policy (Priority: P2)

**Goal**: The durable boundary — new work opens here; the old repo gets only bridge/archive/
provenance/emergency fixes; the old repo's historical artifacts are archive-only.

**Independent Test**: The hub's *Directional policy* and *Archive note* sections answer "where does new
work go?" and "what may change in the old repo?" unambiguously and mark old specs/reports/readiness as
archive-only.

### Implementation for User Story 4

- [x] T011 [US4] Fill the *Directional policy* section of `docs/bridge/README.md`: new rendering work opens in this repo; the old repo receives only bridge / archive / provenance / emergency-migration changes; governance experiments stay out of rendering work (FR-008, SC-004). *(Same file as T004/T005 — run after them.)*
- [x] T012 [US4] Fill the *Archive note* section of `docs/bridge/README.md`: the old repo's specs, reports, and readiness artifacts are archive-only history, not a second source of truth (FR-007). *(Same file as T011 — run after T011.)*

**Checkpoint**: The hub is complete; all six sections filled.

---

## Phase 7: Validation & Cross-Cutting (the evidence)

**Purpose**: Run the four mechanical checks from `quickstart.md`; each must be shown to fail under its
discriminating-power perturbation (Principle V). These are the feature's tests.

- [x] T013 [P] Run **Check 1 — Link integrity** (quickstart §Check 1): every in-repo link in `docs/bridge/*.md`, `PROVENANCE.md` cross-refs, and the new `README.md` link resolves; confirm a deliberately broken link is caught (FR-009, SC-005).
- [x] T014 [P] Run **Check 2 — Provenance coverage** (quickstart §Check 2) over `PROVENANCE.md` vs the `IMPORTED` set; confirm removing a row for an un-named area is caught (FR-003, SC-002).
- [x] T015 [P] Run **Check 3 — No-product-change guard** (quickstart §Check 3): `git diff --name-only main...HEAD` lists only `docs/bridge/**`, `PROVENANCE.md`, `README.md`, `CLAUDE.md`, `specs/007-bridge-old-repository/**`; zero `src/**`/`tests/**`/`*.props`/`*.slnx`/`template/**` (FR-010, SC-007).
- [x] T016 [P] Run **Check 4 — No-overclaim / no-rebrand grep** (quickstart §Check 4): recorded-action "NOT yet applied" header present; zero "applied" claims for the redirect; zero rename instructions / new package IDs across bridge artifacts (FR-006, FR-011, SC-003, SC-006).
- [x] T017 Cross-check the FR→artifact coverage map in [`data-model.md`](./data-model.md) and Success Criteria SC-001…SC-008 are all satisfied; record the validation result (pass + discriminating-power confirmation) in the feature's checklist notes.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: after Setup — produces the `IMPORTED` reference; blocks US1's *What moved* and US2.
- **User Stories (Phases 3–6)**: after Foundational. US1, US2, US3 are mutually independent; US4 edits the same hub file as US1 (sections), so US4 runs after US1's hub edits (T004/T005).
- **Validation (Phase 7)**: after all desired stories complete (checks read the finished artifacts).

### User Story Dependencies

- **US1 (P1)**: after Phase 2. Independent. *(MVP)*
- **US2 (P1)**: after Phase 2. Independent (different files: `PROVENANCE.md`). T005's link to `PROVENANCE.md` is satisfied once US2 completes, but US1 is testable on its own before then.
- **US3 (P2)**: after Phase 2. Fully independent (new file `package-identity-migration.md`).
- **US4 (P2)**: after US1's hub edits (shared file `docs/bridge/README.md`).

### Within Each Story

- US1: T004 → T005 (same file); T006, T007 parallel to each other and to T004/T005 (different files).
- US2: T008 → T009 (same file).
- US4: T011 → T012 (same file), both after T005.

### Parallel Opportunities

- Setup: T001 then T002 (T002 needs the dir).
- After Phase 2, these can run concurrently: **T006** (redirect), **T007** (README link), **T008→T009** (provenance, US2), **T010** (migration note, US3), alongside US1's hub edits **T004→T005**.
- Validation: T013, T014, T015, T016 all parallel; T017 last.

---

## Parallel Example: after Foundational (Phase 2)

```bash
# Independent-file tasks that can proceed together:
Task: "T006 [US1] Create docs/bridge/old-repo-redirect.md"
Task: "T007 [US1] Add bridge link to README.md"
Task: "T008 [US2] Complete PROVENANCE.md path-map coverage"
Task: "T010 [US3] Create docs/bridge/package-identity-migration.md"
# (US1 hub edits T004→T005 run on docs/bridge/README.md in parallel with the above,
#  then US4 T011→T012 fill the remaining hub sections.)
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 Setup → Phase 2 Foundational.
2. Phase 3 US1 (hub canonical-home + redirect + discoverability).
3. **STOP and VALIDATE**: a visitor reaches the canonical home from the redirect and from `README.md`.
   This alone is a shippable handoff.

### Incremental Delivery

1. Setup + Foundational → folder, hub skeleton, imported-area reference.
2. US1 → discoverable canonical home + paste-ready redirect (MVP).
3. US2 → bridge-grade provenance (the hub's lineage link is now authoritative).
4. US3 → retained-identity note (prevents rename confusion).
5. US4 → directional policy + archive note (the durable boundary; hub complete).
6. Phase 7 → run the four checks with discriminating-power perturbations.

### Notes

- [P] = different files, no incomplete-task dependency.
- The hub `docs/bridge/README.md` is shared by US1 (T004/T005) and US4 (T011/T012) — never edit it in parallel.
- No product code, build, identity, or template changes (FR-010) — the no-product-change guard (T015) enforces this.
- Out-of-repo changes (old-repo redirect, any org `.github` touch-up) ship as copy-ready content + a recorded action; never report them as applied (Principle VI).
- Commit after each task or logical group; stop at any checkpoint to validate a story independently.

---

description: "Task list for feature 251 — surface the keyboard-only host input boundary"
---

# Tasks: Surface the Keyboard-Only Host Input Boundary

**Input**: Design documents from `/specs/251-keyboard-host-boundary/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/boundary-note-surface.md, quickstart.md

**Tests**: Included — the spec requests a generated-product assertion that the surfaced note is **present** and
**accurate** to the emitted host contract (FR-005/FR-006, contract assertions A1–A5).

**Organization**: Grouped by user story. US1 (the in-file `Model.fs` comment) is the MVP; US2 (the keyboard-input
skill + fragment note) is an independent increment.

**Note on scope**: This is a **documentation/surfacing-only** feature. It ships **no** runtime change — no durable,
governance-scanned host file (`Program.fs`, `LayoutEvidence.fs`, `EvidenceCommands.fs`, `WindowOptions.fs`) is
touched, and no input capability is added. Every edit lands on a **replaceable / authoring** surface. Because there is
no new public API, no `.fsi`/surface baseline changes (Constitution II N/A).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 / US2 (setup, foundational, polish carry no story label)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the no-regression baseline and confirm the working surfaces exist.

- [x] T001 Confirm the working tree is on branch `251-keyboard-host-boundary` and the design docs are present under `specs/251-keyboard-host-boundary/` (plan, spec, research, data-model, contracts, quickstart).
- [x] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/251-keyboard-host-boundary/readiness/baseline.md` (runs EVERY test project — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge).
- [x] T003 [P] Locate and confirm the two surfacing targets exist and their current text: the game-branch input-wiring site in `template/base/src/Product/Model.fs` (`profile == "game"` branch — `paddleForKey` ~L135 and the `ViewerInput of ViewerKey * isDown` handler ~L209), and the skill/fragment pair `template/product-skills/fs-gg-keyboard-input/SKILL.md` + `template/fragments/keyboard-input/README.md`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the boundary facts against the **real emitted host contract** before any note is written, so the
surfaced text is accurate (FR-006), not asserted from memory. For a docs-only feature the honest "run the app" check
is **inspecting the actual generated host surface** — there is no runtime behavior to drive.

**⚠️ CRITICAL**: No surfacing task (US1/US2) may finalize wording until T004 confirms the contract.

> **⚠️ Early live confirmation (STANDING, do not omit).** The plan's boundary claim is an **unverified assumption
> until the real emitted contract is read**. T004 is that confirmation — the equivalent of the early live smoke run
> for a documentation-accuracy feature. If any fact below is now false on the current template, **stop and correct the
> planned wording** before writing it (quickstart Scenario 1).

- [x] T004 **Early live confirmation** (quickstart Scenario 1): read the shipped emitted surface and confirm, on the current template, that (a) `template/base/docs/api-surface/KeyboardInput/KeyboardInput.fsi` `ViewerKey` has **no** mouse/pointer case; (b) `template/base/docs/api-surface/SkiaViewer/SkiaViewer.fsi` shows `DispatchInput of ViewerKey * isDown`, `GeneratedAppHost … MapKey: ViewerKey -> bool -> 'msg option` with **no** `MapPointer`, `InteractiveAppHost … MapPointer: ViewerPointerInput -> Size -> 'model -> 'msg list`, and `val runApp`; (c) `template/base/src/Product/Program.fs` launches the game host via `Viewer.runApp`. Record the confirmed exact names/signatures in `specs/251-keyboard-host-boundary/readiness/contract-confirmation.md` — these are the strings the note and the test will use.
- [x] T005 Resolve the research open item (research Decision 3): confirm whether the boundary note belongs in `fs-gg-keyboard-input/SKILL.md` as-is (its stated scope is the `app` profile) or needs a small scope-widening note, and confirm which keyboard skill a **game** author actually reads. Record the decision in `contract-confirmation.md`. (The `Model.fs` comment — US1 — is authoritative regardless of this outcome.)
- [x] T006 Draft the test seam in `template/base/tests/Product.Tests/BehaviorTests.fs`: decide how A1 (comment present at the game input-wiring site) and A2/A3 (accuracy — emitted `ViewerKey` has no mouse case; `GeneratedAppHost` has `MapKey`, no `MapPointer`; `InteractiveAppHost` has `MapPointer`) are asserted against the **real** generated surface (prefer the emitted `.fsi`/host over a synthetic string, per Principle V). Confirm the assertion fails now (no note / not yet asserted).

**Checkpoint**: Boundary facts confirmed against the live contract and recorded; the test seam is drafted and red — surfacing can begin.

---

## Phase 3: User Story 1 — In-file boundary comment at the input-wiring site (Priority: P1) 🎯 MVP

**Goal**: An author opening the game starter `Model.fs` at the input-wiring site reads a comment stating the default
host is keyboard-only, `ViewerKey` has no mouse case, and mouse-aim requires the pointer-aware interactive host path.

**Independent Test**: Read the shipped starter `Model.fs`; confirm the boundary comment is present at the
`paddleForKey`/`ViewerInput` site; a fresh game scaffold builds clean and behavior tests pass.

### Tests for User Story 1

- [x] T007 [P] [US1] Add assertion A1 to `template/base/tests/Product.Tests/BehaviorTests.fs`: the game-starter input-wiring site carries the boundary note (default host keyboard-only + the pointer-aware alternative named). Ensure it FAILS before T008.

### Implementation for User Story 1

- [x] T008 [US1] Add the boundary comment in `template/base/src/Product/Model.fs`, **inside the `profile == "game"` branch only**, at the input-wiring site (adjacent to `paddleForKey` / the `ViewerInput` handler). Content per contract Surface A: (1) default game host (`Viewer.runApp` / `GeneratedAppHost`) is keyboard-only; (2) `ViewerKey` has no mouse/pointer case (`DispatchInput of ViewerKey * isDown`); (3) a mouse-aimed scheme requires `InteractiveAppHost` / `Controls.Elmish.runInteractiveApp` (`MapPointer`) — a different, non-default host wiring, not an edit here. Comment only — no change to `paddleForKey`, the `ViewerInput` handler, or any logic. Use the exact names from T004.
- [x] T009 [US1] Verify the edit is confined to the `profile == "game"` branch: the `app`/`governed`/`headless-scene` branches of `Model.fs` are byte-identical (diff-review the conditional boundaries `//#if`/`//#else`/`//#endif`).

**Checkpoint**: US1 delivers the MVP — the in-file comment an author cannot miss; A1 passes; the game scaffold builds.

---

## Phase 4: User Story 2 — Boundary note in the keyboard-input skill + fragment mirror (Priority: P2)

**Goal**: An author reading the keyboard-input product skill (and its fragment source) finds the same
capability-boundary note before wiring input.

**Independent Test**: Read `fs-gg-keyboard-input/SKILL.md` and `keyboard-input/README.md`; both carry the same
boundary note naming the keyboard-only default host and the pointer-aware interactive alternative.

### Implementation for User Story 2

- [x] T010 [P] [US2] Add a "Capability boundary" note to `template/product-skills/fs-gg-keyboard-input/SKILL.md` per contract Surface B: the game family's default persistent host is keyboard-only (`MapKey` / `ViewerKey`, no `MapPointer`); mouse-aimed input requires the pointer-aware interactive host (`InteractiveAppHost` / `runInteractiveApp`, `MapPointer`) rather than the default `runApp` path. Apply the scope decision from T005.
- [x] T011 [P] [US2] Mirror the same note in `template/fragments/keyboard-input/README.md` (fragment source parity, FR-004) so materialized skill and fragment source do not drift.
- [x] T012 [US2] Confirm parity between the two texts (they convey the same boundary and the same signpost to the pointer-aware host).

**Checkpoint**: US1 and US2 both independently done — the boundary is surfaced at the edit site and in the guidance.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Accuracy assertions, non-regression proof, manifest regen, and release readiness.

- [x] T013 [P] Add accuracy assertions A2/A3 to `template/base/tests/Product.Tests/BehaviorTests.fs`: the emitted `ViewerKey` exposes no mouse/pointer case; the emitted `GeneratedAppHost` exposes `MapKey` but no `MapPointer` while `InteractiveAppHost` exposes `MapPointer` — asserted against the real generated surface so the note cannot rot silently (FR-006).
- [x] T014 Regenerate the skill manifest (a shipped skill body changed): `dotnet fsi scripts/generate-skill-manifest.fsx` — refresh the `fs-gg-keyboard-input` digest; confirm no new skill id was introduced.
- [x] T015 Run the skill-parity check (`scripts/check-agent-skill-parity.fsx`) to confirm the product-skill and any mirror stay in parity after the note edits.
- [x] T016 Non-regression proof (A4/A5): scaffold a game product from the local feed and run `dotnet build` + `dotnet test`; confirm the durable spine, evidence tokens, and a starter swap still pass and no durable/governance-scanned host file changed. Confirm non-game profiles are byte-identical. Record in `specs/251-keyboard-host-boundary/readiness/`.
- [x] T017 Run quickstart.md Scenario 2 end-to-end and record the result (note present on both surfaces + accurate + clean build + no non-game/durable regression).
- [x] T018 [P] Update `template/base/docs/scaffold-map.md` IF T005 concluded the boundary should also be signposted there by the input/model-swap guidance (otherwise mark N/A with a one-line rationale).
- [x] T019 Refresh the no-regression baseline and diff against T002: confirm the only deltas are the added game-starter comment, the skill/fragment note, the new/updated tests, and the regenerated manifest digest.
- [x] T020 Draft `specs/251-keyboard-host-boundary/release-coordination.md` (STAGED; flip at merge) following the #138 precedent: Tier-2 template-content, publish-before-flip, `FS.GG.UI.Template` republish, then close #139 and roll up epic #137 on the org Coordination board.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. **Blocks** all surfacing wording — T004 must confirm the contract first.
- **US1 (Phase 3)**: Depends on Foundational (T004 for exact names, T006 for the test seam). MVP.
- **US2 (Phase 4)**: Depends on Foundational (T004 for exact names, T005 for the skill-scope decision). Independent of US1 — can run in parallel with US1 once Phase 2 is done.
- **Polish (Phase 5)**: Depends on US1 + US2 being complete (accuracy/non-regression assertions, manifest regen, release doc).

### Within Each User Story

- US1: T007 (test, must fail first) → T008 (comment) → T009 (branch-confinement check).
- US2: T010 ∥ T011 (skill + fragment, different files) → T012 (parity check).

### Parallel Opportunities

- T003 [P] in Setup.
- Once Phase 2 completes, **US1 and US2 can proceed in parallel** (different files: `Model.fs` vs skill/README).
- T010 [P] and T011 [P] edit different files and can run together.
- T013 [P] and T018 [P] in Polish edit different files.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (baseline).
2. Complete Phase 2: Foundational — **T004 confirms the boundary against the real emitted contract** before any wording.
3. Complete Phase 3: US1 — the in-file `Model.fs` comment + A1.
4. **STOP and VALIDATE**: fresh game scaffold builds clean; A1 passes; the comment is present at the edit site.
5. Demo: an author opening `Model.fs` meets the boundary.

### Incremental Delivery

1. Setup + Foundational → contract confirmed, test seam red.
2. US1 → the edit-site comment (MVP) → validate independently.
3. US2 → the skill + fragment note → validate independently.
4. Polish → accuracy assertions, manifest regen, non-regression proof, release-coordination doc.

---

## Notes

- [P] tasks = different files, no dependencies.
- No durable/governance-scanned host file is touched; every content edit is on a replaceable/authoring surface.
- The `Model.fs` comment (US1) is authoritative; the skill/fragment note (US2) reinforces it.
- Prefer asserting the note's accuracy against the **real** emitted surface over a synthetic string (Principle V).
- Commit after each task or logical group; keep the feature-250 `release-coordination.md` (untracked on this branch) out of 251 commits.

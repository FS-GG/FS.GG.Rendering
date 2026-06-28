---
description: "Task list for Feature 210 — Close the Lifecycle-Agnostic Template Epic"
---

# Tasks: Close the Lifecycle-Agnostic Template Epic

**Input**: Design documents from `/specs/210-lifecycle-template-closure/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: No automated product tests are requested (this feature ships no product `.fs`/`.fsi` change). The
"test" deliverable is the **acceptance harness** (`scripts/validate-published-acceptance.fsx`) that drives the
real published package, plus the quickstart validation in Polish. The harness IS the live evidence path.

**Organization**: Tasks are grouped by user story (US1 P1, US2 P2, US3 P3) for independent delivery.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 / US2 / US3 — user-story phases only
- Paths are repo-relative to `/home/developer/projects/FS.GG.Rendering/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the inputs the closure work depends on exist before any validation runs.

- [X] T001 Confirm the pinned published package is present in the local feed: verify `~/.local/share/nuget-local/FS.GG.UI.Template.0.1.51-preview.1.nupkg` exists and `dotnet new` engine is available; create `specs/210-lifecycle-template-closure/readiness/` for evidence output.
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/210-lifecycle-template-closure/readiness/baseline.md` (runs EVERY test project — solution + Package.Tests + samples — and records the full red/green set so pre-existing reds are known up front; this feature changes no product code, so the baseline is the reference, not a target to move).
- [X] T003 [P] Confirm coordination tooling: `gh auth status` and access to `FS-GG/FS.GG.SDD` issues + the `FS-GG` Projects v2 "Coordination" board (needed for US3); record any access gap in readiness as `environment-limited`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Pin the facts every story depends on, and **prove the published package behaves before writing
any conclusion**.

**⚠️ CRITICAL**: No user-story work begins until this phase completes.

> **⚠️ Early live smoke run (STANDING, do not omit).** The plan's premise — "the published package behaves
> like the working tree the children validated" — is an **unverified hypothesis** (spec edge case: child
> evidence may have drifted from the published artifact). T005 drives the real installed package BEFORE the
> harness/record is built, so the close/don't-close work rests on observed behavior, not inference.

- [X] T004 Build the gated-set + baseline reference map in `specs/210-lifecycle-template-closure/readiness/gated-set-map.md`: enumerate the Feature-204 gated lifecycle file set (`.specify/`, generated constitution, `.agents/`, `.claude/`, generated `AGENTS.md`/`CLAUDE.md`) by reading `.template.config/template.json`, and restate the pre-lifecycle baseline source (Features 204/206) the byte-identical check compares against. Per `research.md` R3/R4.
- [X] T005 **Early live smoke run**: install the pinned package (`dotnet new install FS.GG.UI.Template::0.1.51-preview.1 --add-source ~/.local/share/nuget-local/`), confirm `dotnet new list` resolves `fs-gg-ui` to the **package** (not the working-tree source), manually scaffold a spot-check matrix (at minimum `spec-kit/app`, `sdd/app`, `none/headless-scene`), eyeball gated-set present/absent + product present, then `dotnet new uninstall FS.GG.UI.Template`. Record the observation in `readiness/early-smoke.md` and explicitly confirm-or-replace the "published == child-validated working tree" hypothesis. Per `research.md` R1.
- [X] T006 [P] Draft the harness seam: scaffold `scripts/validate-published-acceptance.fsx` with the verdict-core skeleton + the three invocation modes (default / `--emit-report` / `FS_GG_RUN_PUBLISHED_ACCEPTANCE=1`) per `contracts/acceptance-harness.md`, no live loop body yet. This is the "draft the contract seam first" step.

**Checkpoint**: Published package confirmed to instantiate and gate correctly on a live spot-check; gated-set/baseline map fixed; harness seam in place — user-story work can begin.

---

## Phase 3: User Story 1 — Single epic-level acceptance against the published template (P1) 🎯 MVP

**Goal**: One consolidated, version-pinned, reproducible Epic Acceptance Record proving the published template
emits Spec Kit only when asked, with a Rendering-side close/don't-close conclusion.

**Independent Test**: Run the live harness against the installed published package; confirm
`readiness/epic-acceptance.md` shows all 4 profiles gated-present under `spec-kit` (byte-identical default),
gated-absent under `sdd`/`none`, `none` carrying no orchestrator marker, and one CLOSE conclusion — readable
without opening the 204/205/206 folders.

- [X] T007 [US1] Implement the live loop in `scripts/validate-published-acceptance.fsx`: install the pinned package into an isolated store, run the 3 lifecycle × 4 profile matrix (`app`, `headless-scene`, `governed`, `sample-pack`) via real `dotnet new fs-gg-ui`, and uninstall/restore on exit (incl. on failure). Per `contracts/acceptance-harness.md` behavior steps 1–2, 5 (cleanup).
- [X] T008 [US1] Implement the assertions in the harness (step 3): `spec-kit` (and no-flag default) → gated set present + `diff-vs-baseline=none` (presence AND content) all profiles; `sdd` → gated absent, product present, default−sdd differs only in gated paths; `none` → gated absent + no orchestrator marker + `none == sdd`; unknown value rejected. Fail loudly on any misclassification (Constitution VI). Then implement the **build spot-check** (harness step 4): `dotnet build` the `app`-profile `sdd` and `none` outputs, asserting exit 0 — or record `buildability: environment-limited` if the toolchain/restore is absent (disclosed, never a silent pass). Per `research.md` R3/R4/R8 and `contracts/acceptance-harness.md` steps 3–4.
- [X] T009 [US1] Implement the record writer in the harness: emit `specs/210-lifecycle-template-closure/readiness/epic-acceptance.md` satisfying `contracts/acceptance-record.md` (validated_package `FS.GG.UI.Template 0.1.51-preview.1`, tag_anchor `fs-gg-ui/v0.1.51-preview.1` + missing-template-tag note, provenance, per-lifecycle table, byte-identical section, build spot-check section (`buildability: pass | environment-limited`), reproduction commands, remainder placeholders, conclusion). Include the `provenance: verdict-core` synthesized fallback for `--emit-report`. Per `research.md` R5.
- [X] T010 [US1] Run the live acceptance: `FS_GG_RUN_PUBLISHED_ACCEPTANCE=1 dotnet fsi scripts/validate-published-acceptance.fsx`; confirm exit 0 and that `readiness/epic-acceptance.md` is written with `provenance: live`, zero misclassified gated files (SC-002), zero byte diffs on the default (SC-003), and `buildability: pass` for the `sdd`/`none` `app`-profile build spot-check (SC-007) — or a disclosed `environment-limited` if the build toolchain is absent.
- [X] T011 [US1] Validate the record is self-contained (SC-001): a reviewer reaches the Rendering-side close/don't-close decision from `epic-acceptance.md` alone; verify it rolls up (not merely links) per-value results and pins a reproducible version. Fix any gap in the writer (T009) and re-run T010 if needed.

**Checkpoint**: The epic can be closed on the Rendering side on evidence — a single live, pinned, reproducible record exists.

---

## Phase 4: User Story 2 — Consumer lifecycle guidance and migration note (P2)

**Goal**: Discoverable consumer guidance so a first-time reader picks the correct lifecycle value and can
migrate from the pre-lifecycle template.

**Independent Test**: A reader unfamiliar with the template maps all three scenarios (governed / SDD-composed
/ standalone) to the right value from `.template.package/README.md` alone, with no maintainer help.

- [X] T012 [US2] Add the lifecycle decision tree + per-value include/exclude table to `.template.package/README.md` (governed→`spec-kit`, SDD-composed→`sdd`, standalone→`none`), per `contracts/lifecycle-guidance.md` sections 1–2 (FR-007).
- [X] T013 [US2] Add the explicit standalone-`none` statement to `.template.package/README.md`: "no governance and no orchestrator are attached or expected" (FR-008), per `contracts/lifecycle-guidance.md` section 3.
- [X] T014 [US2] Add the migration note to `.template.package/README.md`: pre-lifecycle → lifecycle-aware is a drop-in upgrade; selecting the default (`spec-kit`/omit flag) reproduces prior output (FR-009), per `contracts/lifecycle-guidance.md` section 4.
- [X] T015 [US2] Verify SC-004 against `quickstart.md` Scenario 3: `grep -n "lifecycle" .template.package/README.md` shows all required content; a cold read maps the three scenarios correctly.

**Checkpoint**: The shipped `--lifecycle` parameter is documented and adoptable without maintainer consultation.

---

## Phase 5: User Story 3 — Track the cross-repo remainder and record true closure state (P3)

**Goal**: Every SDD-owned remaining item tracked exactly once and referenced from the closure record; the
Coordination board reflects Rendering-side complete with remainder attributed to owning repos.

**Independent Test**: From the closure record + board, a reader enumerates every item required for *full*
closure, sees its owning repo, and follows a tracked request for each — no untracked blockers, no duplicates.

- [X] T016 [US3] Confirm and reuse the existing scaffold-path request: `gh issue view FS-GG/FS.GG.SDD#1`; verify it still covers the git-init/chmod obligations and is open. Do NOT file a new one (FR-010, dedupe edge case).
- [X] T017 [US3] Capture the constitution-ownership decision for `lifecycle=sdd`: `gh issue list --repo FS-GG/FS.GG.SDD --search "constitution ownership lifecycle"`; reference the existing tracked item if one exists, else create exactly one decision item (FR-010).
- [X] T018 [US3] Reference both remainder items by URL from the closure record's "Cross-repo remainder" section in `specs/210-lifecycle-template-closure/readiness/epic-acceptance.md` (replacing the T009 placeholders); confirm exactly one tracking link per item (SC-005).
- [X] T019 [US3] Update the `FS-GG` Projects v2 "Coordination" board: set the P1 epic to Rendering-side complete, attribute each remainder item to its owning repo, and keep `epic_fully_done = false` while any remainder is open (FR-011, SC-006). Record the board change in `readiness/closure-state.md`. If board access is `environment-limited` (T003), record the intended transition + the exact `gh project` command as the disclosed substitute.

**Checkpoint**: The board is honest — Rendering-done is distinguished from epic-fully-done, with every remainder visibly owned and tracked.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T020 Flag the missing-template-tag follow-up: confirm `epic-acceptance.md` records the absence of a dedicated template tag at 0.1.51 and proposes `git tag fs-gg-ui-template/v0.1.51-preview.1` (per `research.md` R2) — do NOT create the tag here (out of this feature's acceptance/guidance/coordination scope unless the user asks).
- [X] T021 Run full quickstart validation (`specs/210-lifecycle-template-closure/quickstart.md` Scenarios 1–4) end-to-end and confirm every "Done when" box is satisfiable; fix any gap in the owning phase and re-run.
- [X] T022 [P] Capture per-phase feedback via the `fs-gg-feedback-capture` skill into `specs/210-lifecycle-template-closure/feedback/` (process friction, generalizable-code candidates from the published-package harness, severity).
- [X] T023 Final scope check against FR-012: confirm no change to the template's generated output (the harness only reads/installs; guidance edits live in `.template.package/README.md`, which is consumer doc, not generated product output) and no other-repo behavior was implemented.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: depends on Setup; **blocks all user stories**. T005 (early live smoke run) gates everything downstream.
- **US1 (Phase 3)**: depends on Foundational. The MVP.
- **US2 (Phase 4)**: depends on Foundational; independent of US1 (edits a different file — `.template.package/README.md`). Can run in parallel with US1.
- **US3 (Phase 5)**: depends on Foundational; T018 writes into the record produced by US1 (T009), so T016/T017 are independent but **T018 depends on T009**.
- **Polish (Phase 6)**: depends on US1–US3 being complete (T021 quickstart covers all; T023 final scope check).

### Within Each User Story

- US1: T007 → T008 → T009 (same file, sequential) → T010 (live run) → T011 (validate).
- US2: T012 → T013 → T014 (same file, sequential) → T015 (verify).
- US3: T016, T017 independent; → T018 (needs T009 record) → T019 (board).

### Parallel Opportunities

- T003 [P] alongside T001/T002.
- T006 [P] alongside T004/T005 reasoning (different file).
- **US2 (Phase 4) can run fully in parallel with US1 (Phase 3)** — different files, no shared state.
- T016 and T017 can run in parallel.
- T022 [P] in Polish.

---

## Parallel Example: US1 + US2 after Foundational

```bash
# Developer A drives US1 (the harness + record):
Task: "Implement live loop in scripts/validate-published-acceptance.fsx (T007)"

# Developer B drives US2 (consumer guidance) concurrently — different file:
Task: "Add lifecycle decision tree to .template.package/README.md (T012)"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → Phase 2 Foundational (incl. the **early live smoke run** T005 that confirms the published
   package behaves before any record is written).
2. Phase 3 US1 → the single live, pinned, reproducible Epic Acceptance Record.
3. **STOP and VALIDATE**: a reviewer can reach the Rendering-side close decision from one file (SC-001). This
   alone makes the epic closeable on the Rendering side.

### Incremental Delivery

1. Foundational ready → US1 (MVP: epic closeable on evidence).
2. + US2 → the capability becomes adoptable (guidance + migration).
3. + US3 → the board is honest and the remainder is tracked → full closure state recorded.
4. Polish → quickstart re-run, tag follow-up flagged, feedback captured, scope confirmed.

---

## Notes

- [P] = different files, no dependencies.
- This feature changes **no product code** and **no generated template output** (FR-012); the only executable
  artifact is the read-only/installer acceptance harness `.fsx`.
- The live acceptance run (T010) is the authoritative evidence; a `verdict-core` record is a fresh-checkout
  fallback only and is NOT close-eligible (Constitution V).
- Commit after each phase or logical group; reuse — never duplicate — open cross-repo requests.

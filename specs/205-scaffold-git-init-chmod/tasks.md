---
description: "Task list for Feature 205 — move git-init / chmod out of fs-gg-ui template post-actions"
---

# Tasks: Move git-init / chmod Out of the fs-gg-ui Template Post-Actions

**Input**: Design documents from `/specs/205-scaffold-git-init-chmod/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/fs-gg-ui-template-generation.md ✅, quickstart.md ✅

**Tests**: REQUESTED. The plan (§Testing) and Constitution Check (V. Test Evidence) mandate a
Feature-205 behavioral test that fails before (default generation currently auto-inits) and passes
after. Test tasks are therefore included and written before the implementation they cover.

**Organization**: Tasks are grouped by user story (US1 P1, US2 P1, US3 P2) so each story can be
implemented and tested independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: `[US1]` / `[US2]` / `[US3]` — Setup, Foundational, and Polish tasks carry no story label
- Include exact file paths in descriptions

## Profiles under test

`app`, `headless-scene`, `governed`, `sample-pack` — every "for each profile" task means all four.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the no-regression baseline and a runnable template pack/install flow so the
real `dotnet new fs-gg-ui` host can be exercised.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Run **every** test project — the
> solution deliberately omits `tests/Package.Tests` (release-only public-surface gate, where this
> feature's tests live) and the `samples/**/*.Tests` projects. Use the discovery-based runner so
> nothing silently drops out.

- [X] T001 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/205-scaffold-git-init-chmod/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)
- [X] T002 [P] Confirm the local template pack/install flow from `specs/205-scaffold-git-init-chmod/quickstart.md` works: `dotnet pack .template.package/FS.GG.UI.Template.fsproj -o ~/.local/share/nuget-local/` then `dotnet new install FS.GG.UI.Template --nuget-source ~/.local/share/nuget-local/`; record the installed template id used by all later generation tasks

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the root-cause map and the one engine-behavior unknown against the real running
template host BEFORE any user-story edit.

**⚠️ CRITICAL**: No user story work begins until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** Treat the CI-hang / IDE-skip root cause and
> the R3 "instructions-only post-action" assumption as **unverified until the real `dotnet new`
> host has been driven**. Pull live evidence forward to this phase; do not defer it to the
> per-story checkpoints.

- [X] T003 Build the root-cause / change map every story depends on, in `specs/205-scaffold-git-init-chmod/readiness/rootcause.md`: enumerate the three auto-run post-actions and the `skipGitInit` symbol in `.template.config/template.json` (lines ~299–351), the `--skipGitInit true` argument threading in `scripts/validate-lifecycle-template.fsx`, and the 300 s wait/`proc.Kill` defensive loops in `scripts/validate-lifecycle-template.fsx` (lines ~222–230) and `scripts/validate-design-system-template.fsx`
- [X] T004 **Early live smoke run** (real `dotnet new` host, headless): for each profile, `dotnet new fs-gg-ui --name Demo --profile <p> -o <scratch>` with the CURRENT template and record the before-state behavior (auto-init reproduces a `.git` repo today) as evidence in `specs/205-scaffold-git-init-chmod/readiness/smoke-before.md`; this is the live observation that the US1 fix will be measured against (or mark `environment-limited` with a disclosed substitute if pack/install is unavailable)
- [X] T005 Resolve the R3 engine unknown live: instantiate a throwaway template variant (or probe the installed engine) to confirm a `manualInstructions`-only post-action with `continueOnError: true` and no recognized run processor prints cleanly on the `net10.0` template engine without being reported as a hard failure; record the verdict in `specs/205-scaffold-git-init-chmod/readiness/smoke-before.md`. If it errors, the always-on instructions post-action is dropped and manual steps are surfaced via generated README/docs + a one-line message instead (adjust T018 accordingly)
- [X] T006 Re-read `specs/205-scaffold-git-init-chmod/contracts/fs-gg-ui-template-generation.md` as the seam: confirm the `initGit` option shape (bool, default `false`), guarantees G1–G5, clauses C1–C5, §4 manual instructions, and S1–S3 scaffold obligations match what the implementation tasks below will build; note any divergence before editing `template.json`

**Checkpoint**: Root-cause map confirmed against a live run, engine unknown resolved, contract seam
agreed — user-story implementation can begin.

---

## Phase 3: User Story 1 - Generating a product never hangs or runs hidden scripts (Priority: P1) 🎯 MVP

**Goal**: Template generation is side-effect-free by default — no auto git-init, no chmod, no
spawned process, no defensive flag required — with the emitted file set unchanged.

**Independent Test**: Generate each profile headless with **no** Git flag; confirm prompt return,
no `.git`, no spawned process, and an emitted file set identical to the pre-feature fingerprint.

### Tests for User Story 1 (write FIRST, ensure they FAIL before implementation) ⚠️

- [X] T007 [P] [US1] Create `tests/Package.Tests/Feature205TemplateSideEffectTests.fs` with the US1 cases: default generation (no flag) creates **no** `.git` directory and spawns **no** process (G2/G3, SC-001), and the emitted tree fingerprint per profile equals the pre-feature baseline (G1, SC-005) using the existing `treeFingerprint` helper from the validation scripts
- [X] T008 [US1] Register `Feature205TemplateSideEffectTests` in `tests/Package.Tests/Program.fs` and run the suite to confirm the US1 cases FAIL against the current auto-init template

### Implementation for User Story 1

- [X] T009 [US1] In `.template.config/template.json`, delete the three auto-run post-actions (Unix init+chmod, Unix chmod-only, Windows init, ~lines 299–351) and remove the `skipGitInit` symbol from the symbols block — no other symbol or `sources` entry changes (FR-001/FR-002/FR-008)
- [X] T010 [US1] In `scripts/validate-lifecycle-template.fsx`, drop every `--skipGitInit true` argument and remove (or relax to a short sanity timeout) the 300 s `while … proc.Kill` stabilization loop at ~lines 222–230 now that generation exits promptly (FR-010, SC-002)
- [X] T011 [P] [US1] In `scripts/validate-design-system-template.fsx`, remove (or relax) the same wait/`proc.Kill` defensive loop and any post-action hang guard; generation is now side-effect-free (FR-010, SC-002)
- [X] T012 [P] [US1] Update `tests/Package.Tests/Feature204LifecycleTemplateTests.fs` and `tests/Package.Tests/GeneratedConsumerValidationTests.fs` to stop relying on the removed auto-init default (request init explicitly where they previously expected an auto-created repo); they MUST NOT pass `--skipGitInit` (FR-010)
- [X] T013 [US1] Re-pack/install the template and run `Feature205TemplateSideEffectTests` US1 cases + `dotnet fsi scripts/validate-lifecycle-template.fsx` to confirm default generation now creates no repo, spawns no process, and the fingerprint matches (SC-001/SC-002/SC-005)

**Checkpoint**: Default generation is side-effect-free on the real host; no `--skipGitInit` or
kill-loop remains in repo tooling — US1 independently verifiable.

---

## Phase 4: User Story 2 - The scaffold path owns initialization (Priority: P1)

**Goal**: The rendering-repo deliverable — make the template stop owning git-init/chmod and
**publish the contract** the SDD scaffold path fulfils, plus the cross-repo registry note recording
the behavioral break. (The scaffold-side execution itself is owned by the SDD repo — out of scope.)

**Independent Test**: Confirm `contracts/fs-gg-ui-template-generation.md` states the scaffold-path
obligations (S1–S3) and is no longer `Proposed`, and that the `fs-gg-ui-template` compatibility
registry records the auto-init removal so SDD-side consumers pin/adapt.

### Implementation for User Story 2

- [X] T014 [US2] Finalize `specs/205-scaffold-git-init-chmod/contracts/fs-gg-ui-template-generation.md`: confirm §5 (S1–S3 scaffold ownership), §6 migration notes, and the G/C clauses reflect the shipped `template.json`; flip **Status** from `Proposed` to `Accepted` once T009/T018 land
- [X] T015 [US2] Record the Tier-1 behavioral break (auto-init removed; `skipGitInit` gone; `initGit` added) in the `fs-gg-ui-template` compatibility/contract registry via the `cross-repo-coordination` skill so SDD scaffold-path consumers pin/adapt (R5); flag the scaffold-path execution as a cross-repo follow-up, not a code task in this repo

**Checkpoint**: The contract and registry publish the side-effect-free guarantee and the
scaffold-path division of responsibility — SDD repo can consume it (cross-repo verification of the
full scaffold end state, quickstart Scenario H, is tracked under the contract, not asserted here).

---

## Phase 5: User Story 3 - A standalone caller can opt in to initialization (Priority: P2)

**Goal**: A direct `dotnet new` caller reproduces the old convenience with a single explicit
`--initGit true` (repo + initial commit + executable scripts, existing-repo/git-absent safe), and
manual instructions are always surfaced.

**Independent Test**: Instantiate directly twice — `--initGit true` (confirm repo + initial commit +
executable scripts) and no flag (confirm no repo / no process) — and confirm manual instructions are
present in both the console and the generated tree.

### Tests for User Story 3 (write FIRST, ensure they FAIL before implementation) ⚠️

- [X] T016 [P] [US3] Extend `tests/Package.Tests/Feature205TemplateSideEffectTests.fs` with the opt-in cases: `--initGit true` not-inside-repo + git present ⇒ initialized repo, one `[Spec Kit] Initial commit`, executable generated scripts (C1, SC-004); `--initGit true` inside an existing repo ⇒ **no** nested `.git`, surrounding repo untouched (C2, SC-006); `--initGit true` with git absent ⇒ non-fatal skip message, chmod still applied, generation succeeds (C3, FR-006); **and `--initGit true` on a profile/lifecycle that emits no shell scripts (e.g. a non-`spec-kit` lifecycle) ⇒ chmod is a harmless no-op, generation succeeds, no error (C4, FR-007)**
- [X] T017 [US3] Run the suite to confirm the opt-in cases FAIL (the `initGit` symbol does not yet exist)

### Implementation for User Story 3

- [X] T018 [US3] In `.template.config/template.json`, add the `initGit` symbol (bool, default `false`, self-describing `--help` description per FR-012/data-model) and add the `initGit`-gated post-actions: a Unix action (`initGit && OS != Windows_NT`) running `chmod +x` of emitted scripts + git-presence check + `--is-inside-work-tree` guard + `git init/add/commit --allow-empty`, and a Windows action (`initGit && OS == Windows_NT`) running the PowerShell git-presence/guard/commit — reuse today's hardened argument strings verbatim and keep `continueOnError: true` (R2, C1–C5). **Cross-platform parity (FR-011): the two gated actions mirror each other's argument strings; the CI test suite runs on Linux only, so the Windows end-state is asserted by construction and is `environment-limited` per the constitution's evidence rules — disclose this in the test/PR rather than claiming a verified Windows run.**
- [X] T019 [US3] In `.template.config/template.json`, add the always-on `manualInstructions`-only post-action (no run processor, `continueOnError: true`) printing the chmod + git steps for every generation regardless of `initGit` (FR-009, §4) — **only if T005 confirmed the engine prints it cleanly**; otherwise surface via README/docs + a one-line message per T005's fallback
- [X] T020 [P] [US3] Update `.template.package/README.md` options table: remove `--skipGitInit`, add `--initGit` with the side-effect-free-default + scaffold-path-ownership explanation (FR-009/FR-012)
- [X] T021 [P] [US3] Update the generated-product manual instructions in `template/base/README.md` (and any `template/base/docs/` reference to git-init) so the durable guidance describes the chmod + git steps a caller performs by hand (FR-009, §4, US3-3)
- [X] T022 [US3] Re-pack/install and run the US3 opt-in cases + quickstart Scenarios C/E/F to confirm `--initGit true` reaches the initial repository state, never nests, and is git-absent-safe (SC-004/SC-006/FR-006); **also assert `dotnet new fs-gg-ui --help` lists the `initGit` option with its self-describing description (FR-012)**

**Checkpoint**: All three stories independently functional — safe default, published contract, and
working explicit opt-in with always-surfaced manual instructions.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: End-to-end validation, regression proof, and documentation finalization.

- [X] T023 Run the full quickstart suite (`specs/205-scaffold-git-init-chmod/quickstart.md` Scenarios A–G) and capture pass evidence in `specs/205-scaffold-git-init-chmod/readiness/`; confirm `grep -rn -- "--skipGitInit" scripts/ tests/` returns no matches (SC-002)
- [X] T024 Re-run the comprehensive baseline `dotnet fsi scripts/baseline-tests.fsx --out specs/205-scaffold-git-init-chmod/readiness/baseline-after.md` and diff against T001 to prove no new reds across solution + Package.Tests + samples
- [X] T025 [P] Confirm `CLAUDE.md` SPECKIT plan marker points at `specs/205-scaffold-git-init-chmod/plan.md` (already modified in working tree — verify, do not duplicate)
- [X] T026 [P] Capture per-phase Feature-205 feedback into `specs/205-scaffold-git-init-chmod/feedback/` via the `fs-gg-feedback-capture` skill (process friction, generalizable-code candidates, severity)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: depends on Setup (needs the installed template to smoke-test) — **BLOCKS all user stories**.
- **US1 (Phase 3)**: depends on Foundational. Delivers the MVP (side-effect-free default).
- **US2 (Phase 4)**: depends on Foundational; the contract finalize (T014) references the shipped `template.json`, so its `Accepted` flip waits on T009/T018, but the doc/registry work is otherwise independent of US1/US3.
- **US3 (Phase 5)**: depends on Foundational. Shares `.template.config/template.json` and `Feature205TemplateSideEffectTests.fs` with US1 — sequence the `template.json` edits (T009 before T018) and the test-file edits (T007 before T016) to avoid same-file conflicts.
- **Polish (Phase 6)**: depends on all desired stories being complete.

### User Story Dependencies

- **US1 (P1)**: independent — the core risk fix.
- **US2 (P1)**: largely independent (docs + registry); contract `Accepted` flip references US1/US3 output.
- **US3 (P2)**: independent in behavior; file-level ordering with US1 on `template.json` and the test file.

### Within Each User Story

- Tests written and FAILING before implementation (T007/T008 before T009; T016/T017 before T018).
- `template.json` symbol/post-action edits before re-pack/validate.
- Re-pack/install before any live generation assertion.

### Parallel Opportunities

- T002 ‖ T001 setup steps.
- US1: T011 (design-system script) ‖ T012 (test updates) — different files; both after T009/T010 land conceptually but touch independent files.
- US3: T020 (package README) ‖ T021 (template README/docs) — different files.
- Polish: T025 ‖ T026.
- US2 (T014/T015) can proceed alongside US1/US3 once Foundational is done, modulo the T014 `Accepted` flip.

---

## Parallel Example: User Story 3

```bash
# After the initGit symbol + gated post-actions land (T018), the doc updates run in parallel:
Task: "Update .template.package/README.md options table — drop --skipGitInit, add --initGit (T020)"
Task: "Update template/base/README.md manual chmod/git instructions (T021)"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → baseline + installable template.
2. Phase 2 Foundational → **early live smoke run** + R3 engine verdict + contract seam (CRITICAL, blocks stories).
3. Phase 3 US1 → side-effect-free default.
4. **STOP and VALIDATE**: default generation creates no repo / spawns no process / fingerprint unchanged; no `--skipGitInit` or kill-loop remains.
5. This is a shippable risk fix on its own.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. US1 → side-effect-free default (MVP — the CI-hang/IDE-skip risk is removed).
3. US2 → publish contract + registry note (unblocks the SDD scaffold path cross-repo).
4. US3 → explicit `--initGit` opt-in + manual instructions (no direct caller stranded).
5. Polish → full quickstart + regression baseline.

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks.
- US1 and US3 both edit `.template.config/template.json` and `Feature205TemplateSideEffectTests.fs` — these are intentionally sequenced, not parallel.
- Tests must FAIL before the implementation they cover (T008, T017).
- The scaffold-path execution (quickstart Scenario H, US2 acceptance) is cross-repo (SDD); this repo delivers the contract + registry note only.
- Commit after each task or logical group.

## Implementation notes (resolved decisions)

- **T019 — always-on instructions-only post-action: DROPPED per the T005 engine verdict.** The
  `net10.0` template engine rejects a post-action with no `actionId` (`CONFIG0202`), and a
  side-effect-free default (G2) forbids an `echo` process — so there is no clean always-on console
  mechanism. Per research R3's pre-registered fallback, manual chmod/git steps are surfaced via the
  generated product's `README.md` (`template/base/README.md`, durable), the package `README.md`
  ("Manual setup"), and the opt-in action's own `manualInstructions`. Contract §4 updated to match.
- **T007/T016 — Feature205 test is the repo's env-free verdict-core shape** (mirrors Feature204/128):
  it re-derives the side-effect-free + opt-in-shape guarantees structurally from `template.json`
  (GV-1..GV-6, deterministic / GL-free / CI-safe, genuinely failing-first — 6 RED on the pre-feature
  manifest, 6 GREEN after). The live `--initGit true` opt-in end-state (C1/C2/C3) is proven by real
  `dotnet new` runs recorded under `readiness/smoke-after.md` rather than as in-test live invocations,
  keeping `Package.Tests` host-independent (it must not require an installed template to stay green).
- **Headless opt-in flag:** a Run post-action triggers `dotnet new`'s allow-scripts prompt, so
  automated `--initGit true` runs must also pass `--allow-scripts yes` (contract §3 C6). The default
  path fires no post-action and needs no flag — which is what makes it CI-safe and lets the kill-loop
  be removed.

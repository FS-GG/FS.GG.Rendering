---
description: "Task list for feature 211 — Locked, reproducible dependency restore"
---

# Tasks: Locked, reproducible dependency restore (repo lockfiles + locked-mode CI)

**Input**: Design documents from `/specs/211-lockfile-locked-restore/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓ (`restore-policy.md`, `gate-restore.md`), quickstart.md ✓

**Tests**: One deterministic Expecto test is requested by the design (research R6 / Principle V) — `tests/Build.Tests/RestoreLockTests.fs`. It is the only test task; behavioral proof is the live restore (Phase 2) + quickstart perturbation.

**Organization**: Tasks are grouped by user story. This is a build/restore-config feature with **no F# public surface change** (Tier 2); the bulk of the mechanism is shared props + committed lockfiles established in the Foundational phase, with each user story layering one durable, independently-testable artifact on top.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in each description.

## Scope reference (from data-model.md / contracts/restore-policy.md)

**LOCKED set (38 projects = `FS.GG.Rendering.slnx` membership)** — each gets a committed `packages.lock.json`:
`src/**` (18) · `tests/**` (17, every test project in the slnx, incl. `Build.Tests`) · `tools/Rendering.Harness` (1) · `samples/CanvasDemo` + `samples/SymbologyBoard` (2).

**EXCLUDED set** — never locked:
`samples/{AntShowcase,SampleApps,SecondAntShowcase,ControlsGallery}` (shadow root `Directory.Build.props`; not in slnx — zero edits) · `tests/Package.Tests` (explicit one-line `.fsproj` opt-out; not in slnx) · template-instantiated products (out of tree; Feature 204 owns them).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the no-regression baseline before touching any restore config.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Run **every** test project — solution + `Package.Tests` + samples — so pre-existing reds are known now and not mistaken for regressions at merge. Use the discovery-based runner; it globs `*.Tests.fsproj` so nothing silently drops out.

- [X] T001 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/211-lockfile-locked-restore/readiness/baseline.md` (runs EVERY test project — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)
- [X] T002 [P] Capture the pre-change locked-set inventory for later comparison: `git ls-files '*packages.lock.json'` (expected EMPTY today) into `specs/211-lockfile-locked-restore/readiness/lockfiles-before.txt`, and record `dotnet --info` SDK version alongside it (confirms 10.0.x prereq from quickstart)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Stand up the shared restore policy (the props), fence the scope, generate the lockfiles, and **prove the behavior against real restore** — before any user story wires the durable gate/test/docs artifacts.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

> **⚠️ Empirical restore proof (STANDING — the running-app substitute for THIS feature).** This feature has no running app, so the constitution's "drive the real app before trusting hypotheses" discipline is honored, per the plan (plan.md §Standing assumption), as a **live restore proof scheduled here in Foundational, BEFORE the gate/test/docs work**. The two empirical questions are R3 (does `WarningsAsErrors;NU1603` actually fail restore, or does the restore phase ignore it?) and R4 (does locked mode engage as expected?). Treat the props as **unverified until restore has actually been run and observed** — a clean locked restore succeeds, a perturbed graph fails, an un-pinnable version fails with NU1603. Do not defer this to the per-story checkpoints.

- [X] T003 Add the restore policy to the repo-root `Directory.Build.props`: `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>`, the gated `<RestoreLockedMode Condition="'$(ContinuousIntegrationBuild)' == 'true' And Exists('$(MSBuildProjectDirectory)/packages.lock.json')">true</RestoreLockedMode>`, and append `<WarningsAsErrors>$(WarningsAsErrors);NU1603;NU1608</WarningsAsErrors>` — verbatim from `template/base/Directory.Build.props` (FR-007), with the FR-001/002/003/004 comments from `contracts/restore-policy.md`
- [X] T004 Fence the scope: add the one-project opt-out to `tests/Package.Tests/Package.Tests.fsproj` body (after the implicit props import) — `<RestorePackagesWithLockFile>false</RestorePackagesWithLockFile>` + defensive `<RestoreLockedMode>false</RestoreLockedMode>` with the FR-006 comment (data-model.md §2). Confirm-by-inspection that the 4 standalone samples already shadow root (their `Directory.Build.props` carries the "SHADOWS the repository-root" comment) so they need zero edits
- [X] T005 Generate and commit the lockfiles for the LOCKED set: `dotnet restore FS.GG.Rendering.slnx --force-evaluate`. Verify exactly the 38 slnx projects produced a `packages.lock.json` (`git ls-files '*packages.lock.json' | wc -l` → 38) and that **none** appears under `samples/{AntShowcase,SampleApps,SecondAntShowcase,ControlsGallery}` or `tests/Package.Tests`; diff against `readiness/lockfiles-before.txt`. Commit the 38 lockfiles
- [X] T006 **Empirical restore proof (R3/R4) — run real restore, do NOT assume.** Record results to `specs/211-lockfile-locked-restore/readiness/restore-proof.md`:
  (a) **clean locked** — `ContinuousIntegrationBuild=true dotnet restore FS.GG.Rendering.slnx --locked-mode` → MUST succeed (quickstart A / SC-001);
  (b) **drift fail-closed** — perturb one `Directory.Packages.props` version without regenerating, re-run (a) → MUST fail with a locked-mode mismatch, then revert (quickstart B / SC-002);
  (c) **NU1603 fail** — point a centrally-managed version at one no feed provides exactly, `dotnet restore FS.GG.Rendering.slnx --force-evaluate` → MUST fail with NU1603-as-error; if it only **warns**, the props did not promote the restore-phase warning → adopt the R3 fallback `-warnaserror:NU1603;NU1608` on the explicit restore step and re-run to confirm; then revert (quickstart C / SC-003). Note in the proof which mechanism (props alone vs. `-warnaserror`) was needed — US2 (T010) depends on this finding

**Checkpoint**: Policy props in place, scope fenced, 38 lockfiles committed, and locked/NU1603 behavior **proven by real restore** (R3/R4 resolved, fallback decision recorded). User stories can now wire the durable artifacts.

---

## Phase 3: User Story 1 - Locked, reproducible CI restore (Priority: P1) 🎯 MVP

**Goal**: The gate restores the slnx in locked mode against committed lockfiles; a graph that differs from the lockfile fails restore and blocks the merge. Reproducibility guarantee is independently shippable on this story alone.

**Independent Test**: On a clean checkout, `ContinuousIntegrationBuild=true dotnet restore FS.GG.Rendering.slnx --locked-mode` succeeds against the committed lockfiles; perturbing the resolved graph makes it fail instead of silently substituting (quickstart A + B).

### Implementation for User Story 1

- [X] T007 [US1] Add the named `Restore (locked)` step to `.github/workflows/gate.yml` **before** the existing "Build solution (net10.0)" step — `dotnet restore FS.GG.Rendering.slnx --locked-mode` wrapped with `set -euo pipefail` and the `::error::` annotation pointing at the regenerate command (verbatim from `contracts/gate-restore.md`); add `--no-restore` to the existing build step so restore happens exactly once (FR-002, GR1/GR3)
- [X] T008 [US1] Add the deterministic coverage/scope test `tests/Build.Tests/RestoreLockTests.fs` (research R6): assert (1) every `FS.GG.Rendering.slnx` member has a committed `packages.lock.json`, (2) the excluded lanes (`Package.Tests` + the 4 shadowing samples) do **not**, and (3) the root `Directory.Build.props` contains `RestorePackagesWithLockFile`, the gated `RestoreLockedMode`, and `NU1603` in `WarningsAsErrors` (VR-1/VR-2; the props assertion also backstops US2)
- [X] T009 [US1] Register the new file in `tests/Build.Tests/Build.Tests.fsproj` (add `<Compile Include="RestoreLockTests.fs" />` in the correct compile order) and run `dotnet test tests/Build.Tests/Build.Tests.fsproj -c Debug` → green

**Checkpoint**: Locked CI restore is wired and guarded by a committed test. US1 is independently functional and shippable as the MVP.

---

## Phase 4: User Story 2 - Silent version drift becomes a hard error (Priority: P2)

**Goal**: A request that NuGet can only satisfy by substituting a higher version (NU1603) fails the gate instead of warning-and-continuing.

**Independent Test**: Introduce a centrally-managed version no feed provides exactly so a higher one is substituted; the gate fails with NU1603 treated as an error (quickstart C).

### Implementation for User Story 2

- [X] T010 [US2] Apply the R3 finding from T006 to the durable enforcement point: the `WarningsAsErrors;NU1603;NU1608` props entry (added in T003) is the contract, but **if** the proof showed props alone do not promote the restore-phase warning, add `-warnaserror:NU1603;NU1608` to the `dotnet restore` invocation in the `Restore (locked)` step of `.github/workflows/gate.yml` (GR2, `contracts/gate-restore.md`). Confirm the gate is the single enforcement point (no other restore step needs it)
- [X] T011 [US2] Re-run the NU1603 perturbation against the final wiring and append the confirmation (substitution → failed restore, not a warning) to `specs/211-lockfile-locked-restore/readiness/restore-proof.md` (SC-003 closed against the durable artifact, not just the T006 spike)

**Checkpoint**: US1 + US2 both hold — locked drift fails AND un-pinnable substitution fails, at one enforcement point.

---

## Phase 5: User Story 3 - Frictionless local & intentional-update flow (Priority: P3)

**Goal**: A fresh clone builds locally without being blocked by locked mode; an intentional dependency bump is re-locked by a single documented command producing a reviewable diff.

**Independent Test**: Fresh clone `dotnet build FS.GG.Rendering.slnx -c Debug` is not blocked (no `ContinuousIntegrationBuild`); bumping a version + `dotnet restore FS.GG.Rendering.slnx --force-evaluate` shows the lockfile diff (quickstart D + E).

### Implementation for User Story 3

- [X] T012 [P] [US3] Document the single regenerate command (FR-008) — add a short "Updating dependency lockfiles" note giving `dotnet restore FS.GG.Rendering.slnx --force-evaluate` (re-resolve from `Directory.Packages.props` → rewrite all lockfiles → commit props + lockfiles together) to the repo `CONTRIBUTING.md` (or the docs note named in plan.md §Source Code); cross-link `specs/211-lockfile-locked-restore/quickstart.md` Scenario E. In the same note, document the **local pre-push verify** command for the stale-lockfile edge case (spec.md §Edge Cases) — `ContinuousIntegrationBuild=true dotnet restore FS.GG.Rendering.slnx --locked-mode` reproduces the gate's locked-mode failure locally so drift is catchable before push; cross-link quickstart Scenario A/B
- [X] T013 [US3] Verify the local/bootstrap guarantees by real run and record to `readiness/restore-proof.md`: (a) `dotnet build FS.GG.Rendering.slnx -c Debug` with `ContinuousIntegrationBuild` **unset** is not blocked by locked mode (SC-004 / VR-4); (b) `dotnet restore FS.GG.Rendering.slnx --force-evaluate` then `git status --short '*packages.lock.json' Directory.Packages.props` shows a reviewable diff (SC-005), reverting any throwaway version bump afterward

**Checkpoint**: All three user stories independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Prove no regression to the existing gate/release/sample lanes and close the quickstart.

- [X] T014 No-regression sweep (FR-009 / SC-006 / VR-5): `dotnet build FS.GG.Rendering.slnx -c Debug`, `dotnet fsi scripts/refresh-surface-baselines.fsx` (surface drift unchanged), `dotnet fsi scripts/validate-version-coherence.fsx` (coherence guard unchanged); confirm the samples/template/release pack lanes are untouched (they never inherited the policy). Re-run T001's baseline runner and diff the red/green set against `readiness/baseline.md` — must be identical
- [X] T015 [P] Run the full quickstart (`specs/211-lockfile-locked-restore/quickstart.md` Scenarios A–G) end-to-end and confirm each Expected outcome — A (locked restore succeeds), B (drift fail-closed), C (NU1603-as-error), D (fresh-clone local not blocked), E (single regenerate command), **F (scope boundary holds — FR-006/SC-006)**, **G (no regression to gate/release lanes — FR-009/SC-006)**; this is the consolidated acceptance evidence
- [X] T016 [P] Capture per-phase feedback via the `fs-gg-feedback-capture` skill into `specs/211-lockfile-locked-restore/feedback/` (process friction, generalizable-code candidates, severity)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. **BLOCKS all user stories.** T003 → T004 → T005 → T006 are strictly sequential (props before opt-out before generation before proof).
- **User Stories (Phase 3–5)**: All depend on Foundational completion. US1 (P1) is the MVP. US2 depends on the **R3 finding** recorded in T006 (which fallback to use). US3 is independent of US1/US2 wiring.
- **Polish (Phase 6)**: Depends on all targeted user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Needs Foundational only. Independently testable and shippable (MVP).
- **US2 (P2)**: Needs Foundational (specifically the T006 NU1603 finding). Shares the gate `Restore (locked)` step file with US1 T007 — so T010 must run after T007 (same file).
- **US3 (P3)**: Needs Foundational only. Touches a different file (`CONTRIBUTING.md`) — fully independent.

### Within Each User Story

- US1: gate step (T007) and the test file (T008) are different files; T009 (register) depends on T008. T008/T009 can proceed alongside T007.
- US2: T010 edits the same gate file as T007 → sequential after T007; T011 depends on T010.
- US3: T012 (docs) and T013 (verify) are independent of each other.

### Parallel Opportunities

- T001 and T002 (Setup) — T002 is [P] (different output files).
- Within US1: T008 (new test file) is independent of T007 (gate.yml) — can run in parallel; T009 follows T008.
- US3 T012 (docs) is [P] and can be written any time after Foundational, in parallel with US1/US2.
- Polish T015 / T016 are [P] (quickstart run vs. feedback capture).
- **Cross-story**: once Foundational is done, US3 (docs lane) can proceed fully in parallel with US1+US2 (gate/test lane) — different files.

---

## Parallel Example: after Foundational completes

```bash
# US1 gate wiring and US1 test authoring touch different files — run together:
Task: "Add Restore (locked) step + --no-restore to .github/workflows/gate.yml"   # T007
Task: "Author tests/Build.Tests/RestoreLockTests.fs coverage/scope assertions"   # T008

# US3 docs lane is independent of the entire US1/US2 gate lane — run in parallel:
Task: "Document dotnet restore --force-evaluate regenerate command in CONTRIBUTING.md"  # T012
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → baseline recorded.
2. Phase 2 Foundational (CRITICAL) — props + opt-out + 38 committed lockfiles + **empirical restore proof** (locked pass / drift fail / NU1603 fail) before any durable wiring.
3. Phase 3 US1 — gate `Restore (locked)` step + coverage test.
4. **STOP & VALIDATE**: clean locked restore passes, perturbation fails (quickstart A+B).
5. Shippable: lockfiles committed + locked-mode CI delivers reproducibility on its own.

### Incremental Delivery

1. Foundational → foundation ready (lockfiles + proven behavior).
2. US1 → locked CI restore + coverage test → MVP.
3. US2 → NU1603-as-error enforced at the gate.
4. US3 → frictionless local + documented regenerate.
5. Polish → no-regression sweep + full quickstart.

---

## Notes

- This is a Tier 2 build/restore-config feature: **no `.fs`/`.fsi` edits, no new shipped dependency**. The only "code" is one Expecto policy test (T008).
- The 4 standalone samples are excluded **with zero edits** (they shadow root) — any change that makes them inherit root breaks G6 and MUST be rejected.
- The empirical restore proof (T006) is non-negotiable: NuGet's promotion of restore-phase NU16xx under `TreatWarningsAsErrors` is the specific thing not to take on faith (research R3) — the R3 finding directly selects US2's enforcement mechanism.
- Commit `Directory.Packages.props` + regenerated lockfiles together; an un-updated lockfile is meant to fail CI (FR-005).
- [P] = different files, no dependency. [Story] label maps each task to its user story for traceability.

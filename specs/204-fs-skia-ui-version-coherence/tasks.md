---
description: "Task list for Restore fs-skia-ui-version Cross-Repo Coherence"
---

# Tasks: Restore fs-skia-ui-version Cross-Repo Coherence

**Input**: Design documents from `/specs/204-fs-skia-ui-version-coherence/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅ (coherence-verification, snapshot-manifest, cross-repo-resolution), quickstart.md ✅

**Tests**: No separate TDD test tasks are requested. The coherence signal is the template's **existing** checks — `Product.Tests/GovernanceTests` + the per-profile generate→restore→build→evidence run — re-run as verification (Constitution V; FR-008). No new public F# surface is designed, so there are no `.fsi`/semantic-test tasks.

**Organization**: Tasks are grouped by user story (US1 verify → US2 snapshot → US3 reconcile). The order is **mandatory and gated**: US3 MUST NOT begin until US1 **and** US2 pass (FR-007).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1, US2, US3 (Setup / Foundational / Polish carry no story label)

## Path Conventions

In-repo edits are confined to `template/base/` (pin bump, phantom-pin removal, committed lockfile) plus this feature's `contracts/` (snapshot manifest) and `readiness/` (evidence). `src/**` and the packing/scaffolding tooling are read-only/as-is. Cross-repo state lives in the sibling repos `FS-GG/.github` and `FS-GG/FS.GG.Rendering` and is mutated through `gh`, not files in this repo.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the no-regression baseline and confirm the tooling the verification depends on.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline MUST run **every** test project (solution + `tests/Package.Tests` + `samples/**/*.Tests`) via the discovery-based runner so pre-existing reds are known up front and not mistaken for regressions at merge.

- [X] T001 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/204-fs-skia-ui-version-coherence/readiness/baseline.md` (globs every `*.Tests.fsproj` — solution + `tests/Package.Tests` + `samples/**`; record the full red/green set so pre-existing reds are flagged here, not at merge)
- [X] T002 [P] Confirm scaffolding/packing tooling is available and the four profiles are the expected set: verify `dotnet new fs-gg-ui` is installed (or installable from `.template.config/template.json`) and that `--profile` lists exactly `app  headless-scene  governed  sample-pack`; confirm the local feed dir `~/.local/share/nuget-local/` exists
- [X] T003 [P] Confirm cross-repo write path (for US3 only, no writes yet): `gh auth status` shows account `EHotwagner`, `gh api repos/FS-GG/.github` resolves, and `gh issue view 1 --repo FS-GG/FS.GG.Rendering` shows the request OPEN (record current state in `specs/204-fs-skia-ui-version-coherence/readiness/baseline.md`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Fix the phantom pins, pack + re-pin to the resolution-commit version, and prove the hypothesis against **one real generated profile** before committing to the full four-profile sweep.

**⚠️ CRITICAL**: No user-story work (US1/US2/US3) can begin until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** `template/base` is not directly compilable (seed `.fs` carry both `//#if` profile branches). A green Feature-201 against an *older* feed is **not** evidence that the *current* pinned set restores and builds. Treat the plan's coherence hypotheses as **unverified until a product is generated, restored, and built**. T007 pulls one real generate→restore→build forward, BEFORE the full US1 sweep, exactly as Feature 175's lesson requires.

- [X] T004 Remove the two phantom `<PackageVersion>` pins (and their now-false explanatory comments) — `FS.GG.UI.Color` (retired in Feature 179; `src/ColorPolicy` is `IsPackable=false`) and `FS.GG.UI.SkillSupport` (no producing project) — from `template/base/Directory.Packages.props`, so the template pins only packages that actually ship (CV-5; research R2)
- [X] T005 Pack the framework at the resolution commit: `dotnet fsi scripts/refresh-local-feed-and-samples.fsx` (packs all 16 real `FS.GG.UI.*` IDs to `~/.local/share/nuget-local/`); record the version the packer assigned (expected `0.1.51-preview.1`) in `specs/204-fs-skia-ui-version-coherence/readiness/baseline.md`. Do NOT hard-code an integer — the packer fixes the exact value (research R3)
- [X] T006 Re-pin `<FsSkiaUiVersion>` in `template/base/Directory.Packages.props` to exactly the version recorded in T005, keeping `$(FsSkiaUiVersion)` as the single FS.GG.UI version literal (every pin references the property — FR-004, SC-003); confirm all 16 real IDs are present in the feed at that version (SM-A)
- [X] T007 **Early live smoke run**: `dotnet new fs-gg-ui --profile headless-scene -o /tmp/fsgg-smoke` → `dotnet restore` → `dotnet build` against the freshly packed feed; confirm no NU1101 / no version conflict / no Scene-API compile error on this one real profile before the full sweep. Record the result in `specs/204-fs-skia-ui-version-coherence/readiness/baseline.md` (validates the hypothesis live; partial — full gate is US1)
- [X] T008 [P] Confirm `template/base/docs/UPGRADING.md`'s `0.1.68-preview.1` is an illustrative example, not a governed literal (it sits in a sample `<...>` block); confirm `GovernanceTests` scopes the single-source invariant to `build.fsx` + `Directory.Packages.props`, not doc prose. If it is flagged, replace the doc example with a placeholder (research R6; FR-008)

**Checkpoint**: Phantom pins gone, feed packed, pin set to the resolution version, and one real profile generates+restores+builds clean — the full US1 sweep can now begin.

---

## Phase 3: User Story 1 — A downstream consumer scaffolds and builds a working product (Priority: P1) 🎯 MVP

**Goal**: Every supported profile, scaffolded from the template, restores against one consistent pinned set and builds with no Scene-API drift, emitting its expected scene/evidence output.

**Independent Test**: Scaffold each profile, `dotnet restore` (no NU1101 / no version conflict), `dotnet build` (no Scene-API compile error), and run the profile's evidence/governance — all four green under the single pin.

**Gate**: ALL four profiles must pass (CV-1..CV-5). Any red ⇒ stop; the contract stays incoherent and US2/US3 do NOT proceed (edge case "a profile builds but another does not"; FR-007).

- [X] T009 [US1] Verify profile **headless-scene**: `dotnet new fs-gg-ui --profile headless-scene -o /tmp/fsgg-headless-scene` → restore → build → run scene/layout evidence CLI (`--scene-evidence` / `--layout-evidence`) → `dotnet test` (Product.Tests/GovernanceTests); capture the restore+build+evidence transcript and the resolved package list to `specs/204-fs-skia-ui-version-coherence/readiness/profile-headless-scene.md` (CV-1..CV-5)
- [X] T010 [US1] Verify profile **governed**: `dotnet new fs-gg-ui --profile governed -o /tmp/fsgg-governed` → restore → build → evidence → `dotnet test` (GovernanceTests + `FS.GG.UI.Testing` assertions); capture transcript + resolved package list to `specs/204-fs-skia-ui-version-coherence/readiness/profile-governed.md` (CV-1..CV-5)
- [X] T011 [US1] Verify profile **app**: `dotnet new fs-gg-ui --profile app -o /tmp/fsgg-app` → restore → build → app-profile launch/screenshot evidence (live; if not live, `environment-limited` per the Feature-168 evidence rules — the readiness file MUST name the substitute and the reason, per Constitution V) → governance; capture transcript + resolved package list to `specs/204-fs-skia-ui-version-coherence/readiness/profile-app.md` (CV-1..CV-5)
- [X] T012 [US1] Verify profile **sample-pack**: `dotnet new fs-gg-ui --profile sample-pack -o /tmp/fsgg-sample-pack` → restore → build → launch/screenshot evidence (live; if not live, `environment-limited` per the Feature-168 evidence rules — the readiness file MUST name the substitute and the reason, per Constitution V) → governance; capture transcript + resolved package list to `specs/204-fs-skia-ui-version-coherence/readiness/profile-sample-pack.md` (CV-1..CV-5)
- [X] T013 [US1] Consolidate the per-profile evidence into `specs/204-fs-skia-ui-version-coherence/readiness/coherence-evidence.md`: assert all four profiles are restore-ok ∧ build-ok ∧ evidence-ok under the single pin, exactly one FS.GG.UI version literal (not `0.1.0-preview.1`), and no phantom IDs in any resolved set (SC-001, SC-003; the US3 `## Response` will link this)

**Checkpoint**: All four profiles green under the single pin — US1 holds. This is the MVP and the evidence US2 records and US3 reports. If any profile is red, STOP here.

---

## Phase 4: User Story 2 — A reproducible, pin-able release snapshot exists (Priority: P2)

**Goal**: The pinned `FsSkiaUiVersion` refers to an immutable, reproducible snapshot (git tag + committed lockfile + recorded manifest), so two restores resolve byte-for-byte the same set.

**Independent Test**: Restore the pinned template from a clean cache twice → identical resolved `FS.GG.UI.*` set, matching the recorded manifest; re-checkout of the tag reproduces the source.

**Prerequisite**: US1 (Phase 3) green.

- [X] T014 [US2] Enable locked restore for the template in the template-level `template/base/Directory.Build.props` (the single host applying to every generated project): add `RestorePackagesWithLockFile=true` (and `RestoreLockedMode=true` gated to CI/verify), then restore once to generate the per-project `template/base/**/packages.lock.json` and commit it (SM-3)
- [X] T015 [US2] Prove reproducibility (SM-B / SC-002): clear the NuGet cache (or restore into a clean global-packages dir) and restore the pinned template **twice**; confirm the resolved `FS.GG.UI.*` set is identical across both restores and matches the committed lockfile; record the result in `specs/204-fs-skia-ui-version-coherence/readiness/reproducibility.md`
- [X] T016 [US2] Fill `specs/204-fs-skia-ui-version-coherence/contracts/snapshot-manifest.md`: replace every `<version>` placeholder in the 16-row table with the verified version from T005/T006; confirm `pinned-version == every-manifest-row version` and that `FS.GG.UI.Color`/`FS.GG.UI.SkillSupport` are absent by design (SM-2, SM-A, SM-C)
- [X] T017 [US2] Cut the immutable source tag at the resolution commit: `git tag -a fs-skia-ui/v<version> -m "coherent FS.GG.UI.* snapshot for fs-gg-ui template pin"` then `git push origin fs-skia-ui/v<version>`; confirm `pinned-version == tag version` and that re-checkout of the tag reproduces the packed set (SM-1, SM-C, SM-D)

**Checkpoint**: Pin == tag == manifest == every feed-package version, and restores are byte-reproducible — US2 holds. Both US1 and US2 preconditions for the cross-repo flip are now satisfied.

---

## Phase 5: User Story 3 — The cross-repo record reflects coherence and the request is resolved (Priority: P3)

**Goal**: Flip the `fs-skia-ui-version` registry row to `coherent: true` (both files, together) and close request #1 with a `## Response`, consistent with the verified evidence.

**Independent Test**: Read the `fs-skia-ui-version` row + issue #1 — row is coherent, issue has a `## Response` and is closed, and both agree with the US1/US2 evidence.

> **⚠️ HARD GATE (FR-007 — no premature closure).** Do NOT start any task in this phase unless US1 (T013: all four profiles green) **and** US2 (T015 reproducible, T017 tagged) hold. If either fails, STOP: leave `coherent: false`, leave #1 OPEN, and record the blocker.

- [X] T018 [US3] Verify the gate before any write: confirm T013 (four profiles green) and T015/T017 (reproducible + tagged) are complete; if not, halt this phase and record the blocker in `specs/204-fs-skia-ui-version-coherence/readiness/coherence-evidence.md` (XR-D, FR-007)
- [X] T019 [US3] Clone/read sibling `FS-GG/.github` and confirm the exact YAML key/shape of the `fs-skia-ui-version` row in `registry/dependencies.yml` before editing (research R5 open item); note the structure so the `docs/registry/compatibility.md` projection can be edited consistently
- [X] T020 [US3] In `FS-GG/.github`, update the `fs-skia-ui-version` row to `coherent: true` in **both** `registry/dependencies.yml` (authoritative) and `docs/registry/compatibility.md` (projection) **together**, referencing the resolving change (commit/tag `fs-skia-ui/v<version>`/PR); open the change per the cross-repo coordination protocol (`cross-repo-coordination` skill) — XR-A, XR-B
- [X] T021 [US3] Post the resolution comment and close the request: `gh issue comment 1 --repo FS-GG/FS.GG.Rendering --body "## Response …"` naming the option taken (git tag + committed lockfile), the pinned `FsSkiaUiVersion=<version>`, the tag `fs-skia-ui/v<version>`, the four-profile green result, the phantom-pin removal, and linking the T013 evidence; then `gh issue close 1 --repo FS-GG/FS.GG.Rendering` (XR-C)
- [X] T022 [US3] Confirm no stale signal remains (XR-E, SC-005): re-read the registry row and issue #1 — row `coherent: true`, projection agrees, issue CLOSED with `## Response`, `blocked` label cleared; record the final cross-repo state in `specs/204-fs-skia-ui-version-coherence/readiness/cross-repo-resolution.md`

**Checkpoint**: Registry coherent + projection agrees + issue closed with `## Response`, all consistent with verified evidence — US3 holds and the coherence loop is closed.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: FR-008 stale-reference sweep and final whole-feature validation.

- [X] T023 [P] FR-008 stale-reference sweep: grep the template (`template/base/**` — pins, seed `.fs`, `docs/`) and this feature's docs for `0.1.0-preview.1` and for any second FS.GG.UI version literal; confirm exactly one literal (the `$(FsSkiaUiVersion)` value) remains (SC-003)
- [X] T024 Re-run `Product.Tests/GovernanceTests` (single-source `FsSkiaUiVersion` invariant) on a freshly generated product and confirm green under the pinned version (FR-008)
- [X] T025 Re-run the comprehensive baseline (`dotnet fsi scripts/baseline-tests.fsx`) and diff against T001 to confirm no regression was introduced (only intended changes); update `specs/204-fs-skia-ui-version-coherence/readiness/baseline.md`
- [X] T026 Run the `quickstart.md` end-to-end once (Step 0 → Step 4) as the final acceptance pass; confirm every Success Criterion (SC-001..SC-005) maps to captured evidence

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. **BLOCKS all user stories.** T004 → T005 → T006 → T007 are sequential (each consumes the prior's output); T008 is [P].
- **US1 (Phase 3)**: Depends on Foundational. The four profile-verification tasks (T009–T012) are independent of each other; T013 consolidates them.
- **US2 (Phase 4)**: Depends on **US1 green** (T013). Within: T014 → T015; T016 [P after T006]; T017 after T016.
- **US3 (Phase 5)**: **HARD-gated on US1 (T013) AND US2 (T015, T017).** Sequential: T018 (gate) → T019 → T020 → T021 → T022.
- **Polish (Phase 6)**: Depends on the user stories being complete.

### User Story Dependencies (the gated coherence loop)

- **US1 (P1)**: Independent — the verification gate. Produces the evidence everything else needs.
- **US2 (P2)**: Records the snapshot of the set US1 verified — needs US1 green.
- **US3 (P3)**: Reports + reconciles — MUST NOT begin before US1 **and** US2 hold (FR-007). This is a one-directional, gated transition (data-model invariant).

### Parallel Opportunities

- Setup: T002, T003 in parallel (after/with T001).
- Foundational: T008 in parallel with the T004→T007 chain.
- **US1: T009, T010, T011, T012 can run in parallel** (four independent generated dirs / readiness files) — the largest parallel block; T013 waits for all four.
- Polish: T023 in parallel.

---

## Parallel Example: User Story 1 (the four-profile sweep)

```bash
# After Foundational completes, launch all four profile verifications together:
Task: "Verify profile headless-scene → readiness/profile-headless-scene.md"   # T009
Task: "Verify profile governed → readiness/profile-governed.md"               # T010
Task: "Verify profile app → readiness/profile-app.md"                         # T011
Task: "Verify profile sample-pack → readiness/profile-sample-pack.md"         # T012
# Then T013 consolidates — the gate requires ALL FOUR green.
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1: Setup (baseline + tooling confirmation).
2. Complete Phase 2: Foundational (phantom-pin removal, pack + re-pin, **early live smoke run** on one profile).
3. Complete Phase 3: US1 — all four profiles generate→restore→build→evidence green.
4. **STOP and VALIDATE**: this is the coherence proof. If any profile is red, the contract stays incoherent — do not proceed to US2/US3.

### Incremental Delivery (the mandatory order)

1. Setup + Foundational → feed packed, pin set, hypothesis validated live.
2. US1 → four-profile coherence proven (MVP).
3. US2 → snapshot made immutable + reproducible (lockfile + manifest + tag).
4. US3 → cross-repo record reconciled + request closed (**only after US1+US2**).
5. Polish → stale-reference sweep + whole-feature acceptance.

### Fail-loud-and-closed

A partial/missing snapshot or any red profile keeps `coherent: false` and #1 OPEN with the blocker recorded (FR-007; spec edge cases). The registry/issue flip is the **last** thing that happens and only on full, real evidence.

---

## Notes

- [P] = different files, no dependencies on incomplete tasks.
- [Story] label maps each task to US1/US2/US3 for traceability; Setup/Foundational/Polish carry none.
- No version integer is hard-coded — the packer (T005) fixes the exact value; all later tasks reference "the version recorded in T005/T006."
- `src/**` and the packing/scaffolding tooling are read-only/as-is; in-repo edits are confined to `template/base/` + this feature's `contracts/`/`readiness/`.
- Cross-repo writes (US3) happen in `FS-GG/.github` + `FS-GG/FS.GG.Rendering` via `gh`, never as files in this repo.
- Commit after each task or logical group; the git tag (T017) is itself a deliverable.

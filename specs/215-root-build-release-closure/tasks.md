---
description: "Task list for feature 215-root-build-release-closure"
---

# Tasks: Finalize the root-buildable template guarantee (release the coherent set + close #9)

**Input**: Design documents from `/specs/215-root-build-release-closure/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: No standalone test tasks are generated. This is a release/closure slice that edits no
`.fs`/`.fsi`; the load-bearing test evidence is the **release-only `template-product-tests` +
`package-tests` gates running on the real release** (owned by US1, see `contracts/release-gate.md`),
plus the Feature 209 coherence guard. These are exercised as implementation tasks, not authored as new tests.

**Organization**: Tasks are grouped by user story. Hard ordering applies across stories (coherence →
release → registry → closure); see Dependencies.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files/surfaces, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (Setup, Foundational, Polish have no story label)
- Coherent-set version for this release: **`0.1.52-preview.1`** (research R1)

> **⚠️ HARD ORDERING (do not reorder).** The coherent-set bump + framework tag (Foundational) precede the
> real release (US1); the registry PR #25 lands **with or after** the release, **never before** (FR-006);
> #9 closes only once release is Published ∧ coherent ∧ registry Merged (FR-008, data-model invariant 5).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the planning-snapshot starting state and record a no-regression baseline before any release action.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Even though this feature ships no library code,
> the real release packs the whole `FS.GG.Rendering.slnx` **and** runs the release-only `Package.Tests`
> gate. Record the full red/green set up front (solution + `Package.Tests` + samples) so a pre-existing red
> is not mistaken for a release-introduced regression at the gate. Use the discovery-based runner so nothing
> silently drops out.

- [X] T001 Confirm starting state (quickstart Step 0): verify `template/base/Directory.Packages.props` `FsGgUiVersion` = `0.1.51-preview.1`, `.template.package/FS.GG.UI.Template.fsproj` `<Version>` = `0.1.52-preview.1`, latest `git tag --list 'fs-gg-ui/v*'` = `fs-gg-ui/v0.1.51-preview.1`, latest `fs-gg-ui-template/v*` = `fs-gg-ui-template/v0.1.50-preview.1`, `gh issue view 9 --repo FS-GG/FS.GG.Rendering` = OPEN, `gh pr view 25 --repo FS-GG/.github --json state,mergeable` = OPEN/CONFLICTING; record results in `specs/215-root-build-release-closure/readiness/starting-state.md`
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/215-root-build-release-closure/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + `tests/Package.Tests` + `samples/**` — and records the full red/green set; pre-existing reds are flagged here, not discovered at the release gate)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Make the in-repo coherent-set edit, prove it locally, and stage the snapshot tags + coherence
verdict that ALL user stories depend on.

**⚠️ CRITICAL**: No user story (release, registry, closure) can begin until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** Before triggering the real release (US1), drive and
> observe the **real released-shape template** locally: pack → `dotnet new install` → scaffold → stock
> `build`/`test`/`run` (headless). The plan's central assumption — that the template-under-release is
> root-buildable with the stock CLI exactly as `main` is — is **unverified until the scaffolded product is
> actually built and run**. This is the live-evidence pull-forward (research R5); it is **confidence only**,
> NOT #9 evidence (which must come from the real-release gate, T010–T012).

- [X] T003 Build the order-of-operations / coherence map every story depends on: record the version trio (`FsGgUiVersion`, published template version, registry coherence-entry version) all = `0.1.52-preview.1`, the required `fs-gg-ui/v0.1.52-preview.1` + `fs-gg-ui-template/v0.1.52-preview.1` snapshot tags, and the hard ordering (release → registry → closure) in `specs/215-root-build-release-closure/readiness/coherence-map.md` (sources: data-model.md, contracts/coherent-set.md)
- [X] T004 Bump the org version line (the ONLY in-repo source edit): set `<FsGgUiVersion>0.1.51-preview.1</FsGgUiVersion>` → `<FsGgUiVersion>0.1.52-preview.1</FsGgUiVersion>` in `template/base/Directory.Packages.props`; touch NO `.fs`/`.fsi`/surface baseline (FR-011)
- [X] T005 **Early live smoke run (local pre-flight; NOT #9 evidence)**: `dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -o ~/.local/share/nuget-local` → `dotnet new install FS.GG.UI.Template::0.1.52-preview.1` → scaffold `dotnet new fs-gg-ui --name GeneratedProduct --output "$work/GeneratedProduct"` → `dotnet build` + `dotnet test` at the product root → headless `dotnet run` with `WAYLAND_DISPLAY`/`DISPLAY`/`XDG_RUNTIME_DIR` stripped (expect exit 0); record evidence in `specs/215-root-build-release-closure/readiness/preflight.md`
- [X] T005a **FR-003 parity check (byte-neutral across `designSystem`)**: scaffold the template under at least two `designSystem` values into separate output dirs and diff the emitted file sets (excluding the `FsGgUiVersion` pin line) to confirm byte-neutrality and identical stock-vs-FAKE project sets; if the gate does not already cover multiple `designSystem` values, record this spot-check **plus** the discharged-by-construction argument (only `FsGgUiVersion` changed vs the `main`-built template — no template content edit) in `specs/215-root-build-release-closure/readiness/parity.md` (FR-003)
- [X] T006 Create the coherent-set snapshot tags locally (ordering input for the guard): `git tag -a fs-gg-ui/v0.1.52-preview.1` and `git tag -a fs-gg-ui-template/v0.1.52-preview.1` (do NOT overwrite an existing tag — Edge "version already taken"; push deferred to T010)
- [X] T007 Run the Feature 209 staleness guard pre-flight: `dotnet fsi scripts/validate-version-coherence.fsx` (expect exit 0) and `FS_GG_RUN_VERSION_COHERENCE_SMOKE=1 dotnet fsi scripts/validate-version-coherence.fsx` (expect the full member set resolves to `0.1.52-preview.1`); record verdict in `specs/215-root-build-release-closure/readiness/coherence-guard.md`

**Checkpoint**: `FsGgUiVersion` bumped, local pre-flight green, snapshot tags staged, coherence guard exits 0
— the real release (US1) can now proceed.

---

## Phase 3: User Story 1 - A consumer installs the published template and gets a root-buildable product (Priority: P1) 🎯 MVP

**Goal**: Deliver the root-buildable guarantee through a **published** `FS.GG.UI.Template 0.1.52-preview.1`,
proven by the release-only gate running green on the real release and publishing the coherent set to the org feed.

**Independent Test**: From a clean environment with no repo working tree, install the newly published template
version, scaffold a product, and run stock restore → build → test → run at the product root — all succeed,
`app` profile exits 0 headless, zero FAKE invocations.

### Implementation for User Story 1

- [X] T008 [US1] Verify `.github/workflows/release.yml` trigger + gate wiring before triggering: a real `release: published` / `push: tags: ['v*']` resolves `$VER` from the tag and publishes; `publish-packages` declares `needs: [package-tests, template-product-tests]`; repo guard `github.repository == 'FS-GG/FS.GG.Rendering'` is present (contracts/release-gate.md) — read-only confirmation, no edit
- [ ] T009 [US1] Push the staged snapshot tags to origin: `git push origin fs-gg-ui/v0.1.52-preview.1 fs-gg-ui-template/v0.1.52-preview.1` (Foundational T006 tags)
- [ ] T009a [US1] **⚠️ HARD PREDECESSOR of T010 — land the bump on the release target.** `gh release create` defaults to the repo **default branch (`main`)**, which does NOT contain T004's branch-local `FsGgUiVersion` bump; releasing from it would publish a template carrying stale `FsGgUiVersion=0.1.51` and fail the coherence guard (FR-004/SC-003). Ensure the release-target commit contains `FsGgUiVersion=0.1.52-preview.1`: **either** merge `215-root-build-release-closure` → `main` first, **or** plan to pass `--target 215-root-build-release-closure` to T010. Then confirm the T006 snapshot tags (`fs-gg-ui/v0.1.52-preview.1`, `fs-gg-ui-template/v0.1.52-preview.1`) point at that **same** commit so tag ↔ published artifact ↔ bump all agree (resolves I1/I2)
- [ ] T010 [US1] Trigger the real release (NOT a dry run): `gh release create v0.1.52-preview.1 --repo FS-GG/FS.GG.Rendering --target <release-target-from-T009a> --title "fs-gg-ui 0.1.52-preview.1" --notes "Root-buildable template coherent set; closes #9."` — set `--target` to the commit/branch verified in T009a (omit only if the bump is already on `main`). Note: this both pushes tag `v0.1.52-preview.1` and publishes a release, and `release.yml` triggers on **both** `release: published` and `push: tags: ['v*']`; confirm the workflow is concurrency-guarded (or treat `release: published` as the load-bearing run) so no duplicate publish occurs
- [ ] T011 [US1] Watch the release run (`gh run watch --repo FS-GG/FS.GG.Rendering`) and confirm BOTH `package-tests` and `template-product-tests` are green on the real release; **capture the green `template-product-tests` run URL** — this is the load-bearing #9 evidence (FR-002/SC-002); record it in `specs/215-root-build-release-closure/readiness/release-gate-evidence.md`
- [ ] T012 [US1] Confirm `publish-packages` ran only after both gates and pushed the coherent `FS.GG.UI.*` + `FS.GG.UI.Template` set to `https://nuget.pkg.github.com/FS-GG/index.json` at `0.1.52-preview.1`; verify the `fs-gg-ui-template/v0.1.52-preview.1` package is resolvable on the feed (a red gate ⇒ no publish ⇒ #9 stays OPEN — Edge "Partial publish")
- [ ] T013 [US1] Confirm version coherence holds on the published set: the `version-coherence` push-gate step in `.github/workflows/gate.yml` is green on `main` after the bump, and `FsGgUiVersion` == published template version == `fs-gg-ui/v0.1.52-preview.1` tag (FR-004/SC-003)

**Checkpoint**: Published template `0.1.52-preview.1` carries the root-build artifacts, the real-release gate
is green (URL captured), the coherent set is on the feed, no straggler. US1 is independently verifiable.

---

## Phase 4: User Story 2 - The contract registry coherently advertises the released guarantee (Priority: P2)

**Goal**: Land `FS-GG/.github#25` so the `fs-gg-ui-template` contract records the `root-buildable` surface and
a `coherent: true` entry pinned to the **released** `0.1.52-preview.1`, tracking #9 — with no advertise-before-release window.

**Independent Test**: Read the merged `registry/dependencies.yml` + `docs/registry/compatibility.md`; confirm
the `fs-gg-ui-template` entry carries `root-buildable` + `coherent: true` pinned to `0.1.52-preview.1`,
referencing tracker #9.

> **⚠️ Ordering gate**: every task below requires US1's release to be **Published** (T012). Merging #25
> before publish would advertise an unsatisfied guarantee (FR-006/SC-004, Edge "Premature registry merge").
> Use the `cross-repo-coordination` skill for all `FS-GG/.github` edits.

- [ ] T014 [US2] In `FS-GG/.github`, rebase PR `#25` onto its base to clear the CONFLICTING state (do not merge yet)
- [ ] T015 [US2] Re-pin the `fs-gg-ui-template` entry in `FS-GG/.github` `registry/dependencies.yml` to `version: 0.1.52-preview.1` / `tag: fs-gg-ui-template/v0.1.52-preview.1`, `coherent: true`, recording the `root-buildable` surface (root `.slnx` + SDK pin + verb wrapper) and `tracking: FS-GG/FS.GG.Rendering#9` attributed to Feature 215/212 (FR-005)
- [ ] T016 [US2] Apply the matching projection in `FS-GG/.github` `docs/registry/compatibility.md`: same `root-buildable` guarantee + `0.1.52-preview.1` pin + tracker #9, visible to downstream readers (FR-007); confirm both files agree
- [ ] T017 [US2] **Only after T012 (release Published)**: merge PR #25 — `gh pr merge 25 --repo FS-GG/.github --squash`; verify the registry merge timestamp ≥ the release publish time (ordering held, SC-004)

**Checkpoint**: Merged registry advertises `root-buildable` + `coherent: true` pinned to the released
`0.1.52-preview.1` with tracker #9 visible; no advertise-before-publish window existed. US2 verifiable.

---

## Phase 5: User Story 3 - The board and issue close with released-artifact evidence (Priority: P3)

**Goal**: Close #9 with evidence (released version, green real-release gate URL, merged registry change), flip
the Coordination board H1 rendering item to Done, and signal the downstream SDD consumer.

**Independent Test**: Inspect #9 and the Coordination board; #9 is CLOSED with a comment linking the released
version, the green gate run, and the merged #25, and the board H1 rendering item reads "Done".

> **⚠️ Closure gate**: requires US1 Published+coherent (T011–T013) ∧ US2 Merged (T017) — data-model
> invariant 5 (all-or-nothing closure). Read the Coordination board before assuming any item is untracked
> (board draft items are dedupe trackers).

- [ ] T018 [US3] Close issue #9 with evidence: `gh issue comment 9 --repo FS-GG/FS.GG.Rendering --body "Released FS.GG.UI.Template 0.1.52-preview.1 (tag fs-gg-ui-template/v0.1.52-preview.1). Green real-release template-product-tests: <run-url from T011>. Registry coherent: FS-GG/.github#25 merged. Closes #9."` then `gh issue close 9 --repo FS-GG/FS.GG.Rendering` (FR-008)
- [ ] T019 [US3] Flip the FS-GG "Coordination" board H1 rendering item to `Done`: resolve the item id via `gh project item-list`, then `gh project item-edit … --field Status --value Done` (FR-009)
- [ ] T020 [US3] Signal the downstream FS.GG.SDD acceptance-probe consumer that the released root-buildable template `0.1.52-preview.1` is available for its probes (FR-010/SC-006) — via the `cross-repo-coordination` skill (issue comment / board note on the SDD H4 follow-on). **Read the Coordination board first** to resolve the exact SDD target id (board draft items are dedupe trackers — an empty issue search ≠ untracked); if no SDD H4 item is found, post the signal on tracker #9 / the board and note the unresolved target rather than skipping the handshake

**Checkpoint**: #9 CLOSED with released-artifact evidence; board H1 rendering item = Done; SDD consumer
unblocked against a published version. All stories complete.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validate the closure end-to-end and capture process feedback.

- [ ] T021 Run the quickstart acceptance roll-up (`specs/215-root-build-release-closure/quickstart.md` "Acceptance roll-up"): confirm SC-001…SC-006 each map to a completed task with recorded evidence
- [ ] T022 [P] Capture per-phase feedback into `specs/215-root-build-release-closure/feedback/` via the `fs-gg-feedback-capture` skill (release/closure friction, generalizable-release-tooling candidates, severity)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories.
- **US1 (Phase 3)**: Depends on Foundational (bump + tags + green guard). MVP.
- **US2 (Phase 4)**: Depends on Foundational AND **hard-blocked** on US1's release being Published (T012) before merge (T017) — FR-006.
- **US3 (Phase 5)**: Depends on US1 (Published+coherent) AND US2 (Merged) — invariant 5; closure is all-or-nothing.
- **Polish (Phase 6)**: Depends on US1–US3.

### Critical path (hard, not parallelizable across stories)

```
T004 bump → T006 tags → T007 guard → T009 push tags → T009a land bump on release target
  → T010 release → T011 gate green → T012 published → T017 merge #25
  → T018 close #9 → T019 board Done
```

### Within stories

- US1: T008 (read-only) ∥ ready anytime in phase; T009→T009a→T010→T011→T012→T013 are sequential (T009a — landing the bump on the release target — is a hard predecessor of T010, see I1/I2).
- US2: T014→T015→T016 prepare the PR; T017 is gated on T012.
- US3: T018→T019→T020 sequential closure handshake.

### Parallel Opportunities

- T001 and T002 (Setup) can run in parallel — different surfaces.
- T008 (read-only release.yml confirmation) can be done in parallel with Foundational work.
- T022 (feedback capture) is `[P]` — independent of the roll-up.
- Cross-story parallelism is intentionally **limited** by the hard ordering above; do not start US2's merge
  or US3 before US1 publishes.

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1 (Setup) + Phase 2 (Foundational) — including the **early live smoke run** (T005) that
   validates the template-under-release is root-buildable before the real release.
2. Complete Phase 3 (US1): release for real, gate green, coherent set published, capture the gate URL.
3. **STOP and VALIDATE**: a clean-environment install of `FS.GG.UI.Template 0.1.52-preview.1` scaffolds a
   root-buildable product (SC-001). This is the delivery that makes #9 closable.

### Incremental Delivery

1. Setup + Foundational → coherent-set staged and locally proven.
2. US1 → released + green real-release gate (MVP — the guarantee now reaches consumers).
3. US2 → registry coherently advertises the released guarantee (lands with/after the release).
4. US3 → #9 closed with evidence, board Done, SDD unblocked.

---

## Notes

- This feature edits **no** `.fs`/`.fsi`/surface baseline — the only in-repo source change is `FsGgUiVersion`
  (T004). Touching anything else is FR-011 scope creep.
- The #9 evidence MUST be the **real-release** gate run (T011), never a local/dry run (T005 is confidence only).
- Registry PR #25 lands **with or after** the release, never before (T017 gated on T012).
- Cross-repo work (`FS-GG/.github`, board, SDD signal) goes through the `cross-repo-coordination` skill.

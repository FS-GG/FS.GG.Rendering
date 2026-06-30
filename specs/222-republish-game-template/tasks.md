---
description: "Task list for republishing the game-profile-bearing template (Feature 222)"
---

# Tasks: Republish the `game`-Profile-Bearing Template (Release Feature 220)

**Input**: Design documents from `/specs/222-republish-game-template/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/fs-gg-ui-template-release.md, quickstart.md

**Tests**: No automated test tasks. This is a release-cadence + cross-repo registry feature with **no `.fs`/`.fsi` change** (producer code shipped in Feature 220, commit `b78e72a`). Per the spec/research (R5) and Constitution Principle V, evidence is **live cross-repo proof** (feed listing, content ancestry, consumer install/scaffold transcript, governance-green with zero `GovernanceTests` edits, registry projection) — **not** new unit tests, which "cannot observe the feed or a consumer token" and would be synthetic for the contract this feature changes. The pre-publish gates (`package-tests`, `template-product-tests`) run in existing CI and are not weakened.

**Organization**: Tasks are grouped by user story. The P1 stories form a strict **critical path** (publish → selectable → registry flip), so the user-story phases are ordered along that path (US2 → US1 → US3) rather than by spec narrative order; US4 (P2) closes the board. Each phase keeps its spec `[USn]` label for traceability.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files/artifacts, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1–US4)
- Exact file paths / commands / evidence artifacts included in each task

## Path Conventions

This feature has **no `src/` module**. The artifacts are: two in-repo version pins (MSBuild), git tags, already-authored GitHub Actions YAML (read-only), the cross-repo registry YAML in `FS-GG/.github`, and live evidence captured under `specs/222-republish-game-template/readiness/`.

> **⚠️ Operator-gated steps (disclose & defer, do not fake).** Pushing the release tag-set requires
> release rights on `FS-GG/FS.GG.Rendering`; merging the registry PR requires `FS-GG/.github` merge
> rights; the consumer probe requires a `packages: read`-only org token. Where a credential is
> unavailable, mark the task `environment-limited` with the disclosed substitute (Principle V) — never
> fabricate the evidence.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the release preconditions and a no-regression baseline before any tag is cut.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline MUST run **every** test project
> (solution + `tests/Package.Tests` + `samples/**/*.Tests`) via the discovery-based runner so pre-existing
> reds are known up front and not mistaken for regressions at merge. This feature makes **zero `src/`
> changes**, so the post-release re-baseline (T027) must show zero new reds.

- [X] T001 Confirm release preconditions in repo root: working tree clean, branch is `222-republish-game-template`, and Feature-220 is reachable — `git merge-base --is-ancestor b78e72a HEAD && git merge-base --is-ancestor b78e72a main` (both true). Record to `specs/222-republish-game-template/readiness/preconditions.md`.
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/222-republish-game-template/readiness/baseline.md` (globs `*.Tests.fsproj` — runs EVERY test project incl. `tests/Package.Tests` + `samples/**/*.Tests`; records the full red/green set; pre-existing reds are flagged here, not at merge).
- [X] T003 [P] Snapshot the two in-repo version pins (both `0.1.53-preview.1` today) — `grep -n 'FsGgUiVersion\|<Version>' template/base/Directory.Packages.props .template.package/FS.GG.UI.Template.fsproj` — to `specs/222-republish-game-template/readiness/pin-snapshot.md` as the pre-release reference for the bump in T008.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the release/content/registry gate map and **front-load the live feed probe** that confirms (or replaces) the plan's root-cause hypotheses before any release tag is pushed.

**⚠️ CRITICAL**: No user-story work (no tag push) can begin until this phase is complete.

> **⚠️ Early live probe (STANDING, do not omit).** This feature's "app" is the **release/feed/scaffold
> path**, not the Skia viewer (plan §Standing assumption). The mandated early live evidence is the
> **pre-publish feed probe** (quickstart §0): read the actual feed listing, confirm `0.1.53-preview.1`
> lacks `b78e72a`, and exercise a consumer-token install. Treat "the feed lacks `game` / the machinery
> is intact" as **unverified assumptions until probed live** — deterministic local checks have passed
> while the cross-repo path stayed red in Features 175/216/218.

- [X] T004 Build the release/content/registry gate map (the classification/root-cause map every story depends on): enumerate each gate on the critical path (`V > 0.1.53-preview.1`; `b78e72a` ancestor of the tag; feed serves `V` for the whole set; no exit 103; `game` scaffold-selectable; governance green zero-edit; registry flip after listing) → `specs/222-republish-game-template/readiness/gate-map.md`.
- [X] T005 **Early live feed probe** (quickstart §0, BEFORE any tag): `gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template/versions --jq '.[].name'` (expect `0.1.53-preview.1`, no `V`); `git merge-base --is-ancestor b78e72a fs-gg-ui-template/v0.1.53-preview.1` (expect false — feed lacks 220); `git merge-base --is-ancestor b78e72a main` (expect true); attempt a `packages: read`-token `dotnet new install FS.GG.UI.Template::0.1.53-preview.1` to exercise the consumer reachability path. Capture transcript to `specs/222-republish-game-template/readiness/pre-publish-probe.md` (or `environment-limited` with disclosed substitute).
- [X] T006 [P] Confirm the producer machinery is intact and unchanged (READ-ONLY, FR-010): `.github/workflows/release.yml` `publish-packages` job (canonical-repo guard, `GITHUB_TOKEN` `packages: write`, `v*`/`workflow_dispatch` trigger) and `scripts/derive-template-version.sh` + the Feature-216 dispatch-sender reference. Record "no edits required" to `specs/222-republish-game-template/readiness/machinery-check.md`.
- [X] T007 [P] Verify the `game` profile source will pack & be selectable (READ-ONLY): confirm `.template.config/template.json` carries the `game` choice on the current `HEAD`. Record to `specs/222-republish-game-template/readiness/machinery-check.md`.

**Checkpoint**: Gate map written, pre-publish gap confirmed live, machinery confirmed unchanged — the publish (US2) can proceed.

---

## Phase 3: User Story 2 - Publish the `game`-bearing coherent set to the org feed (Priority: P1) 🎯 Gate

**Goal**: Cut and publish a coherent `FS.GG.UI.*` + `FS.GG.UI.Template` set at one version `V > 0.1.53-preview.1` that contains `b78e72a`. This is the publish gate — nothing downstream can select `game` from the feed until it is done.

**Independent Test**: Tag the coherent set so `release.yml` `publish-packages` packs + pushes; then query the org feed and confirm a `FS.GG.UI.Template` version `> 0.1.53-preview.1` is served (verified in Phase 4).

- [X] T008 [US2] Bump BOTH in-repo pins to `V` (`> 0.1.53-preview.1`; expected `0.1.54-preview.1`) — owned by the `speckit-merge`/release flow: `<FsGgUiVersion>` in `template/base/Directory.Packages.props` and `<Version>` in `.template.package/FS.GG.UI.Template.fsproj` must both equal `V` (so `template-product-tests`' local-feed restore resolves). Verify against the T003 snapshot.
- [X] T009 [US2] Cut the release from a `main` commit containing `b78e72a` and push the release tag-set (`fs-gg-ui-template/v<V>` + sibling `v*` tags) so `release.yml` `publish-packages` packs + pushes the whole `FS.GG.UI.*` + template set at `V`. **Operator action** (release rights) — `environment-limited`/defer if unavailable. Record the tag(s) + run link to `specs/222-republish-game-template/readiness/publish.md`.
- [X] T010 [US2] Confirm the dispatch-sender propagated `V`: `scripts/derive-template-version.sh` derived the version from `fs-gg-ui-template/v<V>` and the Feature-216 reusable sender notified Templates (FR-010 / US2 AS2 — Templates re-pin half). Capture the dispatch evidence to `specs/222-republish-game-template/readiness/publish.md`.

**Checkpoint**: The coherent set is tagged and the publish workflow has run — the feed should now serve `V` (proven in Phase 4).

---

## Phase 4: User Story 1 - The `game` profile is selectable from the org feed (Priority: P1)

**Goal**: An ordinary `packages: read` consumer installs `FS.GG.UI.Template@V` and scaffolds the `game` profile to a building, governance-green product, with the non-game profiles unaffected.

**Independent Test**: From a clean env authenticated only as a `packages: read` org consumer, install `FS.GG.UI.Template@<V>` and scaffold the `game` profile; observe the `game` choice accepted and the minimal MVU starter generated (evidence: feed listing + install/scaffold transcript).

**Depends on**: Phase 3 (the feed must serve `V`).

- [X] T011 [US1] Confirm the org feed serves `V`: `gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template/versions --jq '.[].name' | grep -F "<V>"` (one new version `> 0.1.53-preview.1`). Capture to `specs/222-republish-game-template/readiness/feed-listing.md`. (SC-002)
- [X] T012 [US1] Coherent-set probe (edge case "incoherent set"): confirm ≥1 sibling `FS.GG.UI.*` package (e.g. `FS.GG.UI.Core` / `FS.GG.UI.Scene`) is served at the **same** `V` on the org feed and that the template version equals the sibling package versions — `gh api orgs/FS-GG/packages/nuget/<pkg>/versions --jq '.[].name' | grep -F "<V>"` for each — so FR-001 coherence is live-verified, not inferred from `publish-packages` packing. Capture to `specs/222-republish-game-template/readiness/coherent-set.md`. (FR-001, SC-002 — edge case spec.md "coherent set is incoherent")
- [X] T013 [US1] Content gate — `git merge-base --is-ancestor b78e72a fs-gg-ui-template/v<V>` true AND the packed `V` template actually exposes the `game` choice (inspect the packed artifact's `template.json`, not just the version string). Capture to `specs/222-republish-game-template/readiness/content-gate.md`. (FR-002, SC-002)
- [X] T014 [US1] Consumer install from a `packages: read`-only token: `dotnet new install FS.GG.UI.Template::<V>` → exit 0, **not** exit 103 (re-confirms Feature-218 org-readability for `V`). Capture transcript to `specs/222-republish-game-template/readiness/consumer-install.md`. (FR-003, SC-001)
- [X] T015 [US1] Scaffold the `game` profile: `dotnet new fs-gg-ui --profile game -o /tmp/game-probe` (confirm the exact profile flag against the packed `template.json`) → choice accepted, minimal Pong-style `Model`/`Msg`/`update`/`view` + tick starter generated, no missing-profile / unknown-choice error. Capture to `specs/222-republish-game-template/readiness/game-scaffold.md`. (FR-004, SC-001)
- [X] T016 [US1] Generated `game` product builds + passes governance with **zero** `GovernanceTests` edits: in `/tmp/game-probe` run `dotnet build` then `dotnet test`; no-flag launch renders a live interactive game scene (no `-- pong`-style flag). Capture to `specs/222-republish-game-template/readiness/game-governance.md`. (FR-004, SC-003)
- [X] T017 [P] [US1] Non-game parity: `dotnet new fs-gg-ui --profile app -o /tmp/app-probe` still scaffolds the controls showcase; regenerate `headless-scene`/`governed`/`sample-pack` and diff each against Feature 220's diff-verified baseline (byte-identical). Capture to `specs/222-republish-game-template/readiness/non-game-parity.md`. (FR-005, SC-003)

**Checkpoint**: The `game` profile is provably installable + scaffold-selectable from the feed, the set is coherent at `V`, and the non-game profiles are unaffected — the registry may now flip (publish-before-flip satisfied).

---

## Phase 5: User Story 3 - The cross-repo contract record flips UNRELEASED → released (Priority: P1)

**Goal**: `FS-GG/.github` records the `game` profile as released at `V`, flips the coherence entry, and regenerates the compatibility projection — landing as a `contract-change` PR.

**Independent Test**: Inspect the registry entry + compatibility projection after the landing; confirm `game` no longer reads "UNRELEASED", names `V`, and the `coherence` entry is flipped; #33 carries `V` + the registry PR reference.

**Depends on**: Phase 4 (FR-007 — the flip MUST follow a confirmed feed listing).

- [X] T018 [US3] In `FS-GG/.github` `registry/dependencies.yml` (`fs-gg-ui-template`): advance `version` / `package-version` / `package-tag` to `V`; flip the `game`-profile note **UNRELEASED → released @ V**; flip the coherence block (`- id: fs-gg-ui-template` → `resolved_by: fs-gg-ui-template/v<V>`). No coherence flag flips `true→false` — this advances a coherent contract.
- [X] T019 [US3] Regenerate the `FS-GG/.github` `docs/registry/compatibility.md` projection so it names `V` and reads `game` released (no stale `0.1.53-preview.1` for this surface). Do not hand-edit the projection beyond regeneration. (SC-004)
- [X] T020 [US3] Open + land the `contract-change` PR on `FS-GG/.github` carrying T018+T019 — **only after** T011/T012/T013 are green (publish-before-flip, FR-007). **Operator action** (merge rights) — `environment-limited`/defer if unavailable. Record the PR link to `specs/222-republish-game-template/readiness/registry-pr.md`.

**Checkpoint**: The registry + compatibility projection are the source of contract truth for `V` with `game` released — the board can be closed.

---

## Phase 6: User Story 4 - The board and blocked consumers are released (Priority: P2)

**Goal**: #33 closed (with `V` + registry PR linked), board item #33 → `Done`, #31's `Blocked` mirror cleared, and SDD#44 notified of `V`.

**Independent Test**: Confirm #33 is closed with the version + registry PR linked; item #31's `Blocked by` no longer points at an open #33; SDD#44 carries a comment with the published `V`.

**Depends on**: Phase 5 (a `contract-change` item's resolution includes the registry update).

- [X] T021 [US4] Close issue `FS-GG/FS.GG.Rendering#33` with the published `V` string + the registry PR link recorded on it, and move board item #33 to `Done` on the Coordination board (Projects v2 #1). (FR-008, SC-005)
- [X] T022 [P] [US4] Clear the `Blocked by: FS.GG.Rendering#33` mirror on board item `#31` (no longer blocked by an open #33). Capture state to `specs/222-republish-game-template/readiness/board-closure.md`. (FR-008, SC-005)
- [X] T023 [P] [US4] Notify downstream `FS-GG/FS.GG.SDD#44` (the `app → game` default-flip) of the published `V` via a cross-repo comment so the default-selection flip can proceed (the flip itself is out of scope). Capture to `specs/222-republish-game-template/readiness/board-closure.md`. (FR-009, SC-005)

**Checkpoint**: All user stories complete — producer side of #33 fully resolved and the downstream unblock recorded.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Consolidate evidence, run the quickstart end-to-end, and prove no regression.

- [X] T024 Consolidate all live evidence under `specs/222-republish-game-template/readiness/` (feed-listing, coherent-set, content-gate, consumer-install, game-scaffold, game-governance, non-game-parity, registry-pr, board-closure) into a single `evidence-summary.md` mapping each artifact to its SC/FR.
- [X] T025 [P] Run `quickstart.md` end-to-end and check off its "Done When" list (steps 0–7) against the captured evidence.
- [X] T026 [P] Capture per-phase Spec Kit / fs-gg-ui feedback via the `fs-gg-feedback-capture` flow into `specs/222-republish-game-template/feedback/`.
- [X] T027 Re-run `dotnet fsi scripts/baseline-tests.fsx --out specs/222-republish-game-template/readiness/baseline-post.md` and diff against T002 — with zero `src/` changes, there must be **zero new reds**.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories (no tag until the pre-publish probe confirms the gap).
- **User Stories (Phases 3–6)**: Unlike a typical feature, the P1 stories are a **strict critical path**, not independent:
  - **US2 (Phase 3, publish)** → **US1 (Phase 4, selectable/consumer proof)** → **US3 (Phase 5, registry flip, publish-before-flip FR-007)** → **US4 (Phase 6, board closure)**.
- **Polish (Phase 7)**: Depends on Phases 3–6.

### Critical-path note (data-model)

```
main has b78e72a → bump pins to V, push release tag-set (US2)
   → feed serves V coherent set, coherence probed (US2/US1)
   → live probe: no 103, game scaffold ok, governance green, non-game parity (US1)
   → registry flip UNRELEASED→released @ V (US3, publish-BEFORE-flip)
   → close #33 (+V,+PR) · board #33 Done · #31 unblocked · notify SDD#44 (US4)
```

### Within Each Phase

- T001/T002 before T003's interpretation; all Setup [P] tasks (T003) parallel with the rest once the repo state is read.
- T006/T007 are [P] (different read-only artifacts) and run alongside T004/T005.
- T017 is [P] (separate evidence artifact) and runs alongside T011–T016 once the feed serves `V`.
- T022/T023 are [P] (separate GitHub objects) once #33 is closed (T021).

### Parallel Opportunities

- **Setup**: T003 ∥ (after T001/T002).
- **Foundational**: T006 ∥ T007 (∥ the probe narrative T004/T005).
- **US1**: T017 (non-game parity) ∥ T011–T016 once `V` is served.
- **US4**: T022 ∥ T023 once T021 closes #33.
- **Polish**: T025 ∥ T026.

---

## Parallel Example: Foundational (Phase 2)

```bash
# After the gate map (T004) and live probe (T005) are underway, run the read-only confirmations together:
Task: "T006 Confirm release.yml publish-packages + derive-template-version.sh unchanged (FR-010)"
Task: "T007 Verify .template.config/template.json carries the game choice on HEAD"
```

## Parallel Example: User Story 1 (Phase 4)

```bash
# Once the feed serves V, prove the consumer path and non-game parity in parallel:
Task: "T014 Consumer install FS.GG.UI.Template::<V> (packages:read, no 103)"
Task: "T017 Non-game parity: app showcase + headless-scene/governed/sample-pack byte-identical to F220 baseline"
```

---

## Implementation Strategy

### MVP / Gate First (User Story 2 → User Story 1)

1. Complete Phase 1 (Setup) + Phase 2 (Foundational — including the **early live feed probe** confirming the gap before any tag).
2. Complete Phase 3 (US2, publish the coherent set) — the gate.
3. Complete Phase 4 (US1, prove `game` selectable from the feed + governance green zero-edit).
4. **STOP and VALIDATE**: the `game` profile is resolvable on the feed — this is the substantive deliverable of #33.

### Incremental Delivery

1. Setup + Foundational → gap confirmed live.
2. US2 (publish) → US1 (selectable proof) → the feature's core value is delivered.
3. US3 (registry flip) → contract record is truthful (publish-before-flip).
4. US4 (board closure) → #33 done, #31 unblocked, SDD#44 notified.

---

## Notes

- **No automated test tasks** — evidence is live cross-repo proof (R5, Principle V); the existing `package-tests` / `template-product-tests` gates run in CI before publish and are not weakened.
- **No `src/` / `.fs` / `.fsi` change** — only two version pins, git tags, and the cross-repo registry; `release.yml` / `derive-template-version.sh` / dispatch-sender are read-only (FR-010).
- **publish-before-flip (FR-007)** is structural: Phase 5 (registry) MUST NOT start until Phase 4's feed listing + coherence + content gate (T011/T012/T013) are green.
- **Operator-gated** tasks (T009 tag push, T014 consumer-token install, T020 registry PR merge) are `environment-limited`/deferred with a disclosed substitute when the credential is unavailable — never faked.
- `<V>` is fixed by the merge/release flow (expected `0.1.54-preview.1`); the tasks bind only the `> 0.1.53-preview.1` constraint, not the literal.
- Commit after each phase or logical group.

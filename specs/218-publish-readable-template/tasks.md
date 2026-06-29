---
description: "Task list for Publish & Make-Readable the productName-Enabled Template"
---

# Tasks: Publish & Make-Readable the productName-Enabled Template

**Input**: Design documents from `/specs/218-publish-readable-template/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/fs-gg-ui-template-release.md, quickstart.md

**Tests**: NOT requested. This is a release-cadence + package-visibility + cross-repo-registry feature with **no `.fs`/`.fsi` change** (producer code shipped in Feature 217, commit `6df0d39`). Evidence is **live cross-repo proof** (feed listing, foreign-token `dotnet new install`, `--productName` scaffold, registry/issue/board closure) — not new unit tests. The existing release-only gates (`package-tests`, `template-product-tests`) run in CI before publish; no assertion is weakened. Therefore no TDD/contract-test tasks are generated.

**Organization**: Tasks are grouped by user story. NOTE — the four stories are tightly **coupled** (FR-004 "no half-landing"): US2 (publish) and US3 (visibility) are independent producer actions, but US1 (the combined end-to-end proof) only passes once **both** US2 and US3 hold for the **same** `V`, and US4 (registry/closure) records the landing. The true shippable increment is **US2 + US3 + US1 together**, not any single story. Phases are ordered by real dependency within the P1 band: US2 → US3 → US1 → US4.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files/systems, no dependencies)
- **[Story]**: US1 / US2 / US3 / US4 (maps to spec.md user stories)
- `V` = the released coherent-set version (expected `0.1.53-preview.1`, hard constraint `> 0.1.52-preview.1`); the exact literal is fixed by the merge bump, not hard-coded.
- **Operator step** = admin/CI/cross-repo action that cannot be fully scripted in-session (tag push by an operator with rights; org admin package-settings UI; PR against `FS-GG/.github`). These are disclosed and their acceptance is machine-checkable, per Principle V.

## Path Conventions

Release/coordination feature — no `src/` module. Files touched (version pins only) and evidence sources read:

- `template/base/Directory.Packages.props` — `<FsGgUiVersion>` pin → `V`
- `.template.package/FS.GG.UI.Template.fsproj` — `<Version>` pin → `V`
- `.template.config/template.json` — Feature-217 `productName` symbol (READ-ONLY; verify it packs)
- `.github/workflows/release.yml`, `.github/workflows/template-dispatch.yml`, `scripts/derive-template-version.sh` — READ-ONLY (already authored; exercised, not changed)
- `specs/218-publish-readable-template/readiness/` — live evidence capture
- Cross-repo `FS-GG/.github`: `registry/dependencies.yml` + `docs/registry/compatibility.md` (contract-change landing point)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Evidence scaffolding and the no-regression baseline before any release action.

- [X] T001 Create the live-evidence directory `specs/218-publish-readable-template/readiness/` for feed/visibility/install/scaffold/registry/closure transcripts (per research R4 and quickstart).
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/218-publish-readable-template/readiness/baseline.md` (runs EVERY test project — solution + `tests/Package.Tests` + `samples/**/*.Tests` — and records the full red/green set; the release-only `package-tests`/`template-product-tests` gates are exactly the ones that run before publish, so pre-existing reds are flagged here, not discovered at merge).
- [X] T003 [P] Verify the Feature-217 `productName` symbol is present on `main` and is packed by the template: confirm commit `6df0d39` is in history (`git log --oneline | grep 6df0d39`) and that `.template.config/template.json` declares the `productName` symbol (READ-ONLY) — write the confirmation to `specs/218-publish-readable-template/readiness/feature-217-present.md`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the two-gate root-cause map and — STANDING REQUIREMENT — **front-load a live feed/visibility probe** that confirms or replaces the plan's hypotheses BEFORE any release tag is pushed.

**⚠️ CRITICAL**: No user-story release action (US2/US3/US1/US4) may begin until this phase is complete and the baseline failing state is captured.

> **⚠️ Early live smoke run (STANDING, do not omit).** This feature's "app" is the **release/feed/scaffold path**, not the Skia viewer. Feature 175/216 showed deterministic local checks pass while the cross-repo path stays red. Treat exit-127 (productName) and exit-103 (visibility) as the only authoritative pass/fail signals and probe them **live** against the real org feed and a real consumer token before tagging.

- [X] T004 Build the classification / root-cause map of the **two coupled gates** in `specs/218-publish-readable-template/readiness/root-cause-map.md`: (a) exit-127 = feed serves only pre-217 `0.1.52-preview.1`, fixed by publishing a `> 0.1.52-preview.1` Feature-217 version (US2); (b) exit-103 = `FS.GG.UI.Template` is `private`, fixed by visibility `private → internal` (US3); (c) FR-004 binding invariant — both must hold for the **same** `V` (US1). Map each to its FR/INV/SC.
- [X] T005 **Early live feed/visibility probe (the smoke run)**: capture the real pre-release failing state described by #29/#26 into `specs/218-publish-readable-template/readiness/baseline-probe.md` — run `gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template/versions --jq '.[].name'` (expect only `0.1.52-preview.1`), `gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template --jq .visibility` (expect `private`), and attempt a consumer-token `dotnet new install FS.GG.UI.Template@0.1.52-preview.1` + `dotnet new fs-gg-ui --productName Acme` to observe the live 103/127 (or `environment-limited` with a disclosed substitute). This replaces the plan's hypotheses with observed facts before any tag is pushed.
- [X] T006 [P] Confirm the current in-repo version pins and the release tag-set families in `specs/218-publish-readable-template/readiness/preconditions.md`: read `template/base/Directory.Packages.props` `<FsGgUiVersion>` and `.template.package/FS.GG.UI.Template.fsproj` `<Version>` (both should be `0.1.52-preview.1`), and confirm the three tag families exist at prior versions (`git tag --list 'v0.1.5*'`, `'fs-gg-ui-template/v0.1.5*'`, `'fs-gg-ui/v0.1.5*'`) per research R2.
- [X] T007 [P] Confirm the publish machinery is already authored and READ-ONLY for this feature: spot-check `.github/workflows/release.yml` (`publish-packages` packs the coherent set at `-p:Version=V`, pushes to `nuget.pkg.github.com/FS-GG` with `GITHUB_TOKEN`, canonical-repo guard), `.github/workflows/template-dispatch.yml` (fires on `fs-gg-ui-template/v*`), and `scripts/derive-template-version.sh` — record in `preconditions.md` that no workflow logic change is needed (FR scope fence §5).

**Checkpoint**: Two-gate root-cause map confirmed against a **live** pre-release probe; pins and tag-set verified; machinery confirmed unchanged. Release actions can now begin.

---

## Phase 3: User Story 2 - Feature-217-bearing version is on the org feed (Priority: P1)

**Goal**: Cut and publish a `FS.GG.UI.Template` coherent-set version `> 0.1.52-preview.1` that carries Feature 217 to the org feed (resolves #29, the producer half).

**Independent Test**: Query the org feed for `FS.GG.UI.Template`; confirm a version `> 0.1.52-preview.1` is served and that installing it and scaffolding with `--productName` succeeds (no exit 127).

- [X] T008 [US2] Bump the coherent-set version pin `<FsGgUiVersion>` to `V` in `template/base/Directory.Packages.props` (INV-2). NOTE: prefer the repo's `speckit-merge` flow to fix the exact `V` (Feature 204 precedent — the packer fixes the literal); if cut manually, both pins (T008, T009) MUST move together.
- [X] T009 [US2] Bump the template package `<Version>` to `V` in `.template.package/FS.GG.UI.Template.fsproj` so it matches `<FsGgUiVersion>` and the `template-product-tests` local-feed restore resolves (FR-006, INV-2). Confirm `V > 0.1.52-preview.1` strictly (INV-1).
- [X] T010 [US2] **Operator step** — cut the coherent-set release through the existing `speckit-merge` → tag-push → `release.yml` path, pushing the three-tag set at `V`: `v<V>` (publish), `fs-gg-ui-template/v<V>` (Templates dispatch, FR-010), `fs-gg-ui/v<V>` (Feature-209 coherence-mirror snapshot) per research R2. No manual `dotnet nuget push`.
- [X] T011 [US2] Confirm `release.yml` `publish-packages` is green and the whole coherent set (17 `FS.GG.UI.*` packables + the template) was pushed at the same `V` (FR-006); capture the run URL ("Your package was pushed") in `specs/218-publish-readable-template/readiness/publish-run.md`.
- [X] T012 [US2] Confirm the org feed actually **serves** `V` (FR-005, SC-001, INV-3): `gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template/versions --jq '.[].name'` lists `V`; record in `specs/218-publish-readable-template/readiness/feed-serves-V.md`.
- [X] T013 [US2] Confirm the Feature-216 template-released dispatch fired (FR-010, INV-4): verify the `template-dispatch.yml` run triggered by the `fs-gg-ui-template/v<V>` tag is green / FS.GG.Templates was notified; capture the run URL in `specs/218-publish-readable-template/readiness/dispatch-fired.md`, or mark `environment-limited` with a disclosed substitute. A missing dispatch does **not** by itself block FR-004 — Templates can re-pin off the #29 reply (FR-010 is SHOULD).
- [X] T014 [US2] Reply on **#29** (`gh issue comment 29 --repo FS-GG/FS.GG.Rendering`) with a `## Response` containing the published `V` string so Templates has the exact version to re-pin (FR-007, INV-14). (Closure of #29 happens in US4 once both gates hold.)

**Checkpoint**: Feed serves a `> 0.1.52-preview.1` Feature-217 version `V`; #29 has the version string. Still `private` → US1's no-103 proof is not yet possible.

---

## Phase 4: User Story 3 - The template package is org-readable (Priority: P1)

**Goal**: Make `FS.GG.UI.Template` readable by ordinary org consumers — visibility `private → internal` preferred, or grant `FS-GG/FS.GG.Templates` repo Read (resolves #26, the visibility half).

**Independent Test**: With a token that does **not** carry an explicit private-package grant, `dotnet new install` the package from a different repo's job context; observe exit 0 (no exit 103).

**NOTE**: Independent of US2 — visibility is per-package and version-independent (research R3, INV-9), so this can proceed in parallel with US2. It does not, by itself, fix exit 127.

- [X] T015 [US3] **Operator step (org admin — not scriptable)** — flip `FS.GG.UI.Template` package visibility `private → internal` at `https://github.com/orgs/FS-GG/packages/nuget/package/FS.GG.UI.Template/settings`. There is **no** `gh api` endpoint to change visibility (research R3); if admin rights are unavailable in-session, surface it as an explicit manual step (do not silently skip). Fallback that satisfies FR-003 equally: grant `FS-GG/FS.GG.Templates` repo Read on the same settings page.
- [X] T016 [US3] Verify the readability flip (INV-8): `gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template --jq .visibility` returns `internal` (or confirm the Templates Read grant exists); record in `specs/218-publish-readable-template/readiness/visibility-internal.md`.

**Checkpoint**: Package is org-readable. Combined with US2's served `V`, the FR-004 binding invariant can now be proven (US1).

---

## Phase 5: User Story 1 - SDD-orchestrated scaffold succeeds end-to-end (Priority: P1) 🎯 MVP

**Goal**: Prove the combined outcome — an ordinary org consumer token (`packages: read`) installs `V` and scaffolds with `--productName`: no exit 103, no exit 127. This is the entire point of #29 + #26 and the binding invariant FR-004.

**Independent Test**: From a clean environment authenticated only as an org consumer (no special private-package grant), `dotnet new install FS.GG.UI.Template@V` then `dotnet new fs-gg-ui --productName <P>`; observe exit 0 on both, captured as evidence.

**Depends on**: US2 (T012 feed serves `V`) **and** US3 (T016 readable) — both for the **same** `V`.

- [X] T017 [US1] Prove **no exit 103** (SC-002, INV-8): from a context authenticated as an *ordinary* org consumer (`packages: read`, no special grant) — the honest probe is a re-run of the FS.GG.Templates composition CI job that 103'd, or an equivalent foreign-token install — run `dotnet new install FS.GG.UI.Template@V` and assert exit 0 (no "could not be authenticated"/NotFound). Capture transcript in `specs/218-publish-readable-template/readiness/no-103-install.md`.
- [X] T018 [US1] Prove **no exit 127** (SC-003, INV-6): with `V` installed, run `dotnet new fs-gg-ui --productName Acme --output ./Acme` (NO `-n` — the SDD scaffold-provider form) and assert exit 0 and that `./Acme` scaffolds. Capture transcript in `specs/218-publish-readable-template/readiness/no-127-scaffold.md`.
- [X] T019 [US1] Confirm the **combined gate** (FR-004, INV-15): both T017 (no 103) and T018 (no 127) pass for the **same** `V`. Record the binding-invariant verdict in `specs/218-publish-readable-template/readiness/combined-gate.md` — a version published-but-private or readable-but-old is NOT done.

**Checkpoint**: The SDD-orchestrated scaffold path is green end-to-end for one `V`. The coherent landing is achieved (this is the MVP); US4 records it and closes out.

---

## Phase 6: User Story 4 - Cross-repo contract record stays coherent (Priority: P2)

**Goal**: Update the registry, close the issues, move the board, and confirm the downstream unblock so the landing is auditable (ADR-0001 contract-change rule).

**Independent Test**: Inspect `registry/dependencies.yml` and the compatibility projection for the new `package-version` and flipped coherence entry; confirm #29/#26 closed and the board items `Done`.

**Depends on**: US1 (T019 combined gate) — the registry should only name `V` as released once both gates hold.

- [X] T020 [US4] **Operator step (cross-repo PR)** — update the `fs-gg-ui-template` entry in `FS-GG/.github` `registry/dependencies.yml`: `version`/`package-version` → `V`, `package-tag` → `fs-gg-ui-template/v<V>`, and flip the `productName` feed-note from "UNRELEASED on the feed" to **released in `V`** (FR-008, R5, INV-10/11). No contract *surface* field changes (FR-009, INV-13).
- [X] T021 [US4] In the same `FS-GG/.github` PR, advance the coherence block `- id: fs-gg-ui-template` (`coherent: true`) `resolved_by` to `fs-gg-ui-template/v<V>` and record org-readability (visibility `internal`) so the Templates-CI consumer half is no longer auth-blocked; no coherence flag flips `true→false` — this *advances* it (INV-12, contract §3).
- [X] T022 [P] [US4] Regenerate / update the `docs/registry/compatibility.md` projection in the same PR to match the `dependencies.yml` delta (FR-008, R5).
- [X] T023 [US4] Close **#29** and **#26** (`gh issue close 29 26 --repo FS-GG/FS.GG.Rendering`) once **both** gates hold for the same `V` (FR-007, FR-004, INV-15); link the readiness evidence.
- [X] T024 [P] [US4] Move the two Coordination-board rows (Projects v2 #1 — Phase P4 Templates · Workstream Composition · Contract `fs-gg-ui-template`) to `Done` (INV-16).
- [X] T025 [US4] Confirm the downstream unblock (SC-004, INV-16): verify FS.GG.Templates **#32** is no longer `Blocked` (its `Blocked by` on #29/#26 cleared) and **capture the Templates `FSGG_COMPOSITION_FULL=1` → `29/29` re-pin run URL** in `specs/218-publish-readable-template/readiness/downstream-2929.md` when available (`environment-limited` until Templates re-pins). This feature **confirms and links** the unblock; it does NOT perform the Templates re-pin (spec Assumption, contract §4).

**Checkpoint**: Registry + projection name `V`, issues closed, board `Done`, #32 unblocked. The contract record matches reality.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validate end-to-end and capture process feedback.

- [X] T026 Run the full `quickstart.md` top-to-bottom against the released `V` and confirm every step passes (§0 baseline → §1 publish → §2 visibility → §3 combined → §4 registry → §5 closure); archive the consolidated transcript under `specs/218-publish-readable-template/readiness/`.
- [X] T027 [P] Assemble the readiness summary in `specs/218-publish-readable-template/readiness/README.md` mapping each SC-### / FR-### / INV-### to its captured evidence file (live, or `environment-limited` with disclosed substitute per Principle V).
- [X] T028 [P] Capture per-phase fs-gg-ui / Spec Kit feedback into `specs/218-publish-readable-template/feedback/` (use the `fs-gg-feedback-capture` skill) — note any release/visibility friction (e.g. the no-REST-for-visibility gap, operator-gated steps).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. **BLOCKS all user stories** — the live feed/visibility probe (T005) must capture the failing baseline before any tag is pushed.
- **User Stories (Phases 3–6)**: All depend on Foundational. Within the P1 band the real dependency order is:
  - **US2 (publish)** and **US3 (visibility)** are **independent** of each other (visibility is version-independent — research R3) and may proceed in parallel.
  - **US1 (combined proof)** depends on **both** US2 (T012) and US3 (T016) for the **same** `V`.
  - **US4 (registry/closure, P2)** depends on **US1** (T019) — only name `V` as released once both gates hold.
- **Polish (Phase 7)**: Depends on US1 + US4.

### The binding invariant (FR-004 — no half-landing)

There MUST exist **one** `V` for which **both** hold: INV-6 (honors `--productName` → no 127, from US2) AND INV-8 (readable by consumer token → no 103, from US3). Neither US2 nor US3 alone is "done"; **US1 (T019) is the gate that proves the conjunction**, and it is the real MVP increment.

### Parallel Opportunities

- Setup: T003 ∥ T002.
- Foundational: T006 ∥ T007 (after T004/T005).
- **US2 ∥ US3**: the publish track (T008–T014) and the visibility track (T015–T016) can run concurrently — they touch different systems (repo pins/feed vs. org package settings).
- US4: T022 (compatibility projection) ∥ T024 (board) once the registry edit (T020/T021) and closures are in flight.
- Polish: T027 ∥ T028.

---

## Parallel Example: US2 and US3 (the two coupled gates)

```bash
# Track A — US2 publish (repo + feed):
Task: "Bump <FsGgUiVersion> in template/base/Directory.Packages.props to V"
Task: "Bump <Version> in .template.package/FS.GG.UI.Template.fsproj to V"
Task: "Cut release via speckit-merge → push v<V>, fs-gg-ui-template/v<V>, fs-gg-ui/v<V>"

# Track B — US3 visibility (org admin, no code) — runs concurrently:
Task: "Flip FS.GG.UI.Template visibility private → internal in org package settings"
Task: "Verify gh api ... --jq .visibility == internal"
```

---

## Implementation Strategy

### MVP scope (the coherent landing — NOT a single story)

Because FR-004 forbids a half-landing, the MVP is **US2 + US3 + US1 together**: a single `V` that is both Feature-217-bearing (no 127) and feed-readable (no 103), proven end-to-end (US1/T019).

1. Complete Phase 1: Setup (evidence dir + baseline).
2. Complete Phase 2: Foundational — including the **early live feed/visibility probe (T005)** that validates the two-gate hypotheses against the real feed before any tag.
3. Complete Phase 3 (US2 publish) **and** Phase 4 (US3 visibility) — in parallel.
4. Complete Phase 5 (US1) — **STOP and VALIDATE** the combined gate for one `V`.
5. Complete Phase 6 (US4) — registry/closure bookkeeping; confirm #32 unblocked.

### Notes

- This is a release/coordination feature: many tasks are **operator/CI/cross-repo** (tag push, org admin UI, `FS-GG/.github` PR), not local edits — disclosed as such with machine-checkable acceptance.
- The exact `V` literal is fixed by the merge bump, not hard-coded (only `> 0.1.52-preview.1` is mandated).
- All evidence is **live** cross-repo proof; green local packs do not substitute (plan standing assumption). Where a step cannot run live in-session, mark it `environment-limited` with a disclosed substitute (Principle V).
- Commit after each logical group; stop at the US1 checkpoint to validate the coherent landing before bookkeeping.

---

---

## Implementation status (2026-06-30) — ✅ COMPLETE (coherent landing achieved)

Both gates hold for the same `V = 0.1.53-preview.1` (FR-004, no half-landing), proven live.

- **US2 publish (#29):** feed serves the coherent set @ `0.1.53-preview.1`; `--productName` honored
  (no 127); release run green; dispatch fired → Templates#33 pin-bump PR opened. ✅
- **US3 visibility (#26):** whole set (template + 16 `FS.GG.UI.*` libs) flipped `private → public`
  (`internal` unavailable on the free org); consumer install+restore+build with no 103. ✅
- **US1 combined gate:** install + scaffold + restore + build the scaffolded product from the feed,
  all green. ✅
- **US4 registry/closure:** registry contract-change PR **FS-GG/.github#66**; **#29 + #26 closed**;
  board #29/#26 → **Done**; Templates#33 opened (downstream re-pin in flight).

**Residual (Templates-owned, linked not blocking):** the FS.GG.Templates#32 `29/29` composition
re-pin run lands when Templates merges #33; tracked in `readiness/downstream-2929.md`. The registry
PR #66 awaits review/merge.

See `readiness/README.md` for the full SC/FR/INV evidence map.

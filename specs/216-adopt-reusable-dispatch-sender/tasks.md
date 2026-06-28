---
description: "Task list for Adopt Reusable App-Token Dispatch-Sender"
---

# Tasks: Adopt Reusable App-Token Dispatch-Sender

**Input**: Design documents from `/specs/216-adopt-reusable-dispatch-sender/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Test tasks ARE included. The feature is CI/CD YAML + a Bash helper with no F# surface; the
quickstart explicitly defines a Layer 2 derivation harness and Constitution Principle V mandates
fail-before/pass-after evidence, so the derivation test harness is treated as a requested test.

**Organization**: Tasks are grouped by user story (P1→P3) so each is independently verifiable. Because
the entire feature is one workflow file + one helper + its harness, the three stories are tightly
coupled on `template-dispatch.yml`: US1 builds the working dispatching artifact (MVP), US2 is the
credential-hygiene concern (retire the stored PAT, App-token only), US3 is the gating/fork-safety
guarantee. Cross-story tasks therefore cannot run `[P]` against each other on that shared file.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (Setup, Foundational, Polish carry no story label)
- Exact file paths are included in each task

## Path Conventions

CI/CD automation + shell tooling at repository root (per plan.md "Project Structure"):
- `.github/workflows/template-dispatch.yml` — the sender (CHANGED)
- `.github/workflows/release.yml` — release pipeline (MUST stay byte-identical to `origin/main`)
- `scripts/derive-template-version.sh` (NEW), `scripts/test-derive-template-version.sh` (NEW)
- `scripts/template-released-dispatch.sh` + `scripts/test-template-released-dispatch.sh` (RETIRED)
- `specs/216-adopt-reusable-dispatch-sender/` — readiness evidence, contract, quickstart

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the lint/validation tooling and capture the no-regression baseline for this CI change.

> **⚠️ Adapted baseline (STANDING, do not narrow).** This feature has no F# test projects, so the
> `baseline-tests.fsx` runner does not apply. The honest CI analogue is: confirm `actionlint` lints
> both workflows clean today and that `release.yml` already matches `origin/main` — so any later
> diff/lint failure is unambiguously caused by this migration, not pre-existing drift.

- [X] T001 [P] Confirm tooling and record versions in `specs/216-adopt-reusable-dispatch-sender/readiness/tooling.md`: `actionlint --version` (must be the pinned `v1.7.7` at `~/.local/bin/actionlint`) and `gh --version` (Layer 3 only); do NOT install if already present
- [X] T002 Establish the no-regression baseline in `specs/216-adopt-reusable-dispatch-sender/readiness/baseline.md`: run `actionlint .github/workflows/template-dispatch.yml .github/workflows/release.yml` (record exit codes) and `git fetch origin main && git diff --stat origin/main -- .github/workflows/release.yml` (record the clean/no-diff starting state) so pre-existing state is known before any edit

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Pin the reusable-workflow consumption seam, verify the derivation behavior to be preserved, and file the gating cross-repo request — before rewriting anything.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

> **⚠️ Adapted "early live smoke" (STANDING, do not omit).** There is no running app; the live
> cross-repo POST cannot be exercised yet (it is Layer 3, disclosed-deferred on the org App secrets per
> FR-008). The honest analogue pulled forward here is T005: drive the **existing** Feature 214 sender
> through the boundary refs to observe and record the exact derivation/validation behavior the rewrite
> must preserve — treat the plan's "derivation logic is reusable" claim as unverified until observed.

- [X] T003 Pin the reusable-workflow contract: read `.github/workflows/dispatch-sender.yml` on `FS-GG/.github` `main`, capture its full 40-char commit SHA and its `workflow_call` inputs (`target-repo`/`event-type`/`version`/`payload`) + required `secrets` (`app-id`/`app-private-key`) into `specs/216-adopt-reusable-dispatch-sender/readiness/reusable-pin.md` (research R0/R2 — this is the consumption seam)
- [X] T004 Record the one genuine cross-repo unknown in `specs/216-adopt-reusable-dispatch-sender/readiness/reusable-pin.md`: the exact org secret names behind `app-id`/`app-private-key`; adopt the conventional working-assumption names `APP_ID` / `APP_PRIVATE_KEY` until confirmed, and note that `secrets: inherit` cannot bind the hyphenated callee ports (research R1)
- [X] T005 **Early behavior smoke (adapted)**: run the existing `scripts/template-released-dispatch.sh` against the boundary refs (`GITHUB_REF=refs/tags/fs-gg-ui-template/v0.1.52-preview.1` in `DRY_RUN`, plus non-tag / empty / malformed) and record the observed derive+validate behavior in `specs/216-adopt-reusable-dispatch-sender/readiness/baseline.md`, confirming exactly what the new helper must preserve (research R3); note the true live POST is Layer 3 (deferred, FR-008)
- [X] T006 File/track the gating cross-repo dependency EARLY (FR-008) using the `cross-repo-coordination` skill: a `cross-repo`/`cross-repo:request` issue against `FS-GG/.github#22`/`#21` for the exact `app-id`/`app-private-key` org secret names + confirmation the App is installed on FS.GG.Rendering and FS.GG.Templates; keep `FS-GG/FS.GG.Rendering#10` Blocked on the Coordination board (`Contract: fs-gg-ui-template`) until it lands. Filed here in Foundational (not Polish) because it is the sole unblocker of the deferred live-evidence task T012.

**Checkpoint**: Reusable seam pinned, secret-name unknown recorded and filed as a cross-repo request, derivation behavior observed — user-story work can begin.

---

## Phase 3: User Story 1 - Release automatically notifies Templates (Priority: P1) 🎯 MVP

**Goal**: A pushed `fs-gg-ui-template/v<version>` tag derives+validates the version and calls the org reusable App-token dispatch-sender, which POSTs the `fs-gg-ui-template-released` `repository_dispatch` to FS.GG.Templates (whose receiver opens/updates the pin-bump PR) — with no manual step.

**Independent Test**: Push a real (or pre-release) `fs-gg-ui-template/v*` tag in the canonical repo; observe the sender run succeed and a matching pin-bump PR appear in FS.GG.Templates — live evidence (run URL + PR URL), not a dry-run (Layer 3, gated on the T006 cross-repo confirmation).

### Tests for User Story 1 (derivation harness — Layer 2) ⚠️

> **NOTE: Write the harness FIRST and confirm it FAILS (script absent) before T008 implements it.**

- [X] T007 [P] [US1] Write the retargeted derivation harness `scripts/test-derive-template-version.sh` (from `scripts/test-template-released-dispatch.sh`) covering happy-path (`refs/tags/fs-gg-ui-template/v0.1.52-preview.1` → `version=0.1.52-preview.1`, exit 0) + non-tag + empty-after-strip + malformed edges (all non-zero, no version emitted); drop the Feature 214 credential-missing and `DRY_RUN` payload cases (research R3); confirm it fails initially because `scripts/derive-template-version.sh` does not exist yet

### Implementation for User Story 1

- [X] T008 [US1] Implement `scripts/derive-template-version.sh` by repurposing the derivation+validation half of `scripts/template-released-dispatch.sh`: strip `refs/tags/fs-gg-ui-template/v` from `$GITHUB_REF`, assert `^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$`, emit `version=<v>` to stdout AND append to `$GITHUB_OUTPUT`, and fail loudly (non-zero, nothing emitted) on non-tag/empty/malformed (FR-005); make T007 pass; `chmod +x`
- [X] T009 [US1] Rewrite `.github/workflows/template-dispatch.yml` into two jobs (research R4, FR-001/FR-003): `derive` (`if: github.repository == 'FS-GG/FS.GG.Rendering'`, `runs-on: ubuntu-latest`, `permissions: contents: read`, `actions/checkout@v4`, run `derive-template-version.sh`, expose `outputs.version`) and `dispatch` (`needs: derive`, `uses: FS-GG/.github/.github/workflows/dispatch-sender.yml@<full-sha-from-T003>` with `# main as of 2026-06-28`, `with: { target-repo: FS-GG/FS.GG.Templates, event-type: fs-gg-ui-template-released, version: ${{ needs.derive.outputs.version }} }`, and the explicit `secrets:` mapping `app-id`/`app-private-key`); keep `on: push: tags: ['fs-gg-ui-template/v*']` + inspection-only `workflow_dispatch`
- [X] T010 [US1] Substitute the resolved 40-char SHA (from T003) into the `<full-40-char-sha>` placeholder in `specs/216-adopt-reusable-dispatch-sender/contracts/template-released-dispatch.md` so the documented consumption interface matches the wired workflow
- [X] T011 [US1] Validate locally: `actionlint .github/workflows/template-dispatch.yml` (Layer 1, expect exit 0) and `scripts/test-derive-template-version.sh` (Layer 2, expect pass); record both as fail-before/pass-after evidence in `specs/216-adopt-reusable-dispatch-sender/readiness/baseline.md`
- [ ] T012 [US1] **DISCLOSED-DEFERRED live evidence (Layer 3, FR-009/SC-005 — do NOT fake green)**: once the org App secrets are confirmed (T006), push a real `fs-gg-ui-template/v*` tag, then capture the sender run URL + the FS.GG.Templates pin-bump PR URL into `specs/216-adopt-reusable-dispatch-sender/readiness/` AND confirm the PR appeared within 10 min of the tag push (SC-002); close `FS-GG/FS.GG.Rendering#10` and move the board item Blocked → Done; **blocked** on T006 landing

**Checkpoint**: The migrated workflow lints clean and the derivation harness is green; the dispatching path is wired and ready to go live the moment the org App secrets are confirmed (US1 complete pending only the deferred Layer 3 evidence).

---

## Phase 4: User Story 2 - Credential is minted per-run, never stored (Priority: P2)

**Goal**: The cross-repo send authenticates only with a run-time-minted org App installation token (inside the reusable workflow); no long-lived cross-repo PAT remains a Rendering secret.

**Independent Test**: Inspect `template-dispatch.yml` and (when available) a run log — confirm the token is obtained at run time from the App via the reusable workflow and that no `TEMPLATES_DISPATCH_TOKEN`-style stored secret is referenced anywhere in Rendering.

### Implementation for User Story 2

- [X] T013 [US2] Delete the retired bespoke-send artifacts `scripts/template-released-dispatch.sh` and `scripts/test-template-released-dispatch.sh` (the `gh api` POST + `GH_TOKEN`/`DRY_RUN` machinery is superseded by the reusable workflow; research R3)
- [X] T014 [US2] **Absence check** — assert no long-lived cross-repo secret remains anywhere: `grep -rn 'TEMPLATES_DISPATCH_TOKEN\|GH_TOKEN' .github scripts` returns nothing, confirming the stored-PAT path is fully gone and the `dispatch` job authenticates solely via the run-time-minted App token (FR-002/SC-003); record in `specs/216-adopt-reusable-dispatch-sender/readiness/baseline.md`
- [X] T015 [US2] **Binding-mechanism check (distinct from T014)** — confirm in `.github/workflows/template-dispatch.yml` that the credential is wired *correctly*: `secrets: inherit` is NOT used and the hyphenated callee ports `app-id`/`app-private-key` are mapped explicitly from the (working-assumption) repo secrets `APP_ID`/`APP_PRIVATE_KEY` (research R1), documenting why inherit cannot bind hyphenated ports

**Checkpoint**: No stored cross-repo credential remains; the only auth path is the reusable workflow's run-time App token.

---

## Phase 5: User Story 3 - Release gating and fork safety are preserved (Priority: P3)

**Goal**: The migration must not change package-release behavior (`release.yml` byte-identical, triggers disjoint) and forks must never run the sender or see the credential.

**Independent Test**: `git diff --exit-code origin/main -- .github/workflows/release.yml` shows no change; the template-tag trigger is disjoint from the `v*` release trigger; the canonical-repo guard gates the sender so forks skip it.

### Implementation for User Story 3

- [X] T016 [US3] Verify fork safety in `.github/workflows/template-dispatch.yml`: the `if: github.repository == 'FS-GG/FS.GG.Rendering'` guard is on the `derive` job and `dispatch` declares `needs: derive`, so on a fork `derive` is skipped and `dispatch` never starts (no send, no credential exposure) (FR-004/US3); record in `specs/216-adopt-reusable-dispatch-sender/readiness/baseline.md`
- [X] T017 [US3] Verify trigger disjointness: `template-dispatch.yml` triggers only on `fs-gg-ui-template/v*` (+ inspection-only `workflow_dispatch`), which cannot match `release.yml`'s bare `v*` glob (the slash guarantees disjointness) (FR-006, research R6)
- [X] T018 [US3] Run the `release.yml` byte-diff regression guard: `git fetch origin main && git diff --exit-code origin/main -- .github/workflows/release.yml && echo "release.yml unchanged ✓"` (expect exit 0, no diff) (SC-004/FR-006)

**Checkpoint**: Release gating is provably untouched and forks are provably inert.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Capture feedback and run the full quickstart. (The gating cross-repo request was pulled forward to Foundational T006.)

- [X] T019 [P] Capture per-phase feedback via the `fs-gg-feedback-capture` skill into `specs/216-adopt-reusable-dispatch-sender/feedback/` (process friction, generalizable-code candidates, severity)
- [X] T020 Run the full quickstart.md validation end-to-end (Layer 1 actionlint + Layer 2 harness + the `release.yml` byte-diff regression guard) and record the result plus the disclosed-deferred Layer 3 status in `specs/216-adopt-reusable-dispatch-sender/readiness/`

---

## Edge cases handled outside Rendering (documented, not gaps)

Two spec edge cases have **no sender-side task by design** — they are owned by the callee or receiver,
not by anything Rendering can assert locally:

- **Receiver unavailable / rejects the dispatch → run reports failure**: owned by the `FS-GG/.github`
  reusable workflow (it performs the POST and surfaces a non-2xx as a failed run). Observed, if at all,
  via the T012 live run — not a separate Rendering task.
- **Re-pushed / duplicate tag → idempotent pin-bump**: owned by the FS.GG.Templates receiver
  (`upstream-bump.yml` opens-or-updates a single PR per version). Out of scope for the sender (research R5).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories (the pinned SHA from T003 is required by T009; the cross-repo request T006 unblocks T012).
- **User Stories (Phase 3–5)**: All depend on Foundational. Because they share `template-dispatch.yml`, they are best done in priority order (US1 builds the artifact; US2/US3 are largely verification layered on it) rather than fully in parallel.
- **Polish (Phase 6)**: Depends on the user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Needs Foundational (T003 SHA). Produces the working dispatching workflow + helper + harness (MVP). T012 is deferred on T006 landing.
- **US2 (P2)**: Builds on US1's `template-dispatch.yml` (verifies/cleans the credential path; deletes retired scripts). Independently testable via grep + config inspection.
- **US3 (P3)**: Builds on US1's `template-dispatch.yml` (verifies guard/trigger/`release.yml`). Independently testable via the byte-diff guard.

### Within Each User Story

- US1: harness T007 (fails) → helper T008 (passes) → workflow T009 → doc-SHA T010 → local validation T011 → deferred live T012.
- US2: delete retired artifacts T013 → absence check T014 → binding-mechanism check T015.
- US3: fork-safety T016 → trigger-disjointness T017 → `release.yml` byte-diff T018.

### Parallel Opportunities

- T001 (tooling) is `[P]` against the rest of Setup.
- T007 (`[P]`) is its own new file and can be written while Foundational notes are finalized.
- T019 (`[P]`) touches only `specs/` — independent of the workflow edits.
- Cross-story tasks are NOT `[P]` against each other: US1/US2/US3 all touch `.github/workflows/template-dispatch.yml`.

---

## Parallel Example: setup & foundational

```bash
# Setup: confirm tooling while the baseline is captured
Task: "T001 Confirm actionlint v1.7.7 + gh and record versions in readiness/tooling.md"

# Foundational: the cross-repo request touches only trackers/specs — fileable alongside the pin notes
Task: "T006 File the cross-repo:request for the app-id/app-private-key secret names (.github#22/#21)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (tooling + baseline).
2. Complete Phase 2: Foundational — pin the reusable SHA, record the secret-name unknown, run the **early behavior smoke** (T005) against the existing sender, and file the cross-repo request (T006) so the deferred live check has its unblocker in flight.
3. Complete Phase 3: User Story 1 — harness → helper → two-job workflow → local Layer 1/2 validation.
4. **STOP and VALIDATE**: actionlint clean + harness green + (deferred) live evidence captured the moment the org App secrets land.
5. The MVP is the working, lint-and-harness-proven dispatching workflow, ready to go green on the first authenticated release.

### Incremental Delivery

1. Setup + Foundational → seam pinned, behavior understood, cross-repo request filed.
2. US1 → migrated sender wired & locally proven (MVP); Layer 3 goes green when T006 confirms the secrets.
3. US2 → retire the stored-PAT path; prove App-token-only.
4. US3 → prove `release.yml` byte-unchanged + fork-inert.
5. Polish → full quickstart re-run; close `#10` on live evidence when unblocked.

---

## Notes

- `[P]` = different files, no dependencies on incomplete tasks.
- `[Story]` label maps each task to US1/US2/US3 for traceability; Setup/Foundational/Polish carry none.
- The live Layer 3 evidence (T012) is **disclosed-deferred** on the org App secrets — never fake it green (FR-008/FR-009).
- `release.yml` is byte-frozen — this feature must touch only `template-dispatch.yml` + the `scripts/` helper/harness.
- Commit after each task or logical group.

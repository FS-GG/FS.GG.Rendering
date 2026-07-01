---
description: "Task list for feature 229 — drop the .claude/skills/ UI-skill mirror (ADR-0011)"
---

# Tasks: fs-gg-ui template emits UI skills to the provider-owned tree only

**Input**: Design documents from `/specs/229-drop-claude-skills-mirror/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: This feature's verification *is* test-logic correction. The existing Feature 204/219 Expecto gates currently assert the **Feature 228** shape (`.claude/skills/` product mirror spec-kit-gated), so they are part of the fix surface (not optional). Test tasks below are therefore REQUIRED, per plan.md and `contracts/gate-assertion-contract.md`.

**Change tier**: Tier 2 (content/config + test-logic) — no `src/**`, no `.fsi`, no dependency. **Does** include a template **version bump / re-release** (FR-008), unlike Feature 228.

**Organization**: Tasks grouped by user story. Because the post-228 gates assert the spec-kit-gated shape, the template edit (US1) and the gate corrections (US3) must land in the **same commit** to keep the build green — see Dependencies.

> **⚠️ Scope revised during /implement (full confinement).** Tasks below were authored for the initial
> "delete the 9 per-profile `.claude/skills/` product-skill sources" scope. Live evidence (T005/T009) showed
> that left `spec-kit`'s `.claude/skills/` inconsistent (7/8 UI skills via the base mirror), so — per the
> maintainer's decision — T007 was **expanded** to remove **every** `.claude/skills/…` source (also the base
> `.agents/skills/`→`.claude/skills/` mirror and the sample/feedback rows), keeping the base `.claude/`
> workspace (`fs-gg-project`). US3 gained a universal "no source targets `.claude/skills/`" guard, and
> `claudeProductSkillCount` excludes `fs-gg-project`. See `spec.md` FR-007, `research.md` R2, and
> `readiness/leak-surface-map.md` for the revised scope; the readiness evidence reflects full confinement.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 / US2 / US3 (maps to spec.md user stories)
- Exact file paths included in every task

## Path Conventions

Single-repo template/package change. Touch-points (verified in plan.md):
`.template.config/template.json`, `.template.package/FS.GG.UI.Template.fsproj` (version bump),
`tests/Package.Tests/Feature204LifecycleTemplateTests.fs`,
`tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs`, `scripts/validate-lifecycle-template.fsx`,
`specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md` (regenerated),
evidence under `specs/229-drop-claude-skills-mirror/readiness/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the no-regression baseline before touching anything.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Run **every** test project — the solution
> deliberately omits `tests/Package.Tests` (release-only, owns the surface gate) and `samples/**/*.Tests`
> (feed consumers), which is exactly where the Feature 175 surprises hid. Use the discovery-based runner
> so nothing silently drops out.

- [X] T001 Confirm feature prerequisites: on branch `229-drop-claude-skills-mirror`, `.NET net10.0` SDK present, and the `fs-gg-ui` template is installed **from the working tree** (pack `.template.package` + `dotnet new install`, per the "Template live-test workflow" memory — the installed package, not the working tree, is what `dotnet new` uses). Record versions + the installed template version in `specs/229-drop-claude-skills-mirror/readiness/env.md`
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/229-drop-claude-skills-mirror/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Map the exact leak surface and **reproduce the residual leak on the live scaffold** before any edit. After Feature 228 the leak survives only under **`spec-kit`** (sdd/none are already clean), so the pre-fix repro is a `spec-kit` scaffold, not `sdd`.

**⚠️ CRITICAL**: No user-story work begins until the residual spec-kit leak is confirmed on the current template.

> **⚠️ Early live smoke run (STANDING, do not omit).** The "app to drive" here is the **scaffold path**.
> Drive the real scaffold under `--lifecycle spec-kit` on the *current (post-228)* template and observe
> the `.claude/skills/fs-gg-*` UI mirror still present, before any `template.json` edit. Treat the plan's
> root-cause hypothesis as unverified until the scaffold has actually been run and the mirrored files observed.

- [X] T003 Build the leak-surface map: enumerate the 9 per-profile `.claude/skills/fs-gg-*/` product-skill sources in `.template.config/template.json` and confirm each is currently gated `(<profile predicate>) && lifecycle == "spec-kit"` (the Feature 228 state); confirm the 9 matching `.agents/skills/fs-gg-*/` siblings, the base `.agents/skills/`→`.claude/skills/` mirror (source[5]), and the sample/feedback `.claude/skills/` sources are OUT OF SCOPE. Record the source→target×condition table in `specs/229-drop-claude-skills-mirror/readiness/leak-surface-map.md` (cross-checks data-model.md)
- [X] T004 Static before-scan: run the `template.json` scan from quickstart §1 and confirm it prints `9` product-skill sources targeting `.claude/skills/` (all spec-kit-gated) and `9` targeting `.agents/skills/`; save output to `specs/229-drop-claude-skills-mirror/readiness/leak-before.md`
- [X] T005 **Early live smoke run (residual spec-kit leak repro)**: `dotnet new fs-gg-ui -o <scratch> --profile game --lifecycle spec-kit`, then confirm `.claude/skills/fs-gg-{scene,symbology,skiaviewer,elmish,keyboard-input,ui-widgets,styling,layout}` are PRESENT (8 for `game`) — the mirror ADR-0011 §3 requires the provider to stop writing. Also confirm `--lifecycle sdd` is already clean (0), i.e. the residual is spec-kit-only. Append observed paths to `specs/229-drop-claude-skills-mirror/readiness/leak-before.md`
- [X] T006 Confirm the post-228 gates encode the spec-kit-gated shape (they must go red after the template edit): run `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "Feature204|Feature219"` and record which assertions currently assert the mirror (Feature 219 `sources.Length >= 18` + "each id emits under BOTH surfaces" + `.claude` spec-kit-gated; Feature 204 `workspace >= 15` + `gated-condition:` naming the mirror) in `specs/229-drop-claude-skills-mirror/readiness/pre-fix-gate-state.md`

**Checkpoint**: Residual spec-kit leak reproduced live, surface map confirmed to exactly 9 sources, post-228 gate assertions catalogued. Fix work can begin.

---

## Phase 3: User Story 1 - Provider writes only its own skill tree, under every lifecycle (Priority: P1) 🎯 MVP

**Goal**: The `fs-gg-ui` template writes zero UI skills into `.claude/skills/` under **every** lifecycle (spec-kit included), so an SDD-orchestrated scaffold returns success with no `providerWroteSddTree` and the orchestrator (SDD#57) owns the three-root union mirror.

**Independent Test**: Scaffold `app`/`game` under `sdd` and `spec-kit`; `.claude/skills/` and `.codex/skills/` contain zero template-authored `fs-gg-*` UI skills, while `.agents/skills/` holds the full profile set.

> **Build note**: deleting the 9 `.claude/skills/` sources turns the *post-228* Feature 204/219 gates red.
> Those gate corrections live in US3 (T013–T015) and MUST be applied in the same commit as T007 to keep
> the build green. US1 is the product value; US3 is the guard that keeps it — but they are co-committed.

### Implementation for User Story 1

- [X] T007 [US1] Apply the core fix in `.template.config/template.json`: **delete** the 9 per-profile `.claude/skills/fs-gg-*/` product-skill `sources` rows (targets `fs-gg-scene`, `fs-gg-symbology`, `fs-gg-skiaviewer`, `fs-gg-elmish`, `fs-gg-keyboard-input`, `fs-gg-ui-widgets`, `fs-gg-styling`, `fs-gg-layout`, `fs-gg-testing`). Leave the 9 `.agents/skills/fs-gg-*/` siblings, the base `.agents/skills/`→`.claude/skills/` mirror, and the sample/feedback `.claude/skills/` sources untouched; add no `.codex/skills/` source (data-model.md table). Result: every `template/product-skills/` source targets `.agents/skills/` only
- [X] T008 [US1] Static after-scan: re-run the quickstart §1 `template.json` scan and confirm it now prints `0` product-skill sources targeting `.claude/skills/` and still `9` targeting `.agents/skills/`; append to `specs/229-drop-claude-skills-mirror/readiness/leak-before.md` (before/after in one file)
- [X] T009 [US1] Live after-observation (`spec-kit` + `sdd`): re-run `FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx --emit-report`; confirm `count(.claude/skills/fs-gg-*) == 0` under `spec-kit`, `sdd`, and `none`, and `set(.agents/skills/fs-gg-*) == UI(profile)` for `app` and `game`. Record in `specs/229-drop-claude-skills-mirror/readiness/fixed-scaffold-speckit.md` and `fixed-scaffold-sdd.md` (SC-002)
- [X] T010 [US1] End-to-end SDD-orchestrated acceptance (SC-001/SC-004/FR-004/FR-005): in a scratch dir run `fsgg-sdd scaffold --provider rendering --param productName=Spaceinvaders --profile game` (or `new-sdd-fullstack ./SpaceInvaders Spaceinvaders`); confirm `outcome: success`, no `scaffold.providerWroteSddTree`, and the full-stack script proceeds past scaffold into governance-overlay + `doctor`. Record in `specs/229-drop-claude-skills-mirror/readiness/success-criteria.md`. If `fsgg-sdd` is unavailable or predates the SDD#57 fan-out (publish-before-flip), mark `environment-limited` with the disclosed substitute (the T009 live scaffold showing `.claude/skills/` UI = 0 + boundary-rule reasoning), noting the clean end-to-end run requires the orchestrator half published

**Checkpoint**: The provider writes UI skills only to `.agents/skills/`, observed on the real scaffold under both `spec-kit` and `sdd`. MVP deliverable complete.

---

## Phase 4: User Story 2 - The provider-owned `.agents/skills/` tree still carries the full UI-skill set (Priority: P2)

**Goal**: `.agents/skills/` is never reduced under any lifecycle (byte-identical to baseline); `sdd` and `none` produce identical skill-tree output; no `.claude/skills/fs-gg-*` UI skill is authored under any lifecycle.

**Independent Test**: Across all three lifecycles confirm `.agents/skills/` holds exactly `UI(profile)` (identical set to the pre-change baseline); confirm zero template-authored `.claude/skills/fs-gg-*` UI skills.

### Implementation for User Story 2

- [X] T011 [US2] Live observation (`none` ≡ `sdd`, FR-003 / SC-003): from the T009 report run, confirm the `none/app` and `none/game` scaffolds produce `count(.claude/skills/fs-gg-*) == 0` and `set(.agents/skills/fs-gg-*) == UI(profile)`, byte-identical skill-tree to the `sdd` column. Record in `specs/229-drop-claude-skills-mirror/readiness/fixed-scaffold-none.md`
- [X] T012 [US2] Provider-tree-intact (SC-003) + spec-kit reversal disclosed: confirm `.agents/skills/` == `UI(profile)` for `app`/`game` under all three lifecycles (identical to the pre-change baseline captured in T004/leak-before), and that `spec-kit` now emits `UI(profile)` into `.agents/skills/` ONLY (the `.claude/skills/` UI mirror gone — the intended Feature 228 FR-003 reversal). Note that explicit `spec-kit` still equals the no-flag default (Feature 204 GV-3, verified in T017). Record in `specs/229-drop-claude-skills-mirror/readiness/agents-tree-intact.md`

**Checkpoint**: The provider tree is proven unmoved across lifecycles; `sdd ≡ none`; the spec-kit UI-mirror removal is the only skill-set change and is disclosed.

---

## Phase 5: User Story 3 - The corrected emission gates prove the provider boundary holds (Priority: P3)

**Goal**: Repo-owned gates fail on the post-228 template and pass on the fixed one, encoding the ADR-0011 invariant (no product-skill source targets `.claude/skills/`/`.codex/skills/`), so a future added skill cannot silently re-leak.

**Independent Test**: The corrected gates fail on the post-228 template (T006 state) and pass on the fixed template, naming any offending `.claude/skills/` source.

> These gate corrections are co-committed with T007 (see US1 build note). They encode the corrected
> invariant per `contracts/gate-assertion-contract.md`.

### Implementation for User Story 3

- [X] T013 [P] [US3] Correct Feature 219 in `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs` (G-EMIT surface test, ~L144-164): change `sources.Length >= 18` → `>= 9`; assert every product-skill source targets `.agents/skills/` (and is not spec-kit-gated, keeping the profile-predicate check) and that **no** source targets `.claude/skills/`/`.codex/skills/`; replace the "each id emits under BOTH surfaces" block with "each distinct id emits under `.agents/skills/` and no id emits under `.claude/skills/`". Rename the test + update the comment to cite ADR-0011 §3/§4
- [X] T014 [P] [US3] Correct Feature 204 in `tests/Package.Tests/Feature204LifecycleTemplateTests.fs`: (a) `gatedSourceAudit` (~L102-144) — a `template/product-skills/` source whose target is NOT under `.agents/skills/` is a hard **violation** (ADR-0011 §3), drop the "`.claude/skills/` product mirror → workspace" classifier note; (b) GV-2 floors (~L169-171) workspace `>=15`→`>=6` (framework `>=9`, product `>=3` unchanged); (c) update the expected `gated-condition:` report string to the corrected wording; (d) refresh GV-4/GV-5 comments from "Feature 228" to ADR-0011/#42 (the `claude-product-skills=0` assertions stay)
- [X] T015 [US3] Update `scripts/validate-lifecycle-template.fsx`: mirror the Feature 204 classifier correction in the gating audit (~L149-176) and change `workspaceChecked >= 15` → `>= 6`; update the emitted `gated-condition:` line (~L438) to match the Feature 204 expected string byte-for-byte; add a `SpecKitClaudeProductSkills` verdict field + a `spec-kit/<profile>: claude-product-skills=0` report line, and assert `claudeProductSkillCount def = 0` in `validateProfileLive` (reuse the existing `claudeProductSkillCount`). Keep `.agents/skills/` `framework-skills-present=ok` and the `sdd`/`none` `claude-product-skills=0` lines
- [X] T016 [US3] Regenerate the shared readiness artifact both gates read: `FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx --emit-report` → `specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md` (regenerated by the fsx, not hand-edited)
- [X] T017 [US3] Run the corrected gates: `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "Feature204|Feature219"` — expect Feature 204 green (`framework=9`, `workspace>=6`, 0 violations, spec-kit/sdd/none `claude-product-skills=0`, sdd/none `framework-skills-present=ok`, spec-kit `diff-vs-today=none` GV-3 unchanged) and Feature 219 green (`sources.Length>=9`, all `.agents/skills/`, none `.claude/skills/`). Capture transcript to `specs/229-drop-claude-skills-mirror/readiness/gate-transcripts.md` (SC-005)

**Checkpoint**: The guard is live and green on the fix; it would have caught Feature 227's `.claude/skills/` addition.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Version bump / re-release, full regression confirmation, evidence completion, quickstart validation.

- [X] T018 [US1] Re-release prep (FR-008 / SC-006): bump the template package version in `.template.package/FS.GG.UI.Template.fsproj` `0.1.58-preview.1` → `0.1.59-preview.1` (coherent-set bump; ships Feature 228 + 229 together). Do NOT hand-pack here if `/speckit-merge` owns the bump+pack — if so, record the intended target version and let merge perform it; otherwise pack to `~/.local/share/nuget-local/`. Record the decision + version in `specs/229-drop-claude-skills-mirror/readiness/rerelease.md`
- [X] T019 Re-run the full baseline runner (`dotnet fsi scripts/baseline-tests.fsx --out specs/229-drop-claude-skills-mirror/readiness/baseline-after.md`) and diff vs T002 — confirm no new reds outside the intended Feature 204/219 changes (Constitution V: no assertion weakened to green a build)
- [X] T020 Walk the whole `quickstart.md` recipe end-to-end (steps 1→4) and confirm every "Expected outcome" holds; record any deviation
- [X] T021 [P] Complete the success-criteria evidence index in `specs/229-drop-claude-skills-mirror/readiness/success-criteria.md`, mapping SC-001…SC-006 to their evidence files
- [X] T022 Confirm out-of-scope invariants held (FR-007/FR-010): run `git diff --name-only main...HEAD` and confirm the change touches **only** `.template.config/template.json`, `.template.package/FS.GG.UI.Template.fsproj` (version only), the two `tests/Package.Tests/Feature20{4,19}*.fs` gates, `scripts/validate-lifecycle-template.fsx`, and `specs/**` — **no `src/**` file and no `.fsi`**. Confirm `.codex/skills/` still never written; the base `.agents/`→`.claude/` mirror + sample/feedback `.claude/skills/` sources unchanged; Feature 224/225 gates untouched and green; `docs/reports/skills-parity.md` NOT regenerated (no skill added/removed). Note the `git diff` evidence in `specs/229-drop-claude-skills-mirror/readiness/non-goals-held.md`
- [X] T023 File/track the cross-repo follow-ons (publish-before-flip, out of this repo's code): note in `readiness/rerelease.md` the two downstream steps that close #42's remaining DoD — (1) registry `fs-gg-ui-template` flip in `FS-GG/.github` to the re-released version; (2) `FS.GG.Templates` `providers/rendering.providers.yml` re-pin + composition gate green — and confirm they are captured on the Coordination board / via the `cross-repo-coordination` protocol. Do not attempt them here

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: depends on Setup — BLOCKS all user stories (residual spec-kit leak must be reproduced live first).
- **User Stories (Phase 3-5)**: all depend on Foundational.
- **Polish (Phase 6)**: depends on all user stories.

### Critical co-commit constraint

Deleting the 9 `.claude/skills/` sources **T007 (US1)** turns the post-228 gates red. The gate corrections
**T013–T015 (US3)** must be committed together with T007. Sequence within the fix commit:
T013 + T014 (parallel, different test files) → T007 → T015 → T016 → T017.

### User Story Dependencies

- **US1 (P1)**: after Foundational. Product value. Co-commits with US3 gate edits (build constraint).
- **US2 (P2)**: after US1 (reads the same live report run T009). Independently testable via the `none`/`spec-kit`/`agents` observations.
- **US3 (P3)**: after Foundational; edits co-commit with T007. Independently testable: gate fails pre-fix, passes post-fix.

### Within stories

- T013 and T014 are [P] (different files).
- T009 produces the live report that T011/T012 read (do T009 before T011/T012).
- T016 must run after T015 (script change) and before T017 (gates read the regenerated artifact).
- T018 (version bump) is independent of the gate/observation flow; place it before merge/pack.

### Parallel Opportunities

- T013 and T014 can run in parallel (`Feature219…fs` vs `Feature204…fs`).
- T021 can run in parallel with other polish tasks.
- Evidence-writing tasks that touch distinct readiness files are independent once their observation exists.

---

## Parallel Example: US3 gate corrections

```bash
# Different test files — run together:
Task: "Correct Feature 219 surface assertion (.agents-only, no .claude) in tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs"
Task: "Correct Feature 204 classifier + GV-2 floors + gated-condition string in tests/Package.Tests/Feature204LifecycleTemplateTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 Setup (baseline, template installed from working tree).
2. Phase 2 Foundational — **reproduce the residual spec-kit leak live** before editing (T005).
3. Phase 3 US1 — delete the 9 `.claude/skills/` sources + observe the scaffold clean under spec-kit/sdd. Co-commit the US3 gate edits (T013–T015) so the build stays green.
4. **STOP and VALIDATE**: `.claude/skills/` UI = 0 under every lifecycle, `.agents/skills/` = `UI(profile)`.

### Incremental Delivery

1. Setup + Foundational → residual spec-kit leak confirmed live.
2. US1 (+ co-committed US3 gate edits) → provider confined to `.agents/skills/` → MVP.
3. US2 → agents/none/spec-kit invariants proven; `sdd ≡ none`.
4. US3 → guard green and catalogued as the regression backstop.
5. Polish → version bump/re-release, full baseline re-run, quickstart walk, evidence index, cross-repo follow-on tracking.

---

## Notes

- [P] = different files, no dependencies.
- Tier 2: no `src/**`, no `.fsi`, no dependency. **Includes** a version bump / re-release (FR-008) — the one difference from Feature 228's delivery shape.
- FR-009: delivery confirmed by an **observed scaffold** (T005 before / T009–T010 after), not a green test alone.
- ADR-0011 §3/§4 is the governing decision; this reverses Feature 228's spec-kit-keeps-the-mirror invariant on purpose.
- Do NOT edit Feature 224/225 gates or regenerate `docs/reports/skills-parity.md` (no skill added/removed).
- The `.agents/skills/` provider surface must never shrink — every observation checks it, and no assertion over it is loosened.
- The registry flip (`FS-GG/.github`) and `FS.GG.Templates` re-pin are cross-repo follow-ons (T023), not this repo's implement scope.

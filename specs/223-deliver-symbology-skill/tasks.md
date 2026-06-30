---
description: "Task list for delivering the symbology product skill to consumers"
---

# Tasks: Deliver the Symbology Product Skill to Consumers

**Input**: Design documents from `/specs/223-deliver-symbology-skill/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ (all present)

**Tests**: REQUIRED. The spec mandates an emit test (FR-005/SC-001), a parity blind-spot test
(FR-004/SC-003), and a regression guard (FR-006/SC-004). Test tasks are first-class here and must
follow fail-before / pass-after (Constitution Principle V).

**Organization**: Tasks are grouped by user story. US1 (P1) is the MVP; US2 and US3 (both P2)
harden reachability and parity honesty.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1, US2, US3 (Setup/Foundational/Polish carry no story label)
- Exact file paths are included in every task.

## Profile decision (FR-002, resolved per research R2)

Symbology ships to the **`fs-gg-scene` profile set**: `app`, `headless-scene`, `governed`,
`sample-pack`, `game` — same predicate as `fs-gg-scene`, **no** `lifecycle` clause. This is the
documented, asserted decision; `app` is **included**.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Get the src+tests graph restored/building and capture the full pre-change red/green set.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Run **every** test project via the
> discovery-based runner so `tests/Package.Tests` (release-only, owns the public-surface gate) and
> the `samples/**/*.Tests` projects are not silently dropped. Pre-existing reds get flagged here,
> not mistaken for regressions at merge.

- [X] T001 Restore/build the src+tests graph; if the stale FSharp.Core lockfile hash blocks restore, apply the documented NU1403 workaround (see memory `nu1403-fsharp-core-lockfile-workaround`) before continuing
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/223-deliver-symbology-skill/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the exact pre-edit source lines each story touches, and verify the plan's
central read-derived finding (R1: GV-3 stays green) against the **real** validator before any fix.

**⚠️ CRITICAL**: No user-story work begins until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** The plan's de-risking claim — that wiring the
> symbology source leaves the explicit-`spec-kit` == default invariant byte-identical — is **read-
> derived, unverified until the validator is run**. T004 drives the real env-gated lifecycle
> validator and records live evidence BEFORE any test edit hardens. If GV-3 is not `none`, R1 is
> wrong — stop and reassess.

- [X] T003 Build the root-cause / touch-point map into `specs/223-deliver-symbology-skill/readiness/touch-points.md`: record the exact pre-edit lines each story edits — the OR'd satisfaction in `tools/Rendering.Harness/SkillParity.fs:842-847` (`exposedAsAlias` at 842-844, the `if … || … || …` at 847), the `fs-gg-scene` source pair shape at `.template.config/template.json:253-262` (condition at :254/:258), `expectedFrameworkSkills` at `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs:42-46`, G-NODANGLE-SYMB at `:199-211`, and the report token at `scripts/validate-lifecycle-template.fsx:418`. (Line numbers are vs. `main` at spec time; re-confirm here — they are the authoritative capture all later tasks rely on.)
- [X] T004 **Early live smoke run (pre-wiring)**: `FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx`; record into `specs/223-deliver-symbology-skill/readiness/early-live-run.md` that GV-3 is `diff-vs-today=none` for every profile and that the current token reads `symbology: not-vendored` — the live baseline that confirms R1's invariant holds before the fix (quickstart Scenario 1)

**Checkpoint**: Touch points captured and GV-3 neutrality confirmed live — story work can begin.

---

## Phase 3: User Story 1 - Symbology skill ships in the generated product (Priority: P1) 🎯 MVP

**Goal**: The symbology product skill is sourced into the ship list and emits to both consumer skill
surfaces for every scene-bearing profile (including `game`), under every lifecycle.

**Independent Test**: `dotnet new fs-gg-ui --profile game -o /tmp/qs-game` produces
`.claude/skills/fs-gg-symbology/SKILL.md` + `.agents/skills/fs-gg-symbology/SKILL.md` containing the
~12788-byte product skill (not the 506B stub); the env-free G-EMIT matrix re-derives the same.

### Implementation for User Story 1

- [X] T005 [US1] Add the symbology product-skill source pair to `.template.config/template.json` — two entries, condition `(profile == "app" || profile == "headless-scene" || profile == "governed" || profile == "sample-pack" || profile == "game")`, **no** `lifecycle` clause, `source: template/product-skills/fs-gg-symbology/`, targets `.agents/skills/fs-gg-symbology/` and `.claude/skills/fs-gg-symbology/`, mirroring the `fs-gg-scene` pair (per `contracts/template-ship-list.md`)
- [X] T006 [US1] **Post-wiring live confirmation (before test edits harden)**: re-run `FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx`; append to `specs/223-deliver-symbology-skill/readiness/early-live-run.md` that GV-3 stays `diff-vs-today=none` for every profile, `framework-skills-present=ok` under `sdd`/`none`, and the 12788B symbology `SKILL.md` now emits to both surfaces
- [X] T007 [US1] Flip the validator report token `symbology: not-vendored` → `symbology: vendored` at `scripts/validate-lifecycle-template.fsx:418` (and the unwired-set comment to `{ }`)
- [X] T008 [US1] Update the G-EMIT matrix in `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs`: add `fs-gg-symbology` to every shipping profile row of `expectedFrameworkSkills` (:42-46) and add the new `game` row (scene, skiaviewer, elmish, keyboard-input, ui-widgets, symbology) — fail-before / pass-after. **Not `[P]`**: shares `Feature219EmitFrameworkSkillsTests.fs` with T009 and T016; sequence T008 → T009, and run T016 (comment reversal) after both
- [X] T009 [US1] Update `G-NODANGLE-SYMB` in `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs:199-211`: unwired-product-skill set `{ fs-gg-symbology }` → `{ }` (empty) and the report-token assertion `symbology: not-vendored` → `symbology: vendored`; bump the `sources.Length` comment at `:131` to note the new count (14)
- [X] T010 [US1] In `tests/Package.Tests/Feature204LifecycleTemplateTests.fs`, verify the `gatedSourceAudit` framework-skill/product `>=` thresholds still pass at the new count and bump their comments; confirm GV-3 (`:172`) is untouched

**Checkpoint**: A scaffolded `game` (and `app`) product contains the real symbology skill on both surfaces under every lifecycle; emit matrix + validator record `vendored`. MVP delivered.

---

## Phase 4: User Story 2 - Symbology is reachable through the standard consumer wrapper (Priority: P2)

**Goal**: `fs-gg-product-symbology` exists on both wrapper surfaces and routes to the symbology
product-skill content, coexisting with the bare framework `fs-gg-symbology` with no collision.

**Independent Test**: `ls .claude/skills/ .agents/skills/ | grep fs-gg-product-symbology` shows both;
each `SKILL.md` points at `../../../template/product-skills/fs-gg-symbology/SKILL.md`.

### Implementation for User Story 2

- [X] T011 [P] [US2] Create `.claude/skills/fs-gg-product-symbology/SKILL.md` — frontmatter `name: fs-gg-product-symbology`, the product-facing description, "Claude-active wrapper …" preamble, and the relative pointer `../../../template/product-skills/fs-gg-symbology/SKILL.md`; mirror `fs-gg-product-scene` exactly (per `contracts/consumer-wrapper.md`)
- [X] T012 [P] [US2] Create `.agents/skills/fs-gg-product-symbology/SKILL.md` — identical shape with the "Codex-active wrapper …" per-surface wording (per `contracts/consumer-wrapper.md`)

**Checkpoint**: 7 of 7 product skills reachable via their `fs-gg-product-*` wrappers; both surfaces resolve with no name collision against the framework wrapper.

---

## Phase 5: User Story 3 - Parity harness fails honestly when a product wrapper is missing (Priority: P2)

**Goal**: A product skill whose product-alias wrapper is absent yields a `MissingWrapper` finding
even when a bare same-named framework wrapper exists — closing the blind spot — with no regression
for the six already-delivered product skills or the ant/package/fixture self-exposure paths.

**Independent Test**: Remove `fs-gg-product-symbology` and run `dotnet run --project
tools/Rendering.Harness -- skill-parity` ⇒ a `MissingWrapper` finding for `fs-gg-symbology`; restore
⇒ green.

### Tests for User Story 3 (write FIRST — fail before the fix) ⚠️

- [X] T013 [P] [US3] Add `tests/Rendering.Harness.Tests/Feature223SymbologyParityTests.fs` (GL-free, fixture-built) covering: (a) blind-spot — a `template/product-skills` canonical with the **bare** wrapper present but the **product alias absent** ⇒ `MissingWrapper` (must FAIL before T014); (b) alias present ⇒ no finding; (c) regression — fixture mirroring the six (alias present) and a non-product canonical satisfied by its bare name ⇒ no findings; register the file in `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`

### Implementation for User Story 3

- [X] T014 [US3] Narrow `missingWrapperFindings` in `tools/Rendering.Harness/SkillParity.fs:842-847`: hoist a single `let isProductSkill = entry.Path.Contains("template/product-skills", StringComparison.OrdinalIgnoreCase)` and **reuse it** in `exposedAsAlias` (which already recomputes the same `template/product-skills` check at :843 — collapse that to `exposedAsAlias = isProductSkill && names.Contains productAliasName`, no duplicate expression); gate the bare-name match with `not isProductSkill` (`canonicalSatisfies = (not isProductSkill) && names.Contains(canonicalName)`) so the `if` at :847 reads `if canonicalSatisfies || exposedAsAlias || antCanonicalSelfExposed`; leave `antCanonicalSelfExposed` unchanged (per `contracts/parity-missing-wrapper.md`)
- [X] T015 [US3] Run `dotnet test tests/Rendering.Harness.Tests` — confirm Feature223 (SC-003) and the Feature168 parity regression fixtures (SC-004/FR-006) are green with all seven product wrappers present

**Checkpoint**: All three stories independently functional; parity is honest and regression-clean.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T016 Reverse the narrative records: the "symbology not-vendored (research R5)" comments at `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs:38-41` → "vendored (Feature 223)", and align the `data-model.md` narrative. **Not `[P]`**: edits the same `Feature219EmitFrameworkSkillsTests.fs` as T008/T009 — run after both have landed
- [X] T017 Run quickstart Scenarios 2–6 (`specs/223-deliver-symbology-skill/quickstart.md`) and re-run `dotnet fsi scripts/baseline-tests.fsx` to confirm zero regressions against the T002 baseline
- [ ] T018 Cross-repo delivery (FR-007/FR-008) via the `cross-repo-coordination` skill: update the `fs-gg-ui-template` contract entry in `FS-GG/.github` (`registry/dependencies.yml` + `docs/registry/compatibility.md`) to record symbology as vendored, carry the change on the next `fs-gg-ui-template` republish, and close/Done Coordination board item **#35** with its acceptance checklist satisfied

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: depends on Setup. **Blocks all user stories.** T004 (live GV-3 check) gates the whole feature — if GV-3 ≠ `none`, stop.
- **US1 (Phase 3)**: depends on Foundational. The MVP — delivers the core value alone.
- **US2 (Phase 4)**: depends on Foundational. Independently testable; pairs with US1 for end-to-end reachability but its wrapper files stand alone.
- **US3 (Phase 5)**: depends on Foundational. Independently testable via fixtures. **Note**: SC-004's "7 of 7 wrappers present ⇒ zero findings" full-run regression is only fully demonstrable once US2's `fs-gg-product-symbology` wrapper exists — but the focused Feature223 fixture test (T013/T015) is self-contained and needs neither US1 nor US2.
- **Polish (Phase 6)**: depends on US1–US3. T018 (republish + cross-repo) is the last step and rides the standard release flow.

### Within-Story Order

- **US1**: T005 (wire) → T006 (live confirm, before hardening) → T007 (validator token) → T008 → T009 → T010 (test edits). T008, T009, and T016 all edit `Feature219EmitFrameworkSkillsTests.fs`, so they are **not** mutually `[P]` — sequence them (T008 → T009, then T016 in Polish). They remain independent of US2/US3 files.
- **US3**: T013 (test, must fail) → T014 (fix) → T015 (green). Strict TDD order.

### Parallel Opportunities

- T008 (emit matrix rows) is independent of US2/US3 files, but shares `Feature219EmitFrameworkSkillsTests.fs` with T009/T016 — so it parallelizes *across stories*, not against those two tasks.
- T011 and T012 `[P]` — the two wrapper files are different files, fully parallel.
- T013 `[P]` (new parity test file) can be written while US1/US2 proceed.
- Once Foundational completes, US1, US2, and US3 can be staffed in parallel (different files: `template.json` + 219 tests / two wrapper `SKILL.md` / `SkillParity.fs` + 223 test).

---

## Parallel Example: after Foundational

```bash
# US2 wrappers (different files) in parallel:
Task: "Create .claude/skills/fs-gg-product-symbology/SKILL.md (Claude-active)"
Task: "Create .agents/skills/fs-gg-product-symbology/SKILL.md (Codex-active)"

# US3 test authored alongside US1 matrix edit (different files):
Task: "Add tests/Rendering.Harness.Tests/Feature223SymbologyParityTests.fs"
Task: "Add game row + symbology to Feature219EmitFrameworkSkillsTests.fs expectedFrameworkSkills"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 Setup → restore/build + baseline.
2. Phase 2 Foundational → touch-point map + **live GV-3 confirmation** (the de-risking gate).
3. Phase 3 US1 → wire the source pair, confirm live post-wiring, flip validator token, harden emit tests.
4. **STOP and VALIDATE**: scaffold `game`/`app` and confirm the real symbology skill emits. This alone closes the core defect of #35.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. US1 → symbology ships (MVP, SC-001/SC-005).
3. US2 → reachable via `fs-gg-product-symbology` (SC-002).
4. US3 → parity fails honestly (SC-003/SC-004).
5. Polish → narrative reversal, full quickstart, cross-repo republish + close #35 (SC-006).

---

## Notes

- `[P]` = different files, no dependency on an incomplete task.
- Tests must be shown failing before their implementation (T013 before T014; T008/T009 against the pre-wiring manifest).
- GV-3 stays green by design (research R1) — but T004 proves it live before anything hardens; do not skip it.
- The feature is not "delivered to consumers" until T018's republish carries it (spec Edge Case "Republish timing").
- Commit after each task or logical group.

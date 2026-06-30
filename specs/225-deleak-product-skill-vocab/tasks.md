---
description: "Task list for De-leak Product Skill Vocabulary"
---

# Tasks: De-leak Product Skill Vocabulary

**Input**: Design documents from `/specs/225-deleak-product-skill-vocab/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/leak-guard-check.md, quickstart.md

**Tests**: Test tasks ARE in scope here — the regression guard (FR-007 / SC-005) is the feature's
deliverable, and its failing-before / passing-after evidence is mandatory (Constitution V). The guard
is authored once in the Foundational phase (so it reds on today's leaks) and greens incrementally as
each story lands.

**Organization**: Tasks are grouped by user story (P1 → P2 → P3). Because all three stories edit
overlapping skill bodies, stories run **sequentially by priority**, not in parallel — see Dependencies.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in each task

## Path Conventions

Single-repo F# framework + `dotnet new` template (plan §Project Structure). Edited content lives under
`template/product-skills/fs-gg-*/SKILL.md`; the guard lives under `tests/Package.Tests/`; discovery is
reused from `tools/Rendering.Harness/SkillParity.fs` (not edited). Readiness evidence under
`specs/225-deleak-product-skill-vocab/readiness/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the repo builds and capture the no-regression baseline.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline MUST run **every** test
> project and record the full red/green set, so pre-existing failures are known up front and not
> mistaken for regressions at merge. The solution run deliberately omits `tests/Package.Tests`
> (where this feature's guard lives) and the `samples/**/*.Tests` projects — exactly where Feature
> 175's surprises hid. Use the discovery-based runner that globs `*.Tests.fsproj`.

- [X] T001 Confirm restore/build is healthy from repo root `/home/developer/projects/FS.GG.Rendering` (no package-ref / lockfile changes in this feature — see CONTRIBUTING and the NU1403 lockfile note if restore stalls)
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/225-deleak-product-skill-vocab/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + `tests/Package.Tests` + `samples/**` — and records the full red/green set; pre-existing reds are flagged here, not at merge)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Verify the produced surface the guard will scan, then author the guard so it reds on
today's leaks — before any prose edit.

**⚠️ CRITICAL**: No user-story prose edit may begin until T004 (produced-surface verification) and
T005 (guard authored) are complete.

> **⚠️ Early produced-surface verification (STANDING, do not omit — plan §Standing assumption / research R1).**
> US2's premise (these 7 skills reach `app`/`game`/`sdd`/`none` products, not just spec-kit) and the
> guard's discovery surface are **provisional assumptions until a real scaffold confirms them**. This
> is the content-feature analogue of the "early live smoke run": enumerate the produced product-skill
> set the way the guard will and, where feasible, scaffold a non-spec-kit product to prove the
> Class-B path actually dead-ends. Do NOT build the de-leak edits on an unverified surface.

- [X] T003 Anchor the leak-inventory ground truth: re-scan `template/product-skills/` and confirm the line-anchored leak sites in research R0 still hold (Class A in `fs-gg-testing`/`fs-gg-ui-widgets`/`fs-gg-skiaviewer`, Class B in all 7, Class C in `fs-gg-symbology`); note any line drift so each later edit can be verified as reframing, not removal
- [X] T004 **Early produced-surface verification**: enumerate via `SkillParity.inventorySkills (defaultRequest root) (discoverDefaultSurfaces root)` filtered to `entry.Path.Contains("template/product-skills")` and confirm it equals exactly the 7 expected ids; where feasible scaffold a non-spec-kit product (`FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx --emit-report`) and confirm all 7 skills are vendored and `specs/<feature>/feedback/` is **absent**; record `specs/225-deleak-product-skill-vocab/readiness/produced-surface.md`
- [X] T005 Author the leak guard `tests/Package.Tests/Feature225ProductSkillVocabularyTests.fs` (sibling of `Feature224SkillCatalogCurrencyTests.fs`): reuse `SkillParity` discovery (no new public surface), scan each `entry.Content` line-by-line for the three leak classes per `contracts/leak-guard-check.md`, apply the R2 paragraph-scoped `spec kit`/`spec-kit` gating rule for Class B, emit one finding per violation `{ Skill; Class; Token; File; Line }`, and include the synthetic inject/revert negative test (one ungated `specs/<feature>/feedback/`, one `package-feed`, one `feature 200` → exactly three findings). Also assert the **discovery surface itself** did not silently narrow: the `template/product-skills`-filtered enumeration returns at least the 7 expected ids (matching T004's produced-surface record), so a regression that drops skills from the scan is caught, not masked by a fixed-list scan (spec edge case "a skill not in scope today gains the leaky boilerplate later")
- [X] T006 Run the guard against today's skills and capture **failing-before** evidence: `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature225ProductSkillVocabulary` — confirm it FAILS naming each offending skill, leak class, matched token, and `file:line`; append to `specs/225-deleak-product-skill-vocab/readiness/regression-evidence.md`

**Checkpoint**: Produced surface confirmed = the 7 skills; guard authored and demonstrably reds on the
current leaks — story edits can begin.

---

## Phase 3: User Story 1 - Evidence guidance speaks the product author's language (Priority: P1) 🎯 MVP

**Goal**: Rewrite the Class-A framework-evidence blocks in the testing / widgets / viewer skills into
product-local "what evidence to record and where", removing every framework-repo-only artifact while
preserving the evidence intent (record visual/readback/control evidence; verify before claiming done).

**Independent Test**: Guard reports **zero Class-A findings** over `fs-gg-testing`, `fs-gg-ui-widgets`,
`fs-gg-skiaviewer`; the evidence sections still tell the author what to record and where, with no
`refresh-local-feed-and-samples.fsx`, `package-feed` proof, `specs/*/readiness/`, `.gitignore`
allowlist, or `BaseOutputPath` reference.

- [X] T007 [P] [US1] Reframe the `## Feature 168 Evidence Rules` block in `template/product-skills/fs-gg-testing/SKILL.md` (lines ~53–63): retitle to `## Evidence Rules`, replace the feed-refresh script / `package-feed` proof / `specs/*/readiness/` + `.gitignore` allowlist / `BaseOutputPath` text with product-local evidence guidance; body only, front-matter untouched (FR-001/FR-004)
- [X] T008 [P] [US1] Reframe the `## Feature 168 Control Evidence Rules` block in `template/product-skills/fs-gg-ui-widgets/SKILL.md` (lines ~111–115): retitle and replace `refresh-local-feed-and-samples` + `package-feed` text with product-local control-evidence guidance; body only (FR-001/FR-004)
- [X] T009 [P] [US1] Reframe the `## Feature 168 Viewer Evidence Rules` block in `template/product-skills/fs-gg-skiaviewer/SKILL.md` (lines ~53–57): retitle and replace `refresh-local-feed-and-samples` + `package-feed` text with product-local viewer-evidence guidance; body only (FR-001/FR-004)
- [X] T010 [US1] Re-run the guard (`--filter Feature225ProductSkillVocabulary`) and confirm **zero Class-A findings**; spot-check `git diff` on the three files to confirm the evidence lesson is preserved (reframe, not removal — SC-004)

**Checkpoint**: Class-A leaks gone from the three evidence-bearing skills; intent preserved. This is
the MVP increment (the "worst leak", per the audit).

---

## Phase 4: User Story 2 - Feedback / persistent-problems guidance fits the author's lifecycle (Priority: P2)

**Goal**: Make the `specs/<feature>/feedback/` recommendation in **all 7** skills spec-kit-conditional,
so under a non-spec-kit lifecycle the "where to record findings" instruction resolves to a real
location (the skill's durable-lessons / a product-local `docs/`), while spec-kit authors keep the path.

**Independent Test**: Guard reports **zero unconditional Class-B findings** over all 7 skills; a
properly gated `spec kit`/`spec-kit` paragraph still passes; under a non-spec-kit scaffold the
recommended record location exists.

> The conditional phrasing (research R2): *"If your product uses Spec Kit, record findings under
> `specs/<feature>/feedback/`; otherwise record them in this skill's Sources / durable-lessons line
> (and any product-local `docs/` location)."* The gating phrase must sit in the **same paragraph** as
> the path so the guard recognizes it as conditional.

- [X] T011 [P] [US2] Make the "Persistent problems" feedback line spec-kit-conditional in `template/product-skills/fs-gg-elmish/SKILL.md` (line ~65)
- [X] T012 [P] [US2] Make the "Persistent problems" feedback line spec-kit-conditional in `template/product-skills/fs-gg-keyboard-input/SKILL.md` (line ~101)
- [X] T013 [P] [US2] Make the "Persistent problems" feedback line spec-kit-conditional in `template/product-skills/fs-gg-scene/SKILL.md` (line ~82)
- [X] T014 [US2] Make the feedback line spec-kit-conditional in `template/product-skills/fs-gg-testing/SKILL.md` (line ~89) — same file as T007, so after US1
- [X] T015 [US2] Make the feedback line spec-kit-conditional in `template/product-skills/fs-gg-ui-widgets/SKILL.md` (line ~150) — same file as T008, so after US1
- [X] T016 [US2] Make the feedback line spec-kit-conditional in `template/product-skills/fs-gg-skiaviewer/SKILL.md` (line ~106) — same file as T009, so after US1
- [X] T017 [US2] Make the (shortened) feedback line spec-kit-conditional in `template/product-skills/fs-gg-symbology/SKILL.md` (line ~200) — same file as US3 (T019), coordinate edits
- [X] T018 [US2] Re-run the guard and confirm **zero unconditional Class-B findings** across all 7; verify a gated paragraph still passes (conditional spec-kit path preserved, FR-002); spot-check non-spec-kit record location resolves (SC-002)

**Checkpoint**: All 7 skills carry a lifecycle-resolvable feedback recommendation; the spec-kit path
survives as a conditional option.

---

## Phase 5: User Story 3 - No framework feature-number stamps in consumer prose (Priority: P3)

**Goal**: Remove the remaining Class-C feature/spec-number stamps from `fs-gg-symbology` (`feature 199`,
`feature 200` ×2, `spec-196 baseline`), rewording so the capability prose (rich-text layout,
auto-label, label-bound motion) stays intact. The three `## Feature 168 …` headings were already
retitled by US1.

**Independent Test**: Guard reports **zero Class-C findings** across the whole shipped set; the
symbology capability prose remains while no `[Ff]eature \d+` / `spec-\d+` stamp survives.

- [X] T019 [US3] Remove the `feature 199` (line ~96), `feature 200` (lines ~122, ~137), and `spec-196 baseline` (line ~63) stamps in `template/product-skills/fs-gg-symbology/SKILL.md`, rewording each sentence to carry its lesson/capability without the number; body only, front-matter untouched (FR-003/FR-004) — coordinate with T017 (same file)
- [X] T020 [US3] Re-run the guard and confirm **zero Class-C findings** over the full set; spot-check `git diff` confirms the symbology capabilities (rich-text, auto-label, label-bound motion) are preserved (SC-003/SC-004)

**Checkpoint**: All three leak classes are gone from every shipped skill.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Prove the whole de-leaked state, the parity invariant, lesson preservation, and record
the cross-repo delivery dependency.

- [X] T021 Capture **passing-after** evidence: run the guard over the corrected set + the synthetic inject/revert test → zero findings on the real set, exactly three on the injected body; append to `specs/225-deleak-product-skill-vocab/readiness/regression-evidence.md` (SC-005)
- [X] T022 Run the wrapper-vs-canonical parity suite `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj` (`Feature168*` + `Feature223SymbologyParityTests.fs`) → green; if parity requires body-coverage match, apply the same de-leak to the canonical source skill(s) and re-run (FR-006/SC-006)
- [X] T023 [P] Lesson-preservation **and scope-confinement** review across all edits: `git diff template/product-skills/` — confirm every removed token is replaced by product-language guidance carrying the same intent, 100% reframing not removal (SC-004/FR-004). Additionally assert the FR-005 negatives: `git status` shows **no** change to the consumer catalog docs (`skillist-reference.md` / `scaffold-map.md`, owned by #36); the discovered product-skill **id set is unchanged** (same count + names — no skill added/removed); `name:`/`description:` front-matter is **byte-identical** across all edited skills (and any canonical-source body edited under the FR-005 parity exception is body-only); diff is confined to `template/product-skills/` plus the new guard test (and, if parity required it, the canonical source body only)
- [X] T024 Re-run the full baseline (`dotnet fsi scripts/baseline-tests.fsx`) and walk `quickstart.md` steps 1–6 end-to-end; confirm no new reds vs T002 baseline
- [X] T025 Cross-repo coherence (FR-008/FR-009): via the `cross-repo-coordination` skill, record the delivery dependency (republished `FS.GG.UI.Template` coherent set + downstream pin bump FS-GG/FS.GG.Templates#8) and update the originating board item #37 and parent epic #34 to reflect delivery — this feature produces content + guard, not the publish

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. **Blocks all story work.** T004 (produced-surface
  verification) and T005/T006 (guard authored + reds-before) must complete before any prose edit.
- **User Stories (Phase 3–5)**: Depend on Foundational. Run **sequentially by priority** (P1 → P2 →
  P3) because the stories edit overlapping files (see below) — they are independently *testable* via
  the guard's per-class findings, but not safely *parallelizable*.
- **Polish (Phase 6)**: Depends on all three stories complete.

### Cross-story file overlap (why stories are sequential)

- `fs-gg-testing`, `fs-gg-ui-widgets`, `fs-gg-skiaviewer`: edited by **US1** (Class A) **and** **US2**
  (Class B) → US2 tasks T014/T015/T016 run after their US1 counterparts T007/T008/T009.
- `fs-gg-symbology`: edited by **US2** (T017, Class B) **and** **US3** (T019, Class C) → coordinate /
  sequence the two edits on that one file.
- `fs-gg-elmish`, `fs-gg-keyboard-input`, `fs-gg-scene`: Class-B only → US2-only, no cross-story
  conflict.

### Within Each User Story

- The guard (T005) is written before any edit and verified red (T006) — TDD failing-before.
- Each story ends with a guard re-run confirming its class is clean (T010 / T018 / T020).

### Parallel Opportunities

- **Setup**: T001 then T002 (T002 depends on a healthy build).
- **Foundational**: T003 and T004 can overlap; T005 depends on T004 (surface) and the contract; T006
  depends on T005.
- **US1**: T007, T008, T009 are `[P]` — three distinct files, no shared edits.
- **US2**: T011, T012, T013 are `[P]` (Class-B-only files); T014–T017 are sequential against US1/US3
  edits on shared files.
- **US3**: single file — no internal parallelism.
- **Polish**: T023 (`[P]`, read-only diff review) can run alongside T021/T022.

---

## Parallel Example: User Story 1

```bash
# Launch the three Class-A reframes together (distinct files, no shared edits):
Task: "Reframe evidence block in template/product-skills/fs-gg-testing/SKILL.md"
Task: "Reframe control-evidence block in template/product-skills/fs-gg-ui-widgets/SKILL.md"
Task: "Reframe viewer-evidence block in template/product-skills/fs-gg-skiaviewer/SKILL.md"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (build + baseline).
2. Complete Phase 2: Foundational — including the **early produced-surface verification** (T004) that
   confirms the guard's scan surface before any edit, and the guard authored + red-before (T005/T006).
3. Complete Phase 3: User Story 1 (the worst leak — Class-A evidence process).
4. **STOP and VALIDATE**: guard shows zero Class-A findings; evidence lessons preserved.
5. Demo / hand off the MVP increment.

### Incremental Delivery

1. Setup + Foundational → produced surface confirmed, guard reds on today's leaks.
2. US1 → guard Class-A clean → MVP.
3. US2 → guard Class-B clean (conditional spec-kit path preserved).
4. US3 → guard Class-C clean → whole set clean.
5. Polish → parity green, passing-after evidence, cross-repo #37/#34 updated, delivery dependency
   recorded (rides the `FS.GG.UI.Template` republish + Templates#8 pin).

---

## Notes

- `[P]` = different files, no dependencies.
- `[Story]` label maps each edit to its user story for traceability.
- All edits are **body only** — `name:`/`description:` front-matter stays byte-identical to protect
  wrapper-vs-canonical parity (FR-006).
- Every edit is **reframing, not removal** — preserve the lesson/capability each leaky block carried
  (FR-004/SC-004).
- The guard reuses existing public `SkillParity` discovery — no new public surface, no `.fsi`/baseline
  delta (Principle II; mirrors Feature 224).
- Commit after each task or logical group; stop at any checkpoint to validate the story via the guard.

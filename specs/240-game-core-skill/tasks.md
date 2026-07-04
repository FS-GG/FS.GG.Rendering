---

description: "Task list — fs-gg-game-core product skill (#73)"
---

# Tasks: `fs-gg-game-core` — product skill for simulation patterns

**Input**: Design documents from `specs/240-game-core-skill/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/{skill-body,manifest-entry}.md, quickstart.md

**Tests**: This feature's semantic tests ARE the interlocking Package.Tests roster/count updates
(Constitution Principle I: Spec → contract → Semantic Tests → Implementation). They are first-class
tasks here, not optional. They must FAIL before the wiring/body land and PASS after.

**Organization**: grouped by the three spec user stories. The SKILL.md body + wiring are shared
prerequisites (Foundational) that all three stories build on.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1 / US2 / US3 (Foundational, Setup, Polish carry no story label)

## Path Conventions

Template/packaging feature at repo root: `template/product-skills/`, `.template.config/`, `scripts/`,
`template/skill-manifest/`, `tests/Package.Tests/`, `template/base/docs/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: know the pre-existing red/green set so nothing is mistaken for a regression at merge.

- [x] T001 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/240-game-core-skill/readiness/baseline.md` (globs every `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds, incl. the known local `packages.lock.json` NU1403 drift, are flagged here, not discovered at merge)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: the canonical body + the emission wiring + the regenerated manifest — every user story
depends on these existing. **No story work begins until this phase is complete.**

> **⚠️ Early smoke run (STANDING, do not omit).** This feature's "drive the real thing" analog is a
> **scaffold smoke**: after the body + source + regen exist, actually materialize a `profile=game` and a
> `profile=app` product and observe that the skill file appears for game (byte-equal to source) and is
> absent for app, and that `generate-skill-manifest.fsx --check` is up-to-date. Do this BEFORE finalizing
> the five test edits — it confirms the wiring end-to-end so the tests codify observed reality, not a
> hypothesis. (Feature 175 lesson: deterministic edits can look right while the emitted product is wrong.)

### Packaging — make Canvas consumable on the simulation profiles (FR-011/FR-012)

- [x] T002a Pin `FS.GG.UI.Canvas` in `template/base/Directory.Packages.props`: `<PackageVersion Include="FS.GG.UI.Canvas" Version="$(FsGgUiVersion)" />` inside a `<!--#if (profile == "game" || profile == "sample-pack") -->` gate; update `tests/Package.Tests/Feature209VersionCoherenceTests.fs` `templateExpected` (+`FS.GG.UI.Canvas`) and the "11-member" → "12-member" message
- [x] T002b Reference it in `template/base/src/Product/Product.fsproj`: `<PackageReference Include="FS.GG.UI.Canvas" />` inside a `<!--#if (profile == "game" || profile == "sample-pack") -->` gate
- [x] T002c Bundle the Canvas surface docs: create `template/base/docs/api-surface/Canvas/{Elements,FixedStep,Loop,Rng}.fsi` copied verbatim from `src/Canvas/`; refresh `template/base/docs/api-surface/Scene/Scene.fsi` to add the `Geometry` module (mirror `src/Scene/Geometry.fsi`)

### Skill body + wiring

- [x] T002 Draft the contract seam first — author the canonical body `template/product-skills/fs-gg-game-core/SKILL.md` per `contracts/skill-body.md` + `data-model.md §3`: front-matter (`name: fs-gg-game-core`, one-line family-voice `description`), sections Scope / Public Contract / Fixed-timestep march / RNG determinism / Collision / Culling / Common pitfalls, citing ONLY the D4 member set, including the compilable end-to-end snippet (loop→draw→collide→cull). The Public Contract points at `docs/api-surface/Scene/Scene.fsi` (Geometry) + `docs/api-surface/Canvas/{Rng,FixedStep}.fsi`
- [x] T003 Add the emission source to `.template.config/template.json`: `{ condition: "(profile == \"game\" || profile == \"sample-pack\")", source: "template/product-skills/fs-gg-game-core/", target: ".agents/skills/fs-gg-game-core/", copyOnly: ["**/*"] }` (place beside the other `template/product-skills/*` sources; no `lifecycle` clause)
- [x] T004 Add the generator catalog tuple to `scripts/generate-skill-manifest.fsx` — `"fs-gg-game-core", "template/product-skills/fs-gg-game-core/SKILL.md", "(profile == \"game\" || profile == \"sample-pack\")"` (keep the list sorted asc by id: between `fs-gg-feedback-capture` and `fs-gg-keyboard-input`), then regenerate: `dotnet fsi scripts/generate-skill-manifest.fsx`
- [x] T005 **Scaffold smoke (early live evidence)**: run `dotnet fsi scripts/generate-skill-manifest.fsx --check` (expect up-to-date, 13 entries); scaffold a `profile=game` and a `profile=app` product; confirm `.agents/skills/fs-gg-game-core/SKILL.md` present + byte-equal to source under `game`, absent under `app`; **and that the `game` product references `FS.GG.UI.Canvas` and `restore` resolves it** (FR-011); record evidence under `specs/240-game-core-skill/readiness/`
- [x] T006 Verify the manifest diff is additive-only: `git diff template/skill-manifest/skill-manifest.json` shows exactly one added entry block and the twelve prior entries byte-identical (M8)

**Checkpoint**: Canvas consumable on sim profiles; body + source + 13-entry manifest exist; the scaffold emits the skill for the right profiles and compiles Canvas — story test work can begin.

---

## Phase 3: User Story 1 — A game consumer discovers the simulation primitives (Priority: P1) 🎯 MVP

**Goal**: the materialized body correctly points a consumer at the real Feature-239 surface and reads as
a first-class sibling skill.

**Independent Test**: every member the body names resolves in the packed `.fsi`; the body passes the
product-skill vocabulary/leak checks; the embedded snippet compiles against the packed `Scene`/`Canvas`.

- [x] T007 [US1] Add a **surface-referenced** check (in `tests/Package.Tests/Feature231SkillManifestTests.fs` or a sibling) asserting every FS.GG.UI member named in `fs-gg-game-core/SKILL.md` exists in the packed `.fsi` under `template/base/docs/api-surface/{Scene,Canvas}` — i.e. `Geometry.{intersects,contains,containsPoint,center,ofCenter,sweptIntersects}`, `Rng.{ofSeed,nextFloat,nextInt,split}` + type `Rng`, `FixedStep.{defaultMaxFrameTime,drain,drainWith}`; a deliberately-renamed reference must fail it (SC-004)
- [x] T008 [US1] Add `fs-gg-game-core` to `expectedProductSkillIds` in `tests/Package.Tests/Feature225ProductSkillVocabularyTests.fs` (9 → 10) and update the "9 expected ids" message + stale "7 shipped product skills" comment; run the suite so the vocabulary/leak checks execute against the new body
- [x] T009 [US1] Verify the body's end-to-end snippet compiles against the packed surface (paste into an `.fsx` referencing `Scene`/`Canvas`, run it — loop drains, RNG threads `next`, collision + cull evaluate) per `quickstart.md §5`

**Checkpoint**: US1 green — the body is accurate, well-formed, and consumer-compilable.

---

## Phase 4: User Story 2 — The skill materializes only for simulation profiles (Priority: P1)

**Goal**: the gate condition emits for `game`/`sample-pack` only, and the union machinery reads it
honestly for the excluded profiles.

**Independent Test**: `materializes-when` evaluates true for `{game}`/`{sample-pack}`, false for
`{app}`/`{headless-scene}`/`{governed}`; the sdd-lane framework matrix gains the skill on exactly those
two rows.

- [x] T010 [US2] Add `fs-gg-game-core` to the catalog list in `tests/Package.Tests/Feature238SkillMaterializesWhenTests.fs` (12 → 13); confirm its no-drift check (manifest `materializes-when` == the `template.json` source condition) and true/false-per-profile evaluation pass for the new entry (M3/M4/M5)
- [x] T011 [US2] Update `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs`: add `fs-gg-game-core` to the `game` and `sample-pack` rows of `expectedFrameworkSkills`, bump the framework-skill-source count assertion `9 → 10`, and extend the explanatory comment (M6)
- [x] T012 [US2] Confirm `tests/Package.Tests/Feature224SkillCatalogCurrencyTests.fs` stays green — the new real id resolves (no dangling-reference finding); if a skill-catalog doc enumerates ids, add `fs-gg-game-core` there

**Checkpoint**: US2 green — emission is correctly scoped and the gate reads the condition honestly.

---

## Phase 5: User Story 3 — The manifest and gate stay coherent at 13 skills (Priority: P2)

**Goal**: generator, on-disk manifest, and tests all agree on thirteen skills; no digest/path/condition
drift on the prior twelve.

**Independent Test**: `generate-skill-manifest.fsx --check` up-to-date at 13 entries; the twelve prior
entries byte-identical.

- [x] T013 [US3] Update `tests/Package.Tests/Feature231SkillManifestTests.fs`: add `fs-gg-game-core` to the `catalog` list (12 → 13), update the "Catalog (12 entries)" comment to 13, and confirm the digest check (`sha256` == `sha256(body)`) passes for the new entry (M1/M2)
- [x] T014 [US3] Run the full skill suite green: `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "Feature219|Feature224|Feature225|Feature231|Feature238"` (quickstart §2) — all five files consistent at 13/10/10

**Checkpoint**: US3 green — the machine catalog is coherent and drift-guarded.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [x] T015 [P] Cross-link the skill from `template/base/docs/product.md` — point the collision / RNG / fixed-step guidance at `fs-gg-game-core` (FR-010), mirroring how sibling capabilities reference their skills
- [x] T016 Run the full `quickstart.md` validation (steps 1–5) and confirm `git diff` on the twelve prior manifest entries is empty
- [x] T017 [P] Capture per-phase feedback into `specs/240-game-core-skill/feedback/` via the fs-gg-feedback-capture flow (process friction, any generalizable-code candidates)
- [x] T018 Re-run the no-regression baseline and diff against T001 — confirm no test moved red except the intended roster/count edits now green

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (T001)**: no dependencies.
- **Foundational (T002–T006)**: T002 → T003 → T004 → T005 → T006 are largely sequential (body before source before catalog/regen before smoke before diff-verify). **Blocks all stories.**
- **US1 (T007–T009), US2 (T010–T012), US3 (T013–T014)**: all depend on Foundational; once it is done the three stories are independent and can proceed in parallel (they touch different test files).
- **Polish (T015–T018)**: after the desired stories are green.

### Within/Across Stories — parallel opportunities

- US1, US2, US3 touch **different** test files (Feature231-surface/Feature225 · Feature238/Feature219/Feature224 · Feature231-catalog), so with care they run in parallel. Note T007 and T013 both edit `Feature231SkillManifestTests.fs` — sequence those two (not [P] against each other).
- T015 and T017 are independent of the test edits → [P].

### Parallel Example (after Foundational)

```bash
# US2 and US3 test edits in parallel (different files):
Task: "T011 update Feature219EmitFrameworkSkillsTests.fs (game/sample-pack rows, 9→10)"
Task: "T013 update Feature231SkillManifestTests.fs catalog (12→13)"
Task: "T010 update Feature238SkillMaterializesWhenTests.fs catalog (12→13)"
```

---

## Implementation Strategy

### MVP (Foundational + US1)

1. T001 baseline.
2. T002–T006 Foundational — body + wiring + regen + **scaffold smoke** (the end-to-end evidence).
3. T007–T009 US1 — surface-referenced + vocabulary + snippet-compiles.
4. **STOP & VALIDATE**: the skill materializes for game and accurately cites the real surface — demonstrable value.

### Incremental

- + US2 (T010–T012): prove profile scoping + honest gate.
- + US3 (T013–T014): prove 13-skill catalog coherence.
- Polish (T015–T018): docs cross-link, quickstart, feedback, baseline re-diff.

---

## Notes

- The body (T002) is the single artifact all three stories test; edit it once, test from three angles.
- Regenerate the manifest with the generator only (T004) — never hand-edit the JSON.
- Verify each test FAILS before the wiring/body lands (roster/count mismatch, stale digest) and PASSES after.
- Commit after each logical group; keep the pre-existing `packages.lock.json`/`version-coherence.md` working-tree noise OUT of feature commits (scope `git add` to the feature paths).

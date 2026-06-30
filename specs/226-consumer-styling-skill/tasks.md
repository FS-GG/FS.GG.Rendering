---
description: "Task list for Feature 226 — Consumer Theming/Styling Product Skill"
---

# Tasks: Consumer Theming/Styling Product Skill

**Input**: Design documents from `/specs/226-consumer-styling-skill/`

**Prerequisites**: spec.md (required — user stories & FRs) and plan.md (filled: Tier 2 content-only, Constitution Check PASS, verified touch-points + profile gating). Tasks are grounded in spec.md + plan.md + the verified template-wiring map.

**Feature class**: Package-content only (FR-010). Adds one shipped consumer styling skill + its wrapper + template wiring + cross-link. Changes **no** framework styling code, no existing shipped skill's capability.

**Skill name (decided)**: canonical `fs-gg-styling`, wrapper `fs-gg-product-styling` (consumer "consume-a-style" slice; deliberately distinct from the unshipped framework-internal `fs-gg-design-system` / `fs-gg-ant-design`). Confirm in T001 before authoring; if changed, the name propagates to every path below.

**Tests**: No new test project is added — this feature is content + config, gated by the *existing* repo-owned checks (skill-parity, Feature 225 leak guard, Feature 224 catalog currency). Tasks run those existing checks rather than authoring new ones.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 / US2 / US3 (maps to spec.md user stories)

## Path Conventions

Verified touch-points (from the template-wiring map):

- Canonical body: `template/product-skills/fs-gg-styling/SKILL.md`
- Wrapper pair: `.agents/skills/fs-gg-product-styling/SKILL.md` + `.claude/skills/fs-gg-product-styling/SKILL.md`
- Template wiring: `.template.config/template.json` (`sources` array)
- Catalog (Feature 224): `template/base/docs/skillist-reference.md`
- Leak-guard backstop (Feature 225): `tests/Package.Tests/Feature225ProductSkillVocabularyTests.fs`
- Controls skill (cross-link target, FR-008): `template/product-skills/fs-gg-ui-widgets/SKILL.md`
- Parity engine: `tools/Rendering.Harness/SkillParity.fs` (dynamic discovery — no hardcoded list to edit)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Lock the naming decision and capture the pre-change baseline so nothing is mistaken for a regression at merge.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Run **every** test project, including `tests/Package.Tests` (owns the leak-guard + catalog-currency + public-surface gates this feature touches) and the `samples/**/*.Tests` projects, which the solution deliberately omits. Use the discovery-based runner so nothing silently drops out.

- [X] T001 Confirm the shipped skill name (`fs-gg-styling` / wrapper `fs-gg-product-styling`) against the live name collision set — verify no `template/product-skills/fs-gg-styling/`, no `.claude/skills/fs-gg-product-styling/`, and no clash with `fs-gg-design-system` / `fs-gg-ant-design`; record the decision in `specs/226-consumer-styling-skill/readiness/naming.md`
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/226-consumer-styling-skill/readiness/baseline.md` (runs EVERY test project — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Pin the two facts everything else builds on — (a) the *real* gating mechanism the new skill must mirror, and (b) the *real* consumer-reachable styling surface the skill body must document. Both are unverified assumptions until checked against the running template/produced product.

**⚠️ CRITICAL**: No user-story work begins until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** This feature ships no runtime code change, so the "real app" to drive is the **scaffold path**: actually produce a product from the template and observe the shipped skill set BEFORE authoring. Feature 175's lesson — deterministic tests pass while the produced artifact is wrong (a sibling skill was authored but never shipped, per spec US2) — applies directly here. Confirm the gating empirically; do not build delivery work (US2) on the assumed gate.

- [X] T003 Build the wiring map every story depends on: record in `specs/226-consumer-styling-skill/readiness/wiring-map.md` the exact `fs-gg-ui-widgets` gating in `.template.config/template.json` (the two `sources` entries gated `(profile == "app" || profile == "game")`, targeting `.agents/skills/` and `.claude/skills/`), the wrapper-pairing rule enforced by `tools/Rendering.Harness/SkillParity.fs` (`productAliasTarget` / `requiresWrapper` / `missingWrapperFindings` — a `fs-gg-product-*` alias is the ONLY thing that satisfies the wrapper requirement), and the Feature 225 backstop list `expectedProductSkillIds` plus the Feature 224 catalog at `template/base/docs/skillist-reference.md`
- [X] T004 **Early live smoke run**: scaffold a controls-bearing profile (`profile == "app"`) and **both** scene-only profiles (`profile == "headless-scene"` and `profile == "governed"`) from the template, observe the produced `.agents/skills/` + `.claude/skills/` skill sets, and record in `specs/226-consumer-styling-skill/readiness/scaffold-before.md` that today exactly the seven skills ship, `fs-gg-ui-widgets` is present on `app` and absent on **both** scene-only profiles, and `fs-gg-styling` is absent everywhere — confirming the gate the feature will extend (or record `environment-limited` with the disclosed substitute if scaffolding cannot run locally). Both scene-only profiles named in the spec edge case (FR-006) are exercised, not just one representative.
- [X] T005 Confirm the **consumer styling surface** the skill body will teach: from the produced product's public API (the theme/style types a product author can actually reach — theme selection, style variant, style class, resolved style applied to a control), enumerate the real consumer-visible entry points in `specs/226-consumer-styling-skill/readiness/styling-surface.md`, and mark the boundary line beyond which content would duplicate the unshipped `fs-gg-design-system` (token source / `StyleResolver` internals / surface baselines — out of scope per FR-003)

**Checkpoint**: Gating confirmed against a real scaffold; consumer styling surface enumerated and bounded — authoring (US1) and delivery wiring (US2) can begin.

---

## Phase 3: User Story 1 - A product author learns to theme & style from a shipped skill (Priority: P1) 🎯 MVP

**Goal**: Author the one shipped, consumer-facing styling skill body that teaches theme selection, style variant, style class, and applying a resolved style — in product-author language, with zero framework-internal machinery and zero leak-class vocabulary.

**Independent Test**: Read `template/product-skills/fs-gg-styling/SKILL.md` as a product author with no framework-repo access; confirm it answers "how do I theme my product and style a control" using only consumer-visible concepts and never instructs editing the token source, the resolver, or surface baselines.

**Maps**: FR-001 (body), FR-002 (consumer slice only), FR-003 (no internals), FR-004 (passes leak guard).

- [X] T006 [US1] Create the canonical skill body `template/product-skills/fs-gg-styling/SKILL.md` with the two-field YAML frontmatter exactly as siblings use it (`name: fs-gg-styling`, one-line consumer-oriented `description:`), parallel in form to `template/product-skills/fs-gg-ui-widgets/SKILL.md`
- [X] T007 [US1] Write the body to teach **only** the consumer slice from `specs/226-consumer-styling-skill/readiness/styling-surface.md` — (a) pick/apply a theme, (b) set a control's style variant + style class, (c) apply a resolved style to a control — using product-author language and the real public entry points (FR-002)
- [X] T008 [US1] Enforce the scope boundary in `template/product-skills/fs-gg-styling/SKILL.md`: no DTCG token-source authoring, no `StyleResolver` pipeline internals, no surface-baseline authoring; where the pipeline is the authority, point the reader at "consume the result" not "build the resolver" (FR-003)
- [X] T009 [US1] Scrub `template/product-skills/fs-gg-styling/SKILL.md` for leak-class vocabulary the Feature 225 guard bans — no `[Ff]eature \d+` / `spec-\d+`, no `package-feed` / `refresh-local-feed-and-samples` / `specs/*/readiness` / `.gitignore` / `BaseOutputPath`, no ungated `specs/*/feedback` reference (FR-004)
- [X] T010 [US1] Verify US1 in isolation: run the leak guard over the new body — `dotnet test tests/Package.Tests --filter Feature225ProductSkillVocabularyTests` — and hand-read the body against the US1 Independent Test (consumer-only, no internals); record the read in `specs/226-consumer-styling-skill/readiness/us1-skill-read.md`

**Checkpoint**: The styling skill body exists, teaches the consumer slice, and passes the leak guard — readable and valuable on its own even before it is wired to ship.

---

## Phase 4: User Story 2 - A scaffolded product actually carries the styling skill on the right profiles (Priority: P2)

**Goal**: Deliver the skill — wrapper pair, template wiring gated by product surface (not lifecycle), and enumeration by the repo-owned parity/leak/catalog surfaces — so a scaffolded controls-bearing product carries it and a scene-only product does not force it in.

**Independent Test**: Scaffold each UI-bearing profile and confirm `fs-gg-styling` is present in the produced skill set (and absent from scene-only profiles), independent of the lifecycle choice.

**Maps**: FR-001 (shipped), FR-005 (wired & carried by wrapper), FR-006 (not on scene-only), FR-007 (surface not lifecycle), FR-009 (enumerated by parity/leak/catalog).

- [X] T011 [US2] Create the wrapper pair routing to the canonical body: `.agents/skills/fs-gg-product-styling/SKILL.md` and `.claude/skills/fs-gg-product-styling/SKILL.md`, each with `name: fs-gg-product-styling`, the matching one-line `description:`, and a body that reads `../../../template/product-skills/fs-gg-styling/SKILL.md` — modeled exactly on the `fs-gg-product-ui-widgets` wrapper (required by `SkillParity.fs` `missingWrapperFindings`; a bare same-named wrapper does NOT satisfy it)
- [X] T012 [US2] Wire the two `sources` entries into `.template.config/template.json` mirroring `fs-gg-ui-widgets`: condition `(profile == "app" || profile == "game")`, source `template/product-skills/fs-gg-styling/`, targets `.agents/skills/fs-gg-styling/` and `.claude/skills/fs-gg-styling/` — gated on `profile` only, NOT `lifecycle` (FR-005, FR-006, FR-007). **Decision (resolves spec Assumption):** the gate is `app`+`game` only — `sample-pack` is deliberately **excluded** because the verified wiring map (T003) shows `fs-gg-ui-widgets` itself is not gated to `sample-pack`; the spec's "default reading: app, game, sample-pack" was provisional and is superseded by the real wiring `fs-gg-styling` must mirror
- [X] T013 [P] [US2] Add `"fs-gg-styling"` to the `expectedProductSkillIds` backstop set in `tests/Package.Tests/Feature225ProductSkillVocabularyTests.fs` so discovery is asserted not to have narrowed (FR-009)
- [X] T014 [P] [US2] Add the catalog row for `fs-gg-styling` to `template/base/docs/skillist-reference.md` (Product capability skills table) — `target` `.agents/skills/fs-gg-styling/SKILL.md`, profiles `app, game` — keeping the "(Each also ships a `fs-gg-product-<name>` wrapper alias alongside it.)" note true (Feature 224 currency, FR-009)
- [X] T015 [US2] Run the skill-parity check — `dotnet run --project tools/Rendering.Harness -- skill-parity` (via `scripts/check-agent-skill-parity.fsx`) — and confirm `fs-gg-styling` is inventoried with its `fs-gg-product-styling` wrapper and reports no missing-wrapper / orphan finding
- [X] T016 [US2] Run the catalog-currency + leak guard together — `dotnet test tests/Package.Tests --filter "Feature224SkillCatalogCurrencyTests|Feature225ProductSkillVocabularyTests"` — confirm green (row resolves to a real consumer SKILL.md; new skill scanned clean)
- [X] T017 [US2] Verify US2 Independent Test live: re-scaffold `profile == "app"` (controls-bearing) and **both** scene-only profiles (`profile == "headless-scene"` and `profile == "governed"`) and, for at least two lifecycle choices on `app` (e.g. `spec-kit` and `none`), confirm `fs-gg-styling` ships on `app` identically regardless of lifecycle and is absent on **both** scene-only profiles; record in `specs/226-consumer-styling-skill/readiness/scaffold-after.md` (FR-006, FR-007)

**Checkpoint**: A real scaffold of a controls-bearing profile carries `fs-gg-styling`; scene-only does not; parity/leak/catalog all green.

---

## Phase 5: User Story 3 - An author composing controls is pointed to the styling skill (Priority: P3)

**Goal**: Remove the discovery dead-end — `fs-gg-ui-widgets` explicitly points to `fs-gg-styling` for theming the controls it teaches the author to compose.

**Independent Test**: Read `template/product-skills/fs-gg-ui-widgets/SKILL.md` as a product author and confirm it points to the styling skill in one hop.

**Maps**: FR-008.

- [X] T018 [US3] Add an explicit in-skill pointer in `template/product-skills/fs-gg-ui-widgets/SKILL.md` to `fs-gg-styling` ("to theme/style the controls above, see the `fs-gg-styling` skill"), worded to stay leak-clean (no feature/spec numbers) so it still passes the Feature 225 guard
- [X] T019 [US3] Verify US3: re-run the leak guard over `fs-gg-ui-widgets` — `dotnet test tests/Package.Tests --filter Feature225ProductSkillVocabularyTests` — and hand-confirm the pointer reaches `fs-gg-styling` in one hop with no external search; record in `specs/226-consumer-styling-skill/readiness/us3-crosslink-read.md`

**Checkpoint**: All three stories independently functional — body teaches, scaffold delivers, controls skill points to it.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Confirm the content-only invariant and the success criteria across all stories.

- [X] T020 Confirm FR-010 / SC-002: `git diff --stat` against `main` touches only the new skill body, the wrapper pair, `template.json`, `skillist-reference.md`, the Feature 225 backstop list, and the `fs-gg-ui-widgets` cross-link — NO change to framework styling code (`src/**`) and NO capability change to any existing shipped skill
- [X] T021 Re-run the full no-regression baseline — `dotnet fsi scripts/baseline-tests.fsx --out specs/226-consumer-styling-skill/readiness/baseline-after.md` — and diff against `baseline.md` (T002): the only deltas should be the now-green parity/leak/catalog assertions for `fs-gg-styling`; no new reds
- [X] T022 [P] Validate Success Criteria roll-up in `specs/226-consumer-styling-skill/readiness/success-criteria.md`: SC-001 (answerable from shipped set only), SC-003 (100% controls profiles carry it / 0% scene-only forced), SC-004 (parity + leak green, no undiscovered gap), SC-005 (one-hop cross-link), SC-006 (zero token/resolver/baseline edits) — each citing the readiness artifact that proves it
- [X] T023 Record the delivery-rides-republish note (spec Assumption): this feature's "done" is authored-and-wired-on-branch; consumer delivery follows the next `FS.GG.UI.Template` republish + downstream re-pin, sequenced by the epic — do NOT bump/publish as part of this feature

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. **BLOCKS all user stories** — the gate (T004) and the styling surface (T005) must be confirmed before authoring or wiring.
- **User Stories (Phase 3-5)**: All depend on Foundational. US1 (body) is independent. US2 (delivery) needs the US1 body to exist (T011/T012 ship/route to it). US3 (cross-link) is independent of US2 but its pointer is only *valuable* once the body exists (US1).
- **Polish (Phase 6)**: Depends on all desired stories complete.

### User Story Dependencies

- **US1 (P1)**: After Foundational. No dependency on US2/US3. — MVP.
- **US2 (P2)**: After Foundational; needs the canonical body from US1 (T006) to target. T013/T014 are [P] (different files). 
- **US3 (P3)**: After Foundational; logically references the `fs-gg-styling` name (decided T001) — can be authored in parallel with US2, but verify after US1 body lands.

### Within Each Story

- US1: T006 (body+frontmatter) → T007/T008 (content + scope boundary) → T009 (leak scrub) → T010 (verify).
- US2: T011 (wrapper) + T012 (wiring) → T013/T014 [P] (enumeration) → T015/T016 (checks) → T017 (live scaffold verify).
- US3: T018 (pointer) → T019 (verify).

### Parallel Opportunities

- T013 and T014 are [P] — different files (test backstop vs catalog markdown).
- T022 is [P] — independent readiness write.
- US3 (T018) can proceed alongside US2 once the `fs-gg-styling` name is fixed and the body exists.

---

## Parallel Example: User Story 2

```text
# After the canonical body (T006) and wrapper/wiring (T011/T012) exist,
# the two enumeration edits touch different files and run together:
Task T013: Add "fs-gg-styling" to expectedProductSkillIds in tests/Package.Tests/Feature225ProductSkillVocabularyTests.fs
Task T014: Add the fs-gg-styling catalog row in template/base/docs/skillist-reference.md
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational (CRITICAL — confirm the real gate via a live scaffold + enumerate the real consumer styling surface, before authoring) → 3. Phase 3 US1 → **STOP & VALIDATE**: read the body as a product author, run the leak guard. The shipped consumer styling slice now exists.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. US1 → body authored, leak-clean → MVP.
3. US2 → wrapper + wiring + enumeration → scaffold actually carries it → demo a real produced product.
4. US3 → cross-link from controls skill → discovery dead-end removed.
5. Polish → confirm content-only invariant, no regressions, success criteria.

---

## Notes

- [P] = different files, no dependency.
- This feature changes **no** runtime/styling code (FR-010); the "live" verification is the **scaffold path** (T004/T017), per the standing early-live-smoke-run requirement adapted to a content-only feature.
- The parity engine discovers the shipped set dynamically — there is no hardcoded list to edit there; the *backstop* list (Feature 225) and the *catalog* (Feature 224) are the two human-maintained enumerations the new skill must join.
- **Profile set is `app`+`game`, not `app`+`game`+`sample-pack`.** The spec Assumptions float `sample-pack` as a possible third controls-bearing profile; the real `fs-gg-ui-widgets` wiring (T003) gates only `app`+`game`, so `fs-gg-styling` mirrors that and `sample-pack` is excluded by decision (T012). Both spec-named scene-only profiles (`headless-scene`, `governed`) are exercised in the scaffold checks (T004/T017), not just one representative.
- This is a **Tier 2 (content)** feature (declared in spec.md): no `.fs`/`.fsi` change, no surface-area baseline change — the only F# edit is the `expectedProductSkillIds` test-data list (T013). The plan's Constitution Check records why Principle I's FSI chain is N/A here.
- Delivery to consumers rides the next template republish + downstream re-pin; "merged on branch" ≠ "delivered" (T023).

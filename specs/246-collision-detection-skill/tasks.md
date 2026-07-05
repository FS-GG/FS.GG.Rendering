---
description: "Task list for Collision Detection Skill + Import-and-Adapt Helper Source"
---

# Tasks: Collision Detection Skill + Import-and-Adapt Helper Source

**Input**: Design documents from `/specs/246-collision-detection-skill/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: INCLUDED — required by Constitution Principle V (test evidence is mandatory) and research R7
(a coherence gate test + a collision-logic determinism/totality test).

**Organization**: Grouped by user story. US1 (adaptable source) and US2 (skill) are independent P1
slices; US3 (single-source-of-truth trim) depends on US2 existing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (Setup / Foundational / Polish carry no story label)
- All paths are repo-relative from `/home/developer/projects/FS.GG.Rendering/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the new directories and record the no-regression baseline.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Run EVERY test project (solution +
> `tests/Package.Tests` + `samples/**/*.Tests`) via the discovery-based runner so pre-existing reds
> (stale surface baselines, stale sample pins, missing-report failures) are known up front and not
> mistaken for regressions at merge.

- [x] T001 Create the new capability directories: `template/product-skills/fs-gg-collision/`, `template/fragments/collision/src/Product/`, and `.agents/skills/fs-gg-collision/` (empty placeholders; bodies authored later)
- [x] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/246-collision-detection-skill/readiness/baseline.md` (runs EVERY test project — solution + Package.Tests + samples — and records the full red/green set)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the delivery mechanism actually works on a real generated product, and draft the
seams both P1 stories build on, before authoring collision logic or skill prose.

**⚠️ CRITICAL**: No user story work begins until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** This is greenfield *additive* template surface,
> so the plan carries no defect root-cause hypotheses — but the *delivery mechanism* (a profile-gated,
> `Exists`-guarded compile item that materializes source into a real product and stays delete-safe) IS
> the load-bearing assumption. Prove it on a real render BEFORE building collision logic on top of it.

- [x] T003 Placement/registry map: confirm R2/R3 against the live files — that `capabilities.yml` is NOT the registry for skill-only capabilities (verify `fs-gg-game-core`/`fs-gg-audio` are absent from it), and that the six registry touch-points in `contracts/skill-and-registration.md` are exhaustive. Record in `specs/246-collision-detection-skill/readiness/placement-map.md`
- [x] T004 **Early live smoke run**: materialize a `game` product from the CURRENT template and `./fake.sh build -t Build` it green (pre-change baseline). Then prove the delivery mechanism: temporarily add a gated `<Compile Include="Smoke.fs" Condition="Exists('Smoke.fs')" />` + a throwaway `Smoke.fs`, confirm it compiles for `game`, is ABSENT for `app`, and that deleting `Smoke.fs` still builds (Exists guard). Record evidence (live, or `environment-limited` with disclosed substitute) in `specs/246-collision-detection-skill/readiness/mechanism-smoke.md`; revert the throwaway
- [x] T005 [P] Confirm test + evidence scaffolding both stories depend on: ensure `tests/Canvas.Tests/` and `tests/Package.Tests/` can host the new suites (`tests/Canvas.Tests` already references `FS.GG.UI.Canvas` + `FS.GG.UI.Scene`, so the raw `Collision.fs` compiles there; `YoloDev.Expecto.TestSdk` present); add empty `tests/Canvas.Tests/CollisionHelperTests.fs` and `tests/Package.Tests/Feature246CollisionSkillTests.fs` registered in their `.fsproj`. NOTE: `tests/Product.Tests/` does NOT exist in this framework repo (it is the *generated* product's project at `template/base/tests/Product.Tests/`) — collision-logic tests live in the framework's `tests/Canvas.Tests/`
- [x] T006 Draft the seams (compile-first): author `template/fragments/collision/src/Product/Collision.fs` with the module + `Body<'T>`/`Contact<'T>`/`Resolution<'T>`/`ResponseRule` types and `contact`/`collide`/`resolve`/`step` signatures as compiling stubs (`failwith "TODO"` bodies), per `contracts/collision-helper-source.md`; and create the `fs-gg-collision/SKILL.md` section skeleton (headings only) per `contracts/skill-and-registration.md`

**Checkpoint**: Delivery mechanism proven on a live game render; source + skill seams compile — US1 and US2 can proceed in parallel.

---

## Phase 3: User Story 1 - Scaffold a game and get adaptable collision source (Priority: P1) 🎯 MVP

**Goal**: A game/sample-pack product materializes a product-owned `Collision.fs` the consumer can edit,
that reports overlaps AND resolutions, reuses `Geometry`/`SpatialGrid`, is deterministic, total, and
delete-safe.

**Independent Test**: Scaffold a game product; `Collision.fs` is present, compiles, `step` returns
resolutions for overlapping bodies; edit the response rule → behavior changes; delete the file → still
builds and no gate fails (quickstart B–D).

### Tests for User Story 1 ⚠️ (write FIRST, ensure they FAIL)

- [x] T007 [P] [US1] Fill `tests/Canvas.Tests/CollisionHelperTests.fs` (compiles the raw `template/fragments/collision/src/Product/Collision.fs` — literal `namespace Product` — via a `<Compile Include>`; Canvas.Tests already refs Canvas+Scene): assert (a) two overlapping bodies → non-zero MTV that removes the overlap; (b) repeat-run BYTE-IDENTITY on a fixed multi-body scenario (determinism, FR-008); (c) degenerate totals — empty/singleton set, exact edge touch (NOT a contact), containment, zero-area, non-finite → documented values, never throws (FR-010). Tests fail against the T006 stubs

### Implementation for User Story 1

- [x] T008 [US1] Implement `template/fragments/collision/src/Product/Collision.fs`: `contact` (narrow-phase via `Geometry`, MTV + depth), `collide` (broad-phase via `SpatialGrid.build`/`query`, index-ordered pairs), `resolve` (apply `ResponseRule`), `step` (per-frame pass). Reuse shared `Rect`/`Point` only — no look-alike types (FR-002/FR-009); deterministic ordering by integer index, no hash-iteration/float-tie (FR-008); **response math stays sqrt-free** (MTV via min/subtraction; if a radius is ever needed, reuse `SpatialGrid.queryRadius`'s squared-distance approach) so IEEE-754 output is bit-identical cross-platform (FR-008/SC-004); total on degenerate input (FR-010). Mark the `resolve` rule as the editable line. Makes T007 pass
- [x] T009 [US1] Add the gated compile item to `template/base/src/Product/Product.fsproj` under the existing `(profile == "game" || profile == "sample-pack")` region: `<Compile Include="Collision.fs" Condition="Exists('Collision.fs')" />` (placed after `View.fs`, before `EvidenceCommands.fs`) — delete-safe (FR-007), scan-compatible
- [x] T010 [US1] Add the fragment source to `.template.config/template.json`: `{ "condition": "(profile == \"game\" || profile == \"sample-pack\")", "source": "template/fragments/collision/src/Product/", "target": "src/Product/" }` (no `copyOnly` → `sourceName` substitution runs) (FR-004)
- [x] T011 [P] [US1] Write `template/fragments/collision/README.md`: this is consumer-owned, adaptable source — yours to edit or delete; points at `fs-gg-collision` skill (mirror the `template/fragments/samples/README.md` tone)
- [x] T012 [US1] Update BOTH swap-guidance surfaces so the helper is reachable (FR-013): (a) `template/base/docs/scaffold-map.md` — classify `src/<ProductDir>/Collision.fs` as **replaceable/adaptable** (consumer-owned), noting the `Exists`-guarded delete-safety so deleting it will not break the durable `Product.fsproj`; (b) `template/product-skills/fs-gg-model-swap/SKILL.md` — add `Collision.fs` to the "Which files you touch" **Replaceable — rewrite freely** list, and clarify that `Product.fsproj` stays "durable — do not touch" even when deleting the helper (the compile item is `Exists`-guarded). NOTE: editing model-swap's body retriggers its manifest sha256 → regenerate in T016/T022
- [x] T013 [US1] Verify quickstart B–D on a real `game` render: source present + compiles; edit rule → separation behavior changes; `rm Collision.fs` → `Build` + `Verify` still green. Also assert `Collision.fs` carries **no must-survive governance-scan tokens** (it is absent from any source-scan allow-list) so edit/delete cannot trip a token gate (FR-007). Record in `specs/246-collision-detection-skill/readiness/us1-adaptable-source.md`

**Checkpoint**: US1 is a working MVP — a game product ships editable, delete-safe collision source with real response.

---

## Phase 4: User Story 2 - A dedicated collision skill (Priority: P1)

**Goal**: `fs-gg-collision` materializes for game/sample-pack (and only those), covering
detection→broad-phase→response and pointing at the adaptable helper; every skill/capability registry is
coherent.

**Independent Test**: A game/sample-pack render has `.agents/skills/fs-gg-collision/SKILL.md`; an
app/headless render does not; the skill body reuses `Geometry`/`SpatialGrid` and points at `Collision.fs`
(quickstart E); all coherence gates pass.

### Tests for User Story 2 ⚠️ (write FIRST, ensure they FAIL)

- [x] T014 [P] [US2] Fill `tests/Package.Tests/Feature246CollisionSkillTests.fs`: assert the manifest entry (id + sha256 + `materializes-when: profile in [game, sample-pack]`), both `template.json` sources present with a condition semantically equal to the manifest, the `skillist-reference.md` row, dev-root/mirror byte-parity, and profile gating (present for game/sample-pack, absent for app/headless). Fails until the registrations land

### Implementation for User Story 2

- [x] T015 [US2] Author `template/product-skills/fs-gg-collision/SKILL.md` (fill the T006 skeleton): frontmatter (`name`/`description`), Scope, Public Contract (bundled `Geometry`/`SpatialGrid` `.fsi` + the product-owned `Collision.fs`), Detection / Broad-phase / Response sections, "The adaptable helper" (yours to edit/delete), Common pitfalls (geometry-clash, O(n²) scan, float-tie ordering), Build/Test/Evidence/Package Boundary/Generated Product/Persistent problems, Related (`[[fs-gg-game-core]]`, `[[fs-gg-scene]]`, `[[fs-gg-skiaviewer]]`), Sources
- [x] T016 [US2] Add `fs-gg-collision` to the `catalog` list in `scripts/generate-skill-manifest.fsx` (alphabetical, between `fs-gg-audio` and `fs-gg-elmish`) with condition `(profile == \"game\" || profile == \"sample-pack\")`, then regenerate: `dotnet fsi scripts/generate-skill-manifest.fsx` → updated `template/skill-manifest/skill-manifest.json`
- [x] T017 [US2] Add the skill source to `.template.config/template.json`: `{ "condition": "(profile == \"game\" || profile == \"sample-pack\")", "source": "template/product-skills/fs-gg-collision/", "target": ".agents/skills/fs-gg-collision/", "copyOnly": ["**/*"] }` (near the other game/sample-pack skill sources)
- [x] T018 [P] [US2] Register `fs-gg-collision` in `template/base/docs/skillist-reference.md` (full-registry catalog row: id, materialize condition, one-line purpose — matching existing row format)
- [x] T019 [US2] Materialize dev roots: copy the canonical body to `.agents/skills/fs-gg-collision/SKILL.md`, run `dotnet fsi template/lifecycle/materialize-skill-roots.fsx` (produces the `.claude/skills/fs-gg-collision/` mirror), and `dotnet fsi scripts/check-agent-skill-parity.fsx` to assert `.claude ≡ .agents`. Makes T014 pass
- [x] T020 [US2] Verify quickstart E: a `game`/`sample-pack` render materializes the skill; an `app`/`headless-scene` render does not. Record in `specs/246-collision-detection-skill/readiness/us2-skill-gating.md`

**Checkpoint**: US1 AND US2 both work independently — adaptable source ships and a dedicated, gated skill guides it.

---

## Phase 5: User Story 3 - One source of truth: game-core points at the collision skill (Priority: P2)

**Goal**: `fs-gg-game-core`'s collision write-up becomes a pointer at `fs-gg-collision`; detailed
detection/broad-phase/response guidance lives in exactly one skill.

**Independent Test**: `fs-gg-game-core`'s `## Collision` is a pointer to `[[fs-gg-collision]]`; the
detailed guidance is not duplicated (quickstart F). Depends on US2 (the target skill must exist).

### Implementation for User Story 3

- [x] T021 [US3] Edit `template/product-skills/fs-gg-game-core/SKILL.md`: replace the `## Collision` body with a short pointer to `[[fs-gg-collision]]` (keep `## Culling` and `## Spatial queries` — they are not collision response); add `[[fs-gg-collision]]` to `## Related`
- [x] T022 [US3] Regenerate the manifest (game-core's sha256 changed — and model-swap's, from T012b): `dotnet fsi scripts/generate-skill-manifest.fsx`, then re-materialize + parity for the game-core and model-swap dev roots/mirrors (`template/lifecycle/materialize-skill-roots.fsx`, `scripts/check-agent-skill-parity.fsx`). Update the T014 assertion set if it pins game-core content
- [x] T023 [US3] Verify quickstart F: game-core's collision section is a pointer only; no duplicated detection/broad-phase/response prose. Record in `specs/246-collision-detection-skill/readiness/us3-single-source.md`

**Checkpoint**: All three stories independently functional; collision guidance has one authoritative home.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: End-to-end validation, evidence, and release preparation.

- [x] T024 [P] Author an FSI prelude transcript `scripts/collision-prelude.fsx` exercising `Collision.contact`/`collide`/`resolve`/`step` the way a game consumer would (route two overlapping bodies through a `step`, show separation); referenced from quickstart
- [x] T025 Run the full `quickstart.md` A–F end-to-end on a real `game` render and confirm every SC-001…SC-007 mapping holds; record the consolidated evidence under `specs/246-collision-detection-skill/readiness/`
- [x] T026 Re-run the baseline (`scripts/baseline-tests.fsx`) and diff against T002 — confirm ZERO new reds attributable to this feature (`./fake.sh build -t Test` + Package.Tests + samples)
- [ ] T027 [P] Capture per-phase feedback under `specs/246-collision-detection-skill/feedback/` (process friction, generalizable-code candidates, severity) if the feedback capability is active
- [ ] T028 **Release prep (Tier 1 template-contract change — do NOT flip until release)**: via the `cross-repo-coordination` skill, draft the publish-before-flip updates for the `fs-gg-ui-template` contract in `FS-GG/.github` — `registry/dependencies.yml` (contract version + consuming edge), `registry/CHANGELOG.md` (one dated newest-first entry), `docs/registry/compatibility.md` (dependency-graph + versioned-contracts + coherence rows), and the FS.GG.UI coherent-set bump. Stage as a coordination note; the actual flip happens at release (FR-014)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: depends on Setup — BLOCKS all user stories. T004 (mechanism smoke) gates everything.
- **US1 (Phase 3)** and **US2 (Phase 4)**: both depend only on Foundational; independent of each other — can run in parallel.
- **US3 (Phase 5)**: depends on **US2** (the `fs-gg-collision` skill must exist to point at it). Independent of US1.
- **Polish (Phase 6)**: depends on all desired stories. T022 (US3 manifest regen) and T016/T019 (US2 manifest/parity) both touch the manifest — serialize manifest regeneration (US2 before US3).

### Within Each User Story

- Tests written and FAILING before implementation (T007 before T008; T014 before T015–T019).
- US1: source logic (T008) before the gated compile item / template source (T009–T010) before verification (T013).
- US2: skill body (T015) before catalog/manifest/template/skillist/dev-root registration (T016–T019) before verification (T020).

### Parallel Opportunities

- Setup: T001 then T002 (T002 needs the tree; minor).
- Foundational: T005 [P] alongside T003/T004; T006 after T005.
- **US1 ∥ US2** once Foundational completes (different files: `Collision.fs`/`Product.fsproj`/fragment vs. `SKILL.md`/manifest/`template.json`/skillist). Note both edit `.template.config/template.json` (T010, T017) — serialize those two edits.
- Within US1: T011 [P] (README) alongside T008/T009. Within US2: T018 [P] (skillist) alongside T015/T016.
- Polish: T024 [P], T027 [P] parallel; T025/T026 after all stories.

---

## Parallel Example: after Foundational

```bash
# Two developers pick up the independent P1 slices:
Developer A → US1: implement Collision.fs (T008), gate the compile item (T009), fragment source (T010–T012)
Developer B → US2: author SKILL.md (T015), register in catalog/manifest/template/skillist/dev-roots (T016–T019)
# Coordinate the single shared file: .template.config/template.json (T010 and T017) — one edits, then the other.
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → Phase 2 Foundational (CRITICAL — includes the **early live mechanism smoke** T004).
2. Phase 3 US1 → **STOP and VALIDATE** (quickstart B–D): a game product ships editable, delete-safe collision source with working response.
3. Demo the MVP: scaffold a game, watch two bodies separate, edit the rule, delete the file — all green.

### Incremental Delivery

1. Setup + Foundational → mechanism proven on a live render.
2. US1 → adaptable source (MVP) → validate independently.
3. US2 → dedicated gated skill → validate independently.
4. US3 → trim game-core to a pointer → validate independently.
5. Polish → full quickstart, no-regression diff, release-prep coordination note.

---

## Notes

- [P] = different files, no incomplete-task dependency. The two `.template.config/template.json` edits
  (T010, T017) are NOT [P] with each other.
- This feature adds **no** framework package public surface / `.fsi`, so there is **no** surface-area
  baseline task (contrast Feature 245).
- Determinism (FR-008), totality (FR-010), reuse-not-rewrite (FR-002/FR-009), and delete-safety
  (FR-007) are the load-bearing invariants — verified by T007 and quickstart D, not assumed.
- Commit after each task or logical group. Do NOT flip the cross-repo registry (T028) until release.

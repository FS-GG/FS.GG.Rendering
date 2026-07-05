---
description: "Task list for 2D Visibility Skill + Import-and-Adapt Helper Source"
---

# Tasks: 2D Visibility Skill + Import-and-Adapt Helper Source

**Input**: Design documents from `/specs/247-2d-visibility-skill/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: INCLUDED — required by Constitution Principle V (test evidence is mandatory) and research R8
(a coherence gate test + a visibility-logic occlusion/determinism/totality test).

**Organization**: Grouped by user story. US1 (adaptable source) and US2 (skill + coherent registration)
are independent P1 slices; US3 (catalog coherence + swap-guidance reach) depends on US2 existing.

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
> mistaken for regressions at merge. Package.Tests + samples are excluded from `FS.GG.Rendering.slnx`,
> which is exactly where the count-gate edits in this feature will surface — baseline them now.

- [ ] T001 Create the new capability directories: `template/product-skills/fs-gg-visibility/`, `template/fragments/visibility/src/Product/`, and `.agents/skills/fs-gg-visibility/` (empty placeholders; bodies authored later)
- [ ] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/247-2d-visibility-skill/readiness/baseline.md` (runs EVERY test project — solution + Package.Tests + samples — and records the full red/green set)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the delivery mechanism works on a real generated product, lock the exact
gate-enforced registration set, and draft the seams both P1 stories build on — before authoring
visibility logic or skill prose.

**⚠️ CRITICAL**: No user story work begins until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** This is greenfield *additive* template surface,
> so the plan carries no defect root-cause hypotheses — but the *delivery mechanism* (a profile-gated,
> `Exists`-guarded compile item that materializes source into a real product and stays delete-safe) IS
> the load-bearing assumption. Prove it on a real render BEFORE building visibility logic on top of it.

- [ ] T003 Placement/registry map: confirm R2/R3 against the LIVE files. Verify (a) `capabilities.yml` and `template/base/docs/skillist-reference.md` do NOT list skill-only capabilities (`grep -i collision` in both is empty → visibility touches neither); (b) the full gate-enforced coherent set from `contracts/skill-and-registration.md` §5 is exhaustive — `canonicalSources` in `Feature231SkillManifestTests.fs` + `Feature238SkillMaterializesWhenTests.fs`, the framework count in `Feature204LifecycleTemplateTests.fs` (currently 15), the per-profile sets in `Feature219EmitFrameworkSkillsTests.fs`, and `frameworkChecked` in `scripts/validate-lifecycle-template.fsx` (currently 15); (c) the `fs-gg-product-<x>` dev wrapper exists for the sibling skills. Record in `specs/247-2d-visibility-skill/readiness/placement-map.md`
- [ ] T004 **Early live smoke run**: materialize a `game` product from the CURRENT template and `./fake.sh build -t Build` it green (pre-change baseline). Then prove the delivery mechanism: temporarily add a gated `<Compile Include="Smoke.fs" Condition="Exists('Smoke.fs')" />` + a throwaway `Smoke.fs`, confirm it compiles for `game`, is ABSENT for `app`, and that deleting `Smoke.fs` still builds (Exists guard). Record evidence (live, or `environment-limited` with disclosed substitute) in `specs/247-2d-visibility-skill/readiness/mechanism-smoke.md`; revert the throwaway
- [ ] T005 [P] Confirm test + evidence scaffolding both stories depend on: `tests/Canvas.Tests/` already references `FS.GG.UI.Canvas` + `FS.GG.UI.Scene`, so the raw `Visibility.fs` compiles there (`YoloDev.Expecto.TestSdk` present); add empty `tests/Canvas.Tests/VisibilityHelperTests.fs` and `tests/Package.Tests/Feature247VisibilitySkillTests.fs` registered in their `.fsproj`. NOTE: `tests/Product.Tests/` does NOT exist in this framework repo (it is the *generated* product's project) — visibility-logic tests live in `tests/Canvas.Tests/`
- [ ] T006 Draft the seams (compile-first): author `template/fragments/visibility/src/Product/Visibility.fs` with the module + `Segment`/`Settings`/`VisibilityPolygon` types and `raySegment`/`isVisible`/`polygon` signatures as compiling stubs (`failwith "TODO"` bodies), per `contracts/visibility-helper-source.md`; and create the `fs-gg-visibility/SKILL.md` section skeleton (headings only) per `contracts/skill-and-registration.md` §1

**Checkpoint**: Delivery mechanism proven on a live game render; the exact gate set is locked; source + skill seams compile — US1 and US2 can proceed in parallel.

---

## Phase 3: User Story 1 - Scaffold a game and get adaptable visibility source (Priority: P1) 🎯 MVP

**Goal**: A game/sample-pack product materializes a product-owned `Visibility.fs` the consumer can edit,
that produces a bounded visibility polygon (region visible from a source, occluders excluded), reuses
`Point`/`SpatialGrid`, is deterministic, total, bounded, and delete-safe.

**Independent Test**: Scaffold a game product; `Visibility.fs` is present, compiles, `polygon` returns a
closed bounded polygon that excludes the region behind a wall; edit `Settings.Radius` → visible region
changes; delete the file → still builds and no gate fails (quickstart B–D).

### Tests for User Story 1 ⚠️ (write FIRST, ensure they FAIL)

- [ ] T007 [P] [US1] Fill `tests/Canvas.Tests/VisibilityHelperTests.fs` (compiles the raw `template/fragments/visibility/src/Product/Visibility.fs` — literal `namespace Product` — via a `<Compile Include>`; Canvas.Tests already refs Canvas+Scene): assert (a) OCCLUSION — a target behind a wall segment is NOT visible (`isVisible` false / outside the polygon) and IS visible with the wall removed (FR-006); (b) BOUNDED — `polygon` returns a closed ring whose vertices lie within `source ± Radius` even with an empty segment set (FR-011); (c) DETERMINISM — repeat-run BYTE-IDENTITY of `Vertices` on a fixed scenario that includes equal-angle endpoints (shared corners/collinear walls) (FR-008); (d) TOTALS — empty segment set, zero-length segment, source on a wall/endpoint, collinear/near-parallel grazing ray, non-finite coords, non-positive radius → documented values, never throws, never NaN (FR-010). Tests fail against the T006 stubs
- [ ] T008 [US1] Implement `template/fragments/visibility/src/Product/Visibility.fs`: `raySegment` (sqrt-free parametric ray-segment intersection, `None` on parallel/behind/non-finite), `isVisible` (point line-of-sight built on `raySegment`), `polygon` (broad-phase cull via `SpatialGrid.build`/`queryRadius` at `Settings.Radius`; add the four `source ± Radius` bound-box edges as synthetic walls; order endpoints by a **cross-product angular comparator — NOT `atan2`** with a sqrt-free squared-distance then integer-index tiebreak; sweep the nearest crossing segment per wedge via the parametric `t`; emit the closed CCW `VisibilityPolygon`). Reuse shared `Point`/`Rect` only — no look-alike types; `Segment` is a pair of shared `Point`s (FR-002/FR-009); deterministic ordering, no hash-iteration/`atan2`/`sqrt` in the ordering or nearest-hit so IEEE-754 output is bit-identical cross-platform (FR-008/SC-004); every ray bounded (FR-011); total on degenerate input (FR-010). Mark `Settings` + the sweep body as the editable lines. Makes T007 pass
- [ ] T009 [US1] Add the gated compile item to `template/base/src/Product/Product.fsproj` under the existing `(profile == "game" || profile == "sample-pack")` region, alongside `Collision.fs` and before `Model.fs`: `<Compile Include="Visibility.fs" Condition="Exists('Visibility.fs')" />` — delete-safe (FR-007), compile-order-scan compatible (anchors on the literal `Compile Include="X.fs"`)
- [ ] T010 [US1] Add the fragment source to `.template.config/template.json`: `{ "condition": "(profile == \"game\" || profile == \"sample-pack\")", "source": "template/fragments/visibility/src/Product/", "target": "src/Product/" }` (no `copyOnly` → `sourceName` substitution runs) (FR-004)
- [ ] T011 [P] [US1] Write `template/fragments/visibility/README.md`: consumer-owned, adaptable source — yours to edit (radius, FOV cone, polygon-vs-mask output) or delete; points at the `fs-gg-visibility` skill; cites the Red Blob Games article (mirror the `template/fragments/collision/README.md` tone)
- [ ] T012 [US1] Verify quickstart B–D on a real `game` render: source present + compiles; `polygon` excludes the region behind a wall; edit `Settings.Radius` → visible region changes; `rm Visibility.fs` → `Build` + `Verify` still green. Assert `Visibility.fs` carries **no must-survive governance-scan tokens** (absent from any source-scan allow-list) so edit/delete cannot trip a token gate (FR-007). Record in `specs/247-2d-visibility-skill/readiness/us1-adaptable-source.md`

**Checkpoint**: US1 is a working MVP — a game product ships editable, delete-safe visibility source that produces a real occlusion-correct polygon.

---

## Phase 4: User Story 2 - A dedicated 2D-visibility skill (Priority: P1)

**Goal**: `fs-gg-visibility` materializes for game/sample-pack (and only those), covering the segment
model → cull → angular sweep → polygon and applications, cites the Red Blob Games reference, and points at
the adaptable helper; the skill registers coherently across the full gate-enforced set.

**Independent Test**: A game/sample-pack render has `.agents/skills/fs-gg-visibility/SKILL.md`; an
app/headless render does not; the body reuses `Point`/`SpatialGrid` and points at `Visibility.fs`
(quickstart E); all coherence gates pass.

### Tests for User Story 2 ⚠️ (write FIRST, ensure they FAIL)

- [ ] T013 [P] [US2] Fill `tests/Package.Tests/Feature247VisibilitySkillTests.fs`: assert the manifest entry (id + sha256 + `materializes-when: profile in [game, sample-pack]`), both `template.json` sources present with a condition semantically equal to the manifest, dev-root + `fs-gg-product-visibility` wrapper + `.claude` mirror byte-parity, and profile gating (present for game/sample-pack, absent for app/headless). Fails until the registrations land

### Implementation for User Story 2

- [ ] T014 [US2] Author `template/product-skills/fs-gg-visibility/SKILL.md` (fill the T006 skeleton) per `contracts/skill-and-registration.md` §1: frontmatter (`name: fs-gg-visibility` + description), Scope, Public Contract (bundled `Point`/`Rect`/`Geometry` + `SpatialGrid` `.fsi` + the product-owned `Visibility.fs`), The world model / Broad-phase cull / The angular sweep / The visibility polygon / Applications (line-of-sight, FOV cone, fog-of-war mask, 2D light) sections, "The adaptable helper" (yours to edit/delete), Common pitfalls (geometry-clash, `atan2` last-bit non-determinism vs the cross-product comparator, O(segments) scan without the cull, unbounded rays / forgetting the radius bound), Build/Test/Evidence/Package Boundary/Generated Product/Persistent problems, Related (`[[fs-gg-collision]]`, `[[fs-gg-game-core]]`, `[[fs-gg-scene]]`, `[[fs-gg-skiaviewer]]`), Sources (the Red Blob Games visibility article)
- [ ] T015 [US2] Add `fs-gg-visibility` to the `catalog` list in `scripts/generate-skill-manifest.fsx` (after `fs-gg-ui-widgets`) with condition `(profile == \"game\" || profile == \"sample-pack\")`, then regenerate: `dotnet fsi scripts/generate-skill-manifest.fsx` → updated `template/skill-manifest/skill-manifest.json`
- [ ] T016 [US2] Add the skill source to `.template.config/template.json`: `{ "condition": "(profile == \"game\" || profile == \"sample-pack\")", "source": "template/product-skills/fs-gg-visibility/", "target": ".agents/skills/fs-gg-visibility/", "copyOnly": ["**/*"] }` (near the collision skill source)
- [ ] T017 [US2] Update the gate-enforced coherence set together so the build stays green (contracts §5): add `"fs-gg-visibility", "template/product-skills/fs-gg-visibility/SKILL.md"` to `canonicalSources` in BOTH `tests/Package.Tests/Feature231SkillManifestTests.fs` and `tests/Package.Tests/Feature238SkillMaterializesWhenTests.fs`; bump the framework product-skill count `15 → 16` in `tests/Package.Tests/Feature204LifecycleTemplateTests.fs`; add `"fs-gg-visibility"` to BOTH the `game` and `sample-pack` `expectedFrameworkSkills` sets in `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs`; and set `frameworkChecked = 16` in `scripts/validate-lifecycle-template.fsx`
- [ ] T018 [US2] Materialize dev roots + wrapper: copy the canonical body to `.agents/skills/fs-gg-visibility/SKILL.md`; author the thin `fs-gg-product-visibility` wrapper in BOTH `.agents/skills/fs-gg-product-visibility/SKILL.md` (Codex-active) and `.claude/skills/fs-gg-product-visibility/SKILL.md` (Claude-active) — frontmatter `name: fs-gg-product-visibility` + the canonical description, body pointing at `../../../template/product-skills/fs-gg-visibility/SKILL.md`; run `dotnet fsi template/lifecycle/materialize-skill-roots.fsx` (produces the `.claude/skills/fs-gg-visibility/` mirror); then `dotnet run --project tools/Rendering.Harness -- skill-parity` (0 findings) and re-commit the regenerated `docs/reports/skills-parity.md`. Makes T013 pass
- [ ] T019 [US2] Verify quickstart E: a `game`/`sample-pack` render materializes the skill + `Visibility.fs`; an `app`/`headless-scene` render materializes neither. Run `dotnet test tests/Package.Tests` (Feature247 + the 231/238/204/219 count gates) and `dotnet test tests/Rendering.Harness.Tests` (Deterministic skill-inventory/parity) green. Record in `specs/247-2d-visibility-skill/readiness/us2-skill-gating.md`

**Checkpoint**: US1 AND US2 both work independently — adaptable source ships and a dedicated, gated, coherently-registered skill guides it.

---

## Phase 5: User Story 3 - Catalog coherence & swap-guidance reach (Priority: P2)

**Goal**: The visibility helper is listed in the scaffold's swap/adapt taxonomy as consumer-owned
replaceable source, and every skill/capability coherence gate passes with zero drift.

**Independent Test**: The coherence gates pass (0 drift); `fs-gg-model-swap` and `scaffold-map.md` list
`Visibility.fs` as consumer-owned replaceable source, next to `Collision.fs` (quickstart F). Depends on
US2 (the skill + registration must exist).

### Implementation for User Story 3

- [ ] T020 [US3] Update BOTH swap-guidance surfaces so the helper is reachable (FR-013): (a) `template/base/docs/scaffold-map.md` — classify `src/<ProductDir>/Visibility.fs` as **replaceable/adaptable** (consumer-owned), next to the `Collision.fs` entry, noting it compiles before `Model.fs` and that its `Exists`-guarded compile item keeps the durable `Product.fsproj` safe on deletion; (b) `template/product-skills/fs-gg-model-swap/SKILL.md` — add `src/<ProductDir>/Visibility.fs` to the "Which files you touch" **Replaceable — rewrite freely** list (next to `Collision.fs`), linking `[[fs-gg-visibility]]`
- [ ] T021 [US3] Regenerate the manifest (model-swap's body changed in T020 → its sha256 changed): `dotnet fsi scripts/generate-skill-manifest.fsx`, then re-materialize + parity for the model-swap dev root/mirror (`template/lifecycle/materialize-skill-roots.fsx`, `dotnet run --project tools/Rendering.Harness -- skill-parity`). Re-run `Feature247VisibilitySkillTests` + `Feature231/238` if they pin any body digest
- [ ] T022 [US3] Verify quickstart F + full coherence: `dotnet fsi scripts/validate-lifecycle-template.fsx` clean; `dotnet test tests/Package.Tests` green (0 registry drift); `grep -c "Visibility.fs"` in both swap surfaces ≥ 1. Record in `specs/247-2d-visibility-skill/readiness/us3-coherence-swap.md`

**Checkpoint**: All three stories independently functional; visibility is a coherent, discoverable, consumer-owned capability.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: End-to-end validation, evidence, and release preparation.

- [ ] T023 [P] Author an FSI prelude transcript `scripts/visibility-prelude.fsx` exercising `Visibility.raySegment`/`isVisible`/`polygon` the way a game consumer would (a source with one wall between it and a target: show the target hidden, then visible with the wall removed; print the polygon vertex ring); referenced from quickstart
- [ ] T024 Run the full `quickstart.md` A–F end-to-end on a real `game` render and confirm every SC-001…SC-008 mapping holds; record the consolidated evidence under `specs/247-2d-visibility-skill/readiness/`
- [ ] T025 Re-run the baseline (`scripts/baseline-tests.fsx`) and diff against T002 — confirm ZERO new reds attributable to this feature (`./fake.sh build -t Test` + Package.Tests + samples)
- [ ] T026 [P] Capture per-phase feedback under `specs/247-2d-visibility-skill/feedback/` (process friction, generalizable-code candidates, severity) if the feedback capability is active
- [ ] T027 **Release prep (Tier 1 template-contract change — do NOT flip until release)**: via the `cross-repo-coordination` skill, draft the publish-before-flip updates for the `fs-gg-ui-template` contract in `FS-GG/.github` — `registry/dependencies.yml` (contract version + consuming edge), `registry/CHANGELOG.md` (one dated newest-first entry), `docs/registry/compatibility.md` (dependency-graph + versioned-contracts + coherence rows), and the FS.GG.UI coherent-set bump. Stage as a coordination note; the actual flip happens at release (FR-014)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: depends on Setup — BLOCKS all user stories. T004 (mechanism smoke) gates everything.
- **US1 (Phase 3)** and **US2 (Phase 4)**: both depend only on Foundational; independent of each other — can run in parallel.
- **US3 (Phase 5)**: depends on **US2** (the skill + registration must exist and the manifest must be populated before the model-swap edit re-triggers a regen). Independent of US1.
- **Polish (Phase 6)**: depends on all desired stories.

### Within Each User Story

- Tests written and FAILING before implementation (T007 before T008; T013 before T014–T018).
- US1: source logic (T008) before the gated compile item / template source (T009–T010) before verification (T012).
- US2: skill body (T014) before catalog/manifest/template/gate-set/dev-root registration (T015–T018) before verification (T019). **T015 (manifest add) and T017 (canonicalSources + count bumps) MUST land together** — adding the catalog entry without the gate edits reds `Feature231/238/204/219`.

### Parallel Opportunities

- Setup: T001 then T002 (T002 needs the tree).
- Foundational: T005 [P] alongside T003/T004; T006 after T005.
- **US1 ∥ US2** once Foundational completes (different files: `Visibility.fs`/`Product.fsproj`/fragment vs. `SKILL.md`/manifest/gate-tests/dev-roots). Note both edit `.template.config/template.json` (T010, T016) — serialize those two edits.
- Within US1: T011 [P] (README) alongside T008/T009. Within US2: T013 [P] (coherence test) alongside T014.
- Polish: T023 [P], T026 [P] parallel; T024/T025 after all stories.

---

## Parallel Example: after Foundational

```bash
# Two developers pick up the independent P1 slices:
Developer A → US1: implement Visibility.fs (T008), gate the compile item (T009), fragment source (T010–T011)
Developer B → US2: author SKILL.md (T014), register in catalog/manifest/template/gate-set/dev-roots+wrapper (T015–T018)
# Coordinate the single shared file: .template.config/template.json (T010 and T016) — one edits, then the other.
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → Phase 2 Foundational (CRITICAL — includes the **early live mechanism smoke** T004).
2. Phase 3 US1 → **STOP and VALIDATE** (quickstart B–D): a game product ships editable, delete-safe visibility source with a real occlusion-correct polygon.
3. Demo the MVP: scaffold a game, compute a light behind a wall, shrink the radius, delete the file — all green.

### Incremental Delivery

1. Setup + Foundational → mechanism proven on a live render.
2. US1 → adaptable source (MVP) → validate independently.
3. US2 → dedicated gated skill + coherent registration → validate independently.
4. US3 → swap-guidance reach + full coherence → validate independently.
5. Polish → full quickstart, no-regression diff, release-prep coordination note.

---

## Notes

- [P] = different files, no incomplete-task dependency. The two `.template.config/template.json` edits
  (T010, T016) are NOT [P] with each other.
- This feature adds **no** framework package public surface / `.fsi`, so there is **no** surface-area
  baseline task (contrast Feature 245).
- **Divergence from the collision (246) task set, deliberate:** (a) NO `skillist-reference.md` and NO
  `capabilities.yml` edit — skill-only capabilities appear in neither (verified against `fs-gg-collision`);
  (b) NO game-core trim — there is no pre-existing visibility write-up to consolidate; (c) the
  `canonicalSources`/count-bump gate edits (T017) and the `fs-gg-product-visibility` wrapper (T018) ARE
  required and are called out explicitly (see [[adding-a-product-skill-touchpoints]]).
- Determinism (FR-008 — cross-product comparator, no `atan2`; sqrt-free `t`), boundedness (FR-011),
  totality (FR-010), reuse-not-rewrite (FR-002/FR-009), and delete-safety (FR-007) are the load-bearing
  invariants — verified by T007 and quickstart D, not assumed.
- Commit after each task or logical group. Do NOT flip the cross-repo registry (T027) until release.

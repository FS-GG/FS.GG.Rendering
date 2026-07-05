---
description: "Task list for Grid-Parts Skill + Import-and-Adapt Helper Source"
---

# Tasks: Grid-Parts Skill + Import-and-Adapt Helper Source

**Input**: Design documents from `/specs/249-grid-parts-skill/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: INCLUDED — required by Constitution Principle V (test evidence is mandatory) and research R8
(a coherence gate test + a grid-parts adjacency/pixel round-trip/determinism/totality test).

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

- [ ] T001 Create the new capability directories: `template/product-skills/fs-gg-grids/`, `template/fragments/grids/src/Product/`, and `.agents/skills/fs-gg-grids/` (empty placeholders; bodies authored later)
- [ ] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/249-grid-parts-skill/readiness/baseline.md` (runs EVERY test project — solution + Package.Tests + samples — and records the full red/green set)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the delivery mechanism works on a real generated product, lock the exact
gate-enforced registration set, and draft the seams both P1 stories build on — before authoring
grid-parts logic or skill prose.

**⚠️ CRITICAL**: No user story work begins until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** This is greenfield *additive* template surface,
> so the plan carries no defect root-cause hypotheses — but the *delivery mechanism* (a profile-gated,
> `Exists`-guarded compile item that materializes source into a real product via `sourceName` fileRename
> and stays delete-safe) IS the load-bearing assumption. Prove it on a real render BEFORE building
> grid-parts logic on top of it — including that the fragment lands at `src/<ProductDir>/Grids.fs`
> (namespace + path rewritten), NOT orphaned in a literal `src/Product/`.

- [ ] T003 Placement/registry map: confirm R2/R3 against the LIVE files. Verify (a) `capabilities.yml` and `template/base/docs/skillist-reference.md` do NOT list skill-only capabilities (`grep -i visibility` in both is empty → grids touches neither); (b) the full gate-enforced coherent set from `contracts/skill-and-registration.md` §5 is exhaustive — `canonicalSources` in `Feature231SkillManifestTests.fs` + `Feature238SkillMaterializesWhenTests.fs`, the framework count in `Feature204LifecycleTemplateTests.fs` (currently 16), the per-profile sets in `Feature219EmitFrameworkSkillsTests.fs`, and `frameworkChecked` in `scripts/validate-lifecycle-template.fsx` (currently 16); (c) the `fs-gg-product-<x>` dev wrapper exists for the sibling skills (collision/visibility). Record in `specs/249-grid-parts-skill/readiness/placement-map.md`
- [ ] T004 **Early live smoke run**: materialize a `game` product from the CURRENT template and `./fake.sh build -t Build` it green (pre-change baseline). Then prove the delivery mechanism AND the fragment-target fix: temporarily add a gated `<Compile Include="Smoke.fs" Condition="Exists('Smoke.fs')" />` + a throwaway `template/fragments/smoke/src/Product/Smoke.fs` wired with `source .../src/`, `target src/`, confirm it lands at `src/<ProductDir>/Smoke.fs` (namespace rewritten, NOT orphaned in `src/Product/`), compiles for `game`, is ABSENT for `app`, and that deleting `Smoke.fs` still builds (Exists guard). Record evidence (live, or `environment-limited` with disclosed substitute) in `specs/249-grid-parts-skill/readiness/mechanism-smoke.md`; revert the throwaway
- [ ] T005 [P] Confirm test + evidence scaffolding both stories depend on: `tests/Canvas.Tests/` already references `FS.GG.UI.Canvas` + `FS.GG.UI.Scene`, so the raw `Grids.fs` compiles there (`YoloDev.Expecto.TestSdk` + FsCheck present); add empty `tests/Canvas.Tests/GridsHelperTests.fs` and `tests/Package.Tests/Feature249GridsSkillTests.fs` registered in their `.fsproj`. NOTE: `tests/Product.Tests/` does NOT exist in this framework repo (it is the *generated* product's project) — grid-parts logic tests live in `tests/Canvas.Tests/`
- [ ] T006 Draft the seams (compile-first): author `template/fragments/grids/src/Product/Grids.fs` with the module + `EdgeOrientation`/`Edge`/`Vertex`/`GridSpec` types and the six adjacency + six pixel-mapping signatures as compiling stubs (`failwith "TODO"` bodies), per `contracts/grids-helper-source.md`; and create the `fs-gg-grids/SKILL.md` section skeleton (headings only) per `contracts/skill-and-registration.md` §1

**Checkpoint**: Delivery mechanism proven on a live game render (fragment lands at `src/<ProductDir>/`); the exact gate set is locked; source + skill seams compile — US1 and US2 can proceed in parallel.

---

## Phase 3: User Story 1 - Scaffold a game and get adaptable grid-parts source (Priority: P1) 🎯 MVP

**Goal**: A game/sample-pack product materializes a product-owned `Grids.fs` the consumer can edit, that
addresses the grid parts (faces/edges/vertices), converts between them (the six adjacency conversions),
and maps every part to/from pixels; reuses `Cell`/`Point`/`Rect`; is deterministic, total, round-tripping,
and delete-safe.

**Independent Test**: Scaffold a game product; `Grids.fs` is present at `src/<ProductDir>/`, compiles;
feed a cell → its four edges and four corners; take one edge → the two cells it separates (one is the
original); round-trip a cell through `cellCenter`→`cellAt` and get it back; edit `GridSpec` → pixel
positions change; delete the file → still builds and no gate fails (quickstart B–D).

### Tests for User Story 1 ⚠️ (write FIRST, ensure they FAIL)

- [ ] T007 [P] [US1] Fill `tests/Canvas.Tests/GridsHelperTests.fs` (compiles the raw `template/fragments/grids/src/Product/Grids.fs` — literal `namespace Product` — via a `<Compile Include>`; Canvas.Tests already refs Canvas+Scene): FsCheck properties for (a) ADJACENCY ROUND-TRIP — every edge in `cellEdges c` reports `c` in its `edgeCells`, every corner in `cellCorners c` reports `c` in its `vertexCells`; list lengths are 4 (`cellEdges`/`cellCorners`/`vertexCells`/`vertexEdges`) and 2 (`edgeCells`/`edgeVertices`) (FR-009); (b) CANONICAL EDGE — two references to the same boundary are equal `Edge` records (FR-006); (c) PIXEL ROUND-TRIP — `cellAt spec (cellCenter spec c) = c` for random cells/specs (FR-010); (d) DETERMINISM — repeat-run BYTE-IDENTITY of every conversion + pixel map on a fixed scenario (FR-008); (e) TOTALS — non-finite / non-positive `CellSize`, non-finite `Origin`/point → documented fallbacks (`1.0`/`0.0`/cell `0`), never throws, never NaN (FR-010). Tests fail against the T006 stubs
- [ ] T008 [US1] Implement `template/fragments/grids/src/Product/Grids.fs`: the six adjacency conversions (pure integer arithmetic, fixed list order — `cellCorners`/`vertexCells` TL/TR/BR/BL, `cellEdges` top/right/bottom/left, `vertexEdges` up/right/down/left, `edgeCells`/`edgeVertices` per contract) and the pixel mapping (`cellRect`/`cellCenter`/`vertexPoint`/`edgeSegment`/`edgeMidpoint`/`cellAt`) with the non-finite guards (`safeCellSize`→1.0, `safeOriginX/Y`→0.0, `cellAt` non-finite axis→0). Reuse shared `Cell`/`Point`/`Rect` only — no look-alike types; `Edge`/`Vertex`/`GridSpec` are the only new shapes (FR-006/SC-005); one canonical name per edge (FR-006); adjacency round-trips (FR-009); no hash-iteration/`atan2`/`sqrt` so integer parts + straight-line pixels are bit-identical cross-platform (FR-008/SC-004); total on degenerate input (FR-010). Mark `GridSpec` + the adjacency/pixel bodies as the editable lines. Makes T007 pass
- [ ] T009 [US1] Add the gated compile item to `template/base/src/Product/Product.fsproj` under the existing `(profile == "game" || profile == "sample-pack")` region, alongside `Collision.fs`/`Visibility.fs` and before `Model.fs`: `<Compile Include="Grids.fs" Condition="Exists('Grids.fs')" />` — delete-safe (FR-007), compile-order-scan compatible (anchors on the literal `Compile Include="X.fs"`)
- [ ] T010 [US1] Add the fragment source to `.template.config/template.json`: `{ "condition": "(profile == \"game\" || profile == \"sample-pack\")", "source": "template/fragments/grids/src/", "target": "src/" }` (no `copyOnly` → `sourceName` substitution runs; `target src/` NOT `src/Product/` so the `Product/` segment fileRenames to `src/<ProductDir>/` — the Feature 246→247 fix, [[fragment-target-sourcename-rename]]) (FR-004)
- [ ] T011 [P] [US1] Write `template/fragments/grids/README.md`: consumer-owned, adaptable source — yours to edit (origin, cell size, corner order, diagonal-edge variant, hex extension) or delete; points at the `fs-gg-grids` skill; cites the two Red Blob Games references (mirror the `template/fragments/visibility/README.md` tone)
- [ ] T012 [US1] Verify quickstart B–D on a real `game` render: source present at `src/<ProductDir>/Grids.fs` + compiles; `cellEdges`/`cellCorners` + `edgeCells` round-trip; `cellAt (cellCenter c) = c`; edit `GridSpec` → pixel positions change; `rm Grids.fs` → `Build` + `Verify` still green. Assert `Grids.fs` carries **no must-survive governance-scan tokens** (absent from any source-scan allow-list) so edit/delete cannot trip a token gate (FR-007). Record in `specs/249-grid-parts-skill/readiness/us1-adaptable-source.md`

**Checkpoint**: US1 is a working MVP — a game product ships editable, delete-safe grid-parts source with real, round-tripping adjacency and pixel conversions.

---

## Phase 4: User Story 2 - A dedicated grid-parts skill (Priority: P1)

**Goal**: `fs-gg-grids` materializes for game/sample-pack (and only those), covering the parts vocabulary
→ canonical coordinates → adjacency conversions → pixel mapping and applications, cites the two Red Blob
Games references, and points at the adaptable helper; the skill registers coherently across the full
gate-enforced set.

**Independent Test**: A game/sample-pack render has `.agents/skills/fs-gg-grids/SKILL.md`; an
app/headless render does not; the body reuses `Cell`/`Point`/`Rect` and points at `Grids.fs` (quickstart
E); all coherence gates pass.

### Tests for User Story 2 ⚠️ (write FIRST, ensure they FAIL)

- [ ] T013 [P] [US2] Fill `tests/Package.Tests/Feature249GridsSkillTests.fs`: assert the manifest entry (id + sha256 + `materializes-when: profile in [game, sample-pack]`), both `template.json` sources present with a condition semantically equal to the manifest (the fragment source is `source .../grids/src/`, `target src/`, no `copyOnly`), dev-root + `fs-gg-product-grids` wrapper + `.claude` mirror byte-parity, and profile gating (present for game/sample-pack, absent for app/headless). Fails until the registrations land

### Implementation for User Story 2

- [ ] T014 [US2] Author `template/product-skills/fs-gg-grids/SKILL.md` (fill the T006 skeleton) per `contracts/skill-and-registration.md` §1: frontmatter (`name: fs-gg-grids` + description), Scope, Public Contract (bundled `Cell` `.fsi` + `Point`/`Rect` `.fsi` + the product-owned `Grids.fs`), The parts of a grid / Canonical coordinates / Adjacency conversions / Pixel mapping / Applications (edge-walls, autotiling / marching-squares, region borders, snapping) sections, "The adaptable helper" (yours to edit/delete), Common pitfalls (look-alike `Cell`/`Point` type, two names for one edge, edge-orientation confusion, off-by-one corner/cell indexing, deleting the file with the `Exists` guard understood), Build/Test/Evidence/Package Boundary/Generated Product/Persistent problems, Related (`[[fs-gg-collision]]`, `[[fs-gg-visibility]]`, `[[fs-gg-game-core]]`, `[[fs-gg-scene]]`, `[[fs-gg-skiaviewer]]`), Sources (the two Red Blob Games references: "Parts of a grid" and "Grid edges")
- [ ] T015 [US2] Add `fs-gg-grids` to the `catalog` list in `scripts/generate-skill-manifest.fsx` **alphabetically** (after `fs-gg-game-core`, before `fs-gg-keyboard-input`) with condition `(profile == \"game\" || profile == \"sample-pack\")`, then regenerate: `dotnet fsi scripts/generate-skill-manifest.fsx` → updated `template/skill-manifest/skill-manifest.json`
- [ ] T016 [US2] Add the skill source to `.template.config/template.json`: `{ "condition": "(profile == \"game\" || profile == \"sample-pack\")", "source": "template/product-skills/fs-gg-grids/", "target": ".agents/skills/fs-gg-grids/", "copyOnly": ["**/*"] }` (near the visibility skill source)
- [ ] T017 [US2] Update the gate-enforced coherence set together so the build stays green (contracts §5): add `"fs-gg-grids", "template/product-skills/fs-gg-grids/SKILL.md"` to `canonicalSources` in BOTH `tests/Package.Tests/Feature231SkillManifestTests.fs` and `tests/Package.Tests/Feature238SkillMaterializesWhenTests.fs`; bump the framework product-skill count `16 → 17` in `tests/Package.Tests/Feature204LifecycleTemplateTests.fs`; add `"fs-gg-grids"` to BOTH the `game` and `sample-pack` `expectedFrameworkSkills` sets in `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs`; and set `frameworkChecked = 17` in `scripts/validate-lifecycle-template.fsx`
- [ ] T018 [US2] Materialize dev roots + wrapper: copy the canonical body to `.agents/skills/fs-gg-grids/SKILL.md`; author the thin `fs-gg-product-grids` wrapper in BOTH `.agents/skills/fs-gg-product-grids/SKILL.md` (Codex-active) and `.claude/skills/fs-gg-product-grids/SKILL.md` (Claude-active) — frontmatter `name: fs-gg-product-grids` + the canonical description, body pointing at `../../../template/product-skills/fs-gg-grids/SKILL.md`; run `dotnet fsi template/lifecycle/materialize-skill-roots.fsx` (produces the `.claude/skills/fs-gg-grids/` mirror); then `dotnet run --project tools/Rendering.Harness -- skill-parity` (0 findings) and re-commit the regenerated `docs/reports/skills-parity.md`. Makes T013 pass
- [ ] T019 [US2] Verify quickstart E: a `game`/`sample-pack` render materializes the skill + `Grids.fs`; an `app`/`headless-scene` render materializes neither. Run `dotnet test tests/Package.Tests` (Feature249 + the 231/238/204/219 count gates) and `dotnet test tests/Rendering.Harness.Tests` (Deterministic skill-inventory/parity) green. Record in `specs/249-grid-parts-skill/readiness/us2-skill-gating.md`

**Checkpoint**: US1 AND US2 both work independently — adaptable source ships and a dedicated, gated, coherently-registered skill guides it.

---

## Phase 5: User Story 3 - Catalog coherence & swap-guidance reach (Priority: P2)

**Goal**: The grid-parts helper is listed in the scaffold's swap/adapt taxonomy as consumer-owned
replaceable source, and every skill/capability coherence gate passes with zero drift.

**Independent Test**: The coherence gates pass (0 drift); `fs-gg-model-swap` and `scaffold-map.md` list
`Grids.fs` as consumer-owned replaceable source, next to `Collision.fs`/`Visibility.fs` (quickstart F).
Depends on US2 (the skill + registration must exist).

### Implementation for User Story 3

- [ ] T020 [US3] Update BOTH swap-guidance surfaces so the helper is reachable (FR-012): (a) `template/base/docs/scaffold-map.md` — classify `src/<ProductDir>/Grids.fs` as **replaceable/adaptable** (consumer-owned), next to the `Collision.fs`/`Visibility.fs` entries, noting it compiles before `Model.fs` and that its `Exists`-guarded compile item keeps the durable `Product.fsproj` safe on deletion; (b) `template/product-skills/fs-gg-model-swap/SKILL.md` — add `src/<ProductDir>/Grids.fs` to the "Which files you touch" **Replaceable — rewrite freely** list (next to `Collision.fs`/`Visibility.fs`), linking `[[fs-gg-grids]]`
- [ ] T021 [US3] Regenerate the manifest (model-swap's body changed in T020 → its sha256 changed): `dotnet fsi scripts/generate-skill-manifest.fsx`, then re-materialize + parity for the model-swap dev root/mirror (`template/lifecycle/materialize-skill-roots.fsx`, `dotnet run --project tools/Rendering.Harness -- skill-parity`). Re-run `Feature249GridsSkillTests` + `Feature231/238` if they pin any body digest
- [ ] T022 [US3] Verify quickstart F + full coherence: `dotnet fsi scripts/validate-lifecycle-template.fsx` clean; `dotnet test tests/Package.Tests` green (0 registry drift); `grep -c "Grids.fs"` in both swap surfaces ≥ 1. Record in `specs/249-grid-parts-skill/readiness/us3-coherence-swap.md`

**Checkpoint**: All three stories independently functional; grid-parts is a coherent, discoverable, consumer-owned capability.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: End-to-end validation, evidence, and release preparation.

- [ ] T023 [P] Author an FSI prelude transcript `scripts/grids-parts-prelude.fsx` exercising the adjacency conversions and pixel mapping the way a game consumer would (feed a cell → print its four edges and four corners; take one edge → print the two cells it separates and its `edgeSegment`; round-trip a cell through `cellCenter`→`cellAt`; snap a sample point via `cellAt`); referenced from quickstart
- [ ] T024 Run the full `quickstart.md` A–F end-to-end on a real `game` render and confirm every SC-001…SC-008 mapping holds; record the consolidated evidence under `specs/249-grid-parts-skill/readiness/`
- [ ] T025 Re-run the baseline (`scripts/baseline-tests.fsx`) and diff against T002 — confirm ZERO new reds attributable to this feature (`./fake.sh build -t Test` + Package.Tests + samples)
- [ ] T026 [P] Capture per-phase feedback under `specs/249-grid-parts-skill/feedback/` (process friction, generalizable-code candidates, severity) if the feedback capability is active
- [ ] T027 **Release prep (coordination note STAGED — see release-coordination.md; the flip itself is deferred to release)**: (Tier 1 template-contract change — do NOT flip until release)**: via the `cross-repo-coordination` skill, draft the publish-before-flip updates for the `fs-gg-ui-template` contract in `FS-GG/.github` — `registry/dependencies.yml` (contract version + consuming edge), `registry/CHANGELOG.md` (one dated newest-first entry), `docs/registry/compatibility.md` (dependency-graph + versioned-contracts + coherence rows), and the FS.GG.UI coherent-set bump. Stage as a coordination note; the actual flip happens at release (FR-013)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: depends on Setup — BLOCKS all user stories. T004 (mechanism smoke, incl. the fragment-target check) gates everything.
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
- **US1 ∥ US2** once Foundational completes (different files: `Grids.fs`/`Product.fsproj`/fragment vs. `SKILL.md`/manifest/gate-tests/dev-roots). Note both edit `.template.config/template.json` (T010, T016) — serialize those two edits.
- Within US1: T011 [P] (README) alongside T008/T009. Within US2: T013 [P] (coherence test) alongside T014.
- Polish: T023 [P], T026 [P] parallel; T024/T025 after all stories.

---

## Parallel Example: after Foundational

```bash
# Two developers pick up the independent P1 slices:
Developer A → US1: implement Grids.fs (T008), gate the compile item (T009), fragment source (T010–T011)
Developer B → US2: author SKILL.md (T014), register in catalog/manifest/template/gate-set/dev-roots+wrapper (T015–T018)
# Coordinate the single shared file: .template.config/template.json (T010 and T016) — one edits, then the other.
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → Phase 2 Foundational (CRITICAL — includes the **early live mechanism smoke** T004, with the fragment-target check).
2. Phase 3 US1 → **STOP and VALIDATE** (quickstart B–D): a game product ships editable, delete-safe grid-parts source with real, round-tripping adjacency and pixel conversions.
3. Demo the MVP: scaffold a game, walk a cell's edges/corners, draw a fence on an edge, snap the cursor via `cellAt`, delete the file — all green.

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
- **Divergence from the collision (246) task set, deliberate (matches visibility 247):** (a) NO
  `skillist-reference.md` and NO `capabilities.yml` edit — skill-only capabilities appear in neither
  (verified against `fs-gg-collision`/`fs-gg-visibility`); (b) NO game-core trim — there is no pre-existing
  grid-parts write-up to consolidate; (c) the `canonicalSources`/count-bump gate edits (T017) and the
  `fs-gg-product-grids` wrapper (T018) ARE required and are called out explicitly (see
  [[adding-a-product-skill-touchpoints]]).
- **Fragment target (T010):** `source template/fragments/grids/src/`, `target src/` (NOT `src/Product/`)
  so `sourceName` fileRename rewrites the `Product/` segment to `src/<ProductDir>/` — the Feature 246→247
  fix ([[fragment-target-sourcename-rename]]). T004's smoke proves it before US1 relies on it.
- Determinism (FR-008 — integer part-addressing, fixed list order, no `atan2`/`sqrt`/hash-iteration;
  non-finite-guarded pixels), adjacency round-trip (FR-009), pixel round-trip + totality (FR-010),
  reuse-not-rewrite (FR-006/SC-005), and delete-safety (FR-007) are the load-bearing invariants — verified
  by T007 and quickstart D, not assumed.
- Commit after each task or logical group. Do NOT flip the cross-repo registry (T027) until release.

---
description: "Task list for Grid Line-Drawing Skill + Import-and-Adapt Helper Source"
---

# Tasks: Grid Line-Drawing Skill + Import-and-Adapt Helper Source

**Input**: Design documents from `/specs/248-grid-line-drawing/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: INCLUDED — required by Constitution Principle V (test evidence is mandatory) and research R2
(a coherence gate test + a line-logic connectivity/determinism/LOS/totality test).

**Organization**: Grouped by user story. US1 (adaptable source) and US2 (skill + coherent registration)
are independent P1 slices; US3 (catalog coherence + swap-guidance reach) depends on US2 existing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (Setup / Foundational / Polish carry no story label)
- All paths are repo-relative from `/home/developer/projects/FS.GG.Rendering/`

---

## Phase 1: Setup (Shared Infrastructure)

- [x] T001 Create the new capability directories: `template/product-skills/fs-gg-line-drawing/` and `template/fragments/line-drawing/src/Product/`
- [ ] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx` (runs EVERY test project — solution + Package.Tests + samples — and records the full red/green set), so the count-gate edits below are not mistaken for regressions at merge

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: No user story work begins until this phase is complete.

- [x] T003 Placement/registry map: confirm against LIVE files that (a) `template/capabilities.yml` and `template/base/docs/skillist-reference.md` do NOT list skill-only capabilities (`grep -i visibility` in both is empty → line-drawing touches neither); (b) the gate-enforced coherent set from `contracts/skill-and-registration.md` is exhaustive — `canonicalSources` in `Feature231`/`Feature238`, the framework count in `Feature204` (currently 16), the per-profile sets in `Feature219`, and `frameworkChecked` in `scripts/validate-lifecycle-template.fsx` (currently 16)
- [x] T004 Draft the seams (compile-first): author `template/fragments/line-drawing/src/Product/LineDrawing.fs` with the `LineDrawing` module and `line`/`supercover`/`lineOfSight` signatures as compiling stubs, per `contracts/linedrawing-helper-source.md`; and create the `fs-gg-line-drawing/SKILL.md` section skeleton (headings only)

**Checkpoint**: the exact gate set is locked; source + skill seams compile — US1 and US2 can proceed in parallel.

---

## Phase 3: User Story 1 - Scaffold a game and get adaptable line-drawing source (Priority: P1) 🎯 MVP

**Goal**: A game/sample-pack product materializes a product-owned `LineDrawing.fs` the consumer can edit,
that produces an ordered connected cell line (endpoints included), reuses `Cell`, is deterministic, total,
bounded, and delete-safe.

### Tests for User Story 1 ⚠️ (write FIRST, ensure they FAIL)

- [x] T007 [P] [US1] Fill `tests/Canvas.Tests/LineDrawingHelperTests.fs` (compiles the raw `template/fragments/line-drawing/src/Product/LineDrawing.fs` — literal `namespace Product` — via a `<Compile Include>`; Canvas.Tests already refs Canvas): assert (a) ENDPOINTS — `line`/`supercover` include both endpoints, head = `a`, last = `b`; (b) CONNECTIVITY — each consecutive pair of `line` differs ≤1 in each axis, and `supercover` differs by exactly 1 in exactly one axis (no diagonal gap); (c) LOS — a target behind a blocking cell is NOT visible and IS visible with the blocker removed (FR-006); (d) DETERMINISM — repeat-run byte-identity of the cell list across all 8 octants + axes/diagonals (FR-008); (e) TOTALS — `a = b`, axis-aligned, diagonal, negative-delta lines, always-false/always-true predicate → documented values, never throws (FR-010). Tests fail against the T004 stubs
- [x] T008 [US1] Implement `template/fragments/line-drawing/src/Product/LineDrawing.fs`: `line` (integer Bresenham over all 8 octants — error accumulator, NO float interpolation/rounding), `supercover` (visit every touched cell, no diagonal gap), `lineOfSight` (fold the interior supercover cells through the `Cell -> bool` predicate). Reuse shared `Cell` only — no look-alike `(row,col)` type (FR-002/FR-009); deterministic integer arithmetic so output is bit-identical cross-platform (FR-008/SC-004); every walk bounded by the endpoint separation (FR-011); total on degenerate input (FR-010). Mark the walk + LOS bodies as the editable lines. Makes T007 pass
- [x] T009 [US1] Add the gated compile item to `template/base/src/Product/Product.fsproj` under the existing `(profile == "game" || profile == "sample-pack")` region, alongside `Visibility.fs` and before `Model.fs`: `<Compile Include="LineDrawing.fs" Condition="Exists('LineDrawing.fs')" />` — delete-safe (FR-007)
- [x] T010 [US1] Add the fragment source to `.template.config/template.json`: `source: "template/fragments/line-drawing/src/"`, `target: "src/"`, condition `(profile == "game" || profile == "sample-pack")`, NO `copyOnly` (so `sourceName` substitution runs and the `Product/` segment is fileRename'd) (FR-004) — the correct source-relative form, NOT `target: src/Product/`
- [x] T011 [P] [US1] Write `template/fragments/line-drawing/README.md`: consumer-owned, adaptable source — yours to edit (thin↔supercover, cap length, custom LOS) or delete; points at the `fs-gg-line-drawing` skill; cites the Red Blob Games line-drawing article (mirror the visibility README tone)
- [x] T012 [US1] Verify quickstart 1–4 on a real `game` render: source present + compiles; `line` connects two cells; `lineOfSight` blocked by a wall cell, clear without it; `rm LineDrawing.fs` → build still green

**Checkpoint**: US1 is a working MVP — a game product ships editable, delete-safe line-drawing source.

---

## Phase 4: User Story 2 - A dedicated grid line-drawing skill (Priority: P1)

**Goal**: `fs-gg-line-drawing` materializes for game/sample-pack (and only those), covering the `Cell`
model → Bresenham → supercover → LOS and applications, cites the Red Blob Games reference, points at the
adaptable helper; registers coherently across the full gate-enforced set.

### Tests for User Story 2 ⚠️ (write FIRST, ensure they FAIL)

- [x] T013 [P] [US2] Fill `tests/Package.Tests/Feature248LineDrawingSkillTests.fs`: assert the manifest entry (id + sha256 + `materializes-when: profile in [game, sample-pack]`), both `template.json` sources present with a condition semantically equal to the manifest, `fs-gg-product-line-drawing` wrapper + `.claude` mirror byte-parity, and profile gating (present for game/sample-pack, absent for app/headless). Fails until the registrations land

### Implementation for User Story 2

- [x] T014 [US2] Author `template/product-skills/fs-gg-line-drawing/SKILL.md` (fill the T004 skeleton) per `contracts/skill-and-registration.md`: frontmatter, Scope, Public Contract (reused `Cell`/`Pathfinding` + product-owned `LineDrawing.fs`), grid model / Bresenham line / supercover / line-of-sight / Applications sections, "The adaptable helper", Common pitfalls (float-lerp drift, thin-line diagonal gaps for sight, `Cell` vs `Point`, re-rolled `(row,col)`), Build/Test/Evidence/Package Boundary/Generated Product/Persistent problems, Related (`[[fs-gg-collision]]`, `[[fs-gg-visibility]]`, `[[fs-gg-game-core]]`, `[[fs-gg-scene]]`, `[[fs-gg-model-swap]]`), Sources (the Red Blob Games line-drawing article)
- [x] T015 [US2] Add `fs-gg-line-drawing` to the `catalog` in `scripts/generate-skill-manifest.fsx` (after `fs-gg-visibility`) with condition `(profile == \"game\" || profile == \"sample-pack\")`, then regenerate: `dotnet fsi scripts/generate-skill-manifest.fsx`
- [x] T016 [US2] Add the skill source to `.template.config/template.json`: skill → `.agents/skills/fs-gg-line-drawing/`, `copyOnly` (near the visibility skill source)
- [x] T017 [US2] Update the gate-enforced coherence set together (contracts): add the `fs-gg-line-drawing` tuple to `canonicalSources` in BOTH `Feature231` and `Feature238`; bump framework count `16 → 17` in `Feature204`; add `"fs-gg-line-drawing"` to BOTH the `game` and `sample-pack` sets in `Feature219`; set `frameworkChecked = 17` in `scripts/validate-lifecycle-template.fsx`
- [x] T018 [US2] Author the thin `fs-gg-product-line-drawing` wrapper in BOTH `.agents/skills/fs-gg-product-line-drawing/SKILL.md` (Codex-active) and `.claude/skills/fs-gg-product-line-drawing/SKILL.md` (Claude-active); run `dotnet run --project tools/Rendering.Harness -- skill-parity` (0 findings) and re-commit `docs/reports/skills-parity.md`. Makes T013 pass
- [x] T019 [US2] Verify: a `game`/`sample-pack` render materializes the skill + `LineDrawing.fs`; an `app`/`headless-scene` render materializes neither. `dotnet test tests/Package.Tests` (Feature248 + 231/238/204/219) and `dotnet test tests/Rendering.Harness.Tests` (Deterministic skill-inventory/parity) green

**Checkpoint**: US1 AND US2 both work independently.

---

## Phase 5: User Story 3 - Catalog coherence & swap-guidance reach (Priority: P2)

- [x] T020 [US3] Update BOTH swap-guidance surfaces (FR-013): (a) `template/base/docs/scaffold-map.md` — classify `src/<ProductDir>/LineDrawing.fs` as **replaceable/adaptable**, next to the `Visibility.fs` entry; (b) `template/product-skills/fs-gg-model-swap/SKILL.md` — add `src/<ProductDir>/LineDrawing.fs` to the **Replaceable — rewrite freely** list, linking `[[fs-gg-line-drawing]]`
- [x] T021 [US3] Regenerate the manifest (model-swap body changed → sha256 changed): `dotnet fsi scripts/generate-skill-manifest.fsx`, re-run skill-parity
- [x] T022 [US3] Verify full coherence: `dotnet fsi scripts/validate-lifecycle-template.fsx` clean; `dotnet test tests/Package.Tests` green (0 registry drift); `grep -c "LineDrawing.fs"` in both swap surfaces ≥ 1

---

## Phase 6: Polish & Cross-Cutting Concerns

- [x] T023 [P] Author an FSI prelude transcript `scripts/line-drawing-prelude.fsx` exercising `LineDrawing.line`/`supercover`/`lineOfSight` the way a game consumer would; referenced from quickstart
- [x] T024 Run the full `quickstart.md` 1–4 end-to-end on a real `game` render and confirm every SC-001…SC-008 mapping holds
- [ ] T025 Re-run the baseline (`scripts/baseline-tests.fsx`) and diff against T002 — confirm ZERO new reds attributable to this feature
- [x] T027 **Release prep (coordination note STAGED; the flip is deferred to release)**: via the `cross-repo-coordination` skill, draft the publish-before-flip updates for the `fs-gg-ui-template` contract in `FS-GG/.github` and the FS.GG.UI coherent-set bump. Stage as a coordination note; the actual flip happens at release (FR-014)

---

## Dependencies & Execution Order

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: depends on Setup — BLOCKS all user stories.
- **US1 (Phase 3)** and **US2 (Phase 4)**: both depend only on Foundational; independent of each other.
- **US3 (Phase 5)**: depends on **US2**. **Polish (Phase 6)**: depends on all desired stories.
- Tests written and FAILING before implementation (T007 before T008; T013 before T014–T018).
- **T015 (manifest add) and T017 (canonicalSources + count bumps) MUST land together** — adding the
  catalog entry without the gate edits reds `Feature231/238/204/219`.
- Both US1 (T010) and US2 (T016) edit `.template.config/template.json` — serialize those two edits.

## Notes

- This feature adds **no** framework package public surface / `.fsi`, so there is **no** surface-area
  baseline task.
- **Deliberate divergences:** NO `skillist-reference.md`/`capabilities.yml` edit (skill-only capability);
  NO game-core trim (no pre-existing line-drawing write-up); NO `.agents/skills/fs-gg-line-drawing/`
  canonical dev root (only the two `fs-gg-product-*` wrappers — matches the 247 file set).
- Determinism (FR-008 — integer Bresenham, no float), boundedness (FR-011), totality (FR-010),
  reuse-not-rewrite (FR-002/FR-009), and delete-safety (FR-007) are the load-bearing invariants.

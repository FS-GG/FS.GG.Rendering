---
description: "Task list for Import Selected Source (Migration Stage R4)"
---

# Tasks: Import Selected Source (Migration Stage R4)

**Input**: Design documents from `/specs/003-import-selected-source/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: This feature **imports** the R3-selected test projects as product material (US2);
it does not write new TDD tests. Verification is build-and-test (quickstart V1–V5) plus the
imported tests running green.

**Organization**: Tasks are grouped by user story (from spec.md) for independent delivery.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files/dirs, no dependencies)
- **[Story]**: US1 / US2 / US3 from spec.md
- Exact paths included. Source-of-record: `/home/developer/projects/FS-Skia-UI` @ commit
  `f759f399`. **The source is already GL** (research Finding 1) — no Vulkan port, only cleanup.

## Path Conventions

Product source lands under `src/`, tests under `tests/`, template under `template/`, docs
under `docs/`, build config + `PROVENANCE.md` at the repo root. Import in dependency tiers
(research Finding 3), building after each tier to localize failures.

---

## Phase 1: Setup (Build Scaffolding)

**Purpose**: The solution and build configuration the imported projects plug into.

- [x] T001 Create `FS.GG.Rendering.sln`, `Directory.Build.props` (`net10.0`, `LangVersion latest`, `Version`, Authors → FS.GG), and `Directory.Packages.props` (SkiaSharp `4.147.0-preview.3.1` + `NativeAssets.Linux/Win32`, `Silk.NET.OpenGL/Input/Windowing` `2.23.0`, `Elmish 5.0.2`) at the repo root, adapted from the source's equivalents.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The import manifest and provenance skeleton that govern every import.

**⚠️ CRITICAL**: Blocks all import work.

- [x] T002 Author the import manifest at `specs/003-import-selected-source/import-manifest.md` per `contracts/import-manifest.schema.md`: map every source path → repo path with disposition, covering runtime/tests/template/docs; mark `src/SkillSupport`, `tests/Governance.Tests`, `tests/SkillSupport.Tests`, `build/Governance` (the `FS.Skia.UI.Build` evidence-graph/merge-gate engine), `readiness/`, and `docs/testSpecs` as `exclude`.
- [x] T003 Create `PROVENANCE.md` at the repo root per `contracts/provenance.schema.md` with source repo + commit `f759f399`; leave the path-map/adaptations sections to be completed as import proceeds (US3).

**Checkpoint**: Manifest + provenance skeleton ready — module import can begin.

---

## Phase 3: User Story 1 - Product source compiles in the fresh repository (Priority: P1) 🎯 MVP

**Goal**: All selected runtime libraries + controls/design-system/theme/kit modules + template compile as product source, GL-only, no governance runtime.

**Independent Test**: From a fresh checkout, `dotnet build FS.GG.Rendering.sln` succeeds with every R2 import-from-source module present and no governance/Vulkan dependency (quickstart V1).

### Implementation for User Story 1 (import in dependency tiers, build after each)

- [x] T004 [US1] Tier 1 — import `src/Scene/` (`.fs`/`.fsi`/`.fsproj`); strip any `.fs` top-level access modifiers (→ `.fsi`); add to the solution; `dotnet build src/Scene` succeeds.
- [x] T005 [P] [US1] Tier 2 — import `src/Color/` (→ Scene); build.
- [x] T006 [P] [US1] Tier 2 — import `src/Layout/` (→ Scene); build.
- [x] T007 [P] [US1] Tier 2 — import `src/KeyboardInput/` (→ Scene); build.
- [x] T008 [P] [US1] Tier 2 — import `src/Testing/` (→ Scene); build.
- [x] T009 [US1] Tier 3 — import `src/SkiaViewer/` (GL host `Host/OpenGl.fs`, `SceneRenderer.fs`); remove the vestigial `ViewerBackendPreference.Vulkan` case and stale `Vulkan` comments (research Finding 1); build.
- [x] T010 [P] [US1] Tier 4 — import `src/Input/` (→ Scene, SkiaViewer); build.
- [x] T011 [P] [US1] Tier 4 — import `src/Elmish/` (→ Scene, SkiaViewer); build.
- [x] T012 [US1] Tier 5 — import `src/Controls/` including the layer modules (`DesignTokens`, `Theme`, `Style`, kit modules) and `design-tokens.tokens.json`; honor the four UI layers at module level per `docs/product/layering.md` (one control set, no per-theme forks); build.
- [x] T013 [US1] Tier 6 — import `src/Controls.Elmish/` (→ Controls, KeyboardInput, SkiaViewer); build.
- [x] T014 [US1] Import the `dotnet new` template + template package into `template/` (FR-003); **strip the governance-coupled "governed" profiles** and any reference to `build/Governance`/`FS.Skia.UI.Build` from the template (`.template.config` + `.template.package`), importing only the non-governed profiles (app / headless-scene / sample-pack) so the generated product needs no governance runtime (FR-006).
- [x] T015 [US1] Wire all `src/` projects + template into `FS.GG.Rendering.sln`; full `dotnet build FS.GG.Rendering.sln -c Release` succeeds (SC-001).
- [x] T016 [US1] Source compliance sweep: confirm no `Silk.NET.Vulkan`/`GRVkBackend`/`SkillSupport` references and no `.fs` top-level access modifiers in `src/`; confirm package identity `FS.Skia.UI.*` is preserved (resolve the actual `PackageId` mapping — plan open item) and target is `net10.0` (SC-003/004/005/007, source side).

**Checkpoint**: The product compiles — MVP base exists.

---

## Phase 4: User Story 2 - Imported tests run and pass (Priority: P2)

**Goal**: The R3 import-now test set runs from this repo; the default local tier passes.

**Independent Test**: From a fresh checkout, the default local test tier executes and passes; each imported test keeps its R3 justification note (quickstart V2).

### Implementation for User Story 2

- [x] T017 [US2] Import the local-tier test projects into `tests/` — `Color.Tests`, `Scene.Tests`, `Layout.Tests`, `Input.Tests`, `KeyboardInput.Tests`, `Elmish.Tests`, `Controls.Tests`, `Testing.Tests`, `SkiaViewer.Tests`, `Lib.Tests`, `Smoke.Tests` — each with its R3 justification note; do NOT import `Governance.Tests`/`SkillSupport.Tests` (FR-006); add to the solution.
- [x] T018 [US2] Import the CI-tier checks: `tests/surface-baselines/` + `refresh-surface-baselines.fsx`, and the `fsdocs` docs-build configuration; keep their R3 justification notes.
- [x] T019 [US2] Import the release-only checks: `tests/Package.Tests/` and the template `Product.Tests` (with justification notes); add to the solution but not the default local run.
- [x] T020 [US2] Run the default local tier (`dotnet test` over the local-frequency projects); ensure green. Any environment-blocked test is skipped with a written rationale carrying the `Synthetic`/skip disclosure (Principle V) — never marked passing, never weakened (SC-002).
- [x] T021 [US2] Generate/refresh a surface-area baseline per public module so `surface-baselines` has a baseline for each; record any regenerated baseline's origin (SC-004 completion).

**Checkpoint**: Product compiles AND the local test tier is green.

---

## Phase 5: User Story 3 - Provenance and current docs travel with the import (Priority: P3)

**Goal**: Every imported file traces to source commit/path; imported docs describe current behavior; historical logs left behind.

**Independent Test**: Pick any imported file → provenance identifies its source commit + path; imported docs describe current behavior, not retired workflow (quickstart V5).

### Implementation for User Story 3

- [x] T022 [P] [US3] Complete `PROVENANCE.md`: fill the path map for every imported group (`src/**`, `tests/**`, `template/`, `docs/`) and the Adaptations section (governance removal, Vulkan-naming cleanup, Authors/identity, `.fs` access-modifier strips) per the provenance contract (SC-006).
- [x] T023 [P] [US3] Import current controls docs/examples and still-current ADRs into `docs/`, rewriting retired-governance references; do NOT import historical readiness logs or old feature-workflow artifacts (FR-009).

**Checkpoint**: Import is auditable and documented.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Full acceptance run and repo-doc updates.

- [x] T024 Run the full quickstart V1–V5 (`dotnet build`, local `dotnet test`, governance/Vulkan/identity greps, `.fsi`/access-modifier check, provenance review) and confirm all of SC-001..SC-007; re-confirm `specs/003-import-selected-source/checklists/requirements.md` still passes.
- [x] T025 [P] Update repo docs (`README.md`, `CLAUDE.md`) to note the product source now lives here and that the test harness is the next stage (R5).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (P1)** → **Foundational (P2)** → **User Stories (P3–P5)** → **Polish (P6)**.
- US2 and US3 both depend on US1 (need imported, compiling source). US2 and US3 are
  independent of each other.

### Within User Story 1 (dependency tiers — build order matters)

- T004 (Scene) → T005–T008 (Tier 2, parallel) → T009 (SkiaViewer) → T010–T011 (Tier 4, parallel)
  → T012 (Controls) → T013 (Controls.Elmish). T014 (template) any time after T001. T015 (sln
  build) after all module imports. T016 (sweep) after T015.

### Parallel Opportunities

- Tier 2 imports T005–T008 run in parallel; Tier 4 imports T010–T011 run in parallel.
- After US1, US3's doc tasks (T022, T023) run in parallel with US2's test import.
- Polish T025 is `[P]`; T024 runs last.

---

## Parallel Example: User Story 1, Tier 2

```bash
# After Scene (T004) builds, import the Scene-only dependents together:
Task: "Import src/Color (T005)"
Task: "Import src/Layout (T006)"
Task: "Import src/KeyboardInput (T007)"
Task: "Import src/Testing (T008)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Setup (T001) + Foundational (T002–T003).
2. US1 (T004–T016) — import runtime + controls + template in dependency tiers; full build green.
3. **STOP and VALIDATE**: `dotnet build` succeeds (V1). A compiling product is the usable base.

### Incremental Delivery

1. Setup + Foundational → build scaffold + manifest.
2. US1 → compiles (MVP).
3. US2 (tests green) and US3 (provenance + docs) in parallel.
4. Polish → full V1–V5 acceptance + repo-doc updates.

---

## Notes

- The source is already FSI-first, GL, `net10.0`; the import preserves compliance rather than
  re-deriving it. Strip stray `.fs` access modifiers and remove vestigial `Vulkan` naming.
- Excluded (per R2/R3): `SkillSupport`, `Governance.Tests`, `SkillSupport.Tests`, historical
  readiness logs. No governance runtime is imported.
- Tier-1 change: keep `.fsi` + surface baselines + provenance current as you go.

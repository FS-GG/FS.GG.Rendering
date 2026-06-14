# Feature Specification: Import Selected Source (Migration Stage R4)

**Feature Branch**: `003-import-selected-source`

**Created**: 2026-06-14

**Status**: Draft

**Change Classification**: **Tier 1 (contracted change)** — introduces the product's public API
surface, dependencies, and package contracts. Requires the full artifact chain: `.fsi`
signatures, surface-area baselines, test evidence, and docs. This is an **import** of an
existing FSI-first, already-tested codebase, so Principle I's Spec→FSI→Tests→Implementation
order is satisfied by the source's own history rather than re-derived here (the full artifact
chain still ships); future *changes* follow the normal order.

**Input**: User description: "next phase FS.gg"

## Context

This is the first **code-importing** stage of the FS.GG.Rendering migration. Stages R1
(fresh repo), R2 (product shape), and R3 (initial validation set) are done. R4 copies the
*selected* product source from the archived source repository (FS-Skia-UI) into this repo,
bounded by two prior decisions: the **import-from-source** modules in
`docs/product/module-map.md` (R2) and the **import-now** checks in
`docs/validation/validation-set.md` (R3).

Unlike R2/R3 (decision documents), this feature produces **real F# source and tests** and
must compile and run here. Per the user's decision, R4 is done as a **single feature**:
all selected runtime libraries, the controls/design-system/theme/kit layers, the template,
and the import-now test set are imported together. Building the test harness is the later
Stage R5; stabilizing CI validation is Stage R6.

"Users" are the maintainers and contributors who will build, test, and extend the product
in this repository.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Product source compiles in the fresh repository (Priority: P1)

A contributor clones the repository and builds it; the selected runtime libraries —
scene, color, layout, input, keyboard input, the SkiaSharp-over-GL viewer, Elmish
integration, controls, controls-Elmish, testing helpers — plus the design-system/theme/kit
layers and the template, all compile as product source with no dependency on any
governance runtime.

**Why this priority**: A repository whose product code does not compile delivers nothing.
Compiling source is the foundation every later stage (tests, harness, release) stands on,
and is the core exit criterion of R4. It is the minimum viable outcome: even with nothing
else, a compiling product is a usable base.

**Independent Test**: From a fresh checkout, build the solution; it succeeds with the
selected modules present and no governance/Vulkan dependencies.

**Acceptance Scenarios**:

1. **Given** a fresh checkout, **When** the solution is built, **Then** it compiles
   successfully with every `import-from-source` module from the R2 module map present.
2. **Given** the imported viewer/render path, **When** it is built, **Then** it targets
   SkiaSharp over **OpenGL (GL)** with no remaining Vulkan dependency.
3. **Given** the imported source, **When** it is inspected, **Then** no retired governance
   material (SkillSupport, evidence/skill gates, project/feature-graph machinery) is
   present, and package identity is still `FS.Skia.UI.*` on target `net10.0`.
4. **Given** any public module, **When** its files are inspected, **Then** it has a curated
   `.fsi` signature and its `.fs` files carry no top-level access modifiers.

---

### User Story 2 - Imported tests run and pass (Priority: P2)

A contributor runs the validation set; the **import-now** test projects from R3 (the local
unit tests, GL smoke, public-surface baseline checks, docs build, package and template
generated-product checks) run from this repository, and the default local tier passes.

**Why this priority**: Compiling code without runnable tests gives no confidence the import
preserved behavior. Running the R3-selected tests is the second R4 exit criterion and
protects the imported product contracts. Builds on US1.

**Independent Test**: From a fresh checkout, run the default local test tier; it executes
and passes, and each imported test retains its justification note.

**Acceptance Scenarios**:

1. **Given** the imported test projects, **When** the default local tier is run, **Then**
   it executes and passes against the imported source.
2. **Given** an imported check, **When** it is reviewed, **Then** its R3 justification note
   (product contract / failure mode / owner / frequency / cost) is retained alongside it.
3. **Given** a test that depends on an excluded module (Governance/SkillSupport), **When**
   the import is reviewed, **Then** that test was not imported, consistent with R3.
4. **Given** a test that cannot pass in this environment for an out-of-scope reason,
   **When** it is handled, **Then** it is skipped with a written rationale — never marked
   passing and never weakened to green the build.

---

### User Story 3 - Provenance and current docs travel with the import (Priority: P3)

A maintainer can trace every imported file back to its source commit and path, and the
imported controls docs/ADRs describe current product behavior — with historical readiness
logs and old workflow artifacts left behind in the archive.

**Why this priority**: Provenance and accurate docs make the import auditable and
maintainable, but they protect future work rather than unblocking the build/test loop.
Lower priority than a compiling, tested product.

**Independent Test**: Pick any imported file; a provenance record identifies its source
commit and original path. Open the imported docs; they describe current behavior, not
retired workflow state.

**Acceptance Scenarios**:

1. **Given** the imported material, **When** provenance is checked, **Then** a record
   identifies the source repository, the source commit, and the copied paths.
2. **Given** the imported docs and ADRs, **When** they are read, **Then** they describe
   current product behavior; retired-governance references are removed or rewritten.
3. **Given** the repository after import, **When** it is scanned, **Then** no historical
   readiness logs or old feature-workflow artifacts were imported.

### Edge Cases

- The source references Vulkan only as an *unsupported-backend* affordance (the
  `ViewerBackendPreference.Vulkan` case returns a clear "no longer supported; presents through
  OpenGL" result) and in historical comments → kept as graceful-degradation behavior; there is
  no Vulkan backend or dependency to remove. The source is already GL.
- A `.fs` file carries `private`/`internal`/`public` modifiers → visibility is moved to the
  `.fsi` and the modifiers are stripped (constitution Principle II).
- Design-system, theme, or kit code is entangled inside `Controls` → it is separated into
  distinct layers per `docs/product/layering.md`; if a clean split is risky, it is isolated
  by namespace/folder with a note, preserving the single semantic control set.
- A public surface differs from the source baseline on import → the surface-area baseline is
  regenerated as the new baseline and recorded, not silently diffed away.
- An imported dependency is unpinned or a preview package → it is pinned to an explicit
  version (constitution Engineering Constraints).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Import every `import-from-source` runtime module named in the R2 module map
  (`docs/product/module-map.md`): scene, color, layout, input, keyboard input, the
  SkiaSharp viewer/host, Elmish integration, controls, controls-Elmish integration, and
  testing helpers — as product source.
- **FR-002**: Import the design-system primitives, themes, and design-specific kits,
  organized as the four distinct layers defined in `docs/product/layering.md`, preserving
  one semantic control set styled by many themes (no per-theme control forks).
- **FR-003**: Import the `dotnet new` template and its template package.
- **FR-004**: Import the R3 **import-now** validation set from `docs/validation/validation-set.md`
  (local unit tests, GL smoke, public-surface baselines + refresh, docs build, package
  checks, and the template generated-product check), keeping each imported check's R3
  justification note.
- **FR-005**: The viewer/render path MUST target SkiaSharp over **OpenGL (GL)** with **no
  Vulkan dependency or backend** (no `Silk.NET.Vulkan` / `GRVkBackend`). The source already
  presents through GL. The public `ViewerBackendPreference.Vulkan` case is **retained** as an
  explicit *unsupported-backend* result (it returns "Vulkan backend is no longer supported;
  presents through OpenGL") — consistent with the `Software` case and Principle VI (degrade
  explicitly). It is a graceful-degradation affordance, not a dependency; removing it would
  break the public API for no dependency benefit. (No backend port; see `research.md` Finding 1.)
- **FR-006**: Retired governance material MUST NOT be imported and references to it MUST be
  removed or rewritten: the `SkillSupport` module, `Governance.Tests`, `SkillSupport.Tests`,
  evidence/skill gates, and project/product/feature-graph machinery.
- **FR-007**: Every imported public module MUST have a curated `.fsi`; imported `.fs` files
  MUST NOT carry top-level access modifiers; a surface-area baseline file MUST exist for each
  public module (imported or regenerated).
- **FR-008**: Provenance MUST be recorded for the imported material — source repository,
  source commit, and copied paths.
- **FR-009**: Imported docs and ADRs MUST be limited to those describing current product
  behavior; historical readiness logs and old feature-workflow artifacts MUST NOT be
  imported.
- **FR-010**: Package identity MUST remain `FS.Skia.UI.*`, the target framework `net10.0`,
  and SkiaSharp pinned to an explicit version (preview packages explicitly pinned).
- **FR-011**: After import, the product source MUST compile and the imported tests MUST run
  in this repository, with no dependency on any governance runtime. Tests that cannot pass
  for an out-of-scope reason MUST be skipped with written rationale, never marked passing.

### Key Entities *(include if feature involves data)*

- **Imported module**: A runtime source module copied from the source repo, with its `.fsi`,
  `.fs`, and surface-area baseline.
- **Imported test project**: A selected test project with its retained R3 justification note.
- **Provenance record**: Source repository + commit + copied-paths mapping for imported
  material.
- **Surface-area baseline**: The recorded public surface for a module, used by the
  API-drift check.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A fresh checkout builds the full solution successfully (product code compiles),
  with every R2 `import-from-source` module present.
- **SC-002**: The default local test tier from R3 runs and passes from the fresh checkout.
- **SC-003**: Zero references to retired governance machinery remain in imported source
  (no `SkillSupport`, project/feature-graph, or evidence-gate imports), and the excluded
  test projects are absent.
- **SC-004**: 100% of imported public modules have a `.fsi` and a surface-area baseline; zero
  `.fs` top-level access modifiers remain.
- **SC-005**: The viewer/render path runs on GL with no remaining Vulkan dependency.
- **SC-006**: Every imported file is traceable to a source commit and original path via the
  provenance record.
- **SC-007**: Package identity is `FS.Skia.UI.*`, target is `net10.0`, and SkiaSharp is
  pinned to an explicit version.

## Assumptions

- **Scope = full R4 selected import, in one feature** (user-chosen). It imports all R2
  `import-from-source` modules + the R3 import-now set + template + current docs. Building
  the test harness (R5), stabilizing CI validation (R6), bridging the old repo (R7), and the
  rebrand (R8) are out of scope.
- **Source of record** is the archived FS-Skia-UI repository at a pinned source commit;
  what is imported is bounded by `docs/product/module-map.md` and
  `docs/validation/validation-set.md`.
- Imported tests are expected to pass as-is in this repository, modulo the GL and
  governance-cleanup adaptations; genuine environment-blocked tests are skipped with
  rationale (never weakened), per the constitution.
- The development environment provides hardware GL (per the R3/harness capability baseline),
  so GL-dependent unit/smoke tests can run locally.
- The source is already SkiaSharp-over-GL (migrated upstream, "feature 119"); R4 needs **no
  backend port** — only removal of vestigial Vulkan naming. The rest of the source imports
  unchanged.
- R2/R3 decisions stand and are not re-litigated here (package identity deferred-rebrand,
  four-layer model, the validation-set decisions).

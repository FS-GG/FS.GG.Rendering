# Feature Specification: Define Product Shape (Migration Stage R2)

**Feature Branch**: `001-define-product-shape`

**Created**: 2026-06-14

**Status**: Draft

**Input**: User description: "start the fs.gg migration process."

## Context

This is the first product-definition increment of the FS.GG.Rendering migration.
The migration is staged R1 → R8 in the active rendering implementation plan. Stage
R1 (fresh repository) is already complete: the repo exists with README, license,
ignore files, constitution v1.0.0, and standard Spec Kit. "Starting the migration
process" therefore begins at the next uncompleted stage, **R2 — Define product
shape**: deciding and documenting what the rendering product owns *before* any
source code is copied (source import is the later Stage R4).

This feature produces decision and map artifacts, not runtime code. Its "users" are
the maintainers and contributors of FS.GG.Rendering and the people who later perform
the source import.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Maintainer can explain what rendering owns (Priority: P1)

A maintainer or new contributor opens the repository and needs to understand the
shape of the product: which modules exist, what each is responsible for, and which
will be brought in from the source repository (FS-Skia-UI) versus deferred or left
behind. They read a single product/module map and can name the boundary of the
rendering product without reading source code.

**Why this priority**: The module map is the foundation of the whole migration. Every
later stage (validation-set selection, source import, harness) references "what
rendering owns." Without it, import decisions have no frame of reference. It is the
minimum viable output of R2: even if nothing else in this feature ships, a correct map
makes the next stage actionable.

**Independent Test**: Hand the map to someone unfamiliar with the project and ask them
to list the product's modules and what each does; the map alone should let them answer
correctly for every named module.

**Acceptance Scenarios**:

1. **Given** the product/module map, **When** a maintainer looks up any of the runtime
   areas (scene, color, layout, input, viewer, Elmish integration, controls, controls
   Elmish integration, testing helpers, template support), **Then** each appears with a
   one-line responsibility statement and a disposition (owned-here, import-from-source,
   or deferred/excluded).
2. **Given** the map, **When** a reader asks "what does the rendering repository own?",
   **Then** the answer is derivable from the map without consulting source code or the
   archived repository.
3. **Given** an area that is explicitly out of scope for this repository (e.g. the
   Vulkan backend, governance/SkillSupport material), **When** a reader scans the map,
   **Then** that exclusion is stated rather than silently omitted.

---

### User Story 2 - Layer boundaries are unambiguous (Priority: P2)

A contributor adding or changing UI material needs to know which layer their work
belongs to. The product distinguishes four layers — semantic controls, design-system
primitives, themes, and design-specific kits — and a layering document states the rule
that there is ONE semantic control set styled by MANY themes (no per-design-system
control forks such as `AntButton` / `FluentButton`).

**Why this priority**: Layer confusion is the specific failure the source repository's
design guidance was written to prevent. Recording the rule now, before import, keeps
the imported controls/themes from re-introducing forks. It builds on US1 but is
independently valuable: the layering doc can be reviewed and adopted on its own.

**Independent Test**: Give a contributor three sample change requests (a new control, a
new spacing/elevation primitive, a new visual skin) and ask which layer each belongs
to; the layering document should make all three unambiguous.

**Acceptance Scenarios**:

1. **Given** the layering document, **When** a contributor classifies a proposed change,
   **Then** controls, design-system primitives, themes, and design-specific kits each
   have a distinct, non-overlapping definition that resolves the classification.
2. **Given** a proposal to add a theme-specific control variant (e.g. a Fluent-styled
   button as a separate control type), **When** it is checked against the document,
   **Then** the document identifies it as a violation of the one-control-set rule and
   points to the theme layer instead.

---

### User Story 3 - Migration decisions are recorded (Priority: P3)

Before code is copied, the open product-shape decisions are written down so the import
stage has no unresolved ambiguity: the package-identity (rebrand) decision, the
template-ownership decision, and the list of product docs to import.

**Why this priority**: These decisions unblock Stage R3/R4 but are lower risk than the
map and layering rule because reasonable defaults already exist (defer rebrand; keep
templates with rendering). Recording them removes ambiguity and prevents re-litigation
during import.

**Independent Test**: Review each decision record and confirm it states the choice, the
rationale, and (where the choice is "defer") the trigger that would revisit it — with no
"to be decided later, unspecified" gaps.

**Acceptance Scenarios**:

1. **Given** the package-identity decision, **When** it is read, **Then** it explicitly
   states whether package IDs stay `FS.Skia.UI.*` for now or move, with a rationale and
   a named revisit point (the rebrand is its own later stage).
2. **Given** the template-ownership decision, **When** it is read, **Then** it states
   whether rendering owns the templates or they move to a separate repository, with the
   condition that would change the answer.
3. **Given** the docs-to-import list, **When** it is reviewed, **Then** each entry names
   a source document and whether it is imported as-is, adapted, or excluded.

### Edge Cases

- What happens when a source module spans layers (e.g. a control that bundles its own
  theming)? The map/layering doc must assign it a primary layer and note the split to be
  resolved at import, not leave it unclassified.
- How is a module handled that exists in the source repository but has no agreed home
  here (e.g. governance-flavored `SkillSupport`)? It must be marked deferred/excluded
  with a reason, never silently dropped.
- What happens if a deliverable cannot be settled (genuine open question)? It is recorded
  as an explicit open decision with options, not resolved by omission.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST produce a product/module map covering at minimum scene,
  color, layout, input, viewer, Elmish integration, controls, controls Elmish
  integration, testing helpers, and template support.
- **FR-002**: Each module entry in the map MUST state a one-line responsibility and a
  disposition: owned and defined here, to be imported from the source repository, or
  deferred/excluded (with a reason for exclusions).
- **FR-003**: The map MUST make the rendering product's ownership boundary explicit,
  including stated exclusions (e.g. the Vulkan backend and governance/SkillSupport
  material are out of scope for this repository).
- **FR-004**: The feature MUST produce a design/control layering document, copied or
  adapted from the source repository's design-and-controls guidance, defining four
  distinct layers: semantic controls, design-system primitives, themes, and
  design-specific kits.
- **FR-005**: The layering document MUST state and justify the rule that there is one
  semantic control set styled by many themes, and MUST reject per-theme/per-design-system
  control forks.
- **FR-006**: The feature MUST record a package-identity decision stating whether package
  IDs remain `FS.Skia.UI.*` initially or move, including rationale and the point at which
  the decision is revisited. The rebrand MUST be either explicitly deferred or explicitly
  planned (not left undecided).
- **FR-007**: The feature MUST record a template-ownership decision stating whether the
  rendering repository owns the templates or they move to a separate repository, with the
  condition that would change the answer.
- **FR-008**: The feature MUST produce a list of product documents to import, each marked
  as import-as-is, adapt, or exclude.
- **FR-009**: All artifacts MUST be expressed as definitions/decisions only; this feature
  MUST NOT copy runtime source code, import the legacy test surface, or reintroduce
  removed governance machinery (feature graphs, evidence-audit gates, mandatory skill
  gates).
- **FR-010**: Any deliverable that cannot be resolved MUST be recorded as an explicit open
  decision with options, rather than omitted.

### Key Entities *(include if feature involves data)*

- **Product/module map**: The catalog of product areas, each with responsibility and
  disposition; the authoritative answer to "what does rendering own?"
- **Module entry**: One area (e.g. viewer, controls) with its responsibility line and
  disposition (owned / import / deferred-excluded).
- **Layering document**: The definition of the four UI layers and the one-control-set
  rule, with classification guidance.
- **Decision record**: A recorded choice (package identity, template ownership) with
  choice, rationale, and revisit trigger.
- **Docs-to-import list**: Source documents with an import disposition (as-is / adapt /
  exclude).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A reader unfamiliar with the project can correctly state the rendering
  product's ownership boundary and the role of each of the named modules using the map
  alone, with no source-code or archived-repo lookup.
- **SC-002**: 100% of the modules named in FR-001 appear in the map, each with a
  responsibility line and a disposition; there are zero modules left without a
  disposition.
- **SC-003**: Given a set of classification cases (a new control, a new design-system
  primitive, a new theme, a new design-specific kit), a contributor using the layering
  document classifies every case to a single layer with no overlap or ambiguity.
- **SC-004**: The package-identity decision and the template-ownership decision are each
  recorded as a definite choice with rationale and revisit trigger; zero product-shape
  decisions remain in an "unspecified / decide later" state.
- **SC-005**: Every entry in the docs-to-import list carries an import disposition; a
  reviewer can act on the list at the source-import stage without further clarification.
- **SC-006**: The R2 exit criteria from the rendering implementation plan are all
  satisfiable from these artifacts: maintainers can explain what rendering owns, the four
  layers have distinct boundaries, and the rebrand is explicitly deferred or planned.

## Assumptions

- **Scope = Stage R2 only.** "Start the fs.gg migration process" is interpreted as
  beginning at the next uncompleted migration stage. R1 (fresh repo) is already done, and
  R2 (Define product shape) is the natural next step per the active plan and project
  notes. Source import (R4), the validation set (R3), and the test harness (R5) are
  separate later features and are out of scope here.
- This feature delivers definition/decision documents, not runtime code. No source is
  copied and no tests are imported at this stage.
- The default package-identity choice, absent contrary direction, is to keep
  `FS.Skia.UI.*` and defer the rebrand to its own later release decision (Stage R8),
  consistent with the constitution and migration notes.
- The default template-ownership choice is that the rendering repository owns the
  templates for now, revisited only if template cadence later justifies a separate
  repository.
- The source of record for module names, layering guidance, and candidate docs is the
  archived FS-Skia-UI repository and its `docs/FS.GG/` migration documents (notably
  `design-and-controls.md` and `rendering-project.md`).
- The four-layer model (semantic controls / design-system primitives / themes /
  design-specific kits) and the single-control-set rule are carried forward as product
  policy, not re-derived.

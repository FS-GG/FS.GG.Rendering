# Phase 0 Research: Define Product Shape (Stage R2)

This stage produces decision/definition artifacts, so "research" means resolving the
open product-shape questions against the source repository and the migration docs. No
software-stack unknowns exist (this stage writes no code), so there are no
`NEEDS CLARIFICATION` items to clear. Each decision below is grounded in inspected
sources.

## Sources inspected

- Archived source tree `EHotwagner/FS-Skia-UI/src/` — confirmed modules:
  `Color`, `Controls`, `Controls.Elmish`, `Elmish`, `Input`, `KeyboardInput`,
  `Layout`, `Scene`, `SkiaViewer`, `SkillSupport`, `Testing`.
- Template packaging: `.template.config/template.json` + `.template.package/FS.Skia.UI.Template.fsproj`.
- Sample galleries (13): `BasicViewer`, `ChartsGallery`, `ControlsGallery`,
  `DataGridGallery`, `DemoReel`, `EffectsGallery`, `InteractiveViewer`, `KeyboardInput`,
  `KeyboardInputGallery`, `LayoutGraphGallery`, `ParityGallery`,
  `PointerInteractionGallery`, `ScreenshotGallery`.
- Migration docs `docs/FS.GG/`: `design-and-controls.md`, `rendering-project.md`,
  `rendering-implementation-plan.md` (the active plan, Stage R2).
- This repo's `constitution.md` v1.0.0 — Engineering Constraints (layering rule,
  package identity).

## Decision 1 — Module-map taxonomy

**Decision**: Map the product as the ten runtime areas named in the plan, anchored to the
source modules, and add `template support` as the eleventh entry. Group them under the
conceptual layers from `design-and-controls.md` (Rendering.Core / Controls / DesignSystem /
Themes / Kits). Assign each a disposition: **owned-here**, **import-from-source** (Stage
R4), or **deferred/excluded**.

**Rationale**: The plan's R2 deliverable lists exactly these areas; anchoring to real source
module names keeps the map verifiable and gives Stage R4 a concrete import checklist.

**Notable dispositions**:
- `SkillSupport` → **deferred/excluded** with reason: governance-flavored; the constitution
  removed mandatory skill gates and treats skills as advisory. Do not auto-import; scrutinize
  later if a product need appears.
- Vulkan backend → **excluded** (constitution scopes this repo to SkiaSharp over OpenGL).
- DesignSystem / Themes / Kits → currently embedded within `Controls` in the source; the map
  records them as **distinct target layers** even though they may import as one slice, flagging
  the split to resolve at import.

**Alternatives considered**: Mapping by source `.fsproj` only (rejected — hides the
design-system/theme/kit layering the plan requires as distinct boundaries); mapping by sample
gallery (rejected — galleries are validation surface, not product modules).

## Decision 2 — Layering document source

**Decision**: Adapt `docs/FS.GG/design-and-controls.md` into `docs/product/layering.md`
largely as-is: four layers (semantic controls, design-system primitives, themes,
design-specific kits), the one-control-set rule, and the decision-rule table (Visual→Theme,
shared slots→Design system, behavior→Control, composition→Kit).

**Rationale**: The source doc already matches the constitution's Engineering-Constraints
layering rule verbatim and is product-owned guidance. Re-deriving it would risk drift; the
constitution says when constitution and a doc disagree, the constitution wins — they agree
here.

**Alternatives considered**: Writing a fresh layering model (rejected — needless divergence
from an accepted, constitution-aligned source).

## Decision 3 — Package identity (rebrand)

**Decision**: **Defer.** Keep `FS.Skia.UI.*` package IDs for now; any move to `FS.GG.UI.*`
is a separate, explicit release decision (migration Stage R8). Record as ADR-style note
`docs/product/decisions/0001-package-identity.md` with revisit trigger = "Stage R8 / explicit
release decision."

**Rationale**: The constitution already mandates this (`Package identity stays FS.Skia.UI.*
initially`), and the migration notes call for deferring rebrand to avoid churn during import.
Recording it removes ambiguity for Stage R4 without changing any package.

**Alternatives considered**: Rebrand now (rejected — premature; multiplies churn across
source, namespaces, template, and docs before the product even compiles here).

## Decision 4 — Template ownership

**Decision**: The rendering repository **owns the templates** for now (the `dotnet new`
template + its package). Revisit only if template release cadence later diverges enough to
justify a separate repository. Record as `docs/product/decisions/0002-template-ownership.md`.

**Rationale**: The plan's default is "keep templates with rendering unless their cadence
later justifies a separate repository." Templates validate the product's generated-consumer
contract, which is a rendering concern.

**Alternatives considered**: Separate template repo now (rejected — no cadence pressure yet;
adds coordination cost the working style avoids).

## Decision 5 — Docs-to-import list scope

**Decision**: Produce `docs/product/docs-to-import.md` listing the `docs/FS.GG/` migration
docs and any current product/architecture docs, each marked **import-as-is**, **adapt**, or
**exclude**. Galleries and ADRs that remain current are candidates; historical readiness logs
are excluded.

**Rationale**: Stage R2 deliverable explicitly requires a "list of product docs to import";
marking disposition makes the list directly actionable at Stage R4 without re-review.

**Alternatives considered**: Deferring the list to R4 (rejected — R2 exit criteria and the
spec FR-008 require it now; deciding disposition early is cheap and unblocks import).

## Out of scope (explicit deferrals)

- Selecting which tests/checks to import → Stage R3 (validation-set justification records).
- Copying any source code or tests → Stage R4.
- Building the test harness → Stage R5.
- Choosing the structured-logging library → tracked as `TODO(STRUCTURED_LOGGING)` in an ADR,
  not part of this stage.

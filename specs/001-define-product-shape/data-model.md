# Phase 1 Data Model: Define Product Shape (Stage R2)

The "data" of this feature is the structure of its decision/definition artifacts. These are
documentation entities, not runtime types. Each maps to a deliverable file under
`docs/product/`. Field-level formats are specified in [`contracts/`](./contracts/).

## Entity: Module Map (`docs/product/module-map.md`)

The authoritative catalog answering "what does rendering own?"

- **Composed of**: one or more **Module Entry** rows.
- **Validation** (from FR-001..FR-003, SC-002):
  - MUST contain an entry for each of: scene, color, layout, input, viewer, Elmish
    integration, controls, controls Elmish integration, testing helpers, template support.
  - MUST state the ownership boundary including explicit exclusions (Vulkan backend,
    governance/SkillSupport).
  - Zero modules without a disposition.

### Sub-entity: Module Entry

| Field | Description | Rule |
|---|---|---|
| Area | Product area name (anchored to a source module where one exists) | Required; unique |
| Source module | Originating `src/**` module, or "—" if new | Required |
| Structural area | One of: Rendering.Core, Controls, DesignSystem, Themes, Kits, Tooling/Template, Testing | Required |
| Responsibility | One-line statement of what it does | Required; ≤ ~140 chars |
| Disposition | `owned-here` \| `import-from-source` \| `deferred` \| `excluded` | Required |
| Reason | Why, when disposition is `deferred` or `excluded` | Required iff deferred/excluded |

> **Note**: "Structural area" is a *broader* classification than the four UI layers
> defined in `layering.md`. Four of its values — Controls, DesignSystem, Themes, Kits —
> are exactly those UI layers; Rendering.Core, Tooling/Template, and Testing are
> non-UI structural buckets. Do not read this column as "there are seven UI layers."

## Entity: Layering Document (`docs/product/layering.md`)

Defines the four UI layers and the one-control-set rule.

- **Composed of**: four **Layer Definition** entries + the **Decision Rule** table.
- **Validation** (from FR-004..FR-005, SC-003):
  - Exactly four layers defined: semantic controls, design-system primitives, themes,
    design-specific kits — each with a distinct, non-overlapping definition.
  - MUST state and justify "one semantic control set, many themes" and reject per-theme
    control forks (no `AntButton`/`FluentButton`).
  - MUST include a classification decision rule (change type → layer).

### Sub-entity: Layer Definition

| Field | Description | Rule |
|---|---|---|
| Layer | Layer name | Required; one of the four |
| Owns | What this layer is responsible for | Required |
| Does NOT own | Explicit boundary (what belongs to an adjacent layer) | Required |
| Examples | Representative members | Optional |

## Entity: Decision Record (`docs/product/decisions/NNNN-*.md`)

A recorded product-shape choice. Two instances in this feature: package identity, template
ownership.

| Field | Description | Rule |
|---|---|---|
| ID | `NNNN` sequential | Required; unique |
| Title | Short decision name | Required |
| Status | `accepted` \| `deferred` | Required |
| Decision | The choice made | Required; definite (no "decide later") |
| Rationale | Why | Required |
| Revisit trigger | The condition/stage that reopens it | Required |

- **Validation** (from FR-006, FR-007, SC-004): both records present; each a definite choice
  with rationale and revisit trigger; zero decisions left "unspecified".

## Entity: Docs-to-Import List (`docs/product/docs-to-import.md`)

- **Composed of**: one or more **Doc Import Entry** rows.

### Sub-entity: Doc Import Entry

| Field | Description | Rule |
|---|---|---|
| Source document | Path/name in the source repo | Required |
| Disposition | `import-as-is` \| `adapt` \| `exclude` | Required |
| Note | Adaptation/exclusion reason | Optional (required for `exclude`) |

- **Validation** (from FR-008, SC-005): every entry carries a disposition; list is actionable
  at Stage R4 without further clarification.

## Cross-cutting invariants

- **No code/test/governance import** (FR-009): no entity in this model includes `src/**`
  code, imported tests, or governance machinery.
- **Open questions are explicit** (FR-010): an unresolved deliverable is recorded as a
  Decision Record with `status: deferred` and listed options — never omitted.

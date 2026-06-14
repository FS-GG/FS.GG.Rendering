# Quickstart / Validation: Define Product Shape (Stage R2)

This feature ships documentation/decision artifacts, so "running it" means producing the
artifacts under `docs/product/` and validating them by review. No build or test commands
apply at this stage.

## Prerequisites

- Read access to the archived source repo at `/home/developer/projects/FS-Skia-UI`
  (`src/**`, `.template.config/`, samples) and its `docs/FS.GG/` migration docs.
- This repo's `constitution.md` v1.0.0 (layering rule, package-identity rule).

## Expected artifacts after implementation

```text
docs/product/module-map.md
docs/product/layering.md
docs/product/docs-to-import.md
docs/product/decisions/0001-package-identity.md
docs/product/decisions/0002-template-ownership.md
```

## Validation scenarios

Each scenario validates a Success Criterion from the spec by review.

### V1 — Ownership boundary readable from the map alone (SC-001, SC-002)

1. Open `docs/product/module-map.md`.
2. Confirm every area in FR-001 appears with a responsibility and a disposition; no blank
   dispositions.
3. Hand the file to someone unfamiliar with the project; ask them to state what rendering
   owns and what each module does. **Pass** if they can, without opening source.

### V2 — Layer classification is unambiguous (SC-003)

Using `docs/product/layering.md` only, classify these cases to a single layer:

| Case | Expected layer |
|---|---|
| Add a new `DatePicker` control with keyboard/focus behavior | Control |
| Add a `spacing.lg` token used across themes | Design system |
| Add a Fluent-styled skin for existing `Button` | Theme |
| Add `AntDesign.Form` with validation-flow layout | Kit / pattern |
| Propose a separate `FluentButton` behavior type | **Rejected** by one-control-set rule |

**Pass** if all five resolve as above with no overlap.

### V3 — Decisions are definite (SC-004)

1. Open both files in `docs/product/decisions/`.
2. Confirm each has a definite Decision, a Rationale, and a Revisit trigger; none say
   "decide later" without a stated trigger/options.
3. Confirm `0001-package-identity.md` keeps `FS.Skia.UI.*` (agrees with the constitution).

### V4 — Docs-to-import list is actionable (SC-005)

1. Open `docs/product/docs-to-import.md`.
2. Confirm every entry has a disposition (`import-as-is` / `adapt` / `exclude`).
3. **Pass** if a reviewer could execute the import at Stage R4 without re-asking what to do
   with any listed doc.

### V5 — R2 exit criteria satisfiable (SC-006)

Confirm, from the artifacts together: maintainers can explain what rendering owns; the four
layers have distinct boundaries; the rebrand is explicitly deferred. These are the Stage R2
exit criteria from the rendering implementation plan.

## Done when

- All five artifacts exist and conform to their [`contracts/`](./contracts/).
- V1–V5 pass on review.
- The `checklists/requirements.md` items remain satisfied.

# Quickstart / Validation: Define the Initial Validation Set (Stage R3)

This feature ships decision artifacts under `docs/validation/`; "running it" means
producing them and validating by review. No build or test commands apply at this stage.

## Prerequisites

- Read access to the source test surface at `/home/developer/projects/FS-Skia-UI/tests/`,
  `readiness/surface-baselines`, and `template/base/tests/`.
- R2 outputs: `docs/product/module-map.md`, `docs/product/docs-to-import.md`.
- Constitution v1.0.0 (Development Workflow + Principle V).

## Expected artifacts after implementation

```text
docs/validation/README.md
docs/validation/justification-records.md
docs/validation/validation-set.md
docs/validation/deferral-ledger.md
docs/validation/harness.md
```

## Validation scenarios

### V1 — Every candidate is justified and decided (SC-001)

1. Open `docs/validation/justification-records.md`.
2. Confirm every reviewed candidate has all six fields (contract, failure mode, owner,
   frequency, cost) + a decision ∈ {import-now, defer, archive, rewrite-smaller}.
3. Confirm all seven candidate classes appear (runtime unit, surface-drift, package,
   template, docs build, historical readiness, fixtures). **Pass** if none are undecided.

### V2 — Active set is bounded and frequency-labeled (SC-002, SC-003)

1. Open `docs/validation/validation-set.md`.
2. Confirm every member has a frequency label and appears in exactly one group.
3. Confirm the Local inner-loop group is enumerated and small enough to run routinely.
4. Confirm Release-only checks are a separate group with no overlap with Local. **Pass**
   if all hold.

### V3 — Deferred work is captured, non-binding (SC-004)

1. Open `docs/validation/deferral-ledger.md`.
2. Confirm every non-`import-now` candidate appears with a reason and the "not an active
   obligation" marker. **Pass** if nothing is silently dropped.

### V4 — Harness recorded as infrastructure (SC-005)

1. Open `docs/validation/harness.md`.
2. Confirm the harness is classified as deliberate infrastructure (build at Stage R5),
   distinct from imported legacy tests. **Pass** if so.

### V5 — R3 exit criteria satisfiable (SC-006)

Confirm from the artifacts together: the set is small enough for routine work; every
imported check has a justification record; deferred checks are preserved but non-binding;
release-only checks are separated from local checks.

## Done when

- All five artifacts exist and conform to their [`contracts/`](./contracts/).
- V1–V5 pass on review.
- `checklists/requirements.md` items remain satisfied.
- No `src/`/`tests/` changes and no harness code (those are Stage R4/R5).

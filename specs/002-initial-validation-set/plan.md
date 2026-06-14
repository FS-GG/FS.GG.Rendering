# Implementation Plan: Define the Initial Validation Set (Migration Stage R3)

**Branch**: `002-initial-validation-set` *(auto-created by the git extension's before_specify hook)* | **Date**: 2026-06-14 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-initial-validation-set/spec.md`

## Summary

Produce the Stage R3 validation-set decision: a justification record for each candidate
test/check in the source repository (FS-Skia-UI), a bounded "import now" active set
partitioned by frequency, a deferral/archive ledger, and a distinct harness justification
record. These are durable Markdown decision artifacts under `docs/validation/`. No tests
or source are copied (Stage R4) and the harness is not built (Stage R5).

Technical approach: enumerate the source test surface (16 test projects + surface
baselines + 1 template test project, ~221 test files), score each candidate against the six
justification fields, apply the plan's default decisions and the constitution's
"checks must pay for themselves" rule, and record the results. This operationalizes the
constitution's Development Workflow clause (every active check carries a justification:
contract / when it runs / owner / cost).

## Technical Context

**Language/Version**: N/A for this stage — deliverables are Markdown decision artifacts.
(Product stack, for reference: F# on .NET `net10.0`, SkiaSharp over OpenGL.)

**Primary Dependencies**: Source material only — the FS-Skia-UI test surface at
`/home/developer/projects/FS-Skia-UI/tests/**` (Color/Scene/Layout/Input/KeyboardInput/
Elmish/SkiaViewer/Controls/Testing/Lib/Smoke/Parity/Package/Governance/SkillSupport.Tests,
ControlsPreview.Harness), `readiness/surface-baselines` + `scripts/refresh-surface-baselines.fsx`,
`template/base/tests/Product.Tests`, and the R2 outputs (`docs/product/module-map.md`,
`docs/product/docs-to-import.md`).

**Storage**: Files in the repository — durable artifacts under `docs/validation/`; planning
artifacts under `specs/002-initial-validation-set/`.

**Testing**: Review-based acceptance against `checklists/requirements.md` and the spec's
Success Criteria. No automated tests at this stage (no behavior-changing code).

**Target Platform**: N/A (documentation).

**Project Type**: Documentation / decision artifacts (migration Stage R3).

**Performance Goals**: N/A. (The artifacts themselves bound the *active set's* cost so the
local tier stays fast — that is a content requirement, not a build target.)

**Constraints**: Must conform to constitution v1.0.0 — Development Workflow ("checks kept
only when narrow and pay for themselves; each active check carries a justification") and
Principle V (test-evidence philosophy: prefer real evidence; disclose synthetic). MUST NOT
copy tests/source (R4), build the harness (R5), or reintroduce removed governance machinery
(evidence-audit gates, synthetic-evidence ledger, mandatory skill gates).

**Scale/Scope**: 16 source test projects + 1 template test project + surface-baseline checks
to triage (~221 test files in the source); 6 justification fields per candidate; 4 decision
values; 4 frequency labels.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Documentation/decision artifacts, not behavior-changing code. Code-centric principles are
not applicable at the artifact level, but the artifacts must conform to the constitution's
product/process rules — and this feature is, in fact, the one that *operationalizes* the
test-strategy rules.

| Principle | Applies? | Assessment |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | Indirect | No public API created. The set will *shape* future test imports but writes no code now. **PASS** |
| II. Visibility in `.fsi` | N/A | No `.fs`/`.fsi` files. **PASS** |
| III. Idiomatic Simplicity | Yes | Applies to the artifacts: keep records terse, table-driven, reference source rather than duplicate. **PASS** |
| IV. Elmish/MVU boundary | N/A | No stateful/I-O code. **PASS** |
| V. Test Evidence Mandatory | Subject matter | This feature *defines* the test strategy but produces no behavior-changing code, so it owes no test evidence itself. It MUST honor the principle's spirit: prefer real-evidence checks, mark deferrals explicitly, and carry forward the synthetic-disclosure rule (not a governance ledger). **PASS** |
| VI. Observability & Safe Failure | N/A | No runtime. **PASS** |
| Development Workflow (checks pay for themselves; each active check justified) | Yes — central | FR-001..FR-005 produce exactly the constitution's required justification per check (contract, when it runs, owner, cost) and bound the active set. **PASS** |
| Engineering Constraints (no governance machinery; package identity) | Yes | FR-009 forbids reintroducing evidence-audit/synthetic ledger/skill gates; R2's `FS.Skia.UI.*` and layering decisions stand. **PASS** |

**Change Classification**: Artifact-only (documentation/decision). It records the test
strategy that governs future Tier 1 work but changes no API, dependency, or package
contract, so neither code Tier 1 nor Tier 2 obligations apply.

**Result**: No violations. Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/002-initial-validation-set/
├── plan.md              # This file (/speckit-plan command output)
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output — artifact-format contracts
│   ├── justification-record.schema.md
│   ├── validation-set.schema.md
│   └── deferral-ledger.schema.md
└── checklists/
    └── requirements.md  # Spec quality checklist (already created)
```

### Source Code (repository root)

This feature produces no source code or tests. It adds durable validation-strategy
documentation; `src/` and `tests/` are intentionally untouched (test import is Stage R4,
harness build is Stage R5).

```text
docs/
└── validation/
    ├── README.md                 # index / entry point
    ├── justification-records.md  # FR-001/002/007/008: per-candidate records (6 fields + decision)
    ├── validation-set.md         # FR-003/004: the "import now" active set, partitioned by frequency
    ├── deferral-ledger.md        # FR-005/010: deferred / archived / rewrite-pending, non-binding
    └── harness.md                # FR-006: harness as deliberate-infrastructure record
```

**Structure Decision**: Documentation-only feature. Durable artifacts live under
`docs/validation/` (sibling to R2's `docs/product/`), since the validation set is a
process/testing concern rather than product shape. Planning artifacts stay under the feature
folder. No `src/`/`tests/` layout is chosen — no code is written; that belongs to Stage R4/R5.

## Complexity Tracking

No constitution violations — section intentionally empty.

# Implementation Plan: Define Product Shape (Migration Stage R2)

**Branch**: `001-define-product-shape` *(directory-based feature tracking via `.specify/feature.json`; no git branch)* | **Date**: 2026-06-14 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-define-product-shape/spec.md`

## Summary

Produce the product-shape definition for the FS.GG.Rendering migration (Stage R2):
a product/module map, a four-layer design/control layering document (one semantic
control set, many themes), and the open product-shape decision records
(package identity, template ownership, docs-to-import list). These are durable
Markdown decision/definition artifacts that establish *what rendering owns* before
any source is copied (import is the later Stage R4). No F# code, tests, or governance
machinery are produced or imported in this stage.

Technical approach: derive the artifacts from the archived FS-Skia-UI source tree and
its `docs/FS.GG/` migration documents (`design-and-controls.md`, `rendering-project.md`),
reconcile them against this repo's constitution v1.0.0 (layering rule, package-identity
rule), and publish them as durable docs under `docs/product/` with the planning
artifacts kept under the feature folder.

## Technical Context

**Language/Version**: N/A for this stage — deliverables are Markdown documentation/decision
artifacts. (Product stack context, for reference only: F# on .NET `net10.0`, SkiaSharp over
OpenGL.)

**Primary Dependencies**: Source material only — the archived `EHotwagner/FS-Skia-UI`
repository (`src/**`, ~28k LOC) and its `docs/FS.GG/` migration docs (staged locally at
`/home/developer/projects/FS-Skia-UI/docs/FS.GG/` and `/home/developer/projects/FS-GG.github/docs/`).

**Storage**: Files in the repository — durable artifacts under `docs/product/`; planning
artifacts under `specs/001-define-product-shape/`.

**Testing**: Review-based acceptance against `checklists/requirements.md` and the spec's
Success Criteria. No automated tests at this stage (no behavior-changing code; see
Constitution Check, Principle V).

**Target Platform**: N/A (documentation).

**Project Type**: Documentation / decision artifacts (migration Stage R2).

**Performance Goals**: N/A.

**Constraints**: Must conform to constitution v1.0.0 — the layering rule (one semantic
control set, many themes; no per-theme control forks) and the package-identity rule
(`FS.Skia.UI.*` initially, rebrand is a separate release decision). MUST NOT copy runtime
source, import the legacy test suite, or reintroduce removed governance machinery
(feature/product/project graphs, evidence-audit gates, mandatory skill gates).

**Scale/Scope**: ~10 named modules to map; 4 UI layers to define; 2 decision records; 1
docs-to-import list. Source inventory ≈ Controls (11k), SkiaViewer (5.8k), Controls.Elmish
(2.2k), Testing (1.7k), plus Scene, Layout, Input, KeyboardInput, Color, Elmish,
SkillSupport, 13 sample galleries, and a `dotnet new` template.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

This feature delivers documentation/decision artifacts, not behavior-changing F# code.
Code-centric principles are therefore **not applicable at the artifact level**, but the
artifacts MUST stay consistent with the constitution's product rules.

| Principle | Applies? | Assessment |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | Indirect | No public API created. The map/layering inform future `.fsi` surfaces but produce no code now. **PASS** |
| II. Visibility in `.fsi` | N/A | No `.fs`/`.fsi` files in this stage. **PASS** |
| III. Idiomatic Simplicity | Yes | Applies to the artifacts: keep them plain, minimal, and reference source rather than duplicate it. **PASS** |
| IV. Elmish/MVU boundary | N/A | No stateful/I-O code. **PASS** |
| V. Test Evidence Mandatory | Adapted | Targets behavior-changing code; none here. Verification = review against the requirements checklist + Success Criteria. **PASS** |
| VI. Observability & Safe Failure | N/A | No runtime. **PASS** |
| Engineering Constraints (layering rule, package identity) | Yes | FR-004/005 carry the layering rule forward verbatim; FR-006 defaults to `FS.Skia.UI.*` with deferred rebrand, matching the constitution. **PASS** |
| Development Workflow (standard Spec Kit, explicit deferrals) | Yes | Standard `specify → plan → tasks`; R3/R4/R5 explicitly deferred as bounded follow-ups. **PASS** |

**Change Classification**: Artifact-only (documentation/decision). It does not change public
API surface, dependencies, or package contracts — it *records intent* that governs future
Tier 1 work — so neither the code Tier 1 nor Tier 2 obligations (e.g. `.fsi`/baseline
updates) apply. No surface-area baselines exist yet to update.

**Result**: No violations. Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/001-define-product-shape/
├── plan.md              # This file (/speckit-plan command output)
├── spec.md              # Feature specification
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output — artifact-format contracts
│   ├── module-map.schema.md
│   ├── layering.schema.md
│   └── decision-record.schema.md
└── checklists/
    └── requirements.md  # Spec quality checklist (already created)
```

### Source Code (repository root)

This feature produces no source code. It adds durable product-shape documentation to the
repository; `src/` and `tests/` are intentionally untouched (source import is Stage R4).

```text
docs/
└── product/
    ├── module-map.md             # FR-001..FR-003: areas, responsibilities, dispositions
    ├── layering.md               # FR-004..FR-005: 4 layers + one-control-set rule
    ├── docs-to-import.md         # FR-008: source docs with import disposition
    └── decisions/
        ├── 0001-package-identity.md     # FR-006: keep FS.Skia.UI.* / defer rebrand
        └── 0002-template-ownership.md   # FR-007: rendering owns templates (for now)
```

**Structure Decision**: Documentation-only feature. Durable, discoverable artifacts live
under `docs/product/` (the authoritative answer to "what does rendering own?"), with
decision records as lightweight ADR-style notes under `docs/product/decisions/`. Planning
artifacts stay under the feature folder. No `src/`/`tests/` layout is chosen because no code
is written; that decision belongs to Stage R4.

## Complexity Tracking

No constitution violations — section intentionally empty.

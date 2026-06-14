# Docs to import

> Migration Stage R2 deliverable. Triage of source documents for the rendering product, each
> marked **import-as-is**, **adapt**, or **exclude**. This list is the doc-import checklist for
> Stage R4. Source: `EHotwagner/FS-Skia-UI` migration docs at `docs/FS.GG/` (staged locally at
> `/home/developer/projects/FS-GG.github/docs/`).

## Migration docs (`docs/FS.GG/`)

| Source document | Disposition | Note |
|---|---|---|
| `design-and-controls.md` | adapt | Already adapted into [`layering.md`](./layering.md) this stage; import the source as provenance/history only, not as a second source of truth. |
| `rendering-project.md` | adapt | Rendering scope/overview; refresh to describe the current repo and drop split-era framing. |
| `transition-and-boundaries.md` | adapt | Bridge/archive + package-identity handling; keep the parts that describe this repo's boundaries; defer bridge specifics to Stage R7. |
| `research-notes.md` | adapt | Durable findings (Spec Kit, NuGet/template identity, fsdocs); import the still-current notes, prune stale ones. |
| `rendering-implementation-plan.md` | import-as-is | The active R1→R8 plan; keep as the migration roadmap reference. |
| `index.md` | adapt | Split-direction overview; trim to a short pointer once docs land here. |
| `project-split-decision.md` | import-as-is | Decision record (why split); useful provenance, keep verbatim. |
| `governance-project.md` | exclude | Belongs to the deferred governance repository, not the rendering product. |
| `governance-implementation-plan.md` | exclude | Governance repo concern (stages G1–G5); out of scope here. |
| `implementation-plan.md` | exclude | Cross-repo coordination/ordering doc; lives with the org `.github`/governance, not the rendering product. |

## Product / architecture docs

| Source document | Disposition | Note |
|---|---|---|
| Per-module `README.md` files under `src/**` | adapt | Import alongside their module at Stage R4; refresh references to retired governance assumptions. |
| Architecture docs / ADRs that remain current | adapt | Import only those describing current product behavior; record `TODO(STRUCTURED_LOGGING)` as an open ADR. |
| Historical readiness logs / synthetic-evidence reports | exclude | Superseded; the constitution removed the evidence-audit machinery. Leave in the archive, not active state. |

## Notes

- Sample-gallery docs and test docs are decided with the validation set at **Stage R3**, not
  here.
- Every entry above carries a disposition, so a reviewer can execute the import at Stage R4
  without re-asking what to do with any listed doc.

---
schemaVersion: 1
workId: typed-sdd-p4-rendering
title: Typed Sdd P4 Rendering
stage: plan
changeTier: tier1
status: planned
sourceSpec: work/typed-sdd-p4-rendering/spec.md
sourceClarifications: work/typed-sdd-p4-rendering/clarifications.md
sourceChecklist: work/typed-sdd-p4-rendering/checklist.md
publicOrToolFacingImpact: true
---

# Typed Sdd P4 Rendering Plan

Prose status: planned

## Source Snapshot
- spec: work/typed-sdd-p4-rendering/spec.md sha256:299fc616b9cc7979f7ce97f56abdf6595847ea1aad63dce7ae0eac6fb398e2d8 schemaVersion:1
- clarifications: work/typed-sdd-p4-rendering/clarifications.md sha256:4209ed34384386313ad4cffac2fce7293e0640d93f067b85664e9e765d39ced8 schemaVersion:1
- checklist: work/typed-sdd-p4-rendering/checklist.md sha256:a2b528d200e9c16b5c71049636da6411afd39525cdbde4ea54637ca86a3ccec0 schemaVersion:1

## Plan Scope
- Work item typed-sdd-p4-rendering is planned from the current specification, clarification, and checklist facts.
- Requirement count: 1.
- Clarification decision count: 0.
- Checklist result count: 1.

## Plan Decisions
- PD-001 [AC-001] [FR-001] complete: Extend the existing template lifecycle choice with `typed-sdd`; keep `sdd` as `defaultValue`, preserve `none`, and leave `spec-kit` frozen. Treat `typed-sdd` like `sdd` at the raw product boundary except for a lifecycle-specific fail-closed sentinel message, because FS.GG.SDD supplies the canonical Typed SDD workspace after provider instantiation.

## Contract Impact
- PC-001 [PD-001] template lifecycle choice: `.template.config/template.json` gains the additive public value `typed-sdd`; conditional content and validation scripts must distinguish it from unknown input while retaining omitted `sdd` semantics.

## Verification Obligations
- VO-001 [PD-001] [PC-001] semanticTest: Instantiate the packed template for omitted, `sdd`, `none`, `typed-sdd`, and invalid values; prove omitted equals explicit `sdd`, Typed SDD carries its intended guard, unknown input fails, and a mutated default makes the focused gate red.

## Performance Intent
No performance intent is declared for this work item.

## Migration Posture
- PM-001 [PC-001] additiveChoice: Existing explicit and omitted invocations retain their byte contract. No existing workspace is rewritten; consumers adopt `typed-sdd` only by explicit selection until P5.

## Generated View Impact
- GV-001 [PD-001] packageProjection: Template help, generated product documentation, lifecycle sentinel prose, and package-test expectations are regenerated or updated together; readiness views refresh from the final implementation evidence.

## Accepted Deferrals
No accepted plan deferrals recorded.

## Planning Findings
No blocking planning findings recorded.

## Advisory Notes
- Optional Governance pointers remain compatibility facts only.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd tasks --work typed-sdd-p4-rendering`.

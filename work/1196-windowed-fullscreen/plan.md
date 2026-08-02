---
schemaVersion: 1
workId: 1196-windowed-fullscreen
title: Windowed Fullscreen
stage: plan
changeTier: tier1
status: planned
sourceSpec: work/1196-windowed-fullscreen/spec.md
sourceClarifications: work/1196-windowed-fullscreen/clarifications.md
sourceChecklist: work/1196-windowed-fullscreen/checklist.md
publicOrToolFacingImpact: true
---

# Windowed Fullscreen Plan

Prose status: planned

## Source Snapshot
- spec: work/1196-windowed-fullscreen/spec.md sha256:c046b97d40053db9271e66ba6952f72a9c02e0cbb1002e0d51afc4ffdde4b86a schemaVersion:1
- clarifications: work/1196-windowed-fullscreen/clarifications.md sha256:a9d9294f6c09a71e737fee34d0e8602a227730ddd2bd193a4d7942d782683004 schemaVersion:1
- checklist: work/1196-windowed-fullscreen/checklist.md sha256:ab050901a90e7c7e64d2a7b710d8b3cc6bc661ab3c707b4d94f656d21c96d2ff schemaVersion:1

## Plan Scope
- Work item 1196-windowed-fullscreen is planned from the current specification, clarification, and checklist facts.
- Requirement count: 1.
- Clarification decision count: 0.
- Checklist result count: 1.

## Plan Decisions
- PD-001 [AC-001] [FR-001] complete: Defer WindowedFullscreen geometry resolution to the live native window and select its current monitor's bounds; this prevents a primary-monitor plan from moving a secondary-monitor window off screen.

## Contract Impact
- PC-001 [PD-001] public API: RuntimeWindowTarget gains a work-area reader used only for WindowedFullscreen; its fake implementation makes the native transition policy testable without a compositor.

## Verification Obligations
- VO-001 [PD-001] [PC-001] semanticTest: Build SkiaViewer and run the Issue1022 native-target regression proving that the active monitor work area overrides stale planned geometry; inspect the game-shell guidance for safe defaults and field-overlay semantics.

## Performance Intent
No performance intent is declared for this work item.

## Migration Posture
- PM-001 [PC-001] none: This is an additive internal test seam; no persisted format or consumer migration is required.

## Generated View Impact
- GV-001 [PD-001] workModel: Refresh readiness views after authoring this decision so their source digests bind the implementation route and verification evidence.

## Accepted Deferrals
No accepted plan deferrals recorded.

## Planning Findings
No blocking planning findings recorded.

## Advisory Notes
- Optional Governance pointers remain compatibility facts only.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd tasks --work 1196-windowed-fullscreen`.

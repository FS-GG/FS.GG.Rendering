# Tasks: Retained Pointer Routing → Authored Control ID (Feature 110)

**Input**: Design documents from `/specs/110-retained-authored-routing/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/retained-authored-routing.md, quickstart.md

**Tests**: Conformance pass — the implementation (`authoredControlIds` in `RetainedRender`; `routeRetained*` +
`FrameMetrics.FullRenderFallbackCount` in `Controls.Elmish`) and the three `Elmish.Tests` suites already exist.
110 imported with **no `readiness/`** — authoring it is the one genuine deliverable. No new behaviour is built.

**Organization**: by user story.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup

- [X] T001 Build clean: `dotnet build FS.GG.Rendering.slnx -c Release` — 0 warn / 0 err
- [X] T002 [P] Confirm `authoredControlIds` in `src/Controls/RetainedRender.fsi` (internal) and `routeRetainedInteraction`/`routeRetainedPointer` + `FrameMetrics.FullRenderFallbackCount` in `src/Controls.Elmish/ControlsElmish.fsi`
- [X] T003 [P] Confirm the three suites exist: `tests/Elmish.Tests/Feature110RetainedRoutingTests.fs`, `Feature110RetainedRoutingParityTests.fs`, `Feature110FallbackTests.fs`

## Phase 2: Foundational (gates)

- [X] T004 Verify signatures vs `contracts/retained-authored-routing.md` (C1–C3): `authoredControlIds: Set<ControlId> -> RetainedRender -> Map<RetainedId, ControlId>`; the `routeRetained*` internals; `FullRenderFallbackCount: int` on public `FrameMetrics` (FR-003, FR-009, FR-013)
- [X] T005 Confirm zero new public-surface delta: `git status -s tests/surface-baselines/` empty — routing internal, `FullRenderFallbackCount` additive on already-baselined `FrameMetrics` (FR-013)

## Phase 3: User Story 1 — zero-render routing + coalescing (P1) 🎯 MVP

- [X] T006 [US1] Run `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release --filter "110"`; confirm in `Feature110RetainedRoutingTests.fs`: a routed move/click reports `FullRenderCount=0`, `ViewCalled=false`, `FullRenderFallbackCount=0` (SC-001/SC-002; FR-002/FR-004/FR-008)
- [X] T007 [US1] Confirm the move-burst coalescing (≤ 1 processed move, zero routing renders) and the interleaved-move / drag-freehand fidelity assertions (SC-009/FR-012)
- [X] T008 [P] [US1] Author `readiness/us1-zero-render-routing.md` against SC-001/SC-002/SC-009

## Phase 4: User Story 2 — dispatch parity with the oracle (P1)

- [X] T009 [US2] Confirm in `Feature110RetainedRoutingParityTests.fs`: keyed controls + nested containers dispatch identically to the oracle (SC-003); unkeyed siblings each fire their own binding (SC-004/FR-005); composite binding-above (FR-003); `MapPointer` + focus-identity parity (FR-006)
- [X] T010 [P] [US2] Author `readiness/us2-oracle-parity.md` against SC-003/SC-004

## Phase 5: User Story 3 — counted full-render fallback (P2)

- [X] T011 [US3] Confirm in `Feature110FallbackTests.fs`: every normal scenario reports `FullRenderFallbackCount = 0` (SC-005); a constructed unroutable case increments by exactly one and matches the oracle (SC-006); a resolvable hit takes no fallback (FR-007/FR-009)
- [X] T012 [P] [US3] Author `readiness/us3-counted-fallback.md` against SC-005/SC-006

## Phase 6: Polish

- [X] T013 Full suite `dotnet test FS.GG.Rendering.slnx -c Release` — 0 failures (standing skips unrelated to 110)
- [X] T014 Re-confirm zero new public-surface delta (FR-013)
- [X] T015 [P] Verify readiness → SC mapping in quickstart; each readiness file discloses deterministic / message-list-parity scope (no pixel claim)
- [X] T016 Record DF-1 (redundant access modifiers in `.fs`) as out-of-scope (Complexity Tracking) — not edited here
- [X] T017 Run `/speckit-analyze` for cross-artifact consistency

## Dependencies & Parallel

- Setup → Foundational (gates) → US1–US3 (parallel; same suite, distinct assertions) → Polish.
- [P] readiness authoring (T008/T010/T012) writes distinct files.

## Notes

- No source edits beyond making suites green; a red test is a finding, not a redesign license. DF-1 deferred (T016).
- The genuine deliverable is the readiness evidence (110 imported without it; tests do not self-write).
- Surface gate (T005/T014) is the direct check of FR-013 (no separate SC).
</content>

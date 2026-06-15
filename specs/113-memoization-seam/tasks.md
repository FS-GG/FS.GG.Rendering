# Tasks: Memoization Seam (DataGrid) (Feature 113)

**Input**: Design documents from `/specs/113-memoization-seam/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/memoization-seam.md, quickstart.md

**Tests**: Conformance pass — the `memoize` seam, the `MemoEnabled` oracle, the public `FrameMetrics.MemoHitCount`/
`MemoMissCount`, `Diagnostics.stabilityReport`, and the four suites already exist. 113 imported with **no
`readiness/`** — authoring it is the genuine deliverable. No new behaviour is built.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup

- [X] T001 Build clean: `dotnet build FS.GG.Rendering.slnx -c Release` — 0/0
- [X] T002 [P] Confirm `memoize`/`MemoOutcome`/`MemoEntry`/`MemoCache`/`Memo`/`MemoEnabled` (internal) in `src/Controls/RetainedRender.fsi` and `FrameMetrics.MemoHitCount`/`MemoMissCount` in `src/Controls.Elmish/ControlsElmish.fsi`
- [X] T003 [P] Confirm the four suites: `tests/Controls.Tests/Feature113Memo{Seam,Parity}Tests.fs`, `Feature113StabilityDiagTests.fs`, `tests/Elmish.Tests/Feature113MemoMetricsTests.fs`

## Phase 2: Foundational (gates)

- [X] T004 Verify signatures vs `contracts/memoization-seam.md` (C1–C4): `memoize: id -> dependency:obj -> compute -> cache -> Scene list * MemoCache * MemoOutcome`; `MemoEnabled: bool`; `MemoHitCount`/`MemoMissCount: int` on public `FrameMetrics` (FR-004/005/008/009)
- [X] T005 Confirm zero new public-surface delta: `git status -s tests/surface-baselines/` empty — seam internal, metrics additive on already-baselined `FrameMetrics` (FR-013)

## Phase 3: User Story 1 — the memoize seam (P1) 🎯 MVP

- [X] T006 [US1] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "113"`; confirm `Feature113MemoSeamTests`: cold Miss (thunk once + stored), stable Hit (same instance via `ReferenceEquals`, no thunk), changed Miss, structural-equality Hit (FR-005), per-`ControlId` cold miss (FR-001/004; C1–C3)
- [X] T007 [P] [US1] Author `readiness/us1-hit-miss.md`

## Phase 4: User Story 2 — memo-on ≡ memo-off + no staleness (P1)

- [X] T008 [US2] Confirm `Feature113MemoParityTests`: every frame byte-identical seam-active vs forced always-miss (C5/SC-002); real Hit on unchanged data; changed inputs → Miss + fresh different scene = memo-off build (C6/FR-006/FR-007)
- [X] T009 [P] [US2] Author `readiness/us2-parity-no-staleness.md` against SC-002

## Phase 5: User Story 3 — memo metrics (P2)

- [X] T010 [US3] Run `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release --filter "113"`; confirm `Feature113MemoMetricsTests`: steady-state `MemoHitCount > 0`/`MemoMissCount = 0` (SC-004); perturbed misses; idle 0/0; no-memoizable 0/0 (C7/C8; FR-009/FR-010)
- [X] T011 [P] [US3] Author `readiness/us3-metrics.md` against SC-004

## Phase 6: User Story 4 — stability diagnostic (P2)

- [X] T012 [US4] Confirm `Feature113StabilityDiagTests`: stable rebuild → no findings (FR-012); per-frame event closure / always-new value / unstable key → exactly the offending node flagged `UnstableReuseInput` (FR-011)
- [X] T013 [P] [US4] Author `readiness/us4-stability-diagnostic.md`

## Phase 7: Polish

- [X] T014 Full suite `dotnet test FS.GG.Rendering.slnx -c Release` — 0 failures (standing skips unrelated to 113)
- [X] T015 Re-confirm zero new public-surface delta (FR-013)
- [X] T016 [P] Verify readiness → SC mapping; each file discloses deterministic / Hit-Miss-and-scene-equality scope (no pixel claim)
- [X] T017 Record DF-1 (redundant access modifiers in `.fs`) AND the `MemoEnabled` doc-comment narrative nit as out-of-scope, routed to Workstream E (E1 / E2) — not edited here
- [X] T018 Run `/speckit-analyze` for cross-artifact consistency

## Dependencies & Parallel

- Setup → Foundational (gates) → US1–US4 (parallel; distinct suites/assertions) → Polish.
- [P] readiness authoring (T007/T009/T011/T013) writes distinct files.

## Notes

- No source edits in this backfill. The `MemoEnabled` narrative nit is a recorded finding routed to E2 (T017),
  NOT fixed here — keeping all seven backfills uniform (behavior-neutral, like DF-1).
- The genuine deliverable is the readiness evidence (113 imported without it; tests do not self-write).
- Surface gate (T005/T015) is the direct check of FR-013.
</content>

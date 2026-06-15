# Tasks: Frame-Rate Pacing & No-Alloc Idle Tick (Feature 121)

**Input**: Design documents from `/specs/121-paced-noalloc-tick/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/paced-noalloc-tick.md, quickstart.md

**Tests**: Conformance pass — `advanceStateClocks`, `GlHost.shouldAdvanceFrame`, `ViewerOptions.FrameRateCap`
+ validation, and the two suites already exist. 121 imported with **no `readiness/`** — authoring it is the
genuine deliverable. No new behaviour is built.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup

- [X] T001 Build clean: `dotnet build FS.GG.Rendering.slnx -c Release` — 0/0
- [X] T002 [P] Confirm `advanceStateClocks` (internal) in `src/Controls/RetainedRender.fsi`; `GlHost.shouldAdvanceFrame` in `src/SkiaViewer/Host/OpenGl.fsi`; `ViewerOptions.FrameRateCap` + non-positive validation in `src/SkiaViewer/SkiaViewer.fs`
- [X] T003 [P] Confirm the two suites: `tests/Controls.Tests/Feature121IdleTickTests.fs`, `tests/SkiaViewer.Tests/Feature121LiveHostPacingTests.fs`

## Phase 2: Foundational (gates)

- [X] T004 Verify signatures vs `contracts/paced-noalloc-tick.md` (C1–C3): `advanceStateClocks: delta -> Map<RetainedId, RetainedUiState> -> Map<...>`; `shouldAdvanceFrame: float -> float -> float -> bool`; `ViewerOptions.FrameRateCap` (FR-002/004/005)
- [X] T005 Confirm zero new public-surface delta: `git status -s tests/surface-baselines/` empty — `advanceStateClocks` internal; `shouldAdvanceFrame`/`FrameRateCap` on already-baselined public types (FR-005)

## Phase 3: User Story 1 — frame-rate pacing (P1) 🎯 MVP

- [X] T006 [US1] Run `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release --filter "121"`; confirm `Feature121LiveHostPacingTests`: `shouldAdvanceFrame` false-before / true-at-and-after the interval (SC-001); a tighter cap (30 vs 60 fps) yields strictly fewer advances over the same window, each within tolerance (FR-002/SC-001)
- [X] T007 [US1] Confirm the validation seam: non-positive `FrameRateCap` (0 and negative) rejected as `Classification = ProductDefect` ("frame-rate cap") (FR-003/SC-005); a positive cap clears validation (FR-001)
- [X] T008 [P] [US1] Author `readiness/us1-frame-rate-pacing.md` against SC-001/SC-005

## Phase 4: User Story 2 — no-alloc idle tick (P1)

- [X] T009 [US2] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "121"`; confirm `Feature121IdleTickTests`: no active clock (empty + settled) ⇒ `advanceStateClocks` returns the state `obj.ReferenceEquals` (no allocation) (SC-003/FR-004)
- [X] T010 [US2] Confirm an active clock ⇒ result is NOT reference-equal (rebuilt) and `Elapsed` advanced by the delta, equal to `RetainedRender.advance` on the same clock (099/103 unchanged) (FR-004)
- [X] T011 [P] [US2] Author `readiness/us2-no-alloc-idle.md` against SC-003

## Phase 5: Polish

- [X] T012 Full suite `dotnet test FS.GG.Rendering.slnx -c Release` — 0 failures (standing skips unrelated to 121)
- [X] T013 Re-confirm zero new public-surface delta (FR-005)
- [X] T014 [P] Verify readiness → SC mapping; each file discloses deterministic / reference-equality + pure-decision scope (no pixel claim; the persistent window is not driven)
- [X] T015 Record DF-1 (redundant access modifiers in `.fs`) as out-of-scope (Complexity Tracking) — not edited here
- [X] T016 Run `/speckit-analyze` for cross-artifact consistency

## Dependencies & Parallel

- Setup → Foundational (gates) → US1 + US2 (parallel; distinct suites/assemblies) → Polish.
- [P] readiness authoring (T008/T011) writes distinct files.

## Notes

- No source edits in this backfill; a red test is a finding, not a redesign license. DF-1 deferred (T015).
- The genuine deliverable is the readiness evidence (121 imported without it; tests do not self-write).
- Surface gate (T005/T013) is the direct check of FR-005. The live clock advance/sample (099) and cross-fade
  (103) are unchanged by 121.
</content>

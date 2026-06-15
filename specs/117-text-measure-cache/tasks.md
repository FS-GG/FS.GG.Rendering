# Tasks: Text-Measure Cache (LRU) (Feature 117)

**Input**: Design documents from `/specs/117-text-measure-cache/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/text-measure-cache.md, quickstart.md

**Tests**: Conformance pass — `measureTextCached`, the bounded cache, the always-miss oracle, the public
`FrameMetrics` text + layout-invalidated fields, and five suites (incl. `Audit_TextCache`) already exist. 117
imported with **no `readiness/`** — authoring it is the genuine deliverable. No new behaviour is built.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup

- [X] T001 Build clean: `dotnet build FS.GG.Rendering.slnx -c Release` — 0/0
- [X] T002 [P] Confirm `TextMeasureKey`/`TextMeasureCache`/`measureTextCached`/`TextMeasureCacheCap`/`TextCache`/`TextCacheEnabled` (internal) in `src/Controls/RetainedRender.fsi`; `FrameMetrics.TextMeasureCacheHitCount`/`MissCount`/`LayoutInvalidatedNodeCount` in `src/Controls.Elmish/ControlsElmish.fsi`
- [X] T003 [P] Confirm the five suites: `tests/Controls.Tests/Feature117{TextCache,CacheBound,LayoutInvalidated}Tests.fs`, `Audit_TextCache.fs`, `tests/Elmish.Tests/Feature117MetricsTests.fs`

## Phase 2: Foundational (gates)

- [X] T004 Verify signatures vs `contracts/text-measure-cache.md` (C1–C4): `measureTextCached: cache -> enabled -> text -> font -> TextMetrics * cache * bool`; `TextMeasureKey { Text; Family; Size; Weight }`; `TextMeasureCacheCap = 256`; the three public metric fields (FR-001/003/006/011)
- [X] T005 Confirm zero new public-surface delta: `git status -s tests/surface-baselines/` empty — cache internal, the three metric fields additive on already-baselined `FrameMetrics` (FR-011)

## Phase 3: User Story 1 — text-measure cache (P1) 🎯 MVP

- [X] T006 [US1] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "117"`; confirm `Feature117TextCacheTests`: cold Miss → warm Hit (byte-identical metrics) (FR-001/SC-001/SC-002); one differing keyed field misses (FR-002); empty/whitespace caches; fitted-caption distinct sizes; always-miss oracle byte-identical scene + layout + `RemeasuredNodeCount`, hits=0 (FR-004/SC-004)
- [X] T007 [US1] Confirm `Feature117CacheBoundTests`: `Entries.Count ≤ cap` under pressure; deterministic eviction; evicted re-miss correct, no stale hit; in-cap sweep evicts nothing (FR-003/SC-005)
- [X] T008 [US1] Confirm `Audit_TextCache`: adversarial single-field key-completeness; cache-on ≡ cache-off discriminating; effectiveness (>95% hit rate vs disabled oracle) (FR-004/009)
- [X] T009 [P] [US1] Author `readiness/us1-text-cache.md` against SC-001/002/004/005

## Phase 4: User Story 2 — style-only zero-work + layout-invalidated (P2)

- [X] T010 [US2] Confirm `Feature117LayoutInvalidatedTests`: idle → 0 invalidated/re-measured (FR-006); style-only / visual-state-only → 0 invalidated / 0 re-measured / 0 text misses over warm text (FR-006/FR-007/SC-003); geometry → `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount`, both > 0 (FR-006/SC-006); feature-101 name set = `{width;height;orientation}` (FR-008)
- [X] T011 [P] [US2] Author `readiness/us2-style-only-zero-work.md` against SC-003/SC-006

## Phase 5: User Story 3 — metrics over a host script (P2)

- [X] T012 [US3] Run `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release --filter "117"`; confirm `Feature117MetricsTests`: cold misses → warm hits + zero misses (SC-001/SC-002); style-only zeros; idle all-three 0; geometry `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount` (SC-006); three metrics re-run byte-identically (FR-005/006/010)
- [X] T013 [P] [US3] Author `readiness/us3-metrics.md` against SC-001/002/003/006

## Phase 6: Polish

- [X] T014 Full suite `dotnet test FS.GG.Rendering.slnx -c Release` — 0 failures (standing skips unrelated to 117)
- [X] T015 Re-confirm zero new public-surface delta (FR-011)
- [X] T016 [P] Verify readiness → SC mapping; each file discloses deterministic / byte-identical-metrics scope (no pixel claim)
- [X] T017 Record DF-1 (redundant access modifiers in `.fs`) as out-of-scope (Complexity Tracking) — not edited here
- [X] T018 Run `/speckit-analyze` for cross-artifact consistency

## Dependencies & Parallel

- Setup → Foundational (gates) → US1–US3 (parallel; distinct suites/assertions) → Polish.
- [P] readiness authoring (T009/T011/T013) writes distinct files.

## Notes

- No source edits in this backfill; a red test is a finding, not a redesign license. DF-1 deferred (T017).
- The genuine deliverable is the readiness evidence (117 imported without it; tests do not self-write).
- Surface gate (T005/T015) is the direct check of FR-011. `LayoutInvalidatedNodeCount` shares the dirty-set
  with feature 097 (097 owns `layoutDirtySet`/`RemeasuredNodeCount`; 117 surfaces this metric).
</content>

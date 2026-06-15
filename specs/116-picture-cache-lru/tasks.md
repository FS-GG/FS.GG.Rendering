# Tasks: Picture Cache (LRU) & Damage Set (Feature 116)

**Input**: Design documents from `/specs/116-picture-cache-lru/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/picture-cache.md, quickstart.md

**Tests**: Conformance pass — the damage set, the bounded picture cache, `offscreenEffect`, the public
`FrameMetrics` damage + cache fields, and six suites (incl. `Audit_PictureCache`) already exist. 116 imported
with **no `readiness/`** — authoring it is the genuine deliverable. No new behaviour is built.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup

- [X] T001 Build clean: `dotnet build FS.GG.Rendering.slnx -c Release` — 0/0
- [X] T002 [P] Confirm `PictureCacheKey`/`PictureCache`/`PictureCacheCap`/`offscreenEffect` (internal) + damage/cache `WorkReductionRecord` fields in `src/Controls/RetainedRender.fsi`; the public `FrameMetrics` damage + cache fields in `src/Controls.Elmish/ControlsElmish.fsi`
- [X] T003 [P] Confirm the six suites: `tests/Controls.Tests/Feature116{Damage,PictureCache,CacheBound,OffscreenDiag}Tests.fs`, `Audit_PictureCache.fs`, `tests/Elmish.Tests/Feature116MetricsTests.fs`

## Phase 2: Foundational (gates)

- [X] T004 Verify signatures vs `contracts/picture-cache.md` (C1–C5): `PictureCacheKey { Box; Fingerprint }`; `PictureCache { Entries; Clock }`; `PictureCacheCap = 256`; `offscreenEffect: Scene list -> string option`; public damage + cache `FrameMetrics` fields (FR-005/009/011/014)
- [X] T005 Confirm zero new public-surface delta: `git status -s tests/surface-baselines/` empty — cache internal, metrics additive on already-baselined `FrameMetrics` (FR-014)

## Phase 3: User Story 1 — damage set (P1) 🎯 MVP

- [X] T006 [US1] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "116"`; confirm `Feature116DamageTests`: idle 0/0/0 (FR-003); localized small vs theme-switch frame-spanning (SC-001/FR-001/002); deduped `DirtyRectCount`; deterministic re-run (FR-004)
- [X] T007 [P] [US1] Author `readiness/us1-damage-set.md` against SC-001

## Phase 4: User Story 2 — picture cache reuse + parity (P1)

- [X] T008 [US2] Confirm `Feature116PictureCacheTests`: unchanged boundary Hit, byte-identical to a rebuild (FR-005/SC-002); content/box/theme change misses exactly the affected rows (FR-006); cache-on ≡ cache-off (FR-007/SC-003)
- [X] T009 [P] [US2] Author `readiness/us2-picture-cache.md` against SC-002/SC-003

## Phase 5: User Story 3 — bounded deterministic LRU (P2)

- [X] T010 [US3] Confirm `Feature116CacheBoundTests`: under cap no eviction + all reused; over cap `PictureCacheEntryCount ≤ cap` (SC-004/FR-009); deterministic survivors (FR-010); evicted re-miss correct, no stale hit (FR-010/SC-004)
- [X] T011 [P] [US3] Author `readiness/us3-bounded-lru.md` against SC-004

## Phase 6: User Story 4 — offscreen-effect detector (P2)

- [X] T012 [US4] Confirm `Feature116OffscreenDiagTests`: drop-shadow/image-filter/path-clip/non-opaque-over-group flagged; plain opaque + `RectClip` NOT flagged; advisory diagnostic via `step` (SC-005/FR-011)
- [X] T013 [P] [US4] Author `readiness/us4-offscreen-detector.md` against SC-005

## Phase 7: User Story 5 — metrics (P2)

- [X] T014 [US5] Run `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release --filter "116"`; confirm `Feature116MetricsTests`: idle 0; stable reuse zero damage; localized small damage + single miss; bounded under pressure (SC-007); six metrics re-run byte-identically (SC-006/FR-012/013)
- [X] T015 [US5] Confirm `Audit_PictureCache`: present-but-dead (hits provably move), cache-on ≡ cache-off discriminating, effectiveness (hits ≫ 0, misses → 0)
- [X] T016 [P] [US5] Author `readiness/us5-metrics.md` against SC-006/SC-007

## Phase 8: Polish

- [X] T017 Full suite `dotnet test FS.GG.Rendering.slnx -c Release` — 0 failures (standing skips unrelated to 116)
- [X] T018 Re-confirm zero new public-surface delta (FR-014)
- [X] T019 [P] Verify readiness → SC mapping; each file discloses deterministic / render-only scope (no pixel claim; backend replay is 120)
- [X] T020 Record DF-1 (redundant access modifiers in `.fs`) as out-of-scope (Complexity Tracking) — not edited here
- [X] T021 Run `/speckit-analyze` for cross-artifact consistency

## Dependencies & Parallel

- Setup → Foundational (gates) → US1–US5 (parallel; distinct suites/assertions) → Polish.
- [P] readiness authoring (T007/T009/T011/T013/T016) writes distinct files.

## Notes

- No source edits in this backfill; a red test is a finding, not a redesign license. DF-1 deferred (T020).
- The genuine deliverable is the readiness evidence (116 imported without it; tests do not self-write).
- Surface gate (T005/T018) is the direct check of FR-014. The `PictureCacheKey.Fingerprint` + backend replay
  are feature 120 (documented scope boundary).
</content>

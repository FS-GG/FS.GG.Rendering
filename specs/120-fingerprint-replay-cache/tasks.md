# Tasks: Structural Fingerprint & Backend Replay Cache (Feature 120)

**Input**: Design documents from `/specs/120-fingerprint-replay-cache/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/fingerprint-replay-cache.md, quickstart.md

**Tests**: Conformance pass — `hashScene`, `unionArea`, `CacheBoundary`/`CachedSubtree`, `PictureReplayCache`,
the public `FrameMetrics` replay + timing fields, and five suites (incl. two audits) already exist. 120 imported
with **no `readiness/`** — authoring it is the genuine deliverable. No new behaviour is built.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup

- [X] T001 Build clean: `dotnet build FS.GG.Rendering.slnx -c Release` — 0/0
- [X] T002 [P] Confirm `hashScene`/`unionArea` (internal) in `src/Controls/RetainedRender.fsi`; `CacheBoundary`/`CachedSubtree` in `src/Scene/Scene.fsi`; `PictureReplayCache` in `src/SkiaViewer/PictureReplayCache.fsi`; `GlHost.shouldPresent` in `src/SkiaViewer/Host/OpenGl.fsi`; replay + timing fields on public `FrameMetrics`
- [X] T003 [P] Confirm the five suites: `tests/Controls.Tests/Feature120FingerprintTests.fs` + `Audit_Fingerprint.fs`; `tests/Elmish.Tests/Feature120MetricsTests.fs`; `tests/SkiaViewer.Tests/Feature120ReplayCacheTests.fs` + `Audit_ReplayCache.fs`

## Phase 2: Foundational (gates)

- [X] T004 Verify signatures vs `contracts/fingerprint-replay-cache.md` (C1–C6): `hashScene: Scene list -> uint64`; `CacheBoundary { CacheId; Fingerprint; Scene }`; `PictureReplayCache` create/paintBoundary/stats/dispose; `unionArea: Rect list -> int -> int`; `GlHost.shouldPresent`; public replay/timing `FrameMetrics` fields (FR-007/008/013/015/016)
- [X] T005 Confirm zero new public-surface delta: `git status -s tests/surface-baselines/` empty — fingerprint/replay/metrics internal; `CacheBoundary`/`CachedSubtree` already in `FS.GG.UI.Scene.txt`; `FrameMetrics` fields additive (FR-016)

## Phase 3: User Story 1 — structural fingerprint (P1) 🎯 MVP

- [X] T006 [US1] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "120"`; confirm `Feature120FingerprintTests`: identical scenes equal + deterministic (FR-008); every single change incl. opacity/alpha flips (FR-008/010); `%A`-collision long-list yields a different fingerprint (SC-005)
- [X] T007 [US1] Confirm `Audit_Fingerprint`: scaffold reachability; determinism; collision probe; **FsCheck ≥500** distinct-width cases never collide (SC-005)
- [X] T008 [P] [US1] Author `readiness/us1-fingerprint.md` against SC-005

## Phase 4: User Story 2 — backend replay pixel parity (P1)

- [X] T009 [US2] Run `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release --filter "120"`; confirm `Feature120ReplayCacheTests`: matching fingerprint Hit (FR-007); changed re-records, no stale hit, `Entries=1` (FR-010/013); LRU bound never exceeded (FR-013); disabled oracle never records/replays (FR-011); dispose releases all (FR-013); cache-on ≡ cache-off **pixel** readback (SC-003/FR-009); idle-skip `shouldPresent` truth table (US2/FR-004/005/006)
- [X] T010 [US2] Confirm `Audit_ReplayCache` runs OR honest-skips: scaffold always runs (CPU); the two pixel/counter tests run on raster, else `skiptest` with a tier reason (degrade-and-disclose, no fake pass) — record which occurred
- [X] T011 [P] [US2] Author `readiness/us2-replay-pixel-parity.md` against SC-003 (disclose raster scope + the degrade-and-disclose path)

## Phase 5: User Story 3 — replay metrics + timing (P2)

- [X] T012 [US3] Run `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release --filter "120"`; confirm `Feature120MetricsTests`: `PaintDuration`/`ComposeDuration == TimeSpan.Zero` (FR-002/SC-001); stable grid `ReplayHitCount == PictureCacheHitCount`, records = misses, hits/skipped/native-bytes > 0 (FR-014/SC-004); idle zero replay
- [X] T013 [P] [US3] Author `readiness/us3-replay-metrics.md` against SC-001/SC-004

## Phase 6: User Story 4 — damage union (P2)

- [X] T014 [US4] Confirm `Feature120FingerprintTests` union case: two overlapping 100×100 → 17500 (not 20000); disjoint sums; clamps to the frame area; empty → 0 (FR-015/SC-007)
- [X] T015 [P] [US4] Author `readiness/us4-damage-union.md` against SC-007

## Phase 7: Polish

- [X] T016 Full suite `dotnet test FS.GG.Rendering.slnx -c Release` — 0 failures (standing 18 skips + any raster-gated `Audit_ReplayCache` honest-skip)
- [X] T017 Re-confirm zero new public-surface delta (FR-016)
- [X] T018 [P] Verify readiness → SC mapping; each file discloses its scope (deterministic / raster pixel readback / FsCheck collision); the `Audit_ReplayCache` degrade-and-disclose path is noted
- [X] T019 Record DF-1 (redundant access modifiers in `.fs`) AND the `renderHash` alpha-insensitivity finding as out-of-scope, routed to Workstream E (E1 / E3) — not edited here
- [X] T020 Run `/speckit-analyze` for cross-artifact consistency

## Dependencies & Parallel

- Setup → Foundational (gates) → US1–US4 (parallel; distinct suites/assertions) → Polish.
- [P] readiness authoring (T008/T011/T013/T015) writes distinct files.

## Notes

- No source edits in this backfill. The `renderHash` alpha-insensitivity is a recorded finding routed to E3
  (T019), NOT fixed here — keeping all seven backfills uniform. 120's `hashScene` is alpha-sensitive (proven).
- The genuine deliverable is the readiness evidence (120 imported without it; tests do not self-write).
- Surface gate (T005/T017) is the direct check of FR-016. `Audit_ReplayCache` degrades-and-discloses when
  raster is unavailable — an honest skip, not a pass.
</content>

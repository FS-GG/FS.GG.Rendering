# Quickstart — Validating Feature 116 (Picture Cache LRU & Damage Set)

Conformance backfill: code + tests exist. Validation = build green + the 116 suites green + readiness authored
+ zero new public-surface delta.

## 1. Build

```bash
dotnet build FS.GG.Rendering.slnx -c Release
```

## 2. Run the 116 suites (headless, no GL)

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "116"
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj   -c Release --filter "116"
```

Expected green:
- `Feature116DamageTests` — US1: idle 0/0/0; localized small; theme-switch frame-spanning; deduped
  `DirtyRectCount`; deterministic (SC-001; FR-001/002/003/004).
- `Feature116PictureCacheTests` — US2: unchanged boundary Hit (byte-identical to rebuild); content/box/theme
  miss exactly the affected rows; cache-on ≡ cache-off (SC-002/SC-003; FR-005/006/007).
- `Feature116CacheBoundTests` — US3: under cap no eviction; over cap `EntryCount ≤ cap`; deterministic
  eviction; evicted re-miss correct (SC-004; FR-009/010).
- `Feature116OffscreenDiagTests` — US4: drop-shadow/image-filter/path-clip/non-opaque-over-group flagged;
  plain opaque + `RectClip` not flagged; advisory via `step` (SC-005; FR-011).
- `Feature116MetricsTests` — US5: idle 0; stable reuse; localized single miss; bounded under pressure; the six
  metrics re-run byte-identically (SC-006/SC-007; FR-012/013).
- `Audit_PictureCache` — feature-006 audit: present-but-dead (hits provably move), cache-on ≡ cache-off with a
  discriminating divergence, effectiveness (hits ≫ 0, misses → 0).

## 3. Author the readiness evidence

116 imported without `readiness/`. Author `specs/116-picture-cache-lru/readiness/`: `us1-damage-set.md`
(SC-001), `us2-picture-cache.md` (SC-002/SC-003), `us3-bounded-lru.md` (SC-004), `us4-offscreen-detector.md`
(SC-005), `us5-metrics.md` (SC-006/SC-007). Gitignored — transient.

## 4. Confirm zero new public-surface delta (FR-014)

```bash
git status -s tests/surface-baselines/   # MUST be empty
```

Cache internal; damage + cache metrics additive on the already-baselined public `FrameMetrics`.

## 5. Full suite (no regression)

```bash
dotnet test FS.GG.Rendering.slnx -c Release   # 0 failures; standing 18 skips unrelated to 116
```

## Success = the C8 conformance bar

Build green; the six 116 suites green; readiness authored; zero new public-surface delta; `/speckit-analyze`
consistent. No pixel/desktop claim — the picture cache is modeled deterministically (the backend SKPicture
replay is feature 120).
</content>

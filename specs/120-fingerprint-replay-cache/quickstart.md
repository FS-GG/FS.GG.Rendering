# Quickstart — Validating Feature 120 (Structural Fingerprint & Backend Replay Cache)

Conformance backfill: code + tests exist. Validation = build green + the 120 suites green + readiness authored
+ zero new public-surface delta.

## 1. Build

```bash
dotnet build FS.GG.Rendering.slnx -c Release
```

## 2. Run the 120 suites

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj   -c Release --filter "120"   # US1 fingerprint + US4 union (headless, FsCheck 500)
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj       -c Release --filter "120"   # US3 metrics (headless)
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release --filter "120"   # US2 replay + pixel parity (raster)
```

Expected green:
- `Feature120FingerprintTests` — US1/US4: identical scenes equal; every change (incl. alpha) flips the hash;
  the `%A`-collision long-list case differs; `unionArea` union-not-sum + clamp (FR-008/010/015; SC-005/SC-007).
- `Audit_Fingerprint` — US1: scaffold; determinism; collision probe; **FsCheck ≥500** distinct widths never
  collide.
- `Feature120MetricsTests` — US3: timing `TimeSpan.Zero`; replay coincides with picture-cache counters +
  skipped nodes > 0; idle zero replay (FR-002/014; SC-001/SC-004).
- `Feature120ReplayCacheTests` — US2: matching fingerprint Hit; changed re-records; bounded LRU; disabled
  oracle; dispose; cache-on ≡ cache-off **pixel** readback; idle-skip `shouldPresent` (FR-007/009/010/011/013;
  SC-003). *Raster-headless.*
- `Audit_ReplayCache` — feature-006 audit: scaffold; US2 parity; US3 effectiveness. **Degrades-and-discloses**
  (skiptest with a tier reason) when an offscreen `SKSurface` is unavailable.

## 3. Author the readiness evidence

120 imported without `readiness/`. Author `specs/120-fingerprint-replay-cache/readiness/`:
`us1-fingerprint.md` (SC-005), `us2-replay-pixel-parity.md` (SC-003), `us3-replay-metrics.md` (SC-001/SC-004),
`us4-damage-union.md` (SC-007). Gitignored — transient.

## 4. Confirm zero new public-surface delta (FR-016)

```bash
git status -s tests/surface-baselines/   # MUST be empty
```

Fingerprint/replay/metrics internal; `CacheBoundary`/`CachedSubtree` already baselined; `FrameMetrics` fields additive.

## 5. Full suite (no regression)

```bash
dotnet test FS.GG.Rendering.slnx -c Release   # 0 failures; standing 18 skips + any raster-gated Audit_ReplayCache skip
```

## Note — recorded finding (E3)

`SceneEvidence.renderHash` (distinct from `hashScene`) is alpha-insensitive — recorded, routed to Workstream
E3, **not** fixed here. 120's `hashScene` is alpha-sensitive (proven).

## Success = the C10 conformance bar

Build green; the five 120 suites green (raster-gated audit honest-skips when raster absent); readiness authored;
zero new public-surface delta; `/speckit-analyze` consistent.
</content>

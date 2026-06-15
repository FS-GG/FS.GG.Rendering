# Quickstart — Validating Feature 117 (Text-Measure Cache LRU)

Conformance backfill: code + tests exist. Validation = build green + the 117 suites green + readiness authored
+ zero new public-surface delta.

## 1. Build

```bash
dotnet build FS.GG.Rendering.slnx -c Release
```

## 2. Run the 117 suites (headless, no GL)

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "117"
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj   -c Release --filter "117"
```

Expected green:
- `Feature117TextCacheTests` — US1: cold Miss → warm Hit (byte-identical metrics); one differing keyed field
  misses; empty/whitespace caches; fitted-caption distinct sizes; always-miss oracle byte-identical scene +
  layout (SC-001/SC-002/SC-004; FR-001/002/004).
- `Feature117CacheBoundTests` — US1: `Entries.Count ≤ cap` under pressure; deterministic eviction; evicted
  re-miss correct (SC-005; FR-003).
- `Feature117LayoutInvalidatedTests` — US2: idle / style-only zero invalidated + re-measured + text misses;
  geometry `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount`; feature-101 name set unchanged (SC-003/SC-006;
  FR-006/007/008).
- `Feature117MetricsTests` — US3: cold misses → warm hits; style-only zeros; idle zeros; geometry bounded; the
  three new metrics re-run byte-identically (SC-001/002/003/006; FR-005/006/010).
- `Audit_TextCache` — feature-006 audit: adversarial key-completeness, cache-on ≡ cache-off discriminating,
  effectiveness (>95% hit rate vs the disabled oracle).

## 3. Author the readiness evidence

117 imported without `readiness/`. Author `specs/117-text-measure-cache/readiness/`: `us1-text-cache.md`
(SC-001/002/004/005), `us2-style-only-zero-work.md` (SC-003/SC-006), `us3-metrics.md` (SC-001/002/003/006).
Gitignored — transient.

## 4. Confirm zero new public-surface delta (FR-011)

```bash
git status -s tests/surface-baselines/   # MUST be empty
```

Cache internal; the three metric fields additive on the already-baselined public `FrameMetrics`.

## 5. Full suite (no regression)

```bash
dotnet test FS.GG.Rendering.slnx -c Release   # 0 failures; standing 18 skips unrelated to 117
```

## Success = the C9 conformance bar

Build green; the five 117 suites green; readiness authored; zero new public-surface delta; `/speckit-analyze`
consistent. No pixel/desktop claim — proofs are Hit/Miss + byte-identical metrics/scene/layout + work-count regimes.
</content>

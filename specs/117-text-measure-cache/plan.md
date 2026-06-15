# Implementation Plan: Text-Measure Cache (LRU) (Feature 117)

**Branch**: `117-text-measure-cache` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/117-text-measure-cache/spec.md`

## Summary

Feature 117 adds a bounded cross-frame **text-measure cache**: `measureTextCached` looks up a
`(text, family, size, weight)` key in a fixed-cap (256) LRU (mirroring the 116 picture cache) — a resident key
Hits (reused without re-invoking `Scene.measureText`), a cold/changed/evicted key Misses. The always-miss
oracle (`TextCacheEnabled = false`) proves cache-on ≡ cache-off byte-identical scene **and** layout, because
the cached value equals the un-cached measure. 117 also surfaces `LayoutInvalidatedNodeCount` (the pre-pinning
dirty-set size; `≤ RemeasuredNodeCount`, `0` on style-only/idle frames).

**This is a backfill plan** (task **C9**). The implementation, the accreted surface (internal cache + additive
public `FrameMetrics` fields), and five suites (incl. `Audit_TextCache`) already exist; 117 imported with **no
`readiness/`**. `/speckit-tasks`/`/speckit-implement` reduce to a conformance pass.

## Technical Context

**Language/Version**: F# on .NET (`net10.0`), `LangVersion=latest`.
**Primary Dependencies**: Expecto (deterministic; no FsCheck); `Scene.measureText` (pure); the retained render
`step`; feature 097's incremental layout (`layoutDirtySet`/`RemeasuredNodeCount`); the feature-101 geometry-name
set; public `FrameMetrics`; `ControlsElmish.Perf.runScript`. No new dependency.
**Storage**: N/A. `TextMeasureCache` rides the retained record carried frame-to-frame.
**Testing**: Headless across `tests/Controls.Tests/` (`Feature117TextCacheTests` + `Feature117CacheBoundTests`
US1, `Feature117LayoutInvalidatedTests` US2, `Audit_TextCache`) and `tests/Elmish.Tests/Feature117MetricsTests`
(US3). Deterministic; **no GL**.
**Target Platform**: Linux/dev; headless.
**Project Type**: F# UI framework — internal cache in `Controls`, metrics in `Controls.Elmish`.
**Performance Goals**: No wall-clock target. Goals: complete-key Hit/Miss + byte-identical metrics
(SC-001/SC-002/SC-004); bounded deterministic LRU (SC-005); style-only zero-work (SC-003); geometry-frame
bounded `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount` (SC-006).
**Constraints**: Zero new public-surface delta (FR-011) — cache internal; the three metric fields additive on
the already-baselined public `FrameMetrics`. `measureTextCached` pure/total/deterministic; bounded `≤ cap`.
**Scale/Scope**: One bounded LRU + three public metric fields; reuses 097's dirty-set for `LayoutInvalidatedNodeCount`.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1** — alters observable behaviour (text reuse + new metrics). Public delta is
three additive fields on an already-baselined type ⇒ zero new baseline lines.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec → FSI → Tests → Impl | ⚠️ Justified deviation | Order inverted by import; backfill restores the chain. Recorded in Complexity Tracking. |
| II. Visibility in `.fsi` | ⚠️ Pass with noted drift | Cache `internal`; metrics on public `FrameMetrics`. Imported `.fs` redundant modifiers — shared DF-1 (E1), not fixed here. |
| III. Idiomatic simplicity | ✅ Pass | Map-based LRU + pure lookup; no SRTP/reflection. |
| IV. Elmish/MVU boundary | ✅ Pass | `TextMeasureCache` is durable Model state; `measureTextCached` is pure; the host script drives the real seam. |
| V. Test evidence mandatory | ✅ Pass | US1 Hit/Miss + key-completeness + bounded + oracle parity (`Audit_TextCache` adds adversarial key-completeness + effectiveness), US2 style-only zero-work, US3 metrics. Readiness authored in `/speckit-implement`. |
| VI. Observability & safe failure | ✅ Pass | `measureTextCached` total (cold/evicted ⇒ Miss, never stale); bounded `≤ cap`; deterministic. Empty/whitespace text caches without error. |

**Gate result**: PASS (two deviations justified/recorded, inherited).

## Project Structure

### Documentation (this feature)

```text
specs/117-text-measure-cache/
├── plan.md · research.md · data-model.md · quickstart.md · spec.md · tasks.md
├── contracts/text-measure-cache.md
├── checklists/requirements.md
└── readiness/            # AUTHORED in /speckit-implement (117 imported without evidence)
```

### Source Code (repository root)

```text
src/Controls/RetainedRender.fsi / .fs   # TextMeasureKey/TextMeasureCache/measureTextCached/TextMeasureCacheCap; TextCache/TextCacheEnabled; TextMeasureCacheHits/Misses; LayoutInvalidatedNodeCount (internal)
src/Controls.Elmish/ControlsElmish.fsi  # FrameMetrics.TextMeasureCacheHitCount/MissCount + LayoutInvalidatedNodeCount (public, additive)
tests/Controls.Tests/Feature117TextCacheTests.fs · Feature117CacheBoundTests.fs · Feature117LayoutInvalidatedTests.fs · Audit_TextCache.fs
tests/Elmish.Tests/Feature117MetricsTests.fs
```

**Structure Decision**: Single F# solution. 117 adds no project; cache internal, metrics additive. Surface
baselines remain byte-unchanged.

## Complexity Tracking

| Deviation | Why it exists | Why not the simpler/orthodox path |
|---|---|---|
| Contract-first order inverted (code before spec) | Cache + suites imported wholesale (task C9). | Re-deriving from a fresh spec discards working, evidence-backed code. |
| No `readiness/` imported | 117 arrived with suites but no captured evidence. | Authoring readiness from the suites is cheaper/honest. |
| Redundant access modifiers in `.fs` | Inherited from import. | Behavior-neutral Tier-2; shared DF-1 (E1), not bundled. |
| Three public fields added to `FrameMetrics` | Text hit/miss + layout-invalidated must be observable. | Baseline is type-granular ⇒ additive fields are zero new delta. |
| `LayoutInvalidatedNodeCount` shared with 097 | The pre-pinning dirty-set count is computed by 097's incremental layout; 117 surfaces it as a metric. | The metric naturally belongs to the same `step`; 117 scopes its own surface (the text cache) and defers the dirty-set computation to 097 (already backfilled). |
</content>

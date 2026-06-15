# Implementation Plan: Picture Cache (LRU) & Damage Set (Feature 116)

**Branch**: `116-picture-cache-lru` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/116-picture-cache-lru/spec.md`

## Summary

Feature 116 adds a per-frame **damage set** (`RepaintedNodeCount`/`DirtyRectCount`/`DirtyArea`) so frame work
is proportional to change, and a **bounded cross-frame picture cache** — a fixed-cap (256) LRU keyed by a
complete correctness key (box + structural fingerprint) per cacheable `data-grid-row` boundary. An unchanged,
resident boundary is a Hit (reused, byte-identical to a rebuild); a changed/cold/evicted key Misses; the
always-miss oracle proves cache-on ≡ cache-off. An advisory `offscreenEffect` detector flags offscreen-forcing
paint. All modeled deterministically (no live backend).

**This is a backfill plan** (task **C8**). The implementation, the accreted surface (internal cache + additive
public `FrameMetrics`), and six suites (incl. `Audit_PictureCache`) already exist; 116 imported with **no
`readiness/`**. `/speckit-tasks`/`/speckit-implement` reduce to a conformance pass.

## Technical Context

**Language/Version**: F# on .NET (`net10.0`), `LangVersion=latest`.
**Primary Dependencies**: Expecto (deterministic; no FsCheck); the retained render structure + stable
`RetainedId`; the `data-grid-row` boundary; public `FrameMetrics`; `ControlsElmish.Perf.runScript`. The
`PictureCacheKey.Fingerprint` is feature 120's `hashScene`. No new dependency.
**Storage**: N/A. `PictureCache` rides the retained record carried frame-to-frame.
**Testing**: Headless across `tests/Controls.Tests/` (`Feature116DamageTests` US1, `Feature116PictureCacheTests`
US2, `Feature116CacheBoundTests` US3, `Feature116OffscreenDiagTests` US4, `Audit_PictureCache`) and
`tests/Elmish.Tests/Feature116MetricsTests` (US5). Deterministic; **no GL** (render-only model).
**Target Platform**: Linux/dev; headless.
**Project Type**: F# UI framework — internal cache in `Controls`, metrics in `Controls.Elmish`.
**Performance Goals**: No wall-clock target. Goals: damage proportional (SC-001); Hit reuse + cache parity
(SC-002/SC-003); bounded deterministic LRU + no stale hit (SC-004); offscreen detector precision (SC-005);
deterministic bounded metrics (SC-006/SC-007).
**Constraints**: Zero new public-surface delta (FR-014) — cache internal; metrics additive on the
already-baselined public `FrameMetrics`. Bounded LRU (`≤ cap`); monotonic `Clock`, no wall-clock; deterministic.
**Scale/Scope**: One damage walk + one bounded LRU at the `data-grid-row` boundary + the advisory detector + six public metric fields.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1** — alters observable behaviour (reuse + new metrics + diagnostic). Public
delta is additive fields on an already-baselined type ⇒ zero new baseline lines.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec → FSI → Tests → Impl | ⚠️ Justified deviation | Order inverted by import; backfill restores the chain. Recorded in Complexity Tracking. |
| II. Visibility in `.fsi` | ⚠️ Pass with noted drift | Cache `internal`; metrics on public `FrameMetrics`. Imported `.fs` redundant modifiers — shared DF-1 (E1), not fixed here. |
| III. Idiomatic simplicity | ✅ Pass | Map-based LRU + pure walk; no SRTP/reflection. |
| IV. Elmish/MVU boundary | ✅ Pass | `PictureCache` is durable Model state; the walk is a pure projection; the host script drives the real seam. |
| V. Test evidence mandatory | ✅ Pass | US1 damage, US2 cache parity (always-miss counterfactual), US3 bounded LRU, US4 offscreen detector, US5 metrics; `Audit_PictureCache` adds present-but-dead + effectiveness checks. Readiness authored in `/speckit-implement`. |
| VI. Observability & safe failure | ✅ Pass | The cache is total (cold/evicted ⇒ Miss, never stale); `offscreenEffect` is advisory; bounded `≤ cap`; deterministic eviction. |

**Gate result**: PASS (two deviations justified/recorded, inherited).

## Project Structure

### Documentation (this feature)

```text
specs/116-picture-cache-lru/
├── plan.md · research.md · data-model.md · quickstart.md · spec.md · tasks.md
├── contracts/picture-cache.md
├── checklists/requirements.md
└── readiness/            # AUTHORED in /speckit-implement (116 imported without evidence)
```

### Source Code (repository root)

```text
src/Controls/RetainedRender.fsi / .fs   # PictureCacheKey/PictureCache/PictureCacheCap/walkPictures/offscreenEffect; damage + cache WorkReductionRecord fields (internal)
src/Controls.Elmish/ControlsElmish.fsi  # FrameMetrics.RepaintedNodeCount/DirtyRectCount/DirtyArea + PictureCacheHitCount/MissCount/EntryCount (public, additive)
tests/Controls.Tests/Feature116DamageTests.fs · Feature116PictureCacheTests.fs · Feature116CacheBoundTests.fs · Feature116OffscreenDiagTests.fs · Audit_PictureCache.fs
tests/Elmish.Tests/Feature116MetricsTests.fs
```

**Structure Decision**: Single F# solution. 116 adds no project; cache internal, metrics additive. Surface
baselines remain byte-unchanged.

## Complexity Tracking

| Deviation | Why it exists | Why not the simpler/orthodox path |
|---|---|---|
| Contract-first order inverted (code before spec) | Cache + suites imported wholesale (task C8). | Re-deriving from a fresh spec discards working, evidence-backed code. |
| No `readiness/` imported | 116 arrived with suites but no captured evidence. | Authoring readiness from the suites is cheaper/honest. |
| Redundant access modifiers in `.fs` | Inherited from import. | Behavior-neutral Tier-2; shared DF-1 (E1), not bundled. |
| Six public fields added to `FrameMetrics` | Damage + cache outcomes must be observable. | Baseline is type-granular ⇒ additive fields are zero new delta. |
| `PictureCacheKey.Fingerprint` is feature 120's `hashScene` | 120 replaced 116's truncating `%A` digest in place and is the backend replay realization. | The features deliberately co-evolve; 116's spec scopes its own surface and defers the fingerprint/backend to 120. |
</content>

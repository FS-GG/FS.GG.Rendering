# Implementation Plan: Structural Fingerprint & Backend Replay Cache (Feature 120)

**Branch**: `120-fingerprint-replay-cache` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/120-fingerprint-replay-cache/spec.md`

## Summary

Feature 120 is the backend realization of the picture cache (116): a collision-resistant FNV-1a structural
fingerprint `hashScene` (no truncation; replaced 116's `%A` digest), a transparent Scene-IR replay boundary
`CachedSubtree`/`CacheBoundary`, the backend `PictureReplayCache` (bounded LRU of recorded `SKPicture`s keyed
by `CacheId`, validated by `Fingerprint`; matching ⇒ replay/skip the draw walk, changed ⇒ re-record; disabled
= parity oracle; cache-on pixel-identical to direct), replay metrics, the damage union `unionArea` (overlap
once, clamped — corrects 116's sum), and the present/compose timing (US1) + idle-skip `GlHost.shouldPresent`
(US2) diagnostics.

**This is a backfill plan** (task **C10**). The implementation, the accreted surface (internal fingerprint/
replay/metrics + public `CacheBoundary`/`CachedSubtree` + additive public `FrameMetrics` fields, all already
baselined), and five suites (incl. two audits) already exist; 120 imported with **no `readiness/`**.
`/speckit-tasks`/`/speckit-implement` reduce to a conformance pass.

## Technical Context

**Language/Version**: F# on .NET (`net10.0`), `LangVersion=latest`.
**Primary Dependencies**: Expecto + FsCheck (`Audit_Fingerprint` ≥500 cases); the Scene IR; the controls-side
picture cache (116); SkiaSharp raster/GL backend (`SKSurface`/`SKPicture`); public `FrameMetrics`;
`ControlsElmish.Perf.runScript`. No new dependency.
**Storage**: N/A. `PictureReplayCache` holds native `SKPicture`s (disposed on teardown); the controls cache
rides the retained record.
**Testing**: `tests/Controls.Tests/Feature120FingerprintTests.fs` + `Audit_Fingerprint.fs` (US1, headless,
FsCheck 500); `tests/Elmish.Tests/Feature120MetricsTests.fs` (US3, headless); `tests/SkiaViewer.Tests/Feature120ReplayCacheTests.fs`
(US2/US4 + pixel parity, **raster-headless**) + `Audit_ReplayCache.fs` (**degrade-and-disclose** when raster
unavailable). Reaches internals via `InternalsVisibleTo`.
**Target Platform**: Linux/dev. The fingerprint/union/metrics proofs are deterministic-headless; the pixel
parity proofs use a raster `SKSurface` (no GL window).
**Project Type**: F# UI framework — fingerprint/metrics in `Controls`, replay cache in `SkiaViewer`, Scene
boundary in `Scene`.
**Performance Goals**: No wall-clock target. Goals: collision-resistant alpha-sensitive fingerprint (SC-005);
pixel-identical replay (SC-003); replay coincides with the picture cache + skips work (SC-004); union-area
correctness (SC-007); timing excluded from the golden surface (SC-001).
**Constraints**: Zero new public-surface delta (FR-016) — internal except `CacheBoundary`/`CachedSubtree`
(already baselined) + additive `FrameMetrics` fields. `hashScene`/`unionArea` pure/total/deterministic;
bounded LRU; disabled = parity oracle.
**Scale/Scope**: fingerprint + Scene boundary + backend LRU + replay/timing metrics + damage union + idle-skip
decision. Reuses 116's boundary/LRU; replaced 116's digest; corrected 116's `DirtyArea`.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1** — alters observable behaviour (backend replay, fingerprint, union-area
correction, new metrics/diagnostics). Public delta is two already-baselined Scene types + additive `FrameMetrics`
fields ⇒ zero new baseline lines.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec → FSI → Tests → Impl | ⚠️ Justified deviation | Order inverted by import; backfill restores the chain. Recorded in Complexity Tracking. |
| II. Visibility in `.fsi` | ⚠️ Pass with noted drift | `hashScene`/`unionArea`/`PictureReplayCache`/metrics internal; `CacheBoundary`/`CachedSubtree` public (already baselined). Imported `.fs` redundant modifiers — shared DF-1 (E1), not fixed here. **Plus** the `renderHash` alpha-insensitivity finding — recorded, routed to E3, not fixed here. |
| III. Idiomatic simplicity | ✅ Pass | Pure FNV fold + coordinate-compression union + Map-based LRU; no SRTP/reflection. |
| IV. Elmish/MVU boundary | ✅ Pass | The fingerprint/union are pure projections; the replay cache lives at the backend edge (interpreter); the host script + raster surface drive the real seam. |
| V. Test evidence mandatory | ✅ Pass | US1 fingerprint (FsCheck 500 collision probe), US2 replay pixel parity (disabled oracle counterfactual), US3 metrics coincidence + skipped-node work, US4 union-area; two feature-006 audits. Readiness authored in `/speckit-implement`. |
| VI. Observability & safe failure | ✅ Pass | `hashScene`/`unionArea` total; replay cache: changed ⇒ re-record (never stale), bounded, dispose releases pictures; disabled = parity oracle; `Audit_ReplayCache` degrades-and-discloses (skiptest with tier reason) when raster is unavailable — no fake pass. |

**Gate result**: PASS (deviations justified/recorded — DF-1 + the E3 `renderHash` finding + import-before-spec).

## Project Structure

### Documentation (this feature)

```text
specs/120-fingerprint-replay-cache/
├── plan.md · research.md · data-model.md · quickstart.md · spec.md · tasks.md
├── contracts/fingerprint-replay-cache.md
├── checklists/requirements.md
└── readiness/            # AUTHORED in /speckit-implement (120 imported without evidence)
```

### Source Code (repository root)

```text
src/Scene/Scene.fsi / .fs               # CachedSubtree / CacheBoundary { CacheId; Fingerprint; Scene } (public Scene types, already baselined)
src/Controls/RetainedRender.fsi / .fs   # hashScene, unionArea, RenderFragment.Fingerprint, PictureCacheKey.Fingerprint, Replay* metric fields (internal)
src/SkiaViewer/PictureReplayCache.fsi / .fs   # the backend bounded-LRU SKPicture record/replay (module internal)
src/SkiaViewer/Host/OpenGl.fsi          # GlHost.shouldPresent (idle-skip, US2)
src/Controls.Elmish/ControlsElmish.fsi  # FrameMetrics.ReplayHitCount/.../ReplayCacheNativeBytes + PaintDuration/ComposeDuration (public, additive)
tests/Controls.Tests/Feature120FingerprintTests.fs · Audit_Fingerprint.fs
tests/Elmish.Tests/Feature120MetricsTests.fs
tests/SkiaViewer.Tests/Feature120ReplayCacheTests.fs · Audit_ReplayCache.fs
```

**Structure Decision**: Single F# solution. 120 adds no project; the fingerprint/replay/metrics are internal,
the Scene boundary types + `FrameMetrics` fields already baselined. Surface baselines remain byte-unchanged.

## Complexity Tracking

| Deviation | Why it exists | Why not the simpler/orthodox path |
|---|---|---|
| Contract-first order inverted (code before spec) | Fingerprint + replay + suites imported wholesale (task C10). | Re-deriving from a fresh spec discards working, evidence-backed code. |
| No `readiness/` imported | 120 arrived with suites but no captured evidence. | Authoring readiness from the suites is cheaper/honest. |
| Redundant access modifiers in `.fs` | Inherited from import. | Behavior-neutral Tier-2; shared DF-1 (E1), not bundled. |
| `renderHash` alpha-insensitivity | Inherited; `SceneEvidence.renderHash` is a coarse capability hash that ignores opacity. | Extending/​documenting it is a behavior change to a *different* hash than 120's `hashScene`; recorded and routed to **Workstream E3**, not bundled into this doc-only backfill. 120's `hashScene` is alpha-sensitive (proven). |
| 120 co-evolves with 116 (replaced its `%A` digest; corrected its `DirtyArea` to `unionArea`) | The backend realization needed a collision-resistant key and a correct damage area. | The features deliberately co-evolve; 120 scopes its own surface and references 116 as its model. |
</content>

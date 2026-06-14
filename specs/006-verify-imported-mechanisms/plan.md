# Implementation Plan: Verify Imported Rendering & Controls Mechanisms

**Branch**: `006-verify-imported-mechanisms` *(no git branch — repo is not under git; spec dir is the unit)* | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/006-verify-imported-mechanisms/spec.md`

## Summary

Audit the rendering/controls mechanisms imported from `fs-skia-ui` to learn which actually work as advertised. The imported code ships with substantial per-feature tests (Feature091/092/097/113/116/117/120 suites) — but those tests came **with** the code; they have never been scrutinized here as a coherent body of evidence, and they over-index on *correctness* while saying little about *effectiveness* (does the cache ever hit? does "incremental" ever skip?). The audit therefore does three things, in order: **(1) inventory** every advertised mechanism as a falsifiable claim with its source reference and the evidence that currently exists; **(2) verify** each claim against the real code — confirming existing correctness tests have *discriminating power* (red when the mechanism is deliberately bypassed), adding the missing **effectiveness/no-op** measurements via the work-reduction counters the code already exposes, and adding adversarial probes (cache-key completeness, determinism under reordering/collision, present-but-dead detection); **(3) report** a verdict per mechanism (works-as-advertised / benefit-overstated / not-working-or-no-op / unverifiable-and-why) with severity and a recommended action.

The audit builds almost no product code. It drives the real imported modules through their existing seams — the three always-miss oracle flags (`MemoEnabled`/`PictureCacheEnabled`/`TextCacheEnabled`), the `WorkReductionRecord`/`FrameMetrics` counters, `Reconcile.diff`/`apply`, `Layout.evaluate`/`evaluateIncremental`, `Animation.applyAt`, `hashScene`, `PictureReplayCache.create enabled:false`, and the harness `perf` CLI — and records what they prove. Capability-dependent timing claims (frame pacing, frame-rate cap, live present) degrade-and-disclose to the R5 harness tiers exactly as Stage R6 (spec 005) prescribes; they are never reported as passing on a headless runner.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (existing toolchain). Verification is **test code** (Expecto + FsCheck) added to the existing module test projects, plus Markdown audit artifacts. No new product F# module, therefore no new `.fsi`/baseline obligation.

**Primary Dependencies**: Existing only — Expecto + FsCheck (test framework already used across `tests/*.Tests`), the imported modules under test (`Scene`, `Layout`, `Controls` incl. `RetainedRender`/`Reconcile`, `Controls.Elmish`, `SkiaViewer`, `Elmish`), and the R5 harness CLI (`tests/Rendering.Harness`, subcommands `probe`/`offscreen`/`perf`) as the evidence producer for effectiveness/timing claims. **No new NuGet.**

**Storage**: Two durable in-repo artifacts plus ephemeral run output. Durable: `docs/audit/mechanism-inventory.md` (the claims inventory) and `docs/audit/mechanism-audit.md` (the findings report). Ephemeral: harness `run.json`/`metrics.csv`/`summary.md` for effectiveness measurements, captured under `artifacts/harness/` and cited by the report.

**Testing**: Expecto/FsCheck suites added to the existing internals-visible test projects (`Controls.Tests`, `Layout.Tests`, `Scene.Tests`, `Elmish.Tests`, `SkiaViewer.Tests`). Audit-specific tests are name-prefixed `Audit:` so the whole audit subset is runnable via Expecto's `--filter`. Three test *kinds*: **discriminating-power** (mutate/bypass the mechanism → existing-style correctness assertion must now fail), **effectiveness** (assert work-reduction counters move in the advertised direction enabled-vs-disabled by a meaningful margin), **adversarial** (cache-key completeness, determinism under reordering/collision, settled-animation identity). Deterministic kinds run headless in the default local tier; timing/live kinds defer to harness tiers T2/T3.

**Target Platform**: Headless Linux for the deterministic + effectiveness-by-counter audit (no DISPLAY needed — these read counters, not pixels). Pixel-parity and timing claims need GL/X11 (harness `offscreen` T1 / `perf` T3 / `live-x11` T2); on a headless runner they degrade-and-disclose (Principle VI).

**Project Type**: Verification / audit — test evidence + analysis report over existing product code. Not product API.

**Performance Goals**: The deterministic audit subset (discriminating-power + counter-based effectiveness + adversarial) joins the fast inner loop and completes within the default-local-tier budget (the < 10-min gate from spec 005). Timing/live measurements run on the scheduled/manual capability cadence and add zero time to a routine push.

**Constraints**: Constitution v1.0.0. **Principle V (central):** every audit test must have proven discriminating power — a test that passes whether or not the mechanism works proves nothing and is itself an audit finding; no assertion is weakened to green a build; synthetic substitution is disclosed at the use site and in the report. **Principle VI (central):** capability-absent ⇒ skip-with-rationale + the required tier, never green-as-proof; each measurement states what it proved and what it did not. The audit MUST NOT change product runtime behavior — oracle flags and instrumentation stay on test/harness paths only.

**Scale/Scope**: ~14 mechanisms (reconciliation, incremental layout, memo cache, picture cache, text-measure cache, backend SKPicture replay cache, scene fingerprint, per-identity animation clock, declarative animation sampling, animation-tick gating, damage-rect tracking, virtualization, present-mode selection, frame-rate cap). 2 audit docs (inventory + report). 3 contract schemas (claim / verification / verdict). Audit tests spread across 5 existing test projects; **0 new projects, 0 new NuGet, 0 new product `.fsi`**.

### Mechanisms under audit and their verification seam (verified present)

| Mechanism | Source (`.fsi`) | Existing evidence | Audit-added evidence |
|---|---|---|---|
| Keyed reconciliation | `Controls/Reconcile.fsi` `diff`/`apply` | `Controls.Tests/ReconcileTests.fs` (round-trip, keyed reorder, determinism) | Discriminating-power check; adversarial keyed/positional/kind-mismatch inputs |
| Incremental layout | `Layout/Layout.fsi` `evaluate`/`evaluateIncremental` | `Layout.Tests/Feature097IncrementalTests.fs` (equivalence, empty/full dirty) | Effectiveness: `RemeasuredNodeCount`/`Invalidated` ≪ baseline for localized change |
| Memo cache | `RetainedRender.fsi` `MemoEnabled` | `Controls.Tests/Feature113MemoParityTests.fs` (parity, real-hit, staleness) | Effectiveness margin; cache-key completeness probe |
| Picture cache | `RetainedRender.fsi` `PictureCacheEnabled` | `Controls.Tests/Feature116PictureCacheTests.fs` (key, LRU, counts) | Effectiveness: `PictureCacheHits` steady-state; present-but-dead check |
| Text-measure cache | `RetainedRender.fsi` `TextCacheEnabled` | `Controls.Tests/Feature117TextCacheTests.fs` (key, LRU, parity) | Key-completeness adversarial (text+family+size+weight) |
| Backend replay cache | `SkiaViewer/PictureReplayCache.fsi` `create enabled` | (backend; check coverage) | Parity enabled-vs-disabled; effectiveness via `stats` Hits/Records |
| Scene fingerprint | `RetainedRender.fsi` `hashScene` | `Controls.Tests/Feature120FingerprintTests.fs` (determinism, collisions) | Adversarial collision probe over render-affecting field diffs |
| Animation clock | `RetainedRender.fsi` `advance`/`clockActive`/`sampleOnPaint` | `Controls.Tests/Feature092*` | Determinism + clamp (no overshoot); `clockActive` gates redraw |
| Animation sampling | `Scene/Animation.fsi` `applyAt`/`sampleFrames` | `Scene.Tests/AnimationTests.fs` (easing, clamp, endpoints) | Settled-animation byte-identity to static (identity-at-rest) |
| Animation-tick gating | `Elmish/AnimationTick.fsi` `tickSubscription` | `Elmish.Tests/*` | Effectiveness: no ticks requested when no clock active |
| Damage-rect tracking | `RetainedRender.fsi` `DirtyRectCount`/`DirtyArea` | `Elmish.Tests/Feature116*` | Union-area correctness (overlaps once); effectiveness vs full repaint |
| Virtualization | `RetainedRender.fsi` `VirtualMaterialized`/`VirtualTotal` | `Elmish.Tests/Feature114*` | Effectiveness: materialized bounded by viewport, not logical count |
| Present-mode selection | `SkiaViewer/PresentMode.fsi` `ViewerPresentMode` | (viewer) | **Capability tier** (T1/T2) — degrade-disclose headless |
| Frame-rate cap | `SkiaViewer/SkiaViewer.fsi` `FrameRateCap` | (viewer) | **Capability tier** (T3 `perf --mode paced-*`) — degrade-disclose |

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

This feature is **verification + analysis** (test code + audit docs) over existing product code. Code-centric principles bind any F# added (all of it test code); the evidence/observability principles bind fully and are the heart of the work.

| Principle | Assessment |
|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | No new public product surface. Tests exercise the real packed/internal modules through their existing signatures, asserting behavior not internals. **PASS (N/A for new surface)** |
| II. Visibility in `.fsi` | No new public module ⇒ no `.fsi`/baseline obligation. Audit reaches `internal` mechanisms via the existing `InternalsVisibleTo` already configured on the module test projects — no new internals exposure. **PASS (N/A)** |
| III. Idiomatic Simplicity | Reuses existing oracle flags, counters, and the harness CLI rather than building new instrumentation; tests are plain Expecto/FsCheck; no new project, NuGet, or framework. **PASS** |
| IV. Elmish/MVU boundary | No stateful product workflow added. Where the audit drives MVU paths (animation tick, ControlsElmish frame metrics) it uses the existing pure `update`/deterministic-script seams. **PASS (N/A)** |
| V. Test Evidence Mandatory | **Central.** Every audit assertion must demonstrate discriminating power (red when the mechanism is bypassed); a non-discriminating test is recorded as a finding, not a pass. No weakened assertions; synthetic use disclosed in-line and in the report. **PASS** |
| VI. Observability & Safe Failure | **Central.** Effectiveness/timing claims that need GL/X11 degrade-and-disclose to the harness tiers with written rationale and the required tier; each measurement states proved/not-proved. Determinism violations and present-but-dead mechanisms are surfaced loudly, not swallowed. **PASS** |
| Engineering Constraints | `net10.0`; no new NuGet; no governance dependency; package identity untouched (`FS.Skia.UI.*`); no product behavior change — instrumentation stays on test/harness paths. **PASS** |

**Change Classification**: **Tier 2 (internal).** Adds verification and documentation; no public-API change, no new product dependency, no observable product-behavior change. Defects the audit uncovers may each warrant a follow-up change (a fix could be Tier 1 if it alters public behavior); those are out of scope here beyond being recorded as recommendations in the report.

**Result**: No violations. Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/006-verify-imported-mechanisms/
├── plan.md              # This file
├── spec.md
├── research.md          # Phase 0: verification methodology decisions
├── data-model.md        # Phase 1: Mechanism / Claim / Verification / Finding / Report shapes
├── quickstart.md        # Phase 1: how to run the audit suite + regenerate the report
├── contracts/
│   ├── claim-record.md       # schema: one inventoried claim (name, restated claim, source, status)
│   ├── verification-record.md# schema: one executed test/measurement bound to a claim
│   └── verdict-record.md     # schema: per-mechanism verdict (category, severity, evidence, action)
└── checklists/requirements.md
```

### Source Code (repository root)

```text
docs/audit/
├── mechanism-inventory.md        # P1 deliverable: every mechanism as a falsifiable claim + current evidence
└── mechanism-audit.md            # P2 deliverable: verdict per mechanism + severity + recommendation + repro

tests/Controls.Tests/             # extend (already has InternalsVisibleTo to Controls)
├── Audit_Reconcile.fs            #   discriminating-power + adversarial keyed/positional/kind-mismatch
├── Audit_MemoCache.fs            #   effectiveness margin + cache-key completeness
├── Audit_PictureCache.fs         #   effectiveness steady-state + present-but-dead
├── Audit_TextCache.fs            #   key-completeness adversarial
└── Audit_Fingerprint.fs          #   collision probe over render-affecting field diffs
tests/Layout.Tests/
└── Audit_IncrementalLayout.fs    #   effectiveness: Remeasured/Invalidated ≪ baseline for localized change
tests/Scene.Tests/
└── Audit_AnimationSampling.fs    #   settled-animation byte-identity to static
tests/Elmish.Tests/
├── Audit_AnimationTickGating.fs  #   no ticks when no clock active
├── Audit_DamageTracking.fs       #   union-area overlaps-once + effectiveness vs full repaint
└── Audit_Virtualization.fs       #   materialized bounded by viewport
tests/SkiaViewer.Tests/
└── Audit_ReplayCache.fs          #   parity enabled-vs-disabled + stats effectiveness (degrade-disclose if no GL)

# Capability-tier claims (present-mode, frame-rate cap) are exercised through the EXISTING
# harness perf/live subcommands on the scheduled cadence — no new harness code unless a gap is found.
```

**Structure Decision**: Audit tests live **inside the existing module test projects**, not a new project. Those projects already carry `InternalsVisibleTo` to reach the `internal` mechanisms and already reference the right modules; a dedicated audit project would re-plumb internals exposure for no benefit (Principle III, "reuse existing seams"). Cohesion as an *audit* comes from two places instead: the `Audit_*.fs` filename + `Audit:` test-name prefix make the whole subset runnable in one filtered pass, and the **report** (`docs/audit/mechanism-audit.md`) is the single unifying deliverable that cites each test and harness run as evidence. Capability-dependent timing claims reuse the R5 harness `perf`/`live-x11` subcommands (the evidence engine from spec 005) rather than adding measurement code — keeping the headless inner loop deterministic and pushing GL/timing work onto the disclosed capability cadence.

## Complexity Tracking

No constitution violations — section intentionally empty.

# Implementation Plan: God-Module Splits (Code-Health Refactoring Phase 5)

**Branch**: `182-god-module-splits` | **Date**: 2026-06-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/182-god-module-splits/spec.md`

## Summary

Phase 5 of the whole-repo code-health refactoring brings the six largest modules/functions under
control by **splitting them along existing seams with zero observable behavior change**. Phases 0–4
(features 177–181) removed the highest-volume duplication; what remains is structural — six shipped
`FS.GG.UI.*` units that are hard to read, navigate, and review:

| # | Story | Target | File | Lines @HEAD | Seam |
|---|-------|--------|------|-------------|------|
| 1 | US1/P1 | `SkiaViewer.fs` (`module Viewer`) | `src/SkiaViewer/SkiaViewer.fs` | 4,063 | type header → `Viewer.Types`; split `Viewer` by concern (responsiveness, window-behavior/validation, native run-loops, evidence/screenshot, app/interactive runners); unify `runPresentedPersistentWindow` ≈ `runPersistentWindow` |
| 2 | US2/P2 | `Control.fs` (`ControlInternals`) | `src/Controls/Control.fs` | 3,570 | split into `ChartGeometry`, `WidgetGeometry`, `SceneHash`/`Fingerprint`, `LayoutEval`, `NodeAssembly`; hoist the ×17 `match pts with [] -> emptyState` chart preamble into a `withPoints` combinator + shared bar-layout helper |
| 3 | US3/P3 | `Scene.fs` | `src/Scene/Scene.fs` | 2,077 | move `VisualInspection`/`RetainedInspection`/`LayoutEvidence`/`SceneEvidence` into own files; separate the ~767-line type block; finish the `cleanToken`/`duplicateIds`/`finding` dedup; isolate the `realTextMeasurer` module-level mutable |
| 4 | US4/P4 | `Testing.fs` | `src/Testing/Testing.fs` | 4,629 | split into per-domain files (Visual, RetainedInspection, Evidence, Compositor, Feature-readiness) |
| 5 | US5/P5 | `RetainedRender.step` | `src/Controls/RetainedRender.fs` | 2,087 (fn ~600) | extract a `StepMetrics` record + named passes; unify with `init`'s duplicated build/paint scaffolding |
| 6 | US6/P6 | `runInteractiveAppWithLauncher` | `src/Controls.Elmish/ControlsElmish.fs` | 2,227 (fn ~500) | promote ~20 `ref`-cell ad-hoc frame state to a `FrameLoopState` record + module functions |

This is a **Tier 2 internal change**. Unlike Phase 4 (181, which targeted the internal
`tools/Rendering.Harness/` — no `.fsi`, no shipped surface), **every target here is a shipped
`FS.GG.UI.*` package with a companion `.fsi`**. The split MUST keep each package's public `.fsi`
surface and its `readiness/surface-baselines/*.txt` snapshot **byte-identical**: consumers, samples,
the template, and downstream generated products see no change. This is reorganization behind a stable
contract, not a contract change.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This feature carries **no defect/root-cause hypothesis** to confirm against a running app: it is a
> pure structural refactor that must not change any observed output. The early-live-smoke clause of
> the plan template is therefore resolved as **N/A**. `/speckit-tasks` MUST instead schedule
> **baseline capture** as the first Foundational task: snapshot the 12 surface baselines, regenerate
> every readiness/evidence artifact for the touched subsystems, run the full `*.Tests.fsproj` sweep
> *before any edit*, and gate every story on (a) a byte-identical surface-baseline diff, (b) a
> byte-for-byte diff of regenerated artifacts, and (c) the full sweep showing the **same** red/green
> set as baseline. Never green a build by weakening an assertion; if a split forces an output or
> surface change, that split is out of scope — leave the unit explicit (FR-009) and record why.

> **Carried lesson from Phases 3–4 (180/181).** An abstraction is only worth it when it removes
> genuine duplication. Here the goal is explicitly **module/function size and legibility, not line
> reduction** (SC-005, Out of Scope): net lines may rise slightly from new file/`.fsi` headers, and
> that is acceptable. The Phase-4 measured-collapse discipline still applies to the three *dedup*
> sub-goals (viewer window-lifecycle unification, the ×17 chart preamble, the Scene inspection dedup):
> each is unified only if output stays byte-identical; otherwise it is retained explicitly (FR-009).

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`Directory.Build.props`: `TargetFramework=net10.0`,
`FSharpLanguageVersion=latest`). F# compiles in file order — module-split file ordering is a
first-class constraint here (see Edge Cases / FR below).

**Primary Dependencies**: SkiaSharp over OpenGL + Silk.NET (viewer/host/render path). Yoga (layout).
No new dependencies introduced; no new project; no new inter-project reference (FR-010).

**Storage**: No persisted runtime state. The durable artifacts that MUST stay byte-stable are:
(1) the 12 public-surface baselines in `readiness/surface-baselines/*.txt`; (2) regenerated
readiness/evidence artifacts (Markdown + JSON) under `specs/###-*/readiness/**` and the harness's
emitted reports; (3) viewer screenshots/observations, scene hashes/fingerprints, and damage regions.

**Testing**: `dotnet test FS.GG.Rendering.slnx -c Release` driven under `DISPLAY=:1` (GL needs a
display). Comprehensive baseline capture via `dotnet fsi scripts/baseline-tests.fsx --out <path>`
(globs every `*.Tests.fsproj`, including release-only / sample lanes outside the solution). The
surface oracle is `dotnet fsi scripts/refresh-surface-baselines.fsx` → `git diff` of
`readiness/surface-baselines/` (must be empty). Per-package FSI surface tests
(`tests/*/PublicSurfaceTests.fs`, `SurfaceAreaTests.fs`) are the live gate.

**Target Platform**: Linux desktop (SkiaSharp/GL under `DISPLAY=:1`); CI runs Debug build + tests.

**Project Type**: F# UI framework / library set built from `FS.GG.Rendering.slnx`. The units of change
are six shipped `src/` projects: `SkiaViewer`, `Controls` (×2 — `Control.fs`, `RetainedRender.fs`),
`Scene`, `Testing`, `Controls.Elmish`. Each split stays **within its existing project** (FR-010).

**Performance Goals**: N/A as a target — but `RetainedRender.step` (US5) and the Elmish frame loop
(US6) are measured hot paths. Restructuring MUST NOT regress their timing/render-lag traces, and
mutation MAY be retained where it is the simpler/faster code with a one-line disclosure comment
(Constitution III, FR-007). The named-pass refactor threads a typed accumulator; it does not add
allocation on the per-frame path.

**Constraints**:
- **Byte-stability is binding.** Public `.fsi` surface + 12 surface baselines + all regenerated
  rendered/evidence/readiness artifacts + viewer observations + scene hashes/fingerprints + damage
  regions MUST be byte-identical to a baseline captured immediately before the change. When
  size/legibility and byte-stable output conflict, **byte-stable output wins** (FR-002/003, SC-001/002).
- **No public surface change** (FR-002, Tier 2). No public symbol may change name, namespace, module
  path, or signature. A split that would relocate a public symbol's module path, or accidentally
  promote a private helper (or hide a public one), is a surface change and is forbidden.
- **No `private`/`internal`/`public` modifiers on top-level `.fs` bindings** (Constitution II).
  Visibility lives in the `.fsi`. Newly-extracted files use `module internal` (FS0078) or a companion
  internal `.fsi` as needed; the **union** of public surface across the split files MUST equal the
  pre-split `.fsi` exactly.
- **File order / no new cycle** (FR-010, Edge Cases). The split's compile order MUST preserve every
  existing reference with no new back-edge or inter-project edge. A seam that would require a back-edge
  or reorder a public symbol's definition site is out of scope for that family and stays as-is (FR-009).
- **Size targets are goals, not hard rules** (SC-005): no touched module > ~1,500 lines, no touched
  function > ~150 lines — except cohesive units explicitly retained per FR-009 with recorded rationale.

**Scale/Scope** (verified at HEAD by the Phase-0 Explore pass — see [research.md](./research.md)):
- `SkiaViewer.fs` 4,063 lines; `module Viewer` @777; `runPresentedPersistentWindow` @2114 ≈
  `runPersistentWindow` @2437 (both `private`, the unify target); type headers + `RenderLagTrace`
  precede `Viewer`.
- `Control.fs` 3,570 lines; `module internal ControlInternals` @124–3134 (~3,010 lines); 170 `Geom`
  bindings; **exactly 17** `match pts with` chart preambles (the `withPoints` target).
- `Scene.fs` 2,077 lines; large `VisualInspection*`/`RetainedInspection*` type block from ~432; the
  `cleanToken`/`duplicateIds`/`finding` dedup and `realTextMeasurer` mutable confirmed present.
- `Testing.fs` 4,629 lines; **~30 top-level modules** already grouped by domain (Visual*,
  RetainedInspection*, Evidence*, Compositor*, Feature-readiness) — clean per-domain seams.
- `RetainedRender.fs` 2,087 lines; `init` @1254, `step` @1424 (~600 lines, ~30 `let mutable`
  accumulators, several shared by `init`).
- `ControlsElmish.fs` 2,227 lines; `runInteractiveAppWithLauncher` @1186 (~500 lines, ~20 `ref` cells
  of frame state).

## Constitution Check

*GATE: evaluated before Phase 0 research; re-checked after Phase 1 design. Result: **PASS**.*

| Principle | Assessment |
|-----------|------------|
| **I. Spec → FSI → Semantic Tests → Implementation** | No new public surface is designed — the existing `.fsi` of each package IS the frozen contract, and the existing FSI/surface tests are the oracle. Newly-extracted concern modules contribute the *same* union of public surface. The "FSI is the honest audience" order is satisfied trivially because the audience-facing shape does not change. **Pass.** |
| **II. Visibility in `.fsi`, not `.fs`** | The crux of this feature. Each split preserves the per-package `.fsi` as-is; new files declare visibility via `module internal` or a new internal `.fsi`, never via access modifiers on `.fs` bindings. The surface-drift check (`refresh-surface-baselines.fsx` + `SurfaceAreaTests`) is the binding gate (FR-002, SC-001). **Pass.** |
| **III. Idiomatic Simplicity** | The whole feature *is* this principle applied: smaller, named, navigable units. `StepMetrics`/`FrameLoopState` are plain records; `withPoints` is an ordinary combinator; concern modules are ordinary modules. **Mutation is retained where it is the simpler/faster code** (US5 hot render path, US6 per-frame loop) with one-line disclosure comments — explicitly endorsed by Principle III, not a violation. No SRTP/reflection/type-providers/non-trivial CEs introduced. **Pass.** |
| **IV. Elmish/MVU boundary** | No new stateful/I-O workflow. US6 restructures the *interpreter edge* of an existing Elmish loop (the `ref` cells are interpreter-local frame state, not `Model`); promoting them to a typed `FrameLoopState` record keeps `update` pure and I/O at the edge — it strengthens, not crosses, the boundary. No `Model`/`Msg`/`Cmd` contract changes. **Pass.** |
| **V. Test Evidence** | Behavior is preserved; the existing fail-before/pass-after suites plus the byte-diff of surface baselines + regenerated artifacts are the evidence. No assertion weakened, no test deleted to green a build, no synthetic evidence introduced. Pre-existing baseline reds are recorded as not-regression, not silenced. **Pass.** |
| **VI. Observability & Safe Failure** | All diagnostic/evidence/screenshot emission paths are preserved verbatim (byte-stable, FR-003). Relocating a code path must not change emission ordering or constants — the byte-diff sweep is the catch-all. **Pass.** |

**Change Classification**: **Tier 2 (internal change)** for all six stories. The refactor reorganizes
*internal* structure only; it introduces no public API surface change, no new dependency, no new
project/reference (FR-010), and no observable-behavior change covered by a product spec. Per the
Constitution, Tier 2 requires spec + tests and leaves shipped `.fsi`/baselines **untouched** — which
is exactly FR-002. **A Tier 1 outcome (any shipped `.fsi` or surface baseline requiring an edit) means
the offending split overshot and must be reverted/re-scoped (FR-009), not baselined forward.**

**No constitution violations → Complexity Tracking table intentionally omitted.**

## Project Structure

### Documentation (this feature)

```text
specs/182-god-module-splits/
├── spec.md              # Feature specification (input)
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — per-target seam map, file-order analysis, byte-oracle decision
├── data-model.md        # Phase 1 — StepMetrics, FrameLoopState, concern-module catalog, surface-union
├── quickstart.md        # Phase 1 — baseline-capture + surface-diff + byte-diff validation guide
├── contracts/           # Phase 1 — per-story split contracts (the binding invariants)
│   ├── surface-invariance.md   # the union-of-public-surface == frozen .fsi rule + how it's checked
│   ├── split-viewer.md         # US1 — concern files, Viewer.Types, run-loop unification (FR-004)
│   ├── split-control.md        # US2 — geometry modules, withPoints combinator (FR-005)
│   ├── split-scene.md          # US3 — inspection files, type block, dedup (FR-006), mutable isolation
│   ├── split-testing.md        # US4 — per-domain files
│   ├── refactor-retainedrender.md  # US5 — StepMetrics + named passes, init/step scaffold unify (FR-007)
│   └── refactor-framestate.md      # US6 — FrameLoopState record + module functions (FR-007)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/SkiaViewer/                          # US1 — FS.GG.UI.SkiaViewer (.fsi FROZEN)
├── SkiaViewer.fsproj                    # include-order updated for new concern files (before SkiaViewer.fs)
├── SkiaViewer.fsi                        # UNCHANGED (frozen public surface)
├── Viewer.Types.fs (+ internal .fsi?)   # NEW — type header / RequireQualifiedAccess types carved out
├── ViewerResponsiveness.fs              # NEW — responsiveness summarization
├── ViewerWindowBehavior.fs              # NEW — window-behavior / validation
├── ViewerRunLoops.fs                    # NEW — native run-loops + UNIFIED persistent-window scaffold (FR-004)
├── ViewerEvidence.fs                    # NEW — evidence / screenshot
└── SkiaViewer.fs                         # SHRUNK — app/interactive runners; same public `module Viewer` union

src/Controls/                            # US2 + US5 — FS.GG.UI.Controls (.fsi FROZEN)
├── Controls.fsproj                      # include-order updated for new geometry files (before Control.fs)
├── Control.fsi / RetainedRender.fsi     # UNCHANGED (frozen public surface)
├── ChartGeometry.fs / WidgetGeometry.fs # NEW (US2) — the *Geom families + withPoints combinator + bar-layout helper
├── SceneFingerprint.fs / LayoutEval.fs  # NEW (US2) — SceneHash/Fingerprint, LayoutEval
├── NodeAssembly.fs                      # NEW (US2) — node assembly
├── Control.fs                            # SHRUNK — ControlInternals reduced to assembly glue + public Control module
├── StepMetrics.fs (or in RetainedRender)# US5 — StepMetrics record + named passes (internal)
└── RetainedRender.fs                     # SHRUNK — step restructured; init/step shared scaffold unified

src/Scene/                               # US3 — FS.GG.UI.Scene (.fsi FROZEN)
├── Scene.fsproj                         # include-order updated for new inspection/type files (before Scene.fs)
├── Scene.fsi                             # UNCHANGED (frozen public surface)
├── SceneTypes.fs                        # NEW — the ~767-line type block carved out
├── VisualInspection.fs / RetainedInspection.fs   # NEW — inspection sub-modules; shared cleanToken/duplicateIds/finding (FR-006)
├── LayoutEvidence.fs / SceneEvidence.fs # NEW — evidence sub-modules
└── Scene.fs                              # SHRUNK — root primitives; isolated realTextMeasurer side-channel

src/Testing/                             # US4 — FS.GG.UI.Testing (.fsi FROZEN)
├── Testing.fsproj                       # include-order updated for per-domain files (before Testing.fs)
├── Testing.fsi                           # UNCHANGED (frozen public surface)
├── TestingVisual.fs / TestingRetainedInspection.fs / TestingEvidence.fs
├── TestingCompositor.fs / TestingFeatureReadiness.fs   # NEW — per-domain carve-outs
└── Testing.fs                            # SHRUNK — residual glue / re-exports preserving the public union

src/Controls.Elmish/                     # US6 — FS.GG.UI.Controls.Elmish (.fsi FROZEN)
├── Controls.Elmish.fsproj               # include-order updated if FrameLoopState lands in its own file
├── ControlsElmish.fsi                    # UNCHANGED (frozen public surface)
├── FrameLoopState.fs                    # NEW (or internal block) — FrameLoopState record + module functions
└── ControlsElmish.fs                     # SHRUNK — runInteractiveAppWithLauncher over typed FrameLoopState

specs/182-god-module-splits/readiness/
├── baseline/                            # pre-edit: 12 surface baselines + regenerated artifacts + full sweep log
└── post-change/                         # post-edit: same, diffed byte-for-byte (the acceptance gate)
```

> The exact new-file names/counts above are the **planned** seams; the binding requirement is the
> public-surface union and byte-stability, not a specific file inventory. A seam that cannot be split
> without a back-edge or surface change is retained per FR-009 and recorded in that story's contract.

**Structure Decision**: Single-solution F# multi-project layout (`FS.GG.Rendering.slnx`). Every new
file is added **inside its target's existing `src/` project**, inserted into the `.fsproj`
`<Compile Include>` order *before* the residual god-file so the residual file can reference the
extracted modules with no back-edge (F# file-order rule). No new project, no new package dependency,
no new inter-project reference (FR-010). Each package's companion `.fsi` is **frozen**; newly-public
sub-modules (if any) declare their internal surface via `module internal` or a new *internal* `.fsi`
that does not alter the package's external surface union.

## Sequencing & Independence

The six stories map to spec priorities (largest/highest-navigation-cost first) and are **each
independently shippable** — none depends on another (Spec "User Scenarios" note). Each ends green on
`dotnet build` + `dotnet test` and holds its package's surface + byte-stability on its own (SC-004).
They share **one** baseline captured once up front (mirrors features 179/180/181):

1. **Setup** — create `specs/182-…/readiness/`; capture the pre-edit baseline: snapshot all 12
   `readiness/surface-baselines/*.txt`; regenerate every readiness/evidence artifact for the six
   touched subsystems + capture viewer observations / scene hashes / damage regions; run the full
   `*.Tests.fsproj` sweep into `baseline/`.
2. **Foundational (GATE)** — record the allowed pre-existing non-green set (known `Package.Tests` /
   `ControlsGallery` stale-feed reds, per feature 180/181 evidence) as baseline-not-regression;
   resolve the early-live-smoke clause as N/A; lock the byte-stability + surface-invariance evidence
   contract ([contracts/surface-invariance.md](./contracts/surface-invariance.md)). No code edits.
3. **US1 / P1 — SkiaViewer split** (Tier 2): carve `Viewer.Types` + concern files; unify
   `runPresentedPersistentWindow`/`runPersistentWindow` (FR-004) or retain explicit (FR-009). Build +
   full test + surface-diff (`SkiaViewer.txt` unchanged) + viewer evidence/screenshot byte-diff.
   **MVP — independently shippable here** (SC-001/003/004).
4. **US2 / P2 — Control split** (Tier 2): carve `ChartGeometry`/`WidgetGeometry`/`SceneFingerprint`/
   `LayoutEval`/`NodeAssembly`; hoist the ×17 preamble into `withPoints` + bar-layout helper (FR-005)
   or retain explicit (FR-009). Build + full test + surface-diff (`Controls.txt`) + scene-hash/
   fingerprint byte-diff.
5. **US3 / P3 — Scene split** (Tier 2): move inspection sub-modules + type block to own files; finish
   the `cleanToken`/`duplicateIds`/`finding` dedup (FR-006); isolate `realTextMeasurer`. Build + full
   test + surface-diff (`Scene.txt`) + inspection-record byte-diff.
6. **US4 / P4 — Testing split** (Tier 2): carve per-domain files. Build + full test + surface-diff
   (`Testing.txt`) + readiness/evidence markdown+JSON byte-diff.
7. **US5 / P5 — RetainedRender.step** (Tier 2): extract `StepMetrics` + named passes; unify init/step
   scaffold (FR-007). Build + retained-render + damage-locality suites + rendered/metrics/damage
   byte-diff. Mutation retained on the hot path with disclosure comments (Constitution III).
8. **US6 / P6 — FrameLoopState** (Tier 2): promote the ~20 `ref` cells to `FrameLoopState` +
   module functions (FR-007). Build + frame-loop/render-lag trace suite + surface-diff
   (`Controls.Elmish.txt`) + trace byte-diff.
9. **Polish** — full `dotnet build` + `dotnet test`; capture `post-change/`; verify SC-001…SC-007;
   record every FR-009 retention (un-split seam / un-unified dedup) with rationale; confirm no shipped
   `.fsi` or surface baseline changed and the dependency graph is unchanged (FR-010).

Stories may land in any order; US1 and US2 are sequenced first for payoff. US2 (Control) and US5
(RetainedRender) both touch `src/Controls/` but different files — they are independent but should be
serialized to keep one clean per-story `Controls.txt` surface diff.

## Done When

- [ ] Plan workflow executed; design artifacts generated (research, data-model, contracts, quickstart).
- [ ] Each of the six targets has a per-story contract pinning its surface-union + byte oracle.
- [ ] CLAUDE.md SpecKit marker points at this plan.

## Complexity Tracking

> No Constitution Check violations — table omitted.

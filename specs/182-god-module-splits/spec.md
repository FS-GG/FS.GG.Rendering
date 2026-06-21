# Feature Specification: God-Module Splits (Code-Health Refactoring Phase 5)

**Feature Branch**: `182-god-module-splits`

**Created**: 2026-06-21

**Status**: Draft

**Input**: User description: "next item in plan." — resolved to **Phase 5** of the code-health
refactoring plan (`docs/reports/2026-06-21-05-19-code-health-refactoring-analysis-and-plan.md`):
bring the largest modules and functions under control by splitting them along existing seams, with
no observable behavior change. Scope confirmed by the maintainer as **all six Phase-5 splits in one
feature** (rather than one split per feature).

## Context

The codebase is clean of rot but **structurally heavy**: debt is concentrated in a handful of
god-modules and god-functions. Phases 0–4 (features 177–181) removed the highest-volume duplication
(shared helpers, placement decisions, `ReadinessStatus`, and the per-feature data-table refactor).
What remains is the genuinely structural work: several `src/` modules exceed ~2,000 lines (two
exceed ~4,000) and a few functions exceed ~300–600 lines, making them hard to read, navigate, and
review.

Unlike Phase 4 — which targeted the internal `tools/Rendering.Harness/` (no `.fsi`, no shipped
surface) — **every module in this phase is a shipped `FS.GG.UI.*` package with a companion `.fsi`**.
The split is therefore a **Tier 2 internal change** that MUST keep each package's public `.fsi`
surface and its surface-area baseline **byte-identical**: consumers, samples, the template, and
downstream generated products must see no change. The work is reorganization behind a stable
contract, not a contract change.

The six god-module / god-function targets (current line counts at HEAD):

| # | Target | File | Lines | Seam to split along |
|---|---|---|---|---|
| 1 | `SkiaViewer.fs` (`module Viewer`) | `src/SkiaViewer/SkiaViewer.fs` | 4,063 | type header → `Viewer.Types`; split `Viewer` by concern (responsiveness, window-behavior/validation, native run-loops, evidence/screenshot, app/interactive runners); unify `runPresentedPersistentWindow`/`runPersistentWindow` |
| 2 | `Control.fs` (`ControlInternals`) | `src/Controls/Control.fs` | 3,570 | split into `ChartGeometry` (the `*Geom` family), `WidgetGeometry`, `SceneHash`/`Fingerprint`, `LayoutEval`, `NodeAssembly`; hoist the `match pts with \| [] -> emptyState` chart preamble (×17) into a `withPoints` combinator + shared bar-layout helper |
| 3 | `Scene.fs` | `src/Scene/Scene.fs` | 2,077 | move `VisualInspection`, `RetainedInspection`, `LayoutEvidence`, `SceneEvidence` into their own files; separate the ~767-line type block; finish the started `cleanToken`/`duplicateIds`/`finding` dedup; isolate the `realTextMeasurer` module-level mutable |
| 4 | `Testing.fs` | `src/Testing/Testing.fs` | 4,629 | split into per-domain files (Visual, RetainedInspection, Evidence, Compositor, Feature-readiness) |
| 5 | `RetainedRender.step` | `src/Controls/RetainedRender.fs` | 2,087 (fn ~600) | extract a `StepMetrics` record + named passes; unify with `init`'s duplicated build/paint scaffolding |
| 6 | `runInteractiveAppWithLauncher` | `src/Controls.Elmish/ControlsElmish.fs` | 2,227 (fn ~500) | promote the ~20 `ref`-cell ad-hoc frame state to a `FrameLoopState` record + module functions |

**Carried lessons from earlier phases.** (a) Phase 3 (180) and Phase 4 (181) confirmed that an
abstraction is only worth it when it removes genuine duplication; here the goal is **module/function
size and legibility, not line reduction** — net lines may rise slightly (new file headers, possibly
new internal `.fsi` for newly-public sub-modules), and that is acceptable as long as no public
surface changes. (b) Byte-stability against a captured baseline is the acceptance gate, exactly as in
Phases 3–4.

## User Scenarios & Testing *(mandatory)*

> Each story is one independently-shippable split: it builds, passes the full suite, and keeps its
> package's public surface and rendered/evidence output byte-stable on its own. They may land in any
> order. Priorities reflect a sensible sequence (largest/highest-navigation-cost first), not a hard
> dependency chain — none of these splits depends on another.

### User Story 1 - Split the SkiaViewer god-module (Priority: P1)

A contributor working on the viewer host wants `module Viewer` broken into concern-scoped files
(types, responsiveness summarization, window-behavior/validation, native run-loops,
evidence/screenshot, app/interactive runners) so a viewer change touches a focused file instead of a
4,063-line monolith, and the two near-duplicate window-lifecycle paths
(`runPresentedPersistentWindow`/`runPersistentWindow`) are unified behind one scaffold.

**Why this priority**: `SkiaViewer.fs` is the second-largest `src` file and the host entry point that
most viewer/sample work passes through; it also carries the most duplicated scaffolding
(`runPresentedPersistentWindow` ≈ `runPersistentWindow`). Highest navigation cost, highest payoff.

**Independent Test**: Build `FS.GG.UI.SkiaViewer`, run its test project and the viewer-driven
smoke/evidence lanes, and confirm the `SkiaViewer.fsi` surface baseline and all generated
viewer evidence/screenshots are unchanged versus baseline.

**Acceptance Scenarios**:

1. **Given** the split viewer modules, **When** the `FS.GG.UI.SkiaViewer` package is built, **Then**
   its public `.fsi` surface and surface-area baseline are byte-identical to the pre-split baseline.
2. **Given** the unified window-lifecycle scaffold, **When** the persistent-window run paths execute,
   **Then** window observations, diagnostics, and evidence output match the pre-split harness exactly.
3. **Given** the concern-scoped files, **When** the suite runs, **Then** no single viewer file
   exceeds the size target and no public symbol moved namespaces.

---

### User Story 2 - Split the Control god-module (Priority: P2)

A contributor extending controls wants `ControlInternals` (~2,990 lines inside a 3,570-line file)
divided into `ChartGeometry`, `WidgetGeometry`, `SceneHash`/`Fingerprint`, `LayoutEval`, and
`NodeAssembly`, with the repeated chart preamble (`match pts with | [] -> emptyState …`, ×17) hoisted
into a `withPoints` combinator and a shared bar-layout helper, so chart/widget geometry lives in named
units instead of one wall of code.

**Why this priority**: `Control.fs` is the largest module in the busiest project (`src/Controls/`);
the 17× chart preamble is concrete, mechanical duplication with an obvious combinator seam.

**Independent Test**: Build `FS.GG.UI.Controls`, run `Controls.Tests`, and confirm `Control.fsi`
surface baseline plus all control scene-hash / fingerprint / inspection outputs are unchanged.

**Acceptance Scenarios**:

1. **Given** the split control internals, **When** `FS.GG.UI.Controls` is built, **Then** its public
   `.fsi` surface and surface-area baseline are byte-identical to baseline.
2. **Given** the `withPoints` combinator, **When** any chart control is laid out and hashed, **Then**
   the produced scene, scene-hash, and fingerprint are byte-identical to baseline.

---

### User Story 3 - Split the Scene god-module (Priority: P3)

A contributor working on scene primitives wants `VisualInspection`, `RetainedInspection`,
`LayoutEvidence`, and `SceneEvidence` moved into their own files, the ~767-line type block separated,
the started-but-unfinished `cleanToken`/`duplicateIds`/`finding` dedup completed, and the
`realTextMeasurer` module-level mutable isolated — so the dependency-free scene root is legible and its
one mutable side-channel is contained.

**Why this priority**: `Scene` is the dependency-free root referenced by 17 projects; keeping it
legible benefits the whole tree, and the half-finished dedup is a known loose end. Lower risk than the
viewer/controls splits because the seams (inspection sub-modules) are already conceptually distinct.

**Independent Test**: Build `FS.GG.UI.Scene`, run `Scene` tests and the codec round-trip suite, and
confirm `Scene.fsi` surface baseline and all visual/retained inspection records are unchanged.

**Acceptance Scenarios**:

1. **Given** the split scene files, **When** `FS.GG.UI.Scene` is built, **Then** its public `.fsi`
   surface and surface-area baseline are byte-identical to baseline.
2. **Given** the finished `cleanToken`/`duplicateIds`/`finding` dedup, **When** visual and retained
   inspection records are produced, **Then** their tokens, findings, and serialized form are unchanged.

---

### User Story 4 - Split the Testing god-module (Priority: P4)

A contributor authoring readiness/evidence helpers wants `Testing.fs` (4,629 lines, the largest `src`
file) divided into per-domain files (Visual, RetainedInspection, Evidence, Compositor,
Feature-readiness) so each testing concern is its own unit.

**Why this priority**: Largest single `src` file; clear per-domain seams. It is consumed widely by
tests and the harness, so byte-stable surface matters, but the split itself is mechanical.

**Independent Test**: Build `FS.GG.UI.Testing`, run the suites that consume it, and confirm
`Testing.fsi` surface baseline plus all emitted readiness/evidence markdown+JSON are unchanged.

**Acceptance Scenarios**:

1. **Given** the per-domain testing files, **When** `FS.GG.UI.Testing` is built, **Then** its public
   `.fsi` surface and surface-area baseline are byte-identical to baseline.
2. **Given** the split, **When** readiness/evidence artifacts are regenerated, **Then** every emitted
   markdown and JSON file is byte-identical to baseline.

---

### User Story 5 - Tame the `RetainedRender.step` god-function (Priority: P5)

A contributor maintaining the retained renderer wants `step` (~600 lines, ~30 `let mutable`
accumulators, 8 nested recursive walks) restructured around a `StepMetrics` record with each pass
pulled into a named function, and the build/paint scaffolding it duplicates with `init` unified — so a
render-step change is made in a named pass rather than the middle of a 600-line body.

**Why this priority**: The single largest function in the repo and a frequent edit site, but the
refactor is internal to one function family and the renderer is heavily test-covered, so it can land
independently. Mutation is allowed where it is the simpler/faster code (Constitution III) — the goal is
named passes over a typed accumulator, not dogmatic immutability.

**Independent Test**: Build `FS.GG.UI.Controls`, run the retained-render and damage-locality suites,
and confirm step output (rendered scene, damage regions, metrics, promotion decisions) is unchanged.

**Acceptance Scenarios**:

1. **Given** the extracted `StepMetrics` + named passes, **When** the retained renderer runs any
   covered scenario, **Then** its rendered output, damage regions, and metrics are byte-identical.
2. **Given** the unified build/paint scaffold, **When** `init` and `step` run, **Then** neither
   duplicates the other's scaffolding and both produce unchanged results.

---

### User Story 6 - Tame the `runInteractiveAppWithLauncher` god-function (Priority: P6)

A contributor working on the Elmish frame loop wants the ~20 `ref` cells of ad-hoc frame state in
`runInteractiveAppWithLauncher` (~500 lines, ~15 nested closures) promoted to a `FrameLoopState`
record with module-level functions, so the frame loop reads as typed state transitions instead of an
untyped mutable object.

**Why this priority**: Second-largest function and a complex closure tangle, but it sits at the
interactive-app edge (harder to exercise headlessly) and is the most contained of the six, so it is
sequenced last.

**Independent Test**: Build `FS.GG.UI.Controls.Elmish`, run its tests and any frame-loop/render-lag
trace suite, and confirm `ControlsElmish.fsi` surface baseline and frame-loop behavior/traces are
unchanged.

**Acceptance Scenarios**:

1. **Given** the `FrameLoopState` record, **When** the interactive app runs a covered scenario,
   **Then** frame-loop transitions, emitted commands, and render-lag traces match baseline.
2. **Given** the promoted state, **When** `FS.GG.UI.Controls.Elmish` is built, **Then** its public
   `.fsi` surface and surface-area baseline are byte-identical to baseline.

---

### Edge Cases

- **Public-surface leakage.** Splitting a module risks accidentally promoting a previously-private
  helper to public, or relocating a public symbol to a new namespace/module path. Either is a surface
  change and is forbidden — the `.fsi` surface baseline diff is the gate that catches it. New files may
  need their own `module internal` (FS0078) or a companion `.fsi`; the *union* of public surface must
  be unchanged.
- **Module ordering / forward references.** F# compiles in file order; splitting one module into
  several files requires a compile order that preserves every existing reference with no new cycle. A
  split that would require a back-edge or reorder a public symbol's definition site is out of scope for
  that family and stays as-is, with the reason recorded.
- **Module-level mutable side-channels.** `realTextMeasurer`/`measurementVersionBucket` (Scene) and any
  similar global must keep identical observable behavior after isolation; moving a mutable must not
  change initialization timing or first-use semantics.
- **Net line increase.** Splits add file/`.fsi` headers and may slightly increase total lines; that is
  acceptable here (the goal is size-per-unit and legibility, not line reduction). What is NOT acceptable
  is a split that changes output, surface, or build/test red-green state.
- **Hidden output differences.** A relocated renderer/evidence path may emit a subtly different
  ordering or constant; the byte-stability sweep (regenerated readiness/evidence artifacts + surface
  baselines + full test red/green) is the catch-all gate.
- **Target overshoot.** The size targets (no module > ~1,500 lines, no function > ~150 lines) are
  goals, not hard rules: if a genuinely cohesive unit cannot be split below target without harming
  legibility or surface stability, it stays and the reason is recorded.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Each of the six god-module / god-function targets MUST be reorganized along the seams
  named in the Context table (concern-scoped files, extracted records, named passes, combinators),
  bringing each touched file toward the size targets in SC-005.
- **FR-002**: Every shipped `FS.GG.UI.*` package's public `.fsi` surface and surface-area baseline MUST
  remain byte-identical to a baseline captured immediately before the change. No public symbol may
  change name, namespace, module path, or signature.
- **FR-003**: All rendered output, evidence/readiness artifacts (Markdown + JSON), viewer
  screenshots/observations, scene hashes/fingerprints, damage regions, and CLI/harness output MUST be
  byte-identical to baseline for every touched subsystem.
- **FR-004**: The two near-duplicate viewer window-lifecycle paths
  (`runPresentedPersistentWindow`/`runPersistentWindow`) MUST be unified behind one lifecycle scaffold
  with identical observable behavior, OR, if unification is found to change behavior or surface, left
  explicit with the reason recorded (FR-009).
- **FR-005**: The ×17 chart preamble in `Control.fs` MUST be hoisted into a shared `withPoints`
  combinator (plus a shared bar-layout helper) with byte-identical chart output, OR left explicit per
  FR-009 where a call site genuinely diverges.
- **FR-006**: The started-but-unfinished `cleanToken`/`duplicateIds`/`finding` dedup between Scene's
  `VisualInspection` and `RetainedInspection` MUST be completed, with inspection records byte-identical
  to baseline.
- **FR-007**: `RetainedRender.step` MUST be restructured around a `StepMetrics` record and named passes,
  and `runInteractiveAppWithLauncher` around a `FrameLoopState` record, with identical observable
  behavior; mutation MAY be retained where it is the simpler/faster code with a one-line disclosure
  comment (Constitution III).
- **FR-008**: `dotnet build` and `dotnet test` (full Release sweep under `DISPLAY=:1`) MUST be green at
  the end of each story, with pre-existing baseline reds (known `Package.Tests` / `ControlsGallery`
  package-feed reds) unchanged and not regressed. No assertion may be weakened to green a build.
- **FR-009**: Any target (or sub-seam of a target) whose split would change public surface, change
  output, require a new dependency/back-edge, or harm legibility MUST be left in its current form and
  the exclusion recorded with its rationale, rather than forcing the split.
- **FR-010**: The change MUST introduce no new project, no new package dependency, and no new
  inter-project reference; all new files stay within their existing project and the dependency graph
  stays acyclic and unchanged.

### Key Entities *(include if feature involves data)*

- **Surface-area baseline**: the per-package `.fsi`-derived public-surface snapshot that the API
  surface-drift check validates; the binding oracle for "no public surface change" (FR-002).
- **Byte-stability baseline**: the pre-change capture of regenerated readiness/evidence artifacts,
  viewer observations, scene hashes, and the full test red/green set, diffed after each story (FR-003,
  FR-008).
- **`StepMetrics`**: the extracted record replacing `RetainedRender.step`'s ~30 ad-hoc `let mutable`
  accumulators, threaded through named passes.
- **`FrameLoopState`**: the extracted record replacing `runInteractiveAppWithLauncher`'s ~20 `ref`
  cells of ad-hoc frame state.
- **Concern-scoped module**: a newly-extracted file (e.g. `Viewer.Types`, `ChartGeometry`,
  `VisualInspection`) carved from a god-module along an existing seam, contributing the same public
  surface to its package as before.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every shipped package's public `.fsi` surface and surface-area baseline is byte-identical
  to baseline (surface-drift check green, zero baseline edits required).
- **SC-002**: 100% of regenerated rendered/evidence/readiness artifacts, viewer observations, scene
  hashes/fingerprints, and damage regions are byte-identical to baseline across all six splits.
- **SC-003**: `dotnet build` and `dotnet test` are green at the end of each story and at phase end, with
  the known pre-existing baseline reds unchanged (same red/green set as the captured baseline).
- **SC-004**: Each of the six targets is delivered as an independently-shippable slice that builds,
  passes the suite, and holds byte-stability on its own (verifiable per-story without the others).
- **SC-005**: After the splits, no touched module exceeds ~1,500 lines and no touched function exceeds
  ~150 lines — except units explicitly retained per FR-009, each recorded with its rationale. (Targets,
  not hard rules.)
- **SC-006**: The duplicated viewer window-lifecycle scaffolding, the ×17 chart preamble, and the
  unfinished Scene inspection dedup are each either removed/unified or explicitly retained with a
  recorded reason (FR-004/005/006/009).
- **SC-007**: No new project, package dependency, or inter-project reference is introduced; the
  dependency graph remains acyclic and unchanged.

## Assumptions

- "Next item in plan" resolves to **Phase 5** of the code-health refactoring plan; Phases 0–4 are done
  and merged (features 177/178/179/180/181), per project memory and recent commit history.
- The maintainer chose to scope this feature to **all six Phase-5 splits** (confirmed via clarification),
  rather than one split per feature, even though the plan describes Phase 5 as "do incrementally — each
  split is its own PR." The six user stories preserve that incremental, independently-shippable shape
  within one feature/branch.
- Line counts in the Context table are the actual counts at HEAD (re-confirmed by `wc -l`); they differ
  from the original report (e.g. `Testing.fs` is 4,629, not 4,550) because the tree moved since the
  report. Authoritative scope is the code at HEAD.
- All six targets are shipped `FS.GG.UI.*` packages with companion `.fsi` files; this is a **Tier 2**
  internal change with no public surface change (contrast Phase 4's internal `tools/` harness).
- Byte-stability against a captured baseline (surface baselines + regenerated artifacts + full Release
  sweep red/green) is the acceptance gate for "behavior unchanged," consistent with Phases 3–4.
- The size targets are legibility goals; net line count is expected to hold roughly flat or rise
  slightly (file/`.fsi` headers) and is explicitly NOT a success metric for this phase.

## Out of Scope

- **Phase 6 — type-safety hardening** (Control `Kind` registry, `SceneCodec` per-case symmetry table,
  boolean-trap cleanup). Some of these touch the same files but are a distinct, surface-affecting phase.
- Any change to shipped package public surfaces, `.fsi` contracts, or surface-area baselines.
- Changing observable behavior: rendered output, evidence/readiness verdicts, viewer observations, scene
  hashes, damage regions, or metrics — this is a pure structural refactor.
- New abstractions whose only justification is line reduction (the Phase-3/180 lesson): splits are
  justified by module/function size and legibility, not by net line savings.
- Cross-project DU migrations deferred from Phase 3 (e.g. `RetainedInspectionStatus` /
  `VisualInspectionStatus` ownership in `FS.GG.UI.Scene`).
- Relocating or renaming projects (Phase 2 work, already complete).

# Feature Specification: Cross-Cutting Dedup + State Records (Pattern C)

**Feature Branch**: `186-cross-cutting-dedup-state-records`

**Created**: 2026-06-22

**Status**: Draft

**Input**: User description: "next item in the plan" — Phase 2 of the god-module decomposition (`docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md` §6). Phase 1 shipped as feature 185 (harness data-table refactor).

## Overview *(context, not a spec section)*

This is **Phase 2 — Cross-cutting dedup + state records (Pattern C)** of the god-module
decomposition. It targets the duplicated *frame-metrics tuple* and the *scattered mutable state*
inside the two largest render-loop functions, plus two copy-pasted blocks in the testing/evidence
layer. Per the parent plan (§6, §8) this phase is **byte-identical-by-construction**: a pure
state-shape and dedup refactor with **no behavior change**, modeled on feature 182's `FrameLoopState`.
It is the cheap, low-risk insurance that makes the later hot-path pipeline split (Phase 6) tractable
by giving `RetainedRender.step` an explicit, named state record first.

Because Phase 2 is byte-identical, it does **not** require the §7 golden-image / perf gates (the
parent report scopes those to the render-path-altering phases only). Existing tests plus byte-level
comparison of frame output and emitted artifacts are the verification.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Frame metrics defined and built once (Priority: P1)

A maintainer adding or reading a per-frame metric currently has to find and edit a 32-field record
construction copied across the Elmish frame loop (2 full hand-spelled sites plus 4 `{ zero with … }`
partial-update sites; spec originally estimated ~36 fields / ~5 sites — revised at plan time, see
plan.md "Scale/Scope"). After this story, the 32-field `FrameMetrics` value is assembled by a
**single builder** that maps the frame's work-reduction carriers and metadata into the record; every
full-construction frame-emitting site delegates to that one builder instead of re-spelling all 32 fields.

**Why this priority**: Highest duplication-count win in the phase (one 32-field record × 2 full
construction sites), fully self-contained in one file, lowest risk, and immediately removes the most
error-prone copy-paste (a new field today must be wired in five places or silently reads stale/zero).

**Independent Test**: Build the project; confirm the metrics record is constructed at exactly one
site and the frame-emit paths route through it; run the existing Elmish metrics test suite
(Feature108–120 metrics tests) and confirm the same pass/fail set and byte-identical metrics output.

**Acceptance Scenarios**:

1. **Given** the Elmish frame loop, **When** the project is built, **Then** the full 32-field
   metrics record is spelled out at exactly one builder site and the 2 former full-construction sites
   delegate to it.
2. **Given** the existing metrics/baseline test corpus, **When** the suite runs after the change,
   **Then** the pass/fail set is identical to before and the emitted per-frame metrics values are
   byte-identical.
3. **Given** a maintainer adds one new metric field, **When** they wire it into the single builder,
   **Then** every frame-emit path reflects it with no other edit site required.

---

### User Story 2 - Explicit named frame state for the render-loop god-functions (Priority: P2)

The retained-render `step` function threads ~19 `let mutable` accumulators (id minting, recompute /
shift counters, memo/text/picture cache hit-miss pairs, repainted-box accumulator, virtualization
counters, replay counters) by hand, and the Elmish `runScriptCore` threads 7 mutable metric carriers
that shuttle those metrics into the builder from Story 1 (3 further workflow-state mutables are out of
this story's scope; research Decision 2). After this story, each function's scattered
mutables are collapsed into a single named state record (a `FrameState` for `step`, a
`FrameScriptState` for `runScriptCore`), making the frame's data flow explicit while preserving the
exact same update order and values.

**Why this priority**: This is the strategic deliverable of the phase — it makes mutable state
explicit and named, and is the stated **prerequisite** for the Phase 6 `step` pipeline split. It is
larger and slightly higher-risk than Story 1 (touches the hot path), so it follows the clean win but
is still byte-identical by construction.

**Independent Test**: Build; confirm `step` and `runScriptCore` no longer declare loose
frame-accumulator mutables but instead operate over one state record each; run the Controls retained
render + Elmish metrics suites and confirm the same pass/fail set and byte-identical rendered frames
and metrics.

**Acceptance Scenarios**:

1. **Given** `RetainedRender.step`, **When** the project is built, **Then** its ~19 loose frame
   accumulators are held in one named state record with the same fields and the same update sequence.
2. **Given** `RetainedRender.init` (which today re-spells `step`'s cache-seeding scaffolding),
   **When** it is converged onto the shared state record, **Then** cold-start cache seeding produces
   the same values as before.
3. **Given** `ControlsElmish.runScriptCore`, **When** the project is built, **Then** its 7 mutable
   metric carriers are held in one named state record (the 3 workflow-state mutables MAY also move in
   for cohesion but are not required).
4. **Given** the retained-render and metrics test corpora, **When** the suites run after the change,
   **Then** the pass/fail set is identical and rendered frames + metrics are byte-identical.

---

### User Story 3 - Inspection-validation logic written once (Priority: P3)

The visual-inspection and retained-inspection validators contain ~95 lines of near-identical
"validate the declared exceptions against the findings, compute unused/invalid, build diagnostics,
derive status" logic, differing only in the status/severity types and diagnostic wording (retained
also admits a `Warning` severity that visual does not, and derives a `ReviewRequired` status when a
`Warning` is present). After this story that algorithm exists once,
parameterized over the per-family differences, with both call paths delegating to it.

**Why this priority**: Independent of the render-loop stories and lives in the testing/evidence
layer, so it can land any time; medium value (removes a ~95-line copy-paste) but lower urgency than
the render-loop state work that unblocks Phase 6.

**Independent Test**: Build; confirm a single shared validation routine backs both the visual and
retained validators; run the Testing inspection-validation suites (Feature165 visual, Feature170
retained) and confirm the same pass/fail set and that the retained path still admits `Warning` while
the visual path still does not.

**Acceptance Scenarios**:

1. **Given** both inspection validators, **When** the project is built, **Then** the shared
   validation algorithm is defined once and both validators delegate to it.
2. **Given** a retained inspection with a `Warning`-severity finding, **When** validation runs,
   **Then** it is handled exactly as before (including deriving `ReviewRequired`); **And** the visual
   path still rejects/omits `Warning` as before.
3. **Given** the inspection-validation test corpus, **When** the suites run, **Then** the pass/fail
   set is identical and emitted validation diagnostics/status are semantically equivalent
   (byte-identical where the prior wording was identical).

---

### User Story 4 - Single markdown managed-section updater (Priority: P4)

Three separate `updateManagedSection` implementations (~129 lines total, across the readiness-metrics,
inspection-summary, and retained-summary report writers) re-implement the same begin/end-marker
section update: count the markers, and on `(0,0)` append, on `(1,1)` replace between markers, else
fail loud. After this story that logic exists once behind a single generic managed-section abstraction
that all three writers call.

**Why this priority**: Smallest, fully internal to the evidence-writing layer, and independent of all
other stories; pure tail-end cleanup with no behavior change, so it carries the lowest priority.

**Independent Test**: Build; confirm one shared managed-section updater backs all three report
writers; re-emit the affected readiness/inspection summary artifacts and confirm byte-identical
output, including the fail-loud behavior on duplicate/imbalanced markers.

**Acceptance Scenarios**:

1. **Given** the three report writers, **When** the project is built, **Then** all three route their
   section updates through one shared managed-section updater.
2. **Given** a target file with no existing managed section `(0,0)`, **When** an update runs, **Then**
   the section is appended exactly as before.
3. **Given** a target with one balanced marker pair `(1,1)`, **When** an update runs, **Then** the
   region between markers is replaced exactly as before.
4. **Given** a target with duplicate or imbalanced markers, **When** an update runs, **Then** it
   fails loud (reports an error / refuses to write) exactly as before — never a silent last-wins
   overwrite.

---

### Edge Cases

- **Float accumulation order**: collapsing the `step` accumulators into a record MUST preserve the
  exact evaluation/update order of the metric sums, because a reordering could change floating-point
  accumulation and therefore the rendered frame / metric bytes. Byte-identity is the gate that
  catches a slip.
- **`init`/`step` cache-seeding convergence**: `init` seeds caches cold while `step` advances them;
  converging both onto the shared state record MUST keep the cold-start seeded values identical.
- **Severity asymmetry**: the unified inspection validator MUST preserve that retained inspection
  admits a `Warning` severity (and derives `ReviewRequired` when one is present) while visual
  inspection admits neither — the generalization must not accidentally widen or narrow either family's
  accepted severities or derived statuses.
- **Imbalanced section markers**: the unified managed-section updater MUST keep the existing
  fail-loud branch (neither `(0,0)` append nor `(1,1)` replace) rather than guessing.
- **Public surface accident**: a new state record or builder accidentally exposed as a **public**
  symbol in a `.fsi` would change the package surface; all new types introduced by this phase MUST
  stay non-public — private by `.fsi` absence, or `internal` (assembly-internal) for the shared
  Testing helpers declared in `TestingVisual.fsi`'s `module internal …` (which do not enter the
  surface baseline). The gate is the surface-baseline diff, not the mere presence of `.fsi` edits.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST construct the (≈32-field) per-frame metrics record at exactly one builder
  site, with every full-construction frame-emitting path delegating to it (no field re-spelled across
  multiple sites). The 4 `{ zero with … }` partial-update sites are out of strict scope (research
  Decision 1); they MAY route through the builder if byte-identical, but are not required to.
- **FR-002**: `RetainedRender.step` MUST hold its frame accumulators (id minting, recompute/shift
  counters, memo/text/picture cache hit-miss pairs, repainted-box accumulator, virtualization and
  replay counters) in a single named state record, preserving the same fields, update order, and
  final values.
- **FR-003**: `RetainedRender.init` MUST be converged onto the same shared state record / seeding path
  used by `step`, producing byte-identical cold-start cache seeding and first-frame output.
- **FR-004**: `ControlsElmish.runScriptCore` MUST hold its 7 frame-metric carrier mutables in a single
  named state record that feeds the FR-001 builder. The 3 workflow-state mutables (`model`/`retained`/
  `lastRender`) are out of scope and MAY remain loose (research Decision 2).
- **FR-005**: The visual-inspection and retained-inspection validators MUST share one validation
  routine parameterized over their differing status/severity types and diagnostic wording, preserving
  each family's accepted severities and derived statuses (retained admits `Warning` and derives
  `ReviewRequired` when one is present; visual admits neither).
- **FR-006**: The three markdown managed-section updaters MUST be unified behind one shared
  abstraction implementing the `(0,0)`→append / `(1,1)`→replace / else→fail-loud semantics
  identically for all callers.
- **FR-007**: The refactor MUST produce **no behavior change**: rendered frames and per-frame metrics
  MUST be byte-identical to the pre-refactor baseline, and all emitted readiness / evidence /
  inspection artifacts MUST be byte-identical where the deduplicated logic was already identical and
  otherwise semantically equivalent.
- **FR-008**: The refactor MUST preserve the same red/green test set across all affected test projects
  (`tests/Controls.Tests`, `tests/Elmish.Tests`, `tests/Testing.Tests`, `tests/Rendering.Harness.Tests`);
  no assertion may be weakened to pass.
- **FR-009**: The refactor MUST NOT change the public package surface. The **public** signatures of the
  affected modules (`RetainedRender`, `ControlsElmish`, `TestingVisual`, `TestingRetainedInspection`)
  MUST be unchanged, the surface baseline (`readiness/surface-baselines/`) MUST be unchanged, and no
  package version bump is required. New types/builders introduced by this phase MUST NOT be public:
  most are private by absence from the curated `.fsi`. The one permitted exception is that the shared
  Testing helpers (US3 validation routine, US4 managed-section updater) MAY be declared `internal`
  inside a `module internal …` in `src/Testing/TestingVisual.fsi` — because the F# module-ordering
  back-edge requires a declaration visible to the later `TestingRetainedInspection` module. `internal`
  symbols are **not** part of the public surface and do **not** appear in the surface baseline
  (verified: the existing `module internal ReadinessFormatting` is absent from
  `readiness/surface-baselines/FS.GG.UI.Testing.txt`). Thus `TestingVisual.fsi` MAY gain `internal`
  lines while every other affected `.fsi` and every surface baseline stays byte-identical.
- **FR-010**: The refactor MUST NOT add any new project, external dependency, or inter-project
  reference; all extraction stays within the existing modules/projects.
- **FR-011**: Fail-loud behavior MUST be preserved at every deduplicated site (imbalanced section
  markers report an error; invalid declared inspection exceptions are surfaced as before) — never a
  silent skip, default, or last-wins overwrite.

### Key Entities

- **Frame metrics builder**: the single internal routine that maps a frame's work-reduction carriers
  and metadata into the existing 32-field metrics record; replaces the 2 full hand-spelled
  construction sites.
- **Retained frame state record**: an internal record holding the ~19 frame accumulators currently
  scattered as `let mutable` in `RetainedRender.step`, shared with `init`'s seeding path.
- **Script frame state record**: an internal record holding the 7 mutable metric carriers currently
  in `ControlsElmish.runScriptCore` (the 3 workflow-state mutables optionally included).
- **Inspection validation routine**: the single shared algorithm behind the visual and retained
  inspection validators, parameterized over status/severity type and diagnostic wording.
- **Managed-section updater**: the single shared abstraction behind the three `updateManagedSection`
  report writers, encapsulating the marker-count → append/replace/fail-loud decision.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The 32-field metrics record is constructed at exactly **1** full-construction site (down
  from 2; spec originally estimated ~5); verified by inspection/search.
- **SC-002**: `RetainedRender.step` declares **0** loose frame-accumulator `let mutable` bindings for
  the accumulators now held in the state record (down from ~19); `runScriptCore` declares **0** loose
  metric-carrier mutables (down from the 7 in-scope carriers; the 3 workflow-state mutables are
  excluded).
- **SC-003**: The inspection-validation algorithm and the managed-section update algorithm each exist
  at exactly **1** definition site (down from 2 and 3 respectively).
- **SC-004**: The red/green test set across the four affected test projects is **identical** to the
  pre-refactor baseline (no test added-green by weakening, none newly red).
- **SC-005**: Rendered frames and per-frame metrics are **byte-identical** to the pre-refactor
  baseline; emitted readiness/inspection artifacts are byte-identical where prior logic was identical
  and otherwise semantically equivalent.
- **SC-006**: The public package surface is **unchanged** — the surface baseline diff is empty and no
  package version is bumped.
- **SC-007**: Adding one new per-frame metric requires editing exactly **1** builder site to have it
  appear on every frame-emit path (demonstrated by a walkthrough, not necessarily a committed change).

## Assumptions

- **Byte-identical scope.** Per the parent plan (§6 Phase 2, §7, §8), Phase 2 is byte-identical by
  construction and does **not** require the §7 golden-image / perceptual / perf gates; those are
  scoped to the render-path-altering phases (5–6). Standing up the §7 gates is therefore **out of
  scope** for this feature and deferred to the first phase that actually alters render output.
- **Verification spine.** A pre-refactor baseline of rendered frames, per-frame metrics, and the
  affected evidence/readiness artifacts is captured before any production edit, and each user story is
  diffed against it (byte-identical for frame/metrics, byte-or-semantic for artifacts) — mirroring
  feature 185's baseline-first obligation.
- **Internal-only surface.** `src/` modules here are part of the `FS.GG.UI.*` package, but the new
  records/builders are non-public helpers: render-loop helpers are private by `.fsi` absence, and the
  shared Testing helpers are `internal` declarations in `TestingVisual.fsi`'s `module internal …`.
  Neither kind enters the public surface, so this is a Tier-2 internal change with no surface-baseline
  regeneration and no version bump — even though `TestingVisual.fsi` itself gains `internal` lines
  (contrast with feature 185, whose harness assembly is not in the package surface at all).
- **Field counts are current-tree estimates, reconciled at plan time.** The spec's original ~36 metric
  fields / ~5 sites, ~10 `runScriptCore` mutables, ~95-line validator overlap, and ~129-line
  section-updater overlap were re-confirmed against the 2026-06-22 tree in plan.md "Scale/Scope" and
  research Decision 1: **32 fields**, **2** full construction sites (+4 partials), **19** `step`
  mutables, **7** `runScriptCore` metric carriers (+3 workflow-state), **~65–71-line** validator
  overlap, **3 × ~41–47-line** section-updaters. The FR/SC text above is updated to those numbers; the
  requirement remains "defined/built once," not a specific count.
- **Story independence.** The four user stories are independently shippable: Story 1 and Story 2 share
  the metrics path (Story 2's `runScriptCore` record feeds Story 1's builder, so landing Story 1 first
  is natural but not required), while Stories 3 and 4 are independent of the render loop and of each
  other.

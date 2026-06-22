# Feature Specification: `RetainedRender.step` Pipeline Decomposition (Pattern B + C)

**Feature Branch**: `190-retained-render-step-pipeline`

**Created**: 2026-06-22

**Status**: Draft

**Input**: User description: "next item in plan" — Phase 6 (final phase) of the god-module decomposition
campaign (`docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md` §4.1 / §6).
Decompose the ~645-line `RetainedRender.step` frame function into a composition of named, independently
testable stages, and converge the parallel `init` scaffolding onto the same stage bodies.

## Overview

`RetainedRender.step` is the single largest and riskiest function in the codebase: one monolithic
per-frame function that performs the entire retained-mode frame lifecycle — diff the previous against
the next control tree, compute the layout dirty set and run incremental layout, mint identities and walk
the reconciliation tree (Keep/Replace/Update + ChildKeep/Move/Insert/Remove), make fragment-reuse
decisions, count virtualization, update the bounded picture cache, sample animation clocks, collect
offscreen/state diagnostics, and assemble the render result with 15+ metrics. `init` (~97 lines)
duplicates much of the build/paint scaffolding as a parallel cold-start copy.

Phase 2 of the campaign (feature 186) already introduced the **`FrameState` record** that replaced the
15+ `let mutable` accumulators; both `step` and `init` already thread that record. This phase builds on
that foundation to extract the frame lifecycle into **stage modules** so the data flow is explicit, each
stage is testable in isolation, and `init` shares one set of stage bodies instead of a divergent copy.

This is the **last and highest-risk** phase of the campaign because it touches the render hot path: stage
boundaries can materialize intermediate values that change allocation counts and (via float-accumulation
order) frame bytes. It is therefore gated by an explicit regression posture (see Assumptions and the §7
campaign gates) rather than relying solely on byte-equality the way the earlier, off-hot-path splits did.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - `step` is a stage composition (Priority: P1)

A maintainer reading or modifying the per-frame render path opens `RetainedRender` and finds `step`
expressed as a short composition of four named stages — diff, layout, paint, assembly — each a
self-contained unit that consumes and produces an explicit value (threading the existing `FrameState`),
instead of one ~645-line function with interleaved concerns. They can reason about, test, and change one
stage without holding the entire frame lifecycle in their head.

**Why this priority**: This is the core deliverable of Phase 6 and the campaign's final structural goal.
It delivers the standalone value (a comprehensible, independently testable frame pipeline) even if `init`
convergence (US2) is never done.

**Independent Test**: Confirm `step`'s post-decomposition body is a stage composition whose stages each
have focused unit tests; render the existing scene corpus through the new `step` and confirm emitted
scenes and `hashScene` fingerprints match the captured baseline (golden-hash zero-delta, or a reviewed
and approved delta), the public `step` signature/result is unchanged, and the full test matrix red set
equals the pre-change baseline.

**Acceptance Scenarios**:

1. **Given** the pre-change frame corpus and golden hashes, **When** the corpus is rendered through the
   decomposed `step`, **Then** every emitted scene and `hashScene` fingerprint is byte-identical to the
   baseline (or any divergence is explicitly reviewed and recorded under the golden-hash review gate
   before being accepted).
2. **Given** the decomposed pipeline, **When** a stage (e.g. the layout dirty-set / incremental-layout
   stage) is exercised in isolation with a crafted input, **Then** it produces the expected output and
   updated `FrameState` without needing the other stages to run — i.e. the stage is independently
   unit-testable.
3. **Given** the public `RetainedRender` surface, **When** the surface-drift gate runs after the split,
   **Then** the `FS.GG.UI.Controls` public surface diff is empty (the stages are internal;
   `step`/`init`/their result types are unchanged) — or, if non-empty, it is reviewed and the version is
   bumped per the gate.
4. **Given** the existing per-frame perf/responsiveness lanes, **When** they run against the decomposed
   `step`, **Then** per-frame allocation count and frame time stay within the agreed budget margin
   (no regression).

---

### User Story 2 - `init` converges onto the shared stages (Priority: P2)

A maintainer changing cold-start (first-frame) behavior no longer has to keep `init`'s parallel
build/paint scaffolding in sync with `step` by hand. `init` is re-expressed as the same stage bodies in
their cold-start configuration (full layout + seed paint + assembly), so a change to a shared stage
benefits both paths and the ~97-line duplicate copy is eliminated.

**Why this priority**: High-value de-duplication and the report's stated Phase 6 exit ("converge `init`
onto the shared stages"), but secondary to US1 — it is only worthwhile if it nets a genuine reduction in
duplicated logic without distorting cold-start semantics.

**Independent Test**: Render a representative set of controls through `init` cold-start and confirm the
`RetainedInit` result (scene, bounds, identities, seeded caches, metrics) is byte-identical to the
baseline; confirm the duplicated build/paint scaffolding is removed in favor of the shared stage bodies;
confirm net line count drops.

**Acceptance Scenarios**:

1. **Given** the baseline cold-start outputs, **When** the same controls are passed through the converged
   `init`, **Then** the emitted scene, bounds-by-id, minted identities, seeded caches, and metrics are
   byte-identical (or a reviewed, approved golden delta).
2. **Given** the converged implementation, **When** a shared stage body is modified, **Then** both `step`
   and `init` observe the change from one definition (no second copy to update).
3. **Given** the carry-forward lesson (feature 180 SC-005 / 189 US4), **When** convergence does not net a
   real reduction in duplicated logic, **Then** US2 is dropped without blocking US1.

---

### User Story 3 - Hot-path regression gate is in place and green (Priority: P3)

A maintainer (and CI) can demonstrate that this hot-path change did not regress rendering or performance,
because the campaign's §7 replacement gates exist and pass for this feature: a golden equivalence check
over the scene corpus, a golden-hash review step for any intentional hash change, and a per-frame
allocation/time budget on the existing perf lanes.

**Why this priority**: The report mandates these gates be available **before** Phases 5–6 land. The
earlier phases (185–189) achieved byte-identity and leaned on golden-hash + existing lanes; Phase 6 is the
one most likely to perturb allocation/float order, so the gate must be concrete and asserted here. It is
P3 because it is verification infrastructure supporting US1/US2 rather than user-visible behavior, but it
is a hard gate on accepting US1.

**Independent Test**: Run the regression gate against an intentionally perturbed `step` and confirm it
fails loudly (catches a regression); run it against the real decomposition and confirm it passes.

**Acceptance Scenarios**:

1. **Given** the scene/golden-hash corpus, **When** an unreviewed hash divergence is introduced, **Then**
   the gate flags it for review rather than silently accepting it.
2. **Given** the per-frame perf lanes (features 160/161/167/173), **When** allocation count or frame time
   exceeds the agreed margin for a scenario, **Then** the gate fails.
3. **Given** the final decomposition, **When** the full gate runs, **Then** it passes (golden equivalent /
   reviewed delta; within perf budget).

### Edge Cases

- **Compile-order / back-edge**: stage modules must compile in producer→consumer order within
  `src/Controls`; a stage extraction that creates a back-edge (e.g. an assembly stage a layout stage
  depends on, or a stage referencing a not-yet-declared helper) must not be introduced. The reconciliation
  walk (`build`/`recurse`) and the offscreen `layoutNode` helper that earlier phases kept residual must
  not be forced into a cycle.
- **Float-accumulation order**: any stage boundary that materializes an intermediate value MUST preserve
  the numeric accumulation order that produces frame bytes / hashes; otherwise the change is a reviewed,
  recorded golden-hash delta, not a silent one.
- **FrameState threading**: the single mutable `FrameState` must be threaded through stages so that
  per-frame accumulators (text/memo/picture cache hits-misses, id minting, virtualization tallies,
  repainted boxes) end with exactly the same final values as today; no accumulator may be double-counted
  or dropped across a stage seam.
- **Empty / idle frame**: a frame with an empty dirty set (no changes) must still flow through all stages
  and produce the same zero-work result and metrics (`invalidated = 0`, etc.) as today.
- **Cold start vs. steady state**: `init` (full layout, seeded caches) and `step` (incremental layout,
  inherited caches) must remain distinguishable behaviors even when sharing stage bodies — the shared
  stage must be parameterized (full vs. incremental; seed vs. inherit) rather than collapsing the two.
- **Diagnostics/tracing parity**: the existing `RetainedRenderTrace.time "retained-step-*"` spans (diff,
  layout-dirty-set, build, count-virtual, picture-walk, offscreen-diagnostics, index-prior-own,
  state-collect, work-node-count) must still be emitted (same names / coverage) so responsiveness and
  render-lag evidence is unchanged.
- **Fail-loud preserved**: duplicate-key `KeyCollision` diagnostics and any other fail-loud behavior in
  the diff/reconcile path must keep firing identically; no exception is swallowed by a stage seam.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `RetainedRender.step` MUST be re-expressed as a composition of named stages covering, at
  minimum, diff, layout (dirty-set + incremental evaluation), paint (reconciliation walk + reuse/cache
  decisions), and assembly (render-result + metrics construction). The exact stage count and boundaries
  are fixed at design time but MUST make the four concerns separately identifiable.
- **FR-002**: Each stage MUST consume and produce explicit values (threading the existing `FrameState`),
  with no stage relying on hidden mutable state outside that threaded record and its declared inputs.
- **FR-003**: Each stage MUST be independently unit-testable — exercisable with a crafted input to assert
  its output and `FrameState` mutations without running the other stages.
- **FR-004**: The public `RetainedRender` surface MUST be preserved: `step`, `init`, and their
  argument/result types keep their names, shapes, and call sites. Stage modules are internal.
- **FR-005**: Emitted scenes and `hashScene` fingerprints over the captured baseline corpus MUST be
  byte-identical, OR any divergence MUST be explicitly reviewed and recorded as an approved golden-hash
  delta before acceptance (no silent change).
- **FR-006**: Per-frame allocation count and frame time MUST stay within an agreed budget margin on the
  existing perf/responsiveness lanes (features 160/161/167/173); a regression beyond the margin fails the
  feature.
- **FR-007**: `init` SHOULD be re-expressed onto the shared stage bodies (full-layout + seed configuration)
  to eliminate the duplicated build/paint scaffolding — conditional on netting a real reduction in
  duplicated logic (else dropped per FR-016).
- **FR-008**: The existing `RetainedRenderTrace.time "retained-step-*"` instrumentation spans MUST remain
  present with equivalent coverage so render-lag/responsiveness evidence is unchanged.
- **FR-009**: No new circular module dependency may be introduced; stage modules compile in
  producer→consumer order within `src/Controls`, and helpers earlier phases kept residual (e.g. the
  offscreen `layoutNode`, the reconciliation walk) stay placed to avoid a back-edge.
- **FR-010**: Fail-loud behavior MUST be preserved at every seam — `KeyCollision` and other diagnostics
  keep firing identically; no stage seam swallows an exception or weakens a failure path.
- **FR-011**: No existing test may be deleted, skipped, or weakened; every suite keeps its red/green
  result except explicitly reviewed golden-hash expected-output updates (Constitution V). New stage unit
  tests are added (FR-003).
- **FR-012**: A baseline MUST be captured BEFORE any production edit: the scene/golden-hash corpus,
  per-frame perf-lane numbers, the public-surface snapshot, and the full test-matrix red set, so each step
  diffs against it.
- **FR-013**: This phase introduces NO new project or package reference; work stays within
  `src/Controls` plus any baseline/consumer edits a non-empty surface diff would require.
- **FR-014**: The `FS.GG.UI.Controls` package version is bumped **iff** the regenerated, reviewed surface
  baseline diff is non-empty; an empty diff means no bump.
- **FR-015**: The campaign's §7 regression gate (golden equivalence over the corpus + golden-hash review
  step + per-frame alloc/time budget) MUST be available and asserted for this feature before US1 is
  accepted; it must demonstrably catch an injected regression.
- **FR-016**: Any sub-story that does not net a real reduction (notably US2 `init` convergence) MUST be
  dropped rather than shipped as added indirection (carry-forward lesson 180 SC-005 / 181 / 189 US4).

### Key Entities

- **Frame stage**: a named, internal unit of the per-frame lifecycle (diff / layout / paint / assembly)
  that takes explicit inputs plus the threaded `FrameState` and yields explicit outputs plus the updated
  `FrameState`. Composition of the stages reconstructs the current `step`.
- **`FrameState`**: the per-frame accumulator record (introduced in feature 186) holding text/memo/picture
  cache counters, id minting, virtualization tallies, repainted boxes, etc.; threaded through every stage.
- **Frame baseline**: the captured pre-change evidence set — scene/golden-hash corpus, perf-lane numbers,
  public-surface snapshot, test-matrix red set — that every story diffs against.
- **Regression gate**: the §7 mechanism (golden equivalence + golden-hash review step + per-frame
  alloc/time budget) that proves the hot-path change did not regress rendering or performance.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `RetainedRender.step` is no longer a single ~645-line function; it is a stage composition in
  which no individual stage body exceeds ≈250 lines, and `RetainedRender.fs` (currently ~2,172 lines) is
  reduced or its stages relocated such that no resulting file exceeds ≈1,500 lines.
- **SC-002**: Every emitted scene and `hashScene` fingerprint over the baseline corpus is byte-identical,
  or 100% of any divergences are recorded as reviewed-and-approved golden-hash deltas (zero silent
  changes).
- **SC-003**: Each named stage has at least one focused unit test that exercises it in isolation; all such
  tests pass.
- **SC-004**: Per-frame allocation count and frame time on the existing perf/responsiveness lanes are
  within the agreed margin of baseline for every measured scenario (no regression).
- **SC-005**: The full test matrix red set after the change equals the pre-change baseline red set (no new
  failures introduced).
- **SC-006**: The `FS.GG.UI.Controls` public surface diff is empty (no version bump), or any non-empty
  diff is reviewed and the version bumped accordingly.
- **SC-007**: If US2 ships, `init`'s duplicated build/paint scaffolding is gone and total line count drops;
  if it does not net a reduction, it is dropped (and that decision is recorded).
- **SC-008**: The regression gate (FR-015) demonstrably fails on an injected regression and passes on the
  final decomposition.

## Assumptions

- **Regression posture (resolves the §7 question with the campaign default)**: this phase TARGETS
  byte-identical scene/hash output by construction — `FrameState` already exists, so threading it through
  stage functions (rather than recomputing) is expected to preserve numeric/dispatch order. The §7
  golden-image equivalence harness is therefore treated as a *fallback* gate: byte-identity + golden-hash
  zero-delta + the existing perf/responsiveness lanes + existing visual-inspection evidence are the
  primary gate, and a perceptual golden-image harness is only stood up if byte-identity is genuinely
  broken by a stage boundary. This mirrors how phases 185–189 passed without a separate golden-image
  corpus. (If the team prefers building the perceptual harness unconditionally up front, that is a scope
  expansion to confirm at plan time.)
- **Phase 2 prerequisite is in place**: the `FrameState` record (feature 186) already replaces the 15+
  `let mutable` accumulators in both `step` and `init`; this phase composes stages over it rather than
  introducing it.
- **Stage boundaries follow the existing trace seams**: the current `RetainedRenderTrace.time
  "retained-step-*"` spans (diff, layout-dirty-set, build, count-virtual, picture-walk,
  offscreen-diagnostics, index-prior-own, state-collect, work-node-count) are the natural seams and map
  onto the four target stages; the precise grouping is fixed at design time and confirmed by a
  full-solution compile probe before US1 edits.
- **Internal stage modules follow the 188/189 precedent**: new `module internal` stage code lives under
  `src/Controls/Internal/` (the AttrKeys/Hashing/ControlPrimitives precedent), exempt from the
  folder-scoped `.fsi`-pairing gate, reached by tests via `InternalsVisibleTo`, never on the public
  surface — keeping the surface diff empty.
- **Names preserved on move**: `step`, `init`, `recurse`/`build`, `layoutDirtySet`, and the result types
  keep their names and call shapes so internal callers and the ~consumer/test tree resolve unchanged.
- **Single F# library project**: work stays in `src/Controls` (`FS.GG.UI.Controls`), net10.0, SkiaSharp
  over GL; GL-dependent suites run under `DISPLAY=:1` (X11) for local validation.
- **This is the final campaign phase**: after this, the only optional remainder is the deferred feature
  187 live-path work (viewer window runners / `GlHost.run`), which `OpenGl.fs` ≤~1,500 lines means is not
  required for the size goal.

## Dependencies

- Builds on feature 186 (`FrameState`/state-record extraction, Phase 2) — the stated prerequisite for the
  Phase 6 pipeline split.
- Depends on the campaign §7 regression-gate posture (golden-hash review + existing perf lanes 160/161/
  167/173 + visual inspection); see the Regression-posture assumption.
- Coordinates with the surface-drift gate (`tests/Package.Tests/SurfaceAreaTests.fs` +
  `scripts/refresh-surface-baselines.fsx` → `readiness/surface-baselines/FS.GG.UI.Controls.txt`).

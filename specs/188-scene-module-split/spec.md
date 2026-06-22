# Feature Specification: Scene.fs Module Split (Pattern E, finish FR-006 inspection dedup)

**Feature Branch**: `188-scene-module-split`

**Created**: 2026-06-22

**Status**: Draft

**Input**: User description: "next item in the plan."

> **Plan context.** This is **Phase 4** of the god-module decomposition campaign
> (`docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md` §4.3 / §6). Phase 1
> (harness data-table refactor, feature 185), Phase 2 (cross-cutting dedup + state records,
> feature 186), and Phase 3 (viewer/host/codec splits, feature 187) are shipped. Phase 4 decomposes
> `src/Scene/Scene.fs` — the dependency-free root of the rendering stack — by responsibility
> (Pattern E), and **finishes the started FR-006 inspection-finding dedup** that prior phases left
> in place because it changes which findings are emitted. Unlike Phases 1–3, this phase **takes the
> relaxed-constraints latitude** the maintainer granted (parent report preamble): it is permitted to
> **re-home public types** (a potential public-surface change, carrying a version bump only iff the
regenerated baseline diff is non-empty — see Change Classification) and to make the **behavior-
> affecting** inspection-dedup change. It therefore sits above the conservative module-split tier and
> is validated by semantic equivalence + reviewed-change sign-off rather than byte-equality alone.

> **Terminology notes (disambiguation).**
> - **"FR-006 dedup" / "legacy FR-006"** refers to the *started-but-unfinished inspection-finding
>   dedup work item carried over from a prior feature*, whose original requirement id there was FR-006.
>   It is **not** this spec's own **FR-006** (rendered-frame / artifact equivalence, below). In this
>   spec the dedup is governed by **FR-005**; "legacy FR-006" is used only as the historical name of
>   the work being finished. Where both could be meant, this spec writes "the started inspection dedup
>   (legacy FR-006)".
> - **`Scene.Types` / `Scene.Inspection` / `Scene.Evidence`** name the *file groupings* under
>   `src/Scene` (`Types.fs`, `Inspection.fs`, `Evidence.fs`), **not** nested sub-modules. Per the
>   planning-confirmed mechanism (research Decision 1 / 3) the extracted code stays at
>   `namespace FS.GG.UI.Scene` (namespace-level types in `Types.fs`; preserved module names
>   `VisualInspection`/`RetainedInspection`/`SceneEvidence`/`LayoutEvidence` in the new files), so the
>   re-home is surface-neutral. Read "extract into `Scene.Types`" etc. as "move into the `Types.fs`
>   file", not "nest under a new `Scene.Types` module".

## Change Classification

**Tier 1 (surface-changing) + behavior-affecting.** This phase intentionally exercises the relaxed
constraints. `Scene` is the dependency-free root with ~17 downstream consumers; splitting its
~767-line public type wall re-homes public types and *could* change the public `.fsi` surface, the
`FS.GG.UI.Scene` surface-area baseline, and any templates/consumers that name those types. **Whether a
package version bump is required is therefore conditional, gated on the regenerated surface baseline:**
under the planning-confirmed namespace-level file-split mechanism (plan §Summary / research Decision 1)
the re-home is surface-neutral (CLR `FullName`s unchanged → empty baseline diff → **no bump**); a bump
is required *iff* the regenerated, reviewed `FS.GG.UI.Scene` baseline diff is non-empty (the only
candidate is US2's shaping/measurer relocation). Separately, **finishing the started inspection dedup
(legacy "FR-006", see note below) is a deliberate behavior change**: it alters which duplicate
inspection findings are emitted,
so the inspection/evidence artifacts change semantically. Both changes are gated by the parent
report's §7 discipline applied as a **verification method** (semantic-artifact diff for evidence;
golden-hash/golden-image review-gate for any rendered output; explicit reviewed-and-approved sign-off
for the dedup delta) — not by byte-identical equality. Rendered frames for unchanged scenes must
remain equivalent; only the inspection-finding set changes, and only where the dedup intends it.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Extract `Scene.Types`; `Scene` becomes builders-only root (Priority: P1)

A maintainer opening `src/Scene/Scene.fs` today faces a single ~2,084-line file that opens with a
~767-line wall of primitive and `SceneNode` type definitions before any builder code, then mixes
color/paint/path/scene builders, the glyph-shaping trio, the `realTextMeasurer` side-channel, and
four inspection/evidence modules. After this story, the primitive/`SceneNode` type block lives in its
own `Scene.Types` file (see naming note below) and `Scene` itself is reduced to the builder surface
(`empty`/`group`/`rectangle`/…) rooted on those types. Because `Scene` is the dependency-free root,
the extracted types are re-homed deliberately. Under the planning-confirmed namespace-level mechanism
this re-home keeps every type's CLR `FullName` unchanged, so the public `.fsi`, the `FS.GG.UI.Scene`
surface baseline, the ~17 consumers, and any templates that name the types resolve without churn and
the surface baseline diff is empty — **no version bump is required for the re-home** (a bump is only
triggered if the *overall* regenerated baseline diff turns out non-empty; see Change Classification).

**Why this priority**: The type wall is the single largest block in the file and the root-position
hazard the parent report calls out (re-homing `Scene`'s types ripples to 17 consumers). Doing it
first establishes the new module floor every other story builds on, and delivers the biggest
standalone size reduction. It is the change that genuinely needs the relaxed surface freeze, so
proving it lands cleanly de-risks the rest of the phase.

**Independent Test**: Can be fully tested by building the full solution (all ~17 consumers compile
against the re-homed types), running the `Scene` round-trip / package / inspection suites, and
confirming the regenerated `FS.GG.UI.Scene` surface baseline reflects exactly the intended type
re-home (reviewed diff) with the version bump applied — no other public-surface drift.

**Acceptance Scenarios**:

1. **Given** the pre-refactor build, **When** the primitive/`SceneNode` types are extracted to
   `Scene.Types` and `Scene` is reduced to builders, **Then** the whole solution still compiles, all
   ~17 consumers resolve the types from their new home, and no back-edge/ordering error is introduced.
2. **Given** the corpus exercised by existing `Scene` round-trip and rendering tests, **When** scenes
   are built and rendered after the extraction, **Then** the produced scene values and rendered frames
   are equivalent to the pre-refactor baseline (the type re-home changes location, not representation).
3. **Given** the regenerated `FS.GG.UI.Scene` surface-area baseline, **When** the US1 split is
   complete, **Then** under the namespace-level mechanism its diff against the prior baseline is
   **empty** (the re-home is surface-neutral, so no version bump is required for US1); any non-empty,
   reviewed diff that does arise is limited to the intended changes and triggers the version bump.

---

### User Story 2 - Unify the glyph-shaping trio into `Text.Shaping`; isolate the text-measurer seam (Priority: P2)

A maintainer touching text shaping today must keep three near-parallel builders in sync —
`buildGlyphRun`, `buildFallbackShapedText` (which wraps `buildGlyphRun`), and
`glyphRunDataFromShapedText` — which share ~60% of their logic, plus a module-level mutable
`realTextMeasurer` side-channel (`setRealTextMeasurer`) wired into measurement. After this story, the
trio is unified behind one parameterized shaped-text builder (plus its fingerprint) in a `Text.Shaping`
module that also owns the `realTextMeasurer` seam, so a shaping change is made once and the
measurement side-channel has a single clear home.

**Why this priority**: It removes a real triplicated-logic hazard and quarantines the only mutable
side-channel in the file, improving both legibility and safety. It is sequenced after the type
extraction because the shaping builders depend on the (now re-homed) primitive types, and before the
inspection work because shaping has no behavior change to gate (it is byte-equivalent by construction).

**Independent Test**: Can be fully tested by running the text-shaping / glyph-run / text-measurement
suites and confirming glyph runs, shaped-text results, and fingerprints are byte-identical before and
after, with the `realTextMeasurer` seam behaving identically (set/clear/measure round-trips unchanged).

**Acceptance Scenarios**:

1. **Given** the text corpus exercised by existing shaping tests, **When** the trio is unified behind
   one parameterized builder, **Then** every glyph run, shaped-text result, and fingerprint is
   byte-identical to the pre-refactor output.
2. **Given** a registered real text measurer (`setRealTextMeasurer`), **When** measurement runs after
   the seam is moved into `Text.Shaping`, **Then** the same measurement results are produced and the
   set/clear lifecycle behaves identically (no measurer is silently dropped or double-applied).

---

### User Story 3 - Extract `Scene.Inspection` + `Scene.Evidence`; finish the FR-006 inspection-finding dedup (Priority: P3)

A maintainer working on inspection/evidence today faces four modules (`SceneEvidence`,
`LayoutEvidence`, `VisualInspection`, `RetainedInspection`) living inside `Scene.fs`, and a started-
but-unfinished dedup of inspection findings (the `cleanToken` / `duplicateIds` / finding-dedup work
flagged FR-006) that prior phases deliberately left incomplete because completing it changes which
findings are emitted. After this story, `VisualInspection` + `RetainedInspection` live in
`Scene.Inspection` and `SceneEvidence` + `LayoutEvidence` live in `Scene.Evidence`, and the FR-006
dedup is completed so duplicate findings are collapsed consistently across both inspection paths.

**Why this priority**: This is the only behavior-affecting slice (the dedup changes emitted findings),
so it is sequenced last and validated by semantic-artifact diff + an explicit reviewed-change sign-off
rather than byte-equality. It is the slice that most needs the relaxed behavior-freeze, and isolating
it last keeps the surface-only (US1) and byte-equivalent (US2) slices clean and independently
shippable.

**Independent Test**: Can be fully tested by running the visual-inspection / retained-inspection /
scene-evidence / layout-evidence suites; for unchanged-finding cases the evidence artifacts are
semantically equivalent, and for the dedup cases the new (de-duplicated) finding set matches the
approved expected output recorded in the baseline review.

**Acceptance Scenarios**:

1. **Given** the inspection/evidence suites, **When** the four modules are moved to `Scene.Inspection`
   and `Scene.Evidence`, **Then** for inputs unaffected by the dedup the emitted findings and evidence
   artifacts are semantically equivalent to the pre-refactor baseline.
2. **Given** a scene that previously emitted duplicate findings (the FR-006 case), **When** it is
   inspected after the dedup is finished, **Then** the duplicate findings are collapsed exactly as the
   approved expected-output change specifies (the delta is intentional, reviewed, and recorded — not an
   accidental regression).
3. **Given** a malformed/degenerate scene that must still surface a finding, **When** it is inspected
   after the dedup, **Then** the genuine finding is still emitted with the same fail-loud diagnostic
   (dedup collapses duplicates only; it never silences a unique real finding).

---

### Edge Cases

- **F# root-position back-edge.** `Scene.fs` compiles first in `src/Scene/*.fsproj` and is the
  dependency-free root; any extracted file/module MUST be ordered so the type module precedes the
  builders and both precede consumers — a split that creates a circular module dependency won't compile.
- **Namespace-type resolution after re-home.** Re-homing public types from `Scene` to `Scene.Types`
  MUST keep them resolvable by the ~17 consumers (e.g. via the namespace, re-export, or consumer
  `open` updates) so no consumer silently fails to find a type or binds the wrong one.
- **Float / accumulation order on render-adjacent paths.** Builders and shaping must preserve the
  existing order of any numeric/float computation so rendered frames and glyph fingerprints stay
  equivalent for unchanged scenes.
- **Dedup must not silence unique findings.** The FR-006 dedup collapses *duplicate* findings only;
  it MUST NOT drop a unique real finding, reorder findings in a way that changes their meaning, or
  weaken a fail-loud diagnostic.
- **`realTextMeasurer` lifecycle.** Moving the mutable measurer seam MUST preserve set/clear/measure
  semantics; no measurer may be dropped, double-applied, or leaked across the move.
- **Evidence/inspection artifacts compared semantically.** Because the dedup intentionally changes
  emitted findings, equivalence for these artifacts is judged on parsed structure (status/counts/
  headers) against the approved baseline, not on byte-identity.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The primitive and `SceneNode` type definitions (the ~767-line type wall at the head of
  `Scene.fs`) MUST be extracted into their own `Scene.Types` module/file, leaving `Scene` as the
  builder surface rooted on those types, such that `Scene.fs` is reduced toward the ~1,500-line
  guideline (plan §6 "≈", not a hard assertion boundary).
- **FR-002**: The glyph-shaping trio (`buildGlyphRun`, `buildFallbackShapedText`,
  `glyphRunDataFromShapedText`) MUST be unified behind one parameterized shaped-text builder (plus its
  fingerprint) in a `Text.Shaping` module, eliminating the ~60% duplicated logic.
- **FR-003**: The module-level mutable `realTextMeasurer` side-channel (and `setRealTextMeasurer`)
  MUST be relocated into `Text.Shaping` as its single owner, preserving set/clear/measure semantics.
- **FR-004**: `VisualInspection` and `RetainedInspection` MUST be extracted into a `Scene.Inspection`
  module/file, and `SceneEvidence` and `LayoutEvidence` into a `Scene.Evidence` module/file.
- **FR-005**: The started FR-006 inspection-finding dedup (the `cleanToken` / `duplicateIds` /
  finding-dedup work) MUST be completed so duplicate findings are collapsed consistently across the
  visual and retained inspection paths.
- **FR-006**: Rendered frames and glyph fingerprints for scenes unaffected by the dedup MUST remain
  equivalent to the pre-refactor baseline (byte-identical where construction guarantees it). Inspection
  and evidence artifacts MUST remain **semantically** equivalent except for the **intended, reviewed**
  dedup delta, which MUST match an approved expected-output change.
- **FR-007**: The public-surface changes are limited to (a) the type re-homing into `Scene.Types`
  (surface-neutral under the namespace-level mechanism) and (b) any module-path changes from the
  inspection/evidence/shaping extraction. The regenerated `FS.GG.UI.Scene` surface-area baseline MUST
  reflect exactly these intended changes and no incidental drift. The package version MUST be bumped
  **iff** that regenerated, reviewed baseline diff is non-empty; if the split achieves a surface-neutral
  (empty-diff) outcome, no bump is required and FR-007 is satisfied by "zero incidental drift."
- **FR-008**: Every affected test suite MUST run with the same red/green result as the pre-refactor
  baseline, except for tests whose expected output is intentionally updated for the FR-006 dedup (those
  updates MUST be explicit, reviewed, and recorded). No test may be deleted, skipped, or have an
  assertion weakened to obtain green; tests blocked by genuine environment limits keep their existing
  skip + rationale.
- **FR-009**: Fail-loud behavior MUST be preserved at every refactored site — malformed/degenerate
  scenes, missing measurers, and genuine inspection findings MUST still surface the same diagnostics
  with no swallowed exceptions or silent degradation; the dedup MUST NOT silence a unique real finding.
- **FR-010**: No new project, NuGet dependency, or inter-project reference may be introduced. Work
  stays within `src/Scene` and the consumer/template/baseline updates that the type re-home requires.
- **FR-011**: A pre-refactor baseline (affected-suite red/green set + reference frames/fingerprints +
  inspection/evidence artifact corpus + public `.fsi`/surface snapshot) MUST be captured before any
  production edit; each story MUST be diffed against it, and the FR-006 dedup delta MUST be reviewed
  and approved against that baseline before it is treated as expected.
- **FR-012**: The FR-006 dedup change MUST be validated by the parent report's §7 discipline applied
  as a verification method — semantic-artifact diff for evidence/inspection output and a golden-hash/
  golden-image review-gate for any rendered output — rather than by byte-equality. Standing up the §7
  gates as a permanent harness is **out of scope** (deferred follow-up).

### Key Entities *(include if feature involves data)*

- **`Scene.Types` module**: the re-homed primitive and `SceneNode` type definitions, now the
  dependency-free type root that `Scene` builders and all consumers reference.
- **Unified shaped-text builder**: the single parameterized glyph/shaped-text construction routine
  (plus fingerprint) that replaces the three near-parallel builders, living in `Text.Shaping`.
- **Text-measurer seam**: the `realTextMeasurer` mutable side-channel and its set/clear accessor,
  re-homed under `Text.Shaping` as its single owner.
- **Inspection finding**: an emitted inspection result; the FR-006 dedup collapses duplicate findings
  (by their identity token) while preserving every unique finding.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `src/Scene/Scene.fs` no longer contains the primitive/`SceneNode` type wall, the glyph
  trio, the `realTextMeasurer` seam, or the four inspection/evidence modules; the resulting `Scene.fs`
  and each extracted file are at or below the ~1,500-line guideline (down from 2,084).
- **SC-002**: The three glyph-shaping builders are reduced to **one** parameterized builder (the
  duplicate shaped-text-builder count goes from 3 → 1) and the `realTextMeasurer` seam has exactly one
  owner.
- **SC-003**: The four inspection/evidence modules live in exactly two new files (`Inspection.fs`,
  `Evidence.fs`) and the dedup is applied uniformly across both inspection paths — meaning **each path
  collapses findings that share its own `FindingId`** (zero inspection paths still emit known-duplicate
  findings). "Uniform" denotes the same *collapse rule* (dedupe by `FindingId`, keep first, preserve
  unique findings) on both paths; it does **not** require an identical identity-key *function* — the
  retained path's `stableFindingId` legitimately includes a `transitionId` the visual path does not,
  and that distinction MUST be preserved (collapsing only within each path's own identity scope).
- **SC-004**: For the corpus exercised by existing tests, scenes unaffected by the dedup render to
  equivalent frames and glyph fingerprints (byte-identical where construction guarantees it), and the
  text-shaping output is 100% byte-identical before vs. after.
- **SC-005**: The affected test suites (`Scene` round-trip/package, visual/retained inspection,
  scene/layout evidence, text shaping, plus any consumer suites and harness evidence suites that read
  these artifacts) produce a red/green set identical to the captured baseline, except for the explicitly
  reviewed expected-output updates tied to the FR-006 dedup; no assertion weakened.
- **SC-006**: The regenerated `FS.GG.UI.Scene` surface-area baseline differs from the prior baseline
  **only** by intended, reviewed-and-approved changes (target: an empty diff under the namespace-level
  re-home and preserved module names; the sole legitimate candidate for drift is US2's shaping/measurer
  relocation), and the package version is bumped **iff** that diff is non-empty (empty diff ⇒ no bump).
- **SC-007**: The FR-006 dedup delta is captured as an approved expected-output change (semantic-diff
  review record), demonstrating the changed findings are intentional and that no unique real finding was
  silenced.

## Assumptions

- **This phase takes the relaxed-constraints latitude.** Per the user's scope decision, Phase 4
  exercises the maintainer-granted relaxation of the public-surface freeze and the behavior freeze
  (parent report preamble) — unlike Phases 1–3, which deliberately stayed behavior-preserving + surface-
  stable. Re-homing public types (with a version bump only if it proves surface-changing under the
  namespace-level mechanism) and finishing the started inspection dedup (legacy FR-006; a behavior
  change) are in scope by design.
- **§7 gates are applied as a verification method, not built as a deliverable.** The parent report
  mandates §7 gates once byte-stability is relaxed. This phase satisfies that by *applying* the §7
  disciplines (semantic-artifact diff, golden-hash/golden-image review-gate, reviewed-change sign-off)
  as the regression check for the dedup and any rendered output. Standing up the §7 gates as a permanent
  CI harness is **out of scope** and tracked as a separate follow-up (the report's "build gates early"
  recommendation).
- **The dedup delta is small and reviewable.** The FR-006 finding-dedup is assumed to change only the
  set of *duplicate* findings emitted for a bounded corpus; the change is captured as an approved
  expected-output diff. If completing the dedup turns out to require broad behavioral changes beyond
  duplicate collapse, that excess is **out of scope** and defers to a dedicated feature.
- **Type re-homing keeps types resolvable for all consumers.** The exact mechanism (namespace
  placement, re-export, or consumer `open` updates) is a planning decision; the assumption is that all
  ~17 consumers continue to resolve the types, validated by a full-solution compile.
- **Splits land inside `src/Scene`.** New responsibility groups are sub-modules/files within
  `src/Scene`; no new top-level project, package, or inter-project reference is created (FR-010). Only
  consumer/template/baseline edits required by the type re-home extend beyond `src/Scene`.
- **The current-tree line/location references in this spec (re-confirmed 2026-06-22 — `Scene.fs`
  2,084 lines; type wall ~L1–767; glyph trio L1127/1201/1249; `realTextMeasurer` L1306; inspection/
  evidence modules at L1532/1607/1710/1880) may drift** before implementation and MUST be re-confirmed
  at planning/implementation time, per the parent report's standing note.
- **Affected test projects**: the `Scene` codec round-trip / package / inspection / evidence suites,
  text-shaping suites, the ~17 consumer projects' suites, and any `tools/Rendering.Harness` evidence
  suites that consume scene inspection/evidence artifacts. The exact project list is confirmed during
  `/speckit-plan`.
- **Out of scope**: `Control.fs` (Phase 5), `RetainedRender.step` (Phase 6), the deferred Phase 3
  viewer-window/GlHost legibility passes (feature 187 follow-up), and standing up the §7 gate harnesses
  as permanent CI deliverables.

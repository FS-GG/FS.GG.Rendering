# Feature Specification: Viewer + GlHost + SceneCodec Module Splits (Pattern E + A)

**Feature Branch**: `187-viewer-glhost-codec-splits`

**Created**: 2026-06-22

**Status**: Draft

**Input**: User description: "next item in the plan."

> **Plan context.** This is **Phase 3** of the god-module decomposition campaign
> (`docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md` §6). Phase 1
> (harness data-table refactor, feature 185) and Phase 2 (cross-cutting dedup + state records,
> feature 186) are shipped. Phase 3 is the **module-by-responsibility split** of the three
> mid-sized god-modules in the viewer/host/codec layer (Pattern E), plus converting the
> hand-maintained scene-node serializer into a per-case codec table (Pattern A). It is the
> conventional-module-split tier that sits between the low-risk state-record work (Phase 2) and the
> hot-path structural work behind the §7 replacement gates (Phases 5–6).

## Change Classification

**Tier 2 (internal change)** — a behavior-preserving refactor. No public API surface change is
required, no observable behavior changes, no new dependency or project. Internal-only `.fsi`
module additions are permitted (precedent: feature 186 added an internal-only `module internal`
to one `.fsi`). The rendered frames, GL/window lifecycle behavior, emitted evidence/screenshot
artifacts, and serialized scene-package bytes all stay equivalent — byte-identical where
construction guarantees it, semantically equivalent where wording/ordering legitimately varies.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - SkiaViewer `Viewer` split + unified window lifecycle (Priority: P1)

A maintainer opening `src/SkiaViewer/SkiaViewer.fs` today faces a single ~3,370-line file whose
`Viewer` module owns input queueing, responsiveness/render-lag tracing, evidence/screenshot
workflows, and two near-duplicate persistent-window run loops (`runPresentedPersistentWindow` and
`runPersistentWindow`). After this story, the same responsibilities live in clearly named
responsibility groups (input queue, responsiveness, evidence, window lifecycle), and the two
near-duplicate window runners are unified behind one lifecycle scaffold so a window-loop change is
made once instead of twice.

**Why this priority**: It is the largest single god-module in the viewer layer and carries the
worst duplication (two parallel window run loops). Reducing it delivers the most maintainability
value and removes the highest-risk copy-forward divergence point in the viewer.

**Independent Test**: Can be fully tested by running the existing SkiaViewer / viewer-host /
responsiveness suites under `DISPLAY=:1` and confirming identical pass/fail plus equivalent
emitted evidence (screenshots, responsiveness/render-lag traces) before and after the split — with
no public `.fsi`/surface-baseline change.

**Acceptance Scenarios**:

1. **Given** the pre-refactor viewer test baseline, **When** the `Viewer` module is split into
   responsibility groups and the two window runners are unified, **Then** every previously passing
   viewer/responsiveness test still passes and every previously failing one fails identically.
2. **Given** a windowed run that produces a screenshot/evidence artifact, **When** it runs against
   the refactored viewer, **Then** the artifact is byte-identical (or semantically equivalent where
   wording/ordering legitimately varies) to the pre-refactor artifact.
3. **Given** the curated `SkiaViewer.fsi`, **When** the split is complete, **Then** its public
   surface (the `Viewer` / `GeneratedAppHost` / `Text` entry points consumers call) is unchanged
   (internal-only module additions allowed).

---

### User Story 2 - OpenGl `GlHost` split (Priority: P2)

A maintainer working on the GL host today faces a ~1,454-line file whose `GlHost.run` is a
~295-line function mixing GL lifecycle, the `interpretEffect` effect interpreter, offscreen
render→readback/screenshot capture, input handling, and damage/repaint logic in one body. After
this story, those concerns are separated into named responsibility groups (rendering, input, damage,
effects/screenshots) so a defect localizes to one stage rather than the whole run loop.

**Why this priority**: The GL host is the hardest-to-read function in the viewer layer and the
place GL/context failures must surface cleanly (Observability principle). Separating its concerns
makes the effect interpreter and screenshot path independently legible. It is sequenced after the
viewer split because both live in the SkiaViewer project and the viewer is the larger win.

**Independent Test**: Can be fully tested by running the GL-host / smoke / screenshot-evidence
suites under `DISPLAY=:1` and confirming identical pass/fail and equivalent screenshot/readback
artifacts, with the public `OpenGl.fsi` surface unchanged.

**Acceptance Scenarios**:

1. **Given** the pre-refactor GL-host baseline, **When** `GlHost.run` is decomposed into named
   responsibility groups, **Then** the GL smoke/host suite pass/fail set is identical and screenshot
   readbacks are equivalent.
2. **Given** a GL/context-creation failure path, **When** it is exercised after the split, **Then**
   it still fails loud with the same actionable diagnostic (no swallowed exception, no silent
   degradation) — distinguishing an implementation defect from a missing window-system/presentation
   setup.

---

### User Story 3 - SceneCodec split + per-case node codec table (Priority: P3)

A maintainer adding or changing a scene-node wire format today must edit three hand-maintained,
physically separate places in `src/Scene/SceneCodec.fs` — the `writeSceneNode` match (25 arms), the
`readSceneNode` tag dispatch, and the round-trip test — and nothing structurally guarantees the
write side and read side stay in sync. After this story, the codec is split into cohesive
responsibility groups (primitives, paint, path, text, scene, package) and each node type is
described by **one** codec entry holding both its write and its read, so the encode/decode symmetry
is enforced by construction and adding a node kind is a one-site change.

**Why this priority**: It is the most self-contained target (lives in the `Scene` project with no
viewer dependency) and the codec round-trip is already well-tested, making it the lowest-risk split.
Its payoff is correctness as well as size: it removes the repo's highest-risk silent-drift point
(write/read divergence). Sequenced last only because the viewer-layer files are the larger volume.

**Independent Test**: Can be fully tested by running the SceneCodec round-trip / package
import-export / inspection suites and confirming serialized bytes for the corpus of scenes are
identical before and after, with the public `SceneCodec.fsi` surface unchanged.

**Acceptance Scenarios**:

1. **Given** the corpus of scenes/packages exercised by the existing round-trip tests, **When** the
   codec is converted to a per-case table and split into responsibility groups, **Then** every scene
   serializes to identical bytes and deserializes to an equal value (round-trip identity preserved).
2. **Given** the per-case node codec table, **When** a node-kind case is present in the write side,
   **Then** the read side for that case is structurally co-located in the same entry (the compiler
   surfaces an incomplete table rather than allowing a silent write-without-read drift).
3. **Given** a malformed/truncated package or unknown node tag, **When** it is decoded, **Then** the
   codec fails loud with the same diagnostic behavior as before (no swallowed error).

---

### Edge Cases

- **Float / accumulation order on the render path**: window-lifecycle and GL-host extraction MUST
  preserve the existing order of frame composition and metric accumulation so rendered frames and
  per-frame traces stay equivalent (the same constraint that gated Phase 2).
- **Two window runners that look identical but are not**: unifying `runPresentedPersistentWindow`
  and `runPersistentWindow` MUST preserve each runner's genuinely distinct behavior (presented vs.
  non-presented paths); only the shared scaffold is collapsed, divergent steps remain selectable.
- **Codec endianness / tag width / field order**: the per-case table MUST write each case's bytes in
  the exact order and width the current `writeSceneNode` uses, so existing serialized artifacts and
  cross-version reads remain valid.
- **F# file/module ordering & the `Scene` root position**: splitting must not introduce a back-edge
  (a split that creates a circular module dependency won't compile); `Scene`/`SceneCodec` sit near
  the dependency root, so any extracted file must be ordered ahead of its consumers.
- **GL/context absent (CI deterministic tier)**: GL-dependent suites that already skip when no
  window-system surface is available MUST keep skipping with the same rationale — the split does not
  newly require or newly remove a GL context.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The `Viewer` module in `SkiaViewer.fs` MUST be reorganized into clearly named
  responsibility groups (input queue, responsiveness/render-lag, evidence/screenshot, window
  lifecycle) such that no single resulting viewer/host/codec source file exceeds ~1,500 lines
  (guideline target from plan §6, not a hard assertion boundary).
- **FR-002**: The two near-duplicate persistent-window run loops
  (`runPresentedPersistentWindow` / `runPersistentWindow`) MUST be unified behind one shared
  lifecycle scaffold, with their genuinely divergent steps preserved as selectable behavior.
- **FR-003**: `GlHost.run` MUST be decomposed so that GL rendering, input handling, damage/repaint,
  and effect-interpretation/screenshot capture are separated into named units rather than inlined in
  one ~295-line body.
- **FR-004**: `SceneCodec.fs` MUST be split into cohesive responsibility groups (primitives, paint,
  path, text, scene, package).
- **FR-005**: The scene-node serializer MUST be converted from the hand-maintained parallel
  `writeSceneNode`/`readSceneNode` pair into a per-case codec representation in which each node kind's
  write and read are co-located in a single entry, so encode/decode symmetry is enforced by
  construction and adding a node kind is a one-site change.
- **FR-006**: Rendered frames, per-frame/responsiveness traces, screenshot/evidence artifacts, and
  serialized scene-package bytes MUST remain equivalent to the pre-refactor baseline — byte-identical
  where construction guarantees it, semantically equivalent where wording/ordering legitimately
  varies. (No behavioral change; Tier 2.)
- **FR-007**: The public `.fsi` surfaces (`SkiaViewer.fsi`, `OpenGl.fsi`, `SceneCodec.fsi`) and the
  two package surface-area baselines that cover them (`FS.GG.UI.SkiaViewer.txt` — which includes the
  `OpenGl` surface — and `FS.GG.UI.Scene.txt`) MUST remain unchanged, except for internal-only module
  additions (a `module internal` helper not part of the consumed public surface). No version bump.
- **FR-008**: Every affected test suite MUST run with the same red/green result as the pre-refactor
  baseline. No test may be deleted, skipped, or have an assertion weakened to obtain green; tests
  blocked by genuine environment limits keep their existing skip + rationale.
- **FR-009**: Fail-loud behavior MUST be preserved at every refactored site — GL/context creation
  failures, screenshot-before-first-frame, malformed/truncated packages, and unknown node tags MUST
  surface the same diagnostics with no swallowed exceptions or silent degradation.
- **FR-010**: No new project, NuGet dependency, or inter-project reference may be introduced. Work
  stays within `src/SkiaViewer` (and its `Host` folder) and `src/Scene`.
- **FR-011**: A pre-refactor baseline (affected-suite red/green set + reference frames/traces/
  screenshots + serialized-byte corpus + public `.fsi`/surface snapshot) MUST be captured before any
  production edit, and each user story MUST be diffed against it.

### Key Entities *(include if feature involves data)*

- **Window lifecycle scaffold**: the single shared run-loop skeleton that both the presented and
  non-presented persistent-window runners specialize, replacing the two near-duplicate bodies.
- **GlHost responsibility units**: the named rendering / input / damage / effects(+screenshot) units
  carved out of `GlHost.run`.
- **Node codec entry**: the per-node-kind record/representation pairing one write routine with its
  matching read routine (and tag), replacing the hand-aligned `writeSceneNode`/`readSceneNode` arms.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: No `src/SkiaViewer` or `src/Scene` source file involved in this feature exceeds ~1,500
  lines after the split (down from 3,370 / 1,454 / 1,571).
- **SC-002**: The two persistent-window run loops are reduced to **one** shared lifecycle scaffold
  (the duplicate body count for the window run loop goes from 2 → 1).
- **SC-003**: Each scene-node kind is described in **exactly one** codec entry; there are zero node
  kinds whose write side and read side live in physically separate, independently-editable blocks.
- **SC-004**: For the corpus exercised by existing round-trip tests, 100% of scenes/packages
  serialize to identical bytes and round-trip to an equal value before vs. after.
- **SC-005**: The affected test suites (`SkiaViewer`/viewer-host, responsiveness, GL smoke,
  SceneCodec round-trip, package import/export, inspection, and any harness evidence suites that read
  these artifacts) produce a red/green set identical to the captured baseline; no assertion weakened.
- **SC-006**: The two public surface-area baselines covering the `SkiaViewer`/`OpenGl` and `SceneCodec`
  `.fsi` surfaces (`FS.GG.UI.SkiaViewer.txt`, `FS.GG.UI.Scene.txt`) are byte-identical after the
  feature (internal-only `.fsi` additions excepted), with no version bump.
- **SC-007**: Rendered-frame / evidence-artifact equivalence is demonstrated against the pre-refactor
  baseline for each user story (byte-identical or documented semantic equivalence).

## Assumptions

- **Behavior-preserving, surface-stable by default.** Although the campaign's relaxed-constraints
  context (parent report preamble) *permits* surface/behavior changes, this phase's three targets
  are Pattern E (plain extraction) + Pattern A (codec table), which are behavior-preserving by
  construction. The feature therefore stays Tier 2 and keeps the public surface stable — matching how
  Phases 1–2 (features 185, 186) actually executed. This is the default; deviating would require an
  explicit scope change.
- **§7 replacement gates are not a blocking prerequisite for this phase.** The parent report's §7
  golden-image / golden-hash / per-frame perf / semantic-artifact-diff gates exist to protect phases
  whose output *legitimately changes* (Phases 5–6). Because this phase commits to behavior
  preservation, the existing test suites plus byte/semantic-equivalence checks against the captured
  baseline serve as the regression check — consistent with Phases 1–2, which the report explicitly
  states "don't need them." Any split that turns out to force a render-path behavior change is **out
  of scope** here and defers to a later §7-gated phase.
- **Splits land inside the existing projects.** New responsibility groups are internal sub-modules
  and/or additional source files within `src/SkiaViewer` (and `src/SkiaViewer/Host`) and `src/Scene`;
  no new top-level project, package, or inter-project reference is created (FR-010).
- **The ~1,500-line target is a guideline, not a hard gate.** Per plan §6 it is written with "≈";
  the binding outcomes are the structural ones (unified scaffold, single codec entry per node) and
  equivalence, not an exact line count.
- **The current-tree line/location references in this spec (re-confirmed 2026-06-22) may drift**
  before implementation and MUST be re-confirmed at planning/implementation time, per the parent
  report's standing note.
- **Affected test projects**: the viewer/host suites, responsiveness/render-lag lanes, GL smoke
  suites, the `Scene` codec round-trip / package / inspection suites, and any
  `tools/Rendering.Harness` evidence suites that consume viewer screenshots or scene-package
  artifacts. The exact project list is confirmed during `/speckit-plan`.
- **Out of scope**: `Scene.fs` itself (Phase 4), `Control.fs` (Phase 5), `RetainedRender.step`
  (Phase 6), and standing up the §7 gate harnesses as deliverables of this feature.

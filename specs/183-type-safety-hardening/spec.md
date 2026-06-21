# Feature Specification: Type-Safety Hardening (Code-Health Refactoring Phase 6)

**Feature Branch**: `183-type-safety-hardening`

**Created**: 2026-06-21

**Status**: Draft

**Input**: User description: "next item in plan." — resolved to **Phase 6** of the code-health
refactoring plan (`docs/reports/2026-06-21-05-19-code-health-refactoring-analysis-and-plan.md`).
Phases 0–5 (features 177–182) are done and merged. Phase 6 removes the remaining **stringly-typed and
hand-symmetry hazards** so that adding a control kind or a scene node becomes a single-site,
compiler-checked change. Scope confirmed with the maintainer as **all three sub-goals** at **full
appetite** — accepting the public-surface changes (Tier 1) and the package version bumps they imply.

## Context

The codebase is clean of rot but carries a small set of **type-safety hazards** where correctness is
maintained by hand instead of by the compiler. Phases 0–5 removed the high-volume duplication and
brought the god-modules under control; what remains are the places where adding one thing means
remembering to edit several disjoint sites, with no exhaustiveness help:

| # | Hazard | Where | Today's failure mode |
|---|---|---|---|
| 1 | Stringly-typed control `Kind` dispatch | `Control.fs` (matches @502, 1930, 2157, plus `richFamilies`/`chartFamilies`/`prettyKind`), `Inspection.fs`, `Accessibility.fs`, `Catalog.fs`, `ControlRuntime.fs`, `RetainedRender.fs` | ~98 distinct kind string literals dispatched by ~13 parallel `match …Kind` switches across 6 files; the same literals (`"data-grid"`, `"line-chart"`, …) are re-typed in ~11 files. Adding a control kind means editing ~13 disjoint switches with **no compiler exhaustiveness**. |
| 2 | Hand-symmetric `SceneNode` codec | `SceneCodec.fs` (`writeSceneNode`/`readSceneNode`), `Scene.fs` (`SceneNode` DU @391, 25 cases) | Every new `SceneNode` case requires **3 coordinated hand-edits** (DU + writer + reader) with no compiler enforcement — the highest-risk drift point in the codebase. The DU also mixes styles (named-field cases like `Circle of center: Point …` alongside bare-tuple cases like `Rectangle of (float*float*float*float)*Color`), inviting confusion at every match site. |
| 3 | Boolean-trap / long positional flag tails | `validateDamage` (5 bools, `OpenGl.fs`), `classifyWindowObservation` (4 flags — 2 bool + 2 bool option, `SkiaViewer.fs`), `damageRegion` (10 positional args, `Scene.fs`), `promotionDecision` (`RetainedRender.fs`), `damageRegionSet` (`RetainedRender.fs`), `popoverGeom … withActions` (`Control.fs`) | Call sites pass anonymous runs of `true`/`false` with no name at the call; transposing two flags compiles cleanly and silently changes behavior. |

**This is a Tier 1 change — and that is the deliberate difference from Phase 5.** Phase 5 (182) was a
pure internal reorganization that kept every shipped `.fsi` and surface baseline byte-identical. Phase
6 intentionally changes public surface where the type-safe form is the better contract:

- **Sub-goal 1 (Kind registry)** is **internal**: the public `Control.Kind: string` field is unchanged
  and the registry table is private dispatch — no public surface change, no package bump.
- **Sub-goal 2 (codec symmetry)** has an **internal** part (the per-case write/read table, since
  `writeSceneNode`/`readSceneNode` are internal) and a **public** part: normalizing the `SceneNode` DU
  to named fields throughout changes `Scene.fsi` and every match site.
- **Sub-goal 3 (boolean-trap cleanup)** changes signatures: `validateDamage`, `classifyWindowObservation`,
  and `damageRegion` are **public**; `promotionDecision`/`damageRegionSet` are `internal`; `popoverGeom`
  is `private`. Converting the public ones to named flag records is a public surface change.

So the acceptance gate is **not** "surface byte-identical." It is: **runtime behavior stays
byte-stable** (rendered output, codec round-trips, evidence/readiness verdicts, scene
hashes/fingerprints, damage regions, metrics), while **public surface changes are intentional,
minimal, and reviewed** — the surface-baseline diff must reflect exactly the planned signature/DU
changes and nothing more, with the affected `FS.GG.UI.*` package versions bumped and every consumer
(other `src/` projects, samples, the template, downstream generated products) updated to compile.

**Carried lessons from earlier phases.** (a) Phases 3/4 (180/181) showed an abstraction is only worth
it when it removes genuine hazard or duplication; here each sub-goal is justified by **restored
compiler enforcement** (exhaustiveness / symmetry / named-at-call-site), not by line count — net lines
may rise. (b) Behavior byte-stability against a captured baseline remains the correctness gate; only
the *surface* is allowed to move, and only as planned.

## User Scenarios & Testing *(mandatory)*

> Each story is one independently-shippable hardening: it builds, passes the full suite, keeps runtime
> behavior byte-stable, and carries its own (intentional) surface diff + any package bump on its own.
> They may land in any order. Priorities reflect a sensible sequence (highest day-to-day hazard first),
> not a hard dependency chain — none of these depends on another.

### User Story 1 - Single-source control `Kind` registry (Priority: P1)

A contributor adding or changing a control kind wants one registry table — keyed by kind, carrying its
painter, required attributes, pretty name, accessibility role, and layout traits — so the ~13 parallel
`match …Kind` switches across `Control.fs`, `Inspection.fs`, `Accessibility.fs`, `Catalog.fs`,
`ControlRuntime.fs`, and `RetainedRender.fs` collapse to one lookup, and adding a kind is a single table
entry instead of ~13 disjoint edits with no exhaustiveness help.

**Why this priority**: Adding/altering a control kind is the most frequent extension point in the
busiest project, and today it is the most error-prone (silent omission of one of ~10 switches). The
registry is **internal** — the public `Control.Kind: string` surface is unchanged — so it carries the
highest payoff at the lowest surface risk.

**Independent Test**: Build `FS.GG.UI.Controls`, run `Controls.Tests`, and confirm every control kind
produces the same painter output, required-attribute validation, pretty name, a11y role, and layout
traits as baseline (scene-hash / fingerprint / inspection / accessibility outputs byte-identical), with
`Control.fsi` and its surface baseline unchanged.

**Acceptance Scenarios**:

1. **Given** the kind registry, **When** any control of any of the ~98 kinds is laid out, hashed, and
   inspected, **Then** its produced scene, scene-hash, fingerprint, required-attribute diagnostics,
   pretty name, and a11y role are byte-identical to baseline.
2. **Given** the registry replaces the parallel switches, **When** a hypothetical new kind is added in a
   test, **Then** the compiler/registry surfaces every site that must handle it (exhaustiveness
   restored) instead of silently defaulting.
3. **Given** the internal refactor, **When** `FS.GG.UI.Controls` is built, **Then** its public `.fsi`
   surface and surface-area baseline are unchanged and no package bump is required for this story.

---

### User Story 2 - Compiler-enforced `SceneNode` codec symmetry (Priority: P2)

A contributor adding a `SceneNode` case wants the writer and reader driven by one per-case codec table
(so a missing case is a compile error, not a silent runtime drift), and the `SceneNode` DU normalized
to **named fields throughout** so every match site reads consistently — eliminating the repo's
highest-risk "3 coordinated hand-edits per case" hazard.

**Why this priority**: `writeSceneNode`/`readSceneNode` are the single most dangerous drift point — a
desynchronized writer/reader corrupts every persisted/replayed scene silently. The codec table is
internal, but the DU normalization is a **public** `Scene.fsi` change with the widest blast radius
(every `SceneNode` match site in `src/`, samples, template, generated products), so it is sequenced
after the contained Kind work and gated on a full-tree recompile.

**Independent Test**: Build `FS.GG.UI.Scene`, run the Scene suite and the codec round-trip suite, and
confirm every `SceneNode` value serializes to **byte-identical** bytes and round-trips identically to
baseline; confirm `Scene.fsi`'s surface diff contains **only** the planned DU field-naming changes; and
confirm the whole solution (consumers, samples, template) still compiles and passes.

**Acceptance Scenarios**:

1. **Given** the per-case codec table, **When** every `SceneNode` case is written and read back, **Then**
   the wire bytes and the reconstructed value are byte-identical to baseline (no format change).
2. **Given** the normalized DU, **When** a `SceneNode` case is added in a test without a codec-table
   entry, **Then** the build fails (symmetry is compiler-enforced) rather than silently mis-serializing.
3. **Given** the public DU change, **When** `FS.GG.UI.Scene` is built and consumers/samples/template are
   recompiled, **Then** `Scene.fsi`'s surface baseline reflects exactly the planned field-naming changes,
   `FS.GG.UI.Scene` is version-bumped, and every downstream consumer compiles and passes unchanged.

---

### User Story 3 - Named flag records replace boolean traps (Priority: P3)

A contributor calling the multi-flag diagnostic/geometry functions wants the trailing runs of
positional `bool`s replaced by small named flag records, so each flag is named at the call site and
transposing two flags becomes a compile error instead of a silent behavior change — for `validateDamage`,
`classifyWindowObservation`, `damageRegion`, `promotionDecision`, `damageRegionSet`, and `popoverGeom`.

**Why this priority**: The most mechanical of the three and the smallest per-function blast radius, but
it touches multiple packages and changes **public** signatures (`validateDamage` in `OpenGl.fsi`,
`classifyWindowObservation` in `SkiaViewer.fsi`, `damageRegion` in `Scene.fsi`), so it carries package
bumps and is sequenced last.

**Independent Test**: Build the affected packages (`FS.GG.UI.SkiaViewer`, `FS.GG.UI.Scene`,
`FS.GG.UI.Controls`), run their suites and the damage/diagnostics lanes, and confirm every diagnostic
verdict, damage region, and promotion decision is byte-identical to baseline; confirm each surface diff
contains only the planned signature change.

**Acceptance Scenarios**:

1. **Given** the named flag records, **When** each converted function runs with the same inputs as
   baseline, **Then** its result (damage validation verdict, window observation, damage region,
   promotion decision) is byte-identical to baseline.
2. **Given** a call site, **When** two flags would be transposed, **Then** the named-field form makes the
   intent explicit and an accidental swap is caught at the call (no silent inversion).
3. **Given** the public signature changes, **When** the affected packages are built, **Then** their
   surface baselines reflect exactly the planned flag-record signatures, the affected `FS.GG.UI.*`
   packages are version-bumped, and all consumers/samples/template compile and pass.

---

### Edge Cases

- **Cascading consumer recompiles (DU change).** Normalizing the `SceneNode` DU touches every match site
  across `src/`, samples, the template, and downstream generated products. Any site missed is a compile
  error (good) — but the story is only done when the **whole tree** (including release-only / sample /
  generated-product lanes) recompiles and passes, not just the owning package.
- **Codec wire-format stability.** Renaming DU fields and table-driving the codec MUST NOT change the
  serialized byte layout: a round-trip of any pre-existing scene must produce identical bytes and an
  identical value. A format change is out of scope and would break replay/persisted caches.
- **Registry behavioral equivalence.** The kind registry must reproduce the exact painter, required
  attributes, pretty name, a11y role, and layout traits the parallel switches produced for every one of
  the ~98 kinds — including any current fallthrough/default behavior for unknown kinds. A kind that the
  old switches handled by omission (implicit default) must keep that exact behavior.
- **Internal vs public boolean-traps.** `popoverGeom` is `private` and `promotionDecision`/
  `damageRegionSet` are `internal` (no public bump); `validateDamage`/`classifyWindowObservation`/
  `damageRegion` are public (bump + baseline update). The two classes must be handled accordingly — an
  internal change must NOT alter a public baseline, and a public change must be reflected in exactly one
  planned baseline diff.
- **Surface diff must be exact.** Every intentional surface change must appear in the baseline diff and
  nothing else may. An accidental extra promotion/relocation of a symbol is a defect, caught by the
  surface-drift check, even though this is a Tier 1 feature.
- **Package version bump fan-out.** Bumping `FS.GG.UI.Scene` / `FS.GG.UI.SkiaViewer` (and any other
  affected package) requires aligning dependent package versions, the local feed, samples, and the
  template so the solution restores to a consistent, packable state (per the established merge flow).
- **No behavior change is the contract.** Despite being Tier 1, runtime behavior is byte-stable: any
  observed output difference (rendered scene, codec bytes, evidence verdict, metric) means the refactor
  changed semantics and must be corrected, not baselined forward.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The ~13 parallel `match …Kind` switches across `Control.fs`, `Inspection.fs`,
  `Accessibility.fs`, `Catalog.fs`, `ControlRuntime.fs`, and `RetainedRender.fs` MUST be collapsed into
  one internal kind-registry table (kind → painter, required attributes, pretty name, a11y role, layout
  traits) that reproduces the current behavior for every one of the ~98 kinds, including existing
  default/fallthrough behavior.
- **FR-002**: The `SceneNode` write/read codec MUST be driven by a single per-case table (or equivalent)
  such that adding a `SceneNode` case without its codec entry is a **compile-time** error, eliminating
  the silent writer/reader drift hazard.
- **FR-003**: The `SceneNode` DU MUST be normalized to named fields throughout, applied consistently at
  every match and construction site across the solution (including samples, template, and generated
  products), with the public `Scene.fsi` updated accordingly.
- **FR-004**: The boolean-trap / long positional-flag tails of `validateDamage`,
  `classifyWindowObservation`, `damageRegion`, `promotionDecision`, `damageRegionSet`, and `popoverGeom`
  MUST be replaced with small named flag records so each flag is named at the call site.
- **FR-005**: Runtime behavior MUST be byte-stable against a baseline captured immediately before the
  change: rendered output, `SceneNode` codec wire bytes and round-trip values, evidence/readiness
  verdicts (Markdown + JSON), viewer observations, scene hashes/fingerprints, damage regions, and metrics
  are byte-identical for every touched subsystem. No assertion may be weakened to green a build.
- **FR-006**: Every public-surface change MUST be **intentional and minimal**: each affected package's
  surface-area baseline diff MUST contain exactly the planned signature/DU changes and nothing else (no
  accidental symbol promotion, hiding, or relocation). Internal-only changes (Kind registry; internal
  codec table; `internal`/`private` flag functions) MUST leave the public baseline unchanged.
- **FR-007**: Every `FS.GG.UI.*` package whose **public** surface changes (at minimum `FS.GG.UI.Scene`
  and `FS.GG.UI.SkiaViewer`; others if their public `.fsi` changes) MUST be version-bumped, with
  dependent package versions, the local feed, samples, and the template aligned so the solution restores
  to a consistent, packable state.
- **FR-008**: The full solution — every consumer in `src/`, all samples, the template, and downstream
  generated-product lanes — MUST recompile and pass after the DU and signature changes; the feature is
  not done while any lane is left red by an un-updated match/call site.
- **FR-009**: `dotnet build` and `dotnet test` (full Release sweep under `DISPLAY=:1`) MUST be green at
  the end of each story, with pre-existing baseline reds (known `Package.Tests` / `ControlsGallery`
  package-feed reds) unchanged and not regressed.
- **FR-010**: Any sub-goal (or part of one) found to change runtime behavior, require a codec wire-format
  change, or demand a new project/dependency/back-edge MUST be left in its current form and the exclusion
  recorded with its rationale, rather than forcing it.
- **FR-011**: The change MUST introduce no new project, no new package dependency, and no new
  inter-project reference; the dependency graph stays acyclic and unchanged. (Surface and versions may
  change; the project/reference graph may not.)

### Key Entities *(include if feature involves data)*

- **Kind registry**: an internal table keyed by control kind carrying `{ painter; requiredAttrs;
  prettyName; a11yRole; layoutTraits }` (and any other per-kind trait the switches currently encode),
  the single source of truth replacing the parallel `match …Kind` switches.
- **Codec case table**: a per-`SceneNode`-case mapping of writer/reader pairs that makes the
  write/read symmetry exhaustive and compiler-enforced, over the unchanged wire format.
- **`SceneNode` (normalized)**: the 25-case scene DU restated with named fields on every case; a public
  `Scene.fsi` entity whose change fans out to every match/construction site.
- **Named flag records**: small records replacing the positional `bool` tails of the six trap functions
  (e.g. a `DamageValidationFlags`-style record), naming each flag at the call site.
- **Surface-area baseline**: the per-package public-surface snapshot; here it is *expected to change*
  for the affected packages, and the diff is the reviewed record that the change was exactly as planned
  (FR-006).
- **Behavior baseline**: the pre-change capture of rendered output, codec bytes/round-trips,
  evidence/readiness artifacts, scene hashes, damage regions, and the full test red/green set — the
  byte-stable correctness gate (FR-005).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Adding a new control kind requires editing **exactly one** site (the registry table); the
  parallel `match …Kind` switches no longer exist as independent edit points (verified by a test that a
  new kind is surfaced exhaustively rather than silently defaulted).
- **SC-002**: Adding a new `SceneNode` case without its codec entry is a **compile error** (demonstrated
  by a deliberate omission failing the build), and the `SceneNode` DU presents named fields on 100% of
  its cases.
- **SC-003**: 100% of the six boolean-trap functions take named flag records; no converted function
  retains a trailing run of unnamed positional `bool` parameters.
- **SC-004**: Runtime behavior is byte-stable across all three sub-goals: rendered output, `SceneNode`
  codec wire bytes + round-trip values, evidence/readiness artifacts, scene hashes/fingerprints, damage
  regions, and metrics are byte-identical to baseline.
- **SC-005**: Each affected package's surface-area baseline diff contains **only** the planned changes
  (zero accidental surface drift); internal-only stories leave the public baseline unchanged.
- **SC-006**: The full solution (consumers, samples, template, generated-product lanes) builds and
  `dotnet test` passes at the end of each story and at phase end, with the known pre-existing baseline
  reds unchanged.
- **SC-007**: Affected `FS.GG.UI.*` packages are version-bumped and the feed/samples/template are aligned
  so the solution is restored to a consistent, packable state.
- **SC-008**: No new project, package dependency, or inter-project reference is introduced; the
  dependency graph remains acyclic and unchanged.

## Assumptions

- "Next item in plan" resolves to **Phase 6** of the code-health refactoring plan; Phases 0–5 are done
  and merged (features 177–182), per project memory and recent commit history.
- The maintainer chose **full Phase 6 appetite** (confirmed via clarification): all three sub-goals,
  accepting the public-surface changes (Tier 1) and the package version bumps they imply — in contrast
  to Phases 0–5, which were kept Tier 2 / byte-stable wherever a surface change threatened.
- The kind registry is an **internal** dispatch table; the public `Control.Kind: string` field is
  unchanged, so Story 1 needs no package bump unless an unrelated public symbol is touched.
- The `SceneNode` codec wire format is **frozen**: normalizing DU field names and table-driving the
  codec must not change the serialized bytes (replay/persisted caches depend on it).
- The publicly-affected packages are at least `FS.GG.UI.Scene` (DU + `damageRegion`) and
  `FS.GG.UI.SkiaViewer` (`validateDamage`, `classifyWindowObservation`); `FS.GG.UI.Controls` changes are
  internal (registry; `internal` codec/flag functions) and bump only if a public `.fsi` actually moves.
  The authoritative bump set is whatever the surface-drift check reports.
- Behavior byte-stability against a captured baseline is the correctness gate; the surface baselines are
  *expected* to change and their diffs are the reviewed record of intent (not a failure).
- Line counts and call-site locations cited here are at HEAD (re-confirmed by grep); they differ
  slightly from the original report because the tree moved through Phase 5 (182). Authoritative scope is
  the code at HEAD.

## Out of Scope

- Changing the `SceneNode` codec **wire format**, or any persisted/replay cache representation — symmetry
  and field naming only, over the frozen format.
- Changing the **meaning** of any control kind, scene node, diagnostic verdict, damage region, or metric
  — this is a representation/typing change, not a behavior change.
- Introducing the kind registry as a **public** API surface (e.g. exposing the table for external
  registration); it is internal dispatch only for this feature.
- Converting the `Control.Kind` field itself from `string` to a closed DU (a larger, surface-breaking
  redesign); the registry keys off the existing string kind.
- Phase 5 god-module splits (feature 182, already merged) and any further structural file splitting.
- New abstractions whose only justification is line reduction; each sub-goal here is justified by
  restored compiler enforcement, not net lines.

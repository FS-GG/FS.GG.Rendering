# Feature Specification: Control.fs / ControlInternals Decomposition (Patterns A+E, kind registry)

**Feature Branch**: `189-control-module-split`

**Created**: 2026-06-22

**Status**: Draft

**Input**: User description: "next item in the plan."

> **Plan context.** This is **Phase 5** of the god-module decomposition campaign
> (`docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md` §4.2 / §6). Phase 1
> (harness data-table refactor, feature 185), Phase 2 (cross-cutting dedup + state records,
> feature 186), Phase 3 (viewer/host/codec splits, feature 187 — partial), and Phase 4 (`Scene.fs`
> split, feature 188) are shipped. Phase 5 decomposes `src/Controls/Control.fs` — a flat
> ~3,513-line file whose `module internal ControlInternals` alone is ~2,942 lines — by responsibility
> (Pattern E) **and** routes the per-kind dispatch (the `faithfulContent` painter and the parallel
> `match …Kind` sites) through the already-built `ControlKindRegistry` (Pattern A). Like Phase 4 it
> **takes the maintainer-granted relaxed-constraints latitude**: it is permitted to make a **reviewed
> public-surface change** (carrying a version bump **iff** the regenerated surface baseline diff is
> non-empty) and to **reorder hash/fingerprint computation** where the registry/visitor restructuring
> makes that unavoidable — validated by the §7 *golden-hash review gate* + existing suites, not by
> blind byte-equality. Per the user's scope decision it does **not** stand up a new golden-image
> harness; it reuses the existing test suites + `hashScene`/fingerprint byte-equality + semantic
> artifact diff as the regression gate (consistent with how Phases 1–4 actually shipped).

> **Prior-art note — the metadata registry already exists.** Feature 183 built
> `src/Controls/ControlKindRegistry.fs` (`module internal ControlKindRegistry`): a per-kind metadata
> table (`IsRich`/`IsChart`/`ChartSource`/`LayoutRow`/`HasScrollAffordance`/`Virtualization`/
> `InspectionNodeKind`/`SurfaceRole`/`A11yRole`) that several call sites already read. It deliberately
> **retained the painter and required-attribute validation at their original sites** (its own FR-010
> retention). Phase 5's Pattern-A work is therefore an **extension** of that existing registry to also
> own the kind→geometry painter dispatch and to absorb the remaining disjoint `match …Kind` switches —
> not a greenfield registry.

> **Terminology / naming notes (disambiguation).**
> - **`ControlInternals.SceneHash` / `.ContentRender` / `.ChartGeometry` / `.WidgetGeometry` /
>   `.LayoutEval` / `.NodeAssembly`** name the *responsibility-scoped file groupings* the giant
>   `module internal ControlInternals` is split into. Because `ControlInternals` is `module internal`,
>   the chosen mechanism (planning decision) — sibling internal sub-modules and/or
>   `[<CompilationRepresentation(ModuleSuffix)>]` on companion modules, the FS0250 lever proven in
>   feature 188 — keeps these out of the **public** package surface. Read "extract into `SceneHash`"
>   as "move into the `SceneHash` grouping/file", not "introduce a new public module".
> - **"the 30 trivial tail modules"** = the public thin constructors at the file tail
>   (`TextBlock`/`Label`/`Image`/…/`Overlay`, currently ~L3358–3511+). `Control.Helpers` collapses
>   their *duplicated bodies* behind a shared data-driven helper **while preserving each public
>   module's surface** (thin delegations) — surface-neutral by construction, mirroring 188's
>   `Text.Shaping` public delegations.

## Change Classification

**Tier 1 (surface-changing-conditional) + behavior-sensitive (hash/fingerprint).** `Control.fs` carries
the public `Control.fsi` (~687 lines) plus the 30 public tail-constructor modules; most decomposition
targets (`hashScene`, `faithfulContent`, the ~40 `*Geom` functions, `toLayout`/`evaluateLayout`/
`paintNode`) are **`private` members inside `module internal ControlInternals`**, so moving them into
sibling internal groupings is *largely* surface-neutral to the published `FS.GG.UI.Controls` baseline.
**Whether a package version bump is required is therefore conditional, gated on the regenerated surface
baseline** (same rule as feature 188): bump **iff** the regenerated, reviewed
`FS.GG.UI.Controls` surface-area baseline diff is non-empty; an empty diff ⇒ no bump and the phase is
satisfied by "zero incidental drift." Separately, routing `hashScene` through a visitor and
`faithfulContent` through the registry **may reorder scene-hash / fingerprint computation**; rendered
*scenes* must stay equivalent, but hash bytes that legitimately change are gated by the §7
**golden-hash review** discipline (explicit "hashes changed, reviewed" sign-off) — not asserted
byte-identical. The campaign's standing target remains byte-identical output where construction
guarantees it; only where the Pattern-A restructuring genuinely reorders computation does the review
gate apply. No new golden-image harness is built (user scope decision); the existing
`Controls`/`RetainedRender`/inspection suites + semantic artifact diff are the regression gate.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Extract chart + widget geometry into `ChartGeometry`/`WidgetGeometry`; factor the shared preamble (Priority: P1)

A maintainer touching a chart or widget today scrolls through ~40 `private *Geom` functions
(`lineGeom`, `barGeom`, `pieGeom`, `scatterGeom`, `graphGeom`, `areaGeom`, `columnGeom`,
`histogramGeom`, `boxPlotGeom`, `heatmapGeom`, `radarGeom`, …, plus the widget set
`buttonGeom`/`switchGeom`/`checkboxGeom`/`toggleGeom`/`sliderGeom`/`tabsGeom`/…) packed mid-file inside
`ControlInternals`, ~17 of which repeat the same `match pts with [] -> emptyState theme box caption`
guard preamble. After this story the chart-geometry functions live in a `ChartGeometry` grouping and
the widget-geometry functions in a `WidgetGeometry` grouping (each in its own file), and the repeated
empty-points/empty-state preamble is factored into one `withPoints` combinator. Every produced `Scene
list` is **byte-identical** to the pre-refactor geometry — this is a pure relocation + shared-skeleton
collapse with no behavior change.

**Why this priority**: This is the single largest line-count block in `ControlInternals` (~1,000+
lines of geometry) and the lowest-risk extraction — the functions are leaf producers of `Scene list`
with no hidden mutable state. Doing it first establishes the new file floor every later story builds
on (the `faithfulContent` registry routing in US3 dispatches *to* these functions), and delivers the
biggest standalone size reduction. The `withPoints` collapse is constrained by the carry-forward
lesson (feature 180/181): only the genuinely-shared guard skeleton is collapsed; divergent bodies stay.

**Independent Test**: Build the full `Controls` solution and run the controls / faithful-content /
chart suites; confirm every chart and widget renders a `Scene list` byte-identical to the captured
baseline, and that `Control.fs`/`ControlInternals` shrinks by the relocated geometry while the
`FS.GG.UI.Controls` surface baseline is unchanged (geometry is internal).

**Acceptance Scenarios**:

1. **Given** the pre-refactor build, **When** the chart/widget `*Geom` functions are moved into the
   `ChartGeometry`/`WidgetGeometry` groupings and the shared preamble is factored into `withPoints`,
   **Then** the whole `Controls` solution compiles with no back-edge/ordering error and the public
   surface baseline is unchanged.
2. **Given** the corpus of chart and widget controls exercised by existing tests, **When** each is
   rendered after the extraction, **Then** the produced `Scene list` (and any downstream fingerprint)
   is byte-identical to the pre-refactor output.
3. **Given** a chart with an empty point list, **When** it is rendered through the `withPoints`
   combinator, **Then** it produces exactly the same empty-state scene as the pre-refactor per-function
   `match pts with [] -> emptyState` guard (the combinator changes structure, not output).

---

### User Story 2 - Extract `SceneHash`, `LayoutEval`, and `NodeAssembly` into responsibility files (Priority: P2)

A maintainer working on hashing or layout/paint today faces `hashScene` (~381 lines, a 25-case
`goNode`/`goScene` walk with inline mixer closures, ~L2405–2853), the layout evaluators
(`toLayout`/`evaluateLayout`/`evaluateLayoutIncremental`, ~L2163–2297), and the assembly functions
(`paintLeaf`/`paintNode`/`renderScene`, ~L2036–2404) all interleaved in the same module. After this
story `hashScene` lives in a `SceneHash` grouping (recast as a `SceneHasher` visitor over `SceneNode`,
Pattern A), the layout evaluators in `LayoutEval`, and the assembly functions in `NodeAssembly`.
`renderScene`/`paintNode`/`evaluateLayout` keep their existing internal names and call shapes so
`Control`, `ControlRuntime`, and `RetainedRender` reference them unchanged.

**Why this priority**: `hashScene` is the second-worst function in the file and the one most entangled
with the hot path (`RetainedRender` picture-cache / fingerprint), so it is sequenced after the safe
geometry move but before the behavior-sensitive registry routing. Recasting it as a visitor is the
Pattern-A change the report calls out; it reorders nothing intentionally but must be proven equal on
golden hashes. Pulling `LayoutEval`/`NodeAssembly` out alongside leaves `ControlInternals` as the
content/registry core for US3.

**Independent Test**: Run the controls / retained-render / hash-fingerprint / layout suites; confirm
`hashScene` output is byte-identical for the scene corpus (golden-hash equality), `evaluateLayout`
bounds are byte-identical (INV-1), and `paintNode`/`renderScene` produce identical scenes — or, where
the visitor recast legitimately reorders a hash, that the changed hashes are captured under an explicit
reviewed "hashes changed, reviewed" sign-off rather than silently asserted.

**Acceptance Scenarios**:

1. **Given** the scene corpus exercised by existing hash/fingerprint tests, **When** `hashScene` is
   recast as a `SceneHasher` visitor in `SceneHash`, **Then** the 64-bit hash for every scene is
   byte-identical to the pre-refactor value — or any change is limited to the reviewed-and-approved
   golden-hash delta and breaks no `RetainedRender` picture-cache invariant.
2. **Given** the layout corpus, **When** `toLayout`/`evaluateLayout`/`evaluateLayoutIncremental` are
   moved into `LayoutEval`, **Then** the returned `root`/`boundsById` are byte-identical to a full
   pre-refactor `evaluateLayout` (INV-1 preserved) with no ordering/back-edge error.
3. **Given** the control corpus, **When** `paintLeaf`/`paintNode`/`renderScene` are moved into
   `NodeAssembly`, **Then** the assembled `Scene` for every control is equivalent to the pre-refactor
   output and all internal callers resolve the moved functions unchanged.

---

### User Story 3 - Route `faithfulContent` + the 6 parallel `match …Kind` sites through the extended `ControlKindRegistry` (Priority: P3)

A maintainer adding or changing a control kind today must edit `faithfulContent` (~168 lines, 60+ kind
branches each dispatching to a `*Geom`, ~L1868–1990) **and** keep ~6 other disjoint `match …Kind`
switches in sync across `Control.fs` (×2), `ControlRuntime.fs`, `Catalog.fs`, `Inspection.fs`, and
`RetainedRender.fs` — a known silent-drift hazard (a kind added to one switch but forgotten in another
loses exhaustiveness). After this story `faithfulContent`'s kind→geometry dispatch is expressed as a
per-kind painter in the extended `ControlKindRegistry` (the existing `ControlKindEntry` gains a painter
field, e.g. `Painter: Theme -> Rect -> Control -> Scene list`), and the remaining parallel `match …Kind`
sites read the single registry table, so adding a control kind becomes a **one-site, compiler-checked**
change and the catalog↔registry completeness oracle (feature 183 SC-001) covers the painter too.

**Why this priority**: This is the Pattern-A core the user explicitly scoped in ("Full Patterns A+E")
and the highest-value change (it kills the cross-file duplication that causes real exhaustiveness bugs),
but also the most behavior-sensitive — collapsing 60+ branches into a table can reorder dispatch and,
through it, hash/fingerprint output. It is sequenced after the geometry (US1, its dispatch targets) and
hash (US2) extractions so it routes into already-stable groupings, and it is gated by the golden-hash
review + the registry-completeness oracle.

**Independent Test**: Run the controls / faithful-content / inspection / catalog-completeness suites;
confirm every catalog kind renders the same faithful geometry through the registry painter as through
the pre-refactor `match`, the 6 former `match …Kind` sites now resolve via the table with identical
results, and the catalog↔registry completeness oracle fails loudly if any kind lacks a painter entry.

**Acceptance Scenarios**:

1. **Given** every kind in the control catalog, **When** its faithful content is produced through the
   registry painter, **Then** the resulting `Scene list` is equivalent to the pre-refactor
   `faithfulContent` branch for that kind (byte-identical where the dispatch order is preserved;
   otherwise within the reviewed golden-hash delta).
2. **Given** the 6 parallel `match …Kind` sites (`Control.fs` ×2, `ControlRuntime.fs`, `Catalog.fs`,
   `Inspection.fs`, `RetainedRender.fs`), **When** each is routed through the single registry table,
   **Then** each produces the same result it produced from its own `match`, and the disjoint switches
   are eliminated.
3. **Given** a control kind present in the catalog but missing a registry painter entry, **When** the
   build/completeness oracle runs, **Then** it fails loudly (no kind silently falls through to a wrong
   or empty painter) — exhaustiveness is restored, not weakened.

---

### User Story 4 - Collapse the 30 trivial tail-constructor module bodies behind `Control.Helpers` (Priority: P4)

A maintainer adding a simple control today copies one of ~30 near-identical public tail modules
(`TextBlock`/`Label`/`Image`/`Icon`/`Separator`/`Badge`/`Button`/…/`Overlay`, ~L3358–3511+), each a
thin `create`/`text` wrapper differing only by kind string and a couple of defaults. After this story
their **duplicated bodies** are collapsed behind a shared data-driven `Control.Helpers` routine while
**each public module keeps its exact surface** (the public `create`/`text` entry points become thin
delegations) — so the published `FS.GG.UI.Controls` surface is unchanged and consumers/templates need
no edits.

**Why this priority**: Lowest individual payoff and the only slice touching public tail modules, so it
is sequenced last and held to the strictest "surface-neutral" bar. It is explicitly subject to the
carry-forward lesson (feature 180 SC-005 / 181): a record-of-functions abstraction can *increase* line
count and indirection — so this slice ships **only if** it nets a real reduction while keeping every
public name; otherwise it is dropped without blocking US1–US3.

**Independent Test**: Build the full solution and run the controls construction suites; confirm every
public tail-module entry point (`TextBlock.text`, `Button.create`, …) produces an identical `Control`
value to the pre-refactor body, the `FS.GG.UI.Controls` surface baseline is unchanged, and the file is
net-smaller (else the slice is dropped).

**Acceptance Scenarios**:

1. **Given** each of the ~30 public tail modules, **When** its body is delegated to `Control.Helpers`,
   **Then** the public module/function names and signatures are unchanged (empty surface-baseline diff
   for these modules) and each entry point yields a `Control` value identical to the pre-refactor one.
2. **Given** the collapse is applied, **When** line counts are compared, **Then** the change nets a
   real reduction in `Control.fs`; if it does not (indirection ≥ duplication removed), the slice is
   **not** shipped and US1–US3 stand on their own.

---

### Edge Cases

- **F# file/module ordering & internal back-edge.** `ControlInternals` members reference each other
  (geometry ← `faithfulContent` ← `paintLeaf`/`renderScene`; `hashScene` consumed by `RetainedRender`).
  Every extracted grouping MUST be ordered so producers precede consumers in `Controls.fsproj`; a split
  that creates a circular module dependency won't compile. The `module internal` span constraint (a
  non-namespace module cannot span files) is handled by the planning-chosen mechanism (sibling internal
  modules and/or the feature-188 `[<CompilationRepresentation(ModuleSuffix)>]` lever).
- **Hash/fingerprint reorder vs. `RetainedRender` picture-cache.** Recasting `hashScene` as a visitor
  and `faithfulContent` as a table MUST NOT break the picture-cache/fingerprint invariants in
  `RetainedRender`; any hash change is intentional, reviewed (golden-hash gate), and proven not to cause
  spurious cache misses or visual drift.
- **Registry exhaustiveness / unknown runtime kind.** A catalog kind missing a painter entry MUST fail
  the completeness oracle (build/test), and a non-catalog runtime kind MUST fall back to the same
  default the pre-refactor `match` used — never to a silently wrong painter.
- **Public tail-module surface preservation.** `Control.Helpers` MUST preserve every public tail
  module's names/signatures; no public `create`/`text` entry point may be renamed, dropped, or have its
  type changed (the conditional version-bump gate covers any drift that does arise).
- **Float / dispatch-order on render-adjacent paths.** Geometry, painting, and the registry painter
  MUST preserve the existing order of numeric/float computation so rendered scenes stay equivalent for
  unchanged controls (byte-identical where construction guarantees it).
- **Required-attribute validation retained.** The `required kind` / attribute-validation behavior that
  feature 183 deliberately kept at its original site MUST keep firing identically after the split (it is
  not silently absorbed or skipped by the painter routing).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The ~40 chart/widget `*Geom` functions MUST be extracted out of `ControlInternals` into
  `ChartGeometry` and `WidgetGeometry` responsibility groupings (own files), each producing `Scene
  list` byte-identical to the pre-refactor function.
- **FR-002**: The repeated empty-points guard preamble (`match pts with [] -> emptyState …`, ~17 sites)
  MUST be factored into one shared `withPoints` combinator, collapsing only the genuinely-shared
  skeleton (divergent bodies retained per the feature-180/181 lesson).
- **FR-003**: `hashScene` MUST be extracted into a `SceneHash` grouping, recast as a `SceneHasher`
  visitor over `SceneNode`, producing a hash that is byte-identical to the pre-refactor value for the
  scene corpus — or, where the recast legitimately reorders computation, a hash change captured under
  the §7 golden-hash review gate.
- **FR-004**: The layout evaluators (`toLayout`/`evaluateLayout`/`evaluateLayoutIncremental`) MUST be
  extracted into a `LayoutEval` grouping with `root`/`boundsById` byte-identical to a pre-refactor full
  `evaluateLayout` (INV-1), and the assembly functions (`paintLeaf`/`paintNode`/`renderScene`) into a
  `NodeAssembly` grouping with equivalent assembled scenes.
- **FR-005**: `faithfulContent`'s kind→geometry dispatch MUST be expressed as a per-kind painter on an
  **extended `ControlKindRegistry`** (the existing `ControlKindEntry` gains a painter field), routing
  each catalog kind to the same geometry it produced via the pre-refactor `match`.
- **FR-006**: The ~6 parallel `match …Kind` sites (`Control.fs` ×2, `ControlRuntime.fs`, `Catalog.fs`,
  `Inspection.fs`, `RetainedRender.fs`) MUST be routed through the single registry table, eliminating
  the disjoint switches and restoring compiler/oracle-checked exhaustiveness (one-site kind addition).
- **FR-007**: The catalog↔registry completeness oracle (feature 183 SC-001) MUST be extended to cover
  the painter dispatch, failing loudly if any catalog kind lacks a painter entry; non-catalog runtime
  kinds MUST fall back to the same default the pre-refactor dispatch used.
- **FR-008**: The 30 trivial public tail modules MAY have their duplicated bodies collapsed behind a
  data-driven `Control.Helpers` routine, but ONLY while preserving every public module's names and
  signatures (thin public delegations) AND only if it nets a real line reduction; if it does not, the
  slice MUST be dropped without blocking the other stories.
- **FR-009**: Rendered scenes for unchanged controls MUST remain equivalent to the pre-refactor
  baseline (byte-identical where construction guarantees it). Inspection/evidence artifacts MUST remain
  **semantically** equivalent; any hash/fingerprint change MUST be limited to the reviewed golden-hash
  delta.
- **FR-010**: The public-surface change MUST be limited to whatever the registry-routing / module-move
  unavoidably exposes. The regenerated `FS.GG.UI.Controls` surface-area baseline MUST reflect exactly
  the intended changes and no incidental drift; the package version MUST be bumped **iff** that
  regenerated, reviewed baseline diff is non-empty (empty diff ⇒ no bump).
- **FR-011**: Every affected test suite MUST run with the same red/green result as the pre-refactor
  baseline, except for tests whose expected output is intentionally updated for a reviewed golden-hash
  delta (those updates MUST be explicit, reviewed, and recorded). No test may be deleted, skipped, or
  have an assertion weakened to obtain green; tests blocked by genuine environment limits keep their
  existing skip + rationale.
- **FR-012**: Fail-loud behavior MUST be preserved at every refactored site — malformed/degenerate
  controls, unknown kinds, missing painter entries, and required-attribute violations MUST still
  surface the same diagnostics with no swallowed exceptions or silent degradation.
- **FR-013**: No new project, NuGet dependency, or inter-project reference may be introduced. Work
  stays within `src/Controls` plus the consumer/template/baseline updates (if any) that a non-empty
  surface diff requires.
- **FR-014**: A pre-refactor baseline (affected-suite red/green set + reference scene-hashes/
  fingerprints + faithful-content/inspection artifact corpus + public `.fsi`/surface snapshot) MUST be
  captured before any production edit; each story MUST be diffed against it, and any golden-hash delta
  MUST be reviewed and approved against that baseline before it is treated as expected.
- **FR-015**: The phase MUST be validated by the existing test suites + `hashScene`/fingerprint
  byte-equality + semantic artifact diff + the §7 golden-hash review gate. Standing up a new
  golden-image equivalence harness or per-frame perf-budget lane is **out of scope** (deferred
  follow-up — the report's "build §7 gates early" recommendation, which Phases 1–4 also deferred).

### Key Entities *(include if feature involves data)*

- **`ControlKindRegistry` (extended)**: the existing internal per-kind dispatch table (feature 183),
  extended so each `ControlKindEntry` also carries the **painter** (`Theme -> Rect -> Control -> Scene
  list`) that `faithfulContent` formerly selected via `match`; remains the catalog↔registry
  completeness oracle and the single source the parallel `match …Kind` sites read.
- **`SceneHasher` visitor**: the recast of `hashScene` — a structured visitor over `SceneNode`
  replacing the 25-case inline `goNode`/`goScene` walk, producing the same 64-bit hash.
- **`withPoints` combinator**: the shared empty-points/empty-state guard factored out of ~17 chart
  `*Geom` functions.
- **Responsibility groupings**: `ChartGeometry`, `WidgetGeometry`, `SceneHash`, `LayoutEval`,
  `NodeAssembly`, `ContentRender` (the registry painter wiring), and `Control.Helpers` — the
  internal file/module groupings `ControlInternals` is split into.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `src/Controls/Control.fs` (and the `module internal ControlInternals` within it) no
  longer contains the chart/widget geometry block, `hashScene`, the layout/assembly functions, or the
  `faithfulContent` kind dispatch as inline `match`; the resulting `Control.fs` and each extracted file
  are at or below the ~1,500-line guideline (down from ~3,513 — "≈" guideline, not a hard assertion).
- **SC-002**: The ~17 repeated chart empty-points preambles are reduced to **one** `withPoints`
  combinator, and every chart/widget `*Geom` produces a byte-identical `Scene list`.
- **SC-003**: `faithfulContent`'s 60+ inline kind branches and the ~6 parallel `match …Kind` sites are
  reduced to **one** registry table read each (the count of disjoint kind-dispatch `match` sites for
  faithful geometry / kind metadata goes from ~7 to effectively 1 — the registry), and adding a control
  kind is a one-site, oracle-checked change.
- **SC-004**: For the corpus exercised by existing tests, scenes for unchanged controls render to
  equivalent `Scene list`s and `evaluateLayout` bounds are byte-identical (INV-1); `hashScene` output
  is byte-identical except for explicitly reviewed-and-approved golden-hash deltas.
- **SC-005**: The affected test suites (controls construction/faithful-content, retained-render/
  hash-fingerprint, layout, inspection/catalog-completeness, plus any consumer/harness suites reading
  these artifacts) produce a red/green set identical to the captured baseline, except for the explicitly
  reviewed golden-hash expected-output updates; no assertion weakened.
- **SC-006**: The regenerated `FS.GG.UI.Controls` surface-area baseline differs from the prior baseline
  **only** by intended, reviewed-and-approved changes (target: empty diff, since the targets are
  internal and the tail modules keep their public surface), and the package version is bumped **iff**
  that diff is non-empty (empty diff ⇒ no bump).
- **SC-007**: The catalog↔registry completeness oracle covers the painter dispatch — there exists a
  test that fails if any catalog kind lacks a painter entry (exhaustiveness is machine-enforced, not
  convention).
- **SC-008**: Any golden-hash delta is captured as an approved review record demonstrating the change
  is an intentional computation reorder (not a regression) and breaks no `RetainedRender` picture-cache
  invariant.

## Assumptions

- **This phase takes the relaxed-constraints latitude (Full Patterns A+E).** Per the user's scope
  decision, Phase 5 exercises the maintainer-granted relaxation: it routes `faithfulContent` + the 6
  `match …Kind` sites through the extended registry (Pattern A), accepts a reviewed public-surface
  change with a conditional version bump, and accepts reviewed hash/fingerprint reorders — it is **not**
  limited to a pure surface-neutral file move.
- **§7 gates are applied as a verification method, not built as a deliverable.** Per the user's scope
  decision, this phase reuses the existing test suites + `hashScene`/fingerprint byte-equality +
  semantic artifact diff + the golden-hash *review* gate as its regression check. Standing up a new
  golden-image equivalence harness or per-frame perf-budget lane is **out of scope** and tracked as a
  separate follow-up (consistent with how Phases 1–4 shipped).
- **The metadata `ControlKindRegistry` already exists and is extended, not rebuilt.** Feature 183's
  `module internal ControlKindRegistry` is the foundation; Phase 5 adds the painter field + completeness
  coverage and routes the remaining switches to it.
- **Public tail-module surface is preserved.** `Control.Helpers` keeps every public `create`/`text`
  entry point; the slice is dropped if it cannot net a reduction while preserving surface. The exact
  internal-module mechanism (sibling internal modules vs. the 188 `ModuleSuffix` lever) is a planning
  decision validated by a full-solution compile + surface-baseline regen.
- **Geometry/hash reorders stay bounded.** The Pattern-A restructuring is assumed to reorder at most a
  small, reviewable set of hashes (ideally none); if completing it forces broad visual/behavioral change
  beyond a reviewed golden-hash delta, that excess is **out of scope** and defers to a dedicated feature.
- **The current-tree line/location references in this spec (re-confirmed 2026-06-22 — `Control.fs`
  ~3,513 lines; `module internal ControlInternals` ~L124–3066; chart/widget geometry ~L626–1700;
  `faithfulContent` ~L1868; `renderScene` ~L2036; `toLayout`/`evaluateLayout` ~L2163/2268; `hashScene`
  ~L2405–2853; the 30 tail modules ~L3358–3511+) may drift** before implementation and MUST be
  re-confirmed at planning/implementation time, per the parent report's standing note.
- **Affected test projects**: the `Controls` construction / faithful-content / inspection / catalog
  suites, the retained-render hash/fingerprint and layout suites, and any `tools/Rendering.Harness` /
  consumer suites that read controls scene-hash or inspection artifacts. The exact project list is
  confirmed during `/speckit-plan`.
- **Out of scope**: `RetainedRender.step` (Phase 6), the deferred Phase 3 viewer-window/`GlHost.run`
  passes (feature 187 follow-up), and standing up the §7 golden-image/perf harnesses as permanent CI
  deliverables.

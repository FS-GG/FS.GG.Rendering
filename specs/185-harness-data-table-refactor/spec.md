# Feature Specification: Harness Data-Table Refactor

**Feature Branch**: `185-harness-data-table-refactor`

**Created**: 2026-06-22

**Status**: Draft

**Input**: User description: "next item in the plan" → Phase 1 of the god-module decomposition
(`docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md`): the harness
data-table refactor (Patterns D+E) — highest line-count payoff, lowest risk, no public surface.

## Context *(why this feature exists)*

The rendering harness (`tools/Rendering.Harness/`) is a build-time CLI tool with **no *package*
public surface and no render hot path**, so the 177–184 code-health campaign never touched it. (It is
not in `readiness/surface-baselines/`, so no surface-baseline or package bump applies. It *does* carry
an internal, compiler-enforced 387-val `Compositor.fsi` consumed by the test projects — see research
R-1 — so removing the per-feature `renderFeature*`/directory vals forces test retargeting (FR-010),
not a package change.) It is
now the single largest concentration of mechanical copy-forward duplication in the repository:

- `Compositor.fs` — **5,512 lines**, **85** `renderFeature<N><Variant>` functions following a rigid
  `renderFeature{148,149,152–161}{ValidationSummary|CompatibilityLedger|PackageValidation|RegressionValidation|Timing|LiveProof|Parity|ProofSet|Reuse|Snapshot|UnsupportedHost}` naming grid (the 12 catalog features; 150/151 are absent), plus 6 per-feature
  state machines and **110** `*ReadinessDirectory` constants.
- `Cli.fs` — **3,928 lines**, one near-duplicate command handler per feature.
- `ValidationLanes.fs` — `runLane` is a 154-line function fusing process spawn, timeout, output
  capture, and result assembly.

A partial `FeatureCatalog.fs` already exists: it defines a `FeatureDescriptor` record (Id, Slug,
CliAliases, **Variants set**, RequiredHeaders, Config) and a `catalog` list covering features
the 12 catalog features (148,149,152–161; 150/151 absent) — but it is consumed **only** by `Cli.fs` and does not yet drive `Compositor.fs`'s render
grid or directory constants. This feature promotes that descriptor table to the single source of
truth (SSOT) the whole harness reads from, and replaces per-feature *code* with per-feature *data*.

Each new harness feature should become **one descriptor row**, not a new copy of ~10 functions
across two files.

## User Scenarios & Testing *(mandatory)*

> The "user" of this feature is a **harness maintainer / FS.GG framework developer** who adds and
> runs feature-readiness lanes. Every story is an internal-quality slice; none changes the harness's
> externally observable command behavior or the *semantic* content of the readiness/evidence
> artifacts it emits.

### User Story 1 - Single source of truth for feature metadata (Priority: P1)

A maintainer wants every per-feature constant the harness needs — slug, CLI aliases, readiness/
parity/timing directory paths, required report headers, accepted-profile/policy/scenario config, and
the set of report variants the feature emits — to live in **one descriptor row** in
`FeatureCatalog`, so that adding or amending a feature is a single-site, compiler-checked change
instead of editing scattered constants across `Compositor.fs` and `Cli.fs`.

**Why this priority**: It is the foundation every other story builds on — the renderer parametrization
(US2) and the command collapse (US3) both read from this table. Promoting the already-started
descriptor to cover directories + headers + config is the lowest-risk, highest-leverage first step,
and it independently removes the 110 `*ReadinessDirectory` constants and the duplicated header/profile
literals.

**Independent Test**: Extend `FeatureCatalog` to carry directories + required headers + the shared
accepted-profile id; repoint `Compositor.fs`'s directory/header/profile constants at descriptor
lookups; build the harness and run the full Release `*.Tests.fsproj` sweep — the readiness artifacts
are byte-identical (paths/headers are literals reproduced exactly), proving the table is the SSOT
with no behavior change.

**Acceptance Scenarios**:

1. **Given** the harness emits readiness artifacts for the 12 catalog features (148,149,152–161), **When** the directory and
   header constants are sourced from `FeatureCatalog` descriptors instead of standalone `let`
   bindings, **Then** every emitted file lands at the same path with the same headers as before.
2. **Given** a maintainer adds a hypothetical feature row to the `catalog` list, **When** they build,
   **Then** the compiler accepts the single-row addition and no other constant table must be edited
   for the harness to know that feature's directories, aliases, and variants.
3. **Given** the 110 `*ReadinessDirectory` constants previously in `Compositor.fs`, **When** the
   refactor lands, **Then** those constants are replaced by descriptor-derived lookups (no standalone
   per-feature directory `let` remains).

---

### User Story 2 - Parametric renderer replaces the 85-function grid (Priority: P2)

A maintainer wants the `renderFeature<N><Variant>` grid in `Compositor.fs` replaced by **one generic
renderer parameterized over a descriptor + variant**, with explicit per-feature override hooks only
where a feature's output genuinely diverges from the template, so that the report-rendering logic is
written once and a feature's variants are declared as data (the descriptor's `Variants` set) rather
than as N copied functions.

**Why this priority**: This is the largest line-count reduction in the repository, but it depends on
US1's table being authoritative. It is P2 because it is higher-risk than US1 (it restructures the
artifact-producing code) and must be proven artifact-equivalent variant-by-variant.

**Independent Test**: Replace the 85 `renderFeature*` functions and the 6 per-feature state machines
with one parametric renderer + state driver reading the descriptor's `Variants`; convert the
`renderPackageValidation`/`renderRegressionValidation` feature-number `match` dispatch into descriptor
lookups; run the full sweep and diff every emitted readiness/evidence artifact against a pre-refactor
baseline for **semantic** equivalence (status, counts, headers, ordering of meaningful entries).

**Acceptance Scenarios**:

1. **Given** a feature with variants `{ValidationSummary; Timing; Parity}` in its descriptor, **When**
   the harness renders that feature, **Then** exactly those three reports are produced, with content
   semantically identical to the prior per-function output.
2. **Given** a feature whose output legitimately differs from the template (e.g. a bespoke proof-set
   layout), **When** the renderer runs, **Then** that difference is supplied via an explicit override
   hook on the descriptor, not by a separate top-level `renderFeatureNNN…` function.
3. **Given** the two feature-number-dispatch renderers (`renderPackageValidation`,
   `renderRegressionValidation`), **When** the refactor lands, **Then** their `match featureNum`
   branches are replaced by a descriptor lookup keyed on `Id`.

---

### User Story 3 - One readiness workflow replaces per-feature CLI commands (Priority: P3)

A maintainer wants the per-feature command handlers in `Cli.fs` collapsed into **one
descriptor-driven `runReadiness` workflow** (probe → make directories → build reports → write the N
variant files → render) plus a command table mapping CLI aliases to descriptors, so that wiring a new
feature into the CLI is a descriptor row rather than a new ~100-line command handler.

**Why this priority**: It is the second-largest reduction but sits on top of US1 (the alias/variant
table) and US2 (the parametric renderer it calls). P3 because the CLI is the most behavior-visible
surface (argument parsing, exit codes, run-id formatting) and must keep its observable contract.

**Independent Test**: Replace the per-feature `Cmd` handlers with one `runReadiness descriptor`
workflow dispatched via the existing `CliAliases`; invoke the harness for each feature alias and
confirm identical observable behavior — same files written, same exit code, same stdout shape (run-id
formatting may vary only where it already embeds a timestamp).

**Acceptance Scenarios**:

1. **Given** a CLI invocation naming a feature by id, `feature<N>`, or slug alias, **When** the
   command runs, **Then** it dispatches through the shared `runReadiness` workflow and produces the
   same artifacts and exit code as the prior dedicated handler.
2. **Given** an unknown feature alias, **When** the command runs, **Then** the harness reports the
   error the same way it did before (no silent success, no changed exit code).
3. **Given** a feature added as a descriptor row, **When** the maintainer builds, **Then** its CLI
   command is available with no new per-feature handler function.

---

### User Story 4 - runLane decomposed into single-responsibility stages (Priority: P4)

A maintainer wants `ValidationLanes.runLane` split into focused units — a process runner, a timeout
manager, an output buffer, and a thin orchestrator — so that lane execution logic is testable in
isolation and a failure localizes to one stage instead of a 154-line function.

**Why this priority**: It is independent of US1–US3 (it touches lane execution, not feature
descriptors) and is the smallest of the four, so it is P4 — valuable cleanup that can land any time
without blocking the descriptor work.

**Independent Test**: Extract the process-spawn, timeout, and output-capture concerns from `runLane`
into named functions/types behind a thin orchestrator; run the validation-lane sweep and confirm lane
results (status, captured output, timeout behavior) are unchanged.

**Acceptance Scenarios**:

1. **Given** a lane that completes normally, **When** it runs through the decomposed pipeline, **Then**
   its `LaneResult` (status, output, timing) matches the pre-refactor result.
2. **Given** a lane that exceeds its timeout, **When** it runs, **Then** it is terminated and reported
   as a timeout exactly as before, with the timeout logic now isolated in its own unit.

---

### Edge Cases

- **Feature not in the catalog**: a feature number referenced by render/CLI code but absent from the
  `catalog` list MUST fail loudly at build or first use (exhaustive descriptor lookup), never silently
  skip producing an artifact.
- **Variant declared but no template path**: if a descriptor lists a `Variant` the generic renderer
  has no template or override for, the build/run MUST surface it rather than emit an empty/garbage
  report.
- **Override hook diverges from template**: where a feature's report genuinely differs, the override
  path MUST produce output semantically identical to today's bespoke function — the generic template
  is not allowed to "round off" a real difference.
- **Directory-path drift**: descriptor-derived directory paths MUST reproduce the exact prior path
  strings (these are consumed by downstream CI evidence collection that greps fixed paths).
- **CLI alias collision**: two descriptors MUST NOT claim the same alias; a collision MUST be caught,
  not resolved by silent last-wins.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The harness MUST source all per-feature metadata (slug, CLI aliases, readiness/parity/
  timing directories, required report headers, accepted-profile/policy/scenario config, and the set of
  emitted report variants) from a single `FeatureCatalog` descriptor table, extending the existing
  partial `FeatureDescriptor`/`catalog` rather than introducing a parallel structure.
- **FR-002**: The 110 `*ReadinessDirectory` constants and the duplicated header/accepted-profile
  literals in `Compositor.fs` MUST be replaced by descriptor-derived lookups; no standalone
  per-feature directory/header constant may remain once its descriptor field exists.
- **FR-003**: The 85 `renderFeature<N><Variant>` functions and the 6 per-feature state machines MUST
  be replaced by one parametric renderer + state driver that reads the descriptor's `Variants` set,
  with explicit per-descriptor override hooks for the genuinely divergent ~20%.
- **FR-004**: The `renderPackageValidation`/`renderRegressionValidation` feature-number `match`
  dispatch MUST be converted to descriptor lookups keyed on the feature `Id`.
- **FR-005**: The per-feature CLI command handlers MUST be collapsed into one descriptor-driven
  `runReadiness` workflow plus a command/alias table; adding a feature MUST require no new per-feature
  handler function.
- **FR-006**: `ValidationLanes.runLane` MUST be decomposed into single-responsibility units (process
  runner, timeout manager, output buffer) behind a thin orchestrator.
- **FR-007**: The harness's externally observable CLI behavior MUST be preserved: same artifacts
  written to the same paths, same exit codes, same error reporting for unknown features/aliases. Run-id
  strings that already embed a wall-clock timestamp may differ only in that timestamp.
- **FR-008**: Every readiness/evidence/parity/timing artifact the harness emits MUST be **semantically
  equivalent** to a pre-refactor baseline — identical status, counts, required headers, and meaningful
  entry ordering. Per the relaxed-constraints decision (report §7), byte-identity is not required for
  artifacts whose only changes are harmless wording/ordering, but semantic structure MUST match. Where
  a path or header is a fixed literal consumed by CI greps, it MUST remain byte-identical.
- **FR-009**: The refactor MUST NOT add any new project, package dependency, or inter-project
  reference; it only splits, parametrizes, and deletes within `tools/Rendering.Harness/`. The harness
  has no *package* public surface, so no surface baseline or package bump is involved — but its internal
  `Compositor.fsi` (387 vals, R-1) is rewritten as the per-feature vals are removed (FR-010).
- **FR-010**: Tests or assertions covering harness behavior MUST keep equivalent coverage on the
  parametric path; any test asserting a now-removed per-feature function's internals MUST be retargeted
  to the descriptor-driven equivalent, not weakened or deleted to pass.
- **FR-011**: A missing or duplicate descriptor (unknown feature id referenced by code, or two
  descriptors sharing a CLI alias) MUST be caught at build or first use, never silently ignored.

### Key Entities *(include if feature involves data)*

- **FeatureDescriptor**: the SSOT row for one harness feature — `Id`, `Slug`, `CliAliases`, the
  `Variants` set of report kinds it emits, `RequiredHeaders`, directory paths, and a `Config`
  (policy / accepted-profile / required-scenario ids). Already partially present in
  `FeatureCatalog.fs`; this feature completes it.
- **ReportVariant**: the enumerated kind of report a feature can emit (ValidationSummary,
  CompatibilityLedger, PackageValidation, RegressionValidation, Timing, LiveProof, Parity, ProofSet,
  Reuse, Snapshot, UnsupportedHost). A descriptor's `Variants` set selects which the generic renderer
  produces.
- **FeatureRenderHooks** (new): the per-descriptor override points for the ~20% of feature/variant
  combinations whose output diverges from the generic template.
- **runReadiness workflow**: the single descriptor-driven CLI pipeline (probe → make dirs → build
  reports → write variant files → render) replacing the per-feature command handlers.
- **Lane-execution units**: process runner, timeout manager, output buffer, and orchestrator carved
  out of `runLane`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: No `.fs` file in `tools/Rendering.Harness/` exceeds 1,500 lines after the refactor (today
  `Compositor.fs` is 5,512, `Cli.fs` is 3,928). `.fsi` signature files are not counted against this cap.
- **SC-002**: Adding a new harness feature requires editing exactly **one** location (a single
  `catalog` descriptor row, plus an override hook only if the feature genuinely diverges) — verified by
  walking through the addition of a sample feature row.
- **SC-003**: The count of `renderFeature<N><Variant>` top-level functions drops from 85 to 0 (replaced
  by the parametric renderer + override hooks), and the 110 `*ReadinessDirectory` standalone constants
  drop to 0.
- **SC-004**: The full Release `*.Tests.fsproj` sweep ends at the same red/green set as the
  pre-refactor baseline (known pre-existing reds recorded as not-regression).
- **SC-005**: Every readiness/evidence/parity/timing artifact emitted by the harness is
  semantically equivalent to its pre-refactor baseline (status, counts, headers, meaningful ordering),
  and every fixed CI-grepped path/header is byte-identical.
- **SC-006**: The harness's observable CLI contract (artifacts, paths, exit codes, unknown-feature
  error reporting) is unchanged across every feature alias.

## Assumptions

- The maintainer's relaxation of the four feature-182 freezes applies; for this phase the only
  relevant relaxation is **#3 (byte-identical evidence artifacts)** — semantic equivalence is the bar
  for harness artifacts (report §7). The render hot path (#2) and public surface (#1) are not touched
  by Phase 1, so the §7 golden-image/perf gates are **not** prerequisites for this feature (they are
  built alongside but gate the later render-path phases).
- The existing `FeatureCatalog.FeatureDescriptor`/`catalog` (the 12 features 148,149,152–161) is the correct seed to
  extend; this feature grows it to cover directories, headers, and render hooks rather than replacing
  it.
- The harness is a build-time/CI CLI tool with no *package* public surface and no package consumers, so
  no surface baseline regeneration, CompatibilityLedger entry, or package version bump is required.
  Per research R-1, it nonetheless has an internal 387-val `Compositor.fsi` that the test projects call
  directly; the removed `renderFeature*`/directory vals are a real internal-surface deletion that forces
  test retargeting (FR-010) and raises US2/US3 scope above the "no `.fsi` surface" first impression.
- "Semantic equivalence" for artifacts is judged by parsed structure (status, counts, required
  headers, meaningful entry order), consistent with the report's §7 semantic-artifact-diff gate.
- Pre-existing red tests carried by prior features (e.g. stale-feed `Package.Tests`/`ControlsGallery`
  reds noted in 182/183 known-reds) are baseline-not-regression and are not introduced or masked by
  this refactor.
- This is **Phase 1 of 6**; subsequent phases (state records, viewer/codec splits, Scene, Control,
  RetainedRender) are out of scope here and tracked by the parent plan.

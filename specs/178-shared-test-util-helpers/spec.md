# Feature Specification: Shared Test/Util Helpers (Code-Health Refactoring Phase 1)

**Feature Branch**: `178-shared-test-util-helpers`

**Created**: 2026-06-21

**Status**: Draft

**Input**: User description: "start next item in the project." → Phase 1 of the code-health
refactoring plan (`docs/reports/2026-06-21-05-19-code-health-refactoring-analysis-and-plan.md`),
following the merged Phase 0 (feature 177).

## Overview

The repository is clean of rot but structurally heavy: its largest mechanical debt is **copy-pasted
helper logic**. Three small utilities are duplicated across many files:

1. A **repository-root finder** (`findRepositoryRoot` + a `repositoryRoot` value) re-typed across
   ~59 test/harness files in two divergent families — a named `findRepositoryRoot` using the
   `*.sln` / `*.slnx` / `build.fsx` marker set, and inline anonymous walks that hard-code the
   `FS.GG.Rendering.slnx` filename — with subtly divergent marker logic; earlier features (045) had
   to fix the same bug in multiple places. (Planning revised the original "~26 definitions" estimate
   upward after the full grep; see `research.md` R1.)
2. An **FNV-1a hash fold** whose offset basis (`0xcbf29ce484222325UL`) and prime are open-coded at
   four production sites in `src/Controls` (`Composition.fs`, `RetainedRender.fs`, `Control.fs` ×2),
   the same family of literals Phase 0 just corrected.
3. A **`clamp`** function defined locally at least three times in `src` (and again in tests).

This feature extracts each into a single shared definition and routes all existing call sites through
it, deleting the duplicates. It is a pure, behavior-preserving refactor: no user-visible output, no
public API surface change, byte-identical hashes and clamped values, build + test stay green.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - One repository-root finder for all tests (Priority: P1)

A contributor adds or fixes a test that needs to locate the repo root (to read `specs/`, `readiness/`,
or packed-feed paths). Today they copy one of several near-identical `findRepositoryRoot` blocks and
risk picking a stale variant. After this change there is exactly **one** shared repository-root finder
that every test/harness call site uses; the copies are gone.

**Why this priority**: Highest-volume duplication and lowest risk (test/tooling code only, no
production-runtime behavior). Removing it banks most of the line reduction and eliminates the
recurring "fix the marker logic in N places" bug class outright.

**Independent Test**: Build and run the full test suite after replacing every local
`findRepositoryRoot`/`repositoryRoot` with the shared helper; every test that resolves repo-relative
paths still passes, and `grep` finds no remaining local definition of the finder.

**Acceptance Scenarios**:

1. **Given** the shared repo-root helper exists, **When** the suite runs from each test project's
   build output directory, **Then** the resolved repository root is identical to what the previous
   per-file finder returned and all path-dependent tests pass.
2. **Given** all call sites are migrated, **When** the codebase is searched for a local repo-root
   finder definition, **Then** only the one shared definition remains (zero inline copies).
3. **Given** the shared finder, **When** it is invoked from a directory with no `.sln`/`.slnx`/
   `build.fsx` marker anywhere up to the filesystem root, **Then** it fails with a clear, actionable
   error message (preserving the existing fail-loud behavior).

---

### User Story 2 - One FNV-1a hash helper for production folds (Priority: P2)

A maintainer reading or changing the retained-render / composition hashing wants a single trustworthy
hash primitive rather than four hand-rolled folds that must be kept in lockstep (the exact failure
mode Phase 0 fixed). After this change the offset basis, prime, and the per-byte/per-string mixing
live in one shared helper, and the four `src/Controls` sites call it.

**Why this priority**: High duplication value but touches **production** hashing whose outputs feed
layer-reuse/promotion identity. It must be byte-identical to today's results (including the
`feature159Hash` value Phase 0 just corrected), so it carries more risk than Story 1 and is sequenced
after it.

**Independent Test**: Route each of the four folds through the shared helper, then run the Feature
159 identity/reuse/promotion suites and the composition/control fingerprint tests; every computed
hash equals its pre-refactor value (no golden/identity drift).

**Acceptance Scenarios**:

1. **Given** the shared FNV helper, **When** any of the four migrated sites computes a hash for a
   given input, **Then** the result is bitwise-identical to the pre-refactor fold's result for the
   same input.
2. **Given** all four sites are migrated, **When** the codebase is searched for an open-coded FNV
   offset basis (`0xcbf29ce484222325UL`) outside the shared helper, **Then** none remain.
3. **Given** the Feature 159 identity/reuse/promotion regression suites, **When** they run after the
   migration, **Then** they pass with no change in relational identity outcomes.

---

### User Story 3 - One `clamp` helper (Priority: P3)

A contributor needs to bound a value to a range. Instead of re-defining `clamp` locally (as done in
at least three `src` files and several tests), they use one shared definition.

**Why this priority**: Smallest of the three (a one-line function), lowest line-count payoff, but
closes the trio and removes a trivially error-prone local-redefinition habit. Lowest priority because
the payoff is modest.

**Independent Test**: Replace each local `clamp` with the shared one; all tests that exercise clamped
behavior (layout sizing, text caret, viewer scaling) pass unchanged, and no local `clamp` definition
remains.

**Acceptance Scenarios**:

1. **Given** the shared `clamp`, **When** it is applied with the same `(low, high, value)` arguments
   as a removed local copy, **Then** it returns the identical result (including boundary and inverted
   range behavior matching the prior local semantics).
2. **Given** all call sites are migrated, **When** the codebase is searched for a local `clamp`
   definition, **Then** only the shared definition remains.

---

### Edge Cases

- **Inconsistent local semantics**: if any duplicate (e.g. a `clamp` variant or a repo-root marker
  set) differs in behavior from the others, the consolidation MUST adopt the single correct/intended
  behavior and the difference MUST be called out, not silently chosen. (The repo-root variants are
  known to differ on marker detection; the canonical version detects `.sln`, `.slnx`, and `build.fsx`.)
- **Test-helper sharing boundary**: test projects currently reference `src` projects, not each other.
  The shared test helper must be reachable from every consuming test/harness project without creating
  a circular reference or pulling production code into test-only concerns, and without a new public
  package surface.
- **Cross-project hash reach**: the FNV helper must be placed so all four `src/Controls` sites can use
  it without introducing a new module cycle or altering `src` public surface.
- **Empty/odd inputs to FNV**: hashing an empty byte sequence / empty string must yield the same value
  the previous fold produced for that input.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a single shared repository-root finder (function + resolved
  root value) usable by every test and harness project that currently defines its own.
- **FR-002**: The shared repo-root finder MUST detect the repository root via the canonical marker set
  (`.sln`, `.slnx`, and the historical `build.fsx`) and MUST fail loudly with an actionable message
  when no marker is found up to the filesystem root.
- **FR-003**: Every existing local `findRepositoryRoot`/`repositoryRoot` definition in test and
  harness code MUST be removed and its call sites routed through the shared helper.
- **FR-004**: The system MUST provide a single shared FNV-1a hash helper exposing the canonical offset
  basis and prime plus the byte/string mixing operations needed by the existing folds.
- **FR-005**: The four `src/Controls` FNV fold sites (`Composition.fs`, `RetainedRender.fs`,
  `Control.fs` ×2) MUST be routed through the shared helper, producing bitwise-identical hashes for
  all inputs, including the Phase-0-corrected `feature159Hash` value.
- **FR-006**: The system MUST provide a single shared `clamp` helper, and every local `clamp`
  definition in `src` (and any duplicated in tests) MUST be removed and routed through it, preserving
  identical results for the same arguments.
- **FR-007**: The refactor MUST NOT change any public API surface: no `.fsi` signature additions,
  removals, or changes that affect the published `FS.GG.UI.*` surface, and surface-area/API-reference
  baselines MUST remain green. (New shared helpers are placed so their visibility is governed by
  `.fsi` presence/absence per the constitution, with no `private`/`internal`/`public` keyword on new
  top-level bindings.)
- **FR-008**: The refactor MUST preserve all user-visible behavior: no change to rendered pixels,
  layout output, hash-driven layer reuse/promotion outcomes, or any persisted golden/readiness
  artifact.
- **FR-009**: Each of the three consolidations (repo-root, FNV, clamp) MUST be independently
  shippable: the build and the full test suite stay green after each one in isolation.
- **FR-010**: Any pre-existing behavioral divergence found among duplicates MUST be reconciled to the
  single intended behavior and documented in the plan/research notes, not silently absorbed.

### Key Entities *(include if feature involves data)*

- **Repo-root helper**: a shared finder (`findRepositoryRoot`) plus the resolved `repositoryRoot`
  value; inputs a starting directory, outputs the nearest ancestor directory containing a repository
  marker.
- **FNV helper**: a shared hash primitive — offset basis, prime, and `mix`/`mixString`-style folding
  operations — producing a `uint64` digest identical to the current open-coded folds.
- **Clamp helper**: a shared bounding function over `(low, high, value)` returning `value` constrained
  to `[low, high]`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After the feature, the build succeeds and the test suite is green except for the two
  documented pre-existing package-feed reds (`tests/Package.Tests`,
  `samples/ControlsGallery/ControlsGallery.Tests`) — no new failures introduced.
- **SC-002**: Exactly one repository-root finder definition exists in the codebase; a repo-wide search
  for additional local definitions returns zero.
- **SC-003**: Exactly one FNV offset-basis literal site exists (inside the shared helper); a repo-wide
  search for `0xcbf29ce484222325UL` outside the helper returns zero, and all Feature 159 identity/
  reuse/promotion hashes are unchanged from their pre-refactor values.
- **SC-004**: Exactly one `clamp` definition exists; a repo-wide search for additional local `clamp`
  definitions returns zero.
- **SC-005**: Net source-line count drops by a four-figure amount (thousands of lines) across the
  removed duplicates, with no public `.fsi`/surface-area baseline change (`git diff -- '*.fsi'` shows
  no signature change to published surface).
- **SC-006**: Each of the three consolidations can be reverted independently without affecting the
  other two (verified by their separation into independent, individually-green change units).

## Assumptions

- The next project item is Phase 1 of the code-health refactoring plan; Phase 0 (feature 177) is
  complete and merged, so its `feature159Hash` correction is the baseline the FNV helper must
  preserve.
- The two pre-existing package-feed test failures are environmental baseline (release/package-feed
  only), unchanged by this feature, and are not counted as regressions.
- "Byte-identical" hash preservation is validated through the existing Feature 159 relational
  identity/reuse/promotion suites and composition/control fingerprint tests rather than by asserting
  absolute hash constants (consistent with the Phase 0 evidence approach).
- Placement of the shared helpers (which module/project) is a planning decision; this spec only
  requires that placement keep visibility in `.fsi`, avoid module cycles, and add no public package
  surface.
- This is a Tier-2 (internal) change: no public API surface is added, removed, or modified.

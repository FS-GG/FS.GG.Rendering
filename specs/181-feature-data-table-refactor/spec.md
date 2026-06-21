# Feature Specification: Per-Feature Data-Table Refactor

**Feature Branch**: `181-feature-data-table-refactor`

**Created**: 2026-06-21

**Status**: Draft

**Input**: User description: "start next item in the project." — resolved to Phase 4 of the code-health refactoring plan (`docs/reports/2026-06-21-05-19-code-health-refactoring-analysis-and-plan.md`): the per-feature data-table refactor that converts copy-forward *code* into *data*.

## Context

The rendering harness (`tools/Rendering.Harness/`) accumulated per-feature scaffolding as features 145–161 shipped. Each feature carries a near-identical family of report renderers (live proof, parity, reuse, snapshot, timing, validation summary, compatibility ledger, package/regression validation, proof set) in `Compositor.fs` (~114 `let render…` functions, ~5,667 lines), a matching command handler in `Cli.fs` (~4,004 lines), and a copy-forward `Feature###CompatibilityLedgerTests.fs` family in `tests/Package.Tests/`. Adding the next feature today means copying an entire family of functions and constants and editing the numbers. The goal is to make a new per-feature addition a **single data entry** plus only the genuinely-unique logic, without changing any observed harness output.

**Carried lesson from Phase 3 (feature 180, SC-005):** a config/record-of-functions abstraction reliably reduces *duplication* but can *increase* total line count and indirection when it replaces bodies that look similar but diverge in detail. This feature therefore treats net line reduction as a measured, gated outcome — not an assumption — and explicitly excludes from collapse any per-feature body that is genuinely divergent.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Add a new feature with a single data entry (Priority: P1)

A maintainer adding harness coverage for a new feature wants to register it by adding one descriptor record (id, slug, directory set, required headers, timing/parity configuration) rather than copying and renumbering an entire family of renderers, a CLI handler, and a test file.

**Why this priority**: This is the core payoff of the phase — converting copy-forward code into data. It delivers the largest expected line reduction in the repo and removes the most error-prone manual step (renumbering copied bodies). It is the MVP: even if CLI and tests are left untouched, a descriptor-driven renderer for the report families already delivers value.

**Independent Test**: Add a descriptor entry for a hypothetical feature and confirm the generic renderer produces the same shape of output the copy-forward functions would have produced, with no new per-feature renderer functions written.

**Acceptance Scenarios**:

1. **Given** the descriptor list and generic renderer, **When** a new feature descriptor is appended, **Then** all standard report variants for that feature render through the generic path with no new `render<Feature>…` function added.
2. **Given** the refactored `Compositor.fs`, **When** the harness renders any existing feature's reports, **Then** the rendered output is byte-for-byte identical to the pre-refactor output.

---

### User Story 2 - Drive the harness CLI from the same descriptor table (Priority: P2)

An operator invoking the harness from the command line wants every existing per-feature command and its output to behave exactly as before, now dispatched from a command table keyed by the shared descriptor rather than per-feature handler bodies.

**Why this priority**: The CLI is the public surface of the harness; collapsing its per-feature handlers compounds the line savings and keeps the descriptor as the single source of truth, but it depends on the descriptor existing (Story 1) and carries higher behavioral risk, so it follows P1.

**Independent Test**: Run each existing per-feature CLI command before and after the change and diff stdout/stderr/exit code; all must match.

**Acceptance Scenarios**:

1. **Given** the descriptor-keyed command table, **When** any existing per-feature command is invoked with its existing arguments, **Then** stdout, stderr, and exit code match the pre-refactor harness exactly.
2. **Given** the shared performance/readiness runner extracted from the per-feature handlers, **When** a timing or readiness command runs, **Then** the emitted metrics and readiness verdicts are unchanged.

---

### User Story 3 - Collapse copy-forward test families into data-driven lists (Priority: P3)

A maintainer running the package and compatibility test suites wants the per-feature `Feature###CompatibilityLedgerTests.fs` families (and the copy-forward compositor test families) expressed as one data-driven `testList` over the descriptor set, so the same assertions cover all features without per-feature test files.

**Why this priority**: Test collapse is the lowest behavioral risk and largest test-side line saving, but it is most valuable once the descriptor table is the established source of truth (Stories 1–2). It is independently shippable.

**Independent Test**: Run the package/compatibility suites before and after; the same set of test cases (by name/coverage) must pass, with the per-feature files replaced by one parameterized `testList`.

**Acceptance Scenarios**:

1. **Given** the data-driven compatibility `testList`, **When** the package test suite runs, **Then** every feature previously covered by a `Feature###CompatibilityLedgerTests.fs` file is still asserted, with equivalent pass/fail outcomes.
2. **Given** a new feature descriptor, **When** the suites run, **Then** that feature is automatically covered with no new test file.

---

### Edge Cases

- **Genuinely-divergent per-feature bodies**: some features have report variants others lack (e.g., `PackageValidation`, `ProofSet`, `RegressionValidation` appear only for some features). The descriptor must express which variants a feature has, and bodies that genuinely differ MUST remain as feature-specific overrides rather than being forced into the generic shape.
- **Net line increase**: if collapsing a family through the generic path produces *more* total lines (the Phase-3 SC-005 outcome), that family MUST be left in its explicit form and the decision recorded, rather than shipping a more-verbose abstraction.
- **Hidden output differences**: a renderer that appears identical may emit a feature-specific header, ordering, or constant; the byte-stability check (full Release sweep vs. baseline) is the gate that catches these.
- **Constant drift**: the ~262 per-feature constants referenced by the original plan must be sourced from the descriptor or a single table, with no silently-changed values.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The harness MUST expose a single `FeatureDescriptor` record (id, slug, directory set, required headers, timing/parity configuration, and the set of report variants the feature supports) as the source of truth for per-feature behavior.
- **FR-002**: `Compositor.fs` MUST render the standard report variants for every existing feature through a generic, descriptor-driven renderer rather than per-feature `render<Feature>…` functions, except where a body is genuinely divergent (FR-007).
- **FR-003**: The refactored harness MUST produce byte-for-byte identical rendered output, CLI stdout/stderr, and exit codes for all existing features and commands compared to the pre-refactor baseline.
- **FR-004**: `Cli.fs` MUST dispatch per-feature commands from a command table keyed by the descriptor, with the shared performance/readiness runner extracted into one reusable body.
- **FR-005**: The per-feature `Feature###CompatibilityLedgerTests.fs` families (and the copy-forward compositor test families) MUST be collapsed into data-driven `testList`s parameterized over the descriptor set, preserving equivalent coverage and outcomes.
- **FR-006**: Adding coverage for a new feature MUST require only a new descriptor entry plus any genuinely-unique logic — no copied-and-renumbered renderer, handler, or test file.
- **FR-007**: Per-feature bodies that are genuinely divergent (variants not shared across features, or bodies whose collapse would increase total line count or obscure intent) MUST be retained as explicit feature-specific code, and each such retention MUST be recorded with its rationale.
- **FR-008**: The change MUST NOT alter any public package surface or `.fsi`-exposed contract of shipped libraries; the harness is an internal tool under `tools/`, and any signature changes MUST stay within it.
- **FR-009**: `dotnet build` and `dotnet test` MUST be green at the end of the feature, with pre-existing baseline reds (e.g., known Package.Tests / ControlsGallery package-feed reds) unchanged and not regressed.

### Key Entities *(include if feature involves data)*

- **FeatureDescriptor**: the single data record describing one harness feature — identity (numeric id, slug), directory set, required headers, timing/parity configuration, and which report variants it supports. The source of truth that the renderer, CLI command table, and tests all read.
- **Report variant**: a named kind of per-feature output (live proof, parity, reuse, snapshot, timing, validation summary, compatibility ledger, package validation, proof set, regression validation). A descriptor declares which variants it supports.
- **Descriptor list / table**: the ordered collection of all `FeatureDescriptor`s for features 145–161, consumed by the generic renderer, the CLI command table, and the data-driven test lists.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Adding a new feature to the harness requires exactly one new descriptor entry (plus only genuinely-unique logic), demonstrated by appending a descriptor and observing full standard-variant coverage with zero new per-feature functions, handlers, or test files.
- **SC-002**: 100% of existing features and CLI commands produce byte-for-byte identical output, stdout/stderr, and exit codes versus the pre-refactor baseline (verified by a full Release sweep diff).
- **SC-003**: The number of per-feature `render<Feature>…` functions in `Compositor.fs` drops from ~114 to the count of genuinely-divergent retained bodies (target: the great majority collapsed into the generic path).
- **SC-004**: The per-feature `Feature###CompatibilityLedgerTests.fs` files are replaced by one data-driven `testList`, reducing per-feature test files to zero while preserving equivalent coverage.
- **SC-005**: Net total line count across the touched files decreases relative to baseline; if any individual family's collapse would increase lines (the Phase-3 lesson), that family is left explicit and the exclusion is recorded — net change MUST NOT be a regression attributable to abstraction overhead.
- **SC-006**: `dotnet build` and `dotnet test` are green, with the known pre-existing baseline reds unchanged.

## Assumptions

- "Next item in the project" resolves to **Phase 4** of the code-health refactoring plan; Phases 0–3 are done and merged (features 177/178/179/180), per the project memory and recent commit history.
- The plan's original counts (97 renderers / 262 constants / 17 ledger test files) predate the Phase-2 relocation of the harness to `tools/`; the authoritative current scope is the actual code at HEAD (~114 `let render…` functions across features 145–161, mirrored in `Cli.fs` and the `tests/Package.Tests/` ledger families).
- The harness under `tools/Rendering.Harness/` is an internal tool, not a shipped package; refactoring its internal signatures is in scope, but no shipped library's public/`.fsi` surface changes.
- Byte-stability against a captured baseline (a full Release sweep) is the acceptance gate for "behavior unchanged."
- Each user story is independently shippable in priority order (renderer → CLI → tests), each ending green on build + test, consistent with the per-phase shippability convention used throughout this refactoring project.

## Out of Scope

- Phase 5 god-module splits (SkiaViewer.fs, Control.fs, Scene.fs, Testing.fs, etc.) and Phase 6 type-safety hardening.
- Cross-project DU migrations (e.g., `RetainedInspectionStatus`/`VisualInspectionStatus` in `FS.GG.UI.Scene`) deferred from Phase 3.
- Any change to shipped package public surfaces or `.fsi` contracts.
- Changing observed harness output, metrics, or readiness verdicts — this is a pure structural refactor.

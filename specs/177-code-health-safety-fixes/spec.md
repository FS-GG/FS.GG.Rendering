# Feature Specification: Code Health — Quick Safety Fixes (Refactoring Phase 0)

**Feature Branch**: `177-code-health-safety-fixes`

**Created**: 2026-06-21

**Status**: Draft

**Input**: User description: "start next item in the project" → Phase 0 of the code-health refactoring plan (`docs/reports/2026-06-21-05-19-code-health-refactoring-analysis-and-plan.md`): verify one possible latent hash bug and bank the safest, lowest-risk cleanups before any larger refactoring work.

## Overview

This is an internal code-health change targeting the **maintainers** of the FS.GG.Rendering codebase, not end users of a generated product. It is the first ("Phase 0") increment of a sequenced refactoring plan and is deliberately scoped to three small, independently verifiable items that carry near-zero behavioral risk. Its purpose is to resolve one suspected latent bug and remove two minor correctness/clarity hazards, so that later, heavier refactoring phases start from a clean, trusted base.

The three items are:

1. **Verify (and resolve) the `feature159Hash` offset constant.** `src/Controls/RetainedRender.fs:851` seeds its FNV-1a fold with `1469598103934665603UL`, which is **not** the standard 64-bit FNV-1a offset basis `0xcbf29ce484222325UL` (= `14695981039346656037UL`) used by every other hash accumulator in the codebase (`Composition.fs:157`, `Control.fs:2454`, `Control.fs:2830`). This is a confirmed discrepancy; the maintainer must decide whether it is a typo to fix or an intentional value to document. **(Resolved in plan.md/research.md: it is the standard basis with the trailing `7` dropped — an unambiguous typo — so the decision is to change it to `0xcbf29ce484222325UL`.)**
2. **Replace the two tautological test placeholders.** `tests/Controls.Tests/Feature093ParityTests.fs:77` and `tests/Controls.Tests/TypedMigrationTests.fs:555` both assert `Expect.isTrue true …`, which can never fail and therefore tests nothing.
3. **Centralize the layout-cache revision `150`.** The revision number is hand-duplicated across **four** sites: the `"rev=150"` token at `src/Layout/Layout.fs:839` and `:964`, and the `Revision = 150` field at `:847` and `:974`. A future bump must edit all four in lockstep or risk silent cache-key drift.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Trustworthy retained-render content hash (Priority: P1)

A maintainer auditing the retained-render layer needs confidence that `feature159Hash` behaves like every other hash in the codebase (or has a documented reason not to). Today the non-standard offset constant is an unexplained outlier that reads as a probable typo, undermining trust in the content-identity hash that drives layer reuse/promotion decisions.

**Why this priority**: It is the only item that could represent an actual latent bug, and it gates the credibility of the whole "Phase 0 = safe base" premise. Resolving it first is the highest-value action.

**Independent Test**: Can be fully tested by inspecting the constant, making the fix-or-document decision, and confirming the existing Feature 159 identity/reuse test suites (`Feature159IdentitySplitTests`, `Feature159ReuseCounterTests`, `Feature159PromotionEvidenceTests`, `Feature159ReadinessPackageTests`) remain green — with any golden/persisted-hash impact explicitly reviewed.

**Acceptance Scenarios**:

1. **Given** the `feature159Hash` offset constant differs from the standard FNV-1a basis, **When** the maintainer reviews whether the difference is intentional, **Then** a decision is recorded: either the constant is changed to `0xcbf29ce484222325UL` **or** a code comment explains why the divergent value is intentional.
2. **Given** the constant is changed to the standard basis, **When** the full test suite and any content-hash-dependent evidence are regenerated/reviewed, **Then** there is no regression and any change to computed hash values is confirmed acceptable (not silently invalidating persisted goldens).
3. **Given** the constant is documented as intentional, **When** a future reader encounters it, **Then** the code itself explains the divergence without needing this report.

### User Story 2 - Tests that can actually fail (Priority: P2)

A maintainer reading the Controls test suite needs every assertion to mean something. The two `Expect.isTrue true` placeholders give false coverage signal: they appear to validate parity/migration behavior but pass unconditionally.

**Why this priority**: Low risk and quick, but directly improves the integrity of the test suite that every later phase relies on for regression safety.

**Independent Test**: Can be tested by locating the two assertions and confirming each is either replaced with a meaningful assertion over real state or removed, with the surrounding test still building and passing.

**Acceptance Scenarios**:

1. **Given** `Feature093ParityTests.fs:77` asserts `Expect.isTrue true`, **When** the placeholder is addressed, **Then** the test either asserts a real, falsifiable condition about the procedural parity baselines or the vacuous assertion is removed.
2. **Given** `TypedMigrationTests.fs:555` asserts `Expect.isTrue true`, **When** the placeholder is addressed, **Then** the test either asserts a real, falsifiable condition about the "no forked model type" guarantee or the vacuous assertion is removed.
3. **Given** the changes, **When** the Controls test suite runs, **Then** it builds and passes with no remaining `Expect.isTrue true` occurrences in the touched files.

### User Story 3 - Single source of truth for the layout cache version (Priority: P3)

A maintainer who needs to invalidate the layout cache (e.g., after changing the layout algorithm) should bump the version in exactly one place. Today the `"rev=150"` token lives in two string-building sites that must stay identical.

**Why this priority**: Pure clarity/maintainability with no behavior change today; lowest urgency but trivially safe and prevents a future drift bug.

**Independent Test**: Can be tested by confirming all four former literal sites (the two `"rev=150"` tokens and the two `Revision = 150` fields) derive from a single named constant and that the composed cache identity/key strings are byte-identical to before the change.

**Acceptance Scenarios**:

1. **Given** the revision `150` is hand-duplicated across four sites (`"rev=150"` at `Layout.fs:839` and `:964`; `Revision = 150` at `:847` and `:974`), **When** centralization is applied, **Then** all four sites derive from one shared constant.
2. **Given** the centralized constant, **When** layout cache identity/key strings are produced, **Then** the resulting strings are unchanged (the cache continues to hit exactly as before).
3. **Given** a future version bump, **When** the maintainer edits the single constant, **Then** all cache-key sites update together with no second edit required.

### Edge Cases

- **Hash change invalidates persisted/golden artifacts**: If the `feature159Hash` value is changed, any stored hash values (test goldens, readiness/evidence artifacts that embed `ContentId`) may change. This must be detected and reviewed before merge — not discovered after.
- **Removing a placeholder empties a test body**: If deleting an `Expect.isTrue true` would leave a test with no assertions, the test must instead gain a real assertion or be removed entirely (no silently empty tests).
- **Cache-version string composition differs between the two sites**: The two `"rev=150"` usages are in different string-building contexts; the shared constant must produce byte-identical output at both sites, not merely "the same number."
- **Public signature stability**: Any new shared constant must not alter a `.fsi` public surface in a way that constitutes a breaking package change unless explicitly intended.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The project MUST record an explicit decision for the `feature159Hash` offset constant (`RetainedRender.fs:851`): either change it to the standard FNV-1a basis `0xcbf29ce484222325UL` or retain the current value with an in-code comment justifying the divergence.
- **FR-002**: If the `feature159Hash` constant is changed, the project MUST verify that no persisted/golden hash artifact is silently invalidated, and any intended change to computed hash output MUST be explicitly reviewed and accepted.
- **FR-003**: The two `Expect.isTrue true` assertions (`Feature093ParityTests.fs:77`, `TypedMigrationTests.fs:555`) MUST each be replaced with a meaningful, falsifiable assertion or removed; no `Expect.isTrue true` placeholder may remain in the touched files.
- **FR-004**: No test in the touched files may be left without at least one meaningful assertion as a result of FR-003.
- **FR-005**: The layout-cache revision number `150` MUST be defined once and referenced by all four former literal sites: the `"rev=150"` token at `Layout.fs:839` and `:964`, and the `Revision = 150` field at `:847` and `:974`.
- **FR-006**: Centralizing the cache version MUST NOT change the composed layout cache identity/key strings (byte-for-byte stable output at both sites).
- **FR-007**: Each of the three items MUST be independently verifiable, and the codebase MUST build and the full test suite MUST pass after the change set.
- **FR-008**: The change set MUST NOT alter runtime behavior except where explicitly intended and reviewed (the only candidate is the optional `feature159Hash` value change under FR-002).
- **FR-009**: The change set MUST preserve `.fsi` signature discipline and MUST NOT introduce an unintended breaking change to any package's public surface.

### Key Entities

- **`feature159Hash` offset constant**: The 64-bit seed value for the retained-render content-identity FNV-1a fold; currently `1469598103934665603UL`, an outlier versus the standard basis used elsewhere.
- **Layout cache revision (`150`)**: The version number embedded in layout cache identity/key composition that must change atomically when the layout algorithm changes. It currently appears as four hand-duplicated literals — the `"rev=150"` token at `Layout.fs:839`/`:964` and the `Revision = 150` field at `:847`/`:974` — which must collapse to one source of truth.
- **Tautological test assertions**: Two `Expect.isTrue true` statements that currently provide false coverage signal.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `dotnet build` succeeds and the full test suite passes after the change set, with no newly skipped tests.
- **SC-002**: The `feature159Hash` constant has a recorded resolution — either it equals the standard FNV-1a basis, or it carries an explanatory comment — and zero ambiguity remains about which was chosen.
- **SC-003**: Zero `Expect.isTrue true` (or equivalently always-true) assertions remain in the two touched test files, and both affected tests retain at least one meaningful assertion.
- **SC-004**: The revision value `150` is defined once (a single named constant) and all four former sites derive from it; no `"rev=150"` string and no second `Revision = 150` literal is hand-written, and layout cache identity/key strings are byte-identical to the pre-change output.
- **SC-005**: No unintended change to runtime behavior, public `.fsi` surface, or persisted golden/evidence artifacts (any intended hash-output change under FR-002 is the sole reviewed exception).

## Assumptions

- "Next item in the project" refers to Phase 0 of the active code-health refactoring plan recorded in project memory and `docs/reports/2026-06-21-05-19-code-health-refactoring-analysis-and-plan.md`.
- The three Phase 0 items are in scope; all later phases (shared helpers, placement/orphan decisions, `ReadinessStatus`, data-table refactor, god-module splits, type-safety hardening) are explicitly out of scope for this feature.
- The standard 64-bit FNV-1a offset basis intended throughout the codebase is `0xcbf29ce484222325UL` (= `14695981039346656037UL`), as evidenced by `Composition.fs:157` and `Control.fs:2454`/`2830`.
- The maintainer (repository owner) is available to make the FR-001 fix-vs-document decision if it cannot be settled from existing evidence; the recommended default, absent a reason, is to align to the standard basis with golden review.
- File/line references reflect the tree at report time (2026-06-21) and will be re-confirmed during implementation.

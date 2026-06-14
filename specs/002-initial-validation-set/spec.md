# Feature Specification: Define the Initial Validation Set (Migration Stage R3)

**Feature Branch**: `002-initial-validation-set`

**Created**: 2026-06-14

**Status**: Draft

**Input**: User description: "next feature in FS.GG migration"

## Context

This is the next increment of the FS.GG.Rendering migration after Stage R2 (Define
product shape, completed in feature `001-define-product-shape`). The migration is staged
R1 → R8 in the active rendering implementation plan. R1 (fresh repo) and R2 (product
shape) are done; the next uncompleted stage is **R3 — Define the initial validation
set**: deciding *which* tests and checks are worth importing from the source repository
(FS-Skia-UI) **before** copying the full test surface, and recording a justification for
each.

This feature produces decision artifacts — justification records and the resulting
bounded validation set — not runtime code or imported tests. Copying the selected tests
is the later Stage R4; building the rendering test harness is Stage R5. "Users" are the
maintainers who decide the set and the people who perform the source import.

"Deliberately light" means not bulk-importing the source repo's several-hundred legacy
tests without justification. It does **not** mean skimping on test infrastructure: the
rendering harness is deliberate infrastructure recorded with its own justification, and
the default local tier must stay fast enough to actually run.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Every candidate check has a justified decision (Priority: P1)

A maintainer reviews the source repository's test and check surface and, for each
candidate, records why it should be imported now, deferred, archived, or rewritten
smaller — so no check is carried over (or dropped) without a reason.

**Why this priority**: This is the core of R3. Without per-check justification, the team
either bulk-imports a heavy legacy suite or loses coverage silently. The justification
records are the prerequisite for a deliberate Stage R4 import. Even if nothing else
ships, a complete set of justified decisions makes the import actionable.

**Independent Test**: Pick any candidate check from the source surface; the record states
its product contract, failure mode, owner, frequency, cost, and a decision — with no
candidate left undecided.

**Acceptance Scenarios**:

1. **Given** the validation-set records, **When** a maintainer looks up any candidate
   test or check, **Then** it has a justification record with all six fields (product
   contract, failure mode, owner, frequency, cost, decision).
2. **Given** a candidate, **When** its decision is read, **Then** the decision is exactly
   one of: import now, defer, archive, or rewrite smaller.
3. **Given** a check that protects a real contract but is expensive/oppressive, **When**
   it is decided, **Then** the decision is "rewrite smaller" and the smaller form is
   described, rather than importing it as-is or dropping it.

---

### User Story 2 - The active set is bounded and frequency-labeled (Priority: P2)

A contributor needs a default local validation tier that is fast enough to run on every
change, with heavier and release-only checks clearly separated so routine work isn't
slowed by them.

**Why this priority**: A validation set that is too heavy won't be run, defeating its
purpose. Bounding the active set and labeling each member's frequency is what keeps the
inner loop fast while still allowing on-demand and release checks. Builds on US1 but is
independently valuable.

**Independent Test**: Read the resulting set; every "import now" member carries a
frequency label, the local inner-loop subset is explicitly enumerated, and release-only
checks are listed separately with no overlap.

**Acceptance Scenarios**:

1. **Given** the initial validation set, **When** a contributor scans it, **Then** each
   member is labeled with a frequency (local inner loop, CI, release-only, or
   manual/advisory).
2. **Given** the set, **When** the local inner-loop subset is identified, **Then** it is
   small enough to run as part of routine work and contains only locally-labeled checks.
3. **Given** the set, **When** release-only checks are listed, **Then** they are clearly
   separated from local development checks with no item in both.

---

### User Story 3 - Deferred work is captured, not lost (Priority: P3)

Deferred and archived checks, and the rendering test harness, are recorded so they are
discoverable later without becoming active obligations that block routine work.

**Why this priority**: The risk of "deliberately light" is that deferred coverage is
forgotten. A ledger of deferred/archived items, plus a distinct justification for the
harness as deliberate infrastructure, preserves intent without re-imposing the heavy
suite. Lower priority because it protects future work rather than unblocking the import
itself.

**Independent Test**: Confirm every non-imported candidate appears in the deferral/archive
ledger with a reason, and that the harness has its own infrastructure justification record
distinct from imported legacy tests.

**Acceptance Scenarios**:

1. **Given** the ledger, **When** a deferred or archived check is reviewed, **Then** it
   has a recorded reason and is explicitly marked as not an active obligation (does not
   block routine product work).
2. **Given** the artifacts, **When** the rendering test harness is reviewed, **Then** it
   appears as a deliberate-infrastructure justification record (built at Stage R5;
   display-agnostic skeleton may scaffold earlier), not as an imported legacy test.

### Edge Cases

- What happens when a test exists but its product contract is unclear? It is recorded as
  an explicit open decision (defer with options), not imported by default.
- How is a generated fixture handled that no longer represents a current product
  contract? It is archived with a reason, not imported.
- What happens when a valuable check is too oppressive to run routinely? It is rewritten
  smaller (with the smaller form described) before being added to the active set, or
  labeled release-only/on-demand.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST produce a justification record for each candidate test or
  check with all six fields: product contract, failure mode, owner, frequency, cost, and
  a decision.
- **FR-002**: Each record's decision MUST be exactly one of: import now, defer, archive,
  or rewrite smaller.
- **FR-003**: The feature MUST define the resulting initial validation set (the
  "import now" items) explicitly and assert it is small enough for routine local product
  work.
- **FR-004**: Each active-set member MUST carry a frequency label (local inner loop, CI,
  release-only, or manual/advisory), and release-only checks MUST be listed separately
  from local development checks with no overlap.
- **FR-005**: Deferred and archived checks MUST be captured in a discoverable ledger, each
  with a reason, and explicitly marked as not active obligations (they do not block
  routine work).
- **FR-006**: The rendering test harness MUST be recorded as deliberate infrastructure
  with its own justification record (decision: build at Stage R5; its display-agnostic
  parts — environment probe, CLI skeleton, evidence schema — MAY scaffold earlier), and
  MUST NOT be treated as an imported legacy test.
- **FR-007**: Candidate coverage MUST include at least: focused unit tests for current
  runtime behavior; public API surface-drift checks; package/consumer checks; template
  pack/install/instantiate checks; docs build checks; broad historical readiness reports;
  and generated fixtures.
- **FR-008**: The feature MUST apply the plan's default decisions unless a specific
  candidate justifies otherwise: import focused runtime unit tests; import API and package
  checks only when they protect current consumers; import template checks that simulate
  real generated products; defer broad historical readiness reports; archive stale
  generated fixtures; rewrite oppressive checks smaller before importing.
- **FR-009**: All artifacts MUST be definitions/decisions only. This feature MUST NOT copy
  tests or source, MUST NOT build the harness (Stage R4/R5 respectively), and MUST NOT
  reintroduce removed governance machinery (evidence-audit gates, synthetic-evidence
  ledger, mandatory skill gates).
- **FR-010**: Any candidate whose value or cost cannot be settled MUST be recorded as an
  explicit open decision (defer with options), rather than silently dropped.

### Key Entities *(include if feature involves data)*

- **Justification record**: One candidate test/check with product contract, failure mode,
  owner, frequency, cost, and decision.
- **Initial validation set**: The collection of "import now" records, partitioned by
  frequency (local / CI / release-only / manual-advisory).
- **Deferral/archive ledger**: Non-imported candidates (deferred, archived, or rewrite-
  pending) with reasons; discoverable but non-binding.
- **Harness justification record**: The rendering test harness recorded as deliberate
  infrastructure, distinct from imported legacy tests.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of reviewed candidate checks have a justification record with all six
  fields and a decision; zero candidates remain undecided.
- **SC-002**: The active local inner-loop subset is a **named, enumerated set** (not "all
  tests" or "everything else") containing only locally-labeled checks, runnable as a single
  routine command — so "small enough for routine work" is verified by the explicit
  enumeration, not a subjective judgement.
- **SC-003**: Every active-set member carries a frequency label, and release-only checks
  are listed in a separate group with zero overlap with local checks.
- **SC-004**: Every non-imported candidate appears in the deferral/archive ledger with a
  reason, and a reader can confirm none of them block routine product work.
- **SC-005**: The rendering test harness appears as its own deliberate-infrastructure
  record, clearly distinguished from imported legacy tests.
- **SC-006**: The Stage R3 exit criteria are all satisfiable from these artifacts: the set
  is small enough for routine work, every imported check has a justification record,
  deferred checks are preserved but non-binding, and release-only checks are separated
  from local checks.

## Assumptions

- **Scope = Stage R3 only.** "Next feature in FS.GG migration" is interpreted as the next
  uncompleted migration stage. R1 and R2 are done; R3 (Define the initial validation set)
  is next per the active plan. Importing the selected tests (R4) and building the harness
  (R5) are separate later features and are out of scope here.
- This feature delivers decision documents, not code. No tests or source are copied and
  the harness is not built at this stage.
- The candidate surface is the source repository (FS-Skia-UI) test projects and checks,
  scoped to what the rendering product owns per the R2 module map
  (`docs/product/module-map.md`) and docs-to-import list.
- The plan's default decisions are adopted as the baseline; deviations are justified per
  candidate.
- Package-identity and layering decisions from R2 stand (keep `FS.Skia.UI.*`; the four UI
  layers); this feature does not revisit them.
- "Deliberately light" means not bulk-importing the legacy suite without justification —
  not skimping on infrastructure such as the harness (Stage R5).

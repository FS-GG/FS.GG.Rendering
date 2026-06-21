# Feature Specification: Shared ReadinessStatus (Code-Health Refactoring Phase 3)

**Feature Branch**: `180-shared-readiness-status`

**Created**: 2026-06-21

**Status**: Draft

**Input**: User description: "start next item in the project" — Phase 3 of the whole-repo
code-health refactoring plan: introduce one shared readiness-status vocabulary, collapse the three
near-identical per-feature readiness validators into one parameterized validator, and extract one
shared Markdown/JSON formatting helper — removing the parallel copies that today drift independently.

## Overview

The code-health analysis found the repository is clean of rot but structurally heavy, with the
largest remaining duplication concentrated in the **readiness/evidence reporting** code. Phases 0–2
(features 177–179) fixed safety issues, extracted shared test/util helpers, and settled placement &
ownership. This phase tackles the duplication those phases deferred: a sprawling, copy-forward
**status vocabulary**, three **near-identical readiness validators**, and repeated **Markdown/JSON
formatting helpers**.

Three concrete clusters of duplication, confirmed against the current tree:

1. **Readiness-status vocabulary** is spread across many parallel discriminated unions (e.g.
   `VisualReadinessStatus`, `RetainedInspectionStatus`, `CompositorReadinessStatus`,
   `Feature159/160/161` readiness DUs in `Testing.fs`; `ReadinessDiagnosticStatus` in
   `Diagnostics.fs`), each re-stating the same conceptual cases (accepted / blocked / rejected /
   environment-limited / incomplete / …) with its own `statusText` and `blocksAcceptance` mapper.
   The same accept/block decision logic is re-written ~9–10 times.
2. **Per-feature readiness validators** `Feature159Readiness`, `Feature160ThroughputReadiness`, and
   `Feature161HostLaneReadiness` (all in `Testing.fs`, ~106 / ~118 / ~133 lines) are structurally
   identical — required-scenario checks → missing-artifact collection → diagnostics list → status
   derivation — differing only in their evidence fields and a few domain-specific checks.
3. **Markdown/JSON formatting helpers** (`esc`, `q`, `jsonStringArray`, `countsText`) are copied
   three times inside `Testing.fs` (the Visual, VisualInspection, and RetainedInspection readiness
   modules) and partially again in `Diagnostics.fs`, each a hand-maintained escaper that must stay
   in lockstep for serialized evidence to remain consistent.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This feature changes no runtime behavior of the shipped product; it consolidates internal
> readiness-classification, validation, and formatting code. It carries **no defect/root-cause
> hypothesis to confirm against a running app**. The "real evidence" is the existing regression
> machinery: a clean `dotnet build` of the solution plus the full `dotnet test` run, captured as a
> baseline before any change and diffed after each story lands. Crucially, the serialized evidence
> this code produces (JSON/Markdown readiness reports and golden outputs) MUST remain **byte-stable**
> across the refactor — that byte-for-byte diff is the acceptance signal for every story here.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - One shared readiness-status vocabulary (Priority: P1)

A maintainer adding or reading a readiness check today must learn a different status enum and a
different `statusText`/`blocksAcceptance` mapper for every domain, even though the cases mean the
same thing. This story introduces a single canonical `ReadinessStatus` vocabulary in a low-level
project reachable by all consumers, with one authoritative status-text function and one
accept/blocks-acceptance rule. The existing per-domain readiness DUs are migrated to wrap or alias
the shared type, and the duplicated generic mappers are removed — while each domain's serialized
status strings stay byte-identical.

**Why this priority**: It is the foundation the other two stories build on (the validator and the
formatting helpers both classify and emit status), and it removes the largest source of "same
decision written many ways" drift. Highest correctness payoff for the conceptual core.

**Independent Test**: With only this story implemented, the shared `ReadinessStatus` type exists in
the chosen low-level project, at least one representative per-domain readiness DU is migrated onto
it, the duplicate generic `statusText`/`blocksAcceptance` mappers it replaces are deleted, the
solution builds, the full test suite matches baseline, and every readiness report's serialized
status text is byte-identical to the captured baseline.

**Acceptance Scenarios**:

1. **Given** the pre-change baseline of build output, test results, and serialized readiness reports,
   **When** the shared `ReadinessStatus` vocabulary is introduced and the targeted readiness DUs are
   migrated onto it, **Then** `dotnet build` and `dotnet test` match baseline and every readiness
   report (JSON and Markdown) is byte-identical to baseline.
2. **Given** a readiness status value, **When** its display text and "does this block acceptance"
   decision are requested, **Then** there is exactly one function in the codebase that answers each,
   and the per-domain copies it replaced no longer exist.
3. **Given** a domain whose readiness DU carries cases that are not part of the shared vocabulary
   (genuinely domain-specific outcomes), **When** that DU is migrated, **Then** the domain-specific
   cases are preserved (not forced into the shared type) and only the shared cases are unified.

---

### User Story 2 - One parameterized readiness validator (Priority: P2)

A maintainer extending the performance/readiness validators must today copy one of three
near-identical ~100-line modules and tweak a handful of fields. This story generalizes
`Feature159Readiness`, `Feature160ThroughputReadiness`, and `Feature161HostLaneReadiness` into a
single parameterized validator driven by a per-feature configuration record, then deletes the three
original modules. Each feature's validation continues to produce the same diagnostics, missing-artifact
lists, and final status as before.

**Why this priority**: Large, self-contained line reduction with a clear correctness oracle (the
three features' existing readiness outputs), but it depends on the shared status vocabulary from
Story 1 being in place. Second-highest payoff.

**Independent Test**: With this story implemented, one parameterized validator drives all three
features via their config records, the three original `Feature*Readiness` modules are gone, the
solution builds, the full test suite matches baseline, and the readiness verdict + diagnostics +
missing-artifact output for features 159, 160, and 161 are byte-identical to baseline.

**Acceptance Scenarios**:

1. **Given** the captured baseline readiness outputs for features 159, 160, and 161, **When** the
   parameterized validator replaces the three modules, **Then** each feature's status, diagnostics
   list, and missing-artifact list is byte-identical to baseline.
2. **Given** a new hypothetical feature with the same readiness shape, **When** a maintainer wants to
   validate it, **Then** it can be expressed as a single configuration entry without copying the
   validator body.

---

### User Story 3 - One shared Markdown/JSON formatting helper (Priority: P3)

A maintainer changing how readiness evidence is escaped or formatted must today find and update every
copy or risk silent divergence in serialized output. This story extracts the repeated `esc`, `q`,
`jsonStringArray`, and `countsText` helpers into a single shared formatting module and replaces the
copies in `Testing.fs` (and reconciles the partial copies in `Diagnostics.fs`). Where the existing
copies differ in behavior, the differences are reconciled so that all callers' serialized output
stays byte-identical.

**Why this priority**: Smallest of the three clusters and lowest risk, but it removes a real
hand-symmetry hazard (escapers that must stay in lockstep). It is independent of Stories 1–2 and can
land in any order, so it is sequenced last.

**Independent Test**: With this story implemented, there is one shared definition of each formatting
helper, the duplicate copies are deleted, the solution builds, the full test suite matches baseline,
and every serialized readiness/evidence artifact is byte-identical to baseline.

**Acceptance Scenarios**:

1. **Given** the baseline serialized evidence artifacts, **When** all formatting-helper call sites
   are pointed at the shared module, **Then** every artifact is byte-identical to baseline.
2. **Given** a request to change an escaping rule, **When** a maintainer edits the shared helper,
   **Then** all readiness/evidence emitters reflect the change from a single edit.

---

### Edge Cases

- **Domain-specific status cases**: Some status DUs (e.g. fit/clip/coverage/node inspection
  enumerations) are domain enumerations, not readiness verdicts. These are **out of scope** for the
  shared vocabulary and must be left intact — migrating them would be incorrect.
- **Divergent serialized strings**: Where two domains historically emit different display strings for
  the same conceptual status, byte-stability takes precedence — the migration must preserve each
  domain's existing string, even if that means a thin per-domain text projection over the shared type.
- **Behaviorally-different helper copies**: The `Diagnostics.fs` `jsonStringArray`/`countsText`
  copies have different signatures/behavior from the `Testing.fs` copies; reconciliation must not
  change any caller's emitted bytes.
- **Internal-visibility consumers**: Any test project that reaches these types/helpers via
  `InternalsVisibleTo` must continue to compile and pass unchanged.
- **Pre-existing baseline reds**: Pre-existing failing lanes (e.g. Package.Tests / package-feed
  lanes noted in earlier phases) are baseline, not regressions; the bar is "no new failures vs.
  captured baseline," not "all green from zero."

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST define a single canonical readiness-status vocabulary covering the
  shared conceptual cases (the canonical 12: accepted, rejected, blocked, missing, unsupported,
  environment-limited, degraded, incomplete, failed, fallback-only, pending, unknown — see
  `data-model.md`) in a low-level project reachable by all current consumers of readiness status.
- **FR-002**: The system MUST provide exactly one authoritative function for a status's display text
  and exactly one for whether a status blocks acceptance, and MUST remove the duplicate generic
  mappers those replace.
- **FR-003**: Per-domain readiness DUs MUST be migrated to wrap, alias, or reuse the shared
  vocabulary, while preserving any genuinely domain-specific cases that are not readiness verdicts.
- **FR-004**: The three per-feature readiness validators (159, 160, 161) MUST be replaced by one
  parameterized validator driven by a per-feature configuration record, and the original three
  modules MUST be deleted.
- **FR-005**: The repeated Markdown/JSON formatting helpers (`esc`, `q`, `jsonStringArray`,
  `jsonCounts`, `countsText`) MUST be consolidated into one shared module, with all duplicate copies
  removed and behavioral differences reconciled without changing emitted bytes.
- **FR-006**: All serialized readiness and evidence artifacts (JSON and Markdown reports, golden
  outputs) MUST remain **byte-identical** to a baseline captured immediately before the change.
- **FR-007**: The public surface of shipped packages MUST NOT change in ways that break existing
  consumers; any public-type relocation MUST preserve source-compatibility for current call sites
  (e.g. via aliases) or be confirmed unused.
- **FR-008**: Each user story MUST end with a clean `dotnet build` of the solution and a full
  `dotnet test` run that shows no new failures relative to the captured baseline.
- **FR-009**: Each story MUST be independently shippable — landing only Story 1, only Story 3, or
  Stories 1+2 must each leave the repository green relative to baseline.

### Key Entities *(include if feature involves data)*

- **ReadinessStatus**: The shared, canonical set of readiness verdicts. Attributes: the set of
  conceptual cases; a single display-text projection; a single blocks-acceptance rule; a parse from
  text. Replaces the case-and-mapper logic currently re-stated per domain.
- **ReadinessValidatorConfig**: The per-feature configuration record that parameterizes the single
  readiness validator. Attributes: required scenarios/artifacts, the domain-specific checks, and the
  evidence fields that differ between features 159/160/161.
- **FormattingHelper**: The shared Markdown/JSON serialization primitives (escape, quote,
  string-array, counts-text) consumed by every readiness/evidence emitter.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Across the repository there is exactly **one** definition of the readiness display-text
  function and exactly **one** blocks-acceptance rule for the shared vocabulary (down from ~9–10
  parallel copies).
- **SC-002**: The three per-feature readiness validator modules are reduced to **one** parameterized
  validator plus three configuration entries.
- **SC-003**: Each consolidated Markdown/JSON formatting helper has exactly **one** definition (down
  from 3–4 copies).
- **SC-004**: 100% of serialized readiness/evidence artifacts are byte-identical to the pre-change
  baseline.
- **SC-005**: Net source-line count for the touched readiness/reporting code is **strictly reduced**
  relative to baseline (net line delta across the touched `.fs`/`.fsi` files is < 0), with no new test
  failures (the analysis estimates the parallel status cases, three validators, and helper copies
  together as the bulk of Phase 3's payoff).
- **SC-006**: A maintainer can add a same-shaped feature-readiness check as a single configuration
  entry, with no module-body copying, demonstrated by the new validator's parameterization.

## Assumptions

- **Shared module home**: The shared `ReadinessStatus` type and formatting helpers will live in the
  `Diagnostics` project (`FS.GG.UI.Diagnostics`) — it is the lowest project in the dependency chain
  reachable by `Testing`, `SkiaViewer`, and `Scene`, already houses readiness-diagnostic types, and
  introduces no circular dependency. To be confirmed in planning.
- **Byte-stability is the binding constraint**: When line-reduction and byte-stable output conflict,
  byte-stable output wins. Consolidation that would change any emitted artifact is out of scope for
  this feature and must instead preserve existing strings (e.g. via a thin per-domain projection).
- **Scope is readiness/reporting duplication only**: Domain enumeration DUs (visual fit/clip/coverage,
  retained-node, damage inspection) are explicitly excluded from the shared vocabulary.
- **`System.Text.Json` standardization is opportunistic**: Standardizing JSON emission on
  `System.Text.Json` is desirable "where practical" but is subordinate to byte-stability; it is not
  required where it would change emitted bytes.
- **Tiering / `.fsi` discipline**: Public signature files are updated only as needed to expose the
  new shared surface; the change should remain as low-tier as the byte-stability and public-surface
  constraints allow.
- **Baseline capture**: A full `dotnet build` + `dotnet test` and a snapshot of serialized
  readiness/evidence artifacts are captured before any edits and used as the diff oracle throughout.
- **Predecessor phases complete**: Phases 0–2 (features 177, 178, 179) are merged; this phase builds
  on the settled placement/ownership from Phase 2.

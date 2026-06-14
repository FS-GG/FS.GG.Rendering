# Feature Specification: Verify Imported Rendering & Controls Mechanisms

**Feature Branch**: `006-verify-imported-mechanisms`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "we imported lots of code from fs-skia-ui. that rendering and controls code is pretty complicated and is not battletested. do an indepth code analysis, testing performance and other mechanisms if they really work as advertised. be thorough and comprehensive."

## Context & Problem

A large body of rendering and controls code was imported from the sibling repo `fs-skia-ui` (migration Stages R4–R6). That code advertises sophisticated behavioral and performance mechanisms — keyed reconciliation, incremental layout, memoization, picture/text caches, a backend SKPicture replay cache, per-identity animation clocks, damage-rectangle tracking, virtualization, present-mode selection, and frame-rate capping. These mechanisms are intricate, were imported rather than grown here, and have **not been independently verified in this repository**. Existing tests prove the code builds and runs; they do not yet prove the advertised mechanisms deliver the work-reduction and behavioral guarantees they claim.

This feature is a **comprehensive audit**: enumerate every advertised mechanism, state its claim in falsifiable terms, then verify each claim with real evidence — distinguishing "works as advertised," "works but claim is overstated," "does not work / is a no-op," and "cannot be verified yet (and why)." The deliverable is trustworthy knowledge about what is safe to rely on, plus a defect list where reality diverges from the advertisement.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Auditable claims inventory (Priority: P1)

As the maintainer, I want a single inventory that lists every advertised mechanism in the imported rendering and controls code, the specific claim each one makes, and where the claim lives in the source — so the audit has a complete, agreed-upon surface and nothing intricate is silently trusted.

**Why this priority**: You cannot verify what you have not enumerated. The inventory is the backbone every later verification hangs off; on its own it already converts vague unease ("this code is complicated and untested") into a concrete, reviewable list of testable assertions.

**Independent Test**: Review the inventory against the imported source; confirm each performance/behavioral mechanism (reconciliation, incremental layout, memo cache, picture cache, text cache, replay cache, animation clock, damage tracking, virtualization, present mode, frame-rate cap, fingerprinting) appears with a falsifiable claim and a source reference. Delivers value as a standalone audit map even before any test is written.

**Acceptance Scenarios**:

1. **Given** the imported rendering and controls code, **When** the inventory is produced, **Then** every advertised mechanism is listed with: its name, the precise claim restated as a verifiable statement, the source location, and an initial verification status of "unverified."
2. **Given** a mechanism whose claim is ambiguous or untestable as written, **When** it is catalogued, **Then** it is flagged as "claim needs sharpening" with the ambiguity named, rather than being recorded as a vague pass.
3. **Given** the inventory, **When** a reviewer cross-checks it against the source tree, **Then** no advertised performance or behavioral mechanism in the imported code is missing from the list.

---

### User Story 2 - Behavioral correctness verified against real code (Priority: P1)

As the maintainer, I want each mechanism's **correctness** claim verified by tests that exercise the real imported code and would fail if the mechanism were broken — so I know that turning a mechanism on never changes the rendered result, only the work done to produce it.

**Why this priority**: Performance is meaningless if the mechanism produces wrong output. Correctness is the floor: a cache that returns stale pixels, a reconciler whose diff+apply doesn't reproduce the target tree, or an incremental layout that disagrees with full layout is a defect regardless of speed. Several mechanisms already advertise "parity oracle" switches (force-all-miss flags); this story turns those into enforced evidence.

**Independent Test**: For each correctness-bearing mechanism, run it with the optimization enabled and again with it bypassed/forced-miss, on the same inputs, and assert the observable results are identical. Can be tested headlessly for the deterministic mechanisms.

**Acceptance Scenarios**:

1. **Given** the memoization, picture, text, and replay caches, **When** each is run with caching enabled versus its always-miss oracle, **Then** the produced output (scene/pixels/measurements as applicable) is identical, proving the cache is transparent.
2. **Given** the reconciler, **When** a diff is computed between two control trees and then applied, **Then** the result is structurally equal to the target tree, for both keyed and positional children, across a broad range of generated tree pairs.
3. **Given** incremental layout, **When** a set of nodes is changed and incremental evaluation is run, **Then** the resulting geometry equals a full layout evaluation of the same final tree.
4. **Given** the animation clock and declarative animation sampling, **When** the same inputs are sampled at the same time points, **Then** outputs are identical (deterministic), and a settled animation lowers to a result indistinguishable from the equivalent static scene.
5. **Given** any mechanism that cannot be exercised in the current environment, **When** its correctness test is defined, **Then** it is recorded as explicitly skipped with written rationale rather than reported as passing.

---

### User Story 3 - Performance claims measured, not assumed (Priority: P2)

As the maintainer, I want each mechanism's **work-reduction** claim measured with before/after evidence — so a cache that is correct but never hits, or an "incremental" path that quietly recomputes everything, is exposed rather than trusted.

**Why this priority**: The central worry in the request is that mechanisms may not "really work as advertised." A correct-but-ineffective optimization is the most dangerous kind: it passes correctness tests, adds complexity and risk, and delivers none of its promised benefit. This story makes effectiveness observable using the work-reduction counters the code already exposes (recomputed/remeasured node counts, cache hit/miss counts, dirty-rect area, materialized-vs-total items, draw-call counts).

**Independent Test**: Construct representative scenarios (a small localized change in a large tree; a repeated render with no change; a scroll over a large virtualized list) and assert the reported work-reduction metrics move in the advertised direction by a meaningful margin versus a baseline that disables the mechanism.

**Acceptance Scenarios**:

1. **Given** a large control tree with a single localized change, **When** an incremental render frame runs, **Then** the recomputed/remeasured/repainted node counts and dirty area are a small fraction of the full-tree baseline, not equal to it.
2. **Given** a repeated render of unchanged content, **When** the caches are enabled, **Then** reported cache hit rates are high and recomputation counts are near zero across the steady-state frames.
3. **Given** a virtualized collection larger than the viewport, **When** it is rendered, **Then** the count of materialized items is bounded by what the viewport needs rather than the logical item count.
4. **Given** any mechanism whose measured effect is absent or negligible, **When** the measurement runs, **Then** the result is recorded as a finding ("no measurable effect" / "no-op under tested conditions") with the scenario that exposed it.
5. **Given** timing-based claims (frame pacing, frame-rate cap) that depend on capability tiers not present in the current environment, **When** they cannot be faithfully measured, **Then** they are deferred to the appropriate capability tier and disclosed as not-yet-measured rather than assumed.

---

### User Story 4 - Findings report with severity and recommendations (Priority: P2)

As the maintainer, I want a consolidated report that, per mechanism, states the verdict (works as advertised / overstated / not working / unverifiable-and-why), the evidence behind the verdict, and a recommended action — so the audit converts into decisions about what to trust, fix, simplify, or remove.

**Why this priority**: The audit's purpose is to drive decisions. A pile of test results without a verdict and recommendation leaves the maintainer with the same uncertainty they started with. This is P2 because it depends on Stories 1–3 producing evidence, but it is what makes the whole effort actionable.

**Independent Test**: Read the report and confirm every inventoried mechanism has a verdict, a pointer to its supporting evidence, a severity if it failed, and a concrete recommendation.

**Acceptance Scenarios**:

1. **Given** completed verification, **When** the report is produced, **Then** every mechanism from the inventory has one of the defined verdicts and a link to the evidence that justifies it.
2. **Given** a mechanism that diverges from its advertisement, **When** it is reported, **Then** it carries a severity (e.g., correctness defect > silent no-op > overstated benefit > cosmetic) and a recommended action (fix / simplify / remove / re-scope claim / defer to capability tier).
3. **Given** the report, **When** a reader wants to reproduce a verdict, **Then** the report names the test or measurement and how to run it.

---

### Edge Cases

- **Mechanism advertised but unreachable from the wired path**: a mechanism may exist in source but never be invoked by the current render path. The audit must detect "present but dead" and report it distinctly from "present and working."
- **Determinism violations**: any mechanism that claims determinism but produces different output across runs (e.g., hidden wall-clock, ordering, or hash collisions in fingerprints) is a correctness defect even if the difference is rare; verification must include repeated runs and adversarial inputs.
- **Cache key incompleteness**: a cache that omits a field from its key can return wrong results only for specific inputs; verification must probe inputs that differ solely in fields the key might miss.
- **Capability-dependent mechanisms** (live window, GPU readback, faithful frame pacing, kernel input): must degrade-and-disclose per the project's tiered-evidence model rather than being silently skipped or falsely passed.
- **Measurement that perturbs the result**: enabling instrumentation/readback to measure a mechanism must not itself change the work being measured (or the perturbation must be accounted for).
- **Scenario where an optimization is correctly a no-op** (e.g., nothing changed, so incremental does nothing): must be distinguished from a broken optimization that fails to engage when it should.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The audit MUST produce a complete inventory of advertised mechanisms in the imported rendering and controls code, each with a restated falsifiable claim, a source reference, and a verification status.
- **FR-002**: The inventory MUST cover, at minimum, the mechanisms identified in the imported code: keyed reconciliation, incremental layout, memoization cache, picture cache, text-measure cache, backend SKPicture replay cache, scene fingerprinting, per-identity animation clock, declarative animation sampling, animation tick gating, damage-rectangle tracking, virtualization, present-mode selection, and frame-rate capping.
- **FR-003**: For every mechanism with a correctness claim, the audit MUST provide a test that exercises the real imported code and fails if the mechanism is broken.
- **FR-004**: For caches and other transparent optimizations, the audit MUST verify output equivalence between the optimization enabled and the optimization bypassed (using the code's always-miss/disable oracles where they exist, or an equivalent bypass).
- **FR-005**: For the reconciler, the audit MUST verify that diff-then-apply reproduces the target tree for keyed and positional children across a broad, generated range of inputs.
- **FR-006**: For incremental layout, the audit MUST verify that incremental evaluation yields the same geometry as full evaluation of the same final tree.
- **FR-007**: For mechanisms claiming determinism, the audit MUST verify identical output across repeated runs and across inputs designed to provoke ordering, timing, and hash-collision sensitivity.
- **FR-008**: For every mechanism with a work-reduction claim, the audit MUST measure the relevant work metric with the mechanism enabled versus a baseline with it disabled, and report whether the advertised reduction is actually realized and by what margin.
- **FR-009**: The audit MUST exercise cache-key completeness by probing inputs that differ only in fields the cache key might omit, and report any case where a stale result is returned.
- **FR-010**: The audit MUST detect and report mechanisms that are present in source but not reached by the current render/control path ("present but dead").
- **FR-011**: Any verification that cannot be performed in the current environment MUST be recorded as explicitly skipped or deferred, with written rationale and the capability tier required to complete it — never reported as passing. (Constitution Principle VI: no overclaiming.)
- **FR-012**: Any synthetic substitute used because real evidence is unavailable MUST be disclosed at the use site and in the findings, per the project's synthetic-evidence rules.
- **FR-013**: The audit MUST produce a consolidated findings report assigning each mechanism a verdict (works as advertised / benefit overstated / not working or no-op / unverifiable-and-why), the supporting evidence, a severity for any divergence, and a recommended action.
- **FR-014**: The report MUST make each verdict reproducible by naming the test or measurement that produced it and how to run it.
- **FR-015**: The audit MUST prefer real dependencies (real imported code, real layout/scene/measurement, real GL surface where the tier permits) over mocks, consistent with the project's evidence principles.

### Key Entities

- **Mechanism**: an advertised behavioral or performance feature in the imported code. Attributes: name, category (correctness / performance / timing), source reference, advertised claim, restated falsifiable claim.
- **Claim**: the specific, falsifiable assertion a mechanism makes (e.g., "cache-enabled output equals cache-disabled output"; "incremental remeasures only changed subtrees"). Attributes: statement, verification method, status.
- **Verification**: an executed test or measurement bound to a claim. Attributes: method (correctness test / property test / measurement), inputs/scenario, result, evidence reference, environment/tier, skipped-or-deferred flag with rationale.
- **Finding**: a verdict about a mechanism. Attributes: verdict category, severity (if divergent), evidence reference, recommended action.
- **Audit Report**: the consolidated collection of findings across all mechanisms, plus a coverage summary (how many mechanisms verified / overstated / failing / unverifiable).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of advertised performance and behavioral mechanisms in the imported rendering and controls code appear in the inventory with a restated falsifiable claim and a source reference.
- **SC-002**: Every inventoried mechanism ends the audit with exactly one verdict and a pointer to its supporting evidence; none remain "unverified."
- **SC-003**: Every correctness claim is backed by a test that demonstrably fails when the mechanism is deliberately broken or bypassed (the test has proven discriminating power, not just a green run).
- **SC-004**: Every work-reduction claim has a measured enabled-vs-disabled comparison, and each is classified as "reduction realized," "negligible/no effect," or "deferred to capability tier" with the supporting numbers recorded.
- **SC-005**: Every mechanism that cannot be verified in the current environment is recorded as skipped/deferred with written rationale and the required capability tier — zero mechanisms are reported as passing without evidence.
- **SC-006**: Every divergence between advertisement and reality is captured as a finding with a severity and a concrete recommended action.
- **SC-007**: A reader can reproduce any verdict by following the test/measurement reference in the report without further guidance.
- **SC-008**: The audit surfaces at least the count of correctness defects, silent no-ops, and overstated claims found (which may legitimately be zero), so the maintainer knows the audit looked for each failure mode rather than only confirming successes.

## Assumptions

- **Change classification**: This audit is treated as a **Tier 2 (internal) effort** — it adds verification, measurement, and a report without changing public API surface. Defects it uncovers may each require their own follow-up change (a fix could be Tier 1 if it alters public behavior); those are out of scope here beyond being recorded as recommendations.
- **Scope boundary**: The audit covers the rendering and controls mechanisms imported from `fs-skia-ui` (Scene, Layout, Controls including RetainedRender/Reconcile, SkiaViewer caches/present-mode, Elmish animation tick). Build tooling, CI wiring (covered by spec 005), and unrelated modules are out of scope except where they host an advertised mechanism.
- **Evidence environment**: Deterministic, headless verification (reconciliation, incremental layout, caches, fingerprinting, animation sampling) is expected to run in the default local tier. Capability-dependent claims (live window/input, GPU readback faithfulness, faithful frame pacing, frame-rate-cap timing) are verified only where the corresponding tier (T1–T3, T-uinput) is available, and are otherwise deferred-and-disclosed.
- **Reuse of existing seams**: The audit reuses the code's existing oracle/disable switches (always-miss flags, cache-enabled toggles) and work-reduction counters (recomputed/remeasured/materialized counts, hit/miss counts, dirty area, draw-call counts) rather than introducing new instrumentation where these already exist.
- **"As advertised" source of truth**: A mechanism's advertised claim is taken from its source/signature documentation and the feature notes referenced in the imported code; where no explicit claim exists, the audit infers the reasonable intended claim and flags that it was inferred.
- **No production behavior change**: Running the audit MUST NOT alter the framework's runtime behavior for consumers; verification toggles and instrumentation are confined to the test/harness paths.

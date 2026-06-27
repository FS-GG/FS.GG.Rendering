# Feature Specification: Clear the disclosed pre-existing test reds and baseline flakiness

**Feature Branch**: `203-fix-disclosed-test-reds`

**Created**: 2026-06-27

**Status**: Draft

**Input**: User description: "fix the disclosures."

## Context (why this exists)

Feature 202 shipped with an honest readiness ledger that **disclosed**, rather than fixed, a set of
test failures it found already red in the repository — because they were outside that feature's scope
and not caused by it. Those disclosures are now a standing liability: a contributor who runs the
comprehensive baseline (`scripts/baseline-tests.fsx`, the runner that sweeps **every** test project —
solution + `Package.Tests` + samples) sees a noisy result of ~4–5 red projects plus a nondeterministic
GL test. That noise has two costs:

1. **Real regressions hide in known-red noise.** When the baseline is never green, a newly-introduced
   failure is easy to miss at merge — the exact failure mode the comprehensive baseline exists to
   prevent.
2. **Every future feature must re-disclose the same caveats.** Honest disclosure is correct, but
   repeating "these were already red, not my change" on every feature is friction that a one-time
   cleanup removes.

The disclosed conditions (from `specs/202-fix-build-fsx-engine/readiness/quickstart-evidence.md`) are:

- **Design-system validation report missing** — `tests/Package.Tests` *Feature128* GV-1…GV-7 (7
  failures) all fail for one reason: the generated report
  `specs/128-design-system-template-param/readiness/design-system-template-validation.md` is absent, so
  the gate cannot confirm `overall=PASS`.
- **Stale sample package pins** — `tests/Package.Tests` *Feature163* and each sample's own pin check
  fail because a sample pins an old `FS.GG.UI.*` version (e.g. `AntShowcase.Core` pins
  `FS.GG.UI.Themes.AntDesign 0.1.32-preview.1` while the source-controlled version is `0.1.36-preview.1`).
- **Sample internal assertion drift** — sample suites (`AntShowcase.Tests`, `ControlsGallery.Tests`,
  `SecondAntShowcase.Tests`) carry count/snapshot assertions that no longer match current reality
  (e.g. `expected 96, actual 97`).
- **SkiaViewer GL flakiness** — `tests/SkiaViewer.Tests` fails nondeterministically (observed 0, then
  2, then 0 failures of ~207 across consecutive runs) on GL/window-system-sensitive smoke tests.

This feature makes the comprehensive baseline **genuinely green and deterministic**, so disclosures are
no longer required (or, for any irreducible environment limit, the residue is explicitly and narrowly
bounded rather than silently red).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sample package pins and the package-feed gate are coherent (Priority: P1) 🎯 MVP

A maintainer runs the package-feed validation and each sample's pin check. Every `FS.GG.UI.*` pin in
every sample matches the source-controlled package version; the *Feature163* gate and the per-sample
pin assertions pass.

**Why this priority**: Stale pins are the most clearly-correct, lowest-risk fix and they unblock the
largest share of the red projects (the *Feature163* gate plus three sample suites). It is the highest
signal-to-effort slice and a self-contained MVP.

**Independent Test**: Run `tests/Package.Tests` *Feature163* and each sample test project; confirm no
"pin does not match source-controlled version" failure remains.

**Acceptance Scenarios**:

1. **Given** the repository at a coherent package version, **When** the package-feed validation runs,
   **Then** every sample's `FS.GG.UI.*` pin equals the source-controlled version and the gate passes.
2. **Given** a sample whose pin was stale, **When** its pin is brought current and its test suite runs,
   **Then** the pin-coherence assertions pass without weakening any assertion.

---

### User Story 2 - The design-system validation gate confirms a present, current report (Priority: P1)

A maintainer runs `tests/Package.Tests`; the *Feature128* design-system template validation gate
(GV-1…GV-7) finds the design-system validation report present and reporting `overall=PASS`, and the
seven gates pass.

**Why this priority**: Seven of the eight `Package.Tests` failures collapse to this single missing
artifact; resolving it makes `Package.Tests` — the release-surface gate — green. High value, contained
cause.

**Independent Test**: Run `tests/Package.Tests` filtered to *Feature128*; confirm all seven GV gates
pass and none report "design-system validation report missing".

**Acceptance Scenarios**:

1. **Given** the design-system validation report is available and current, **When** the *Feature128*
   gate runs, **Then** GV-1…GV-7 each pass and the overall result is `PASS`.
2. **Given** the report would otherwise be absent in a fresh checkout, **When** a contributor follows
   the documented path, **Then** the report is produced (or its presence is guaranteed by the gate's
   own setup) so the gate is not red by default.

---

### User Story 3 - Sample internal assertions match current reality (Priority: P2)

A maintainer runs the sample test suites; their internal count/snapshot assertions reflect the current
state of the samples, with no stale "expected N, actual N+1" drift, and the suites pass.

**Why this priority**: These are genuine but low-risk staleness fixes that remove the remaining sample
reds once pins (US1) are current. Lower priority than the gate-level fixes because they are localized
to sample fixtures.

**Independent Test**: Run `AntShowcase.Tests`, `ControlsGallery.Tests`, `SecondAntShowcase.Tests`;
confirm zero failures and that each drifted assertion was corrected to the true current value (not
deleted or loosened).

**Acceptance Scenarios**:

1. **Given** a sample assertion that drifted (e.g. an inventory count), **When** the assertion is
   updated to the current true value, **Then** the suite passes and the assertion still verifies a real
   property (it was corrected, not weakened or removed).

---

### User Story 4 - The comprehensive baseline is deterministic (no flaky GL failures) (Priority: P3)

A maintainer runs the comprehensive baseline repeatedly; `tests/SkiaViewer.Tests` produces the **same**
result every run — no nondeterministic GL/window-system failures that appear and vanish between runs.

**Why this priority**: Flakiness is the subtlest and most environment-sensitive item, and the hardest
to fully eliminate; it is sequenced last. A non-flaky baseline is essential to the feature's core
purpose (a trustworthy green signal) but should not block the deterministic fixes.

**Independent Test**: Run `tests/SkiaViewer.Tests` several consecutive times; confirm identical
pass/fail outcomes each run, with no test that fails in one run and passes in the next.

**Acceptance Scenarios**:

1. **Given** the SkiaViewer suite is run multiple times in the same environment, **When** the runs
   complete, **Then** the set of passing tests is identical across runs (deterministic).
2. **Given** a GL/window-system capability is genuinely unavailable in the environment, **When** the
   affected test runs, **Then** it deterministically and explicitly reports the unsupported condition
   (skipped-with-rationale or a stable explicit outcome) rather than failing nondeterministically.

---

### Edge Cases

- A sample pin is stale because the source-controlled version legitimately moved ahead — the fix is to
  advance the pin, not to roll back the source version.
- The design-system validation report is a generated readiness artifact ignored by version control —
  the gate must not depend on an uncommitted, never-regenerated file being present by luck.
- A count/snapshot assertion drifted because the sample legitimately gained an item — the assertion is
  corrected to the new true value, never loosened to "greater-than" or deleted to force green.
- A GL test failure is a *real* defect, not flakiness — determinism work must not mask a genuine
  failure as "unsupported environment."
- Some residue may be irreducibly environment-bound (no display/GPU). Such residue must be explicitly
  and narrowly bounded (skipped-with-rationale), never left as a silent intermittent red.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every `FS.GG.UI.*` package pin in every sample MUST equal the source-controlled package
  version, so the *Feature163* package-feed gate and every per-sample pin assertion pass.
- **FR-002**: The *Feature128* design-system template validation gate (GV-1…GV-7) MUST pass with the
  validation report present and reporting `overall=PASS`, and MUST NOT be red by default in a fresh
  checkout because the report is absent.
- **FR-003**: Drifted internal assertions in the sample test suites MUST be corrected to their current
  true values; assertions MUST NOT be weakened, broadened, or deleted to achieve green (the constitution
  forbids greening a build by weakening an assertion).
- **FR-004**: `tests/SkiaViewer.Tests` MUST produce deterministic results across repeated runs in the
  same environment — a test MUST NOT fail in one run and pass in the next.
- **FR-005**: Any failure that is genuinely caused by an unavailable environment capability (e.g. no
  GL/display) MUST be represented as an explicit, deterministic skipped-with-rationale (or stable
  unsupported) outcome, not as an intermittent failure and not as a silently-passing assertion.
- **FR-006**: The comprehensive baseline runner (`scripts/baseline-tests.fsx`, sweeping solution +
  `Package.Tests` + samples) MUST report zero red projects on a clean run, with any irreducible
  environment-limited residue disclosed as explicitly skipped rather than failed.
- **FR-007**: No previously-passing test may regress; the fix MUST be additive/corrective only, with the
  full set of public-surface, governance, and evidence gates remaining green.
- **FR-008**: The feature 202 readiness ledger's pre-existing-red and flaky disclosures MUST become
  obsolete — i.e. the conditions they described are resolved, or any remaining residue is reduced to an
  explicitly bounded, deterministic skip with written rationale.

### Key Entities *(include if feature involves data)*

- **Comprehensive baseline**: the full red/green set produced by `scripts/baseline-tests.fsx` across
  every test project; the trustworthy signal this feature restores to green.
- **Sample package pin**: a sample project's `FS.GG.UI.*` version reference that must track the
  source-controlled package version.
- **Design-system validation report**: the generated readiness artifact the *Feature128* gate audits.
- **Drifted assertion**: a sample test assertion whose expected value no longer matches current reality.
- **Flaky GL test**: a `SkiaViewer.Tests` case whose pass/fail outcome varies between runs in one
  environment.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A clean run of the comprehensive baseline reports **0 red projects** (down from the 4–5
  disclosed by feature 202).
- **SC-002**: `tests/Package.Tests` passes **100% of its cases** (the 8 disclosed failures —
  7× Feature128 + 1× Feature163 — are eliminated) with no assertion weakened.
- **SC-003**: Each previously-red sample suite (`AntShowcase.Tests`, `ControlsGallery.Tests`,
  `SecondAntShowcase.Tests`) passes **100% of its cases**.
- **SC-004**: Running `tests/SkiaViewer.Tests` **5 consecutive times** yields an identical pass set
  every time (zero tests that flip between pass and fail) — i.e. 0 flaky outcomes observed.
- **SC-005**: Any residual environment-limited test is reported as an **explicit skip with written
  rationale**, and the count of such skips is stated; zero tests remain intermittently red.
- **SC-006**: The pre-existing-red and flaky disclosures in feature 202's readiness ledgers no longer
  apply — re-running the feature-202 acceptance baseline shows the previously-disclosed conditions
  resolved or bounded per SC-005.

## Assumptions

- **Scope is the test debt feature 202 disclosed**, not a broader test-suite audit: the four named
  conditions (sample pins, design-system report, sample assertion drift, SkiaViewer flakiness) define
  the boundary. New, unrelated failures discovered along the way are recorded but not necessarily fixed
  here.
- **Stale pins are corrected forward** to the current source-controlled versions; this feature does not
  re-litigate the package-versioning model (feature 201/202 baseline is the working assumption).
- **Drifted assertions are corrected to true current values** — the samples themselves are assumed
  correct and current; the test expectations are what lagged.
- **The design-system validation report is regenerable** from the existing documented mechanism; if
  generation is itself environment-gated, the gate is made robust to a fresh checkout (so it is not red
  purely because an uncommitted artifact is missing) rather than requiring every contributor to
  hand-run a generator.
- **SkiaViewer flakiness is environment-sensitivity, not a hidden product defect** (feature-202
  investigation showed the failure count varies run-to-run with no source change); the remedy is
  deterministic handling (stabilize or explicit unsupported-skip), not acquiring GPU/display hardware.
- The comprehensive baseline runner and the existing gates (public-surface, governance, evidence) are
  the source of truth for "green"; this feature does not introduce a new validation harness.

## Dependencies

- The comprehensive baseline runner `scripts/baseline-tests.fsx` and the test projects it sweeps
  (`tests/Package.Tests`, `samples/**/*.Tests`, `tests/SkiaViewer.Tests`).
- The design-system template validation mechanism referenced by the *Feature128* gate
  (`scripts/validate-design-system-template.fsx` and its readiness output).
- The coherent local-feed / single-version-pin model established by features 201–202.

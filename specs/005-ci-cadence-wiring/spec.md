# Feature Specification: Wire Validation into CI at Chosen Cadences (Migration Stage R6)

**Feature Branch**: `005-ci-cadence-wiring`

**Created**: 2026-06-14

**Status**: Draft

**Input**: User description: "next phase in fs.gg"

## Context

This is the next increment of the FS.GG.Rendering migration after Stage R5 (Build the
Rendering Test Harness, feature `004-rendering-harness`). The migration is staged R1 → R8 in
the active rendering implementation plan. R1 (fresh repo), R2 (product shape), R3 (validation
set), R4 (source import), and R5 (harness) are done or in hand; the next uncompleted stage is
**R6 — Wire the validation set into CI at chosen frequencies**.

Today the repository defines *which* checks matter and *how often* each should run — the
frequency partition already lives in [`docs/validation/validation-set.md`](../../docs/validation/validation-set.md)
(Local inner loop / CI on push-PR / Release-only / Manual-advisory) — but nothing executes those
checks automatically. There is **no CI configured** (no `.github/workflows`). R6 turns the
existing cadence labels into real automation: each check runs on the trigger that matches its
declared frequency, no more and no less.

R6 **wires**; it does not re-decide. Which checks exist and how often they run was settled at R3
and is recorded in the validation set — changing that partition is a spec change, not part of this
work. The tiered, self-disclosing evidence that CI consumes was built at R5 — the harness CLI
(`probe`/`offscreen`/`live-x11`/`perf`/`input`) already classifies its environment and emits
proof-scoped evidence. R6 reuses both; it builds neither.

"Users" here are the maintainers who push and open pull requests (who must get a fast, trustworthy
signal), the reviewers who read a run result, and the release operator who cuts a package.

A central constraint carries over from R5 and Constitution Principle VI (Observability & Safe
Failure): **CI must never overclaim.** Hosted CI runners are headless — no X11, no hardware GL, no
`/dev/uinput`. Display-, GL-, and input-dependent checks therefore cannot run there. When a
capability is absent, the corresponding check must **degrade and disclose** — skip with written
rationale, never report as passing, never silently vanish — exactly as the harness does. A green
check must mean what it says.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Fast, trustworthy pre-merge gate on every change (Priority: P1)

A maintainer pushes a branch or opens a pull request. Without any manual step, CI builds the
solution and runs the fast, deterministic validation — the default local tier plus the
push/PR-frequency checks — and reports a single clear pass/fail. A failure blocks the merge; a pass
means the deterministic checks genuinely ran and genuinely passed.

**Why this priority**: This is the core of R6 and a viable MVP on its own. The whole point of the
staged migration was to keep a small, fast, named validation set; automating that set as a merge
gate is what converts "we decided what to test" into "broken changes can't land." Everything else
in R6 refines coverage around this gate.

**Independent Test**: Open a PR with a deliberately broken change and confirm CI runs automatically
and reports red, blocking merge; open a clean PR and confirm it reports green. Verify the gate runs
build + the default local deterministic tier + the push/PR checks and nothing release-only.

**Acceptance Scenarios**:

1. **Given** a pushed branch with a compiling, passing change, **When** CI runs, **Then** the
   solution builds, the default local deterministic tier and push/PR checks run, and the gate
   reports pass.
2. **Given** a PR that breaks a default-local test, **When** CI runs, **Then** the gate reports
   fail and the merge is blocked.
3. **Given** a PR, **When** CI runs the gate, **Then** no release-only check (package/template)
   executes as part of it.

---

### User Story 2 - Capability-blocked checks degrade and disclose, never overclaim (Priority: P2)

A maintainer's change touches display/GL-dependent areas. CI runs on a headless runner that has no
X11 or hardware GL. The GL-dependent checks and the harness's display/perf tiers do not silently
pass and do not silently disappear: each is skipped with a written rationale, the run summary states
plainly what was proven and what could not be proven on this runner, and the absence of capability
does not turn the gate red.

**Why this priority**: Without this, a green check is a lie — it would imply visual/live behavior was
verified when the runner physically couldn't verify it. Principle VI makes honest degradation
non-negotiable. It is P2 (not P1) because the deterministic gate delivers value first; this makes
the gate's claims *truthful* about everything it can't reach.

**Independent Test**: Run the gate on a headless runner and confirm every display/GL/input-dependent
check is reported as skipped-with-rationale (not passed, not omitted), the run summary distinguishes
"proven" from "not proven here," and the overall gate is not failed merely because a capability was
absent.

**Acceptance Scenarios**:

1. **Given** a headless runner with no X11/GL, **When** CI runs, **Then** GL-dependent and live/perf
   tiers are skipped with a written rationale and are **not** marked passing.
2. **Given** the same run, **When** a reviewer reads the run summary, **Then** it states what was
   proven and what was not proven on this runner (proof scope), without reading raw logs.
3. **Given** a capability is merely absent (not a misconfiguration), **When** CI runs, **Then** the
   deterministic gate's pass/fail is unaffected by that absence.
4. **Given** the environment is genuinely misconfigured (e.g., a required tool missing where it
   should exist), **When** CI runs, **Then** CI fails fast with actionable probe facts rather than
   silently degrading.

---

### User Story 3 - Each check runs at exactly its declared cadence (Priority: P2)

A maintainer can trust that release-only checks don't slow down every push, and that scheduled or
capability-dependent runs don't gate merges. Each validation-set member executes on the one trigger
matching its frequency label: push/PR checks on push/PR, release-only checks at packaging/release,
and capability-dependent tiers (live, perf, input) as opt-in scheduled or manual runs.

**Why this priority**: The migration's value proposition is a *fast* inner loop. If release-only or
heavy capability checks leaked into the per-push gate, routine work would slow and the gate would go
flaky. Honoring the cadence partition keeps the gate fast and the heavier checks where they belong.

**Independent Test**: Audit the cadence→trigger mapping: confirm each validation-set member appears
in exactly one cadence, that release-only checks run on a release trigger and never on push/PR, and
that capability-dependent tiers run only on opt-in/scheduled triggers and never block a merge.

**Acceptance Scenarios**:

1. **Given** the validation set's frequency labels, **When** the cadence mapping is audited, **Then**
   every member maps to exactly one trigger with no overlap.
2. **Given** a packaging/release trigger, **When** CI runs, **Then** the release-only checks
   (package restore/consumption, template product instantiation) run.
3. **Given** a push or PR, **When** CI runs, **Then** the capability-dependent tiers (live, perf,
   input) are not part of the required gate and a failure or absence in them never blocks merge.

---

### Edge Cases

- **Headless / no GL runner** (the default hosted case): GL-dependent local tests and harness
  T1/T2/T3 tiers degrade-and-disclose; the deterministic gate still completes.
- **Wayland-only or partial display**: treated as capability-absent for the X11 live tier — skipped
  with rationale, not failed.
- **No `/dev/uinput`**: the opt-in input (uinput) tier is inert and disclosed as such; never a gate.
- **First run with no surface baseline present**: define whether the gate establishes or fails on a
  missing baseline (must be unambiguous, not silently "pass").
- **Flaky capability tier**: an advisory live/perf run going red must not turn the deterministic gate
  red or block merges.
- **Fork / external-contributor PR**: capability and release jobs must not require secrets the fork
  can't access, and must not false-fail the contributor for capabilities they can't reach.
- **Concurrent pushes to the same branch**: superseded runs should not produce misleading
  stale results.
- **Genuine misconfiguration vs. honest absence**: the system must distinguish "tool should be here
  and isn't" (fail fast) from "capability simply not available on this runner" (degrade).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: CI MUST trigger automatically on every push and pull request targeting the default
  branch, with no manual step required to start it.
- **FR-002**: The per-push/PR gate MUST build the full solution (`net10.0`) and run the default
  local deterministic validation tier; a build or deterministic-test failure MUST fail the gate.
- **FR-003**: The gate MUST run the push/PR-frequency checks defined in the validation set —
  public-surface baseline drift and the docs build — and fail the gate on drift or a broken docs
  build.
- **FR-004**: The gate MUST run the harness's deterministic tier (T0) and record its evidence as
  part of the per-push/PR run.
- **FR-005**: Checks requiring a capability the runner lacks (hardware GL, X11 display, `/dev/uinput`)
  MUST degrade and disclose — be skipped with a written, machine-readable rationale, never marked
  passing and never silently omitted (Constitution Principles V and VI).
- **FR-006**: Each CI run MUST emit a summary that declares what it proved and what it could not
  prove on that runner (carrying the harness's proof-scope semantics: proof level / authoritative-for
  / not-authoritative-for), so a reviewer can judge a result without reading raw logs.
- **FR-007**: Capability-dependent tiers (live X11, performance, input) MUST be opt-in — scheduled or
  manually triggered — and MUST NOT be required merge gates; their failure or absence MUST NOT block
  a merge.
- **FR-008**: Release-only checks (package restore/consumption, template product instantiation) MUST
  run on a packaging/release trigger and MUST NOT run as part of the per-push/PR gate.
- **FR-009**: Every validation-set member MUST map to exactly one CI cadence matching its frequency
  label; no member may appear in more than one cadence, and release-only checks MUST NOT appear in
  the push/PR gate.
- **FR-010**: CI MUST distinguish genuine misconfiguration (a required tool/condition absent where it
  is expected) — failing fast with actionable probe facts — from honest capability absence — degrading
  cleanly — so the two are never conflated.
- **FR-011**: Advisory/capability runs MUST be isolated from the deterministic gate such that their
  flakiness or environment-dependence cannot turn the required gate red.
- **FR-012**: The CI definitions MUST live in-repo, be reviewable like any other artifact, and the
  cadence→trigger mapping MUST be documented so a maintainer can verify FR-009 by inspection.
- **FR-013**: The configuration MUST handle fork/external-contributor pull requests without requiring
  secrets the fork cannot access and without false-failing on capability-gated jobs.

### Key Entities *(include if feature involves data)*

- **CI cadence/trigger**: one of {push/PR gate, packaging/release, scheduled/manual capability run};
  the binding from a frequency label to an automation trigger.
- **Validation-set member**: a named check (test project, surface-baseline, docs build, package/
  template check, or harness tier) carrying exactly one frequency label, sourced from the validation
  set.
- **Capability requirement**: the environment a check needs to run truthfully (none / GL / X11 display
  / `/dev/uinput`), determining whether it runs or degrades on a given runner.
- **Run summary / proof disclosure**: the per-run statement of what was proven and what was not
  (proof level, authoritative-for, not-authoritative-for), consumed from the harness evidence.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of pushes and pull requests to the default branch trigger the gate automatically,
  with zero manual start steps.
- **SC-002**: The deterministic gate (build + default local tier + surface baselines + docs + harness
  T0) completes in under 10 minutes on a standard hosted runner, preserving the "fast inner loop"
  intent.
- **SC-003**: An audit of the cadence→trigger mapping shows every validation-set member in exactly one
  cadence and zero members in more than one (no overlap; release-only never in the push/PR gate).
- **SC-004**: 100% of capability-blocked checks are reported as skipped-with-written-rationale; zero
  are reported as passing and zero are silently omitted (verified by inspecting a headless run).
- **SC-005**: A reviewer can determine, from a single run summary alone, exactly what was proven and
  what was not — without opening raw logs — in every run.
- **SC-006**: A deliberately broken deterministic check blocks merge on a test PR (verified red);
  capability-tier failure or absence never blocks merge (verified on a headless run).
- **SC-007**: Release-only checks execute on a release trigger and have zero occurrences in the
  per-push/PR gate across a sampled set of runs.
- **SC-008**: Time added to a routine push by release-only or capability work is zero — those checks
  do not run in the per-push/PR gate.

## Assumptions

- **CI platform**: The hosted CI is GitHub-based (the repository's `origin` remote is on GitHub).
  Requirements are written platform-agnostically, but examples assume hosted Linux runners.
- **Hosted runners are headless**: Default hosted runners have no X11, no hardware GL, and no
  `/dev/uinput`. Display-, GL-, and input-dependent checks therefore degrade-and-disclose there by
  design; this is expected, not a defect.
- **Capable-runner provisioning is out of scope**: Running the live (T2), perf (T3), and uinput tiers
  for real needs a display/GL/uinput-capable runner. Provisioning such a runner is **not** part of R6
  — the wiring degrades cleanly until one exists, and capability runs become meaningful once it does.
- **R5 harness is the evidence source**: The harness CLI from feature 004 provides the environment
  probe and the proof-scoped, self-disclosing evidence that CI consumes. R6 wires it; it does not
  modify or rebuild the harness.
- **Cadence labels are settled (R3)**: "Chosen frequencies" means the labels already recorded in
  `docs/validation/validation-set.md`. R6 implements that partition; re-deciding it is an R3-level
  change, not part of this work.
- **Branch protection is maintainer-configured**: The spec defines which checks are *required*;
  enabling merge-blocking branch protection around them is a one-time maintainer action.
- **Later stages remain out of scope**: Bridge/transition specifics (R7) and the package-identity
  rebrand decision (R8) are not touched here; release-only checks run against the current
  `FS.Skia.UI.*` identity.

## Out of Scope

- Provisioning or maintaining a GL/X11/uinput-capable CI runner (capability tiers degrade until one
  exists).
- Re-deciding the validation set or its frequency labels (owned by Stage R3).
- Changing the harness implementation or its evidence schema (owned by Stage R5).
- Package publishing/rebrand mechanics beyond triggering the existing release-only checks (Stage R8).
- Bridge/transition-and-boundaries wiring (Stage R7).

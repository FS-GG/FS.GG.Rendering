# Phase 0 Research: Wire Validation into CI at Chosen Cadences (Stage R6)

All Technical Context items were resolvable from the repository and the R3/R5 artifacts; no
`NEEDS CLARIFICATION` markers remained in the spec. The decisions below record *how* the wiring is
shaped and *why*, plus the alternatives rejected.

## Decision 1 — One workflow file per cadence (gate / release / capability)

**Decision**: Three in-repo workflows: `gate.yml` (push + pull_request), `release.yml`
(release/tag + manual), `capability.yml` (schedule + manual). Branch protection requires only the
gate.

**Rationale**: The required-vs-advisory boundary is the whole point of R6 (FR-007, FR-008). One file
per cadence makes "what blocks merge" obvious to a reviewer and to branch-protection configuration,
and keeps a flaky capability run physically unable to fail the gate (FR-011). Matches the constitution's
"narrow checks that pay for themselves" framing — each file states its trigger and its job.

**Alternatives considered**: *Single mega-workflow with conditional jobs* — rejected: harder to mark
exactly the gate as a required check, and conditional `if:` logic to keep release/capability off the
push path is more error-prone than separate triggers. *Matrix over all tiers* — rejected: conflates
fast deterministic jobs with capability jobs that need a different runner class.

## Decision 2 — The R5 harness CLI is the evidence engine; YAML only invokes it

**Decision**: Workflows shell to the harness subcommands (`probe`, `offscreen`, `perf`, `live-x11`,
`input`) and consume their exit code + `run.json`/`summary.md`. No tiering, probing, or proof-scope
logic is reimplemented in YAML or a new project.

**Rationale**: Principle III (idiomatic simplicity) and DRY — R5 already computes the pure RunPlan,
probes the environment, and emits proof-scoped evidence with clean degradation. Re-deriving any of it
in CI would create a second, drifting source of truth. The harness exit contract is exactly the CI
primitive needed: `0` = ran-and-passed **or** cleanly-skipped, `1` = assertion failed, `2` = bad usage.

**Alternatives considered**: *Bespoke shell that runs the viewer directly* — rejected: duplicates R5
and bypasses its disclosure guarantees. *A new compiled CI orchestrator project* — rejected: new
public surface + `.fsi`/baseline burden for no benefit over invoking the existing CLI.

## Decision 3 — Capability-absent ⇒ disclosed skip, never green-as-proof (no overclaim)

**Decision**: On the headless gate runner, GL/display/input-dependent checks (the GL-needing local
tests, harness T1 offscreen/T2 live/T3 perf, T-uinput) report **skipped with a written, machine-readable
rationale** and do **not** turn the gate red merely for being absent. The harness already returns exit
`0` + `run.json.status:"skipped"` for clean degradation; CI surfaces that as a skip, not a pass. A
genuine *misconfiguration* (a tool that should exist but doesn't) fails fast with probe facts (FR-010).

**Rationale**: Principle VI and V. A green check must not imply visual/live verification a headless
runner physically can't perform. The probe's classification (display present? GL? `/dev/uinput`?) is
what distinguishes honest absence from misconfiguration — the harness already produces it, so CI keys
off `probe` output rather than guessing.

**Alternatives considered**: *Fail the gate when a capability is missing* — rejected: would make every
hosted run red and destroy the fast inner loop; punishes contributors for runner limitations. *Silently
omit capability checks* — rejected: violates "never silently drop" (FR-005); a reader couldn't tell the
check was even considered.

## Decision 4 — Default GitHub-hosted Linux runner is headless; capability runs need a provisioned runner (out of scope)

**Decision**: Target `ubuntu-latest` (headless: no X11, no hardware GL, no `/dev/uinput`) for the gate.
The `capability.yml` workflow is authored to run T2/T3/T-uinput on a display/GL-capable runner, but
**provisioning that runner is out of scope** — until one is labeled/available the capability jobs
degrade-and-disclose like any other capability-absent run.

**Rationale**: Assumption in the spec; keeps R6 shippable today without blocking on infrastructure.
The wiring is correct and inert now, and becomes live evidence the moment a capable runner exists —
no rework.

**Alternatives considered**: *Spin up Xvfb + software GL (llvmpipe) in the gate* — rejected for the
required gate: software-GL "live" evidence would overclaim (it is not the hardware-GL path the product
targets) and adds minutes to the fast gate. It MAY be revisited as an explicitly-labeled,
non-authoritative capability job, disclosed as software-rendered — but not as a merge gate.

## Decision 5 — Cadence partition is read from R3, audited via an in-repo cadence map

**Decision**: `docs/ci/cadence-map.md` enumerates every validation-set member with its frequency label
and the CI trigger it maps to, cross-referencing `docs/validation/validation-set.md`. R6 implements the
existing labels; it does not re-decide them.

**Rationale**: FR-009/FR-012 require each member in exactly one cadence, verifiable by inspection.
Re-deciding frequencies is an R3-level change (and would be a spec change), so the map is a derivation,
not a new decision. Keeping it as a doc (not buried in YAML comments) makes the no-overlap invariant
auditable.

**Alternatives considered**: *Encode cadence only implicitly in which workflow references a project* —
rejected: no single place to audit the no-overlap invariant; drift between intent and YAML would be
invisible.

## Decision 6 — Per-run proof-scope summary reuses the harness `summary.md`; new glue only if forced

**Decision**: Each run's "what was proved / not proved here" disclosure (FR-006) is assembled from the
harness `summary.md`/`run.json` artifacts plus a job-summary step. A `scripts/ci/summarize-evidence.*`
helper is added **only if** that proves insufficient, and if added its pure logic gets a minimal test.

**Rationale**: Prefer zero new code (Principle III). The harness already emits `proofLevel`/
`authoritativeFor`/`notAuthoritativeFor`; folding those into the run summary is presentation, not new
proof logic.

**Alternatives considered**: *Always build a new aggregation project* — rejected as premature; *no
summary at all, rely on logs* — rejected: FR-006/SC-005 require a reviewer to judge a run without
opening raw logs.

## Decision 7 — Fork-PR safety: gate runs without privileged secrets

**Decision**: The deterministic gate uses no privileged secrets, so it runs on fork PRs. Release and
capability workflows (which may touch package feeds or self-hosted runners) are gated to non-fork
contexts / manual dispatch and never required for merge.

**Rationale**: FR-013 — external contributors must get a real gate signal without exposing secrets and
without false-failing on jobs they can't reach.

**Alternatives considered**: *Run everything on forks* — rejected: leaks secrets / fails on missing
self-hosted runners. *Block fork PRs from CI* — rejected: kills external contribution and the gate's
value for them.

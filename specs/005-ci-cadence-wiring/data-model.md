# Phase 1 Data Model: Wire Validation into CI at Chosen Cadences (Stage R6)

R6 adds no product runtime data types. The "entities" here are the configuration/derivation concepts
the wiring is built from — the things a reviewer reasons about when auditing the cadence map and the
gate. They are realized as YAML triggers/jobs and Markdown rows, not F# records.

## Entity: Cadence / Trigger

The binding from a frequency label to an automation trigger.

| Field | Values | Notes |
|---|---|---|
| `id` | `gate` \| `release` \| `capability` | one per cadence |
| `trigger` | `push`+`pull_request` (gate) · `release`/tag+`workflow_dispatch` (release) · `schedule`+`workflow_dispatch` (capability) | what fires the workflow |
| `required` | `true` (gate) \| `false` (release, capability) | only `gate` blocks merge |
| `runnerClass` | `hosted-headless` (gate, release) \| `capable` (capability) | capability needs display/GL/uinput (out-of-scope to provision) |
| `secretsExposed` | `false` (gate) \| `restricted` (release, capability) | fork PRs only ever hit `gate` (FR-013) |

**Invariant**: exactly one cadence has `required: true`.

## Entity: Validation-set member

A named check the wiring places at exactly one cadence. **Two settled sources** feed the cadence map:
the R3 validation set (`docs/validation/validation-set.md`) supplies the test / baseline / docs / package
members; the R5 harness (`docs/validation/harness.md`) supplies the tiers (T0–T-uinput). Harness tiers
are **not** validation-set members — both are merely *placed* by R6, neither re-decided.

| Field | Values | Notes |
|---|---|---|
| `name` | e.g. `Scene.Tests`, `surface-baselines`, `docs build`, `Package.Tests`, harness `T0` | unique |
| `frequencyLabel` | `local` \| `ci-push-pr` \| `release-only` (R3 validation set) · `infra-r5` (R5 harness tiers) | the R3 "Manual/advisory" group is empty; harness tiers carry `infra-r5`. NB: `capability` is a **cadence id**, not a frequency label |
| `cadence` | the `Cadence.id` it maps to | derived, not re-decided |
| `capabilityRequirement` | `none` \| `gl` \| `x11` \| `uinput` | determines run-vs-degrade on a given runner |
| `gatekeeping` | `blocks-merge` \| `advisory` | `advisory` ⇔ `cadence.required = false` |

**Invariants** (audited in `contracts/cadence-matrix.md`):
- Every row maps to **exactly one** cadence (no overlap). *(FR-009)*
- No `release-only` member appears in the `gate` cadence. *(FR-008, FR-009)*
- Every row traces to a settled source — validation-set members to `validation-set.md` (R3), harness tiers to the R5 harness (`harness.md`); nothing invented here.

## Entity: Capability requirement

What a check needs to run *truthfully* on a runner.

| Value | Needs | On headless hosted runner |
|---|---|---|
| `none` | nothing | runs |
| `gl` | hardware GL context | degrade-and-disclose (e.g. `Smoke.Tests`, `SkiaViewer.Tests`, harness T1) |
| `x11` | X11 display + WM | degrade-and-disclose (harness T2 live) |
| `uinput` | `/dev/uinput` | inert + disclosed (harness T-uinput) |

The harness `probe` subcommand is the authority that classifies the runner; CI keys decisions off its
output, never off a guess. *(FR-005, FR-010)*

## Entity: Run summary / proof disclosure

The per-run statement consumed from harness evidence (`run.json`/`summary.md`) and surfaced in the CI
job summary.

| Field | Source | Meaning |
|---|---|---|
| `status` | harness `run.json.status` | `passed` \| `failed` \| `skipped` (clean degradation) |
| `proofLevel` | harness | strength of evidence the run produced |
| `authoritativeFor` | harness | what this run *does* prove |
| `notAuthoritativeFor` | harness | what it explicitly does **not** prove (e.g. live visuals on a headless runner) |
| `rationale` | harness/CI | written reason for any skip (machine-readable) |

**Invariant**: a `skipped` status is never rendered as a pass; a reviewer can read proved-vs-not-proved
from the summary without opening raw logs. *(FR-005, FR-006, SC-004, SC-005)*

## State transitions (gate outcome)

```text
                 build fail ──────────────► gate: FAIL (blocks merge)        [FR-002]
deterministic ── det. test fail ──────────► gate: FAIL (blocks merge)        [FR-002, FR-003]
   gate run  ── surface drift / docs fail ► gate: FAIL (blocks merge)        [FR-003]
             ── capability absent ────────► check: SKIPPED+rationale ──┐     [FR-005]
             ── misconfiguration ─────────► gate: FAIL (probe facts)   │     [FR-010]
             └─ all deterministic pass ────► gate: PASS ◄──────────────┘     [FR-002]
                                             (skips do not affect pass/fail)  [FR-005, FR-011]
```

Capability-tier runs (`release`/`capability` cadences) produce evidence and never feed this gate
outcome. *(FR-007, FR-011)*

> Gate steps that wrap a multi-tier harness subcommand (`offscreen` → T0+T1) decide pass/skip/fail from
> **each tier's `run.json.status`**, not the aggregate process exit code — `offscreen` exits `1` when T1
> is cleanly *skipped*, which must not red the gate. *(FR-005, FR-011)*

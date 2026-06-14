# Quickstart / Validation: Wire Validation into CI at Chosen Cadences (Stage R6)

Runnable scenarios that prove the wiring works end-to-end. They map to the user stories and success
criteria. Most can be exercised locally by running what the gate runs; the merge-block and trigger
behaviors are confirmed on real CI runs once the workflows land. Details live in
[`contracts/`](./contracts/) and [`data-model.md`](./data-model.md) — not duplicated here.

## Prerequisites

- A clean checkout on `net10.0` with the R5 harness present (`tests/Rendering.Harness`).
- The default-branch CI workflows added by this stage (`.github/workflows/{gate,release,capability}.yml`)
  and `docs/ci/cadence-map.md`.

## V1 — Deterministic gate is green on a clean change *(US1, SC-001, SC-002)*

1. Locally reproduce the gate: `dotnet build FS.GG.Rendering.slnx`, then `dotnet test` over the
   deterministic local-tier projects, the surface-baselines check, the `fsdocs` build, and
   `Rendering.Harness offscreen` (T0).
2. Open a PR with a trivial, passing change.

**Expected**: CI triggers with no manual step; the gate reports green; the deterministic portion
completes in under 10 minutes.

## V2 — Deterministic break blocks merge *(US1, SC-006)*

1. Open a PR that breaks a deterministic local-tier test (or introduces `.fsi` surface drift).

**Expected**: the gate reports red and merge is blocked. Reverting greens it.

## V3 — Capability-blocked checks degrade and disclose *(US2, FR-005, SC-004)*

1. Run the gate on the headless hosted runner (the default).
2. Inspect the run summary and the harness `run.json` for `SkiaViewer.Tests`, `Smoke.Tests`, and
   harness T1/T2/T3/T-uinput.

**Expected**: each appears under `notProvedHere` (skipped) with a written rationale, **never** as
passed and never omitted; the overall gate is still green; `run.json.status` is `skipped`, exit `0`.

## V4 — Run summary states proved vs. not-proved *(US2, FR-006, SC-005)*

1. Open the CI job summary for any gate run.

**Expected**: a reader can answer "was live/visual behavior verified here?" from the summary alone —
on a headless run, an explicit *no, not proved here* with rationale (per
[`run-summary.schema.md`](./contracts/run-summary.schema.md)).

## V5 — Misconfiguration fails fast, absence does not *(US2, FR-010)*

1. Simulate a misconfiguration (e.g. a required tool expected-but-missing) and run the relevant step.

**Expected**: CI fails fast with actionable probe facts — distinct from the clean skip of an absent
capability (V3).

## V6 — Each check runs at exactly its cadence *(US3, FR-009, SC-003, SC-007)*

1. Audit `docs/ci/cadence-map.md` against `docs/validation/validation-set.md` per
   [`contracts/cadence-matrix.md`](./contracts/cadence-matrix.md).
2. Trigger a release/tag build; trigger the capability workflow manually.

**Expected**: every member maps to exactly one cadence (no overlap); `Package.Tests` and template
`Product.Tests` run only on the release trigger and never in the gate; capability tiers run only on
schedule/manual and never block merge.

## V7 — Fork PR gets a real signal without secrets *(US1, FR-013)*

1. Open a PR from a fork.

**Expected**: the gate runs and reports pass/fail without requiring privileged secrets; release/
capability jobs are skipped or restricted, and their absence does not false-fail the contributor.

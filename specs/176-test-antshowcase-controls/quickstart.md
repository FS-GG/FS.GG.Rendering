# Quickstart: Automated Control Pass for the Second AntShowcase

**Feature**: `176-test-antshowcase-controls`

A validation/run guide for the automated control pass. Implementation details live in `tasks.md`
and the contracts; this file shows how to run the pass and confirm the feature works end to end.

## Prerequisites

- .NET `net10.0` SDK.
- Packed `FS.GG.UI.*` packages in the local feed (`~/.local/share/nuget-local/`). The showcase is
  package-consuming; if a Tier 1 fix lands, re-pack the touched packages and bump the sample pins
  before re-running (mirroring the Feature 175 follow-up).
- For **accepted live evidence**: a visible, focusable Linux desktop session (OpenGL). Without one,
  the pass still runs and reports live-only checks as `environment-limited`.

## Build & test

```sh
cd samples/SecondAntShowcase
dotnet build SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release
dotnet test SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release
```

## Run the control pass (headless / unattended)

```sh
# Full pass, deterministic Pure backend, both appearances + both sizes, seeded:
dotnet run --project SecondAntShowcase.App -c Release -- \
  control-pass --seed 1 --themes light,dark --sizes preferred,minimum \
  --out specs/176-test-antshowcase-controls/readiness --json
```

Expected: exactly one verdict record per cataloged control under
`readiness/verdict-records/`, visual evidence under `readiness/visual-evidence/`, a `finding-log.md`,
and a `validation-summary.md`. The process exits non-zero if any control is unclassified, any
finding is non-terminal, or any required live check silently failed.

## Run the control pass (live, accepted evidence)

```sh
# Drive real input through a visible window for accepted live exercise + pixel capture:
dotnet run --project SecondAntShowcase.App -c Release -- \
  control-pass --seed 1 --backend x11xtest --require-live \
  --themes light,dark --out specs/176-test-antshowcase-controls/readiness
```

On a headless host this command marks live-only records `environment-limited` with a non-zero,
well-defined signal — never a silent pass (FR-008).

## Validation scenarios (acceptance)

1. **Every control classified (US1, SC-001)** — run the pass; confirm the record count equals
   `CoverageMap.catalogIds ()` and no record is `Unexercised`/`Unclassified`. The
   `ControlPassCoverageTests` assert this; `dotnet run -- coverage` cross-checks the catalog.
2. **Every behavior exercised (US1, SC-002)** — for an interactive control (e.g. `slider`), confirm
   its record's `BehaviorsExercised` covers every documented behavior in the interaction contract,
   not one action.
3. **Visual matrix complete (US2, SC-003)** — confirm each control has evidence for
   light/dark × preferred/minimum at rest, and each interactive control has per-interaction-state
   evidence, each verified to differ from rest.
4. **Damage-local repaint (US2, FR-005)** — confirm each state transition's `DamageOutcome` is
   `Localized` (or carries an `IntentionalDamageException`), not `Broad`/`FullSurface`.
5. **Determinism (SC-005)** — run the pass twice with the same `--seed` on the same build; confirm
   functional verdicts and byte-stable evidence match (`DeterminismTests`), timestamps excluded.
6. **Environment-limited (FR-008)** — run the headless pass on a host with no display; confirm
   live-only records are `environment-limited`, not silently passed/failed.
7. **Findings reach terminal state (US3, SC-006)** — confirm `finding-log.md` shows every finding as
   `FixedAndReVerified` (with before/after evidence) or `Deferred` (with rationale + follow-up).
8. **No regression (US3, SC-007)** — after fixes, re-run the full pass; confirm no interactive
   control is non-functional and no record regresses vs the pre-fix baseline.
9. **Report delivered (US4, SC-008)** — confirm
   `docs/reports/2026-06-20-feature-176-second-antshowcase-control-pass-report.md` exists, separates
   framework from sample-local items, and each framework entry carries severity, classification,
   evidence, and a recommendation.

## Early live smoke run (gates US3)

Before building any fix, run scenario (1)+(2)+(6) live (or environment-limited) once to confirm or
replace the provisional defect hypotheses in `research.md` §D8. Do not implement a US3 fix against
an unverified hypothesis (Feature 175 lesson: green tests ≠ working app).

## References

- Contracts: [control-pass-runner](contracts/control-pass-runner.md),
  [verdict-record](contracts/verdict-record.md),
  [visual-evidence-matrix](contracts/visual-evidence-matrix.md),
  [framework-report](contracts/framework-report.md)
- Data model: [data-model.md](data-model.md)
- Research / decisions: [research.md](research.md)

# Contract: Control-Pass Runner

**Feature**: `176-test-antshowcase-controls`

Defines the inputs/outputs, CLI surface, execution guarantees, and degradation behavior of the
automated control pass. The runner is **sample-local (Tier 2)** — it adds no public `FS.GG.UI.*`
surface; it composes existing framework + harness surfaces.

## CLI surface

New `SecondAntShowcase.App` subcommand, dispatched in `Program.fs`:

```text
SecondAntShowcase control-pass
    [--seed <int>]                      # determinism seed (default 1)
    [--themes <list>]                   # appearances (default light,dark)
    [--sizes <list>]                    # representative sizes (default preferred,minimum)
    [--backend pure|x11xtest|uinput]    # input backend (default pure)
    [--require-live]                    # fail-closed unless live exercise was accepted
    [--page <id> | --all]              # scope (default --all)
    [--out <dir>]                       # readiness root (default specs/176-.../readiness)
    [--json]                            # also emit machine-readable summary
```

- The headless `pure` path requires **no display** and is the unattended default (FR-001, SC-004).
- `x11xtest` drives real input through a visible window for accepted live evidence.
- `uinput` honest-skips when `/dev/uinput` is absent.

## Inputs

| Input | Source | Contract |
|-------|--------|----------|
| Control catalog | `CoverageMap.catalogIds ()` + template-reachable controls | The complete iteration set (research §D1). Template-reachable controls do **not** add new ids — they reuse catalog identity and are recorded as extra `PageContext` on their catalog id, so the emitted record-id set still equals `CoverageMap.catalogIds ()` exactly (C-1/G-2). |
| Per-control behaviors | `InteractionContracts.fs` | Every documented behavior is driven (research §D2). |
| Input scripts | `Rendering.Harness.Input` (`InputScript`/`InputStep`) | Scripted, no human input. |
| Appearance × size matrix | `VisualCaptureMatrix.expand` | light/dark × preferred/minimum. |
| Seed / pinned clock | `--seed`, showcase deterministic inputs | Determinism (research §D6). |

## Outputs (under `--out`)

- `verdict-records/` — one Control Verdict Record per cataloged control (data-model VR-1/VR-2).
- `visual-evidence/` — appearance × size × state captures + contact sheets.
- `finding-log.md` — findings with lifecycle (data-model Finding entity).
- `validation-summary.md` (+ JSON when `--json`) — aggregate readiness, counts, caveats.

## Behavioral guarantees

- **G-1 Unattended**: the runner completes start to finish with zero human input events (FR-001,
  SC-004). All input is scripted via `Rendering.Harness.Input`.
- **G-2 Complete**: emits exactly one record per cataloged control; the record-id set equals
  `CoverageMap.catalogIds ()` (FR-006, SC-001). Missing/duplicate ⇒ non-zero exit.
- **G-3 Full behavior**: every documented behavior of each interactive control is driven and its
  state change asserted (FR-002, SC-002).
- **G-4 Deterministic**: repeated runs with the same `--seed` on the same build yield identical
  functional verdicts and byte-stable evidence where the framework guarantees determinism;
  time/animation surfaces are pinned or flagged `time-dependent` (FR-007, SC-005). Timestamps live
  in non-asserted metadata (`GeneratedAtUtc` convention).
- **G-5 Damage-local**: each driven state transition is captured as a `RetainedInspectionArtifact`
  and validated; broad/full-surface damage without an `IntentionalDamageException` is a finding
  (FR-005).
- **G-6 Fail-closed degradation**: when no live window can be presented, live-only checks become
  explicit `environment-limited` records with a well-defined non-zero signal — never a silent
  pass/fail (FR-008). Detection via `SkiaViewer` window diagnostics + `ValidationLanes`
  `EnvironmentLimited`. Structural (Pure backend + `ControlInspection`) evidence still runs.
- **G-7 No mutation of catalog**: the pass exercises controls; it never removes a control or page
  (FR-012).

## Exit codes

| Code | Meaning |
|------|---------|
| 0 | All controls classified; all findings terminal; no silent live failure. |
| 1 | Completeness/classification failure, non-terminal finding, or (`--require-live`) live unavailable. |
| 2 | Bad arguments. |

## Test obligations (failing-first, `SecondAntShowcase.Tests`)

- `ControlPassCoverageTests` — record-id set == catalog; behavior coverage == contract per control
  (G-2, G-3).
- `ControlPassRunnerTests` — classification completeness (no `Unexercised`/`Unclassified`),
  deterministic re-run (G-4), environment-limited degradation (G-6), damage-locality recording
  (G-5).
- Any synthetic substitute (e.g. a faked window diagnostic when asserting G-6 in CI) carries the
  `Synthetic` token in the test name and is disclosed at the use site (Principle V).

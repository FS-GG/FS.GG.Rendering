# Contract: Compositor Performance Command

## Scope

Feature 156 adds the canonical same-profile timing command:

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-performance --feature 156 [options]
```

The command collects or evaluates full-redraw and damage-scoped timing evidence for the accepted
Feature 155 host profile. It does not redefine Feature 155 correctness acceptance.

## Arguments

| Argument | Required | Default | Meaning |
|----------|----------|---------|---------|
| `--feature 156` | yes | none | Selects Feature 156 timing rules and output shape. |
| `--out <dir>` | no | `specs/156-same-profile-timing/readiness/timing` | Output directory for timing evidence. |
| `--profile <id>` | no | Feature 155 accepted profile | Expected host profile id; mismatches reject the run. |
| `--policy <id>` | no | `same-profile-live-threshold-v2` | Timing policy to declare before evaluation. |
| `--warmup <n>` | no | `3` | Warmup repetitions per path excluded from metrics. |
| `--repetitions <n>` | no | `5` | Measured repetitions per path per scenario after warmup. |
| `--scenario <id>` | no | all required scenarios | Restricts collection to one scenario for development. |
| `--json` | no | false | Emits machine-readable summary in addition to markdown. |

## Required Output

The command writes:

- `summary.md`: reviewer-facing timing evidence summary.
- `scenarios/<scenario-id>.md`: one scenario report per measured scenario.
- `raw/<scenario-id>-full-redraw.csv`: raw full-redraw samples.
- `raw/<scenario-id>-damage-scoped.csv`: raw damage-scoped samples.
- `unsupported/README.md`: unsupported-host result when environment-limited.
- Optional `summary.json` when `--json` is supplied.

## Exit Codes

- `0`: Evidence collection and summary publication completed, including positive, noisy,
  non-beneficial, rejected, limited, or environment-limited verdicts.
- `1`: Command execution failed before a reviewer-visible evidence package could be written.
- `2`: Invalid command-line arguments or unknown policy/scenario id.

## Validation Rules

- The command must declare policy `same-profile-live-threshold-v2` before evaluating samples.
- Full-redraw and damage-scoped samples must share run identity, host profile, scenario
  definition, renderer identity, display environment, package version, and measured repetition
  count.
- Fewer than five required scenarios or fewer than five measured repetitions per path per scenario
  cannot produce a positive timing decision.
- Cross-profile, stale, duplicated, missing, unreadable, incomplete, noisy, environment-limited,
  readback-dominated, or non-beneficial evidence fails closed with a reason in `summary.md`.
- A positive Feature 156 timing verdict is scoped to the measured host profile and does not become
  a shipped P7 performance claim by itself.

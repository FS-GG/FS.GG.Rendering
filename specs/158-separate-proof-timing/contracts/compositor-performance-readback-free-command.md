# Contract: Compositor Performance Readback-Free Command

## Command

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-performance --feature 158 [options]
```

## Purpose

Collect Feature 158 timing evidence that excludes validation readback from accepted measured
intervals. The command may also run explicit probe mode, but probe samples are always excluded from
performance acceptance.

## Arguments

| Argument | Required | Default | Meaning |
|----------|----------|---------|---------|
| `--feature 158` | yes | none | Selects Feature 158 measurement-separation rules and output shape. |
| `--out <dir>` | no | `specs/158-separate-proof-timing/readiness/timing` | Output directory for timing evidence. |
| `--profile <id>` | no | Feature 155 accepted profile | Expected host profile id; mismatches reject timing acceptance. |
| `--policy <id>` | no | `readback-free-timing-v1` | Measurement policy declared before sampling. |
| `--warmup <n>` | no | `3` | Warmup repetitions per path excluded from metrics. |
| `--repetitions <n>` | no | `5` | Measured repetitions per path per scenario after warmup. |
| `--scenario <id>` | no | all required scenarios | Restricts collection to one scenario for development. |
| `--probe-readback` | no | false | Runs an explicit readback probe; resulting samples are excluded from performance acceptance. |
| `--json` | no | false | Emits machine-readable summary in addition to markdown. |

## Required Scenarios

- `timing/localized-update`
- `timing/no-change`
- `timing/movement-old-new`
- `timing/overlap`
- `timing/edge-clipping`

## Required Output

The command writes:

- `summary.md`: reviewer-facing timing evidence summary.
- `scenarios/<scenario-id>.md`: one scenario report per measured scenario.
- `raw/<scenario-id>-<path>.csv`: raw samples with measurement policy and inclusion status.
- `excluded/<reason>.md`: grouped excluded samples and diagnostics.
- `unsupported/README.md`: unsupported-host result when environment-limited.
- `summary.json`: optional machine-readable summary when `--json` is supplied.
- Probe artifacts under `../proof-probes/` or the requested output location when
  `--probe-readback` is supplied.

## Exit Codes

- `0`: Evidence collection and summary publication completed, including accepted, rejected,
  fallback-only, or environment-limited results.
- `1`: Command execution failed before a reviewer-visible evidence package could be written.
- `2`: Invalid command-line arguments or unknown policy/scenario id.

## Acceptance Rules

- The command must declare `readback-free-timing-v1` before samples are evaluated.
- Accepted samples must be `readback-free` or `readback-outside-measurement`.
- Samples with readback inside the measured interval must be excluded with
  `proof-readback-in-measured-interval`.
- Probe-mode samples must be excluded with `probe-run-excluded`.
- Missing, stale, duplicated, unreadable, cross-profile, cross-run, scenario-mismatched,
  package-mismatched, unsupported-host, or unverifiable-policy samples cannot be included.
- The command must preserve Feature 156 distribution fields and Feature 157 damage readiness
  boundaries while adding policy and exclusion fields.
- A positive or accepted Feature 158 measurement-separation result does not become a shipped
  compositor performance claim by itself.

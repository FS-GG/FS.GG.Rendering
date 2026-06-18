# Contract: Focused Performance Lane Command

## Command

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-performance --feature 160 \
  --lane focused \
  --out specs/160-performance-validation-throughput/readiness/throughput \
  --policy focused-throughput-v1 \
  --attempts 3 \
  --max-iteration-minutes 10
```

## Purpose

Collect focused Feature 160 performance iterations without running broad release validation for
each timing loop. The command publishes bounded throughput evidence and excluded evidence; it does
not mark release readiness by itself.

## Options

- `--feature 160`: selects Feature 160 throughput rules and output shape.
- `--lane focused`: selects the bounded focused lane. Other lanes cannot satisfy Feature 160
  accepted throughput.
- `--out <dir>`: output directory for iteration summaries, raw samples, excluded evidence, and
  unsupported-host records.
- `--policy focused-throughput-v1`: required policy id for accepted throughput evidence.
- `--attempts <n>`: number of fresh focused iterations to collect. Accepted readiness requires at
  least `3`.
- `--max-iteration-minutes <n>`: declared per-iteration bound. Accepted Feature 160 evidence uses
  `10`.
- `--scenario <id>`: optional single-scenario debugging. A restricted run cannot satisfy final
  throughput readiness.
- `--json`: optional machine-readable summary beside markdown output.

## Required Scenarios

- `timing/localized-update`
- `timing/no-change`
- `timing/movement-old-new`
- `timing/overlap`
- `timing/edge-clipping`

Additional scenarios may be published, but they cannot replace required scenarios.

## Required Output

The command writes:

```text
throughput/
|-- summary.md
|-- iterations/
|   `-- iteration-*.md
|-- raw/
|   |-- *.csv
|   `-- *.json
|-- excluded/
|   `-- *.md
`-- unsupported/
    `-- README.md
```

`summary.json` may be added beside `summary.md` when the implementation chooses a stable schema.
Markdown remains the reviewer entry point and JSON remains a derived artifact.

## Acceptance Rules

- Accepted command output requires lane `focused` and policy `focused-throughput-v1`.
- Accepted iterations require host profile `probe-08a47c01` or a later accepted same-profile proof.
- At least three fresh same-profile iterations are required before Feature 160 throughput can be
  `accepted`.
- Every accepted iteration must complete under the declared 10 minute bound.
- Every accepted iteration must include all required scenarios and the Feature 158 sample policy:
  warmup `3`, measured repetitions `5`, and readback-free or readback-outside-measurement samples.
- Unsupported-host command output must complete within 2 minutes with zero accepted same-profile
  performance artifacts.
- Cross-profile, stale, missing-policy, mixed-policy, partial, canceled, timed-out,
  environment-limited, missing-metadata, scenario-coverage-missing, sample-policy-mismatch,
  run-identity-mismatch, artifact-unreadable, or readback-contaminated iterations cannot be
  accepted.
- The command must not invoke broad release validation as part of focused iteration collection.

## Exit Behavior

- `0`: Evidence generation and summary publication completed, including accepted, rejected,
  fallback-only, or environment-limited results.
- `1`: Command execution failed before a reviewer-visible evidence package could be written.
- `2`: Invalid command-line arguments, unknown policy id, unknown lane, or unknown scenario id.

Timeouts and canceled iterations should still write excluded evidence before the command returns
non-accepted throughput status.

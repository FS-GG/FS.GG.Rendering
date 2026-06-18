# Contract: Host Lane Command

## Command

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-performance --feature 161 \
  --lane host-ledger \
  --out specs/161-host-performance-lane-ledger/readiness/lane-ledger \
  --policy host-lane-ledger-v1 \
  --source-throughput specs/160-performance-validation-throughput/readiness/throughput
```

## Purpose

Collect and validate host-lane facts for timing evidence that may be considered for P7 compositor
performance acceptance. The command publishes lane ledger entries and excluded evidence; it does
not accept a universal performance claim by itself.

## Options

- `--feature 161`: selects Feature 161 host-lane rules and output shape.
- `--lane host-ledger`: selects lane-ledger evidence collection. Other lanes cannot satisfy
  Feature 161 accepted host scoping.
- `--out <dir>`: output directory for lane summaries, entries, host facts, excluded evidence, and
  unsupported-host records.
- `--policy host-lane-ledger-v1`: required policy id for accepted Feature 161 evidence.
- `--source-throughput <dir>`: optional path to Feature 160 throughput evidence to scope.
- `--json`: optional machine-readable summary beside markdown output.

## Required Output

The command writes:

```text
lane-ledger/
|-- summary.md
|-- entries/
|   `-- entry-*.md
|-- host-facts/
|   `-- facts-*.md
|-- excluded/
|   `-- *.md
`-- unsupported/
    `-- README.md
```

`summary.json` may be added beside `summary.md` when the implementation chooses a stable schema.
Markdown remains the reviewer entry point and JSON remains a derived artifact.

## Required Host Facts

Accepted command output requires:

- Display server.
- Display identity.
- Renderer identity.
- Direct rendering status.
- Refresh rate or reason unavailable.
- Driver identity.
- Package version set.
- CPU/GPU load notes.
- Known environment limits.
- Host profile.
- Run identity.
- Scenario identity.
- Timing policy identity.
- Collection time.
- Artifact locations.

## Acceptance Rules

- Accepted command output requires lane `host-ledger` and policy `host-lane-ledger-v1`.
- The current lane may be identified as X11 `:1` with direct OpenGL on AMD Radeon/Mesa only when
  collected facts confirm that lane for the timing run.
- Evidence from Wayland, indirect GL, missing-display, software-raster, virtualized, unknown, or
  otherwise different lanes cannot be counted as accepted for the current lane.
- Cross-lane, missing-fact, contradictory-fact, stale, unreadable, unsupported-host, stale-package,
  and run-identity-mismatch records cannot be accepted.
- Noisy same-profile timing may keep complete lane facts, but it cannot accept the shipped
  performance claim.

## Exit Behavior

- `0`: Evidence generation and summary publication completed, including accepted, rejected,
  fallback-only, or environment-limited results.
- `1`: Command execution failed before a reviewer-visible evidence package could be written.
- `2`: Invalid command-line arguments, unknown policy id, unknown lane, or unreadable source
  evidence.

Rejected and environment-limited runs should still write reviewer-visible evidence before the
command returns non-accepted lane status.

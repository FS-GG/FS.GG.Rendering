# Contract: Compositor Damage Command

## Command

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- \
  compositor-damage --feature 157 --out specs/157-no-clear-damage-scissor/readiness/damage
```

## Purpose

Collect Feature 157 damage-scoped correctness evidence. The command exercises the real GL
damage-scissored path when eligible, records full-redraw fallbacks when not eligible, and writes
artifacts that `compositor-readiness --feature 157` can assemble into the final readiness package.

## Inputs

- `--feature 157`: required. Alias values may include `feature157` and
  `157-no-clear-damage-scissor`.
- `--out <dir>`: optional output directory. Defaults to a timestamped harness artifact directory
  if omitted.
- `--attempt-count <n>`: optional capable-host attempt count. Accepted readiness requires at least
  three fresh attempts.
- `--scenario <id>`: optional repeatable filter for local debugging. Omitted means the required
  scenario set.

## Required Scenarios

- `damage/static-preserved`
- `damage/localized-update`
- `damage/movement-old-new`
- `damage/scroll-shifted`
- `damage/nested-retained`

## Fallback Scenarios

- `damage/empty-visible-change`
- `damage/out-of-bounds`
- `damage/stale`
- `damage/incomplete`
- `damage/full-frame-invalidation`
- `damage/missing-retained-backing`
- `damage/resource-failure`
- `damage/parity-mismatch`
- `damage/unsupported-host`

## Outputs

The command writes markdown and machine-readable artifacts under the output directory:

```text
attempts/
fallbacks/
parity/
unsupported/
summary.md
summary.json
```

Each accepted attempt includes:

- Attempt id and run id.
- Host profile.
- Proof gate reference.
- Retained frame state.
- Damage validation status.
- Preserved-pixel evidence.
- Damaged-pixel evidence.
- Parity result.
- Artifact paths.

Each fallback includes:

- Requested path.
- Fallback reason category.
- Host profile when available.
- Damage validation status when available.
- Diagnostics.
- Confirmation that accepted partial-redraw artifact count is zero.

## Acceptance Rules

- The command must reject or fallback when Feature 155 proof is missing, stale, rejected,
  cross-profile, or synthetic-only.
- The command must reject or fallback when retained backing is unavailable, stale, cross-run,
  cross-profile, resized, or resource-failed.
- The command must reject invalid, ambiguous, stale, or incomplete damage.
- Accepted attempts must pass parity against the equivalent full-redraw frame.
- Unsupported-host output must be `environment-limited` and contain zero accepted partial-redraw
  artifacts.

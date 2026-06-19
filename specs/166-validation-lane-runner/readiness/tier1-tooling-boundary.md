# Tier 1 Tooling Boundary

Reviewed files:

- `tests/Rendering.Harness/ValidationLanes.fsi`
- `tests/Rendering.Harness/ValidationLanes.fs`
- `tests/Rendering.Harness/Cli.fs`
- `scripts/run-validation-lanes.fsx`

Boundary result:

- The changed public surface is the maintainer-facing
  `Rendering.Harness.ValidationLanes` contract and `validation-lanes` CLI.
- No public `FS.GG.UI.*` runtime package surface was changed.
- Existing direct validation commands remain documented and runnable.
- No package surface baseline update is required for this tooling-only change.

# Contract: Validation Entry Point

## Canonical Command

The maintained repository entry point for retained inspection readiness is the validation-lane runner:

```sh
dotnet fsi scripts/run-validation-lanes.fsx --lane retained-inspection --out specs/170-retained-damage-inspection/readiness/lanes
```

This replaces any stale or missing legacy wrapper name for retained inspection validation.

## Lane Contract

The `retained-inspection` lane must:

- be listed by `dotnet fsi scripts/run-validation-lanes.fsx --list`
- run retained inspection tests for Controls
- run damage locality validation tests for Testing
- run validation-lane registration tests for Rendering.Harness
- run the selected AntShowcase structured evidence adoption test
- write `log.txt`, `result.json`, `diagnostics.md`, and TRX output under its lane directory
- discover retained inspection JSON/Markdown artifacts when the tests emit them
- fail closed for failed, timed-out, no-progress-timeout, canceled, skipped, not-run, environment-limited, and infrastructure-error statuses when selected explicitly for feature readiness

The lane may be optional in the general validation catalog unless maintainers update `docs/validation/validation-set.md` to make it part of default required readiness.

## Direct Debug Commands

The lane is canonical, but focused debugging may use direct commands:

```sh
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter Feature170
dotnet test tests/Testing.Tests/Testing.Tests.fsproj -c Release --filter Feature170
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --filter Feature170
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --filter Feature170
dotnet test tests/Package.Tests/Package.Tests.fsproj -c Release --filter Surface
```

If a direct command is used instead of the lane for readiness, the readiness evidence must disclose the substitution and keep the missing lane summary visible.

## Prerequisite Failure Contract

The command must fail with actionable diagnostics when:

- the .NET SDK is unavailable
- package restore has not been run when `--no-restore` commands require restored assets
- the AntShowcase sample project is missing or stale
- the local package feed proof is stale for package-consuming sample validation
- output directories cannot be created or written
- the `retained-inspection` lane id is unknown

Diagnostics must name the missing prerequisite and the next action.

## Evidence Contract

Validation evidence must record:

- exact command
- start and end time or elapsed duration
- result status
- artifact root
- lane log path
- lane result JSON path
- lane diagnostics path
- test result paths
- retained inspection summary JSON/Markdown paths
- environment-limited or skipped scope, if any

## Documentation Contract

`docs/validation/validation-set.md` must mention the retained-inspection lane when the feature lands:

- If optional: list it as an on-demand retained inspection lane with the canonical command.
- If required: add it to the required lanes list and state the cadence/cost justification.

The feature readiness summary must state which option was chosen.

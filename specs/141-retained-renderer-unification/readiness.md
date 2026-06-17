# Feature 141 Readiness Summary

Updated: 2026-06-17 18:31 CEST

Feature 141 implements P3 retained-renderer unification:

- `ControlInternals.CurrentNodeAssemblyResult` is the FSI-owned owner assembly result for in-flow scene, overlay scene, structural fingerprint, diagnostics, and child contribution metadata.
- `RetainedRender.RenderFragment` stores `Assembly: ControlInternals.CurrentNodeAssemblyResult` and `RetainedInvalidationEvidence` rather than independent retained-local subtree/overlay/fingerprint fields.
- `Composition.retainedReuseEvidence` exposes Feature 140 normalized modifier evidence for retained invalidation checks.
- Public Controls authoring APIs, public Scene constructors, and `ControlRenderResult` fields remain unchanged.

## Validation

| Area | Result |
|---|---|
| `dotnet restore FS.GG.Rendering.slnx` | PASS |
| `dotnet build tests/Controls.Tests/Controls.Tests.fsproj -c Debug --no-restore` | PASS |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature141 --no-build` | PASS, 10 tests including 200 generated direct/cold/warm retained equivalence cases |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature139 --no-build` | PASS, 8 tests |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature140 --no-build` | PASS, 17 tests |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "Controls public surface" --no-build` | PASS, 3 tests |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit --no-build` | PASS, 32 tests |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature092 --no-build` | PASS, 9 tests |
| `dotnet build FS.GG.Rendering.slnx -c Debug --no-restore` | PASS |
| Non-Controls broad deterministic test projects from quickstart | PASS |
| `dotnet fsi scripts/refresh-surface-baselines.fsx` | PASS, no tracked baseline diff |
| `dotnet run --project tests/Rendering.Harness -- offscreen --json --out artifacts/feature141-harness` | PASS, wrote `artifacts/feature141-harness/T1/run.json` with `status: passed` |

## Limitation

`dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature091 --no-build` was attempted and interrupted after more than two minutes without a result in this shell. The limitation is local validation scope; focused Feature 141, Feature 092, Feature 139, Feature 140, public-surface, and Audit Controls filters passed.

## Scope Confirmation

Feature 141 did not add full text shaping, overlay interaction state, portable scene serialization, compositor promotion/damage-scissored presentation, intrinsic layout protocol, or public retained renderer APIs.

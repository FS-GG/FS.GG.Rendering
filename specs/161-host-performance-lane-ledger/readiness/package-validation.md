# Feature 161 Package Validation

Status: `accepted-with-recorded-limitations`

## Validation Runs

- `compositor-readiness --feature 161`: package assembled.
- `Feature161HostLaneReadiness`: helper surface available.
- `compositor-performance --feature 161 --lane host-ledger`: host lane ledger available.
- `dotnet build FS.GG.Rendering.slnx --no-restore`: passed.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature161&HostLaneFact"`: passed, 4 tests.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature161&LaneLedger"`: passed, 4 tests.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature161&Unsupported"`: passed, 5 tests.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature161"`: passed, 11 tests.
- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-restore --filter "Feature161"`: passed, 4 tests.
- `dotnet test tests/Package.Tests/Package.Tests.fsproj --no-restore --filter "Feature161"`: passed, 4 tests.
- `dotnet test FS.GG.Rendering.slnx --no-restore`: passed, exit code 0.
- `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-restore -- compositor-readiness --feature 161 --out specs/161-host-performance-lane-ledger/readiness`: passed.

## Package Surface

- Rendering.Harness exposes Feature 161 host-lane ledger signatures, command, and readiness rendering.
- Testing package exposes `Feature161HostLaneReadiness` for package validation.
- FSI transcripts cover compositor host-lane authoring and host-lane readiness helper authoring.
- FSI compositor transcript: `compositor-host-lane-authoring.fsx`.
- FSI helper transcript: `feature161-host-lane-readiness-authoring.fsx`.
- Surface evidence: `FS.GG.UI.Testing.txt`, `Rendering.Harness.Compositor.txt`, and `Rendering.Harness.Perf.txt`.

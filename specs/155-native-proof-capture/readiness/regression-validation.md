# Feature 155 Regression Validation

Status: `accepted`

- `dotnet build FS.GG.Rendering.slnx --no-restore` passed.
- `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature155 --no-build` passed: 4 tests.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature155 --no-build` passed: 4 tests.
- `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature155 --no-build` passed: 2 tests.
- `dotnet fsi specs/155-native-proof-capture/readiness/fsi/native-proof-capture-authoring.fsx` passed.
- `dotnet test FS.GG.Rendering.slnx --no-restore` passed on retry. The first broad attempt reported a transient `Layout.Tests` testhost `AccessViolationException`; rerunning the specific `Feature151IntrinsicReuse` filter passed, and the full solution retry passed.
- Feature 155 keeps performance status separate from correctness: P7 current-host partial-redraw correctness is accepted, while performance remains `not-accepted`.

# Feature 159 Package Validation

Status: `passed-with-recorded-limitations`

## Validation Runs

- `dotnet build FS.GG.Rendering.slnx --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet fsi scripts/refresh-surface-baselines.fsx`: passed; intentional `FS.GG.UI.Testing` surface additions recorded.
- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-restore --filter "Feature159"`: passed, 3 tests.
- `dotnet test tests/Package.Tests/Package.Tests.fsproj --no-restore --filter "Feature159"`: passed, 4 tests.
- `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-readiness --feature 159 --out specs/159-layer-promotion-keys/readiness`: passed, package assembled.

## Package Surface

- Controls and SkiaViewer Feature 159 implementation details remain internal.
- Testing package exposes `Feature159Readiness` for generated-product/package validation.
- FSI transcripts cover content/placement identity, promotion command authoring, and readiness helper authoring.
- The shipped compositor performance claim remains `performance-not-accepted`.

# Feature 170 Validation Log

Status: `accepted`

| Command | Status | Evidence |
|---|---|---|
| `dotnet build src/Scene/Scene.fsproj -c Release --no-restore -v minimal` | ✅ passed | Scene retained/damage contracts compiled |
| `dotnet build src/Controls/Controls.fsproj -c Release --no-restore -v minimal` | ✅ passed | Controls retained inspection adapter compiled |
| `dotnet build src/Testing/Testing.fsproj -c Release --no-restore -v minimal` | ✅ passed | Testing retained validation/readiness helpers compiled |
| `dotnet build tests/Rendering.Harness/Rendering.Harness.fsproj -c Release --no-restore -v minimal` | ✅ passed | retained-inspection lane compiled |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --no-restore --filter Feature170` | ✅ passed | 4 tests, `lanes/validation-20260619-155116-212d54/retained-inspection/TestResults/controls/feature170-controls.trx` |
| `dotnet test tests/Testing.Tests/Testing.Tests.fsproj -c Release --no-restore --filter Feature170` | ✅ passed | 6 tests, `lanes/validation-20260619-155116-212d54/retained-inspection/TestResults/testing/feature170-testing.trx` |
| `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore --filter Feature170` | ✅ passed | 3 tests, `lanes/validation-20260619-155116-212d54/retained-inspection/TestResults/harness/feature170-harness.trx` |
| `dotnet run --project samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore -- --filter-test-list Feature170 --summary` | ✅ passed | 1 Expecto test, direct structured adoption evidence |
| `dotnet run --project samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore -- --filter-test-list VisualReadiness --summary` | ✅ passed | 5 Expecto tests, screenshot readiness parity |
| `dotnet test tests/Package.Tests/Package.Tests.fsproj -c Release --no-restore --filter Feature170` | ✅ passed | 3 package compatibility tests |
| `dotnet test tests/Package.Tests/Package.Tests.fsproj -c Release --no-restore --filter Surface` | ✅ passed | 32 package surface tests |
| `dotnet fsi scripts/run-validation-lanes.fsx --lane retained-inspection --out specs/170-retained-damage-inspection/readiness/lanes` | ✅ passed | `validation-20260619-155116-212d54`, elapsed `00:00:37.5334492` |
| `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/AntShowcase --mode refresh --pack --out specs/170-retained-damage-inspection/readiness/package-feed-post-merge` | ✅ passed | packed 14 packages at `0.1.32-preview.1` |
| `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/AntShowcase --mode proof --isolated-cache specs/170-retained-damage-inspection/readiness/package-feed-post-merge/nuget-cache --out specs/170-retained-damage-inspection/readiness/package-feed-post-merge` | ✅ passed | AntShowcase restored from the local feed with no source-rule violations |

## Lane Evidence

- Summary: `specs/170-retained-damage-inspection/readiness/lanes/validation-20260619-155116-212d54/summary.md`
- Summary JSON: `specs/170-retained-damage-inspection/readiness/lanes/validation-20260619-155116-212d54/summary.json`
- Lane result: `specs/170-retained-damage-inspection/readiness/lanes/validation-20260619-155116-212d54/retained-inspection/result.json`
- Lane diagnostics: `specs/170-retained-damage-inspection/readiness/lanes/validation-20260619-155116-212d54/retained-inspection/diagnostics.md`
- Lane log: `specs/170-retained-damage-inspection/readiness/lanes/validation-20260619-155116-212d54/retained-inspection/log.txt`
- Package versions: `specs/170-retained-damage-inspection/readiness/package-feed-post-merge/package-versions.md`
- Package pins: `specs/170-retained-damage-inspection/readiness/package-feed-post-merge/package-pins.md`
- Package source proof: `specs/170-retained-damage-inspection/readiness/package-feed-post-merge/source-proof.md`

## Notes

- The lane is optional and substitutes for `aggregate-solution` when explicitly selected.
- The accepted lane is under the five-minute target.
- Direct AntShowcase Expecto evidence is used for the sample test because the VSTest adapter returns success without useful filtered output for that sample project.
- Post-merge package evidence proves the local package feed and AntShowcase pins at `0.1.32-preview.1`.

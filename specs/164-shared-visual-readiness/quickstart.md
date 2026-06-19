# Quickstart: Shared Visual Readiness Tooling

## Prerequisites

- .NET SDK capable of `net10.0`
- Local repository dependencies restored
- For AntShowcase screenshot capture, a host where `Viewer.captureScreenshotEvidence` can produce offscreen PNGs
- Current local `FS.GG.UI.*` packages packed when running package-consuming sample validation

## 1. Validate the Testing Package Surface

After implementing the public API:

```bash
dotnet test tests/Testing.Tests/Testing.Tests.fsproj
./fake.sh build -t PackageSurfaceCheck
```

Expected outcome:

- Feature 164 Testing tests pass.
- `readiness/surface-baselines/FS.GG.UI.Testing.txt` intentionally includes the new visual-readiness members.

## 2. Validate Matrix, PNG, Reviewer, and Summary Behavior

Focused tests in `tests/Testing.Tests/Feature164VisualReadinessTests.fs` should cover:

- 3 pages x 2 themes x 2 sizes expands to 12 deterministic targets.
- Duplicate ids and duplicate paths are rejected.
- Missing, wrong-size, undecodable, degraded, and complete PNG artifacts receive the expected classification.
- Degraded captures require reasons and cannot be accepted as complete.
- Reviewer templates require one valid row per target.
- Missing, duplicate, malformed, unknown-target, and blocking reviewer records affect readiness as specified.
- Managed summary updates preserve manual content across at least 3 regenerations.
- Malformed markers fail without writing.

Expected outcome:

- The fixture classification matrix matches the expected statuses exactly.
- Manual content outside managed markers remains byte-for-byte unchanged.

## 3. Run Repository Package Checks

```bash
./fake.sh build -t CapabilityCheck
./fake.sh build -t PackLocal
./fake.sh build -t GeneratedProductCheck
```

Expected outcome:

- Testing package builds and packs.
- Generated-product validation still consumes the Testing package without extra runtime-only dependencies.

## 4. Validate AntShowcase Preferred Evidence

Pack local packages first, then run the preferred-size visual readiness command:

```bash
./fake.sh build -t PackLocal
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- visual-readiness --seed 1 --size 1600x1000 --themes light,dark --out specs/164-shared-visual-readiness/readiness/antshowcase-preferred
```

Expected outcome:

- 19 AntShowcase pages x 2 themes produce 38 required targets.
- Real captured screenshots are counted as complete.
- Reviewer classifications are required before accepted readiness.
- Contact sheet paths and summary JSON/Markdown are produced.

## 5. Validate AntShowcase Minimum Evidence

Run the representative minimum-size set:

```bash
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- visual-readiness --seed 1 --size 1280x800 --themes light,dark --pages data-collections,charts-statistical,charts-advanced,feedback-status,tpl-form,tpl-exception --out specs/164-shared-visual-readiness/readiness/antshowcase-minimum
```

Expected outcome:

- 6 pages x 2 themes produce 12 required targets.
- The readiness outcome matches the existing accepted minimum-size semantics after reviewer classifications are complete.

## 6. Validate AntShowcase Tests

```bash
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore --filter "Visual"
```

Expected outcome:

- Visual readiness tests assert that AntShowcase calls the shared Testing APIs.
- Preferred and minimum-size counts, reviewer gates, contact sheet references, and summary meanings match the pre-migration behavior.

## 7. Preserve Manual Summary Notes

Regenerate the visual-readiness section in the feature readiness summary:

```bash
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- visual-readiness --summarize specs/164-shared-visual-readiness/readiness/antshowcase-preferred --minimum-size specs/164-shared-visual-readiness/readiness/antshowcase-minimum --out specs/164-shared-visual-readiness/readiness
```

Expected outcome:

- Generated visual-readiness content is updated only inside managed markers.
- Manual package validation notes, full-solution limitations, and reviewer caveats outside the generated section are preserved.

## 8. Feature 164 Validation Results

Recorded results live under `specs/164-shared-visual-readiness/readiness/`.

- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature164`: passed, 8 tests.
- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj`: passed, 80 tests.
- `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore --filter "Visual"`: passed, 25 tests.
- Preferred AntShowcase visual readiness: 38/38 screenshots, blocked by pending reviewer classifications.
- Minimum AntShowcase visual readiness: 12/12 screenshots, blocked by pending reviewer classifications.
- `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "Surface baselines"`: passed, 11 tests.
- `dotnet build FS.GG.Rendering.slnx` and `dotnet build FS.GG.Rendering.slnx -c Release`: passed with 0 warnings and 0 errors.
- `dotnet pack FS.GG.Rendering.slnx -c Release --no-build -o ~/.local/share/nuget-local`: passed; existing missing-readme warnings remain.

Tooling limitations:

- Root `./fake.sh` is absent, so `CapabilityCheck`, `PackageSurfaceCheck`, `PackLocal`, and `GeneratedProductCheck` were recorded as tooling-limited with direct substitutes where available.
- `dotnet test template/base/tests/Product.Tests/Product.Tests.fsproj` is blocked by pre-existing template/base compile errors unrelated to Feature 164.

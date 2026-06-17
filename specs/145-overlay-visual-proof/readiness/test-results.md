# Feature 145 Test Results

Recorded: 2026-06-17 22:36 CEST.

## Capable-Host Visual Proof

Command:

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- overlay-visual-proof --out specs/145-overlay-visual-proof/readiness
```

Result: passed on X11 display `:1` with AMD Radeon GL renderer. The latest run closed the Feature 144 visual-proof caveat.

Three equivalent capable-host attempts:

| Run ID | Scenario | Status | Decision | Evidence labels |
|---|---|---|---|---|
| `20260617-203509-749` | `feature144-antshowcase-date-picker-reference` | passed | closed | open, closed |
| `20260617-203538-828` | `feature144-antshowcase-date-picker-reference` | passed | closed | open, closed |
| `20260617-203538-994` | `feature144-antshowcase-date-picker-reference` | passed | closed | open, closed |

Latest accepted artifacts:

- `artifacts/20260617-203538-994/open.png`
- `artifacts/20260617-203538-994/closed.png`

## Unsupported-Host Path

Command:

```bash
env -u DISPLAY -u WAYLAND_DISPLAY XDG_SESSION_TYPE= dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- overlay-visual-proof --out /tmp/feature145-unsupported-validation
```

Result: environment-limited, cause `missing-display`, no artifacts accepted, readiness decision `environment-gated`.

## Focused Tests

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature145|Feature144"
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj --filter "Feature145|Feature144|DatePicker"
```

Results:

- Rendering.Harness.Tests: passed, 10 tests.
- AntShowcase.Tests: passed, 5 tests.

## Full Validation

```bash
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature145|Feature144" --no-build
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj --filter "Feature145|Feature144|DatePicker"
```

Results:

- solution restore: passed.
- solution build: passed with 0 warnings and 0 errors.
- Rendering.Harness focused tests after build: passed, 10 tests.
- AntShowcase focused tests after build: passed, 5 tests.

`src/Testing`, `src/SkiaViewer`, public package surfaces, and surface baselines were not changed, so the optional focused `Testing`/`SkiaViewer` commands and baseline refresh were not required for this Tier 2 validation feature.

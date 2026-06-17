# Quickstart: Overlay Visual Proof

## Prerequisites

- .NET SDK that supports `net10.0`
- Local checkout on branch `145-overlay-visual-proof`
- For real visual proof: a host with display/offscreen capture support and a GL renderer
- For unsupported-host validation: a known no-display or no-GL environment

## Restore And Build

```bash
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx
```

Expected outcome: restore and build complete without warnings-as-errors failures.

## Validate Existing Behavioral Baseline

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "Feature144|Feature143"
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter "Feature144|Feature143"
dotnet test tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj --filter "Feature144|Feature143"
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj --filter "Feature144|Feature143|DatePicker"
```

Expected outcome: existing overlay routing, focus, dispatch, replay, and date-picker reference behavior remain
unchanged before visual proof is evaluated.

## Validate Visual-Proof Harness Behavior

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature145|Feature144"
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- overlay-visual-proof --out specs/145-overlay-visual-proof/readiness
```

Expected outcome:

- capable-host runs can accept only current-run, non-empty, scenario-linked open and closed artifacts
- unsupported hosts report environment-limited status with owner, cause, next proof path, and trust rationale
- blank, stale, missing, unreadable, or disconnected artifacts fail rather than passing
- visual and behavioral disagreements fail with a classified diagnostic

## Validate Unsupported-Host Disclosure

Use a separate output directory so this negative-path run does not overwrite the latest capable-host readiness record.

```bash
env -u DISPLAY -u WAYLAND_DISPLAY XDG_SESSION_TYPE= dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- overlay-visual-proof --out /tmp/feature145-unsupported-validation
```

Expected outcome: the run exits successfully with `environment-limited`, records `missing-display`, accepts no artifacts, and does not close the Feature 144 visual-proof caveat.

## Validate Screenshot Evidence Helpers If Touched

Run only if implementation changes `src/Testing`.

```bash
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter "Screenshot|Feature145"
dotnet fsi scripts/refresh-surface-baselines.fsx
```

Expected outcome: screenshot evidence validation still rejects unsupported records that claim screenshots,
successful records require live capture and non-blank pixel validation, and any public surface delta is documented
as Tier 1.

## Validate Viewer Capture If Touched

Run only if implementation changes `src/SkiaViewer`.

```bash
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter "Screenshot|Feature145|Feature063|Feature140"
dotnet fsi scripts/refresh-surface-baselines.fsx
```

Expected outcome: viewer capture behavior still records real screenshot proof only for accepted non-blank images,
and any public surface delta is documented as Tier 1.

## Review Readiness

After validation, inspect:

```text
specs/145-overlay-visual-proof/readiness/visual-proof.md
specs/145-overlay-visual-proof/readiness/unsupported-host.md
specs/145-overlay-visual-proof/readiness/correlation.md
specs/145-overlay-visual-proof/readiness/test-results.md
specs/145-overlay-visual-proof/readiness/artifacts/
```

Expected outcome:

- passed capable-host run closes the Feature 144 visual-proof caveat and links open/closed artifacts
- environment-limited run keeps the caveat open and names the next proof path
- failed run names the diagnostic category and does not authorize later workstreams to treat the caveat as closed
